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

STOPWORDS = {
    "a",
    "an",
    "and",
    "are",
    "as",
    "at",
    "be",
    "by",
    "for",
    "from",
    "get",
    "has",
    "in",
    "is",
    "it",
    "new",
    "not",
    "of",
    "on",
    "or",
    "set",
    "the",
    "this",
    "to",
    "var",
    "void",
    "with",
    "using",
    "system",
    "unityengine",
    "public",
    "private",
    "protected",
    "internal",
    "static",
    "return",
    "class",
    "namespace",
    "true",
    "false",
    "null",
}

TOPIC_KEYWORDS = [
    ("Rendering", ("render", "shader", "light", "shadow", "camera", "sprite", "material", "vfx", "visual")),
    ("Gameplay", ("gameplay", "player", "enemy", "entity", "combat", "skill", "movement", "damage", "health")),
    ("UI", ("ui", "menu", "hud", "button", "panel", "canvas", "screen", "uxml", "uss")),
    ("Level/LDtk", ("level", "ldtk", "scene", "map", "tile", "biome", "world")),
    ("Audio", ("audio", "sound", "music", "fmod", "sfx", "voice")),
    ("Build/CI", ("ci", "build", "workflow", "action", "release", "deploy", "pipeline")),
    ("Data/Save", ("save", "data", "config", "setting", "json", "profile", "serialization")),
    ("Bug/Fix", ("bug", "fix", "crash", "error", "issue", "broken", "fail", "exception")),
    ("Refactor", ("refactor", "cleanup", "rename", "structure", "architecture", "optimize")),
    ("Docs/Stats", ("doc", "readme", "stats", "dashboard", "metric", "report")),
    ("Assets/Art", ("asset", "art", "texture", "sprite", "animation", "prefab", "icon")),
    ("Tests", ("test", "spec", "coverage", "verify")),
]

FIREFIGHTING_WORDS = (
    "fix",
    "bug",
    "crash",
    "error",
    "fail",
    "failed",
    "failure",
    "broken",
    "revert",
    "hotfix",
    "regression",
    "exception",
)


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


def age_days(value):
    dt = parse_dt(value)
    if not dt:
        return None
    return (datetime.now(dt.tzinfo) - dt).days


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


def split_identifier(name):
    if not name:
        return []
    name = re.sub(r"^m_", "", name)
    chunks = re.sub(r"([a-z0-9])([A-Z])", r"\1 \2", name.replace("_", " ")).split()
    return [c.lower() for c in chunks if len(c) > 1 and c.lower() not in STOPWORDS]


def naming_style(name):
    if re.match(r"^m_[a-zA-Z0-9_]+$", name):
        return "m_prefix"
    if re.match(r"^[A-Z][A-Za-z0-9]*$", name):
        return "PascalCase"
    if re.match(r"^[a-z][A-Za-z0-9]*$", name):
        return "camelCase"
    if re.match(r"^[a-z0-9]+(?:_[a-z0-9]+)+$", name):
        return "snake_case"
    if re.match(r"^[A-Z0-9_]+$", name) and "_" in name:
        return "UPPER_SNAKE"
    return "other"


def topic_for_text(text):
    low = (text or "").lower()
    hits = []
    for topic, words in TOPIC_KEYWORDS:
        score = sum(1 for word in words if word in low)
        if score:
            hits.append((score, topic))
    return sorted(hits, reverse=True)[0][1] if hits else "Other"


def aging_bucket(days):
    if days is None:
        return "unknown"
    if days < 14:
        return "0-13d"
    if days < 30:
        return "14-29d"
    if days < 60:
        return "30-59d"
    if days < 90:
        return "60-89d"
    return "90d+"


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
    author_extension_churn = defaultdict(lambda: defaultdict(lambda: {"insertions": 0, "deletions": 0, "files_touched": 0}))
    author_file_touches = defaultdict(lambda: defaultdict(lambda: {"insertions": 0, "deletions": 0, "touches": 0}))
    current = None
    current_author = None
    current_author_key = None
    out = run_git(["log", "--numstat", "--format=commit:%H%x09%an%x09%ae%x09%aI", "--no-renames"])
    for line in out.splitlines():
        if line.startswith("commit:"):
            meta = line[len("commit:") :].split("\t")
            current = meta[0] if meta else None
            current_author = meta[1] if len(meta) > 1 else None
            current_author_key = canonical_author(meta[1] if len(meta) > 1 else "", meta[2] if len(meta) > 2 else "")
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
            ext = Path(path).suffix.lower() or "(none)"
            author_churn[current_author]["insertions"] += ins
            author_churn[current_author]["deletions"] += dels
            author_churn[current_author]["files_touched"] += 1
            author_domain_churn[current_author_key][domain]["insertions"] += ins
            author_domain_churn[current_author_key][domain]["deletions"] += dels
            author_domain_churn[current_author_key][domain]["files_touched"] += 1
            author_extension_churn[current_author_key][ext]["insertions"] += ins
            author_extension_churn[current_author_key][ext]["deletions"] += dels
            author_extension_churn[current_author_key][ext]["files_touched"] += 1
            author_file_touches[current_author_key][path]["insertions"] += ins
            author_file_touches[current_author_key][path]["deletions"] += dels
            author_file_touches[current_author_key][path]["touches"] += 1

    for row in commit_rows:
        row.update(churn_by_commit.get(row["hash"], {"insertions": 0, "deletions": 0, "files": 0}))

    author_domain_rows = []
    for author, domains in author_domain_churn.items():
        for domain, values in domains.items():
            author_domain_rows.append({"author": author, "domain": domain, **values, "churn": values["insertions"] + values["deletions"]})
    author_domain_rows.sort(key=lambda r: (r["author"], -r["churn"]))

    author_extension_rows = []
    for author, extensions in author_extension_churn.items():
        for ext, values in extensions.items():
            author_extension_rows.append({"author": author, "extension": ext, **values, "churn": values["insertions"] + values["deletions"]})
    author_extension_rows.sort(key=lambda r: (r["author"], -r["churn"]))

    author_file_rows = []
    for author, paths in author_file_touches.items():
        for path, values in paths.items():
            author_file_rows.append({"author": author, "path": path, **values, "churn": values["insertions"] + values["deletions"]})
    author_file_rows.sort(key=lambda r: (r["author"], -r["churn"]))

    return commit_rows, file_churn, author_churn, author_domain_rows, author_extension_rows, author_file_rows


