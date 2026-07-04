# Drift Assignment — Project Setup & Baseline Delta

> **Purpose**: Show the interviewer *exactly* what was added on top of an empty Unity 6 URP 3D template — nothing hidden, nothing hand-waved. This doc pairs with [REQUIREMENTS.md](REQUIREMENTS.md) and gets exported to PDF as part of the final deliverable bundle.

---

## 1. Baseline — What a fresh URP 3D Sample template (Unity 6.0.69f1) ships with

A brand-new URP 3D project on Unity 6 contains:

**Packages (top-level)**
- `com.unity.render-pipelines.universal` — URP 17.0.4
- `com.unity.inputsystem` — 1.19.0 (new Input System)
- `com.unity.ide.rider`, `com.unity.ide.visualstudio` — IDE integrations
- `com.unity.collab-proxy` — Version Control integration
- `com.unity.test-framework`, `com.unity.timeline`, `com.unity.ugui`
- `com.unity.ai.navigation`, `com.unity.multiplayer.center`, `com.unity.visualscripting`
- Built-in modules (physics, animation, audio, vehicles, terrain, etc.)

**Assets**
- `Assets/Scenes/SampleScene.unity`
- `Assets/Settings/` — `PC_RPAsset`, `PC_Renderer`, `Mobile_RPAsset`, `Mobile_Renderer`, `DefaultVolumeProfile`, `SampleSceneProfile`, `UniversalRenderPipelineGlobalSettings`
- `Assets/TutorialInfo/` — sample icons, layout, tutorial scripts (template clutter)
- `Assets/Readme.asset` — welcome sheet (template clutter)
- `Assets/InputSystem_Actions.inputactions` — default input actions

**Player settings defaults**
- Build target: **Windows Standalone** (PC)
- Scripting backend: **Mono**
- API compatibility: .NET Standard 2.1
- Company name: **DefaultCompany**
- Product name: **<from template>**

---

## 2. Current state — What's already in *this* project

Verified against `Packages/manifest.json`, `Packages/packages-lock.json`, and `ProjectSettings/ProjectSettings.asset` at the time of writing.

### Packages added on top of URP template ✅

| Package | Version | Purpose |
|---|---|---|
| `com.coplaydev.unity-mcp` | git (`main` branch) | **Agentic Editor automation via Claude / MCP** |
| `com.unity.cinemachine` | 3.1.7 | Multi-angle car cameras (chase / hood / cinematic / look-back) |
| `com.unity.memoryprofiler` | 1.1.12 | Deep memory profiling on-device (Phase 9 optimization) |
| `com.unity.mobile.android-logcat` | 1.4.7 | View device logs inside Unity Editor for Android debugging |
| `com.unity.recorder` | 5.1.6 | Screen / gameplay recording for demo videos + replay tie-ins |
| `com.unity.splines` | 2.8.3 | (Transitive dep of Cinemachine; useful for road / camera paths later) |
| `com.unity.2d.sprite` | 1.0.0 | Sprite editor for HUD icon atlasing (Phase 4 / 9) |

### Player settings already changed ✅

| Setting | Baseline | This project | Notes |
|---|---|---|---|
| Product name | `<template>` | **DriftAssignment** | Set |
| Company name | DefaultCompany | **GrindingStudio** | ✅ set |
| Android scripting backend | Mono (Standalone default) | **IL2CPP** | ✅ Play Store requirement met |
| Android target architectures | ARMv7 | **ARM64 only** (bitmask `2`) | ✅ Play Store 64-bit requirement met |
| API compatibility level | .NET Standard 2.1 | **.NET Standard 2.1** (`6`) | ✅ |
| Android min SDK | 23 (Android 6.0) | **24 (Android 7.0)** | ✅ per REQUIREMENTS spec |
| Android target SDK | Highest installed | **Auto (`0`)** | ✅ |
| Active build target | Windows Standalone | *(inferred Android — `scriptingBackend.Android` configured)* | Verify in Editor |

### Assets currently present

```
Assets/
  Scenes/Main.unity                     ✅ renamed from SampleScene
  Settings/
    Mobile_RPAsset.asset                ✅ mobile-tuned (HDR off, MSAA 2x, ShadowDist 40)
    Mobile_Renderer.asset
    PC_RPAsset.asset                    (unused on Android; kept for Editor)
    PC_Renderer.asset
    DefaultVolumeProfile.asset
    SampleSceneProfile.asset
    UniversalRenderPipelineGlobalSettings.asset
  InputSystem_Actions.inputactions      (baseline — extend with driver actions in Phase 2/4)
  _Project/                             ✅ full folder tree (Scripts, Prefabs, SOs, Materials, etc.)
    Scripts/{Core,Vehicle,Damage,Input,UI,Camera,Character}/
      each with DriftAssignment.<Module>.asmdef  ✅
  ThirdParty/
    Kenney/LICENSE.txt                  ✅
    Quaternius/LICENSE.txt              ✅
    Mixamo/LICENSE.txt                  ✅
  (removed) TutorialInfo/               ✅
  (removed) Readme.asset                ✅
```

