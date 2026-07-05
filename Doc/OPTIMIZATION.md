# Drift Assignment — Optimization Log

> Cross-cutting record of every performance decision — the ones with numbers,
> the ones that were "built right from day 1", and the ones that would be
> next if there were device access.

---

## 1 · Headline results (before → after)

### 1.1 · APK size (Android · IL2CPP · ARM64)

Captured from **Editor Log → Build Report** at the tip of the pre-optimization
commit and again after applying the 12-item optimization pass.

| Category  | Before  | After  | Δ | %   |
|-----------|--------:|-------:|--:|----:|
| **Total APK**     | **100.0 MB** | – MB | – | –% |
| Textures          | 80.3 MB (85.6%) | – MB | – | –% |
| Meshes            | 5.6 MB (5.9%)   | – MB | – | –% |
| Animations        | 0.0 KB (0.0%)   | – MB | – | –% |
| Sounds            | 3.2 MB (3.4%)   | – MB | – | –% |
| Shaders           | 3.1 MB (3.3%)   | – MB | – | –% |
| Other Assets      | 1.3 MB (1.4%)   | – MB | – | –% |
| Levels            | 0.0 KB (0.0%)   | – MB | – | –% |
| File headers      | 0.145 MB (0.2%) | – MB | – | –% |
| Total User Assets | 93.9 MB (100%)  | – MB | – | –% |
| Complete build    | 910.7 MB        | – MB | – | –% |

### 1.2 · In-editor render stats (60 s of driving + drifting via `StressTestCapture`)

Captured programmatically via `Assets/_Project/Scripts/UI/StressTestCapture.cs` — a
`ProfilerRecorder`-based sampler that runs while the driver plays. Two runs, one at
the pre-optimization commit and one at the post-optimization commit; raw sample
JSON lives in `Doc/stress-capture/`. All numbers below are the `avg` field unless
noted; `p99` in parens for peak load.

| Metric | Before (avg / p99) | After (avg / p99) | Δ avg |
|---|--:|--:|--:|
| FPS                    | 90.8 / 107.7 | – / – | – |
| Batches                | 168 / 227    | – / – | – |
| Draw calls             | 174 / 234    | – / – | – |
| SetPass calls          | 53 / 68      | – / – | – |
| Triangles              | 117 k / 144 k| – / – | – |
| Vertices               | 102 k / 123 k| – / – | – |

### 1.3 · Profiler capture (60 s of driving + drifting via `StressTestCapture`)

Same source JSON as §1.2 — the two "sections" split logically for readability.
Baseline sample-count 5,404 frames over 60.0 s. Startup domain-reload spikes are
excluded by using `p50 / p99` rather than raw `min / max`.

| Metric | Before (avg / p99) | After (avg / p99) | Δ avg |
|---|--:|--:|--:|
| CPU main-thread ms        | 11.72 / 15.17 | – / – | – |
| Render thread ms          | *(marker unresolved in Editor — captured on-device only)* | – | – |
| Physics.Simulate ms       | 0.08 / 0.24 | – / – | – |
| GC.Alloc / frame (KB)     | 68 / 92     | – / – | – |
| System used memory (MB)   | 2820        | – | – |

---

## 2 · Optimization catalogue applied in the "after" build

Every row here was applied for the "after" numbers above. Each is a single
concrete change with a clear rationale and the observable metric it moves.

| # | Change | Category | Moves | Commit |
|---|---|---|---|---|
| 1 | Texture Compression → ASTC 6×6 (Android override) | APK, GPU mem | Textures MB, GPU memory | – |
| 2 | Audio Compression → Vorbis (long) / ADPCM (short) | APK, RAM | Sounds MB, RAM at play | – |
| 3 | Managed Code Stripping → High | APK, startup | Scripts MB, cold-start | – |
| 4 | Unused-asset purge (Effect pack samples, Rotary "_on" clips, TMP EmojiOne, docs) | APK | Textures + Sounds MB | – |
| 5 | Mesh Compression → Medium on RMCar26 FBX | APK, GPU mem | Meshes MB | – |
| 6 | Graphics APIs pruned to Vulkan + GLES3 only | APK, startup | Included DLLs MB, startup | – |
| 7 | Split APK by architecture → ARM64 only | APK | ~30% reduction alone | – |
| 8 | Static Batching flag on environment renderers (walls, road, dunes terrain, props) | Draw calls | Batches, SetPass | – |
| 9 | Occlusion Culling bake | Draw calls, GPU | Triangles behind terrain | – |
| 10 | URP asset shadow distance 150m → 60m | GPU, CPU | Render thread ms | – |
| 11 | Sprite Atlas for HUD icons (12 → 1 draw) | Draw calls | UI draw calls | – |
| 12 | GC allocation audit (dent, mixer, HUD hot paths) | CPU, mem | GC.Alloc / frame | – |

---

## 3 · Top 10 biggest assets in the baseline build

