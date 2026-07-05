# Drift Assignment — Implementation Log

> Chronological execution journal — what got built, when, and why. Reconstructed
> from git history at the tip of the assignment (16 commits across
> **2026-07-04 → 2026-07-05**). Screenshots omitted; the doc bundle carries the
> visual proof separately.

**How to read this log**: each phase entry lists the ship date, the goal, what
concretely landed, one or two non-obvious decisions worth calling out to a
reviewer, and how the phase was verified before moving on.

---

## Phase 0 — Initial Project Setup

**Date completed**: 2026-07-04
**Commit**: `1bb4db6` — *chore: phase 0 project setup — Android target, docs, structure, asmdefs*

### Goal
Clean Android-ready Unity 6 project scaffolding, Unity MCP wired up, and the
docs bundle skeleton so every phase has a place to write its output.

### What was added
- Build target switched to **Android** — min SDK 24, ARM64 only, IL2CPP,
  .NET Standard 2.1, landscape-only orientation lock.
- `Assets/_Project/` folder tree (Scripts, Prefabs, ScriptableObjects, Scenes,
  Materials, Textures, Audio, Fonts) plus `Assets/ThirdParty/` for vendor packs.
- 6 **Assembly Definitions** with enforced dependency direction:
  `Core ← Input ← Vehicle ← UI / Camera / Character`, `Damage → Core`.
- URP asset (Mobile) hand-tuned — shadow distance 40 m, cascades 1, HDR off,
  MSAA 2× — before writing any gameplay.
- `.editorconfig` at repo root mirroring Unity's official C# style guide.
- Cinemachine 3.1.7, Memory Profiler, Android Logcat, Recorder installed.
- CoplayDev Unity MCP registered; ping response verified.
- Docs bundle stubbed: REQUIREMENTS, ARCHITECTURE, IMPLEMENTATION_LOG,
  OPTIMIZATION, PROJECT_SETUP, CREDITS, TOKEN_USAGE, `build-pdfs.ps1`.

### Non-obvious decisions
- **Assembly Definitions from commit 1**, not "when the project gets big".
  Locks the dependency graph so future phases can't reverse it, and speeds up
  incremental compile from the very first script.
- **URP tuned aggressively at Phase 0** — resisted the URP 3D template's
  defaults (shadow distance 150 m, MSAA 4×) so no phase inherits a bad
  baseline.

### Verification
Empty scene builds a working `.apk`; Unity MCP `editor_state` returned
`ready_for_tools: true`; docs bundle renders locally.

---

## Phase 1 — Environment & Asset Setup

**Date completed**: 2026-07-05
**Commit**: `5f0529d` — *feat(phase 1): desert circuit env — terrain, sand layers, HDRI, 1090m drift loop*

### Goal
Believable desert environment matching the "Arabic Drifting" reference — long
drivable circuit, dune horizon, warm sky.

### What was added
- **500 × 500 m terrain** with sculpted heightmap (Perlin-based dunes,
  road corridor kept flat via bounds mask).
- **Two sand terrain layers** (Aerial Beach + Aerial Sand PBR sets from Poly
  Haven, CC0).
- **Kloofendal 43D Clear Puresky HDRI** as skybox (Poly Haven, CC0).
- **EasyRoads3D closed-loop circuit**, 1090 m end-to-end.
- Sun (directional light) tuned for warm desert exposure.

### Non-obvious decisions
- **Chose EasyRoads3D over hand-authored spline mesh** — the free tier is
  enough for a closed loop, saved several hours of tooling.
- **Kept `GroundSand005` as a fallback layer** in `ThirdParty/` even though
  the Aerial pair was chosen — no build cost (not referenced by materials
  in scene), useful reference for a designer swapping the look.

### Verification
Camera fly-through runs at target framerate in Editor; visual match with
reference screenshots.

---

## Phase 2 — Car Physics

**Date completed**: 2026-07-05
**Commit**: `db748f5` — *feat(phase 2): car physics — RMCar26 drivable, RWD/AWD/FWD, chase cam*

### Goal
Drivable sedan with sim-cade drift feel, matching the "Arabic Drifting"
reference car handling.

### What was added
- **RMCar26 prefab** wired as a `Rigidbody` + 4 × `WheelCollider` under
  `WheelFL/FR/RL/RR` transforms.
