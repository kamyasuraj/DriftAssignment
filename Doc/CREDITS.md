# Credits & Third-Party Attribution

Every asset shipped in this project is listed below with its source and license. Add a row when importing a new asset — do not defer.

## Assets

| Asset | Source | License | Used in |
|---|---|---|---|
| **Realistic Mobile Car #26 (Demo)** — RMCar26 prefab + Wheel_A/B/C rims + shared shaders/materials from `Main/` | [Unity Asset Store — Surdov Vadym](https://assetstore.unity.com/packages/3d/vehicles/land/realistic-mobile-car-26-demo-305319) | Unity Asset Store EULA (free demo of paid series) | Hero car — physics, damage system (dent/break), enter/exit anims. Path: `Assets/ThirdParty/RealisticMobileCars - Pro3DModels/` |
| **EasyRoads3D Free v3** — road-building Editor extension + demo scenes + Terrain Assets | [Unity Asset Store — AndaSoft](https://assetstore.unity.com/packages/tools/terrain/easyroads3d-free-v3-987) | Unity Asset Store EULA (free tier) | Building the desert circuit (Phase 1). Spline-based road extrusion, terrain-aware. Path: `Assets/ThirdParty/EasyRoads3D/` + `Assets/ThirdParty/EasyRoads3D scenes/` |
| **Aerial Beach 01** — 2K PBR texture set (diff/nor_gl/rough/ao/arm/disp) | [Poly Haven](https://polyhaven.com/a/aerial_beach_01) | CC0 | Terrain texture layer for the desert circuit (Phase 1). Path: `Assets/ThirdParty/AerialBeach/` |
| **Aerial Sand** — 2K PBR texture set (diff/nor_gl/rough/ao/arm/disp) | [Poly Haven](https://polyhaven.com/a/aerial_sand) | CC0 | Terrain texture layer for the desert circuit (Phase 1). Path: `Assets/ThirdParty/AerialSand/` |
| **Kloofendal 43D Clear Puresky** — 2K HDRI | [Poly Haven](https://polyhaven.com/a/kloofendal_43d_clear_puresky) | CC0 | Scene skybox for the desert circuit (Phase 1). Path: `Assets/ThirdParty/SkyBox/` |
| **GroundSand005** — 2K PBR texture set (COL/BUMP/AO/DISP + 16-bit variants) | [ambientCG](https://ambientcg.com/) (or 3DAssets) | CC0 | Alternative sand texture set kept as fallback. Path: `Assets/ThirdParty/GroundSand005/` |
| **Simple Street Props** — 23 prefabs (concrete barriers, traffic cones, water/traffic barrels, road signs, speed breakers, dumpsters/bins) | [Unity Asset Store](https://assetstore.unity.com/packages/3d/props/simple-street-props-194706) | Unity Asset Store EULA (Free) | Track-side props along the DriftCircuit (Phase 1 polish). Path: `Assets/ThirdParty/Simple Street Props/` |
| **Road Props for Games — Diffuse Map Atlas LP** — 25 prefabs (barrier fences, bollards, road signs, borders, tetrapods, urns, bench, speed bumps) | [Unity Asset Store](https://assetstore.unity.com/packages/3d/environments/roadways/road-props-for-games-diffuse-map-atlas-lp-238835) | Unity Asset Store EULA (Free) | Track-side props along the DriftCircuit (Phase 1 polish). Path: `Assets/ThirdParty/Road props for games/` |
| **Sonniss GDC 2024 Audio Bundle (Part 8) — curated subset**: Pole Position Porsche Carrera GT (4 engine clips), Wavemotion Rally Legend (4 rally clips), Mechanical Wave Torturing Metal (4 crash/screech clips), Mechanical Wave Glass Break, DavidDumais Car Explosion, CB Sounddesign Activation 2 (4 UI clicks) | [Sonniss GDC Bundle](https://sonniss.com/gameaudiogdc) | Royalty-free, no attribution required (attribution kept anyway) | Engine, drift/rally SFX, crash/glass/metal impacts, UI. Path: `Assets/ThirdParty/Sonniss/GDC2024/` — 18 wav files, ~284 MB |
| **Kenney Impact Sounds — curated subset** (55 files): impactGlass/Metal/Plate (light/med/heavy variants), impactGeneric_light, footstep_concrete | [Kenney](https://kenney.nl/assets/impact-sounds) | CC0 | Per-collision impact banks — randomized clip per hit. Path: `Assets/ThirdParty/KennyImpactAudio/` |

## Fonts

| Font | Source | License | Used in |
|---|---|---|---|
| **LiberationSans SDF** (+ Fallback) | Ships with Unity's TextMeshPro package (Google's Liberation Sans) | SIL Open Font License 1.1 | Every HUD and Settings-panel `TMP_Text` — no custom font imported |

## Audio

All audio credits are consolidated in the **Assets** table above (Sonniss GDC
2024 subset + Kenney Impact Sounds subset). No separate audio-only assets are
referenced. All AudioClips ship with per-clip Android platform overrides
(Vorbis for clips > 3 s, ADPCM for shorter samples) — see
[OPTIMIZATION.md §2](OPTIMIZATION.md).

## Code / Reference

| Repo | Source | License | Used as |
|---|---|---|---|
| _Prometeo Car Controller_ | _https://github.com/Mecanik/PrometeoCarController_ | _MIT_ | _Reference only — no code copied_ |

---

**Rule**: no asset ships without an entry above. When in doubt about license, leave it out.
