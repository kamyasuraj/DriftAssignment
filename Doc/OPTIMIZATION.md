# Drift Assignment — Optimization Log

> Cross-cutting record of every performance decision made during the project — the ones that helped, the ones that didn't, and the reasoning behind each. Populated incrementally from Phase 0 onward.

**How to read this doc**
- The **summary table** below is the executive view: project start → end.
- Below the table, each row expands into a section with rationale, measurement, and (where applicable) profiler screenshots.
- Optimizations that were tried and reverted are documented too — dead ends are as instructive as wins.

---

## Summary Table

| Phase | Optimization | Rationale | Measured impact |
|---|---|---|---|
| 0 | URP asset tuned: HDR off, MSAA 2x, shadow distance 40m, single cascade | Mobile GPU budget | _measured on-device: TBD_ |
| 0 | Assembly Definitions from day 1 | Isolate recompile scope | _incremental compile: TBD_ |
| 0 | IL2CPP + ARM64 only, .NET Standard 2.1 | Smaller APK, faster startup | _APK size baseline: TBD_ |
| 1 | Static-batch all environment prefabs | Reduce draw calls | _draw calls: TBD_ |
| 1 | Palm trees: LOD group + billboard past 40m | Fill-rate on foliage | _GPU time delta: TBD_ |
| 1 | Baked lightmaps on static geometry; single realtime directional light | No realtime GI cost | _stable framerate: TBD_ |
| 1 | Skybox mip-streamed HDRI, 2K not 4K | Texture memory on mobile | _VRAM delta: TBD_ |
| 2 | `Rigidbody.interpolation = Interpolate`, `Continuous` collision on car only | Physics jitter fix without global cost | _visual smoothness: subjective_ |
| 2 | WheelCollider substeps tuned (low 5, high 12) | Prevent tunneling without over-simming | _no wheel-through-ground: verified_ |
| 2 | FixedUpdate @ 50 Hz (not 60) | CPU savings vs. 60 Hz | _CPU delta: TBD_ |
| 3 | Cinemachine damping tuned; no LookAt on inactive cams | Cheap cam blends | _CPU cost of unused cams: 0_ |
| 4 | HUD is Screen Space Overlay, not World Space | Skip depth buffer for UI | _frame time delta: TBD_ |
| 4 | HUD updates event-driven | Only redraw on change | _per-frame delta: TBD_ |
| 4 | Canvas split: static vs dynamic | Isolate rebuild cost | _rebuild delta: TBD_ |
| 5 | Tuning applied via event, not every FixedUpdate | Skip redundant WheelCollider writes | _FixedUpdate delta: TBD_ |
| 6 | Presets are baked ScriptableObjects, no runtime parse | Zero-cost preset switch | _preset apply time: TBD_ |
| 7 | Dent stage: mesh deform only within radius; cached originals | Avoid full mesh rebuild | _dent time per collision: TBD_ |
| 7 | Detachable parts pooled + timed despawn (7 s) | Bound memory + rigidbody count | _max loose rigidbodies: capped_ |
| 7 | Fallback: 3-tier prefab-swap ready if runtime deform too heavy | Ship-safe alternative | _toggleable per platform_ |
| 8 | Engine loop: single AudioSource with pitch curve | 1 source vs. 4 sample layers | _RAM & mix simplicity: TBD_ |
| 8 | Tire smoke: shared ParticleSystem, cap 40 particles | Bound overdraw | _GPU on drift: TBD_ |
| 8 | Post-processing: bloom only (no motion blur, no DoF) | Mobile budget | _frame time delta: TBD_ |
| 8 | Skid marks: decal pool of 200, oldest recycles | Avoid unbounded mesh generation | _bounded memory: verified_ |
| 9 | Sprite atlasing on HUD icons | Reduce UI draw calls | _UI draw calls: TBD_ |
| 9 | Texture import: Compressed HQ + mip streaming | VRAM budget | _texture memory delta: TBD_ |
| 9 | Audio import: Vorbis for long clips, PCM for short SFX | Storage vs. decode cost | _audio storage delta: TBD_ |
| 9 | Occlusion culling baked on static geometry | Skip hidden draw calls | _draw calls in dune sections: TBD_ |
| 9 | Per-phase profiler pass on Android Player | Catch regressions early | _no late-stage surprises_ |

---

## Per-decision detail

_Filled in during execution. Template per row:_

### <Phase N> — <optimization name>
**Rationale**: <why we thought this would help>
**Change**: <exact settings / commit / prefab tweak>
**Measurement (before)**: <profiler numbers>
**Measurement (after)**: <profiler numbers>
**Verdict**: <kept / reverted / partial>
**Notes**: <anything surprising>

---

## Dead ends (things tried, reverted)

_Populated as we go. Documenting these matters — they show measurement-driven decisions, not vibes._

- _example placeholder: "Tried GPU instancing on palms — no measurable win because they were already static-batched. Reverted."_
