#!/usr/bin/env python3
import csv
import json
import math
import os
import re
import subprocess
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path
from statistics import median


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Stats"

SKIP_DIRS = {
    ".git",
    ".idea",
    ".vs",
    ".vscode",
    "Library",
    "Logs",
    "Temp",
    "UserSettings",
    "obj",
    "bin",
    "Build",
    "Builds",
    "__pycache__",
}

VENDOR_MARKERS = (
    "Assets/Plugins/",
    "Assets/ThirdParty/",
    "Assets/LDtkUnity/",
    "Assets/TextMesh Pro/",
    "Packages/com.unity.",
    "Packages/nuget-packages/",
)

TEXT_EXTS = {
    ".cs",
    ".shader",
    ".hlsl",
    ".compute",
    ".asmdef",
    ".json",
    ".xml",
    ".yml",
    ".yaml",
    ".md",
    ".txt",
    ".uxml",
    ".uss",
    ".asset",
    ".prefab",
    ".unity",
    ".mat",
    ".controller",
    ".anim",
    ".ldtk",
    ".cginc",
    ".config",
    ".sh",
    ".bat",
}

CODE_EXTS = {
    ".cs",
    ".shader",
    ".hlsl",
    ".compute",
    ".uxml",
    ".uss",
    ".asmdef",
    ".json",
    ".yml",
    ".yaml",
    ".sh",
    ".bat",
}

ASSET_GROUPS = {
    ".png": "Raster Art",
    ".jpg": "Raster Art",
    ".jpeg": "Raster Art",
    ".tga": "Raster Art",
    ".psd": "Raster Art",
    ".aseprite": "Raster Art",
    ".prefab": "Prefab",
    ".unity": "Scene",
    ".asset": "Unity Asset",
    ".mat": "Material",
    ".shader": "Shader",
    ".shadergraph": "Shader",
    ".hlsl": "Shader",
    ".compute": "Shader",
    ".controller": "Animation",
    ".anim": "Animation",
    ".wav": "Audio",
    ".mp3": "Audio",
    ".ogg": "Audio",
    ".bank": "Audio",
    ".ttf": "Font",
    ".otf": "Font",
    ".sdf": "Font",
    ".ldtk": "Level Data",
    ".bytes": "Binary Data",
    ".dll": "Plugin Binary",
    ".zip": "Archive",
    ".pdf": "Document",
}

DOMAIN_RULES = [
    ("CI/CD", (".github/", "Scripts/", "ProjectConfig/", ".gitattributes", ".lfsconfig")),
    ("Rendering", ("Rendering/", "Shader", "Shaders/", "VisualEffects/", "Volume/", "RenderPasses/", "RenderFeatures/")),
    ("UI", ("/UI/", "UI Toolkit", ".uxml", ".uss", "MainMenu", "PauseMenu")),
    ("Gameplay Code", ("GamePlay/", "PlayerControl/", "Entities/", "GameCoreSystems/")),
    ("Level Design", ("LDtkProject/", "Scenes/", ".ldtk", ".unity")),
    ("Art", ("Assets/Arts/", "Assets/GameAssets/", ".png", ".jpg", ".jpeg", ".tga", ".psd")),
    ("Audio", ("FMOD/", ".wav", ".mp3", ".ogg", ".bank")),
    ("Tests", ("Assets/Tests/", "Test.", "Tests/")),
    ("Docs", ("README", "SECURITY", ".md", ".txt")),
    ("Config", ("ProjectSettings/", "Packages/", ".asmdef", ".json", ".asset")),
]

CS_KEYWORDS = [
    "MonoBehaviour",
    "ScriptableObject",
    "SerializeField",
    "Update",
    "FixedUpdate",
    "LateUpdate",
    "Awake",
    "Start",
    "OnEnable",
    "OnDisable",
    "OnDestroy",
    "CLogger",
    "Debug.Log",
    "Observable",
    "Subject",
    "StartCoroutine",
    "IEnumerator",
    "async",
    "await",
    "System.Linq",
    ".Where(",
    ".Select(",
    "typeof(",
    "GetType(",
    "Activator.",
    "Resources.Load",
    "try",
    "catch",
    "Zenject",
    "Inject",
]


def run_git(args):
    try:
        result = subprocess.run(
            ["git", *args],
            cwd=ROOT,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            check=False,
        )
        return result.stdout if result.returncode == 0 else ""
    except Exception:
        return ""


def run_cmd(args, timeout=20):
    try:
        result = subprocess.run(
            args,
            cwd=ROOT,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            timeout=timeout,
            check=False,
        )
        return result.returncode, result.stdout, result.stderr
    except Exception as exc:
        return 1, "", str(exc)


def parse_dt(value):
    if not value:
        return None
    try:
        return datetime.fromisoformat(value.replace("Z", "+00:00"))
    except Exception:
        return None


def hours_between(start, end):
    a, b = parse_dt(start), parse_dt(end)
    if not a or not b:
        return None
    return round((b - a).total_seconds() / 3600, 2)


def safe_div(a, b):
    return round(a / b, 4) if b else 0


def pct(a, b):
    return round(safe_div(a, b) * 100, 2)


def percentile(values, q):
    values = sorted(v for v in values if v is not None)
    if not values:
        return None
    if len(values) == 1:
        return values[0]
    pos = (len(values) - 1) * q
    lo = math.floor(pos)
    hi = math.ceil(pos)
    if lo == hi:
        return values[int(pos)]
    return values[lo] + (values[hi] - values[lo]) * (pos - lo)


def median_or_zero(values):
    values = [v for v in values if v is not None]
    return round(median(values), 2) if values else 0


def rel(path):
    return path.relative_to(ROOT).as_posix()


def is_vendor(path):
    p = path.as_posix()
    return any(p.startswith(marker) for marker in VENDOR_MARKERS)


def layer(path):
    parts = path.parts
    if not parts:
        return "Other"
    if parts[0] == "Assets":
        if len(parts) > 1:
            return f"Assets/{parts[1]}"
        return "Assets"
    if parts[0] == "Packages":
        return "Packages"
    if parts[0] == "ProjectSettings":
        return "ProjectSettings"
    if parts[0] == ".github":
        return "GitHub Actions"
    if parts[0] == "Stats":
        return "Stats"
    return parts[0]


def category_for(path, ext):
    p = path.as_posix()
    if path.name.endswith(".meta"):
        return "Unity Meta"
    if ext == ".cs":
        return "C# Code"
    if ext in {".shader", ".shadergraph", ".hlsl", ".compute", ".cginc"}:
        return "Rendering Code"
    if ext in {".uxml", ".uss"}:
        return "UI Toolkit"
    if ext in {".yml", ".yaml"} or p.startswith(".github/"):
        return "Automation"
    if ext in ASSET_GROUPS:
        return ASSET_GROUPS[ext]
    if ext in {".json", ".xml", ".asset", ".prefab", ".unity", ".mat", ".controller", ".anim", ".ldtk"}:
        return "Structured Text Asset"
    if ext in {".md", ".txt"}:
        return "Docs"
    return "Other"


def domain_for_path(path):
    p = path.as_posix()
    for domain, needles in DOMAIN_RULES:
        if any(needle in p for needle in needles):
            return domain
    if p.endswith(".cs"):
        return "Code"
    return "Other"


def read_text_sample(path, max_bytes=2_000_000):
    try:
        if path.stat().st_size > max_bytes:
            return None
        return path.read_text(encoding="utf-8", errors="ignore")
    except Exception:
        return None


def count_text(path):
    text = read_text_sample(path)
    if text is None:
        return None
    lines = text.splitlines()
    blank = sum(1 for line in lines if not line.strip())
    comment = sum(1 for line in lines if line.strip().startswith(("//", "#", "<!--", "*", "/*")))
    todos = sum(1 for line in lines if re.search(r"\b(TODO|FIXME|HACK|BUG)\b", line, re.I))
    lfs_pointer = text.startswith("version https://git-lfs.github.com/spec")
    return {
        "lines": len(lines),
        "blank_lines": blank,
        "comment_like_lines": comment,
        "todo_markers": todos,
        "lfs_pointer": lfs_pointer,
        "text": text,
    }


def scan_files():
    rows = []
    symbols = Counter()
    todo_rows = []
    for dirpath, dirnames, filenames in os.walk(ROOT):
        dirnames[:] = [d for d in dirnames if d not in SKIP_DIRS]
        base = Path(dirpath)
        for filename in filenames:
            path = base / filename
            if path == Path(__file__).resolve():
                pass
            try:
                st = path.stat()
            except OSError:
                continue
            rp = Path(rel(path))
            ext = "".join(rp.suffixes[-2:]) if rp.name.endswith(".meta") else rp.suffix.lower()
            simple_ext = rp.suffix.lower()
            text_stats = count_text(path) if simple_ext in TEXT_EXTS or rp.name.endswith(".meta") else None
            loc = text_stats["lines"] if text_stats else 0
            todos = text_stats["todo_markers"] if text_stats else 0
            lfs_pointer = text_stats["lfs_pointer"] if text_stats else False
            text = text_stats["text"] if text_stats else ""
            blank_lines = text_stats["blank_lines"] if text_stats else 0
            comment_like_lines = text_stats["comment_like_lines"] if text_stats else 0
            if simple_ext == ".cs" and text:
                symbols["classes"] += len(re.findall(r"\bclass\s+\w+", text))
                symbols["interfaces"] += len(re.findall(r"\binterface\s+\w+", text))
                symbols["structs"] += len(re.findall(r"\bstruct\s+\w+", text))
                symbols["enums"] += len(re.findall(r"\benum\s+\w+", text))
                symbols["records"] += len(re.findall(r"\brecord\s+\w+", text))
                symbols["serialized_fields"] += len(re.findall(r"\[SerializeField\]", text))
                symbols["public_methods"] += len(re.findall(r"\bpublic\s+(?:async\s+)?[\w<>,\[\]\s]+\s+\w+\s*\(", text))
                symbols["private_fields_m_prefix"] += len(re.findall(r"\bm_[A-Za-z0-9_]+", text))
            if todos and text:
                for idx, line in enumerate(text.splitlines(), start=1):
                    if re.search(r"\b(TODO|FIXME|HACK|BUG)\b", line, re.I):
                        todo_rows.append({"path": rp.as_posix(), "line": idx, "text": line.strip()[:220]})
            rows.append(
                {
                    "path": rp.as_posix(),
                    "top_layer": layer(rp),
                    "extension": ext or "(none)",
                    "simple_extension": simple_ext or "(none)",
                    "category": category_for(rp, simple_ext),
                    "domain": domain_for_path(rp),
                    "scope": "vendor" if is_vendor(rp) else "first_party",
                    "bytes": st.st_size,
                    "lines": loc,
                    "blank_lines": blank_lines,
                    "comment_like_lines": comment_like_lines,
                    "todo_markers": todos,
                    "lfs_pointer": lfs_pointer,
                    "modified_time": datetime.fromtimestamp(st.st_mtime).isoformat(timespec="seconds"),
                }
            )
    return rows, dict(symbols), todo_rows