def aggregate(files, commits, file_churn, author_churn, author_domain_rows, author_extension_rows):
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
        "author_extension_churn": author_extension_rows,
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
    word_counter = Counter()
    name_word_counter = Counter()
    naming_style_counter = Counter()
    method_rows = []
    field_violation_rows = []
    magic_number_rows = []
    api_usage_rows = []
    symbol_naming_rows = []
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
        identifiers = re.findall(r"\b[A-Za-z_][A-Za-z0-9_]{2,}\b", clean)
        for identifier in identifiers:
            low = identifier.lower()
            if low not in STOPWORDS and not low.startswith("__"):
                word_counter[low] += 1
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
        public_field_names = re.findall(r"\bpublic\s+(?!class|struct|interface|enum|record|static|void)[A-Za-z0-9_<>,\[\]\?]+\s+([A-Za-z_][A-Za-z0-9_]*)\s*(?:=|;)", clean)
        symbol_names = [(m.group(1), m.group(2)) for m in type_matches]
        symbol_names.extend(("method", name) for name in methods)
        symbol_names.extend(("private_field", name) for name in private_fields)
        symbol_names.extend(("public_field", name) for name in public_field_names)
        for kind, name in symbol_names:
            style = naming_style(name)
            naming_style_counter[style] += 1
            for word in split_identifier(name):
                name_word_counter[word] += 1
            symbol_naming_rows.append({"path": rp.as_posix(), "symbol_kind": kind, "name": name, "style": style})
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
    code_word_rows = [{"word": k, "hits": v} for k, v in word_counter.most_common(300)]
    name_word_rows = [{"word": k, "hits": v} for k, v in name_word_counter.most_common(300)]
    naming_style_rows = [{"style": k, "symbols": v} for k, v in naming_style_counter.most_common()]
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
        "code_word_frequency": code_word_rows,
        "code_name_word_frequency": name_word_rows,
        "code_naming_style": naming_style_rows,
        "symbol_naming_rows": symbol_naming_rows,
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
    meta["available"] = True
    errors = []
    prs = []
    for limit in (1000, 500, 200):
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
                str(limit),
                "--json",
                "number,title,state,isDraft,author,createdAt,updatedAt,closedAt,mergedAt,additions,deletions,changedFiles,baseRefName,headRefName,labels",
            ],
            timeout=50,
        )
        if pr_code == 0 and pr_out.strip():
            try:
                prs = json.loads(pr_out)
                break
            except Exception as exc:
                errors.append(f"gh pr JSON parse failed at limit {limit}: {exc}")
        else:
            errors.append("gh pr list failed: " + (pr_err.strip() or pr_out.strip() or f"limit {limit}").splitlines()[0])
    if not prs:
        rest_prs = []
        for page in range(1, 8):
            rest_code, rest_out, rest_err = run_cmd(
                [
                    "gh",
                    "api",
                    "-X",
                    "GET",
                    f"repos/{repo}/pulls",
                    "-f",
                    "state=all",
                    "-f",
                    "per_page=100",
                    "-f",
                    f"page={page}",
                ],
                timeout=45,
            )
            if rest_code != 0:
                errors.append("gh api pulls failed: " + (rest_err.strip() or rest_out.strip() or f"page {page}").splitlines()[0])
                break
            try:
                page_rows = json.loads(rest_out) if rest_out.strip() else []
            except Exception as exc:
                errors.append(f"gh api pulls JSON parse failed at page {page}: {exc}")
                break
            if not page_rows:
                break
            rest_prs.extend(page_rows)
            if len(page_rows) < 100:
                break
        prs = [
            {
                "number": pr.get("number"),
                "title": pr.get("title", ""),
                "state": "MERGED" if pr.get("merged_at") else str(pr.get("state", "")).upper(),
                "isDraft": bool(pr.get("draft")),
                "author": {"login": ((pr.get("user") or {}).get("login", ""))},
                "createdAt": pr.get("created_at", ""),
                "updatedAt": pr.get("updated_at", ""),
                "closedAt": pr.get("closed_at", ""),
                "mergedAt": pr.get("merged_at", ""),
                "additions": 0,
                "deletions": 0,
                "changedFiles": 0,
                "baseRefName": ((pr.get("base") or {}).get("ref", "")),
                "headRefName": ((pr.get("head") or {}).get("ref", "")),
                "labels": [{"name": label.get("name", "")} for label in pr.get("labels", []) if isinstance(label, dict)],
                "rest_fallback": True,
            }
            for pr in rest_prs
        ]
        if prs:
            errors.append(f"gh pr list GraphQL unavailable; used REST pulls fallback ({len(prs)} PRs)")
    issue_code, issue_out, issue_err = run_cmd(
        [
            "gh",
            "issue",
            "list",
            "--repo",
            repo,
            "--state",
            "all",
            "--limit",
            "1000",
            "--json",
            "number,title,state,author,createdAt,updatedAt,closedAt,labels,assignees,comments,milestone",
        ],
        timeout=40,
    )
    issues = []
    if issue_code == 0 and issue_out.strip():
        try:
            issues = json.loads(issue_out)
        except Exception as exc:
            errors.append(f"gh issue JSON parse failed: {exc}")
    elif issue_code != 0:
        errors.append("gh issue list failed: " + (issue_err.strip() or issue_out.strip() or "unknown").splitlines()[0])
    run_code, run_out, run_err = run_cmd(
        ["gh", "run", "list", "--repo", repo, "--limit", "300", "--json", "databaseId,name,workflowName,status,conclusion,createdAt,updatedAt,event,headBranch"],
        timeout=40,
    )
    runs = []
    if run_code == 0 and run_out.strip():
        try:
            runs = json.loads(run_out)
        except Exception as exc:
            errors.append(f"gh run JSON parse failed: {exc}")
    elif run_code != 0:
        errors.append("gh run list failed: " + (run_err.strip() or run_out.strip() or "unknown").splitlines()[0])
    meta["error"] = " | ".join(errors)
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
    inferred_by_number = {row["number"]: row for row in inferred}
    if gh_prs:
        for pr in gh_prs:
            created = pr.get("createdAt")
            merged = pr.get("mergedAt")
            closed = pr.get("closedAt")
            end = merged or closed
            labels = pr.get("labels") or []
            label_text = ",".join(label.get("name", "") for label in labels if isinstance(label, dict))
            topic = topic_for_text(" ".join([pr.get("title", ""), label_text, pr.get("headRefName", ""), pr.get("baseRefName", "")]))
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
                    "additions": pr.get("additions") or inferred_by_number.get(pr.get("number"), {}).get("additions_hint", 0),
                    "deletions": pr.get("deletions") or inferred_by_number.get(pr.get("number"), {}).get("deletions_hint", 0),
                    "changed_files": pr.get("changedFiles") or inferred_by_number.get(pr.get("number"), {}).get("changed_files_hint", 0),
                    "base": pr.get("baseRefName", ""),
                    "head": pr.get("headRefName", ""),
                    "labels": label_text,
                    "topic": topic,
                    "review_decision": pr.get("reviewDecision") or "",
                    "source": "gh_rest" if pr.get("rest_fallback") else "gh",
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
                    "topic": topic_for_text(row["title"]),
                    "review_decision": "",
                    "source": "git_subject_inferred",
                }
            )
    month_counter = defaultdict(lambda: {"prs": 0, "merged": 0, "closed_unmerged": 0, "additions": 0, "deletions": 0})
    label_counter = Counter()
    author_counter = Counter()
    pr_topic_counter = Counter()
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
        pr_topic_counter[pr.get("topic") or "Other"] += 1
        for label in str(pr.get("labels", "")).split(","):
            if label:
                label_counter[label] += 1
    pr_month_rows = [{"month": m, **v} for m, v in sorted(month_counter.items())]
    issue_rows = []
    overdue_issue_rows = []
    issue_topic_counter = Counter()
    issue_label_counter = Counter()
    issue_aging_counter = Counter()
    for issue in gh_issues:
        labels = issue.get("labels") or []
        label_text = ",".join(label.get("name", "") for label in labels if isinstance(label, dict))
        milestone = issue.get("milestone") or {}
        due_on = milestone.get("dueOn", "") if isinstance(milestone, dict) else ""
        created_at = issue.get("createdAt", "")
        updated_at = issue.get("updatedAt", "")
        closed_at = issue.get("closedAt", "")
        issue_age = age_days(created_at)
        stale = age_days(updated_at)
        is_open = str(issue.get("state", "")).lower() == "open"
        due_dt = parse_dt(due_on)
        overdue_reasons = []
        if is_open and due_dt and datetime.now(due_dt.tzinfo) > due_dt:
            overdue_reasons.append("milestone_due_passed")
        if is_open and issue_age is not None and issue_age >= 30:
            overdue_reasons.append("open_30d_plus")
        if is_open and stale is not None and stale >= 14:
            overdue_reasons.append("stale_14d_plus")
        topic = topic_for_text(" ".join([issue.get("title", ""), label_text, milestone.get("title", "") if isinstance(milestone, dict) else ""]))
        bucket = "closed" if not is_open else aging_bucket(issue_age)
        issue_topic_counter[topic] += 1
        issue_aging_counter[bucket] += 1
        for label in label_text.split(","):
            if label:
                issue_label_counter[label] += 1
        row = {
            "number": issue.get("number"),
            "title": issue.get("title", ""),
            "state": issue.get("state", ""),
            "author": (issue.get("author") or {}).get("login", ""),
            "created_at": created_at,
            "updated_at": updated_at,
            "closed_at": closed_at,
            "lead_time_hours": hours_between(created_at, closed_at),
            "age_days": issue_age if issue_age is not None else "",
            "stale_days": stale if stale is not None else "",
            "comments": issue.get("comments", 0),
            "milestone": milestone.get("title", "") if isinstance(milestone, dict) else "",
            "due_on": due_on,
            "labels": label_text,
            "assignees": ",".join(a.get("login", "") for a in issue.get("assignees", []) if isinstance(a, dict)),
            "topic": topic,
            "aging_bucket": bucket,
            "overdue": bool(overdue_reasons),
            "overdue_reason": ",".join(overdue_reasons),
        }
        issue_rows.append(
            row
        )
        if overdue_reasons:
            overdue_issue_rows.append(row)
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
        "gh_available": bool(gh_prs or gh_issues or gh_runs),
        "gh_status": ("available" if (gh_prs or gh_issues or gh_runs) else "not available")
        + (f"; {gh_meta.get('error')}" if gh_meta.get("error") else ""),
        "prs": len(pr_rows),
        "open_prs": sum(1 for r in pr_rows if str(r["state"]).lower() == "open"),
        "merged_prs": sum(1 for r in pr_rows if r["merged"]),
        "closed_unmerged_prs": sum(1 for r in pr_rows if str(r["state"]).lower() == "closed" and not r["merged"]),
        "merge_rate_pct": pct(sum(1 for r in pr_rows if r["merged"]), len(pr_rows)),
        "median_pr_lead_time_hours": median_or_zero(lead_times),
        "p90_pr_lead_time_hours": round(percentile(lead_times, 0.9), 2) if lead_times else 0,
        "issues": len(issue_rows),
        "open_issues": sum(1 for r in issue_rows if str(r["state"]).lower() == "open"),
        "closed_issues": sum(1 for r in issue_rows if str(r["state"]).lower() == "closed"),
        "overdue_issues": len(overdue_issue_rows),
        "median_issue_close_hours": median_or_zero(issue_leads),
        "actions_runs": len(run_rows),
        "actions_success_rate_pct": pct(conclusion_counter["success"], len(run_rows)),
    }
    return {
        "summary": summary,
        "pull_requests": sorted(pr_rows, key=lambda r: r["number"] or 0, reverse=True),
        "prs_by_month": pr_month_rows,
        "pr_labels": [{"label": k, "prs": v} for k, v in label_counter.most_common()],
        "pr_topics": [{"topic": k, "prs": v} for k, v in pr_topic_counter.most_common()],
        "pr_authors": [{"author": k, "prs": v} for k, v in author_counter.most_common()],
        "issues": issue_rows,
        "overdue_issues": sorted(overdue_issue_rows, key=lambda r: (r.get("age_days") or 0, r.get("stale_days") or 0), reverse=True),
        "issue_topics": [{"topic": k, "issues": v} for k, v in issue_topic_counter.most_common()],
        "issue_labels": [{"label": k, "issues": v} for k, v in issue_label_counter.most_common()],
        "issue_aging": [{"bucket": k, "issues": v} for k, v in issue_aging_counter.most_common()],
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


def build_contributor_profiles(alias_contributors, author_domain_rows, author_extension_rows, author_file_rows, files):
    file_size = {r["path"]: r["bytes"] for r in files if not r["path"].startswith("Stats/")}
    domain_by_author = defaultdict(list)
    extension_by_author = defaultdict(list)
    paths_by_author = defaultdict(set)
    churn_by_author = defaultdict(int)
    for row in author_domain_rows:
        domain_by_author[row["author"]].append(row)
    for row in author_extension_rows:
        extension_by_author[row["author"]].append(row)
    for row in author_file_rows:
        paths_by_author[row["author"]].add(row["path"])
        churn_by_author[row["author"]] += row["churn"]

    profiles = []
    for contributor in alias_contributors:
        author = contributor["canonical_author"]
        domains = sorted(domain_by_author.get(author, []), key=lambda r: r["churn"], reverse=True)
        extensions = sorted(extension_by_author.get(author, []), key=lambda r: r["churn"], reverse=True)
        paths = paths_by_author.get(author, set())
        top_domain = domains[0]["domain"] if domains else ""
        top_extension = extensions[0]["extension"] if extensions else ""
        current_bytes = sum(file_size.get(path, 0) for path in paths)
        commits = contributor["commits"]
        churn = contributor["insertions"] + contributor["deletions"]
        profiles.append(
            {
                "canonical_author": author,
                "commits": commits,
                "insertions": contributor["insertions"],
                "deletions": contributor["deletions"],
                "net_lines": contributor["insertions"] - contributor["deletions"],
                "churn": churn,
                "files_touched": contributor["files"],
                "unique_files_touched": len(paths),
                "current_bytes_touched": current_bytes,
                "avg_churn_per_commit": round(churn / commits, 2) if commits else 0,
                "top_domain": top_domain,
                "top_domain_churn": domains[0]["churn"] if domains else 0,
                "top_extension": top_extension,
                "top_extension_churn": extensions[0]["churn"] if extensions else 0,
                "preference": " / ".join(x for x in [top_domain, top_extension] if x),
                "aliases": contributor["aliases"],
            }
        )
    return sorted(profiles, key=lambda r: (r["commits"], r["churn"]), reverse=True)


def analyze_fun_stats(commits, github_data):
    commits_by_pr = defaultdict(list)
    commits_by_author = defaultdict(list)
    commits_by_day_author = defaultdict(lambda: defaultdict(lambda: {"commits": 0, "insertions": 0, "deletions": 0}))
    firefighting_by_author = defaultdict(lambda: {"fire_commits": 0, "reverts": 0, "commits": 0, "churn": 0})
    firefighting_by_month = defaultdict(lambda: {"fire_commits": 0, "commits": 0, "churn": 0})
    for c in commits:
        author = canonical_author(c.get("author", ""), c.get("email", ""))
        commits_by_author[author].append(c)
        if c.get("pr_number"):
            commits_by_pr[c["pr_number"]].append(c)
        daily = commits_by_day_author[author][c["date_day"]]
        daily["commits"] += 1
        daily["insertions"] += c.get("insertions", 0)
        daily["deletions"] += c.get("deletions", 0)
        subject = c.get("subject", "").lower()
        is_fire = any(word in subject for word in FIREFIGHTING_WORDS)
        firefighting_by_author[author]["commits"] += 1
        firefighting_by_author[author]["churn"] += c.get("insertions", 0) + c.get("deletions", 0)
        firefighting_by_month[c["year_month"]]["commits"] += 1
        firefighting_by_month[c["year_month"]]["churn"] += c.get("insertions", 0) + c.get("deletions", 0)
        if is_fire:
            firefighting_by_author[author]["fire_commits"] += 1
            firefighting_by_month[c["year_month"]]["fire_commits"] += 1
        if c.get("is_revert"):
            firefighting_by_author[author]["reverts"] += 1

    deadline_events = []
    deadline_by_author = defaultdict(lambda: {"deadline_prs": 0, "sprint_24h": 0, "sprint_72h": 0, "total_hours_before_deadline": 0})
    for pr in github_data.get("pull_requests", []):
        number = pr.get("number")
        deadline = pr.get("merged_at") or pr.get("closed_at")
        deadline_dt = parse_dt(deadline)
        related_commits = commits_by_pr.get(number, [])
        if not deadline_dt:
            continue
        candidates = []
        created_dt = parse_dt(pr.get("created_at"))
        if created_dt and created_dt <= deadline_dt:
            candidates.append((created_dt, None, "pr_created_at"))
        for c in related_commits:
            commit_dt = parse_dt(c.get("date"))
            if commit_dt and commit_dt <= deadline_dt:
                candidates.append((commit_dt, c, "linked_commit"))
        if not candidates:
            continue
        first_dt, first_commit, first_source = min(candidates, key=lambda x: x[0])
        last_dt = max(dt for dt, _, _ in candidates)
        hours_before = round((deadline_dt - first_dt).total_seconds() / 3600, 2)
        if hours_before < 0:
            continue
        author = canonical_author(first_commit.get("author", ""), first_commit.get("email", "")) if first_commit else canonical_author(pr.get("author", ""), "")
        churn = sum(c.get("insertions", 0) + c.get("deletions", 0) for c in related_commits)
        event = {
            "pr_number": number,
            "title": pr.get("title", ""),
            "author": author,
            "github_author": pr.get("author", ""),
            "topic": pr.get("topic", "Other"),
            "deadline_at": deadline,
            "deadline_type": "merged_at" if pr.get("merged_at") else "closed_at",
            "first_signal_at": first_dt.isoformat(),
            "first_commit_at": first_dt.isoformat(),
            "last_commit_at": last_dt.isoformat(),
            "first_signal_source": first_source,
            "hours_before_deadline": hours_before,
            "within_24h": 0 <= hours_before <= 24,
            "within_72h": 0 <= hours_before <= 72,
            "commit_count": len(related_commits),
            "churn": churn,
            "additions": pr.get("additions", 0),
            "deletions": pr.get("deletions", 0),
            "changed_files": pr.get("changed_files", 0),
        }
        deadline_events.append(event)
        stats = deadline_by_author[author]
        stats["deadline_prs"] += 1
        stats["sprint_24h"] += 1 if event["within_24h"] else 0
        stats["sprint_72h"] += 1 if event["within_72h"] else 0
        stats["total_hours_before_deadline"] += hours_before

    deadline_stats = []
    for author, values in deadline_by_author.items():
        deadline_prs = values["deadline_prs"]
        deadline_stats.append(
            {
                "author": author,
                "deadline_prs": deadline_prs,
                "sprint_24h": values["sprint_24h"],
                "sprint_72h": values["sprint_72h"],
                "sprint_24h_rate_pct": pct(values["sprint_24h"], deadline_prs),
                "avg_hours_before_deadline": round(values["total_hours_before_deadline"] / deadline_prs, 2) if deadline_prs else 0,
                "title": "压哨王" if values["sprint_24h"] else "提前量选手",
            }
        )
    deadline_stats.sort(key=lambda r: (r["sprint_24h"], r["sprint_24h_rate_pct"], r["deadline_prs"]), reverse=True)

    rhythm_profiles = []
    for author, rows in commits_by_author.items():
        if not rows:
            continue
        hour_counter = Counter(c["hour"] for c in rows)
        weekday_counter = Counter(c["weekday"] for c in rows)
        night = sum(1 for c in rows if c["is_night"])
        weekend = sum(1 for c in rows if c["is_weekend"])
        daily_rows = commits_by_day_author[author]
        peak_day, peak = max(daily_rows.items(), key=lambda kv: (kv[1]["commits"], kv[1]["insertions"] + kv[1]["deletions"]))
        churn = sum(c.get("insertions", 0) + c.get("deletions", 0) for c in rows)
        labels = []
        if pct(night, len(rows)) >= 35:
            labels.append("夜猫子")
        if pct(weekend, len(rows)) >= 25:
            labels.append("周末战士")
        if peak["commits"] >= 8:
            labels.append("爆发型")
        if churn / max(len(rows), 1) >= 10000:
            labels.append("大块改造派")
        if not labels:
            labels.append("稳态推进派")
        rhythm_profiles.append(
            {
                "author": author,
                "commits": len(rows),
                "night_commits": night,
                "night_pct": pct(night, len(rows)),
                "weekend_commits": weekend,
                "weekend_pct": pct(weekend, len(rows)),
                "favorite_hour": hour_counter.most_common(1)[0][0],
                "favorite_weekday": weekday_counter.most_common(1)[0][0],
                "peak_day": peak_day,
                "peak_day_commits": peak["commits"],
                "peak_day_churn": peak["insertions"] + peak["deletions"],
                "avg_churn_per_commit": round(churn / max(len(rows), 1), 2),
                "persona": " / ".join(labels),
            }
        )
    rhythm_profiles.sort(key=lambda r: (r["peak_day_commits"], r["night_pct"], r["commits"]), reverse=True)

    firefighting_rows = []
    for author, values in firefighting_by_author.items():
        score = values["fire_commits"] * 2 + values["reverts"] * 3
        firefighting_rows.append(
            {
                "author": author,
                "commits": values["commits"],
                "fire_commits": values["fire_commits"],
                "fire_commit_pct": pct(values["fire_commits"], values["commits"]),
                "reverts": values["reverts"],
                "churn": values["churn"],
                "fire_score": score,
                "label": "救火队长" if score >= 10 else "平稳维护",
            }
        )
    firefighting_rows.sort(key=lambda r: (r["fire_score"], r["fire_commit_pct"], r["commits"]), reverse=True)
    firefighting_month_rows = [
        {
            "month": month,
            **values,
            "fire_commit_pct": pct(values["fire_commits"], values["commits"]),
        }
        for month, values in sorted(firefighting_by_month.items())
    ]

    pr_personality = []
    for pr in github_data.get("pull_requests", []):
        churn = int(pr.get("additions") or 0) + int(pr.get("deletions") or 0)
        lead = pr.get("lead_time_hours")
        lead_num = float(lead) if lead not in ("", None) else None
        tags = []
        if churn >= 100000 or int(pr.get("changed_files") or 0) >= 500:
            tags.append("巨型迁移")
        if lead_num is not None and lead_num <= 0.25:
            tags.append("秒合")
        if lead_num is not None and lead_num >= 72:
            tags.append("长期悬案")
        if churn <= 500 and int(pr.get("changed_files") or 0) <= 10:
            tags.append("小刀快修")
        if pr.get("draft"):
            tags.append("草稿实验")
        if not pr.get("merged"):
            tags.append("未合入")
        if not tags:
            tags.append("常规推进")
        pr_personality.append(
            {
                "number": pr.get("number"),
                "title": pr.get("title", ""),
                "author": pr.get("author", ""),
                "topic": pr.get("topic", "Other"),
                "lead_time_hours": lead if lead not in (None, "") else "",
                "churn": churn,
                "changed_files": pr.get("changed_files", 0),
                "merged": pr.get("merged", False),
                "personality": " / ".join(tags),
            }
        )
    pr_personality.sort(key=lambda r: (r["churn"], r["changed_files"]), reverse=True)

    issue_deadlines = []
    for issue in github_data.get("issues", []):
        if not issue.get("due_on"):
            continue
        due_dt = parse_dt(issue.get("due_on"))
        closed_dt = parse_dt(issue.get("closed_at"))
        days_delta = None
        if due_dt and closed_dt:
            days_delta = round((closed_dt - due_dt).total_seconds() / 86400, 2)
        issue_deadlines.append(
            {
                "number": issue.get("number"),
                "title": issue.get("title", ""),
                "state": issue.get("state", ""),
                "topic": issue.get("topic", "Other"),
                "milestone": issue.get("milestone", ""),
                "due_on": issue.get("due_on"),
                "closed_at": issue.get("closed_at", ""),
                "days_after_due": days_delta if days_delta is not None else "",
                "status": "overdue" if issue.get("overdue") else "on_track_or_closed",
            }
        )

    summary = {
        "deadline_events": len(deadline_events),
        "sprint_24h_events": sum(1 for r in deadline_events if r["within_24h"]),
        "sprint_72h_events": sum(1 for r in deadline_events if r["within_72h"]),
        "top_sprinter": deadline_stats[0]["author"] if deadline_stats else "",
        "top_sprinter_24h": deadline_stats[0]["sprint_24h"] if deadline_stats else 0,
        "firefighting_leader": firefighting_rows[0]["author"] if firefighting_rows else "",
        "firefighting_score": firefighting_rows[0]["fire_score"] if firefighting_rows else 0,
    }
    return {
        "summary": summary,
        "deadline_sprint_stats": deadline_stats,
        "deadline_sprint_events": sorted(deadline_events, key=lambda r: (r["within_24h"], -r["hours_before_deadline"]), reverse=True),
        "contributor_rhythm_profiles": rhythm_profiles,
        "firefighting_index": firefighting_rows,
        "firefighting_by_month": firefighting_month_rows,
        "pr_personality": pr_personality,
        "issue_deadlines": issue_deadlines,
    }


def analyze_file_churn_timeline(top_paths):
    wanted = set(top_paths)
    if not wanted:
        return []
    rows = defaultdict(lambda: {"insertions": 0, "deletions": 0, "touches": 0})
    current_month = None
    out = run_git(["log", "--numstat", "--format=commit:%aI", "--no-renames"])
    for line in out.splitlines():
        if line.startswith("commit:"):
            dt = parse_dt(line[len("commit:") :])
            current_month = dt.strftime("%Y-%m") if dt else None
            continue
        parts = line.split("\t")
        if len(parts) != 3 or current_month is None:
            continue
        ins_s, del_s, path = parts
        if path not in wanted or ins_s == "-" or del_s == "-":
            continue
        key = (path, current_month)
        rows[key]["insertions"] += int(ins_s)
        rows[key]["deletions"] += int(del_s)
        rows[key]["touches"] += 1
    result = []
    for (path, month), values in rows.items():
        result.append({"path": path, "month": month, **values, "churn": values["insertions"] + values["deletions"]})
    return sorted(result, key=lambda r: (r["path"], r["month"]))


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
        ("P0", "中", "贡献者偏好扩展名/触碰体量", "implemented", "git numstat + current file inventory"),
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
        ("P0", "低/中", "PR 主题统计", "implemented" if github_data["summary"]["prs"] else "local-inferred", "PR title/label topic buckets"),
        ("P0", "低/中", "Issue 主题统计", "implemented" if github_data["summary"]["issues"] else "blocked-gh-auth", "Issue title/label topic buckets"),
        ("P0", "低/中", "Issue 逾期/老化统计", "implemented" if github_data["summary"]["issues"] else "blocked-gh-auth", "open age, stale days, milestone due date"),
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
        ("P1", "低", "代码词语偏好", "implemented", "C# identifier token frequency"),
        ("P1", "低", "命名风格分布", "implemented", "symbol naming regex buckets"),
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
        ("P2", "中", "压哨王/DDL 前 24h 开始提交", "implemented", "PR completion time + PR created/linked commit first signal"),
        ("P2", "中", "开发节奏人格标签", "implemented", "commit hour, weekday, burst day, churn"),
        ("P2", "中", "救火指数", "implemented", "commit/PR fix words and reverts"),
        ("P2", "中", "PR 性格分类", "implemented", "PR lead time, churn, changed files, merge state"),
        ("P2", "中", "代码热点命运线", "implemented", "top churn files by month"),
        ("P2", "中", "高风险热点", "implemented", "complexity + churn + size + tests heuristic"),
        ("P2", "中", "协程/async/LINQ/反射/资源路径/异常处理", "implemented", "C# token scan"),
        ("P2", "高", "Review 响应时间/轮次/矩阵", "deferred-high-cost", "needs per-PR GitHub review detail calls"),
        ("P2", "高", "评论情绪温度/争议 PR", "deferred-high-cost", "needs per-PR GitHub comment detail calls"),
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
            "## Fun Metrics",
            "",
            f"- Deadline sprint events: {fmt_int(s.get('deadline_sprint_events', 0))}",
            f"- 24h sprint events: {fmt_int(s.get('deadline_sprint_24h', 0))}",
            f"- Top sprinter: {s.get('top_sprinter') or 'n/a'}",
            f"- Firefighting leader: {s.get('firefighting_leader') or 'n/a'}",
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
            "- `contributor_profiles.csv`: contributor preference, churn, and touched-current-size profile",
            "- `metric_catalog.csv`: implemented / partial / blocked metric catalog",
            "- `code_file_metrics.csv`: file-level C# content metrics",
            "- `code_word_frequency.csv`, `code_name_word_frequency.csv`, `code_naming_style.csv`: code vocabulary and naming preference metrics",
            "- `github_pull_requests.csv`: PR metrics from `gh` when available, otherwise local inferred PR rows",
            "- `github_pr_topics.csv`, `github_issue_topics.csv`, `github_overdue_issues.csv`: GitHub topic and aging metrics",
            "- `deadline_sprint_stats.csv`, `deadline_sprint_events.csv`: DDL/PR completion sprint metrics",
            "- `contributor_rhythm_profiles.csv`: contributor rhythm and persona tags",
            "- `firefighting_index.csv`, `pr_personality.csv`: fun maintenance and PR personality metrics",
            "- `file_churn_timeline.csv`: month-level churn timeline for hot files",
            "- `workflow_complexity.csv`: GitHub Actions workflow complexity",
            "- `unity_scene_complexity.csv`, `unity_prefab_complexity.csv`: Unity YAML complexity metrics",
            "- `risk_hotspots.csv`: combined code risk heuristic",
            "- `extension_summary.csv`, `category_summary.csv`, `directory_summary.csv`: project composition summaries",
            "",
            f"Note: {s['git_status_note']}",
        ]
    )
    return "\n".join(lines) + "\n"


