# DriftAssignment — Claude Code guide

Job-interview assignment: Android drifting game inspired by *Arabic Drifting*.
Unity 6, URP, sim-cade car physics, dent + break damage system, drift-tuned handling.

Full scope in [Doc/REQUIREMENTS.md](Doc/REQUIREMENTS.md); layered architecture in [Doc/ARCHITECTURE.md](Doc/ARCHITECTURE.md); phase-by-phase log in [Doc/IMPLEMENTATION_LOG.md](Doc/IMPLEMENTATION_LOG.md).

---

## Engine + Target

- **Unity 6000.0.69f1** (URP 17.0.4)
- **Target platform: Android** (min SDK 24, ARM64 only, IL2CPP, .NET Standard 2.1, landscape L+R only)
- **Editor test**: keyboard fallback via `KeyboardInputProvider` (WASD + Space + Q/E)

Active scene: `Assets/Scenes/Main.unity` (only scene in build settings).

---

## Code style — mandatory

Follow Unity's official C# style guide:
**https://unity.com/how-to/naming-and-code-style-tips-c-scripting-unity**

Enforced by [`.editorconfig`](.editorconfig) at repo root. Quick cheatsheet:

- Classes / methods / properties / enums / enum members → `PascalCase`
- Local variables & parameters → `camelCase`
- Private fields → `_camelCase` (leading underscore)
- Constants → `PascalCase` (not `SCREAMING_SNAKE_CASE`)
- Interfaces → `IPascalCase`
- Enums → singular name; PascalCase members. `[Flags]` → plural
- Braces → Allman style
- Namespaces → mirror folder path (`DriftAssignment.Vehicle`, etc.)
- Serialized private fields: `[SerializeField] private float _foo;`

---

## Folder structure

```
Assets/
  _Project/                  ← all first-party project code, prefabs, SOs, materials
    Scripts/
      Core/                  ← shared interfaces, enums (IInputProvider, DrivetrainMode)
      Input/                 ← IInputProvider implementations (KeyboardInputProvider)
      Vehicle/               ← CarController + Drivetrain/GearBox/HandBrake/SteeringAssist/
                               WheelController + CarConfig SO + TuningState SO
      Damage/                ← [Phase 7] ImpactReceiver, DentableMesh, DetachablePart
      UI/                    ← [Phase 4] HudController, TuningMenuController, PresetPicker
      Camera/                ← [Phase 3] CameraRig (Cinemachine wrappers)
      Character/             ← [Phase 10 stretch] CharacterEnterExit
    Prefabs/
      Vehicles/              ← RMCar26 project prefab (WIP)
      Environment/           ← CircuitTerrain.asset
      UI/                    ← [Phase 4] HUD.prefab, TuningMenu.prefab
    ScriptableObjects/
      CarConfig/             ← CarConfig_RMCar26.asset
      TuningPresets/         ← [Phase 6] Preset_0..Preset_13 SOs
      TuningState_Default.asset
    Materials/               ← Sky_Kloofendal.mat, TerrainLayer_Sand/Beach.terrainlayer,
                               BarrierConcrete.mat
    Scenes/                  ← Main.unity
  ThirdParty/                ← ALL vendor assets — never keep imports at Assets/ root
    RealisticMobileCars - Pro3DModels/   ← hero car (RMCar26 pack)
    EasyRoads3D/ + EasyRoads3D scenes/   ← circuit builder
    Simple Street Props/                 ← 23 trackside prop prefabs
    Road props for games/                ← 25 trackside prop prefabs
    AerialBeach/ + AerialSand/           ← Poly Haven PBR terrain textures
    SkyBox/                              ← Kloofendal HDRI
    GroundSand005/                       ← alt sand PBR (fallback)
Doc/                         ← REQUIREMENTS, ARCHITECTURE, IMPLEMENTATION_LOG,
                               OPTIMIZATION, PROJECT_SETUP, CREDITS, build-pdfs.ps1
```

**Rule**: any imported Asset Store pack or external download must be moved into `Assets/ThirdParty/<PublisherOrPack>/` before use. `Assets/` root stays clean.

---

## Assembly Definitions

Dependency direction is enforced by asmdefs — never reverse it:

```
Core   ←  Input   ←  Vehicle  ←  UI / Camera / Character
              ↖  Damage  →  Core
```