def parse_git_history():
    commit_rows = []
    out = run_git(["log", "--format=%H%x09%an%x09%ae%x09%aI%x09%s"])
    for line in out.splitlines():
        parts = line.split("\t", 4)
        if len(parts) != 5:
            continue
        h, author, email, iso, subject = parts
        dt = datetime.fromisoformat(iso)
        commit_rows.append(
            {
                "hash": h,
                "short_hash": h[:8],
                "author": author,
                "email": email,
                "date": iso,
                "year_month": dt.strftime("%Y-%m"),
                "date_day": dt.strftime("%Y-%m-%d"),
                "weekday": dt.strftime("%A"),
                "hour": dt.hour,
                "subject": subject,
                "pr_number": int(re.search(r"\(#(\d+)\)", subject).group(1)) if re.search(r"\(#(\d+)\)", subject) else None,
                "is_merge_or_pr": bool(re.search(r"\(#\d+\)|Merge", subject)),
                "mentions_skip_ci": "skip ci" in subject.lower() or "skip cicd" in subject.lower(),
                "is_revert": subject.lower().startswith("revert"),
                "is_weekend": dt.weekday() >= 5,
                "is_night": dt.hour < 6 or dt.hour >= 22,
            }
        )

    churn_by_commit = defaultdict(lambda: {"insertions": 0, "deletions": 0, "files": 0})
    file_churn = defaultdict(lambda: {"insertions": 0, "deletions": 0, "touches": 0})
    author_churn = defaultdict(lambda: {"insertions": 0, "deletions": 0, "files_touched": 0})
    author_domain_churn = defaultdict(lambda: defaultdict(lambda: {"insertions": 0, "deletions": 0, "files_touched": 0}))
    current = None
    current_author = None
    out = run_git(["log", "--numstat", "--format=commit:%H%x09%an%x09%aI", "--no-renames"])
    for line in out.splitlines():
        if line.startswith("commit:"):
            meta = line[len("commit:") :].split("\t")
            current = meta[0] if meta else None
            current_author = meta[1] if len(meta) > 1 else None
            continue
        parts = line.split("\t")
        if len(parts) != 3 or current is None:
            continue
        ins_s, del_s, path = parts
        if ins_s == "-" or del_s == "-":
            continue
        ins, dels = int(ins_s), int(del_s)
        churn_by_commit[current]["insertions"] += ins
        churn_by_commit[current]["deletions"] += dels
        churn_by_commit[current]["files"] += 1
        file_churn[path]["insertions"] += ins
        file_churn[path]["deletions"] += dels
        file_churn[path]["touches"] += 1
        if current_author:
            domain = domain_for_path(Path(path))
            author_churn[current_author]["insertions"] += ins
            author_churn[current_author]["deletions"] += dels
            author_churn[current_author]["files_touched"] += 1
            author_domain_churn[current_author][domain]["insertions"] += ins
            author_domain_churn[current_author][domain]["deletions"] += dels
            author_domain_churn[current_author][domain]["files_touched"] += 1

    for row in commit_rows:
        row.update(churn_by_commit.get(row["hash"], {"insertions": 0, "deletions": 0, "files": 0}))

    author_domain_rows = []
    for author, domains in author_domain_churn.items():
        for domain, values in domains.items():
            author_domain_rows.append({"author": author, "domain": domain, **values, "churn": values["insertions"] + values["deletions"]})
    author_domain_rows.sort(key=lambda r: (r["author"], -r["churn"]))

    return commit_rows, file_churn, author_churn, author_domain_rows


def aggregate(files, commits, file_churn, author_churn, author_domain_rows):
    total_bytes = sum(r["bytes"] for r in files)
    first_party = [r for r in files if r["scope"] == "first_party" and not r["path"].startswith("Stats/")]
    all_no_stats = [r for r in files if not r["path"].startswith("Stats/")]
    code_rows = [r for r in first_party if r["simple_extension"] in CODE_EXTS]

    def group(rows, key):
        c = defaultdict(lambda: {"files": 0, "bytes": 0, "lines": 0, "todos": 0, "blank_lines": 0, "comment_like_lines": 0})
        for r in rows:
            k = r[key]
            c[k]["files"] += 1
            c[k]["bytes"] += r["bytes"]
            c[k]["lines"] += r["lines"]
            c[k]["todos"] += r["todo_markers"]
            c[k]["blank_lines"] += r.get("blank_lines", 0)
            c[k]["comment_like_lines"] += r.get("comment_like_lines", 0)
        return [{"name": k, **v} for k, v in sorted(c.items(), key=lambda kv: kv[1]["bytes"], reverse=True)]

    contributor = defaultdict(lambda: {"commits": 0, "insertions": 0, "deletions": 0, "files_touched": 0})
    for c in commits:
        contributor[c["author"]]["commits"] += 1
    for author, data in author_churn.items():
        contributor[author]["insertions"] += data["insertions"]
        contributor[author]["deletions"] += data["deletions"]
        contributor[author]["files_touched"] += data["files_touched"]
    contributor_rows = [{"author": k, **v} for k, v in sorted(contributor.items(), key=lambda kv: kv[1]["commits"], reverse=True)]

    month_rows = []
    month_counter = defaultdict(lambda: {"commits": 0, "insertions": 0, "deletions": 0})
    for c in commits:
        m = month_counter[c["year_month"]]
        m["commits"] += 1
        m["insertions"] += c["insertions"]
        m["deletions"] += c["deletions"]
    for month, data in sorted(month_counter.items()):
        month_rows.append({"month": month, **data})

    day_rows = []
    day_counter = defaultdict(lambda: {"commits": 0, "insertions": 0, "deletions": 0})
    for c in commits:
        d = day_counter[c["date_day"]]
        d["commits"] += 1
        d["insertions"] += c["insertions"]
        d["deletions"] += c["deletions"]
    for day, data in sorted(day_counter.items()):
        day_rows.append({"date": day, **data})

    hour_rows = [{"hour": h, "commits": 0} for h in range(24)]
    for c in commits:
        hour_rows[c["hour"]]["commits"] += 1

    weekday_order = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"]
    weekday_counter = Counter(c["weekday"] for c in commits)
    weekday_rows = [{"weekday": d, "commits": weekday_counter[d]} for d in weekday_order]

    active_days = sorted({c["date_day"] for c in commits})
    longest_streak = 0
    current_streak = 0
    prev_day = None
    for day in active_days:
        dt = datetime.fromisoformat(day)
        if prev_day is not None and (dt - prev_day).days == 1:
            current_streak += 1
        else:
            current_streak = 1
        longest_streak = max(longest_streak, current_streak)
        prev_day = dt
    recent_streak = 0
    prev_day = None
    for day in reversed(active_days):
        dt = datetime.fromisoformat(day)
        if prev_day is not None and (prev_day - dt).days != 1:
            break
        recent_streak += 1
        prev_day = dt

    burst_days = sorted(day_rows, key=lambda r: (r["commits"], r["insertions"] + r["deletions"]), reverse=True)[:20]
    pr_numbers = sorted({c["pr_number"] for c in commits if c.get("pr_number")})

    top_churn_files = [
        {
            "path": path,
            "insertions": v["insertions"],
            "deletions": v["deletions"],
            "touches": v["touches"],
            "churn": v["insertions"] + v["deletions"],
        }
        for path, v in file_churn.items()
    ]
    top_churn_files.sort(key=lambda r: r["churn"], reverse=True)

    commit_subjects = " ".join(c["subject"] for c in commits).lower()
    keyword_rows = []
    for word in ["fix", "feat", "update", "refactor", "add", "delete", "lighting", "ldtk", "ui", "ci", "save", "player", "rainrust", "revert"]:
        keyword_rows.append({"keyword": word, "count": len(re.findall(re.escape(word), commit_subjects))})
    keyword_rows.sort(key=lambda r: r["count"], reverse=True)

    summary = {
        "generated_at": datetime.now(timezone.utc).astimezone().isoformat(timespec="seconds"),
        "project_root": str(ROOT),
        "tracked_files_seen": len(all_no_stats),
        "first_party_files": len(first_party),
        "vendor_files": sum(1 for r in all_no_stats if r["scope"] == "vendor"),
        "total_bytes": total_bytes,
        "first_party_bytes": sum(r["bytes"] for r in first_party),
        "first_party_lines": sum(r["lines"] for r in first_party),
        "first_party_code_lines": sum(r["lines"] for r in code_rows),
        "lfs_pointer_files": sum(1 for r in all_no_stats if r["lfs_pointer"]),
        "todo_markers": sum(r["todo_markers"] for r in first_party),
        "commit_count": len(commits),
        "contributor_count": len(contributor_rows),
        "first_commit": commits[-1]["date"] if commits else None,
        "latest_commit": commits[0]["date"] if commits else None,
        "pr_or_merge_subjects": sum(1 for c in commits if c["is_merge_or_pr"]),
        "skip_ci_subjects": sum(1 for c in commits if c["mentions_skip_ci"]),
        "reverts": sum(1 for c in commits if c["is_revert"]),
        "weekend_commits": sum(1 for c in commits if c["is_weekend"]),
        "weekend_commit_pct": pct(sum(1 for c in commits if c["is_weekend"]), len(commits)),
        "night_commits": sum(1 for c in commits if c["is_night"]),
        "night_commit_pct": pct(sum(1 for c in commits if c["is_night"]), len(commits)),
        "active_days": len(active_days),
        "longest_commit_streak_days": longest_streak,
        "recent_commit_streak_days": recent_streak,
        "inferred_pr_count": len(pr_numbers),
        "git_status_note": "git status was not used because local Git LFS clean filter failed on at least one PNG; history and tracked files were read with log/ls-files compatible commands.",
    }

    return {
        "summary": summary,
        "extension_summary": group(all_no_stats, "extension"),
        "category_summary": group(all_no_stats, "category"),
        "first_party_category_summary": group(first_party, "category"),
        "directory_summary": group(all_no_stats, "top_layer"),
        "domain_summary": group(all_no_stats, "domain"),
        "first_party_domain_summary": group(first_party, "domain"),
        "scope_summary": group(all_no_stats, "scope"),
        "contributors": contributor_rows,
        "author_domain_churn": author_domain_rows,
        "commits_by_month": month_rows,
        "commits_by_day": day_rows,
        "burst_days": burst_days,
        "commits_by_hour": hour_rows,
        "commits_by_weekday": weekday_rows,
        "top_churn_files": top_churn_files[:80],
        "commit_keywords": keyword_rows,
        "largest_files": sorted(all_no_stats, key=lambda r: r["bytes"], reverse=True)[:80],
        "latest_commits": commits[:40],
    }


def parse_unity_metadata():
    metadata = {"unity_version": None, "packages": [], "addressable_groups": [], "build_scenes": []}
    version_file = ROOT / "ProjectSettings" / "ProjectVersion.txt"
    text = read_text_sample(version_file) if version_file.exists() else None
    if text:
        match = re.search(r"m_EditorVersion:\s*(.+)", text)
        if match:
            metadata["unity_version"] = match.group(1).strip()
    manifest = ROOT / "Packages" / "manifest.json"
    if manifest.exists():
        try:
            data = json.loads(manifest.read_text(encoding="utf-8"))
            deps = data.get("dependencies", {})
            metadata["packages"] = [{"name": k, "version": v} for k, v in sorted(deps.items())]
        except Exception as exc:
            metadata["packages_error"] = str(exc)
    group_dir = ROOT / "Assets" / "AddressableAssetsData" / "AssetGroups"
    if group_dir.exists():
        metadata["addressable_groups"] = sorted(p.stem for p in group_dir.glob("*.asset"))
    scenes_file = ROOT / "ProjectSettings" / "EditorBuildSettings.asset"
    text = read_text_sample(scenes_file) if scenes_file.exists() else None
    if text:
        metadata["build_scenes"] = re.findall(r"path:\s*(Assets/.+?\.unity)", text)
    return metadata


def strip_csharp_comments(text):
    text = re.sub(r"/\*.*?\*/", "", text, flags=re.S)
    text = re.sub(r"//.*", "", text)
    return text