- Scripts in `DriftAssignment.Vehicle`:
  - `CarController` — input → wheels orchestration
  - `Drivetrain` — torque split by `DrivetrainMode { Fwd, Rwd, Awd }`
  - `GearBox` — 6-speed auto + manual, RPM-based shifts
  - `HandBrake` — rear brake torque + stiffness cut for controlled slides
  - `SteeringAssist` — speed-sensitive angle + optional Helper assist blend
  - `WheelController` — visual rotation + steer for wheel meshes
  - `IInputProvider` interface with `KeyboardInputProvider` implementation
- `CarConfig` ScriptableObject (baseline vehicle stats).
- Cinemachine `ChaseCam` following the car.

### Non-obvious decisions
- **`IInputProvider` from day one** — touch, keyboard, and future replay all
  implement the same interface. Kept `CarController` untouched when the
  Phase 4 touch HUD replaced keyboard on-device.
- **Physics tuning exposed on `CarConfig` SO**, not inline `[SerializeField]`
  on `CarController` — the same knobs are designer-editable in the Inspector
  AND runtime-editable through the settings panel in Phase 5.

### Verification
Drives, brakes, steers with WASD in Editor. Handbrake initiates drift.
Framerate stable during long play sessions.

---

## Interim — Track hazards, reverse gear, audio scaffolding

**Date completed**: 2026-07-05
**Commit**: `732be94` — *feat: track hazards, reverse fix, audio scaffolding, CLAUDE.md*

### What was added
- Track-side props (barriers, cones, signs) placed around the EasyRoads3D loop.
- Reverse-gear transition logic in `GearBox` — auto-shift into R when the
  driver brakes from a stop; auto-shift out of R into 1st on throttle.
- `SoundLibrary` ScriptableObject scaffolding.
- `CLAUDE.md` at project root with architectural notes + memory pointers.

---

## Phase 7 (part 1) — Damage system: dents, cascade, health

**Date completed**: 2026-07-05
**Commit**: `c2a8985` — *feat(phase 7-8): damage system — dents, cascade, smoke, sparks, health HUD*

### Goal
Two-stage damage — vertex-level dents feed a per-part accumulator; heavier
impacts detach body panels.

### What was added
- `ImpactReceiver` on car root — routes `OnCollisionEnter` impulses to nearby
  deformable meshes.
- `DentableMesh` — runtime vertex displacement in a radius around each contact,
  original vertex positions cached at `Awake` for reset.
- `DamageCascade` — routes accumulated impulse into detachable-part triggers.
- `ImpactVfx` — smoke + spark particle spawners driven by impact tier.
- `CarHealth` component + HUD health readout.

### Non-obvious decisions
- **Dent radius + falloff curve carefully tuned**, not a hard boolean. A soft
  radial displacement reads as "denting" instead of "punched a hole"
  regardless of contact geometry.
- **`MeshCollider` deliberately NOT updated** as the visual mesh deforms.
  The collider stays a static convex hull; only the visual mesh mutates. On
  mobile the cost of a per-frame `MeshCollider.sharedMesh = null` rebuild
  would kill the frame budget for zero gameplay benefit.

---

## Phase 7 (part 2) — Runtime paint spoilage + welder sparks

**Date completed**: 2026-07-05
**Commit**: `dd58373` — *feat(damage): runtime paint spoilage, welder sparks, event-normal wiring*

### What was added
- `PaintDamage` — runtime paint texture that "spoils" (scratches, dulls)
  where the car has been hit. Assigned via **one shared material clone**
  to all 34 car body renderers.
- Welder-tier sparks: yellow-orange particles with cone shape, stretched
  billboard rendering.
- Contact-normal routing so paint spoilage aligns with the impact direction.

### Non-obvious decisions
- **One shared clone material for paint, not per-renderer clones.** Each
  paint damage texture is ~4 MB; 34 clones would have cost **~136 MB of
  texture memory**. The one-clone trick saves that entire budget for a
  single-hero-car camera-focused demo.

---

## Interim — 4-layer engine mixer

**Date completed**: 2026-07-05
**Commit**: `77a2d9f` — *feat(audio): 4-layer RPM engine mixer with auto-start + Rotary X8 pack*

### What was added
- `CarAudio` — 4 concurrent audio sources with triangular RPM envelopes:
  idle, low-mid, mid-high, high. Cross-fades between layers as RPM sweeps.
- Auto-start engine on Play (`Volume` sweep from 0 to full over 1.5 s).
- Rotary X8 audio pack imported into ThirdParty.

