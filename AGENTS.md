# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

## Project Overview

RainRust is a 2D platformer built in Unity (URP). The player controls a character with grappling hook, rope swinging, throwable bee companion, and whistle/wave mechanics. Levels are authored in LDtk and loaded at runtime via Addressables.

## Code Quality

**Format:** All C# code must be formatted with **CSharpier**.

```bash
dotnet tool install -g csharpier
csharpier check Assets/
csharpier format Assets/
```

**Linting:** Project uses **Roslynator**; follow all its suggestions strictly.

**Logging:** Always use `CLogger` with at least one `LogTag` (e.g., `LogTag.Game`). Never use `Debug.Log` directly.

## Architecture

### Initialization & Lifecycle

`GameCore` (at `Assets/Core/GameCore/GameCore.cs`) is the central entry point. It discovers all `ICoreModuleSystem` implementations, resolves their declared dependencies, and initializes them in order. Each system hooks into named lifecycle events: `OnLoadComplete`, `OnInLevelEnter`, `OnInLevelExit`, etc.

`CoreModuleManagerBase` (`Assets/Core/CoreModule/CoreModuleManagerBase.cs`) is the MonoSingleton base class for all manager singletons. All persistent game managers derive from it.

### Event System

Events live in an `Events/` folder that **mirrors the structure of the business code directory**. Every event is a `struct` implementing `IEvent`. The library is **R3** (not Unity Events or C# delegates).

```
Business code:  GameMain/RunTime/GameCoreSystems/LevelManager/LevelManager.cs
Event file:     GameMain/RunTime/Events/LevelManager/LevelManagerEvents.cs
Wrapper class:  public static class LevelManagerEvents { }
Naming:         [Subject][Action]Event  or  [Subject]Pre|On|PostEvent
```

Namespace must match the `.asmdef` root namespace — no extra `.Events` suffix.

### Command System

Commands mirror the same structure under a `Commands/` folder. Commands implement `ICommand`.

```
Command file:   GameMain/RunTime/Commands/LevelManager/LevelManagerCommands.cs
Wrapper class:  public static class LevelManagerCommands { }
Naming:         [Action][Subject]Command  (e.g. LoadLevelCommand)
```

Both event and command names must be self-contained — never rely on the enclosing static class name for meaning.

### Level Management

`LevelManager` loads LDtk levels via Addressables, activates rooms, manages save points, and spawns entities. It fires events from `LevelManagerEvents` for other systems to react to.

### Key Third-Party Libraries

| Library | Purpose |
|---|---|
| R3 (Cysharp) | Event/reactive system — use this, not UnityEvents |
| UniTask (Cysharp) | All async operations |
| LDtk Unity | Level data from the external level editor |
| FMOD | Audio — all BGM/SFX go through FMOD |
| Odin Inspector | Editor serialization and inspector tooling |
| DOTween | Tweening and animation |
| Addressables | All runtime asset loading |
| Cinemachine 3.x | Camera control |
| UnityHFSM | Hierarchical FSM used in gameplay state machines |
| FP utilities | `Assets/Core/FP/` — Result type and functional helpers |

## Coding Conventions

- **Private/internal fields:** prefix with `m_` (e.g., `m_IsTransitioning`)
- **Class layout:** fields and properties go at the **bottom** of the class
- **No comments** unless the code itself cannot express the information
- **Functional style** preferred; use `Result` from `Assets/Core/FP/` for error handling
- **Namespace:** must match the `.asmdef` root namespace of the assembly

## Asset Naming

Format: `type_category_?subcategory_?action_?variant_001`

- snake_case throughout; pad numbers to 3 digits (`001` not `1`)
- Broad-to-specific ordering (e.g., `enemy_boss_eggman`, not `boss_enemy_eggman`)
- Use game-theme terms, not mechanic names
- Common type prefixes: `gp` (gameplay), `plr` (player), `char` (character), `mus` (music), `amb` (ambience)

## CI/CD

The GitHub Actions pipeline (`.github/workflows/ci-cd-main.yml`) runs in stages:

1. **Code style check** — CSharpier validation
2. **Unit tests** — EditMode + PlayMode via Unity Test Framework (`Assets/Tests/`)
3. **Build tests** — 5 profiles in parallel: `Web-Release`, `Windows-Debug`, `Windows-Release`, `Mac-Debug`, `Mac-Release` (profiles live at `Assets/Settings/BuildProfiles/`)
4. **Release / Pages deploy** — triggered on `release/*` branches or `v*.*.*-release` tags

PRs to `develop` or `main` run stages 1–3 automatically.

## Agent skills

### Issue tracker

Issues and PRDs live as markdown files under `.scratch/<feature-slug>/`. See `docs/agents/issue-tracker.md`.

### Triage labels

Canonical five-role vocabulary: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one `CONTEXT.md` and one `docs/adr/` at the repo root. See `docs/agents/domain.md`.