def analyze_code_content():
    file_rows = []
    symbol_rows = []
    namespace_counter = Counter()
    keyword_counter = Counter()
    method_rows = []
    field_violation_rows = []
    magic_number_rows = []
    api_usage_rows = []
    for path in ROOT.rglob("*.cs"):
        rp = Path(rel(path))
        if any(part in SKIP_DIRS for part in rp.parts) or is_vendor(rp):
            continue
        text = read_text_sample(path, max_bytes=3_500_000)
        if not text:
            continue
        clean = strip_csharp_comments(text)
        lines = text.splitlines()
        code_lines = [line for line in clean.splitlines() if line.strip()]
        comment_like = sum(1 for line in lines if line.strip().startswith(("//", "/*", "*")))
        namespaces = re.findall(r"\bnamespace\s+([A-Za-z0-9_.]+)", clean)
        for ns in namespaces:
            namespace_counter[ns] += 1
        type_matches = list(re.finditer(r"\b(class|struct|interface|enum|record)\s+([A-Za-z_][A-Za-z0-9_]*)", clean))
        type_counts = Counter(m.group(1) for m in type_matches)
        lifecycle_counts = Counter()
        for name in ["Awake", "Start", "Update", "FixedUpdate", "LateUpdate", "OnEnable", "OnDisable", "OnDestroy"]:
            lifecycle_counts[name] = len(re.findall(rf"\b{name}\s*\(", clean))
        keyword_counts = {key: clean.count(key) for key in CS_KEYWORDS}
        for key, count in keyword_counts.items():
            keyword_counter[key] += count
        methods = re.findall(
            r"(?:public|private|protected|internal|static|virtual|override|async|sealed|partial|\s)+\s+[A-Za-z0-9_<>,\[\]\?]+\s+([A-Za-z_][A-Za-z0-9_]*)\s*\([^;{{}}]*\)\s*\{",
            clean,
        )
        branches = len(re.findall(r"\b(if|else if|for|foreach|while|case|catch|switch)\b|&&|\|\||\?", clean))
        approximate_complexity = branches + 1
        public_fields = len(re.findall(r"\bpublic\s+(?!class|struct|interface|enum|record|static|void)[A-Za-z0-9_<>,\[\]\?]+\s+[A-Za-z_][A-Za-z0-9_]*\s*(?:=|;)", clean))
        private_fields = re.findall(r"\b(?:private|protected|internal)\s+(?:readonly\s+)?[A-Za-z0-9_<>,\[\]\?]+\s+([A-Za-z_][A-Za-z0-9_]*)\s*(?:=|;)", clean)
        bad_private_fields = [name for name in private_fields if not name.startswith("m_")]
        for name in bad_private_fields[:30]:
            field_violation_rows.append({"path": rp.as_posix(), "field": name, "rule": "private/protected/internal field should start with m_"})
        numbers = re.findall(r"(?<![A-Za-z0-9_])(?:\d+\.\d+|\d+)(?:f)?(?![A-Za-z0-9_])", clean)
        suspicious_numbers = [n for n in numbers if n not in {"0", "1", "2", "10", "100", "1000"}]
        if suspicious_numbers:
            magic_number_rows.append({"path": rp.as_posix(), "magic_numbers": len(suspicious_numbers), "sample": ", ".join(suspicious_numbers[:12])})
        api_hits = {
            "UnityEngine": clean.count("UnityEngine"),
            "Instantiate": clean.count("Instantiate("),
            "Destroy": clean.count("Destroy("),
            "GetComponent": clean.count("GetComponent"),
            "FindObject": clean.count("FindObject"),
            "Resources.Load": clean.count("Resources.Load"),
            "SceneManager": clean.count("SceneManager"),
            "Addressables": clean.count("Addressables"),
            "CLogger": clean.count("CLogger"),
            "Debug.Log": clean.count("Debug.Log"),
        }
        for api, hits in api_hits.items():
            if hits:
                api_usage_rows.append({"path": rp.as_posix(), "api": api, "hits": hits})
        file_rows.append(
            {
                "path": rp.as_posix(),
                "domain": domain_for_path(rp),
                "lines": len(lines),
                "code_lines": len(code_lines),
                "comment_like_lines": comment_like,
                "comment_density_pct": pct(comment_like, len(lines)),
                "classes": type_counts["class"],
                "structs": type_counts["struct"],
                "interfaces": type_counts["interface"],
                "enums": type_counts["enum"],
                "records": type_counts["record"],
                "methods": len(methods),
                "approx_complexity": approximate_complexity,
                "complexity_per_100_lines": round(approximate_complexity / max(len(code_lines), 1) * 100, 2),
                "mono_behaviour_refs": clean.count("MonoBehaviour"),
                "scriptable_object_refs": clean.count("ScriptableObject"),
                "serialize_fields": clean.count("[SerializeField]"),
                "update_methods": lifecycle_counts["Update"] + lifecycle_counts["FixedUpdate"] + lifecycle_counts["LateUpdate"],
                "lifecycle_methods": sum(lifecycle_counts.values()),
                "public_fields": public_fields,
                "private_fields": len(private_fields),
                "bad_private_fields": len(bad_private_fields),
                "m_prefix_compliance_pct": round(100 - pct(len(bad_private_fields), len(private_fields)), 2) if private_fields else 100,
                "todo_markers": len(re.findall(r"\b(TODO|FIXME|HACK|BUG)\b", text, re.I)),
                "coroutine_hits": clean.count("StartCoroutine") + clean.count("IEnumerator"),
                "async_await_hits": clean.count("async") + clean.count("await"),
                "linq_hits": clean.count("System.Linq") + clean.count(".Where(") + clean.count(".Select("),
                "reflection_hits": clean.count("typeof(") + clean.count("GetType(") + clean.count("Activator."),
                "resources_load_hits": clean.count("Resources.Load"),
                "try_catch_hits": clean.count("try") + clean.count("catch"),
                "magic_numbers": len(suspicious_numbers),
            }
        )
        for kind, count in type_counts.items():
            if count:
                symbol_rows.append({"path": rp.as_posix(), "symbol_kind": kind, "count": count})
        for method in methods:
            method_rows.append({"path": rp.as_posix(), "method": method})

    summary = {
        "cs_files": len(file_rows),
        "total_cs_lines": sum(r["lines"] for r in file_rows),
        "total_code_lines": sum(r["code_lines"] for r in file_rows),
        "mono_behaviour_files": sum(1 for r in file_rows if r["mono_behaviour_refs"] > 0),
        "scriptable_object_files": sum(1 for r in file_rows if r["scriptable_object_refs"] > 0),
        "serialize_fields": sum(r["serialize_fields"] for r in file_rows),
        "update_method_files": sum(1 for r in file_rows if r["update_methods"] > 0),
        "public_fields": sum(r["public_fields"] for r in file_rows),
        "private_field_m_prefix_compliance_pct": round(
            100 - pct(sum(r["bad_private_fields"] for r in file_rows), sum(r["private_fields"] for r in file_rows)), 2
        )
        if sum(r["private_fields"] for r in file_rows)
        else 100,
        "median_complexity_per_file": median_or_zero([r["approx_complexity"] for r in file_rows]),
        "comment_density_pct": pct(sum(r["comment_like_lines"] for r in file_rows), sum(r["lines"] for r in file_rows)),
        "todo_markers": sum(r["todo_markers"] for r in file_rows),
    }
    top_complex = sorted(file_rows, key=lambda r: (r["approx_complexity"], r["lines"]), reverse=True)[:50]
    top_long = sorted(file_rows, key=lambda r: r["lines"], reverse=True)[:50]
    lifecycle_rows = []
    for name in ["Awake", "Start", "Update", "FixedUpdate", "LateUpdate", "OnEnable", "OnDisable", "OnDestroy"]:
        lifecycle_rows.append({"lifecycle": name, "hits": sum(1 for r in file_rows if name in Path(r["path"]).name) + keyword_counter[name]})
    namespace_rows = [{"namespace": ns, "files": count} for ns, count in namespace_counter.most_common()]
    keyword_rows = [{"keyword": k, "hits": v} for k, v in keyword_counter.most_common()]
    api_summary = defaultdict(int)
    for row in api_usage_rows:
        api_summary[row["api"]] += row["hits"]
    api_summary_rows = [{"api": api, "hits": hits} for api, hits in sorted(api_summary.items(), key=lambda kv: kv[1], reverse=True)]
    return {
        "summary": summary,
        "code_file_metrics": file_rows,
        "code_symbol_rows": symbol_rows,
        "namespace_summary": namespace_rows,
        "keyword_summary": keyword_rows,
        "api_usage": api_usage_rows,
        "api_usage_summary": api_summary_rows,
        "top_complex_files": top_complex,
        "top_long_files": top_long,
        "method_rows": method_rows,
        "field_violation_rows": field_violation_rows,
        "magic_number_rows": sorted(magic_number_rows, key=lambda r: r["magic_numbers"], reverse=True),
        "lifecycle_summary": lifecycle_rows,
    }


def parse_yamlish_blocks(text):
    return [line for line in text.splitlines() if line.strip().startswith("- ")]


def analyze_unity_assets():
    scene_rows = []
    prefab_rows = []
    yaml_asset_rows = []
    guid_counter = Counter()
    guid_file_counter = defaultdict(set)
    all_meta_guids = {}
    for meta in ROOT.rglob("*.meta"):
        rp = Path(rel(meta))
        if any(part in SKIP_DIRS for part in rp.parts):
            continue
        text = read_text_sample(meta, max_bytes=100_000)
        if not text:
            continue
        match = re.search(r"guid:\s*([a-f0-9]{32})", text)
        if match:
            asset_path = rp.as_posix()[:-5]
            all_meta_guids[match.group(1)] = asset_path

    for ext, target in [("*.unity", scene_rows), ("*.prefab", prefab_rows), ("*.asset", yaml_asset_rows)]:
        for path in ROOT.rglob(ext):
            rp = Path(rel(path))
            if any(part in SKIP_DIRS for part in rp.parts) or is_vendor(rp):
                continue
            text = read_text_sample(path, max_bytes=8_000_000)
            if not text:
                continue
            guids = re.findall(r"guid:\s*([a-f0-9]{32})", text)
            for g in guids:
                guid_counter[g] += 1
                guid_file_counter[g].add(rp.as_posix())
            row = {
                "path": rp.as_posix(),
                "lines": len(text.splitlines()),
                "bytes": path.stat().st_size,
                "game_objects": text.count("GameObject:"),
                "components": len(re.findall(r"--- !u!\d+ &", text)),
                "mono_behaviours": text.count("MonoBehaviour:"),
                "transforms": text.count("Transform:") + text.count("RectTransform:"),
                "sprite_renderers": text.count("SpriteRenderer:"),
                "colliders_2d": len(re.findall(r"(Collider2D|BoxCollider2D|CircleCollider2D|PolygonCollider2D)", text)),
                "rigidbodies_2d": text.count("Rigidbody2D:"),
                "guid_refs": len(guids),
                "unique_guid_refs": len(set(guids)),
                "complexity_score": len(text.splitlines()) + len(guids) * 3 + text.count("MonoBehaviour:") * 20,
            }
            target.append(row)
    reference_rows = []
    for guid, hits in guid_counter.most_common(120):
        reference_rows.append(
            {
                "guid": guid,
                "asset_path": all_meta_guids.get(guid, "(external or package)"),
                "references": hits,
                "referencing_files": len(guid_file_counter[guid]),
            }
        )
    orphan_rows = []
    for guid, asset_path in all_meta_guids.items():
        if guid_counter[guid] == 0 and not asset_path.endswith(".meta") and not is_vendor(Path(asset_path)):
            orphan_rows.append({"guid": guid, "asset_path": asset_path})
    return {
        "scene_complexity": sorted(scene_rows, key=lambda r: r["complexity_score"], reverse=True),
        "prefab_complexity": sorted(prefab_rows, key=lambda r: r["complexity_score"], reverse=True),
        "asset_complexity": sorted(yaml_asset_rows, key=lambda r: r["complexity_score"], reverse=True),
        "guid_reference_summary": reference_rows,
        "potential_orphan_assets": orphan_rows[:300],
        "summary": {
            "scenes_analyzed": len(scene_rows),
            "prefabs_analyzed": len(prefab_rows),
            "yaml_assets_analyzed": len(yaml_asset_rows),
            "unique_meta_guids": len(all_meta_guids),
            "referenced_guids": len(guid_counter),
            "potential_orphan_assets": len(orphan_rows),
        },
    }


def analyze_addressables():
    rows = []
    group_dir = ROOT / "Assets" / "AddressableAssetsData" / "AssetGroups"
    if not group_dir.exists():
        return rows
    for path in group_dir.glob("*.asset"):
        text = read_text_sample(path, max_bytes=2_000_000) or ""
        rows.append(
            {
                "group": path.stem,
                "path": rel(path),
                "lines": len(text.splitlines()),
                "guid_refs": len(re.findall(r"guid:\s*([a-f0-9]{32})", text)),
                "entries_hint": text.count("m_AssetGUID") + text.count("m_Address"),
                "schemas": text.count("m_SchemaObjects"),
            }
        )
    return sorted(rows, key=lambda r: (r["entries_hint"], r["guid_refs"]), reverse=True)


def analyze_ldtk():
    rows = []
    for path in ROOT.rglob("*.ldtk"):
        rp = Path(rel(path))
        if any(part in SKIP_DIRS for part in rp.parts):
            continue
        try:
            data = json.loads(path.read_text(encoding="utf-8"))
        except Exception:
            continue
        levels = data.get("levels", [])
        defs = data.get("defs", {})
        layer_defs = defs.get("layers", [])
        entity_defs = defs.get("entities", [])
        tile_defs = defs.get("tilesets", [])
        total_layers = sum(len(level.get("layerInstances") or []) for level in levels)
        total_entities = 0
        total_tiles = 0
        for level in levels:
            for layer in level.get("layerInstances") or []:
                total_entities += len(layer.get("entityInstances") or [])
                total_tiles += len(layer.get("gridTiles") or []) + len(layer.get("autoLayerTiles") or [])
        rows.append(
            {
                "path": rp.as_posix(),
                "levels": len(levels),
                "layer_definitions": len(layer_defs),
                "entity_definitions": len(entity_defs),
                "tileset_definitions": len(tile_defs),
                "placed_layers": total_layers,
                "placed_entities": total_entities,
                "placed_tiles": total_tiles,
                "bytes": path.stat().st_size,
            }
        )
    return rows