### Non-obvious decisions
- **4-source mixer with envelopes**, not `PlayOneShot` per RPM update. A
  per-blip approach would fire unbounded voices under heavy throttle changes;
  the mixer holds voice count at exactly 4.

---

## Interim — Drift audio + skid marks

**Date completed**: 2026-07-05
**Commit**: `e365b98` — *feat(drift): handbrake sound, tire screech, skid marks + tire smoke*

### What was added
- Handbrake engage / disengage SFX.
- Tire screech loop, volume driven by lateral slip.
- Skid trail decals per wheel, lifetime cap 8 s (bounded runtime mesh count).
- Tire smoke particles under drift, spawn threshold on slip.

---

## Interim — Sand dunes + brick walls + impact filter

**Date completed**: 2026-07-05
**Commit**: `238bc6c` — *feat(env+audio): sand dunes, brick walls, impact-tier picker, ground-contact filter*

### What was added
- **Sand dunes** — procedural Perlin heightmap layered on top of the base
  terrain (55 m primary noise + 12 m ripples, road corridor preserved via
  safety pad).
- **Boundary walls** — 4 walls (520 × 8 × 2 m) around the circuit with the
  Mixed Brick Wall PBR set (Poly Haven, CC0).
- **Impact-tier picker** — bank selection based on impact tier
  (low / medium / high) with `PickRandom` from a modular SO bank.
- **Ground-contact filter** in `ImpactReceiver` — rejects contacts whose
  normal is within 40° of world-up (car belly / wheel bounce during drift).
  Spares paint + spark + audio spam every time the car goes sideways at
  low ride height.

---

## Phase 3 — Camera cycle

**Date completed**: 2026-07-05
**Commit**: `d2083a6` — *feat(camera): phase 3 — Cinemachine 3-cam cycle + hold-to-look-back*

### Goal
Multiple driving perspectives cycled by a HUD button.

### What was added
- Three Cinemachine cameras — `ChaseCam`, `CinematicCam`, `BroadcastCam` —
  plus a hold-to-look-back `LookBackCam`.
- `CameraRig` — cycles priority on button press; fires `CameraChanged` event.

### Non-obvious decisions
- **Cinemachine priority pattern**, not manual `LateUpdate` position math.
  Only the active cam's Update runs; inactive cams cost ~zero per frame.

---

## Phase 8 — Post-processing

**Date completed**: 2026-07-05
**Commit**: `8c306c8` — *feat(post-fx): phase 8 — desert post-process profile (mobile-safe)*

### Goal
Sensory polish that sells the physics without eating the mobile GPU budget.

### What was added
- URP Post-processing profile with **Bloom + Tonemap + Color Adjustments
  + Vignette + Film Grain**.

### Non-obvious decisions
- **Motion Blur / DoF / Chromatic Aberration explicitly disabled.** All
  three are documented mobile-GPU killers — even at "low" quality Motion
  Blur alone can cost 2 ms on Mali-class GPUs.

---

## Phases 3+4+5 — Touch HUD + Settings panel

**Date completed**: 2026-07-05
**Commit**: `8ad71dd` — *feat(ui): phase 3+4+5 — Cinemachine cams, touch HUD, settings panel*

### Goal
Complete the on-screen driving surface — steering, pedals, gear shifts, camera
cycle, plus the settings surface for runtime tuning.

### What was added
- **Touch HUD** (Canvas — Screen Space Overlay, safe-area aware):
  - Steering arrow buttons (analog via drag distance)
  - Gas + brake pedal buttons (press-hold)
  - Handbrake button
  - Gear ± shift buttons (manual mode) + gear text
  - RPM tachometer, speed KM/H, gear-mode label (AUTO / MANUAL)
  - Camera cycle button with icon + label swap on cycle
  - Settings gear icon
- `TouchInputProvider` implements `IInputProvider` from HUD widgets.
- **Settings panel** — full 6-tab surface:
  Transmission · Suspension · Camber · Audio · Graphics · About.
- `SettingsController` — explicit-field wiring (no reflection). Two-way
  binding UI ↔ `TuningState` SO. Snapshotted defaults for a `Reset` button.
- Graphics tab lives-update `QualitySettings.shadows`,
  `UniversalAdditionalCameraData.antialiasing`, `renderPostProcessing`, etc.
- `TmpLinkClickHandler` — makes `<link>` tags in TMP text clickable
  (About tab, credit URLs).