def _dashboard_top(rows, limit=20):
    return list(rows or [])[:limit]


def payload_for_dashboard(data, unity, symbols):
    archive = [
        ("raw_stats.json", "Raw nested source", "source"),
        ("SUMMARY.md", "Readable generation summary", "source"),
        ("file_inventory.csv", "File inventory", "project"),
        ("category_summary.csv", "Category composition", "project"),
        ("directory_summary.csv", "Directory summary", "project"),
        ("commit_history.csv", "Commit history", "git"),
        ("commits_by_month.csv", "Monthly commit rhythm", "git"),
        ("contributor_alias_summary.csv", "Alias-merged contributors", "people"),
        ("contributor_profiles.csv", "Contributor persona profiles", "people"),
        ("deadline_sprint_stats.csv", "Deadline sprint leaderboard", "fun"),
        ("deadline_sprint_events.csv", "Deadline sprint events", "fun"),
        ("contributor_rhythm_profiles.csv", "Night/weekend/churn rhythm", "fun"),
        ("firefighting_index.csv", "Firefighting index", "fun"),
        ("firefighting_by_month.csv", "Firefighting by month", "fun"),
        ("pr_personality.csv", "PR personality tags", "fun"),
        ("code_file_metrics.csv", "Code file metrics", "code"),
        ("code_word_frequency.csv", "Code vocabulary", "code"),
        ("code_name_word_frequency.csv", "Naming vocabulary", "code"),
        ("risk_hotspots.csv", "Code risk hotspots", "code"),
        ("file_churn_timeline.csv", "Hot file churn timeline", "code"),
        ("github_pull_requests.csv", "GitHub pull requests", "github"),
        ("github_pr_topics.csv", "PR topic buckets", "github"),
        ("github_issues.csv", "GitHub issues", "github"),
        ("github_issue_aging.csv", "Issue aging pipeline", "github"),
        ("github_overdue_issues.csv", "Overdue issues", "github"),
        ("github_actions_runs.csv", "GitHub Actions runs", "github"),
        ("unity_scene_complexity.csv", "Unity scene complexity", "unity"),
        ("unity_prefab_complexity.csv", "Unity prefab complexity", "unity"),
        ("unity_guid_reference_summary.csv", "Unity GUID references", "unity"),
        ("workflow_complexity.csv", "Workflow complexity", "ci"),
    ]
    return {
        "summary": data["summary"],
        "unity": unity,
        "symbols": symbols,
        "composition": _dashboard_top(data["first_party_category_summary"], 18),
        "allComposition": _dashboard_top(data["category_summary"], 18),
        "domains": _dashboard_top(data["first_party_domain_summary"], 18),
        "directories": _dashboard_top(data["directory_summary"], 16),
        "months": data["commits_by_month"],
        "days": data["commits_by_day"],
        "contributors": _dashboard_top(data["contributor_profiles"], 24),
        "aliasContributors": _dashboard_top(data["alias_contributors"], 24),
        "deadlineStats": _dashboard_top(data["fun_stats"]["deadline_sprint_stats"], 24),
        "deadlineEvents": _dashboard_top(data["fun_stats"]["deadline_sprint_events"], 120),
        "rhythm": _dashboard_top(data["fun_stats"]["contributor_rhythm_profiles"], 24),
        "firefighting": _dashboard_top(data["fun_stats"]["firefighting_index"], 24),
        "fireMonths": data["fun_stats"]["firefighting_by_month"],
        "codeSummary": data["code"]["summary"],
        "codeWords": _dashboard_top(data["code"]["code_word_frequency"], 80),
        "nameWords": _dashboard_top(data["code"]["code_name_word_frequency"], 80),
        "namingStyle": data["code"]["code_naming_style"],
        "apiUsage": _dashboard_top(data["code"]["api_usage_summary"], 24),
        "risk": _dashboard_top(data["risk_hotspots"], 56),
        "fileChurnTimeline": _dashboard_top(data["file_churn_timeline"], 360),
        "githubSummary": data["github"]["summary"],
        "prsByMonth": data["github"]["prs_by_month"],
        "prs": _dashboard_top(data["github"]["pull_requests"], 120),
        "prTopics": data["github"]["pr_topics"],
        "issues": _dashboard_top(data["github"]["issues"], 160),
        "issueTopics": data["github"]["issue_topics"],
        "issueAging": data["github"]["issue_aging"],
        "overdueIssues": _dashboard_top(data["github"]["overdue_issues"], 60),
        "actions": data["github"]["actions_conclusions"],
        "prPersonality": _dashboard_top(data["fun_stats"]["pr_personality"], 120),
        "scenes": _dashboard_top(data["unity_assets"]["scene_complexity"], 28),
        "prefabs": _dashboard_top(data["unity_assets"]["prefab_complexity"], 32),
        "guidRefs": _dashboard_top(data["unity_assets"]["guid_reference_summary"], 56),
        "archive": [{"file": f, "label": label, "kind": kind} for f, label, kind in archive],
    }


