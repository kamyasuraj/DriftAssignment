# Drift Assignment — How to Play

*A one-page reference to every on-screen control and every setting.*

---

## Getting into the game

Install the APK on an Android device (min Android 7.0 / API 24, ARM64) and
launch. You'll spawn at the start of the desert circuit, chase camera behind
the car, engine idling. Everything below is playable from the first frame —
no menu clicks required.

**Editor testing?** Use the **keyboard fallback**:
`W` throttle · `S` brake · `A / D` steer · `Space` handbrake · `Q / E` shift down / up (manual).

---

## Touch HUD — on-screen controls

The HUD is anchored to safe-area edges and mirrors a real driving cockpit.
Nothing polls in `Update()` — every widget only redraws when its value changes.

### Bottom-left cluster — steering + handbrake

| Widget | What it does |
|---|---|
| **← Left arrow** | Steers left. Drag distance across the button becomes analog steering angle — a soft press turns gently, a full press turns hard. Release to auto-center. |
| **→ Right arrow** | Same behaviour, opposite direction. |
| **Handbrake (bottom-center)** | Press and hold to cut rear-wheel drive torque and drop rear-tire stiffness — the classic drift-initiation input. Release to grip up again. |

### Bottom-right cluster — pedals + gears

| Widget | What it does |
|---|---|
| **Gas pedal** | Press and hold to apply throttle. Higher throttle spins the engine to the RPM redline (visible on the RPM readout). |
| **Brake pedal** | Press and hold to brake. If the car is at a full stop and you keep braking, the gearbox auto-shifts into **Reverse**. |
| **Gear + / − buttons** | Only interactive in **manual transmission** — dimmed and non-clickable in auto. `+` shifts up, `−` shifts down. In manual you can shift OUT of Reverse with `+`. |

### Top-right cluster — camera + settings

| Widget | What it does |
|---|---|
| **Camera button (icon swap)** | Tap to cycle through 3 cameras: **Chase** (default follow) → **Cinematic** (orbit + look-ahead) → **Broadcast** (wide sideline). The icon and label update to show the current one. **Hold** the button to trigger the rear-facing look-back cam; release to snap back. |
| **Settings ⚙** | Opens the settings panel (see below). Game pauses (`Time.timeScale = 0`) while it's open. |

### Center readouts

| Widget | Meaning |
|---|---|
| **GEAR** | `R` reverse · `N` neutral · `1`–`6` forward. |
| **AUTO / MANUAL** | Small label next to the gear text showing current transmission mode. |
| **SPEED** | Live km/h from the `Rigidbody`. |
| **RPM** | Engine speed. Updates via a smoothed value driven by wheel RPM × gear ratio. |

---

## Settings panel — 6 tabs

Tap the **⚙** icon to open. Every slider / toggle takes effect **immediately**
— no "Apply" button needed. Tap the **backdrop** or the **✕** to close.

### 1 · Transmission
| Control | What it does |
|---|---|
| Automatic ↔ Manual toggle | Switches shifting logic. In **Auto**, the gearbox picks the shift point from throttle + RPM. In **Manual**, the ± HUD buttons become active and shifting is entirely up to you. |

### 2 · Suspension
| Control | What it does |
|---|---|
| Front spring force | Higher = stiffer front, less nose-dive under braking, more understeer. |
| Rear spring force | Higher = stiffer rear, more traction on power exits but harder to initiate drift. |
| Front / rear ride-height | Lifts or lowers the chassis on that axle — affects centre of mass. |

### 3 · Camber
| Control | What it does |
|---|---|
| Front camber angle | Tilts the tops of the front wheels in / out. Aggressive negative camber increases cornering grip at the cost of straight-line tire wear. |
| Rear camber angle | Same on the rear axle — a common drift setup is mild negative rear camber for a controllable slide. |

### 4 · Audio
| Control | What it does |
|---|---|
| Master volume | Global gain on `AudioListener`. |
| Engine / SFX / UI sub-mixers | Individual gain lines for the engine 4-source mixer, gameplay SFX (screech, impacts, wind), and UI clicks. |

### 5 · Graphics
| Control | What it does |
|---|---|
| Quality preset | Switches Unity's Quality Level (Low / Medium / High). Affects shadow distance, texture streaming budget, and LOD bias. |
| Anti-aliasing | Off / FXAA / SMAA on the URP camera. FXAA is the mobile-safe default. |
| Post-processing on/off | Toggles the URP volume (Bloom + Tonemap + Color Adjustments + Vignette + Film Grain). Motion Blur / DoF / Chromatic Aberration are always off for mobile GPU safety. |
| Shadow quality | Off / Hard-only (25 m, low res) / All (80 m, high res). Big lever on low-end phones. |

### 6 · About
Quick credits + links to the reference material and the docs bundle. Tap any
`<link>` to open in the system browser.

---

## Tips to actually drift

1. Get up to ~60 km/h in 2nd or 3rd gear.
2. Point the nose into the corner.
3. **Tap the handbrake briefly** to break the rear tires loose.
4. Counter-steer (opposite direction from the turn) to hold the slide.
5. Feed throttle progressively — too little and you spin out, too much and
   you snap back straight.
6. **Try the presets in Settings → Suspension** — stiffer rear + softer front
   makes the car much more drift-happy.

Damage feedback is live: light bumps leave dents, harder hits detach panels
(bumpers, doors, mirrors). The car keeps driving through anything short of
total loss — this is a demo, not a permadeath sim.