def analyze_asmdefs():
    asm_rows = []
    dep_rows = []
    for path in ROOT.rglob("*.asmdef"):
        rp = Path(rel(path))
        if any(part in SKIP_DIRS for part in rp.parts):
            continue
        try:
            data = json.loads(path.read_text(encoding="utf-8"))
        except Exception:
            continue
        name = data.get("name") or path.stem
        refs = data.get("references", []) or []
        asm_rows.append(
            {
                "path": rp.as_posix(),
                "name": name,
                "root_namespace": data.get("rootNamespace", ""),
                "references": len(refs),
                "include_platforms": ",".join(data.get("includePlatforms", []) or []),
                "allow_unsafe_code": bool(data.get("allowUnsafeCode", False)),
                "auto_referenced": data.get("autoReferenced", True),
            }
        )
        for ref in refs:
            dep_rows.append({"from": name, "to": str(ref).replace("GUID:", ""), "from_path": rp.as_posix()})
    return asm_rows, dep_rows


def analyze_workflows():
    workflow_rows = []
    workflow_dir = ROOT / ".github" / "workflows"
    if not workflow_dir.exists():
        return workflow_rows
    for path in sorted(list(workflow_dir.glob("*.yml")) + list(workflow_dir.glob("*.yaml"))):
        text = read_text_sample(path, max_bytes=800_000) or ""
        jobs = len(re.findall(r"^\s{2}[A-Za-z0-9_-]+:\s*$", text, flags=re.M))
        steps = len(re.findall(r"^\s*-\s+name:", text, flags=re.M))
        uses = len(re.findall(r"\buses:\s*", text))
        runs = len(re.findall(r"\brun:\s*", text))
        triggers = len(re.findall(r"^\s{2,}(push|pull_request|workflow_dispatch|schedule|release|issues|issue_comment):", text, flags=re.M))
        workflow_rows.append(
            {
                "workflow": path.name,
                "path": rel(path),
                "lines": len(text.splitlines()),
                "jobs": jobs,
                "steps": steps,
                "uses_actions": uses,
                "run_commands": runs,
                "triggers_hint": triggers,
                "complexity_score": jobs * 5 + steps * 2 + uses + runs + len(text.splitlines()) // 20,
            }
        )
    return sorted(workflow_rows, key=lambda r: r["complexity_score"], reverse=True)


def analyze_naming(files):
    rows = []
    checked = 0
    snake = re.compile(r"^[a-z0-9]+(?:_[a-zA-Z0-9]+)*(?:_[0-9]{3})?$")
    for r in files:
        path = r["path"]
        if path.startswith("Stats/") or r["scope"] == "vendor":
            continue
        if not path.startswith("Assets/"):
            continue
        name = Path(path).stem
        if Path(path).suffix == ".meta":
            continue
        checked += 1
        reasons = []
        if " " in name:
            reasons.append("contains space")
        if re.search(r"[\u4e00-\u9fff]", name):
            reasons.append("contains CJK")
        if not snake.match(name) and Path(path).suffix.lower() in {".png", ".wav", ".mp3", ".ogg", ".prefab", ".mat", ".asset", ".anim", ".controller", ".uxml", ".uss"}:
            reasons.append("not snake_case-ish")
        if len(name) > 56:
            reasons.append("very long")
        if reasons:
            rows.append({"path": path, "name": name, "reasons": "; ".join(reasons)})
    return {
        "summary": {"checked_assets": checked, "violations": len(rows), "compliance_pct": round(100 - pct(len(rows), checked), 2) if checked else 100},
        "violations": rows,
    }


def analyze_animation_sequences(files):
    grouped = defaultdict(list)
    for r in files:
        path = r["path"]
        if r["scope"] == "vendor" or not path.startswith("Assets/"):
            continue
        suffix = Path(path).suffix.lower()
        if suffix not in {".png", ".jpg", ".jpeg", ".tga"}:
            continue
        stem = Path(path).stem
        match = re.search(r"(.+?)[_\-\s]?(\d{3,4})$", stem)
        if not match:
            continue
        prefix, num = match.group(1), int(match.group(2))
        grouped[(str(Path(path).parent), prefix)].append(num)
    rows = []
    for (folder, prefix), nums in grouped.items():
        nums = sorted(set(nums))
        if len(nums) < 2:
            continue
        expected = set(range(nums[0], nums[-1] + 1))
        missing = sorted(expected - set(nums))
        rows.append(
            {
                "folder": folder,
                "prefix": prefix,
                "frames": len(nums),
                "first": nums[0],
                "last": nums[-1],
                "missing_count": len(missing),
                "missing_sample": ",".join(map(str, missing[:20])),
            }
        )
    return sorted(rows, key=lambda r: (r["missing_count"], r["frames"]), reverse=True)


def analyze_file_age(files):
    tracked = {r["path"] for r in files if not r["path"].startswith("Stats/")}
    first_seen = {}
    last_seen = {}
    current_date = None
    out = run_git(["log", "--format=commit:%aI", "--name-only"])
    for raw in out.splitlines():
        line = raw.strip()
        if not line:
            continue
        if line.startswith("commit:"):
            current_date = line[len("commit:") :]
            continue
        if line not in tracked:
            continue
        if line not in last_seen:
            last_seen[line] = current_date
        first_seen[line] = current_date
    rows = []
    for path in tracked:
        last = last_seen.get(path, "")
        dt = parse_dt(last)
        age_days = (datetime.now(dt.tzinfo) - dt).days if dt else ""
        rows.append({"path": path, "last_commit": last, "first_commit": first_seen.get(path, ""), "days_since_last_commit": age_days})
    return sorted(rows, key=lambda r: r["days_since_last_commit"] if isinstance(r["days_since_last_commit"], int) else -1, reverse=True)


def parse_repo_full_name():
    remote = run_git(["remote", "get-url", "origin"]).strip()
    match = re.search(r"github.com[:/](.+?)(?:\.git)?$", remote)
    return match.group(1) if match else ""


def fetch_github_with_gh(repo):
    meta = {"repo": repo, "source": "gh", "available": False, "error": ""}
    if not repo:
        meta["error"] = "No GitHub origin remote detected."
        return meta, [], [], []
    code, out, err = run_cmd(["gh", "auth", "status"], timeout=12)
    if code != 0:
        meta["error"] = "gh auth unavailable: " + (err.strip() or out.strip()).splitlines()[0]
        return meta, [], [], []
    pr_code, pr_out, pr_err = run_cmd(
        [
            "gh",
            "pr",
            "list",
            "--repo",
            repo,
            "--state",
            "all",
            "--limit",
            "500",
            "--json",
            "number,title,state,isDraft,author,createdAt,updatedAt,closedAt,mergedAt,commits,additions,deletions,changedFiles,baseRefName,headRefName,labels,reviewDecision",
        ],
        timeout=40,
    )
    if pr_code != 0:
        meta["error"] = "gh pr list failed: " + (pr_err.strip() or pr_out.strip()).splitlines()[0]
        return meta, [], [], []
    try:
        prs = json.loads(pr_out)
    except Exception as exc:
        meta["error"] = f"gh pr JSON parse failed: {exc}"
        return meta, [], [], []
    issue_code, issue_out, _ = run_cmd(
        ["gh", "issue", "list", "--repo", repo, "--state", "all", "--limit", "500", "--json", "number,title,state,author,createdAt,updatedAt,closedAt,labels,assignees"],
        timeout=40,
    )
    issues = json.loads(issue_out) if issue_code == 0 and issue_out.strip() else []
    run_code, run_out, _ = run_cmd(
        ["gh", "run", "list", "--repo", repo, "--limit", "300", "--json", "databaseId,name,workflowName,status,conclusion,createdAt,updatedAt,event,headBranch"],
        timeout=40,
    )
    runs = json.loads(run_out) if run_code == 0 and run_out.strip() else []
    meta["available"] = True
    return meta, prs, issues, runs


def analyze_github(commits):
    repo = parse_repo_full_name()
    gh_meta, gh_prs, gh_issues, gh_runs = fetch_github_with_gh(repo)
    inferred = []
    seen = set()
    for c in commits:
        if not c.get("pr_number") or c["pr_number"] in seen:
            continue
        seen.add(c["pr_number"])
        inferred.append(
            {
                "number": c["pr_number"],
                "title": re.sub(r"\s*\(#\d+\)\s*$", "", c["subject"]),
                "merged_at": c["date"],
                "author": c["author"],
                "source": "git_subject",
                "commits_hint": 1,
                "additions_hint": c["insertions"],
                "deletions_hint": c["deletions"],
                "changed_files_hint": c["files"],
            }
        )
    pr_rows = []
    if gh_prs:
        for pr in gh_prs:
            created = pr.get("createdAt")
            merged = pr.get("mergedAt")
            closed = pr.get("closedAt")
            end = merged or closed
            labels = pr.get("labels") or []
            pr_rows.append(
                {
                    "number": pr.get("number"),
                    "title": pr.get("title", ""),
                    "state": pr.get("state", ""),
                    "merged": bool(merged),
                    "draft": bool(pr.get("isDraft")),
                    "author": (pr.get("author") or {}).get("login", ""),
                    "created_at": created,
                    "closed_at": closed,
                    "merged_at": merged,
                    "lead_time_hours": hours_between(created, end),
                    "commits": pr.get("commits", {}).get("totalCount", pr.get("commits", 0)) if isinstance(pr.get("commits"), dict) else pr.get("commits", 0),
                    "additions": pr.get("additions") or 0,
                    "deletions": pr.get("deletions") or 0,
                    "changed_files": pr.get("changedFiles") or 0,
                    "base": pr.get("baseRefName", ""),
                    "head": pr.get("headRefName", ""),
                    "labels": ",".join(label.get("name", "") for label in labels if isinstance(label, dict)),
                    "review_decision": pr.get("reviewDecision") or "",
                    "source": "gh",
                }
            )
    else:
        for row in inferred:
            pr_rows.append(
                {
                    "number": row["number"],
                    "title": row["title"],
                    "state": "merged_inferred",
                    "merged": True,
                    "draft": False,
                    "author": row["author"],
                    "created_at": "",
                    "closed_at": row["merged_at"],
                    "merged_at": row["merged_at"],
                    "lead_time_hours": "",
                    "commits": row["commits_hint"],
                    "additions": row["additions_hint"],
                    "deletions": row["deletions_hint"],
                    "changed_files": row["changed_files_hint"],
                    "base": "",
                    "head": "",
                    "labels": "",
                    "review_decision": "",
                    "source": "git_subject_inferred",
                }
            )
    month_counter = defaultdict(lambda: {"prs": 0, "merged": 0, "closed_unmerged": 0, "additions": 0, "deletions": 0})
    label_counter = Counter()
    author_counter = Counter()
    lead_times = []
    for pr in pr_rows:
        anchor = pr["merged_at"] or pr["closed_at"] or pr["created_at"]
        dt = parse_dt(anchor)
        if dt:
            m = month_counter[dt.strftime("%Y-%m")]
            m["prs"] += 1
            m["merged"] += 1 if pr["merged"] else 0
            m["closed_unmerged"] += 1 if pr["state"].lower() == "closed" and not pr["merged"] else 0
            m["additions"] += int(pr["additions"] or 0)
            m["deletions"] += int(pr["deletions"] or 0)
        if pr.get("lead_time_hours") not in ("", None):
            lead_times.append(float(pr["lead_time_hours"]))
        author_counter[pr["author"]] += 1
        for label in str(pr.get("labels", "")).split(","):
            if label:
                label_counter[label] += 1
    pr_month_rows = [{"month": m, **v} for m, v in sorted(month_counter.items())]
    issue_rows = []
    for issue in gh_issues:
        labels = issue.get("labels") or []
        issue_rows.append(
            {
                "number": issue.get("number"),
                "title": issue.get("title", ""),
                "state": issue.get("state", ""),
                "author": (issue.get("author") or {}).get("login", ""),
                "created_at": issue.get("createdAt", ""),
                "closed_at": issue.get("closedAt", ""),
                "lead_time_hours": hours_between(issue.get("createdAt"), issue.get("closedAt")),
                "labels": ",".join(label.get("name", "") for label in labels if isinstance(label, dict)),
                "assignees": ",".join(a.get("login", "") for a in issue.get("assignees", []) if isinstance(a, dict)),
            }
        )
    run_rows = []
    for run in gh_runs:
        run_rows.append(
            {
                "id": run.get("databaseId"),
                "workflow": run.get("workflowName") or run.get("name"),
                "status": run.get("status"),
                "conclusion": run.get("conclusion"),
                "event": run.get("event"),
                "branch": run.get("headBranch"),
                "created_at": run.get("createdAt"),
                "updated_at": run.get("updatedAt"),
                "duration_minutes": round((hours_between(run.get("createdAt"), run.get("updatedAt")) or 0) * 60, 2),
            }
        )
    conclusion_counter = Counter(r["conclusion"] or r["status"] for r in run_rows)
    issue_leads = [r["lead_time_hours"] for r in issue_rows if r["lead_time_hours"] is not None]
    summary = {
        "repo": repo,
        "source": "gh" if gh_prs else "git_subject_inferred",
        "gh_available": bool(gh_prs),
        "gh_status": "available" if gh_prs else gh_meta.get("error", "not available"),
        "prs": len(pr_rows),
        "merged_prs": sum(1 for r in pr_rows if r["merged"]),
        "closed_unmerged_prs": sum(1 for r in pr_rows if str(r["state"]).lower() == "closed" and not r["merged"]),
        "merge_rate_pct": pct(sum(1 for r in pr_rows if r["merged"]), len(pr_rows)),
        "median_pr_lead_time_hours": median_or_zero(lead_times),
        "p90_pr_lead_time_hours": round(percentile(lead_times, 0.9), 2) if lead_times else 0,
        "issues": len(issue_rows),
        "closed_issues": sum(1 for r in issue_rows if str(r["state"]).lower() == "closed"),
        "median_issue_close_hours": median_or_zero(issue_leads),
        "actions_runs": len(run_rows),
        "actions_success_rate_pct": pct(conclusion_counter["success"], len(run_rows)),
    }
    return {
        "summary": summary,
        "pull_requests": sorted(pr_rows, key=lambda r: r["number"] or 0, reverse=True),
        "prs_by_month": pr_month_rows,
        "pr_labels": [{"label": k, "prs": v} for k, v in label_counter.most_common()],
        "pr_authors": [{"author": k, "prs": v} for k, v in author_counter.most_common()],
        "issues": issue_rows,
        "actions_runs": run_rows,
        "actions_conclusions": [{"conclusion": k, "runs": v} for k, v in conclusion_counter.most_common()],
        "gh_meta": gh_meta,
    }