def style_css():
    return """
:root{
  --ink:#070907; --ink2:#0c100d; --panel:#111711; --panel2:#161d18; --line:#2a362d;
  --text:#f3f7ee; --muted:#9ead9f; --green:#a4ff72; --cyan:#65e7d7; --gold:#ffd166;
  --red:#ff6b5e; --orange:#ff9f43; --violet:#d2a8ff; --shadow:0 24px 70px rgba(0,0,0,.38);
  --max:1180px;
}
*{box-sizing:border-box}
html{scroll-behavior:smooth}
body{margin:0;background:var(--ink);color:var(--text);font:14px/1.55 Inter,ui-sans-serif,system-ui,-apple-system,BlinkMacSystemFont,"Segoe UI",sans-serif;letter-spacing:0}
a{color:inherit} canvas{display:block;width:100%}
button,input,select{font:inherit;color:inherit}
.hero{position:relative;min-height:92vh;overflow:hidden;border-bottom:1px solid var(--line);background:#070907}
.hero canvas{position:absolute;inset:0;height:100%;opacity:.9}
.hero::after{content:"";position:absolute;inset:0;background:linear-gradient(180deg,rgba(7,9,7,.22),rgba(7,9,7,.84) 78%,#070907)}
.topbar{position:sticky;top:0;z-index:30;display:flex;align-items:center;gap:18px;padding:12px clamp(16px,4vw,42px);background:rgba(7,9,7,.84);backdrop-filter:blur(18px);border-bottom:1px solid rgba(164,255,114,.16)}
.brand{font-weight:750;color:var(--green);white-space:nowrap}
.nav{display:flex;gap:8px;overflow:auto;flex:1;scrollbar-width:none}.nav::-webkit-scrollbar{display:none}
.nav a{padding:8px 10px;border:1px solid transparent;border-radius:7px;text-decoration:none;color:var(--muted);white-space:nowrap}
.nav a.active,.nav a:hover{border-color:rgba(164,255,114,.35);color:var(--text);background:rgba(164,255,114,.08)}
.hero-inner{position:relative;z-index:2;width:min(var(--max),calc(100% - 32px));margin:0 auto;padding:18vh 0 10vh}
.eyebrow{color:var(--cyan);text-transform:uppercase;letter-spacing:.14em;font-size:12px;font-weight:750}
h1{font-size:clamp(48px,9vw,116px);line-height:.92;margin:18px 0 18px;letter-spacing:0;max-width:min(960px,100%);overflow-wrap:anywhere}
h1 span{display:block}
.lead{font-size:clamp(18px,2.2vw,28px);max-width:min(760px,100%);color:#dce7da;margin:0 0 34px;overflow-wrap:anywhere}
.hero-metrics{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:12px;max-width:920px}
.metric{border:1px solid rgba(164,255,114,.24);border-radius:8px;background:rgba(13,18,14,.62);padding:16px;box-shadow:var(--shadow)}
.metric b{display:block;font-size:clamp(26px,4vw,44px);line-height:1;color:var(--text)}
.metric span{display:block;margin-top:8px;color:var(--muted)}
.chapter{position:relative;padding:84px clamp(16px,4vw,42px);border-bottom:1px solid var(--line);background:var(--ink)}
.chapter.alt{background:#0b0d0a}
.wrap{max-width:var(--max);margin:0 auto}
.section-head{display:grid;grid-template-columns:minmax(0,1.1fr) minmax(260px,.9fr);gap:28px;align-items:end;margin-bottom:28px}
.kicker{color:var(--gold);font-weight:800;text-transform:uppercase;font-size:12px;letter-spacing:.13em}
h2{font-size:clamp(34px,5.2vw,72px);line-height:1;margin:8px 0 0;letter-spacing:0}
.section-head p{color:var(--muted);margin:0;font-size:16px}
.control-band{position:sticky;top:58px;z-index:24;background:rgba(7,9,7,.92);border-block:1px solid var(--line);backdrop-filter:blur(18px)}
.controls{max-width:var(--max);margin:0 auto;padding:12px 16px;display:grid;grid-template-columns:1fr auto auto;gap:12px;align-items:center}
.search{width:100%;border:1px solid var(--line);border-radius:8px;background:#0b0f0c;padding:10px 12px;outline:none}
.chiprow{display:flex;gap:8px;flex-wrap:wrap}.chip,.ghost{border:1px solid var(--line);background:#101611;border-radius:7px;padding:8px 10px;cursor:pointer;color:var(--muted)}
.chip.active,.ghost:hover{border-color:var(--green);color:var(--text);background:rgba(164,255,114,.08)}
.grid{display:grid;gap:16px}.grid.two{grid-template-columns:1.08fr .92fr}.grid.three{grid-template-columns:repeat(3,minmax(0,1fr))}
.panel{border:1px solid var(--line);border-radius:8px;background:linear-gradient(180deg,var(--panel),#0d120e);padding:16px;min-width:0;box-shadow:var(--shadow)}
.panel h3{margin:0 0 10px;font-size:15px}.panel .sub{margin:-4px 0 12px;color:var(--muted);font-size:12px}
.tall{min-height:420px}.mid{min-height:320px}.short{min-height:230px}
.canvas-box{height:360px;position:relative}.canvas-box.short{height:240px}.canvas-box.tall{height:430px}.canvas-box canvas{height:100%}
.legend{display:flex;gap:8px;flex-wrap:wrap;margin-top:10px;color:var(--muted);font-size:12px}
.dot{width:9px;height:9px;border-radius:9px;display:inline-block;margin-right:5px}
.personas{display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:12px}
.persona{border:1px solid var(--line);border-radius:8px;background:#0e140f;padding:14px;cursor:pointer;transition:.18s transform,.18s border-color}
.persona:hover{transform:translateY(-2px);border-color:rgba(101,231,215,.65)}
.persona b{display:block;font-size:18px}.persona small{color:var(--muted)}
.bars{display:grid;gap:9px}.bar{display:grid;grid-template-columns:118px 1fr auto;gap:10px;align-items:center;color:var(--muted)}
.bar i{height:8px;border-radius:5px;background:linear-gradient(90deg,var(--green),var(--cyan));display:block}
.pipeline{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:10px}.stage{border:1px solid var(--line);border-radius:8px;padding:14px;background:#0e140f;cursor:pointer}
.stage b{display:block;font-size:30px;color:var(--gold)}.stage span{color:var(--muted)}
.list{display:grid;gap:8px}.row{border:1px solid var(--line);border-radius:7px;padding:10px;background:#0d120e;display:grid;grid-template-columns:1fr auto;gap:12px;align-items:center;cursor:pointer}
.row:hover{border-color:rgba(255,209,102,.55)}.row strong{font-weight:700}.row small{color:var(--muted)}
.archive-tools{display:grid;grid-template-columns:1fr auto;gap:12px;margin-bottom:14px}
.archive-list{display:grid;grid-template-columns:repeat(auto-fit,minmax(240px,1fr));gap:10px}
.archive-item{border:1px solid var(--line);border-radius:8px;background:#0f1510;padding:12px;text-decoration:none}
.archive-item:hover{border-color:var(--cyan)}.archive-item small{color:var(--muted);display:block}
.inspector{position:fixed;right:0;top:0;height:100vh;width:min(460px,100vw);z-index:60;transform:translateX(104%);transition:.24s transform;background:#0b0f0c;border-left:1px solid var(--line);box-shadow:var(--shadow);display:flex;flex-direction:column}
.inspector.open{transform:translateX(0)}.inspector header{padding:18px;border-bottom:1px solid var(--line);display:flex;justify-content:space-between;gap:12px}
.inspector main{padding:18px;overflow:auto}.kv{display:grid;grid-template-columns:132px 1fr;gap:9px;border-bottom:1px solid rgba(42,54,45,.6);padding:8px 0}.kv span:first-child{color:var(--muted)}
.mini-map{position:fixed;right:14px;top:42vh;z-index:25;display:grid;gap:8px}.mini-map a{width:9px;height:34px;border-radius:6px;background:#263029;border:1px solid var(--line)}.mini-map a.active{background:var(--green)}
.hidden{display:none!important}
@media (max-width:900px){
  .hero-metrics,.grid.two,.grid.three,.section-head,.controls{grid-template-columns:1fr}
  .chapter{padding:58px 16px}.topbar{padding:10px 14px}.mini-map{display:none}
  .canvas-box,.canvas-box.tall{height:320px}.hero-inner{padding-top:14vh}
  .bar{grid-template-columns:92px 1fr auto}.pipeline{grid-template-columns:1fr 1fr}
}
@media (max-width:700px){
  .hero-inner{width:min(360px,calc(100% - 28px));margin-left:14px;margin-right:auto}
  .hero-metrics{max-width:360px}.lead{max-width:360px}
}
@media (max-width:520px){
  .hero-metrics,.pipeline{grid-template-columns:1fr}.control-band{top:50px}.metric{padding:13px}
  .brand{font-size:13px}.nav{max-width:calc(100vw - 122px)}
  h1{font-size:44px;line-height:.96}.lead{font-size:17px;line-height:1.48}
}
"""


def layout_html():
    return """
<div class="topbar">
  <div class="brand">RainRust Stats</div>
  <nav class="nav" id="chapter-nav">
    <a href="#pulse" data-section="pulse">Pulse</a>
    <a href="#people" data-section="people">People</a>
    <a href="#code" data-section="code">Code</a>
    <a href="#github" data-section="github">GitHub</a>
    <a href="#unity" data-section="unity">Unity</a>
    <a href="#archive" data-section="archive">Archive</a>
  </nav>
</div>
<section class="hero" id="hero">
  <canvas id="universe-canvas"></canvas>
  <div class="hero-inner">
    <div class="eyebrow">single-file telemetry gallery</div>
    <h1><span>RainRust</span><span>Telemetry</span></h1>
    <p class="lead">把文件、提交、PR、Issue 和 Unity 资源折叠成一座可以探索的项目宇宙。滚动进入章节，点击任何闪光点下钻。</p>
    <div class="hero-metrics" id="hero-metrics"></div>
  </div>
</section>
<div class="control-band">
  <div class="controls">
    <input id="command" class="search" placeholder="Command palette: 搜索作者、文件、PR、Issue、topic..." />
    <div class="chiprow" id="state-chips"></div>
    <button class="ghost" id="reset-btn">Reset</button>
  </div>
</div>
<main>
  <section class="chapter" id="pulse" data-chapter="pulse">
    <div class="wrap">
      <div class="section-head"><div><div class="kicker">Pulse</div><h2>项目生命体征</h2></div><p>时间、提交、PR 与 CI 被压成一条可拖拽的河流。拖拽月度河流会联动其他章节，双击恢复全局。</p></div>
      <div class="grid two">
        <div class="panel tall"><h3>Commit / PR Streamgraph</h3><p class="sub">拖拽选择时间范围；双击重置。</p><div class="canvas-box tall"><canvas id="stream-canvas"></canvas></div><div class="legend"><span><i class="dot" style="background:var(--green)"></i>commits</span><span><i class="dot" style="background:var(--cyan)"></i>insertions</span><span><i class="dot" style="background:var(--red)"></i>deletions</span><span><i class="dot" style="background:var(--gold)"></i>PR merged</span></div></div>
        <div class="panel tall"><h3>Repository Terrain</h3><p class="sub">面积代表 bytes/files，点击地貌区域筛选 domain/category。</p><div class="canvas-box tall"><canvas id="terrain-canvas"></canvas></div></div>
      </div>
    </div>
  </section>
  <section class="chapter alt" id="people" data-chapter="people">
    <div class="wrap">
      <div class="section-head"><div><div class="kicker">People</div><h2>贡献者人格图谱</h2></div><p>雷达、压哨时间线和救火火焰仪表共同描述每个人的开发节奏。点击贡献者后整页进入作者视角。</p></div>
      <div class="grid two">
        <div class="panel"><h3>Contributor Radar</h3><p class="sub">夜间、周末、churn、压哨、救火、领域集中度。</p><div class="canvas-box"><canvas id="radar-canvas"></canvas></div></div>
        <div class="panel"><h3>Flame Meter</h3><p class="sub">点击月份查看当月 fix/revert/hotfix 信号。</p><div class="canvas-box"><canvas id="flame-canvas"></canvas></div></div>
      </div>
      <div class="panel" style="margin-top:16px"><h3>Persona Cards</h3><div class="personas" id="persona-cards"></div></div>
      <div class="panel" style="margin-top:16px"><h3>Countdown Timeline</h3><p class="sub">PR 开始信号到完成的代理口径；24h 内事件会发光。</p><div class="chiprow" id="deadline-tabs"></div><div class="canvas-box short"><canvas id="deadline-canvas"></canvas></div></div>
    </div>
  </section>
  <section class="chapter" id="code" data-chapter="code">
    <div class="wrap">
      <div class="section-head"><div><div class="kicker">Code</div><h2>代码结构与风险地形</h2></div><p>风险矩阵找热点，词频轨道看语言习惯，文件病历卡展示 churn、复杂度和 API 命中。</p></div>
      <div class="grid two">
        <div class="panel tall"><h3>Risk Matrix</h3><p class="sub">x=churn, y=complexity, size=lines, color=domain。点击点位打开文件病历。</p><div class="canvas-box tall"><canvas id="risk-canvas"></canvas></div></div>
        <div class="panel tall"><h3>Radial Word Orbit</h3><p class="sub">词根、代码词、API 三种轨道可切换；点击词会进入搜索。</p><div class="chiprow" id="word-tabs"></div><div class="canvas-box"><canvas id="word-canvas"></canvas></div><div id="naming-style" class="legend"></div></div>
      </div>
      <div class="panel" style="margin-top:16px"><h3>Hot File Fate Line</h3><p class="sub">高风险文件的月度 churn 命运线。</p><div class="canvas-box short"><canvas id="file-line-canvas"></canvas></div></div>
    </div>
  </section>
  <section class="chapter alt" id="github" data-chapter="github">
    <div class="wrap">
      <div class="section-head"><div><div class="kicker">GitHub</div><h2>PR / Issue 生态</h2></div><p>PR 气泡图把 lead time、churn、文件数和 topic 放在一个平面；Issue pipeline 展示积压老化。</p></div>
      <div class="grid two">
        <div class="panel tall"><h3>PR Bubble Plot</h3><p class="sub">点击气泡查看 PR 性格标签、lead time、churn 与是否压哨。</p><div class="canvas-box tall"><canvas id="pr-canvas"></canvas></div></div>
        <div class="panel tall"><h3>Issue Aging Pipeline</h3><p class="sub">点击阶段筛选相关 issue；下面保留精选列表。</p><div class="pipeline" id="issue-pipeline"></div><div class="list" id="issue-list" style="margin-top:14px"></div></div>
      </div>
      <div class="grid two" style="margin-top:16px">
        <div class="panel"><h3>Topic Constellation</h3><div class="canvas-box short"><canvas id="topic-canvas"></canvas></div></div>
        <div class="panel"><h3>PR Shortlist</h3><div class="list" id="pr-list"></div></div>
      </div>
    </div>
  </section>
  <section class="chapter" id="unity" data-chapter="unity">
    <div class="wrap">
      <div class="section-head"><div><div class="kicker">Unity</div><h2>场景、Prefab 与 GUID 星座</h2></div><p>Unity YAML 被抽象为组件密度与引用关系。只展示 Top references，完整表格进入 Archive。</p></div>
      <div class="grid two">
        <div class="panel tall"><h3>Component Constellation</h3><p class="sub">节点代表场景、Prefab 和 GUID 引用摘要；点击节点查看详情。</p><div class="canvas-box tall"><canvas id="unity-canvas"></canvas></div></div>
        <div class="panel tall"><h3>Scene / Prefab Skyline</h3><div id="unity-bars" class="bars"></div></div>
      </div>
    </div>
  </section>
  <section class="chapter alt" id="archive" data-chapter="archive">
    <div class="wrap">
      <div class="section-head"><div><div class="kicker">Archive</div><h2>原始数据收纳区</h2></div><p>主叙事不再铺满表格；所有可复现来源、CSV 与 raw JSON 在这里下载查证。</p></div>
      <div class="panel">
        <div class="archive-tools"><input id="archive-search" class="search" placeholder="筛选 CSV / JSON / Markdown..." /><select id="archive-kind" class="search"><option value="">All kinds</option><option>project</option><option>git</option><option>people</option><option>fun</option><option>code</option><option>github</option><option>unity</option><option>ci</option><option>source</option></select></div>
        <div id="archive-list" class="archive-list"></div>
      </div>
    </div>
  </section>
</main>
<div class="mini-map" id="mini-map"><a href="#pulse"></a><a href="#people"></a><a href="#code"></a><a href="#github"></a><a href="#unity"></a><a href="#archive"></a></div>
<aside class="inspector" id="inspector"><header><div><strong id="inspector-title">Inspector</strong><div id="inspector-sub" class="sub"></div></div><button class="ghost" id="inspector-close">Close</button></header><main id="inspector-body"></main></aside>
"""


