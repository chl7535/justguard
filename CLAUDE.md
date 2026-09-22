# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project state

This is a freshly created Unity project (product name `justguard`, company `DefaultCompany`) generated from Unity's default **3D (URP) template**. It has no custom gameplay code yet — the only non-boilerplate assets are the default `SampleScene` and the Unity-generated "Readme" tutorial window (`Assets/TutorialInfo/`). There is no git repository initialized in this directory yet.

## Environment

- Unity Editor version: **6000.6.2f1** (Unity 6), pinned in `ProjectSettings/ProjectVersion.txt`. Use this exact version when opening the project or invoking Unity from the command line.
- Render pipeline: **Universal Render Pipeline (URP)** — pipeline assets live in `Assets/Settings/` (`PC_RPAsset`/`PC_Renderer` and `Mobile_RPAsset`/`Mobile_Renderer` for the two quality tiers, plus `DefaultVolumeProfile` and `UniversalRenderPipelineGlobalSettings`).
- Input: the new **Input System** package, configured via `Assets/InputSystem_Actions.inputactions`.
- Other notable packages (see `Packages/manifest.json`): `com.unity.ai.navigation` (NavMesh), `com.unity.timeline`, `com.unity.visualscripting`, `com.unity.test-framework`.

## Working with the project

- There is no CLI build/test setup yet (no `.sln`, no scripts, no CI config). Editor-driven workflows (opening scenes, adding components, running Play mode, running tests via the Test Framework) go through the Unity Editor itself.
- A `unity-editor-mcp` MCP server is available in this environment for driving the Unity Editor directly (scene/GameObject/component manipulation, script creation, builds, test runs, asset management, baking, etc.) instead of hand-editing `.unity`/`.asset` YAML files. Prefer it over manually editing scene/prefab YAML.
- Scene and asset files (`.unity`, `.asset`, `.prefab`, `.controller`, etc.) are Unity's YAML serialization format; avoid hand-editing them unless necessary, and always keep the paired `.meta` file with any asset you add, move, or rename (the `.meta` file's GUID is how Unity tracks references — losing or regenerating it breaks scene/prefab links).
- `Library/`, `Temp/`, `Logs/`, and `UserSettings/` are Unity-generated caches/local state, not source — don't hand-edit or rely on their contents persisting.