def analyze_tags_and_branches():
    tag_rows = []
    tags = run_git(["for-each-ref", "--sort=creatordate", "--format=%(refname:short)%09%(creatordate:iso8601)", "refs/tags"]).splitlines()
    for line in tags:
        parts = line.split("\t")
        if len(parts) >= 2:
            tag_rows.append({"tag": parts[0], "date": parts[1]})
    branch_rows = []
    branches = run_git(["for-each-ref", "--sort=-committerdate", "--format=%(refname:short)%09%(committerdate:iso8601)%09%(objectname:short)", "refs/heads", "refs/remotes"]).splitlines()
    for line in branches:
        parts = line.split("\t")
        if len(parts) >= 3:
            dt = parse_dt(parts[1])
            branch_rows.append(
                {
                    "branch": parts[0],
                    "last_commit_date": parts[1],
                    "last_commit": parts[2],
                    "days_since_last_commit": (datetime.now(dt.tzinfo) - dt).days if dt else "",
                }
            )
    return tag_rows, branch_rows


def analyze_event_command_coverage():
    asm_dirs = []
    for asmdef in ROOT.rglob("*.asmdef"):
        rp = Path(rel(asmdef))
        if any(part in SKIP_DIRS for part in rp.parts) or is_vendor(rp):
            continue
        asm_dirs.append(asmdef.parent)
    rows = []
    for directory in asm_dirs:
        rp = Path(rel(directory))
        cs_files = [p for p in directory.rglob("*.cs") if "Events" not in p.parts and "Commands" not in p.parts]
        events_dir = directory / "Events"
        commands_dir = directory / "Commands"
        event_files = list(events_dir.rglob("*.cs")) if events_dir.exists() else []
        command_files = list(commands_dir.rglob("*.cs")) if commands_dir.exists() else []
        rows.append(
            {
                "assembly_dir": rp.as_posix(),
                "code_files": len(cs_files),
                "has_events_dir": events_dir.exists(),
                "event_files": len(event_files),
                "has_commands_dir": commands_dir.exists(),
                "command_files": len(command_files),
                "events_per_code_file": round(len(event_files) / max(len(cs_files), 1), 3),
                "commands_per_code_file": round(len(command_files) / max(len(cs_files), 1), 3),
            }
        )
    return rows


def canonical_author(author, email=""):
    low = (author or "").lower()
    email_low = (email or "").lower()
    if "vanish" in low or "消失" in author or "vanish" in email_low or "2013107081" in email_low or "78757142" in email_low or "x.krluf" in email_low:
        return "Vanish"
    if "oris" in low or "101974868" in email_low:
        return "OriSYX"
    if "likoko" in low or "154248572" in email_low:
        return "likoko"
    return author or email or "(unknown)"


def build_alias_contributors(commits):
    rows = defaultdict(lambda: {"commits": 0, "insertions": 0, "deletions": 0, "files": 0, "aliases": set(), "emails": set()})
    for c in commits:
        key = canonical_author(c.get("author", ""), c.get("email", ""))
        rows[key]["commits"] += 1
        rows[key]["insertions"] += c.get("insertions", 0)
        rows[key]["deletions"] += c.get("deletions", 0)
        rows[key]["files"] += c.get("files", 0)
        rows[key]["aliases"].add(c.get("author", ""))
        rows[key]["emails"].add(c.get("email", ""))
    result = []
    for author, values in rows.items():
        result.append(
            {
                "canonical_author": author,
                "commits": values["commits"],
                "insertions": values["insertions"],
                "deletions": values["deletions"],
                "files": values["files"],
                "aliases": ", ".join(sorted(a for a in values["aliases"] if a)),
                "emails": ", ".join(sorted(e for e in values["emails"] if e)),
            }
        )
    return sorted(result, key=lambda r: r["commits"], reverse=True)


def analyze_dependency_sources(unity):
    rows = []
    for pkg in unity.get("packages", []):
        version = str(pkg.get("version", ""))
        if version.startswith("http") or version.startswith("git"):
            source = "Git URL"
        elif version.startswith("file:"):
            source = "Local file"
        elif re.search(r"^\d+\.\d+", version):
            source = "Unity Registry"
        else:
            source = "Built-in or custom"
        rows.append({"package": pkg.get("name", ""), "version": version, "source": source})
    return rows


def summarize_rows(rows, key_name, value_name="count"):
    counter = Counter(r[key_name] for r in rows)
    return [{key_name: k, value_name: v} for k, v in counter.most_common()]


def build_metric_catalog(github_data):
    items = [
        ("P0", "低", "作者别名合并贡献", "implemented", "Git author/email heuristic"),
        ("P0", "低", "开发连续天数 Streak", "implemented", "git log dates"),
        ("P0", "低", "夜间开发指数", "implemented", "git commit hour"),
        ("P0", "低", "周末开发占比", "implemented", "git weekday"),
        ("P0", "低", "爆发期识别", "implemented", "commit day + churn"),
        ("P0", "低", "文件变更热点", "implemented", "git numstat"),
        ("P0", "中", "贡献领域雷达", "implemented", "git numstat + path domain"),
        ("P0", "低", "第一方 vs 第三方占比", "implemented", "path rules"),
        ("P0", "中", "Unity 场景复杂度", "implemented", "Unity YAML scan"),
        ("P0", "中", "Prefab 复杂度", "implemented", "Unity YAML scan"),
        ("P0", "中", "Addressables 覆盖率", "implemented", "Addressable group assets"),
        ("P0", "低", "代码结构地图", "implemented", "path/domain grouping"),
        ("P0", "低", "C# 类型组成", "implemented", "C# regex scan"),
        ("P0", "低", "MonoBehaviour 使用量", "implemented", "C# regex scan"),
        ("P0", "低", "ScriptableObject 使用量", "implemented", "C# regex scan"),
        ("P0", "低", "SerializeField 热点", "implemented", "C# regex scan"),
        ("P0", "低", "Update/FixedUpdate 热点", "implemented", "C# regex scan"),
        ("P0", "低", "Awake/Start 生命周期分布", "implemented", "C# regex scan"),
        ("P0", "低", "文件代码行数 Top", "implemented", "C# file scan"),
        ("P0", "低", "方法数量 Top", "implemented", "C# regex scan"),
        ("P0", "低/中", "PR 生命周期", "implemented" if github_data["summary"]["gh_available"] else "local-inferred", "gh PR API when available; otherwise commit subject #"),
        ("P0", "低/中", "PR 吞吐量/合并率/体积画像", "implemented" if github_data["summary"]["gh_available"] else "local-inferred", "gh PR API when available; otherwise commit subject #"),
        ("P1", "中", "近似圈复杂度", "implemented", "branch keyword count"),
        ("P1", "中", "最大方法/类长度近似", "partial", "file-level size and method count"),
        ("P1", "低", "Namespace 分布", "implemented", "C# regex scan"),
        ("P1", "中", "asmdef 依赖图", "implemented", "asmdef JSON"),
        ("P1", "低", "Runtime/Editor/Test 比例", "implemented", "path/domain grouping"),
        ("P1", "低", "测试代码比例", "implemented", "path/domain grouping"),
        ("P1", "低", "日志调用分布", "implemented", "CLogger/Debug.Log scan"),
        ("P1", "低", "R3 使用情况", "implemented", "Observable/Subject scan"),
        ("P1", "中", "事件系统覆盖", "implemented", "Events path checks"),
        ("P1", "中", "Commands 覆盖", "implemented", "Commands path checks"),
        ("P1", "中", "m_ 字段规范率", "implemented", "C# field scan"),
        ("P1", "低", "注释密度", "implemented", "C# comment-like lines"),
        ("P1", "低", "TODO/FIXME/HACK", "implemented", "text scan"),
        ("P1", "中", "CI 工作流复杂度", "implemented", "GitHub workflow YAML scan"),
        ("P1", "低/高", "CI 成功率/耗时", "implemented" if github_data["summary"]["actions_runs"] else "blocked-gh-auth", "gh run list requires valid gh auth"),
        ("P1", "中", "美术帧序列完整度", "implemented", "numbered image sequence scan"),
        ("P1", "中", "关卡数据复杂度", "implemented", "LDtk JSON parse"),
        ("P1", "低/中", "Issue 关闭速度/积压量", "implemented" if github_data["summary"]["issues"] else "blocked-gh-auth", "gh issue list requires valid gh auth"),
        ("P2", "中", "依赖来源画像", "implemented", "Packages manifest version source"),
        ("P2", "中", "代码主题地图", "implemented", "path/domain/tokens"),
        ("P2", "中", "文件年龄分布", "implemented", "git log per file"),
        ("P2", "高", "GUID 引用中心", "implemented", "meta GUID + YAML refs"),
        ("P2", "高", "疑似孤儿资源", "implemented", "GUID reverse lookup heuristic"),
        ("P2", "中", "提交主题词云", "implemented", "commit subject keywords"),
        ("P2", "中", "个人开发节奏画像", "implemented", "author x time/domain"),
        ("P2", "中", "高风险热点", "implemented", "complexity + churn + size + tests heuristic"),
        ("P2", "中", "协程/async/LINQ/反射/资源路径/异常处理", "implemented", "C# token scan"),
        ("P2", "高", "Review 响应时间/轮次/矩阵", "blocked-gh-auth", "needs GitHub reviews through gh/API"),
        ("P2", "高", "评论情绪温度/争议 PR", "blocked-gh-auth", "needs GitHub comments through gh/API"),
    ]
    return [{"priority": p, "difficulty": d, "metric": m, "status": s, "source": src} for p, d, m, s, src in items]


def build_risk_hotspots(code_data, file_churn):
    rows = []
    churn_lookup = {path: v["insertions"] + v["deletions"] for path, v in file_churn.items()}
    touches_lookup = {path: v["touches"] for path, v in file_churn.items()}
    for r in code_data["code_file_metrics"]:
        churn = churn_lookup.get(r["path"], 0)
        touches = touches_lookup.get(r["path"], 0)
        score = (
            r["approx_complexity"] * 1.8
            + r["lines"] / 20
            + churn / 120
            + touches * 3
            + r["public_fields"] * 4
            + r["bad_private_fields"] * 3
            + r["reflection_hits"] * 8
            + r.get("resources_load_hits", 0) * 8
        )
        score += r["magic_numbers"] * 0.6 + r["update_methods"] * 8
        if "Tests/" in r["path"]:
            score *= 0.35
        rows.append(
            {
                "path": r["path"],
                "risk_score": round(score, 2),
                "lines": r["lines"],
                "complexity": r["approx_complexity"],
                "churn": churn,
                "touches": touches,
                "update_methods": r["update_methods"],
                "public_fields": r["public_fields"],
                "reflection_hits": r["reflection_hits"],
                "magic_numbers": r["magic_numbers"],
            }
        )
    return sorted(rows, key=lambda r: r["risk_score"], reverse=True)


