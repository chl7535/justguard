# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project state

This is a freshly created Unity project (product name `justguard`, company `DefaultCompany`) generated from Unity's default **3D (URP) template**. It has no custom gameplay code yet — the only non-boilerplate assets are the default `SampleScene` and the Unity-generated "Readme" tutorial window (`Assets/TutorialInfo/`). The game design and MVP scope are described in the "JustGuard 게임 기획" section below.

The project is a git repository on the `main` branch, with remote `origin` at `https://github.com/chl7535/justguard.git`. The root `.gitignore` is GitHub's standard `Unity.gitignore`, so `Library/`, `Temp/`, `Logs/`, `UserSettings/`, build output, and IDE-generated `*.csproj`/`*.sln` files are not tracked.

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

# JustGuard 게임 기획

## 개요
- 이동 없이 가드와 패링만으로 보스 패턴을 읽어 반격하는 1:1 결투 로그라이크
- 플랫폼: 모바일 + Steam
- 조작: 가드 버튼, 패링 버튼 2개뿐

## 전투 규칙
- 턴제: 적이 패턴을 연속 공격 → 내 턴에 반격
- 패링 성공 시 스택 누적, 적 턴 종료 후 스택 수만큼 강한 공격이 자동 발동
- 가드는 피해를 완전히 막지만 스택을 쌓지 못함
- 가드 게이지는 라운드당 1회, 소모하면 라운드가 끝날 때까지 회복되지 않음
- 플레이어 체력은 적 공격 2대에 사망

## 기술 방침
- 밸런스 수치는 전부 ScriptableObject(BattleConfig)로 분리해 인스펙터에서 조정 가능하게 유지
- 전투 흐름은 상태 머신으로 관리
- 코드 주석은 한국어로 작성
- 나는 유니티 개발 초보이므로, 작업 후에는 무엇을 왜 그렇게 만들었는지 쉽게 설명해줄 것

## 현재 MVP 범위
- 1라운드, 적 1종, 패링 판정, 스택 누적, 스택 기반 반격, 승패 처리까지만
- 스킬 시스템, 상점, 중간보스/보스, 메타 프로그레션, 그래픽/사운드는 제외