Copied verbatim from the Editor Log after the baseline build. Kept for
context so the reader sees which assets we tackled vs. accepted.

| # | Path | Size | Action | Reasoning |
|---|---|--:|---|---|
| 1 | – | – | – | – |
| 2 | – | – | – | – |
| 3 | – | – | – | – |
| 4 | – | – | – | – |
| 5 | – | – | – | – |
| 6 | – | – | – | – |
| 7 | – | – | – | – |
| 8 | – | – | – | – |
| 9 | – | – | – | – |
| 10 | – | – | – | – |

---

## 4 · Decisions we made from day 1 (not "before/after" — built right)

Not every perf-friendly choice earned an "after" row because it was never
done the wrong way to begin with. Called out here so the interviewer sees
the design constraints that shaped the codebase throughout.

- **6 Assembly Definitions** with enforced dependency direction
  (`Core ← Input ← Vehicle ← UI/Camera/Character`, `Damage → Core`).
  Isolates recompile scope — a change in `UI` doesn't recompile physics.
- **Event-driven HUD**. `HudReadouts` subscribes to `CarController.SpeedChanged`
  / `GearChanged` / `RpmChanged` — no `Update()` polling. Zero cost when
  values don't change.
- **ScriptableObject config throughout** — `CarConfig`, `DamageConfig`,
  `TuningState`, `SoundLibrary`. Designer-editable, hot-swappable at runtime,
  no code recompile for tuning. Runtime tuning UI (Settings panel) writes
  directly to the SO.
- **Cinemachine priority pattern** for camera cycling. Only the active cam's
  Update runs; inactive cams cost ~0.
- **PaintDamage clone-material trick.** Runtime paint damage texture lives
  on ONE shared material clone that's assigned to all 34 body renderers —
  not per-renderer clones. **Saved ~132 MB of texture memory**
  (34 renderers × 4 MB damage tex vs. 1 shared × 4 MB).
- **LOD force to LOD0** on the car body. RMCar26 has 5 LOD levels × ~40
  sub-renderers each = 182 renderers total; we disabled 135 of them at
  startup so only 47 render. Predictable draw-call count, no LOD cross-fade
  cost. Justified for a single-hero-car camera-focused demo.
- **Detachable-part despawn** at 8 s. Bounded rigidbody count during long
  play sessions (~15 loose bodies peak).
- **Skid trail lifetime cap** at 8 s per wheel. Bounded runtime mesh count.
- **Ground-contact filter in `ImpactReceiver`.** Rejects contacts whose
  normal is within 40° of world-up (car belly / wheel bounce during drift).
  Spares paint damage + spark VFX + audio + cascade routing every time you
  throw the car sideways at low ride height.
- **4-source engine mixer** with triangular RPM envelopes + speed-blend.
  Bounded voice count vs. a per-blip PlayOneShot approach that would fire
  unbounded voices at high engagement.
- **Modular SO impact banks** with `PickRandom` — no per-frame allocation
  in the impact hot path.
- **URP post-processing stack** hand-picked for mobile: Bloom + Tonemap +
  Color Adjustments + Vignette + Film Grain. **Motion Blur / DoF /
  Chromatic Aberration explicitly disabled** — Phase 8 spec calls these
  out as mobile-GPU killers.
- **FBX Read/Write flags** flipped only on the mesh that needs runtime
  vertex mutation (RMCar26 FBX) and the paint texture (RMCar26PaintAo).
  All other meshes stay GPU-only. The Read/Write toggle is a documented
  memory trade (per-mesh CPU-side copy) that we opted into deliberately for
  the dent + paint spoilage features.

---

## 5 · Not measured on-device (deferred verification)

No Android device is available during this pass. These would be the next
round of validation if there were:

- **Actual FPS on a mid-tier 2022+ Android device** at 720p and 1080p
- **GPU render time per frame** during hard drifts on-device
- **Memory pressure over a 5-minute session** (Android Studio Profiler)
- **Battery drain per 10 minutes** of continuous play
- **Startup time** (splash → first drivable frame) on-device
- **APK install size vs. download size** post-Play-Store optimization

Verifiable by any reviewer with the built APK + `adb`.

---

## Appendix A · How the numbers were captured

**Baseline:**
1. Made an Android IL2CPP + ARM64 build with no optimization pass applied.
2. Opened Editor Log (`Console → Editor Log`); scrolled to "Build Report".
3. Copied the size breakdown + top-N asset list into §1.1 and §3.
4. Opened Game view stats overlay, entered Play, drove the car through a
   drift sequence for 10 s, took the average visible values into §1.2.
5. Opened Profiler (`Window → Analysis → Profiler`), Recorded for ~10 s of
   driving + drifting, read the averages into §1.3.

**After:**
- Same 5 steps at the tip of commit `<opt-pass-hash>`, on the same machine,
  same Editor version.

Screenshots of both build reports and both profiler captures are included
in `Doc/pdf/` for the PDF export.