---

## 3. Delta table — what's added / removed / changed on top of the URP template

Legend: 🟢 done | 🟡 partially done | 🔴 pending

### 3.1 Packages

| Change | Item | Status | Phase |
|---|---|---|---|
| Add | Cinemachine 3.1.7 | 🟢 | 0 |
| Add | CoplayDev Unity MCP (git) | 🟢 | 0 |
| Add | Memory Profiler | 🟢 | 0 |
| Add | Android Logcat | 🟢 | 0 |
| Add | Unity Recorder | 🟢 | 0 |
| Add | 2D Sprite | 🟢 | 0 |
| Add | (Transitive) Splines | 🟢 | 0 |

### 3.2 Build & Player settings

| Change | Item | Target value | Status | Phase |
|---|---|---|---|---|
| Change | Active build target | Android | 🟡 (scripting backend set — confirm `Switch Platform` was clicked in Editor) | 0 |
| Change | Android scripting backend | IL2CPP | 🟢 | 0 |
| Change | Android target architectures | ARM64 only | 🟢 | 0 |
| Change | API compatibility | .NET Standard 2.1 | 🟢 | 0 |
| Change | Android min SDK | 24 (Android 7.0) | 🟢 | 0 |
| Change | Company name | GrindingStudio | 🟢 | 0 |
| Change | Orientation | Landscape (auto rotate: Landscape L + R) | 🟢 landscape L+R on, portrait off | 0 |
| Change | Multi-touch enabled | true | 🟡 default enabled with Input System — verify in Editor | 0 |

### 3.3 URP asset tuning (Mobile_RPAsset)

| Change | Item | Target | Status | Phase |
|---|---|---|---|---|
| Change | HDR | Off | 🟢 `m_SupportsHDR: 0` | 0 |
| Change | MSAA | 2× | 🟢 `m_MSAA: 2` | 0 |
| Change | Shadow distance | 40 m | 🟢 `m_ShadowDistance: 40` | 0 |
| Change | Shadow cascades | 1 (single) | 🟢 already `1` | 0 |
| Change | Depth texture | Off (turn on only if needed for water/decals) | 🟢 already off | 0 |
| Change | Opaque texture | Off (mobile perf) | 🟢 already off | 0 |
| Change | Post-processing | On (mild bloom only) | 🔴 configure in Phase 8 | 0 / 8 |

### 3.4 Project folders & scenes

| Change | Item | Status | Phase |
|---|---|---|---|
| Remove | `Assets/TutorialInfo/` | 🟢 | 0 |
| Remove | `Assets/Readme.asset` | 🟢 | 0 |
| Rename | `Scenes/SampleScene.unity` → `Scenes/Main.unity` | 🟢 (+ EditorBuildSettings updated, enabled=1) | 0 |
| Add | `Assets/_Project/Scripts/{Core,Vehicle,Damage,Input,UI,Camera,Character}/` | 🟢 | 0 |
| Add | `Assets/_Project/Prefabs/{Vehicles,Environment,UI}/` | 🟢 | 0 |
| Add | `Assets/_Project/ScriptableObjects/{TuningPresets,CarConfig}/` | 🟢 | 0 |
| Add | `Assets/_Project/Materials`, `Textures`, `Audio`, `Fonts` | 🟢 | 0 |
| Add | `Assets/ThirdParty/{Kenney,Quaternius,Mixamo}/` with `LICENSE.txt` | 🟢 | 0 / 1 |
| Add | Assembly Definitions per module (`DriftAssignment.*`) | 🟢 all 7 created | 0 |

### 3.5 Repo-level docs & tooling