def write_csv(path, rows):
    rows = list(rows)
    with path.open("w", newline="", encoding="utf-8") as f:
        if not rows:
            f.write("")
            return
        fieldnames = list(rows[0].keys())
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()
        for row in rows:
            writer.writerow(row)


def fmt_int(n):
    return f"{int(n):,}"


def fmt_bytes(n):
    units = ["B", "KB", "MB", "GB"]
    value = float(n)
    unit = units[0]
    for unit in units:
        if value < 1024 or unit == units[-1]:
            break
        value /= 1024
    return f"{value:.1f} {unit}" if unit != "B" else f"{int(value)} B"


def make_summary_md(data, unity):
    s = data["summary"]
    top_contrib = data["alias_contributors"][:5]
    top_cats = data["first_party_category_summary"][:8]
    code = data["code"]["summary"]
    gh = data["github"]["summary"]
    metric_status = Counter(row["status"] for row in data["metric_catalog"])
    lines = [
        "# RainRust Project Stats",
        "",
        f"- Generated at: {s['generated_at']}",
        f"- Unity version: {unity.get('unity_version') or 'unknown'}",
        f"- Files scanned: {fmt_int(s['tracked_files_seen'])} ({fmt_int(s['first_party_files'])} first-party, {fmt_int(s['vendor_files'])} vendor)",
        f"- First-party lines: {fmt_int(s['first_party_lines'])}",
        f"- Git commits: {fmt_int(s['commit_count'])} across {fmt_int(s['contributor_count'])} author names / {fmt_int(s['canonical_contributor_count'])} canonical contributors",
        f"- History range: {s['first_commit']} -> {s['latest_commit']}",
        f"- GitHub data source: {gh['source']} ({gh['gh_status']})",
        f"- Metric implementation status: " + ", ".join(f"{k}: {v}" for k, v in sorted(metric_status.items())),
        "",
        "## Top Contributors (Alias-Merged)",
        "",
    ]
    for row in top_contrib:
        lines.append(f"- {row['canonical_author']}: {fmt_int(row['commits'])} commits, +{fmt_int(row['insertions'])}/-{fmt_int(row['deletions'])} ({row['aliases']})")
    lines.extend(
        [
            "",
            "## Development Rhythm",
            "",
            f"- Active days: {fmt_int(s['active_days'])}",
            f"- Longest commit streak: {fmt_int(s['longest_commit_streak_days'])} days",
            f"- Night commits: {fmt_int(s['night_commits'])} ({s['night_commit_pct']}%)",
            f"- Weekend commits: {fmt_int(s['weekend_commits'])} ({s['weekend_commit_pct']}%)",
            "",
            "## Code Content",
            "",
            f"- C# files: {fmt_int(code['cs_files'])}; lines: {fmt_int(code['total_cs_lines'])}",
            f"- MonoBehaviour files: {fmt_int(code['mono_behaviour_files'])}; ScriptableObject files: {fmt_int(code['scriptable_object_files'])}",
            f"- SerializeField count: {fmt_int(code['serialize_fields'])}",
            f"- Private field `m_` compliance: {code['private_field_m_prefix_compliance_pct']}%",
            f"- Comment-like density: {code['comment_density_pct']}%",
            "",
            "## Unity Content",
            "",
            f"- Scenes analyzed: {fmt_int(data['unity_assets']['summary']['scenes_analyzed'])}",
            f"- Prefabs analyzed: {fmt_int(data['unity_assets']['summary']['prefabs_analyzed'])}",
            f"- LDtk files analyzed: {fmt_int(len(data['ldtk']))}",
            f"- Potential orphan assets by GUID heuristic: {fmt_int(data['unity_assets']['summary']['potential_orphan_assets'])}",
        ]
    )
    lines.extend(["", "## First-Party Category Mix", ""])
    for row in top_cats:
        lines.append(f"- {row['name']}: {fmt_int(row['files'])} files, {fmt_int(row['lines'])} lines, {fmt_bytes(row['bytes'])}")
    lines.extend(
        [
            "",
            "## Raw Data Files",
            "",
            "- `raw_stats.json`: complete nested stats used by the dashboard",
            "- `file_inventory.csv`: file-level inventory",
            "- `commit_history.csv`: commit-level history and churn",
            "- `contributor_summary.csv`: author-level contribution summary",
            "- `contributor_alias_summary.csv`: alias-merged contributor summary",
            "- `metric_catalog.csv`: implemented / partial / blocked metric catalog",
            "- `code_file_metrics.csv`: file-level C# content metrics",
            "- `github_pull_requests.csv`: PR metrics from `gh` when available, otherwise local inferred PR rows",
            "- `workflow_complexity.csv`: GitHub Actions workflow complexity",
            "- `unity_scene_complexity.csv`, `unity_prefab_complexity.csv`: Unity YAML complexity metrics",
            "- `risk_hotspots.csv`: combined code risk heuristic",
            "- `extension_summary.csv`, `category_summary.csv`, `directory_summary.csv`: project composition summaries",
            "",
            f"Note: {s['git_status_note']}",
        ]
    )
    return "\n".join(lines) + "\n"