def dashboard_js():
    return r"""
const Data = %%PAYLOAD%%;
const fmt = new Intl.NumberFormat('en-US');
const colors = ['#a4ff72','#65e7d7','#ffd166','#ff6b5e','#ff9f43','#d2a8ff','#9df0ff','#f4f1bb'];
const $ = (id) => document.getElementById(id);
const val = (v,d=0) => Number.isFinite(+v) ? +v : d;
const text = (v) => String(v ?? '');
const esc = (v) => text(v).replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#039;'}[c]));
const bytes = (n) => { n=val(n); if(n>1e9)return (n/1e9).toFixed(1)+' GB'; if(n>1e6)return (n/1e6).toFixed(1)+' MB'; if(n>1e3)return (n/1e3).toFixed(1)+' KB'; return fmt.format(n)+' B'; };
const Store = {
  state:{author:'',domain:'',topic:'',query:'',range:null,deadlineWindow:'24h',wordMode:'name',issueStage:''},
  listeners:[],
  set(patch){ Object.assign(this.state, patch); this.hash(); this.listeners.forEach(fn=>fn()); },
  reset(){ this.state={author:'',domain:'',topic:'',query:'',range:null,deadlineWindow:'24h',wordMode:'name',issueStage:''}; this.hash(); this.listeners.forEach(fn=>fn()); },
  on(fn){ this.listeners.push(fn); },
  hash(){ const p=new URLSearchParams(); Object.entries(this.state).forEach(([k,v])=>{ if(v && typeof v !== 'object') p.set(k,v); }); if(this.state.range) p.set('range',this.state.range.join('..')); history.replaceState(null,'','#'+p.toString()); },
  read(){ const p=new URLSearchParams(location.hash.slice(1)); p.forEach((v,k)=>{ if(k==='range') this.state.range=v.split('..'); else this.state[k]=v; }); }
};
Store.read();
function setupCanvas(id){ const c=$(id), ctx=c.getContext('2d'); const r=c.getBoundingClientRect(), d=Math.max(1,devicePixelRatio||1); c.width=Math.max(1,r.width*d); c.height=Math.max(1,r.height*d); ctx.setTransform(d,0,0,d,0,0); return {c,ctx,w:r.width,h:r.height}; }
function colorFor(key){ let h=0; text(key).split('').forEach(ch=>h=(h*31+ch.charCodeAt(0))>>>0); return colors[h%colors.length]; }
function inRange(month){ const r=Store.state.range; return !r || (month>=r[0] && month<=r[1]); }
function rowMatches(row){ const q=Store.state.query.toLowerCase(); const author=Store.state.author, topic=Store.state.topic, domain=Store.state.domain; if(author && !text(row.author || row.canonical_author || row.github_author).includes(author)) return false; if(topic && text(row.topic || row.label || row.status).toLowerCase()!==topic.toLowerCase()) return false; if(domain && !text(row.domain || row.name || row.category).toLowerCase().includes(domain.toLowerCase())) return false; if(q && !JSON.stringify(row).toLowerCase().includes(q)) return false; return true; }
function openInspector(title, sub, row){ $('inspector-title').textContent=title; $('inspector-sub').textContent=sub||''; const entries=Object.entries(row||{}).filter(([,v])=>v!==''&&v!=null).slice(0,34); $('inspector-body').innerHTML=entries.map(([k,v])=>`<div class="kv"><span>${esc(k)}</span><span>${esc(Array.isArray(v)?v.join(', '):v)}</span></div>`).join('') || '<p class="sub">No detail rows.</p>'; $('inspector').classList.add('open'); }
$('inspector-close').onclick=()=>$('inspector').classList.remove('open');
function renderHero(){ const s=Data.summary, gh=Data.githubSummary; const risk=Data.risk[0]?.risk_score||0; $('hero-metrics').innerHTML=[
  ['文件数',fmt.format(s.tracked_files_seen||0),'first-party '+fmt.format(s.first_party_files||0)],
  ['提交数',fmt.format(s.commit_count||0),(s.first_commit||'')+' -> '+(s.latest_commit||'')],
  ['PR 数',fmt.format(gh.prs||0),gh.source||'github'],
  ['风险热点',fmt.format(Math.round(risk)),'Top file risk score']
].map(m=>`<div class="metric"><b>${m[1]}</b><span>${m[0]} · ${esc(m[2])}</span></div>`).join(''); }
let universePts=[];
function drawUniverse(){ const {ctx,w,h}=setupCanvas('universe-canvas'); if(!universePts.length){ const total=Math.min(260,(Data.summary.first_party_files||80)/2 + (Data.summary.commit_count||50)/3 + (Data.githubSummary.prs||10)); for(let i=0;i<total;i++) universePts.push({x:Math.random(),y:Math.random(),r:1+Math.random()*3,v:.15+Math.random()*.45,t:i%4}); } let tick=0; function frame(){ tick+=.01; ctx.clearRect(0,0,w,h); ctx.fillStyle='#070907'; ctx.fillRect(0,0,w,h); ctx.strokeStyle='rgba(164,255,114,.08)'; ctx.lineWidth=1; for(let x=0;x<w;x+=48){ctx.beginPath();ctx.moveTo(x,0);ctx.lineTo(x,h);ctx.stroke()} for(let y=0;y<h;y+=48){ctx.beginPath();ctx.moveTo(0,y);ctx.lineTo(w,y);ctx.stroke()} universePts.forEach((p,i)=>{ const x=(p.x*w+Math.sin(tick*p.v+i)*10+w)%w, y=(p.y*h+Math.cos(tick*p.v+i*.7)*8+h)%h; ctx.fillStyle=colors[p.t]; ctx.globalAlpha=.38+.5*Math.sin(tick+i); ctx.beginPath(); ctx.arc(x,y,p.r,0,Math.PI*2); ctx.fill(); }); ctx.globalAlpha=1; requestAnimationFrame(frame); } frame(); }
function drawStream(){ const {c,ctx,w,h}=setupCanvas('stream-canvas'); const months=Data.months||[], prs=Object.fromEntries((Data.prsByMonth||[]).map(r=>[r.month,r])); const pad=36, innerW=w-pad*2, innerH=h-pad*2; ctx.clearRect(0,0,w,h); if(!months.length)return; const rows=months.map(m=>({month:m.month, commits:val(m.commits), insertions:Math.sqrt(val(m.insertions)), deletions:Math.sqrt(val(m.deletions)), prs:val(prs[m.month]?.merged_prs || prs[m.month]?.prs)})); const max=Math.max(...rows.map(r=>r.commits+r.insertions+r.deletions+r.prs),1); const series=['commits','insertions','deletions','prs']; const pts=[]; rows.forEach((r,i)=>{ const x=pad+(i/(rows.length-1||1))*innerW; let base=h-pad; series.forEach((k,si)=>{ const y=base-(r[k]/max)*innerH*.9; pts.push({x,y,base,month:r.month,k,row:r}); ctx.fillStyle=colors[si]; ctx.globalAlpha=.62; ctx.fillRect(x-8,y,16,base-y); base=y; }); ctx.globalAlpha=1; ctx.fillStyle=Store.state.range&&inRange(r.month)?'#a4ff72':'#62705f'; ctx.fillText(r.month.slice(5)||r.month,x-12,h-10); }); ctx.fillStyle='#9ead9f'; ctx.fillText('drag to select months, double click to reset',pad,18); c.onmousedown=e=>{ c.dataset.dragStart=nearestMonth(e,rows,pad,innerW); }; c.onmouseup=e=>{ if(!c.dataset.dragStart)return; const a=c.dataset.dragStart,b=nearestMonth(e,rows,pad,innerW); delete c.dataset.dragStart; const range=[a,b].sort(); Store.set({range}); }; c.ondblclick=()=>Store.set({range:null}); }
function nearestMonth(e,rows,pad,innerW){ const r=e.currentTarget.getBoundingClientRect(); const x=e.clientX-r.left; const idx=Math.max(0,Math.min(rows.length-1,Math.round(((x-pad)/innerW)*(rows.length-1)))); return rows[idx].month; }
function drawTerrain(){ const {c,ctx,w,h}=setupCanvas('terrain-canvas'); ctx.clearRect(0,0,w,h); const rows=(Data.composition||[]).filter(rowMatches); const total=rows.reduce((a,r)=>a+val(r.bytes,r.files),0)||1; let x=0,y=0,horizontal=true; const rects=[]; rows.forEach((r,i)=>{ const area=(val(r.bytes,r.files)/total)*w*h; let rw,rh; if(horizontal){ rh=h/(Math.ceil(rows.length/3)); rw=Math.max(38,area/rh); if(x+rw>w){x=0;y+=rh;} } else { rw=w/(Math.ceil(rows.length/3)); rh=Math.max(38,area/rw); if(y+rh>h){y=0;x+=rw;} } rects.push({x,y,w:Math.min(rw,w-x),h:Math.min(rh,h-y),row:r}); x+=rw; horizontal=!horizontal; }); rects.forEach((r,i)=>{ ctx.fillStyle=colorFor(r.row.name); ctx.globalAlpha=.72; ctx.fillRect(r.x+2,r.y+2,r.w-4,r.h-4); ctx.globalAlpha=1; ctx.fillStyle='#071008'; ctx.font='12px sans-serif'; ctx.fillText(r.row.name,r.x+8,r.y+20); ctx.fillText(bytes(r.row.bytes),r.x+8,r.y+38); }); c.onclick=e=>{ const b=c.getBoundingClientRect(),x=e.clientX-b.left,y=e.clientY-b.top; const hit=rects.find(r=>x>=r.x&&x<=r.x+r.w&&y>=r.y&&y<=r.y+r.h); if(hit){ Store.set({domain:hit.row.name}); openInspector('Repository Terrain',hit.row.name,hit.row); }}; }
function contributorRows(){
  const rhythm=Object.fromEntries((Data.rhythm||[]).map(r=>[r.author,r]));
  const sprint=Object.fromEntries((Data.deadlineStats||[]).map(r=>[r.author,r]));
  const fire=Object.fromEntries((Data.firefighting||[]).map(r=>[r.author,r]));
  return (Data.contributors||[]).map(r=>{
    const author=r.canonical_author||r.author;
    const merged={...r,...(rhythm[author]||{}),...(sprint[author]||{}),...(fire[author]||{}),canonical_author:author};
    merged.domain_concentration_pct = merged.churn ? Math.round(val(merged.top_domain_churn)/Math.max(1,val(merged.churn))*100) : 0;
    return merged;
  }).filter(rowMatches).slice(0,12);
}
function drawRadar(){ const {ctx,w,h}=setupCanvas('radar-canvas'); ctx.clearRect(0,0,w,h); const rows=contributorRows().slice(0,4); const metrics=['night_pct','weekend_pct','avg_churn_per_commit','sprint_24h','fire_score','domain_concentration_pct']; const labels=['night','weekend','churn','sprint','fire','focus']; const cx=w/2,cy=h/2+8,R=Math.min(w,h)*.34; ctx.strokeStyle='rgba(158,173,159,.35)'; ctx.fillStyle='#9ead9f'; for(let ring=1;ring<=4;ring++){ ctx.beginPath(); for(let i=0;i<metrics.length;i++){ const a=-Math.PI/2+i*Math.PI*2/metrics.length, rr=R*ring/4; const x=cx+Math.cos(a)*rr,y=cy+Math.sin(a)*rr; i?ctx.lineTo(x,y):ctx.moveTo(x,y); } ctx.closePath(); ctx.stroke(); } labels.forEach((l,i)=>{ const a=-Math.PI/2+i*Math.PI*2/labels.length; ctx.fillText(l,cx+Math.cos(a)*(R+18)-16,cy+Math.sin(a)*(R+18)); }); rows.forEach((r,ri)=>{ ctx.beginPath(); metrics.forEach((m,i)=>{ let v=val(r[m]); if(m==='avg_churn_per_commit') v=Math.min(100,v/20); if(m==='sprint_24h'||m==='fire_score') v=Math.min(100,v*8); const a=-Math.PI/2+i*Math.PI*2/metrics.length, rr=R*Math.max(0,Math.min(100,v))/100; const x=cx+Math.cos(a)*rr,y=cy+Math.sin(a)*rr; i?ctx.lineTo(x,y):ctx.moveTo(x,y); }); ctx.closePath(); ctx.fillStyle=colors[ri]; ctx.globalAlpha=.18; ctx.fill(); ctx.globalAlpha=1; ctx.strokeStyle=colors[ri]; ctx.stroke(); ctx.fillText(r.canonical_author||r.author,16,22+ri*18); }); }
function renderPersonas(){ $('persona-cards').innerHTML=contributorRows().slice(0,8).map((r,i)=>`<button class="persona" data-author="${esc(r.canonical_author)}"><b>${esc(r.canonical_author)}</b><small>${esc(r.persona||r.preference||'contributor')} · ${fmt.format(val(r.commits))} commits · ${fmt.format(val(r.churn))} churn</small></button>`).join(''); document.querySelectorAll('.persona').forEach(el=>el.onclick=()=>{ const author=el.dataset.author; Store.set({author}); openInspector('Contributor Inspector',author,contributorRows().find(r=>r.canonical_author===author)||{}); }); }
function drawDeadline(){ const {c,ctx,w,h}=setupCanvas('deadline-canvas'); ctx.clearRect(0,0,w,h); const window=Store.state.deadlineWindow; const maxH=window==='24h'?24:window==='72h'?72:168; const rows=(Data.deadlineEvents||[]).filter(rowMatches).filter(r=>window==='all'||val(r.hours_before_deadline)<=maxH).slice(0,100); ctx.fillStyle='#9ead9f'; ctx.fillText('0h',24,h-18); ctx.fillText(maxH+'h',w-56,h-18); ctx.strokeStyle='rgba(164,255,114,.24)'; ctx.beginPath();ctx.moveTo(34,h-34);ctx.lineTo(w-34,h-34);ctx.stroke(); const pts=[]; rows.forEach((r,i)=>{ const hours=Math.min(maxH,val(r.hours_before_deadline)); const x=34+(1-hours/maxH)*(w-68), y=28+(i%7)*24+Math.floor(i/7)%3*7; const glow=hours<=24; ctx.fillStyle=glow?'#a4ff72':colorFor(r.author); ctx.shadowColor=glow?'#a4ff72':'transparent'; ctx.shadowBlur=glow?12:0; ctx.beginPath();ctx.arc(x,y,4+Math.max(0,24-hours)/14,0,Math.PI*2);ctx.fill(); pts.push({x,y,row:r}); }); ctx.shadowBlur=0; c.onclick=e=>{ const b=c.getBoundingClientRect(),x=e.clientX-b.left,y=e.clientY-b.top; const hit=pts.find(p=>Math.hypot(p.x-x,p.y-y)<10); if(hit) openInspector('PR Inspector','压哨事件 · PR 开始信号到完成',hit.row); }; }
function renderDeadlineTabs(){ $('deadline-tabs').innerHTML=['24h','72h','all'].map(v=>`<button class="chip ${Store.state.deadlineWindow===v?'active':''}" data-v="${v}">${v}</button>`).join(''); $('deadline-tabs').querySelectorAll('button').forEach(b=>b.onclick=()=>Store.set({deadlineWindow:b.dataset.v})); }
function drawFlame(){ const {c,ctx,w,h}=setupCanvas('flame-canvas'); ctx.clearRect(0,0,w,h); const rows=(Data.firefighting||[]).filter(rowMatches); const top=rows[0]||{}; const score=val(top.fire_score); const cx=w/2,cy=h*.48,R=Math.min(w,h)*.32; ctx.lineWidth=18; ctx.strokeStyle='rgba(255,107,94,.18)'; ctx.beginPath();ctx.arc(cx,cy,R,Math.PI*.85,Math.PI*2.15);ctx.stroke(); ctx.strokeStyle=score>20?'#ff6b5e':'#ffd166'; ctx.beginPath();ctx.arc(cx,cy,R,Math.PI*.85,Math.PI*.85+Math.PI*1.3*Math.min(1,score/60));ctx.stroke(); ctx.fillStyle='#f3f7ee'; ctx.font='34px sans-serif'; ctx.textAlign='center'; ctx.fillText(fmt.format(Math.round(score)),cx,cy+8); ctx.font='13px sans-serif'; ctx.fillStyle='#9ead9f'; ctx.fillText(top.author||top.canonical_author||'fire leader',cx,cy+32); ctx.textAlign='left'; const months=(Data.fireMonths||[]).slice(-18), max=Math.max(...months.map(r=>val(r.fire_commits)),1); ctx.beginPath(); months.forEach((r,i)=>{ const x=24+i*(w-48)/(months.length-1||1), y=h-32-val(r.fire_commits)/max*70; i?ctx.lineTo(x,y):ctx.moveTo(x,y); }); ctx.strokeStyle='#ff9f43'; ctx.lineWidth=2; ctx.stroke(); c.onclick=e=>{ const b=c.getBoundingClientRect(),x=e.clientX-b.left; const idx=Math.max(0,Math.min(months.length-1,Math.round((x-24)/(w-48)*(months.length-1)))); openInspector('Fire Month',months[idx]?.month||'',months[idx]||{}); }; }
function drawRisk(){ const {c,ctx,w,h}=setupCanvas('risk-canvas'); ctx.clearRect(0,0,w,h); const rows=(Data.risk||[]).filter(rowMatches); const maxX=Math.max(...rows.map(r=>val(r.churn)),1), maxY=Math.max(...rows.map(r=>val(r.complexity)),1), maxL=Math.max(...rows.map(r=>val(r.lines)),1); const pad=42, pts=[]; ctx.strokeStyle='rgba(158,173,159,.3)'; ctx.beginPath();ctx.moveTo(pad,h-pad);ctx.lineTo(w-pad,h-pad);ctx.lineTo(w-pad,pad);ctx.stroke(); ctx.fillStyle='#9ead9f'; ctx.fillText('churn',w-78,h-16); ctx.fillText('complexity',12,pad-12); rows.forEach(r=>{ const x=pad+val(r.churn)/maxX*(w-pad*2), y=h-pad-val(r.complexity)/maxY*(h-pad*2), rad=4+Math.sqrt(val(r.lines)/maxL)*18; ctx.fillStyle=colorFor(r.domain||r.path); ctx.globalAlpha=.72; ctx.beginPath();ctx.arc(x,y,rad,0,Math.PI*2);ctx.fill(); pts.push({x,y,rad,row:r}); }); ctx.globalAlpha=1; c.onclick=e=>{ const b=c.getBoundingClientRect(),x=e.clientX-b.left,y=e.clientY-b.top; const hit=pts.find(p=>Math.hypot(p.x-x,p.y-y)<p.rad+4); if(hit) openInspector('File Inspector','risk matrix · '+hit.row.path,hit.row); }; }
function drawWords(){ const {c,ctx,w,h}=setupCanvas('word-canvas'); ctx.clearRect(0,0,w,h); const mode=Store.state.wordMode; const rows=(mode==='code'?Data.codeWords:mode==='api'?Data.apiUsage:Data.nameWords)||[]; const weight=r=>val(r.count||r.hits||r.files||r.usages||r.symbols); const label=r=>r.word||r.api||r.name||r.style; const max=Math.max(...rows.map(weight),1); const cx=w/2,cy=h/2, pts=[]; rows.slice(0,42).forEach((r,i)=>{ const freq=weight(r); const rr=18+Math.sqrt(i/42)*Math.min(w,h)*.42, a=i*2.399; const x=cx+Math.cos(a)*rr, y=cy+Math.sin(a)*rr; const word=label(r); ctx.fillStyle=colorFor(word); ctx.font=`${11+Math.sqrt(freq/max)*22}px sans-serif`; ctx.fillText(word,x,y); pts.push({x,y,row:r,word}); }); c.onclick=e=>{ const b=c.getBoundingClientRect(),x=e.clientX-b.left,y=e.clientY-b.top; const hit=pts.map(p=>({...p,d:Math.hypot(p.x-x,p.y-y)})).sort((a,b)=>a.d-b.d)[0]; if(hit){ Store.set({query:hit.word}); $('command').value=hit.word; openInspector('Word Orbit',hit.word,hit.row); } }; }
function renderWordTabs(){ $('word-tabs').innerHTML=[['name','命名词根'],['code','代码词'],['api','API 词']].map(([v,l])=>`<button class="chip ${Store.state.wordMode===v?'active':''}" data-v="${v}">${l}</button>`).join(''); $('word-tabs').querySelectorAll('button').forEach(b=>b.onclick=()=>Store.set({wordMode:b.dataset.v})); $('naming-style').innerHTML=(Data.namingStyle||[]).slice(0,8).map(r=>`<span><i class="dot" style="background:${colorFor(r.style)}"></i>${esc(r.style)} ${fmt.format(val(r.count))}</span>`).join(''); }
function drawFileLine(){ const {ctx,w,h}=setupCanvas('file-line-canvas'); ctx.clearRect(0,0,w,h); const rows=(Data.fileChurnTimeline||[]).filter(rowMatches), groups={}; rows.forEach(r=>{ (groups[r.path] ||= []).push(r); }); const paths=Object.keys(groups).slice(0,8); const months=[...new Set(rows.map(r=>r.month))].sort(); const max=Math.max(...rows.map(r=>val(r.churn)),1); paths.forEach((p,pi)=>{ ctx.beginPath(); months.forEach((m,i)=>{ const r=(groups[p]||[]).find(x=>x.month===m)||{}; const x=32+i*(w-64)/(months.length-1||1), y=h-28-val(r.churn)/max*(h-58); i?ctx.lineTo(x,y):ctx.moveTo(x,y); }); ctx.strokeStyle=colors[pi%colors.length]; ctx.lineWidth=2; ctx.stroke(); }); }
function drawPR(){ const {c,ctx,w,h}=setupCanvas('pr-canvas'); ctx.clearRect(0,0,w,h); const personality=Object.fromEntries((Data.prPersonality||[]).map(r=>[r.number,r])); const rows=(Data.prs||[]).filter(rowMatches); const maxX=Math.max(...rows.map(r=>val(r.lead_time_hours||r.duration_hours)),1), maxY=Math.max(...rows.map(r=>val(r.additions)+val(r.deletions)),1), maxF=Math.max(...rows.map(r=>val(r.changed_files)),1); const pts=[],pad=42; ctx.strokeStyle='rgba(158,173,159,.28)';ctx.beginPath();ctx.moveTo(pad,h-pad);ctx.lineTo(w-pad,h-pad);ctx.lineTo(w-pad,pad);ctx.stroke(); ctx.fillStyle='#9ead9f';ctx.fillText('lead time',w-92,h-15);ctx.fillText('churn',14,pad-12); rows.forEach(r=>{ const x=pad+val(r.lead_time_hours||r.duration_hours)/maxX*(w-pad*2), y=h-pad-(val(r.additions)+val(r.deletions))/maxY*(h-pad*2), rad=5+Math.sqrt(val(r.changed_files)/maxF)*18; ctx.fillStyle=colorFor(r.topic||r.title); ctx.globalAlpha=.72; ctx.beginPath();ctx.arc(x,y,rad,0,Math.PI*2);ctx.fill(); pts.push({x,y,rad,row:{...r,...(personality[r.number]||{})}}); }); ctx.globalAlpha=1; c.onclick=e=>{ const b=c.getBoundingClientRect(),x=e.clientX-b.left,y=e.clientY-b.top; const hit=pts.find(p=>Math.hypot(p.x-x,p.y-y)<p.rad+4); if(hit) openInspector('PR Inspector','#'+(hit.row.number||''),hit.row); }; }
function renderIssues(){ const counts={open:0,stale:0,overdue:0,closed:0}; (Data.issues||[]).forEach(i=>{ if(text(i.state).toLowerCase()==='closed') counts.closed++; else if((Data.overdueIssues||[]).some(o=>o.number===i.number)) counts.overdue++; else if(val(i.age_days)>30) counts.stale++; else counts.open++; }); $('issue-pipeline').innerHTML=Object.entries(counts).map(([k,v])=>`<button class="stage" data-stage="${k}"><b>${fmt.format(v)}</b><span>${k}</span></button>`).join(''); $('issue-pipeline').querySelectorAll('button').forEach(b=>b.onclick=()=>Store.set({issueStage:b.dataset.stage,topic:b.dataset.stage==='overdue'?'':Store.state.topic})); const rows=((Store.state.issueStage==='overdue'?Data.overdueIssues:Data.issues)||[]).filter(rowMatches).slice(0,8); $('issue-list').innerHTML=rows.map(r=>`<div class="row" data-n="${r.number}"><div><strong>#${esc(r.number)} ${esc(r.title).slice(0,82)}</strong><br><small>${esc(r.topic||r.overdue_reason||r.state||'issue')}</small></div><small>${fmt.format(val(r.age_days||r.open_days))}d</small></div>`).join(''); $('issue-list').querySelectorAll('.row').forEach((el,i)=>el.onclick=()=>openInspector('Issue Inspector','#'+(rows[i].number||''),rows[i])); }
function renderPRList(){ const rows=(Data.prs||[]).filter(rowMatches).slice(0,8); $('pr-list').innerHTML=rows.map(r=>`<div class="row"><div><strong>#${esc(r.number)} ${esc(r.title).slice(0,82)}</strong><br><small>${esc(r.topic||r.state||'pr')} · ${esc(r.author||'')}</small></div><small>${fmt.format(val(r.changed_files))} files</small></div>`).join(''); $('pr-list').querySelectorAll('.row').forEach((el,i)=>el.onclick=()=>openInspector('PR Inspector','#'+(rows[i].number||''),rows[i])); }
function drawTopics(){ const {ctx,w,h}=setupCanvas('topic-canvas'); ctx.clearRect(0,0,w,h); const rows=[...(Data.prTopics||[]).map(r=>({...r,count:r.prs,type:'PR'})),...(Data.issueTopics||[]).map(r=>({...r,count:r.issues,type:'Issue'}))].slice(0,32); const max=Math.max(...rows.map(r=>val(r.count)),1), cx=w/2,cy=h/2; rows.forEach((r,i)=>{ const a=i*2.399, rr=18+Math.sqrt(i/rows.length)*Math.min(w,h)*.38, x=cx+Math.cos(a)*rr,y=cy+Math.sin(a)*rr, rad=5+val(r.count)/max*22; ctx.fillStyle=colorFor(r.topic); ctx.globalAlpha=.74; ctx.beginPath();ctx.arc(x,y,rad,0,Math.PI*2);ctx.fill(); ctx.globalAlpha=1; ctx.fillStyle='#f3f7ee'; ctx.fillText(r.topic,x+rad+3,y+4); }); }
function drawUnity(){ const {c,ctx,w,h}=setupCanvas('unity-canvas'); ctx.clearRect(0,0,w,h); const nodes=[...(Data.scenes||[]).slice(0,10).map(r=>({...r,type:'scene',label:r.path||r.name,weight:r.game_objects||r.components})),...(Data.prefabs||[]).slice(0,12).map(r=>({...r,type:'prefab',label:r.path||r.name,weight:r.components||r.game_objects})),...(Data.guidRefs||[]).slice(0,14).map(r=>({...r,type:'guid',label:r.guid||r.path,weight:r.references}))]; const max=Math.max(...nodes.map(n=>val(n.weight)),1), pts=[]; nodes.forEach((n,i)=>{ const a=i*2.399, rr=24+Math.sqrt(i/nodes.length)*Math.min(w,h)*.42, x=w/2+Math.cos(a)*rr,y=h/2+Math.sin(a)*rr; pts.push({x,y,n}); }); ctx.strokeStyle='rgba(101,231,215,.16)'; pts.forEach((p,i)=>{ for(let j=i+1;j<Math.min(pts.length,i+4);j++){ ctx.beginPath();ctx.moveTo(p.x,p.y);ctx.lineTo(pts[j].x,pts[j].y);ctx.stroke(); }}); pts.forEach(p=>{ const rad=5+Math.sqrt(val(p.n.weight)/max)*24; ctx.fillStyle=p.n.type==='scene'?'#a4ff72':p.n.type==='prefab'?'#65e7d7':'#ffd166'; ctx.beginPath();ctx.arc(p.x,p.y,rad,0,Math.PI*2);ctx.fill(); ctx.fillStyle='#f3f7ee'; ctx.fillText(text(p.n.label).split('/').pop().slice(0,18),p.x+rad+4,p.y+4); }); c.onclick=e=>{ const b=c.getBoundingClientRect(),x=e.clientX-b.left,y=e.clientY-b.top; const hit=pts.find(p=>Math.hypot(p.x-x,p.y-y)<34); if(hit) openInspector('Unity Inspector',hit.n.type,hit.n); }; }
function renderUnityBars(){ const rows=[...(Data.scenes||[]).slice(0,8),...(Data.prefabs||[]).slice(0,8)]; const max=Math.max(...rows.map(r=>val(r.components||r.game_objects||r.references)),1); $('unity-bars').innerHTML=rows.map(r=>{ const v=val(r.components||r.game_objects||r.references); return `<div class="bar"><span title="${esc(r.path||r.name)}">${esc(text(r.path||r.name).split('/').pop()).slice(0,18)}</span><i style="width:${Math.max(3,v/max*100)}%;background:${colorFor(r.path||r.name)}"></i><em>${fmt.format(v)}</em></div>`; }).join(''); }
function renderArchive(){ const q=$('archive-search').value.toLowerCase(), k=$('archive-kind').value; const rows=(Data.archive||[]).filter(r=>(!k||r.kind===k)&&(!q||JSON.stringify(r).toLowerCase().includes(q))); $('archive-list').innerHTML=rows.map(r=>`<a class="archive-item" href="${esc(r.file)}" download><strong>${esc(r.label)}</strong><small>${esc(r.kind)} · ${esc(r.file)}</small></a>`).join(''); }
function renderState(){ const s=Store.state; $('state-chips').innerHTML=[s.author&&['author',s.author],s.domain&&['domain',s.domain],s.topic&&['topic',s.topic],s.range&&['range',s.range.join('..')],s.query&&['query',s.query]].filter(Boolean).map(([k,v])=>`<button class="chip active" data-k="${k}">${esc(k)}: ${esc(v)}</button>`).join(''); $('state-chips').querySelectorAll('button').forEach(b=>b.onclick=()=>{ const k=b.dataset.k; Store.set({[k]:k==='range'?null:''}); }); }
function renderAll(){ renderState(); renderHero(); drawStream(); drawTerrain(); drawRadar(); renderPersonas(); renderDeadlineTabs(); drawDeadline(); drawFlame(); drawRisk(); renderWordTabs(); drawWords(); drawFileLine(); drawPR(); renderIssues(); renderPRList(); drawTopics(); drawUnity(); renderUnityBars(); renderArchive(); }
$('command').value=Store.state.query; $('command').addEventListener('input',e=>Store.set({query:e.target.value})); $('reset-btn').onclick=()=>{ $('command').value=''; Store.reset(); }; $('archive-search').oninput=renderArchive; $('archive-kind').onchange=renderArchive;
const sections=[...document.querySelectorAll('[data-chapter]')]; const nav=[...document.querySelectorAll('#chapter-nav a')], mini=[...document.querySelectorAll('#mini-map a')];
new IntersectionObserver(entries=>{ entries.forEach(en=>{ if(en.isIntersecting){ const id=en.target.id; nav.forEach(a=>a.classList.toggle('active',a.getAttribute('href')==='#'+id)); mini.forEach(a=>a.classList.toggle('active',a.getAttribute('href')==='#'+id)); }}); },{threshold:.34}).observe(sections[0]); sections.slice(1).forEach(s=>document.querySelector('[data-chapter="'+s.dataset.chapter+'"]')&&new IntersectionObserver(entries=>{entries.forEach(en=>{if(en.isIntersecting){const id=en.target.id;nav.forEach(a=>a.classList.toggle('active',a.getAttribute('href')==='#'+id));mini.forEach(a=>a.classList.toggle('active',a.getAttribute('href')==='#'+id));}})},{threshold:.34}).observe(s));
Store.on(renderAll); addEventListener('resize',()=>requestAnimationFrame(renderAll)); renderAll(); drawUniverse();
"""