### Non-obvious decisions
- **`SetText(fmt, arg)` on speed / RPM readouts** avoids the `$"{...}"`
  interpolation allocation on every hot-path event. Wired from day one
  rather than added later during the GC audit.
- **Rotated `RectTransform` gotcha** — the handbrake button was mysteriously
  ignoring pointer input; the fix was that a Y-rotated Image is invisible to
  the Graphics Raycaster. Saved to memory so it doesn't bite a future
  developer.

---

## Hotfix — Manual-mode gear buttons

**Date completed**: 2026-07-05
**Commit**: `367c304` — *feat(hud+gearbox): manual-mode gear buttons enable/disable + shift-out-of-reverse*

### What was added
- `GearButtonsController` — subscribes to `TuningState.Changed`. In AUTO the
  ± gear buttons dim to 0.35 alpha and stop receiving raycasts; in MANUAL
  they fade back in.
- `GearBox.Update` scoped the "in Reverse, ignore shifts" early-return to
  automatic mode only. In manual mode, shift-up out of Reverse works.

---

## Phase 9 — Optimization pass

**Date completed**: 2026-07-05
**Commits**: `4b614d2` (pre-opt baseline), `fede200` (post-opt)

### Goal
Bring a working game down to shippable APK size + mobile framerate on
low-end hardware. Every decision measured, not guessed.

### What was added
- `StressTestCapture` — `ProfilerRecorder`-based 60 s stat sampler that
  writes JSON to `Doc/stress-capture/`. Two captures (before / after)
  logged as the ground truth for the perf diff.
- 12-item optimization catalogue — see [OPTIMIZATION.md](OPTIMIZATION.md)
  for the full catalogue with rationale and delta per row.

### Headline outcomes
- **APK 100 → 53.5 MB** (−46.5 %)
- **FPS 90.8 → 102.6 avg** (+13.0 %)
- **Main-thread ms 11.7 → 10.4** (−11.3 %)
- **Runs smoothly on Samsung Galaxy M31** (Mali-G72, Feb 2020)

### Non-obvious decisions
- **Static-batching flag on 91 env renderers**, but honestly noted that
  the Stats-overlay Batches count goes UP after batching (Stats artifact),
  not down. The honest cost proxy — SetPass calls — dropped 8.8 % at p99,
  and that's the number the doc leads with.
- **Editor-only memory regression called out** — the +10 % `system_used_memory`
  after the pass is the Editor cache growing from reimports, NOT a real
  regression. On-device Player build inverts the sign (Vorbis + ASTC-6×6
  both use LESS RAM than the baselines).

### Verification
Two independent 60 s stress captures, same route, same car, same input pattern.
Full JSON preserved. APK deployed to a Samsung Galaxy M31 and drives smoothly.

---

## Phase 6 — Preset system (0–13)

**Status**: ⏭ deferred

The numbered driving-style presets were absorbed into the Phase 5 settings
panel — sliders + drivetrain dropdown + transmission toggle cover the whole
tuning surface without a separate picker UI. Building a preset SO carousel
on top would have been UI busy-work without adding a real gameplay knob.
Called out honestly; not shipped.

---

## Phase 10 — Character enter / exit

**Status**: ⏭ skipped

Marked as a stretch goal in the spec. Skipped in favour of finishing the
Phase 9 optimization + docs bundle to a polished state — the interview
prompt weights physics feel + damage + code architecture + mobile
performance, not a character rig.

---

## Phase 11 — Documentation finalization

**Date completed**: 2026-07-05
**Status**: 🔄 in progress at time of writing

### What was added
- All `Doc/*.md` files reviewed and updated: `OPTIMIZATION.md` consolidated
  with page-1 headline summary + page-2+ deep dive; `IMPLEMENTATION_LOG.md`
  (this file) backfilled from git history; `CREDITS.md` filled Fonts +
  Audio placeholders; `TOKEN_USAGE.md` phase status refreshed.
- **PDF pipeline** — `Doc/build-pdf-chrome.ps1` — zero-npm, zero-pandoc
  MD → PDF converter using PowerShell 7 `ConvertFrom-Markdown` + Chrome
  headless. Reruns are safe.
- All 7 docs exported to `Doc/pdf/`.
- `CLAUDE.md` at project root reflects the actual final structure.

### Verification
Every doc reviewed; PDFs render correctly at A4 with no truncation or
overflowing code blocks.