| Change | Item | Status | Phase |
|---|---|---|---|
| Add | `.editorconfig` (mirrors Unity C# style guide) | 🟢 | 0 |
| Add | `Doc/REQUIREMENTS.md` | 🟢 | Docs |
| Add | `Doc/ARCHITECTURE.md` | 🟢 | Docs |
| Add | `Doc/IMPLEMENTATION_LOG.md` | 🟢 scaffold | Docs |
| Add | `Doc/OPTIMIZATION.md` | 🟢 scaffold | Docs |
| Add | `Doc/CREDITS.md` | 🟢 scaffold | Docs |
| Add | `Doc/PROJECT_SETUP.md` (this doc) | 🟢 | Docs |
| Add | `Doc/build-pdfs.ps1` (Pandoc / md-to-pdf pipeline) | 🟢 | Docs |
| Add | `CLAUDE.md` at project root | 🔴 pending (after Phase 0 lands, next up) | 11 |

### 3.6 Unity MCP registration (already installed, verify wired to Claude)

| Change | Item | Status |
|---|---|---|
| Verify | MCP server responds on `http://127.0.0.1:8080/mcp` | 🔴 verify with Unity Editor open |
| Verify | `claude mcp list` shows `UnityMCP` | 🔴 verify |
| Register (if missing) | `claude mcp add --scope local --transport http UnityMCP http://127.0.0.1:8080/mcp` | (see [REQUIREMENTS §3](REQUIREMENTS.md)) |

---

## 4. Phase 0 execution checklist — status

| # | Task | Status |
|---|---|---|
| 1 | Active build target = Android | 🟡 verify `Switch Platform` clicked in Editor |
| 2 | `AndroidMinSdkVersion` 23 → 24 | 🟢 |
| 3 | Company name set | 🟢 GrindingStudio |
| 4 | Orientation = Landscape L+R only | 🟢 |
| 5 | Multi-touch enabled | 🟡 default with Input System — verify in Editor |
| 6 | `Mobile_RPAsset` mobile tune (HDR, MSAA, ShadowDist, cascades, depth/opaque) | 🟢 |
| 7 | Delete `TutorialInfo/` and `Readme.asset` | 🟢 |
| 8 | Rename `SampleScene.unity` → `Main.unity` (+ update EditorBuildSettings) | 🟢 |
| 9 | Create `_Project/` folder tree | 🟢 |
| 10 | Create `ThirdParty/` folder tree + `LICENSE.txt` | 🟢 |
| 11 | Create 7 Assembly Definitions | 🟢 |
| 12 | Unity MCP: `claude mcp list` + live health-check | 🔴 pending — run `claude mcp add ...` per REQUIREMENTS §3 |
| 13 | Empty Android build produces APK | 🔴 pending — user action in Editor |

**Two items still need user action in the Editor**:
- Open Unity → `File → Build Profiles → Android → Switch Platform` (if not already active). This triggers Unity to reimport assets for Android and confirms Phase 0.
- Run `claude mcp add --scope local --transport http UnityMCP http://127.0.0.1:8080/mcp` (or click **Configure** in the CoplayDev Unity MCP window) so Claude can drive the Editor for Phases 1+.
- Attempt a headless Android build (`File → Build`) to confirm APK produces cleanly.

---

## 5. What we did *not* need to add

Called out explicitly because interviewers often ask "did you remember X?":

- **URP** — already in the template
- **Input System** — already in the template (v1.19.0)
- **Vehicles module** (WheelCollider lives here) — built-in, already available
- **Physics module** — built-in, always available
- **Terrain module** — built-in (used for dune flanks)
- **UI Toolkit / UGUI** — UGUI is in template; using UGUI for HUD (Screen Space Overlay)

---

## 6. Rationale for each package we added

| Package | Why we added it |
|---|---|
| Cinemachine | Multiple camera angles (chase / hood / cinematic / look-back) matching the reference camera-cycle button. Blends between cams are free with Cinemachine and a lot of manual math to replicate. |
| CoplayDev Unity MCP | Lets Claude Code drive the Unity Editor directly — create GameObjects, wire prefabs, run tests, without leaving the chat. Massive iteration-speed multiplier for this assignment. |
| Memory Profiler | Phase 9 optimization requires on-device memory snapshots. Deep Profiler alone isn't enough; the Memory Profiler package captures heap detail. |
| Android Logcat | View `Debug.Log` output from the device inside the Unity Editor without needing external `adb logcat` sessions. |
| Unity Recorder | Capture gameplay video for demo submission + potential replay-record feature (stretch goal per REQUIREMENTS §4.1). |
| 2D Sprite | Needed to build sprite atlases for HUD icons (Phase 4 / 9 optimization — atlasing collapses UI draw calls). |

---

## 7. What this table means for the interviewer

- **No hidden magic**: everything the project has beyond the URP template is listed, versioned, and justified.
- **Nothing added for its own sake**: each package traces to a specific phase or requirement.
- **Reversible**: removing any single addition and doing that work by hand is possible — the choices are pragmatic, not architectural.