def make_dashboard_html(data, unity, symbols):
    payload = {
        "summary": data["summary"],
        "unity": unity,
        "symbols": symbols,
        "extension_summary": data["extension_summary"][:18],
        "category_summary": data["category_summary"][:18],
        "first_party_category_summary": data["first_party_category_summary"][:18],
        "directory_summary": data["directory_summary"][:16],
        "scope_summary": data["scope_summary"],
        "contributors": data["contributors"][:12],
        "alias_contributors": data["alias_contributors"][:12],
        "author_domain_churn": data["author_domain_churn"][:80],
        "commits_by_month": data["commits_by_month"],
        "commits_by_day": data["commits_by_day"],
        "burst_days": data["burst_days"][:12],
        "commits_by_hour": data["commits_by_hour"],
        "commits_by_weekday": data["commits_by_weekday"],
        "domain_summary": data["domain_summary"][:16],
        "first_party_domain_summary": data["first_party_domain_summary"][:16],
        "top_churn_files": data["top_churn_files"][:30],
        "largest_files": data["largest_files"][:30],
        "commit_keywords": data["commit_keywords"],
        "latest_commits": data["latest_commits"][:12],
        "code_summary": data["code"]["summary"],
        "top_complex_files": data["code"]["top_complex_files"][:20],
        "top_long_files": data["code"]["top_long_files"][:20],
        "namespace_summary": data["code"]["namespace_summary"][:16],
        "code_keywords": data["code"]["keyword_summary"][:20],
        "api_usage_summary": data["code"]["api_usage_summary"][:16],
        "risk_hotspots": data["risk_hotspots"][:20],
        "github_summary": data["github"]["summary"],
        "github_prs": data["github"]["pull_requests"][:20],
        "github_prs_by_month": data["github"]["prs_by_month"],
        "github_actions_conclusions": data["github"]["actions_conclusions"],
        "workflow_complexity": data["workflows"][:20],
        "scene_complexity": data["unity_assets"]["scene_complexity"][:20],
        "prefab_complexity": data["unity_assets"]["prefab_complexity"][:20],
        "addressables": data["addressables"],
        "ldtk": data["ldtk"],
        "asmdefs": data["asmdefs"],
        "metric_catalog": data["metric_catalog"],
        "naming_summary": data["naming"]["summary"],
        "animation_sequences": data["animation_sequences"][:20],
        "dependency_source_summary": data["dependency_source_summary"],
    }
    payload_json = json.dumps(payload, ensure_ascii=False)
    return f"""<!doctype html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>RainRust Stats Dashboard</title>
<style>
:root {{
  color-scheme: dark;
  --bg: #111315;
  --panel: #191d20;
  --panel2: #20262a;
  --line: #334047;
  --text: #edf1ec;
  --muted: #99a79f;
  --green: #8fd17f;
  --cyan: #70c5d6;
  --gold: #e3b85c;
  --red: #e17466;
  --blue: #7ea4e8;
  --shadow: rgba(0,0,0,.25);
}}
* {{ box-sizing: border-box; }}
body {{
  margin: 0;
  font: 14px/1.5 ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
  background:
    linear-gradient(180deg, rgba(143,209,127,.10), transparent 320px),
    radial-gradient(circle at 80% 10%, rgba(112,197,214,.13), transparent 280px),
    var(--bg);
  color: var(--text);
}}
header {{
  padding: 28px clamp(18px, 4vw, 56px) 18px;
  border-bottom: 1px solid rgba(255,255,255,.07);
}}
h1 {{ margin: 0; font-size: clamp(30px, 5vw, 58px); letter-spacing: 0; line-height: 1; }}
h2 {{ margin: 0 0 14px; font-size: 18px; }}
h3 {{ margin: 0 0 8px; font-size: 14px; color: var(--muted); font-weight: 600; }}
.sub {{ color: var(--muted); margin-top: 10px; max-width: 900px; }}
.toolbar {{
  display: flex; flex-wrap: wrap; gap: 10px; align-items: center; margin-top: 18px;
}}
button, select {{
  appearance: none;
  border: 1px solid var(--line);
  background: var(--panel);
  color: var(--text);
  border-radius: 8px;
  padding: 9px 12px;
  cursor: pointer;
}}
button.active {{ background: var(--green); border-color: var(--green); color: #102012; font-weight: 700; }}
main {{ padding: 24px clamp(18px, 4vw, 56px) 44px; }}
.grid {{ display: grid; gap: 14px; }}
.cards {{ grid-template-columns: repeat(6, minmax(120px, 1fr)); }}
.two {{ grid-template-columns: repeat(2, minmax(0, 1fr)); margin-top: 14px; }}
.three {{ grid-template-columns: repeat(3, minmax(0, 1fr)); margin-top: 14px; }}
.card, .panel {{
  background: linear-gradient(180deg, rgba(255,255,255,.035), rgba(255,255,255,.015)), var(--panel);
  border: 1px solid rgba(255,255,255,.08);
  border-radius: 8px;
  box-shadow: 0 16px 38px var(--shadow);
}}
.card {{ padding: 14px; min-height: 104px; }}
.metric {{ font-size: clamp(24px, 3vw, 36px); font-weight: 800; letter-spacing: 0; }}
.label {{ color: var(--muted); margin-top: 5px; }}
.panel {{ padding: 16px; min-height: 260px; overflow: hidden; }}
.bars {{ display: grid; gap: 9px; }}
.bar-row {{ display: grid; grid-template-columns: minmax(90px, 190px) 1fr auto; gap: 10px; align-items: center; }}
.bar-label {{ overflow: hidden; text-overflow: ellipsis; white-space: nowrap; color: #dfe8df; }}
.track {{ height: 12px; background: #101315; border-radius: 999px; overflow: hidden; border: 1px solid rgba(255,255,255,.05); }}
.fill {{ height: 100%; background: linear-gradient(90deg, var(--green), var(--cyan)); border-radius: inherit; }}
.value {{ color: var(--muted); font-variant-numeric: tabular-nums; }}
canvas {{ width: 100%; height: 230px; display: block; }}
table {{ width: 100%; border-collapse: collapse; font-size: 13px; }}
th, td {{ border-bottom: 1px solid rgba(255,255,255,.07); padding: 8px 6px; text-align: left; vertical-align: top; }}
th {{ color: var(--muted); font-weight: 700; }}
td.num {{ text-align: right; font-variant-numeric: tabular-nums; }}
.heatmap {{ display: grid; grid-template-columns: repeat(auto-fill, minmax(12px, 1fr)); gap: 3px; align-items: end; min-height: 140px; }}
.day {{ height: 12px; border-radius: 2px; background: #23282b; position: relative; }}
.day:hover::after {{
  content: attr(data-tip); position: absolute; left: 0; bottom: 18px; white-space: nowrap;
  background: #050607; color: var(--text); padding: 5px 7px; border-radius: 6px; z-index: 3;
}}
.pill {{ display: inline-flex; align-items: center; gap: 6px; color: #152017; background: var(--green); padding: 3px 8px; border-radius: 999px; font-size: 12px; font-weight: 700; }}
.muted {{ color: var(--muted); }}
.commit {{ display: grid; gap: 4px; padding: 8px 0; border-bottom: 1px solid rgba(255,255,255,.07); }}
.commit strong {{ font-size: 13px; }}
.footer {{ margin-top: 18px; color: var(--muted); font-size: 12px; }}
@media (max-width: 1100px) {{ .cards {{ grid-template-columns: repeat(3, 1fr); }} .three {{ grid-template-columns: 1fr; }} }}
@media (max-width: 760px) {{ .cards, .two {{ grid-template-columns: 1fr; }} .bar-row {{ grid-template-columns: 1fr; }} canvas {{ height: 190px; }} }}
</style>
</head>
<body>
<header>
  <div class="pill">RainRust / Unity project telemetry</div>
  <h1>项目统计看板</h1>
  <div class="sub">本看板由本地仓库、Unity 配置、文件系统和 Git 历史生成。原始数据保存在同目录 JSON/CSV，页面内的筛选与图表无需联网。</div>
  <div class="toolbar">
    <button id="scope-first" class="active">第一方视角</button>
    <button id="scope-all">全仓库视角</button>
    <select id="metric-select">
      <option value="files">按文件数</option>
      <option value="bytes">按体积</option>
      <option value="lines">按行数</option>
    </select>
  </div>
</header>
<main>
  <section class="grid cards" id="cards"></section>
  <section class="grid two">
    <div class="panel"><h2>项目组成</h2><div id="category-bars" class="bars"></div></div>
    <div class="panel"><h2>月度提交与代码流量</h2><canvas id="month-chart" width="900" height="300"></canvas></div>
  </section>
  <section class="grid three">
    <div class="panel"><h2>贡献者</h2><div id="contrib-bars" class="bars"></div></div>
    <div class="panel"><h2>一周节奏</h2><canvas id="weekday-chart" width="600" height="300"></canvas></div>
    <div class="panel"><h2>一天内的提交峰值</h2><canvas id="hour-chart" width="600" height="300"></canvas></div>
  </section>
  <section class="grid two">
    <div class="panel"><h2>提交热力</h2><div id="heatmap" class="heatmap"></div><div class="footer">颜色越亮代表当天提交越多。</div></div>
    <div class="panel"><h2>提交主题关键词</h2><div id="keyword-bars" class="bars"></div></div>
  </section>
  <section class="grid two">
    <div class="panel"><h2>最高变更文件</h2><table id="churn-table"></table></div>
    <div class="panel"><h2>最大文件</h2><table id="large-table"></table></div>
  </section>
  <section class="grid two">
    <div class="panel"><h2>Unity 项目线索</h2><div id="unity"></div></div>
    <div class="panel"><h2>最近提交</h2><div id="latest"></div></div>
  </section>
  <section class="grid three">
    <div class="panel"><h2>GitHub / PR 画像</h2><div id="github"></div></div>
    <div class="panel"><h2>代码内容雷达</h2><div id="code-summary"></div></div>
    <div class="panel"><h2>规范健康</h2><div id="quality-summary"></div></div>
  </section>
  <section class="grid two">
    <div class="panel"><h2>代码风险热点</h2><table id="risk-table"></table></div>
    <div class="panel"><h2>C# 复杂文件</h2><table id="complex-table"></table></div>
  </section>
  <section class="grid two">
    <div class="panel"><h2>Unity 场景复杂度</h2><table id="scene-table"></table></div>
    <div class="panel"><h2>Prefab 复杂度</h2><table id="prefab-table"></table></div>
  </section>
  <section class="grid two">
    <div class="panel"><h2>GitHub Actions 工作流复杂度</h2><table id="workflow-table"></table></div>
    <div class="panel"><h2>实现覆盖目录</h2><table id="catalog-table"></table></div>
  </section>
  <section class="grid two">
    <div class="panel"><h2>贡献领域分布</h2><div id="domain-bars" class="bars"></div></div>
    <div class="panel"><h2>代码 API 热点</h2><div id="api-bars" class="bars"></div></div>
  </section>
  <div class="footer">Raw data: raw_stats.json, file_inventory.csv, commit_history.csv, contributor_summary.csv. Git status note: <span id="status-note"></span></div>
</main>
<script id="stats-data" type="application/json">{payload_json}</script>
<script>
const data = JSON.parse(document.getElementById('stats-data').textContent);
let scope = 'first';
let metric = 'files';
const fmt = new Intl.NumberFormat('en-US');
function bytes(n) {{
  const u = ['B','KB','MB','GB']; let v = Number(n), i = 0;
  while (v >= 1024 && i < u.length - 1) {{ v /= 1024; i++; }}
  return i === 0 ? `${{Math.round(v)}} B` : `${{v.toFixed(1)}} ${{u[i]}}`;
}}
function numberFor(row) {{ return metric === 'bytes' ? bytes(row[metric]) : fmt.format(row[metric] || 0); }}
function labelForMetric() {{ return metric === 'bytes' ? '体积' : metric === 'lines' ? '行数' : '文件数'; }}
function card(label, value, note) {{ return `<div class="card"><div class="metric">${{value}}</div><div class="label">${{label}}</div><div class="muted">${{note || ''}}</div></div>`; }}
function renderCards() {{
  const s = data.summary;
  document.getElementById('cards').innerHTML = [
    card('文件', fmt.format(scope === 'first' ? s.first_party_files : s.tracked_files_seen), scope === 'first' ? '第一方，不含 Stats' : '包含第三方与 Packages'),
    card('第一方行数', fmt.format(s.first_party_lines), `${{fmt.format(s.first_party_code_lines)}} code/config lines`),
    card('Git 提交', fmt.format(s.commit_count), `${{fmt.format(s.active_days)}} active days`),
    card('贡献者', fmt.format(s.canonical_contributor_count), `${{fmt.format(s.contributor_count)}} raw author names`),
    card('GitHub PR', fmt.format(s.github_prs), data.github_summary.source),
    card('代码风险点', fmt.format(data.risk_hotspots.length), 'Top items shown below')
  ].join('');
}}
function bars(id, rows, key, maxRows = 10) {{
  const max = Math.max(...rows.map(r => r[key] || 0), 1);
  document.getElementById(id).innerHTML = rows.slice(0, maxRows).map(r => {{
    const w = Math.max(2, ((r[key] || 0) / max) * 100);
    return `<div class="bar-row"><div class="bar-label" title="${{r.name || r.author || r.keyword}}">${{r.name || r.author || r.keyword}}</div><div class="track"><div class="fill" style="width:${{w}}%"></div></div><div class="value">${{key === 'bytes' ? bytes(r[key]) : fmt.format(r[key] || 0)}}</div></div>`;
  }}).join('');
}}
function renderBars() {{
  const cats = scope === 'first' ? data.first_party_category_summary : data.category_summary;
  const domains = scope === 'first' ? data.first_party_domain_summary : data.domain_summary;
  bars('category-bars', cats, metric, 12);
  bars('contrib-bars', data.alias_contributors.map(r => ({{name: r.canonical_author, commits: r.commits}})), 'commits', 10);
  bars('keyword-bars', data.commit_keywords.map(r => ({{name: r.keyword, count: r.count}})), 'count', 12);
  bars('domain-bars', domains, metric, 12);
  bars('api-bars', data.api_usage_summary.map(r => ({{name: r.api, hits: r.hits}})), 'hits', 12);
}}
function lineChart(canvasId, rows) {{
  const c = document.getElementById(canvasId), ctx = c.getContext('2d'), w = c.width, h = c.height;
  ctx.clearRect(0,0,w,h);
  const pad = 34, max = Math.max(...rows.map(r => Math.max(r.commits || 0, (r.insertions || 0) / 1000, (r.deletions || 0) / 1000)), 1);
  ctx.strokeStyle = 'rgba(255,255,255,.12)'; ctx.lineWidth = 1;
  for (let i=0;i<4;i++) {{ const y = pad + (h-pad*2)*i/3; ctx.beginPath(); ctx.moveTo(pad,y); ctx.lineTo(w-pad,y); ctx.stroke(); }}
  function draw(key, color, scale=1) {{
    ctx.strokeStyle = color; ctx.lineWidth = 3; ctx.beginPath();
    rows.forEach((r,i) => {{
      const x = pad + (w-pad*2) * (rows.length === 1 ? 0 : i/(rows.length-1));
      const y = h-pad - ((r[key] || 0)/scale)/max * (h-pad*2);
      if (i===0) ctx.moveTo(x,y); else ctx.lineTo(x,y);
    }});
    ctx.stroke();
  }}
  draw('commits', '#8fd17f', 1); draw('insertions', '#70c5d6', 1000); draw('deletions', '#e17466', 1000);
  ctx.fillStyle = '#99a79f'; ctx.font = '12px system-ui';
  ctx.fillText('commits / +lines(k) / -lines(k)', pad, 18);
}}
function barCanvas(canvasId, rows, labelKey, valueKey, color) {{
  const c = document.getElementById(canvasId), ctx = c.getContext('2d'), w = c.width, h = c.height;
  ctx.clearRect(0,0,w,h);
  const pad = 30, max = Math.max(...rows.map(r => r[valueKey] || 0), 1), bw = (w-pad*2)/rows.length;
  rows.forEach((r,i) => {{
    const x = pad + i*bw + 4, bh = ((r[valueKey] || 0)/max)*(h-pad*2), y = h-pad-bh;
    ctx.fillStyle = color; ctx.fillRect(x,y,Math.max(3,bw-8),bh);
    ctx.fillStyle = '#99a79f'; ctx.font = '11px system-ui'; ctx.save(); ctx.translate(x+4,h-9); ctx.rotate(-Math.PI/5); ctx.fillText(String(r[labelKey]).slice(0,8),0,0); ctx.restore();
  }});
}}
function heatmap() {{
  const max = Math.max(...data.commits_by_day.map(r => r.commits), 1);
  document.getElementById('heatmap').innerHTML = data.commits_by_day.map(r => {{
    const a = .12 + .88 * (r.commits / max);
    return `<div class="day" data-tip="${{r.date}}: ${{r.commits}} commits" style="background: rgba(143,209,127,${{a}})"></div>`;
  }}).join('');
}}
function table(id, rows, cols) {{
  document.getElementById(id).innerHTML = `<thead><tr>${{cols.map(c => `<th>${{c[0]}}</th>`).join('')}}</tr></thead><tbody>` +
    rows.map(r => `<tr>${{cols.map(c => `<td class="${{typeof r[c[1]] === 'number' ? 'num' : ''}}">${{c[2] ? c[2](r[c[1]], r) : r[c[1]]}}</td>`).join('')}}</tr>`).join('') + '</tbody>';
}}
function renderTables() {{
  table('churn-table', data.top_churn_files.slice(0,12), [['文件','path', v => `<span title="${{v}}">${{String(v).slice(0,64)}}</span>`], ['Churn','churn', v => fmt.format(v)], ['Touches','touches', v => fmt.format(v)]]);
  table('large-table', data.largest_files.slice(0,12), [['文件','path', v => `<span title="${{v}}">${{String(v).slice(0,64)}}</span>`], ['体积','bytes', v => bytes(v)], ['类型','category']]);
  table('risk-table', data.risk_hotspots.slice(0,12), [['文件','path', v => `<span title="${{v}}">${{String(v).slice(0,58)}}</span>`], ['风险分','risk_score', v => fmt.format(Math.round(v))], ['复杂度','complexity', v => fmt.format(v)], ['Churn','churn', v => fmt.format(v)]]);
  table('complex-table', data.top_complex_files.slice(0,12), [['文件','path', v => `<span title="${{v}}">${{String(v).slice(0,58)}}</span>`], ['行','lines', v => fmt.format(v)], ['复杂度','approx_complexity', v => fmt.format(v)], ['Update','update_methods', v => fmt.format(v)]]);
  table('scene-table', data.scene_complexity.slice(0,12), [['Scene','path', v => `<span title="${{v}}">${{String(v).slice(0,58)}}</span>`], ['对象','game_objects', v => fmt.format(v)], ['组件','components', v => fmt.format(v)], ['GUID','guid_refs', v => fmt.format(v)]]);
  table('prefab-table', data.prefab_complexity.slice(0,12), [['Prefab','path', v => `<span title="${{v}}">${{String(v).slice(0,58)}}</span>`], ['组件','components', v => fmt.format(v)], ['Mono','mono_behaviours', v => fmt.format(v)], ['GUID','guid_refs', v => fmt.format(v)]]);
  table('workflow-table', data.workflow_complexity.slice(0,12), [['Workflow','workflow'], ['Jobs','jobs', v => fmt.format(v)], ['Steps','steps', v => fmt.format(v)], ['Score','complexity_score', v => fmt.format(v)]]);
  table('catalog-table', data.metric_catalog.slice(0,18), [['优先级','priority'], ['指标','metric'], ['状态','status'], ['难度','difficulty']]);
}}
function renderUnity() {{
  const u = data.unity, sym = data.symbols;
  document.getElementById('unity').innerHTML = `
    <table><tbody>
      <tr><th>Unity</th><td>${{u.unity_version || 'unknown'}}</td></tr>
      <tr><th>Packages</th><td>${{fmt.format(u.packages.length)}}</td></tr>
      <tr><th>Addressable Groups</th><td>${{u.addressable_groups.join(', ') || 'none detected'}}</td></tr>
      <tr><th>Build Scenes</th><td>${{u.build_scenes.join('<br>') || 'none detected'}}</td></tr>
      <tr><th>C# Symbols</th><td>${{fmt.format(sym.classes || 0)}} classes, ${{fmt.format(sym.structs || 0)}} structs, ${{fmt.format(sym.interfaces || 0)}} interfaces, ${{fmt.format(sym.enums || 0)}} enums</td></tr>
      <tr><th>SerializeField</th><td>${{fmt.format(sym.serialized_fields || 0)}}</td></tr>
      <tr><th>Addressables</th><td>${{data.addressables.map(g => `${{g.group}} (${{g.entries_hint}})`).join('<br>') || 'none detected'}}</td></tr>
      <tr><th>LDtk</th><td>${{data.ldtk.map(x => `${{x.levels}} levels, ${{x.placed_entities}} entities, ${{x.placed_tiles}} tiles`).join('<br>') || 'none detected'}}</td></tr>
    </tbody></table>`;
}}
function renderGithub() {{
  const g = data.github_summary;
  document.getElementById('github').innerHTML = `
    <table><tbody>
      <tr><th>Repo</th><td>${{g.repo || 'unknown'}}</td></tr>
      <tr><th>Source</th><td>${{g.source}}<br><span class="muted">${{g.gh_status}}</span></td></tr>
      <tr><th>PRs</th><td>${{fmt.format(g.prs)}} total, ${{fmt.format(g.merged_prs)}} merged, ${{g.merge_rate_pct}}% merge rate</td></tr>
      <tr><th>PR Lead Time</th><td>median ${{fmt.format(g.median_pr_lead_time_hours)}}h, p90 ${{fmt.format(g.p90_pr_lead_time_hours)}}h</td></tr>
      <tr><th>Issues</th><td>${{fmt.format(g.issues)}} total, ${{fmt.format(g.closed_issues)}} closed</td></tr>
      <tr><th>Actions</th><td>${{fmt.format(g.actions_runs)}} runs, ${{g.actions_success_rate_pct}}% success</td></tr>
    </tbody></table>`;
}}
function renderCodeSummary() {{
  const c = data.code_summary;
  document.getElementById('code-summary').innerHTML = `
    <table><tbody>
      <tr><th>C# files</th><td>${{fmt.format(c.cs_files)}} files, ${{fmt.format(c.total_cs_lines)}} lines</td></tr>
      <tr><th>Unity Types</th><td>${{fmt.format(c.mono_behaviour_files)}} MonoBehaviour files, ${{fmt.format(c.scriptable_object_files)}} ScriptableObject files</td></tr>
      <tr><th>SerializeField</th><td>${{fmt.format(c.serialize_fields)}}</td></tr>
      <tr><th>Update hot files</th><td>${{fmt.format(c.update_method_files)}}</td></tr>
      <tr><th>Median complexity</th><td>${{fmt.format(c.median_complexity_per_file)}}</td></tr>
      <tr><th>Comment density</th><td>${{c.comment_density_pct}}%</td></tr>
    </tbody></table>`;
}}
function renderQualitySummary() {{
  const s = data.summary, n = data.naming_summary;
  const implemented = data.metric_catalog.filter(x => x.status === 'implemented').length;
  const inferred = data.metric_catalog.filter(x => x.status === 'local-inferred').length;
  const blocked = data.metric_catalog.filter(x => String(x.status).startsWith('blocked')).length;
  document.getElementById('quality-summary').innerHTML = `
    <table><tbody>
      <tr><th>Metric coverage</th><td>${{implemented}} implemented, ${{inferred}} inferred, ${{blocked}} blocked</td></tr>
      <tr><th>Asset naming</th><td>${{n.compliance_pct}}% compliant (${{fmt.format(n.violations)}} violations)</td></tr>
      <tr><th>m_ fields</th><td>${{data.code_summary.private_field_m_prefix_compliance_pct}}% compliant</td></tr>
      <tr><th>Streak</th><td>${{fmt.format(s.longest_commit_streak_days)}} day longest, ${{fmt.format(s.recent_commit_streak_days)}} day recent</td></tr>
      <tr><th>Night / Weekend</th><td>${{s.night_commit_pct}}% night, ${{s.weekend_commit_pct}}% weekend</td></tr>
      <tr><th>Animation gaps</th><td>${{fmt.format(data.animation_sequences.filter(x => x.missing_count > 0).length)}} sequences with gaps</td></tr>
    </tbody></table>`;
}}
function renderLatest() {{
  document.getElementById('latest').innerHTML = data.latest_commits.map(c => `<div class="commit"><strong>${{c.short_hash}} · ${{c.subject}}</strong><span class="muted">${{c.author}} · ${{c.date.slice(0,10)}} · +${{fmt.format(c.insertions)}} / -${{fmt.format(c.deletions)}}</span></div>`).join('');
}}
function draw() {{
  renderCards(); renderBars(); lineChart('month-chart', data.commits_by_month);
  barCanvas('weekday-chart', data.commits_by_weekday, 'weekday', 'commits', '#e3b85c');
  barCanvas('hour-chart', data.commits_by_hour, 'hour', 'commits', '#70c5d6');
  heatmap(); renderTables(); renderUnity(); renderLatest();
  renderGithub(); renderCodeSummary(); renderQualitySummary();
  document.getElementById('status-note').textContent = data.summary.git_status_note;
}}
document.getElementById('scope-first').onclick = () => {{ scope = 'first'; document.getElementById('scope-first').classList.add('active'); document.getElementById('scope-all').classList.remove('active'); draw(); }};
document.getElementById('scope-all').onclick = () => {{ scope = 'all'; document.getElementById('scope-all').classList.add('active'); document.getElementById('scope-first').classList.remove('active'); draw(); }};
document.getElementById('metric-select').onchange = e => {{ metric = e.target.value; draw(); }};
draw();
</script>
</body>
</html>
"""