- `DriftAssignment.Core` — no deps
- `DriftAssignment.Input` → Core, Unity.InputSystem
- `DriftAssignment.Vehicle` → Core, Input
- `DriftAssignment.Damage` → Core
- `DriftAssignment.UI` → Core, Vehicle (events only), Unity.InputSystem
- `DriftAssignment.Camera` → Core, Vehicle, Unity.Cinemachine
- `DriftAssignment.Character` → Core, Vehicle, Input, Unity.InputSystem

---

## Key packages installed on top of the URP 3D template

Full delta table in [Doc/PROJECT_SETUP.md](Doc/PROJECT_SETUP.md).

- `com.unity.cinemachine` 3.1.7 — chase / hood / cinematic / look-back cams
- `com.coplaydev.unity-mcp` (git) — **agentic Editor control from Claude Code**
- `com.unity.memoryprofiler` 1.1.12 — Phase 9 perf work
- `com.unity.mobile.android-logcat` 1.4.7 — device debug
- `com.unity.recorder` 5.1.6 — demo capture

---

## Unity MCP setup

The CoplayDev Unity MCP lets Claude drive the Editor directly.

**Register** (once, user scope preferred so it survives session restarts):
```
claude mcp add --scope user --transport http UnityMCP http://127.0.0.1:8080/mcp
```

**Prereq**: Unity Editor open with the CoplayDev MCP window running (serves on `127.0.0.1:8080/mcp`).

**Health check** for any Claude session before invoking MCP tools:
1. `claude mcp list` should show `UnityMCP - ✓ Connected`
2. Read `mcpforunity://editor/state` → `advice.ready_for_tools == true`

If tools appear as "disconnected" mid-session, the Editor probably lost focus / MCP window closed — reopen it and restart the Claude session.

---

## Running / building

**Editor Play (dev loop)**
- Open `Assets/Scenes/Main.unity`
- Press Play — WASD + Space handbrake + Q/E manual shift work out of the box

**Android APK**
- Build Profiles → Android → Switch Platform (if not already)
- File → Build → confirm APK
- Deploy: `adb install -r drift.apk`

---

## Current status (phases)

- **Phase 0** ✅ Project setup (Android target, folders, asmdefs, docs, `.editorconfig`)
- **Phase 1** ✅ Desert circuit env (500×500 terrain, sand layers, HDRI, EasyRoads3D 1090m closed loop, 84 track-side props, boundary walls)
- **Phase 2** ✅ Car physics — RMCar26 drivable in Editor, RWD/AWD/FWD, chase cam, reverse via auto-shift when braking from stopped
- **Phase 3–6** 🔴 Pending — Camera cycle, Touch HUD, Tuning menu, Presets
- **Phase 7** 🔴 Next — Damage system (dent + break on RMCar26's articulated panels)
- **Phase 8** 🔴 Polish — audio, particles, post-processing
- **Phase 9** 🔴 Optimization pass — cross-cutting, see `Doc/OPTIMIZATION.md`
- **Phase 10** 🔴 Character enter/exit (stretch)
- **Phase 11** 🔴 Docs finalization + PDF export

---

## Conventions / rules Claude should follow

- **Commit at every phase boundary** — offer to stage + commit; never commit without asking
- **All imports go to `Assets/ThirdParty/`** — never leave a pack at `Assets/` root; also add a row to `Doc/CREDITS.md` with source + license
- **Never skip pre-commit hooks or GPG signing** — investigate + fix instead
- **Use MCP for Editor changes** (scene, GameObjects, materials, prefabs) — reserve file-edit tools for `.cs` / `.md` / `.asmdef` / `.editorconfig`
- **After creating/editing scripts**, always `read_console` for compile errors before proceeding
- **Debug logs**: gate diagnostic Debug.Log lines behind a `[SerializeField] private bool _debugLog` flag so they can be disabled per-component without recompiling
- **Terse writing**: docs and comments follow the Unity style guide — no filler, no restating obvious code

---

## Cross-references

- Full requirements: [Doc/REQUIREMENTS.md](Doc/REQUIREMENTS.md)
- Layered architecture: [Doc/ARCHITECTURE.md](Doc/ARCHITECTURE.md)
- Phase-by-phase execution journal: [Doc/IMPLEMENTATION_LOG.md](Doc/IMPLEMENTATION_LOG.md)
- Cross-cutting optimization log: [Doc/OPTIMIZATION.md](Doc/OPTIMIZATION.md)
- Delta vs. bare URP 3D template: [Doc/PROJECT_SETUP.md](Doc/PROJECT_SETUP.md)
- Third-party attribution: [Doc/CREDITS.md](Doc/CREDITS.md)
- PDF export pipeline: [Doc/build-pdfs.ps1](Doc/build-pdfs.ps1)