def make_story_dashboard_html(data, unity, symbols):
    payload = json.dumps(payload_for_dashboard(data, unity, symbols), ensure_ascii=False)
    html = """<!doctype html>
<html lang="zh-CN">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1" />
<title>RainRust Stats Telemetry</title>
<style>%%CSS%%</style>
</head>
<body>
%%LAYOUT%%
<script>
%%JS%%
</script>
</body>
</html>
"""
    return (
        html.replace("%%CSS%%", style_css())
        .replace("%%LAYOUT%%", layout_html())
        .replace("%%JS%%", dashboard_js().replace("%%PAYLOAD%%", payload))
    )


def make_dashboard_html(data, unity, symbols):
    return make_story_dashboard_html(data, unity, symbols)


def make_dashboard_html_legacy(data, unity, symbols):
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
        "contributor_profiles": data["contributor_profiles"][:20],
        "deadline_sprint_stats": data["fun_stats"]["deadline_sprint_stats"][:20],
        "deadline_sprint_events": data["fun_stats"]["deadline_sprint_events"][:40],
        "contributor_rhythm_profiles": data["fun_stats"]["contributor_rhythm_profiles"][:20],
        "firefighting_index": data["fun_stats"]["firefighting_index"][:20],
        "firefighting_by_month": data["fun_stats"]["firefighting_by_month"],
        "pr_personality": data["fun_stats"]["pr_personality"][:40],
        "author_domain_churn": data["author_domain_churn"][:80],
        "author_extension_churn": data["author_extension_churn"][:80],
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
        "code_word_frequency": data["code"]["code_word_frequency"][:40],
        "code_name_word_frequency": data["code"]["code_name_word_frequency"][:40],
        "code_naming_style": data["code"]["code_naming_style"],
        "api_usage_summary": data["code"]["api_usage_summary"][:16],
        "risk_hotspots": data["risk_hotspots"][:20],
        "file_churn_timeline": data["file_churn_timeline"][:240],
        "github_summary": data["github"]["summary"],
        "github_prs": data["github"]["pull_requests"][:20],
        "github_prs_by_month": data["github"]["prs_by_month"],
        "github_pr_topics": data["github"]["pr_topics"],
        "github_issue_topics": data["github"]["issue_topics"],
        "github_issue_labels": data["github"]["issue_labels"][:20],
        "github_issue_aging": data["github"]["issue_aging"],
        "github_overdue_issues": data["github"]["overdue_issues"][:30],
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
button, select, input {{
  appearance: none;
  border: 1px solid var(--line);
  background: var(--panel);
  color: var(--text);
  border-radius: 8px;
  padding: 9px 12px;
}}
button, select {{
  cursor: pointer;
}}
input {{ min-width: min(320px, 100%); }}
button.active {{ background: var(--green); border-color: var(--green); color: #102012; font-weight: 700; }}
.tabs {{ display: flex; flex-wrap: wrap; gap: 8px; margin-top: 14px; }}
.view-tab {{ padding: 7px 10px; font-size: 13px; }}
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
.bar-row, .day, tbody tr, .card {{ cursor: pointer; }}
.bar-label {{ overflow: hidden; text-overflow: ellipsis; white-space: nowrap; color: #dfe8df; }}
.track {{ height: 12px; background: #101315; border-radius: 999px; overflow: hidden; border: 1px solid rgba(255,255,255,.05); }}
.fill {{ height: 100%; background: linear-gradient(90deg, var(--green), var(--cyan)); border-radius: inherit; }}
.value {{ color: var(--muted); font-variant-numeric: tabular-nums; }}
canvas {{ width: 100%; height: 230px; display: block; }}
table {{ width: 100%; border-collapse: collapse; font-size: 13px; }}
th, td {{ border-bottom: 1px solid rgba(255,255,255,.07); padding: 8px 6px; text-align: left; vertical-align: top; }}
th {{ color: var(--muted); font-weight: 700; }}
th.sortable {{ cursor: pointer; }}
td.num {{ text-align: right; font-variant-numeric: tabular-nums; }}
.table-tools {{ display: flex; justify-content: flex-end; gap: 8px; margin: 0 0 8px; }}
.mini {{ padding: 5px 8px; font-size: 12px; border-radius: 6px; }}
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
.is-hidden {{ display: none !important; }}
.drawer {{
  position: fixed; top: 0; right: 0; width: min(520px, 94vw); height: 100vh;
  background: #15191c; border-left: 1px solid rgba(255,255,255,.12);
  box-shadow: -18px 0 50px rgba(0,0,0,.35); padding: 18px; overflow: auto; z-index: 20;
  transform: translateX(105%); transition: transform .18s ease;
}}
.drawer.open {{ transform: translateX(0); }}
.drawer-head {{ display: flex; justify-content: space-between; align-items: center; gap: 12px; margin-bottom: 12px; }}
.drawer pre {{ white-space: pre-wrap; word-break: break-word; color: var(--muted); font-size: 12px; }}
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
    <input id="global-filter" type="search" placeholder="筛选图表和表格">
  </div>
  <div class="tabs" id="view-tabs">
    <button class="view-tab active" data-view="all">全部</button>
    <button class="view-tab" data-view="overview">Overview</button>
    <button class="view-tab" data-view="people">People</button>
    <button class="view-tab" data-view="fun">趣味统计</button>
    <button class="view-tab" data-view="code">Code</button>
    <button class="view-tab" data-view="github">GitHub</button>
    <button class="view-tab" data-view="unity">Unity</button>
  </div>
</header>
<main>
  <section class="grid cards" id="cards" data-view="overview"></section>
  <section class="grid two" data-view="overview">
    <div class="panel"><h2>项目组成</h2><div id="category-bars" class="bars"></div></div>
    <div class="panel"><h2>月度提交与代码流量</h2><canvas id="month-chart" width="900" height="300"></canvas></div>
  </section>
  <section class="grid three" data-view="people">
    <div class="panel"><h2>贡献者</h2><div id="contrib-bars" class="bars"></div></div>
    <div class="panel"><h2>一周节奏</h2><canvas id="weekday-chart" width="600" height="300"></canvas></div>
    <div class="panel"><h2>一天内的提交峰值</h2><canvas id="hour-chart" width="600" height="300"></canvas></div>
  </section>
  <section class="grid two" data-view="people">
    <div class="panel"><h2>贡献者偏好画像</h2><table id="contrib-profile-table"></table></div>
    <div class="panel"><h2>贡献扩展名偏好</h2><table id="contrib-ext-table"></table></div>
  </section>
  <section class="grid three" data-view="fun">
    <div class="panel"><h2>压哨王</h2><div id="deadline-bars" class="bars"></div></div>
    <div class="panel"><h2>开发节奏人格</h2><table id="rhythm-table"></table></div>
    <div class="panel"><h2>救火指数</h2><div id="fire-bars" class="bars"></div></div>
  </section>
  <section class="grid two" data-view="fun">
    <div class="panel"><h2>压哨事件</h2><table id="deadline-event-table"></table></div>
    <div class="panel"><h2>PR 性格图谱</h2><table id="pr-personality-table"></table></div>
  </section>
  <section class="grid two" data-view="overview">
    <div class="panel"><h2>提交热力</h2><div id="heatmap" class="heatmap"></div><div class="footer">颜色越亮代表当天提交越多。</div></div>
    <div class="panel"><h2>提交主题关键词</h2><div id="keyword-bars" class="bars"></div></div>
  </section>
  <section class="grid two" data-view="overview">
    <div class="panel"><h2>最高变更文件</h2><table id="churn-table"></table></div>
    <div class="panel"><h2>最大文件</h2><table id="large-table"></table></div>
  </section>
  <section class="grid two" data-view="unity">
    <div class="panel"><h2>Unity 项目线索</h2><div id="unity"></div></div>
    <div class="panel"><h2>最近提交</h2><div id="latest"></div></div>
  </section>
  <section class="grid three" data-view="github">
    <div class="panel"><h2>GitHub / PR 画像</h2><div id="github"></div></div>
    <div class="panel"><h2>代码内容雷达</h2><div id="code-summary"></div></div>
    <div class="panel"><h2>规范健康</h2><div id="quality-summary"></div></div>
  </section>
  <section class="grid three" data-view="github">
    <div class="panel"><h2>Issue 主题</h2><div id="issue-topic-bars" class="bars"></div></div>
    <div class="panel"><h2>PR 主题</h2><div id="pr-topic-bars" class="bars"></div></div>
    <div class="panel"><h2>Issue 老化</h2><div id="issue-aging-bars" class="bars"></div></div>
  </section>
  <section class="grid two" data-view="github">
    <div class="panel"><h2>逾期 Issue</h2><table id="overdue-issue-table"></table></div>
    <div class="panel"><h2>PR 体积 Top</h2><table id="pr-table"></table></div>
  </section>
  <section class="grid three" data-view="code">
    <div class="panel"><h2>代码常用词</h2><div id="code-word-bars" class="bars"></div></div>
    <div class="panel"><h2>命名词根</h2><div id="name-word-bars" class="bars"></div></div>
    <div class="panel"><h2>命名风格</h2><div id="naming-style-bars" class="bars"></div></div>
  </section>
  <section class="grid two" data-view="code">
    <div class="panel"><h2>代码风险热点</h2><table id="risk-table"></table></div>
    <div class="panel"><h2>C# 复杂文件</h2><table id="complex-table"></table></div>
  </section>
  <section class="grid two" data-view="unity">
    <div class="panel"><h2>Unity 场景复杂度</h2><table id="scene-table"></table></div>
    <div class="panel"><h2>Prefab 复杂度</h2><table id="prefab-table"></table></div>
  </section>
  <section class="grid two" data-view="github">
    <div class="panel"><h2>GitHub Actions 工作流复杂度</h2><table id="workflow-table"></table></div>
    <div class="panel"><h2>实现覆盖目录</h2><table id="catalog-table"></table></div>
  </section>
  <section class="grid two" data-view="people">
    <div class="panel"><h2>贡献领域分布</h2><div id="domain-bars" class="bars"></div></div>
    <div class="panel"><h2>代码 API 热点</h2><div id="api-bars" class="bars"></div></div>
  </section>
  <div class="footer">Raw data: raw_stats.json, file_inventory.csv, commit_history.csv, contributor_summary.csv. Git status note: <span id="status-note"></span></div>
</main>
<aside id="detail-drawer" class="drawer" aria-hidden="true"><div class="drawer-head"><h2 id="drawer-title">详情</h2><button id="drawer-close" class="mini">关闭</button></div><pre id="drawer-body"></pre></aside>
<script id="stats-data" type="application/json">{payload_json}</script>
<script>
const data = JSON.parse(document.getElementById('stats-data').textContent);
let scope = 'first';
let metric = 'files';
let globalFilter = '';
let activeView = 'all';
const tableSorts = {{}};
const fmt = new Intl.NumberFormat('en-US');
function bytes(n) {{
  const u = ['B','KB','MB','GB']; let v = Number(n), i = 0;
  while (v >= 1024 && i < u.length - 1) {{ v /= 1024; i++; }}
  return i === 0 ? `${{Math.round(v)}} B` : `${{v.toFixed(1)}} ${{u[i]}}`;
}}
function numberFor(row) {{ return metric === 'bytes' ? bytes(row[metric]) : fmt.format(row[metric] || 0); }}
function labelForMetric() {{ return metric === 'bytes' ? '体积' : metric === 'lines' ? '行数' : '文件数'; }}
function card(label, value, note) {{ return `<div class="card"><div class="metric">${{value}}</div><div class="label">${{label}}</div><div class="muted">${{note || ''}}</div></div>`; }}
function rowMatches(row) {{ return !globalFilter || JSON.stringify(row).toLowerCase().includes(globalFilter); }}
function labelOf(row) {{ return rText(row.name || row.author || row.canonical_author || row.keyword || row.topic || row.bucket || row.style || row.api || row.extension || row.label || row.persona || row.personality || ''); }}
function rText(value) {{ return String(value ?? ''); }}
function escapeHtml(value) {{
  return rText(value).replace(/[&<>"']/g, ch => ({{'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}}[ch]));
}}
function attr(value) {{ return escapeHtml(value); }}
function showDetail(title, row) {{
  document.getElementById('drawer-title').textContent = title || '详情';
  document.getElementById('drawer-body').textContent = typeof row === 'string' ? row : JSON.stringify(row, null, 2);
  const drawer = document.getElementById('detail-drawer');
  drawer.classList.add('open');
  drawer.setAttribute('aria-hidden', 'false');
}}
function closeDetail() {{
  const drawer = document.getElementById('detail-drawer');
  drawer.classList.remove('open');
  drawer.setAttribute('aria-hidden', 'true');
}}
function setFilter(value) {{
  globalFilter = rText(value).trim().toLowerCase();
  document.getElementById('global-filter').value = value || '';
  draw();
}}
function downloadRows(filename, rows) {{
  if (!rows.length) return;
  const cols = Object.keys(rows[0]);
  const csv = [cols.join(',')].concat(rows.map(r => cols.map(c => `"${{rText(r[c]).replaceAll('"','""')}}"`).join(','))).join('\\n');
  const blob = new Blob([csv], {{type: 'text/csv;charset=utf-8'}});
  const a = document.createElement('a');
  a.href = URL.createObjectURL(blob);
  a.download = filename;
  a.click();
  URL.revokeObjectURL(a.href);
}}
function applyView() {{
  document.querySelectorAll('[data-view]').forEach(el => {{
    const show = activeView === 'all' || el.dataset.view === activeView || (activeView === 'overview' && el.id === 'cards');
    el.classList.toggle('is-hidden', !show);
  }});
  document.querySelectorAll('.view-tab').forEach(btn => btn.classList.toggle('active', btn.dataset.view === activeView));
}}
function wireInteractions() {{
  document.querySelectorAll('.bar-row[data-detail]').forEach(el => {{
    el.onclick = () => {{
      const row = JSON.parse(el.dataset.detail);
      showDetail(el.dataset.filter || '详情', row);
      if (el.dataset.filter) setFilter(el.dataset.filter);
    }};
  }});
  document.querySelectorAll('th.sortable').forEach(th => {{
    th.onclick = () => {{
      const id = th.dataset.table, key = th.dataset.key;
      const current = tableSorts[id] || {{}};
      tableSorts[id] = {{key, asc: current.key === key ? !current.asc : false}};
      draw();
    }};
  }});
  document.querySelectorAll('tbody tr[data-detail]').forEach(tr => {{
    tr.onclick = () => showDetail(tr.dataset.title || '行详情', JSON.parse(tr.dataset.detail));
  }});
  document.querySelectorAll('.day[data-detail]').forEach(day => {{
    day.onclick = () => {{
      showDetail(day.dataset.filter || '提交日', JSON.parse(day.dataset.detail));
      setFilter(day.dataset.filter || '');
    }};
  }});
  document.querySelectorAll('[data-download]').forEach(btn => {{
    btn.onclick = evt => {{
      evt.stopPropagation();
      const payload = JSON.parse(btn.dataset.download);
      downloadRows(payload.filename, payload.rows);
    }};
  }});
}}
function renderCards() {{
  const s = data.summary;
  document.getElementById('cards').innerHTML = [
    card('文件', fmt.format(scope === 'first' ? s.first_party_files : s.tracked_files_seen), scope === 'first' ? '第一方，不含 Stats' : '包含第三方与 Packages'),
    card('第一方行数', fmt.format(s.first_party_lines), `${{fmt.format(s.first_party_code_lines)}} code/config lines`),
    card('Git 提交', fmt.format(s.commit_count), `${{fmt.format(s.active_days)}} active days`),
    card('贡献者', fmt.format(s.canonical_contributor_count), `${{fmt.format(s.contributor_count)}} raw author names`),
    card('GitHub PR', fmt.format(s.github_prs), data.github_summary.source),
    card('压哨 24h', fmt.format(s.deadline_sprint_24h || 0), s.top_sprinter ? `${{s.top_sprinter}} currently leads` : 'PR deadline heuristic'),
    card('救火指数', data.firefighting_index[0] ? fmt.format(data.firefighting_index[0].fire_score) : '0', data.firefighting_index[0] ? data.firefighting_index[0].author : 'no fire words'),
    card('代码风险点', fmt.format(data.risk_hotspots.length), 'Top items shown below')
  ].join('');
}}
function bars(id, rows, key, maxRows = 10) {{
  rows = rows.filter(rowMatches);
  const max = Math.max(...rows.map(r => r[key] || 0), 1);
  document.getElementById(id).innerHTML = rows.slice(0, maxRows).map(r => {{
    const w = Math.max(2, ((r[key] || 0) / max) * 100);
    const label = labelOf(r);
    return `<div class="bar-row" data-filter="${{attr(label)}}" data-detail="${{attr(JSON.stringify(r))}}"><div class="bar-label" title="${{attr(label)}}">${{escapeHtml(label)}}</div><div class="track"><div class="fill" style="width:${{w}}%"></div></div><div class="value">${{key === 'bytes' ? bytes(r[key]) : fmt.format(r[key] || 0)}}</div></div>`;
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
  bars('deadline-bars', data.deadline_sprint_stats.map(r => ({{...r, name: r.author}})), 'sprint_24h', 10);
  bars('fire-bars', data.firefighting_index.map(r => ({{...r, name: r.author}})), 'fire_score', 10);
  bars('issue-topic-bars', data.github_issue_topics, 'issues', 12);
  bars('pr-topic-bars', data.github_pr_topics, 'prs', 12);
  bars('issue-aging-bars', data.github_issue_aging, 'issues', 8);
  bars('code-word-bars', data.code_word_frequency, 'hits', 12);
  bars('name-word-bars', data.code_name_word_frequency, 'hits', 12);
  bars('naming-style-bars', data.code_naming_style, 'symbols', 8);
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
  c.onclick = () => showDetail('月度提交与代码流量', rows);
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
  c.onclick = () => showDetail(canvasId, rows);
}}
function heatmap() {{
  const max = Math.max(...data.commits_by_day.map(r => r.commits), 1);
  document.getElementById('heatmap').innerHTML = data.commits_by_day.map(r => {{
    const a = .12 + .88 * (r.commits / max);
    return `<div class="day" data-filter="${{attr(r.date)}}" data-detail="${{attr(JSON.stringify(r))}}" data-tip="${{r.date}}: ${{r.commits}} commits" style="background: rgba(143,209,127,${{a}})"></div>`;
  }}).join('');
}}
function table(id, rows, cols) {{
  rows = rows.filter(rowMatches);
  const sort = tableSorts[id];
  if (sort && sort.key) {{
    rows = rows.slice().sort((a,b) => {{
      const av = a[sort.key], bv = b[sort.key];
      const an = Number(av), bn = Number(bv);
      const cmp = Number.isFinite(an) && Number.isFinite(bn) ? an - bn : rText(av).localeCompare(rText(bv));
      return sort.asc ? cmp : -cmp;
    }});
  }}
  const download = attr(JSON.stringify({{filename: `${{id}}.csv`, rows}}));
  document.getElementById(id).innerHTML =
    `<caption><div class="table-tools"><button class="mini" data-download="${{download}}">下载 CSV</button></div></caption>` +
    `<thead><tr>${{cols.map(c => `<th class="sortable" data-table="${{id}}" data-key="${{c[1]}}">${{c[0]}}${{sort && sort.key === c[1] ? (sort.asc ? ' ↑' : ' ↓') : ''}}</th>`).join('')}}</tr></thead><tbody>` +
    rows.map(r => `<tr data-title="${{attr(id)}}" data-detail="${{attr(JSON.stringify(r))}}">${{cols.map(c => `<td class="${{typeof r[c[1]] === 'number' ? 'num' : ''}}">${{c[2] ? c[2](r[c[1]], r) : escapeHtml(r[c[1]])}}</td>`).join('')}}</tr>`).join('') + '</tbody>';
}}
function renderTables() {{
  table('contrib-profile-table', data.contributor_profiles.slice(0,12), [['贡献者','canonical_author'], ['偏好','preference'], ['提交','commits', v => fmt.format(v)], ['Churn','churn', v => fmt.format(v)], ['触碰体量','current_bytes_touched', v => bytes(v)]]);
  table('contrib-ext-table', data.author_extension_churn.slice(0,16), [['贡献者','author'], ['扩展名','extension'], ['Churn','churn', v => fmt.format(v)], ['文件触碰','files_touched', v => fmt.format(v)]]);
  table('rhythm-table', data.contributor_rhythm_profiles.slice(0,12), [['贡献者','author'], ['人格','persona'], ['夜间%','night_pct', v => `${{v}}%`], ['周末%','weekend_pct', v => `${{v}}%`], ['峰值日','peak_day'], ['峰值提交','peak_day_commits', v => fmt.format(v)]]);
  table('deadline-event-table', data.deadline_sprint_events.slice(0,14), [['PR','pr_number'], ['标题','title', v => `<span title="${{attr(v)}}">${{escapeHtml(String(v).slice(0,52))}}</span>`], ['压哨者','author'], ['提前小时','hours_before_deadline', v => fmt.format(v)], ['24h','within_24h', v => v ? 'yes' : 'no']]);
  table('pr-personality-table', data.pr_personality.slice(0,14), [['PR','number'], ['标题','title', v => `<span title="${{attr(v)}}">${{escapeHtml(String(v).slice(0,52))}}</span>`], ['性格','personality'], ['Churn','churn', v => fmt.format(v)], ['耗时h','lead_time_hours', v => v === '' ? '' : fmt.format(v)]]);
  table('churn-table', data.top_churn_files.slice(0,12), [['文件','path', v => `<span title="${{v}}">${{String(v).slice(0,64)}}</span>`], ['Churn','churn', v => fmt.format(v)], ['Touches','touches', v => fmt.format(v)]]);
  table('large-table', data.largest_files.slice(0,12), [['文件','path', v => `<span title="${{v}}">${{String(v).slice(0,64)}}</span>`], ['体积','bytes', v => bytes(v)], ['类型','category']]);
  table('risk-table', data.risk_hotspots.slice(0,12), [['文件','path', v => `<span title="${{v}}">${{String(v).slice(0,58)}}</span>`], ['风险分','risk_score', v => fmt.format(Math.round(v))], ['复杂度','complexity', v => fmt.format(v)], ['Churn','churn', v => fmt.format(v)]]);
  table('complex-table', data.top_complex_files.slice(0,12), [['文件','path', v => `<span title="${{v}}">${{String(v).slice(0,58)}}</span>`], ['行','lines', v => fmt.format(v)], ['复杂度','approx_complexity', v => fmt.format(v)], ['Update','update_methods', v => fmt.format(v)]]);
  table('scene-table', data.scene_complexity.slice(0,12), [['Scene','path', v => `<span title="${{v}}">${{String(v).slice(0,58)}}</span>`], ['对象','game_objects', v => fmt.format(v)], ['组件','components', v => fmt.format(v)], ['GUID','guid_refs', v => fmt.format(v)]]);
  table('prefab-table', data.prefab_complexity.slice(0,12), [['Prefab','path', v => `<span title="${{v}}">${{String(v).slice(0,58)}}</span>`], ['组件','components', v => fmt.format(v)], ['Mono','mono_behaviours', v => fmt.format(v)], ['GUID','guid_refs', v => fmt.format(v)]]);
  table('workflow-table', data.workflow_complexity.slice(0,12), [['Workflow','workflow'], ['Jobs','jobs', v => fmt.format(v)], ['Steps','steps', v => fmt.format(v)], ['Score','complexity_score', v => fmt.format(v)]]);
  table('catalog-table', data.metric_catalog.slice(0,18), [['优先级','priority'], ['指标','metric'], ['状态','status'], ['难度','difficulty']]);
  table('overdue-issue-table', data.github_overdue_issues.slice(0,14), [['#','number'], ['标题','title', v => `<span title="${{v}}">${{String(v).slice(0,54)}}</span>`], ['主题','topic'], ['天数','age_days', v => fmt.format(v || 0)], ['原因','overdue_reason']]);
  table('pr-table', data.github_prs.slice().sort((a,b) => ((b.additions || 0) + (b.deletions || 0)) - ((a.additions || 0) + (a.deletions || 0))).slice(0,14), [['#','number'], ['标题','title', v => `<span title="${{v}}">${{String(v).slice(0,54)}}</span>`], ['主题','topic'], ['作者','author'], ['Δ','additions', (v,r) => fmt.format((r.additions || 0) + (r.deletions || 0))]]); 
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
      <tr><th>Issues</th><td>${{fmt.format(g.issues)}} total, ${{fmt.format(g.closed_issues)}} closed, ${{fmt.format(g.overdue_issues)}} overdue</td></tr>
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
  applyView();
  wireInteractions();
}}
document.getElementById('scope-first').onclick = () => {{ scope = 'first'; document.getElementById('scope-first').classList.add('active'); document.getElementById('scope-all').classList.remove('active'); draw(); }};
document.getElementById('scope-all').onclick = () => {{ scope = 'all'; document.getElementById('scope-all').classList.add('active'); document.getElementById('scope-first').classList.remove('active'); draw(); }};
document.getElementById('metric-select').onchange = e => {{ metric = e.target.value; draw(); }};
document.getElementById('global-filter').oninput = e => {{ globalFilter = e.target.value.trim().toLowerCase(); draw(); }};
document.getElementById('drawer-close').onclick = closeDetail;
document.querySelectorAll('.view-tab').forEach(btn => btn.onclick = () => {{ activeView = btn.dataset.view; draw(); }});
draw();
</script>
</body>
</html>
"""


def main():
    OUT.mkdir(exist_ok=True)
    files, symbols, todos = scan_files()
    commits, file_churn, author_churn, author_domain_rows, author_extension_rows, author_file_rows = parse_git_history()
    data = aggregate(files, commits, file_churn, author_churn, author_domain_rows, author_extension_rows)
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
    contributor_profiles = build_contributor_profiles(alias_contributors, author_domain_rows, author_extension_rows, author_file_rows, files)
    risk_hotspots = build_risk_hotspots(code_data, file_churn)
    file_churn_timeline = analyze_file_churn_timeline([row["path"] for row in risk_hotspots[:20]])
    fun_stats = analyze_fun_stats(commits, github_data)
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
            "contributor_profiles": contributor_profiles,
            "risk_hotspots": risk_hotspots,
            "file_churn_timeline": file_churn_timeline,
            "fun_stats": fun_stats,
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
            "deadline_sprint_events": fun_stats["summary"]["deadline_events"],
            "deadline_sprint_24h": fun_stats["summary"]["sprint_24h_events"],
            "top_sprinter": fun_stats["summary"]["top_sprinter"],
            "firefighting_leader": fun_stats["summary"]["firefighting_leader"],
        }
    )
    full = {"summary": data["summary"], "unity": unity, "symbols": symbols, "datasets": data}

    (OUT / "raw_stats.json").write_text(json.dumps(full, ensure_ascii=False, indent=2), encoding="utf-8")
    write_csv(OUT / "file_inventory.csv", files)
    write_csv(OUT / "commit_history.csv", commits)
    write_csv(OUT / "contributor_summary.csv", data["contributors"])
    write_csv(OUT / "contributor_alias_summary.csv", alias_contributors)
    write_csv(OUT / "contributor_profiles.csv", contributor_profiles)
    write_csv(OUT / "author_domain_churn.csv", data["author_domain_churn"])
    write_csv(OUT / "author_extension_churn.csv", data["author_extension_churn"])
    write_csv(OUT / "author_file_touches.csv", author_file_rows)
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
    write_csv(OUT / "code_word_frequency.csv", code_data["code_word_frequency"])
    write_csv(OUT / "code_name_word_frequency.csv", code_data["code_name_word_frequency"])
    write_csv(OUT / "code_naming_style.csv", code_data["code_naming_style"])
    write_csv(OUT / "code_symbol_naming.csv", code_data["symbol_naming_rows"])
    write_csv(OUT / "api_usage_summary.csv", code_data["api_usage_summary"])
    write_csv(OUT / "field_naming_violations.csv", code_data["field_violation_rows"])
    write_csv(OUT / "magic_number_hotspots.csv", code_data["magic_number_rows"])
    write_csv(OUT / "risk_hotspots.csv", risk_hotspots)
    write_csv(OUT / "file_churn_timeline.csv", file_churn_timeline)
    write_csv(OUT / "deadline_sprint_stats.csv", fun_stats["deadline_sprint_stats"])
    write_csv(OUT / "deadline_sprint_events.csv", fun_stats["deadline_sprint_events"])
    write_csv(OUT / "contributor_rhythm_profiles.csv", fun_stats["contributor_rhythm_profiles"])
    write_csv(OUT / "firefighting_index.csv", fun_stats["firefighting_index"])
    write_csv(OUT / "firefighting_by_month.csv", fun_stats["firefighting_by_month"])
    write_csv(OUT / "pr_personality.csv", fun_stats["pr_personality"])
    write_csv(OUT / "issue_deadlines.csv", fun_stats["issue_deadlines"])
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
    write_csv(OUT / "github_pr_topics.csv", github_data["pr_topics"])
    write_csv(OUT / "github_issues.csv", github_data["issues"])
    write_csv(OUT / "github_issue_topics.csv", github_data["issue_topics"])
    write_csv(OUT / "github_issue_labels.csv", github_data["issue_labels"])
    write_csv(OUT / "github_issue_aging.csv", github_data["issue_aging"])
    write_csv(OUT / "github_overdue_issues.csv", github_data["overdue_issues"])
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