def main():
    OUT.mkdir(exist_ok=True)
    files, symbols, todos = scan_files()
    commits, file_churn, author_churn, author_domain_rows = parse_git_history()
    data = aggregate(files, commits, file_churn, author_churn, author_domain_rows)
    unity = parse_unity_metadata()
    code_data = analyze_code_content()
    unity_asset_data = analyze_unity_assets()
    addressables = analyze_addressables()
    ldtk_rows = analyze_ldtk()
    asmdef_rows, asmdef_dependencies = analyze_asmdefs()
    workflow_rows = analyze_workflows()
    naming = analyze_naming(files)
    animation_sequences = analyze_animation_sequences(files)
    github_data = analyze_github(commits)
    tag_rows, branch_rows = analyze_tags_and_branches()
    event_command_rows = analyze_event_command_coverage()
    dependency_source_rows = analyze_dependency_sources(unity)
    alias_contributors = build_alias_contributors(commits)
    risk_hotspots = build_risk_hotspots(code_data, file_churn)
    file_age_rows = analyze_file_age(files)
    metric_catalog = build_metric_catalog(github_data)
    data.update(
        {
            "code": code_data,
            "unity_assets": unity_asset_data,
            "addressables": addressables,
            "ldtk": ldtk_rows,
            "asmdefs": asmdef_rows,
            "asmdef_dependencies": asmdef_dependencies,
            "workflows": workflow_rows,
            "naming": naming,
            "animation_sequences": animation_sequences,
            "github": github_data,
            "tags": tag_rows,
            "branches": branch_rows,
            "event_command_coverage": event_command_rows,
            "dependency_sources": dependency_source_rows,
            "dependency_source_summary": summarize_rows(dependency_source_rows, "source", "packages"),
            "alias_contributors": alias_contributors,
            "risk_hotspots": risk_hotspots,
            "file_age": file_age_rows,
            "metric_catalog": metric_catalog,
        }
    )
    data["summary"].update(
        {
            "canonical_contributor_count": len(alias_contributors),
            "cs_files": code_data["summary"]["cs_files"],
            "mono_behaviour_files": code_data["summary"]["mono_behaviour_files"],
            "scriptable_object_files": code_data["summary"]["scriptable_object_files"],
            "scene_count": unity_asset_data["summary"]["scenes_analyzed"],
            "prefab_count": unity_asset_data["summary"]["prefabs_analyzed"],
            "workflow_count": len(workflow_rows),
            "github_prs": github_data["summary"]["prs"],
            "github_data_source": github_data["summary"]["source"],
            "naming_compliance_pct": naming["summary"]["compliance_pct"],
            "risk_hotspots": len(risk_hotspots),
        }
    )
    full = {"summary": data["summary"], "unity": unity, "symbols": symbols, "datasets": data}

    (OUT / "raw_stats.json").write_text(json.dumps(full, ensure_ascii=False, indent=2), encoding="utf-8")
    write_csv(OUT / "file_inventory.csv", files)
    write_csv(OUT / "commit_history.csv", commits)
    write_csv(OUT / "contributor_summary.csv", data["contributors"])
    write_csv(OUT / "contributor_alias_summary.csv", alias_contributors)
    write_csv(OUT / "author_domain_churn.csv", data["author_domain_churn"])
    write_csv(OUT / "extension_summary.csv", data["extension_summary"])
    write_csv(OUT / "category_summary.csv", data["category_summary"])
    write_csv(OUT / "directory_summary.csv", data["directory_summary"])
    write_csv(OUT / "domain_summary.csv", data["domain_summary"])
    write_csv(OUT / "top_churn_files.csv", data["top_churn_files"])
    write_csv(OUT / "todo_markers.csv", todos)
    write_csv(OUT / "metric_catalog.csv", metric_catalog)
    write_csv(OUT / "code_file_metrics.csv", code_data["code_file_metrics"])
    write_csv(OUT / "code_symbol_rows.csv", code_data["code_symbol_rows"])
    write_csv(OUT / "namespace_summary.csv", code_data["namespace_summary"])
    write_csv(OUT / "code_keyword_summary.csv", code_data["keyword_summary"])
    write_csv(OUT / "api_usage_summary.csv", code_data["api_usage_summary"])
    write_csv(OUT / "field_naming_violations.csv", code_data["field_violation_rows"])
    write_csv(OUT / "magic_number_hotspots.csv", code_data["magic_number_rows"])
    write_csv(OUT / "risk_hotspots.csv", risk_hotspots)
    write_csv(OUT / "unity_scene_complexity.csv", unity_asset_data["scene_complexity"])
    write_csv(OUT / "unity_prefab_complexity.csv", unity_asset_data["prefab_complexity"])
    write_csv(OUT / "guid_reference_summary.csv", unity_asset_data["guid_reference_summary"])
    write_csv(OUT / "potential_orphan_assets.csv", unity_asset_data["potential_orphan_assets"])
    write_csv(OUT / "addressables_summary.csv", addressables)
    write_csv(OUT / "ldtk_complexity.csv", ldtk_rows)
    write_csv(OUT / "asmdef_summary.csv", asmdef_rows)
    write_csv(OUT / "asmdef_dependencies.csv", asmdef_dependencies)
    write_csv(OUT / "workflow_complexity.csv", workflow_rows)
    write_csv(OUT / "asset_naming_violations.csv", naming["violations"])
    write_csv(OUT / "animation_sequence_gaps.csv", animation_sequences)
    write_csv(OUT / "github_pull_requests.csv", github_data["pull_requests"])
    write_csv(OUT / "github_prs_by_month.csv", github_data["prs_by_month"])
    write_csv(OUT / "github_issues.csv", github_data["issues"])
    write_csv(OUT / "github_actions_runs.csv", github_data["actions_runs"])
    write_csv(OUT / "git_tags.csv", tag_rows)
    write_csv(OUT / "git_branches.csv", branch_rows)
    write_csv(OUT / "event_command_coverage.csv", event_command_rows)
    write_csv(OUT / "dependency_sources.csv", dependency_source_rows)
    write_csv(OUT / "file_age.csv", file_age_rows)
    (OUT / "SUMMARY.md").write_text(make_summary_md(data, unity), encoding="utf-8")
    (OUT / "index.html").write_text(make_dashboard_html(data, unity, symbols), encoding="utf-8")
    print(json.dumps({"generated": str(OUT), "files": len(files), "commits": len(commits), "metrics": len(metric_catalog)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
