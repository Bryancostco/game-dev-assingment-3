# MASTER UNITY 3D DELIVERY GAME — GENERATION PROMPT v2

## CONTEXT

I am building a Unity 3D game for a graded college assignment. The game is a **3D delivery driving game** where the player drives a vehicle to pick up packages and deliver them to designated drop-off zones within a city-style environment.

You must generate a **complete, production-style Unity project** with full C# scripts, architecture, setup instructions, and Inspector configuration steps. The result must compile without errors, follow clean architecture principles, and be fully playable from start to finish.

### Graded Deliverables (All Three Required)
1. **Playable Windows executable** — Built and zipped
2. **Documentation PDF** — Following a specific required structure (see Section 14)
3. **Source code** — All C# scripts via git link or zip

---

## 0. TECHNICAL ENVIRONMENT (MUST FOLLOW)

| Setting | Value |
|---|---|
| Unity Version | **2022.3 LTS** (or specify your version here) |
| Render Pipeline | **URP (Universal Render Pipeline)** |
| .NET Target | **.NET Standard 2.1** |
| Input System | **New Input System only** (com.unity.inputsystem) |
| Physics | **3D (PhysX)** — Rigidbody-based vehicle with wheel colliders |
| C# Convention | **PascalCase** for public members, **_camelCase** for private fields, XML doc comments on all public methods |
| Platform Target | **Windows Standalone (PC)** |

### Required Packages (via Package Manager)
- `com.unity.inputsystem`
- `com.unity.render-pipelines.universal`
- `com.unity.textmeshpro`
- `com.unity.cinemachine` (for camera)

---

## 1. CORE GAME DESIGN REQUIREMENTS

The game must include:

- **3D driving vehicle controller** using Unity New Input System and **WheelCollider** components (4 wheels: FL, FR, RL, RR)
- **Package pickup and delivery gameplay loop** — drive to a pickup zone, collect a package, deliver it to a matching drop-off zone
- **Win condition**: Complete all deliveries in the current level (minimum 5 deliveries per level)
- **Lose condition**: Timer runs out (default: 180 seconds per level) OR vehicle health reaches 0
- **Progression system**: At least 2 levels with increasing difficulty (more deliveries, tighter timer, longer distances, or added obstacles)
- **Fully playable loop**: Main Menu → Gameplay → Win/Lose Screen → Restart or Return to Menu
- **In-game player guidance** (GRADED REQUIREMENT): Controls must be displayed to the player (e.g., a controls overlay on first play or a "Controls" button on the main menu). Win and lose conditions must be communicated in-game (e.g., HUD text like "Deliver all packages before time runs out!" on level start, and clear win/lose screen messages explaining what happened).

### Concrete Gameplay Parameters (Use as defaults, expose in Inspector)
| Parameter | Default Value |
|---|---|
| Deliveries per level (Level 1) | 5 |
| Deliveries per level (Level 2) | 8 |
| Timer (Level 1) | 180 seconds |
| Timer (Level 2) | 150 seconds |
| Vehicle max speed | 25 m/s |
| Vehicle max motor torque | 400 Nm |
| Vehicle max steering angle | 35 degrees |
| Vehicle health | 100 HP |
| Damage per collision (scaled by impact force) | 5–20 HP |
| Pickup trigger radius | 3 meters |
| Dropoff trigger radius | 4 meters |

---

## 2. UNITY NEW INPUT SYSTEM (REQUIRED — NO LEGACY INPUT)

### Input Actions Asset (`PlayerInputActions`)

**Action Map: Player**
| Action | Type | Binding |
|---|---|---|
| Move | Value (Vector2) | WASD / Left Stick |
| Brake | Button | Space / South Button (A) |
| Interact | Button | E / West Button (X) |
| Look | Value (Vector2) | Mouse Delta / Right Stick |
| Pause | Button | Escape / Start Button |

**Action Map: UI**
| Action | Type | Binding |
|---|---|---|
| Navigate | Value (Vector2) | Arrow Keys / D-Pad |
| Submit | Button | Enter / South Button (A) |
| Cancel | Button | Escape / East Button (B) |

### Implementation Rules
- Use **C# generated class** from the Input Actions asset (enable "Generate C# Class" in asset inspector)
- Wire actions via **InputAction callbacks** (`action.performed += ctx => ...`), NOT the PlayerInput component
- **Disable the Player action map when paused**; enable the UI action map instead
- **No use of `Input.GetAxis`, `Input.GetKey`, or any legacy input methods anywhere in the project**

---

## 3. SYSTEM ARCHITECTURE

### Namespace
All scripts must be inside the namespace `DeliveryGame`.

### Manager Pattern
All managers (`GameManager`, `UIManager`, `AudioManager`, `SaveManager`, `SettingsManager`) must use the **Singleton pattern** with `DontDestroyOnLoad` and a duplicate-destruction check:

```csharp
// example pattern — apply to all managers
if (Instance != null && Instance != this)
{
    Destroy(gameObject);
    return;
}
Instance = this;
DontDestroyOnLoad(gameObject);
```

### Required Scripts and Responsibilities

| Script | Responsibility | Communicates With |
|---|---|---|
| `GameManager` | Game state machine (Menu, Playing, Paused, Won, Lost), level loading, win/lose evaluation, timer countdown | UIManager, DeliveryManager, SaveManager |
| `VehicleController` | WheelCollider-based driving physics, input reading via generated InputActions class, steering, acceleration, braking | GameManager (reads game state to block input when paused) |
| `VehicleHealth` | Tracks vehicle HP, applies collision damage scaled by `relativeVelocity.magnitude`, raises `OnVehicleDestroyed` event | GameManager |
| `DeliveryManager` | Spawns pickup/dropoff pairs, tracks active delivery, validates pickup and dropoff, raises `OnDeliveryComplete` event, prevents duplicate scoring | GameManager, UIManager |
| `PackagePickup` | Trigger zone for pickups, communicates with DeliveryManager on interact press | DeliveryManager |
| `PackageDropoff` | Trigger zone for dropoffs, validates player is carrying the correct package | DeliveryManager |
| `UIManager` | Updates all UI elements (HUD, menus, screens), listens to game events, never contains game logic | GameManager, DeliveryManager |
| `AudioManager` | Plays SFX and manages engine loop, exposes `PlaySFX(clip)` and `SetVolume(float)` | SettingsManager |
| `SettingsManager` | Reads/writes audio and quality settings, exposes UI-bindable methods | SaveManager, AudioManager |
| `SaveManager` | Wraps all `PlayerPrefs` access with safe defaults and type-checked getters/setters, handles save/load of progression and settings | None (utility class) |
| `CameraController` | Cinemachine FreeLook or Virtual Camera following the vehicle with adjustable offset, damping, and look-ahead | VehicleController (follow target) |
| `MiniMapCamera` | Top-down orthographic camera rendering to a RenderTexture displayed on the HUD | None |

### Communication Rules
- Use **C# events / UnityEvents** for decoupled communication (e.g., `GameManager.OnGameStateChanged`, `DeliveryManager.OnDeliveryComplete`)
- **No script may use `FindObjectOfType` at runtime** — use Singleton references or Inspector-assigned references
- **No `Update()` polling for game state** — use event subscriptions

---

## 4. VEHICLE PHYSICS SPECIFICATION

### Approach: WheelCollider-Based

Provide a `VehicleController` script that:
- References 4 `WheelCollider` components (FL, FR, RL, RR) and 4 corresponding visual wheel `Transform` references
- Applies **motor torque** to rear wheels (RWD) based on vertical input
- Applies **steering angle** to front wheels based on horizontal input
- Applies **brake torque** to all wheels on brake input
- Syncs visual wheel meshes to WheelCollider pose every `FixedUpdate`
- Uses a `Rigidbody` with center of mass lowered by `(0, -0.5, 0)` to prevent flipping
- Includes anti-roll bar logic OR sufficiently stiff `WheelCollider` suspension to prevent excessive body roll

### Physics Stability
- `Rigidbody.interpolation` = Interpolate
- `Rigidbody.collisionDetectionMode` = Continuous
- All colliders must have `PhysicMaterial` with appropriate friction values
- Vehicle mass: 1200–1500 kg

---

## 5. CAMERA SYSTEM

- **Primary camera**: Cinemachine Virtual Camera in third-person follow mode, parented to nothing, targeting the vehicle
  - Body: Transposer (offset: `0, 4, -8`)
  - Aim: Composer (damping: `1, 0.5, 1`)
- **Minimap camera**: Orthographic, top-down, follows vehicle XZ position, renders to a RenderTexture displayed on a RawImage in the HUD corner
- **Look input** (right stick / mouse) should allow orbiting around the vehicle OR be ignored if too complex — state which approach you chose

---

## 6. COLLISION LAYERS

Define and use these layers:

| Layer | Used By |
|---|---|
| `Player` (Layer 8) | Vehicle |
| `Pickup` (Layer 9) | Package pickup triggers |
| `Dropoff` (Layer 10) | Dropoff zone triggers |
| `Obstacle` (Layer 11) | Traffic cones, barriers, moving obstacles |
| `Environment` (Layer 12) | Buildings, roads, terrain |

### Collision Matrix Rules
- `Pickup` and `Dropoff` colliders are **Triggers** — they do NOT physically block the vehicle
- `Player` physically collides with `Obstacle` and `Environment`
- `Pickup` does NOT collide with `Dropoff`, `Obstacle`, or `Environment`

---

## 7. SCENE STRUCTURE

### Scene 0: `MainMenu`
- Full-screen UI Canvas with:
  - "Start Game" button → loads Level 1
  - "Settings" button → opens settings panel (volume slider, quality dropdown)
  - "Quit" button → `Application.Quit()`
- Background: Static camera looking at a parked vehicle or environment (optional skybox-only is acceptable)

### Scene 1: `Level_01`
### Scene 2: `Level_02`

Both gameplay scenes must contain:
- Player vehicle (prefab)
- City/town environment with roads (can use Unity primitives — cubes for buildings, planes for roads — if ProBuilder is unavailable)
- Pickup spawn points (empty GameObjects with `PackagePickup` script)
- Dropoff zones (empty GameObjects with `PackageDropoff` script and visible ground markers)
- Obstacles (static and/or moving)
- Directional light + URP lighting
- UI Canvas with HUD, pause overlay, win screen, lose screen (all as child panels toggled on/off)
- Cinemachine Virtual Camera
- Minimap camera + RenderTexture setup

### Build Settings Scene Order
0. `MainMenu`
1. `Level_01`
2. `Level_02`

---

## 8. EDGE CASES (MUST BE HANDLED IN CODE)

Every edge case below must be **prevented in code** with a comment explaining the guard. If any cannot be prevented, list it explicitly as a **known limitation** at the end of the output.

| Edge Case | Required Handling |
|---|---|
| Player tries to pick up a second package while already holding one | Block pickup; show "Already carrying a package" UI message |
| Player enters dropoff zone without holding a package | Block delivery; show "No package to deliver" UI message |
| Player spam-enters a trigger zone | Use a cooldown flag or `bool _isProcessing` to prevent duplicate triggers within the same frame |
| Timer reaches 0 during an active delivery | Immediately trigger lose state; do not award partial credit |
| Vehicle flips upside down | Auto-reset vehicle position/rotation after 3 seconds upside-down (check `transform.up.y < 0`) |
| Scene reload duplicates Singleton managers | Singleton pattern with `DontDestroyOnLoad` + duplicate destruction (see Section 3) |
| Input fires during pause | Disable Player action map on pause; enable UI action map |
| PlayerPrefs returns corrupted or missing data | All `PlayerPrefs.Get*` calls must use the overload with a default fallback value |
| Collision damage applied multiple times per crash | Use `OnCollisionEnter` only (not `OnCollisionStay`); add a brief invincibility window (0.5s) after each damage event |
| Game is quit during gameplay without saving | Auto-save progress on pause and on level complete |

---

## 9. SAVING & LOADING (PlayerPrefs)

### Keys and Defaults
| Key | Type | Default |
|---|---|---|
| `"MasterVolume"` | float | 1.0 |
| `"SFXVolume"` | float | 1.0 |
| `"HighestLevelCompleted"` | int | 0 |
| `"BestTime_Level_01"` | float | 0.0 (0 = no record) |
| `"BestTime_Level_02"` | float | 0.0 |

### SaveManager Rules
- All reads go through `SaveManager` — no script reads `PlayerPrefs` directly
- All writes call `PlayerPrefs.Save()` immediately after setting values
- Provide a `ResetAllData()` method for debug/testing

---

## 10. AUDIO SYSTEM

### Required Audio Clips (describe placeholder names)
| Clip Name | Type | Trigger |
|---|---|---|
| `engine_loop` | Looping | Always playing during gameplay; pitch scales with vehicle speed |
| `sfx_pickup` | One-shot | Package picked up |
| `sfx_deliver` | One-shot | Package delivered |
| `sfx_crash` | One-shot | Vehicle collision with obstacle |
| `sfx_ui_click` | One-shot | Any UI button press |
| `sfx_win` | One-shot | Win screen appears |
| `sfx_lose` | One-shot | Lose screen appears |

### AudioManager Design
- Use two `AudioSource` components: one for music/engine loop, one for SFX
- Expose `PlaySFX(AudioClip clip)` and `SetMasterVolume(float vol)` / `SetSFXVolume(float vol)`
- Volume values read from `SaveManager` on initialization

---

## 11. UI REQUIREMENTS

All UI must use **TextMeshPro** (not legacy UI Text).

### Main Menu (Scene 0)
- Title text
- Start, Settings, Quit buttons
- **Controls panel** (accessible via "Controls" button): Shows keybindings (WASD to drive, Space to brake, E to interact, Esc to pause) and objective summary ("Pick up packages and deliver them before time runs out!")
- Settings panel (slides in or toggles): Master volume slider, SFX volume slider

### In-Game HUD
- Delivery counter: "Deliveries: 3 / 5"
- Timer: "Time: 2:34" (MM:SS format)
- Vehicle health bar (filled image)
- Minimap (RawImage with RenderTexture)
- Current objective text: "Pick up the package at Marker A" / "Deliver to Zone B"
- **Level start briefing** (fades after 3–4 seconds): "Complete 5 deliveries before time runs out. Drive to the blue markers to pick up packages, then deliver them to the orange zones."

### Pause Menu (Overlay panel in Game Scene)
- Resume button
- Restart Level button
- Settings button (reuses settings panel)
- Quit to Main Menu button

### Win Screen
- "Level Complete!" text
- Time taken
- "Next Level" button (if not final level) / "Main Menu" button
- Delivery stats

### Lose Screen
- "Time's Up!" or "Vehicle Destroyed!" text
- "Retry" button
- "Main Menu" button

---

## 12. ANIMATION & VISUAL POLISH

### Required Animations
| Element | Animation | Implementation |
|---|---|---|
| Pickup packages | Hovering bob + Y-axis rotation | Script-based (`Transform.Rotate` + `Mathf.Sin` for bob) |
| Dropoff zone markers | Pulsing scale or color | Script-based or Animator with a simple loop |
| Moving obstacles | Patrol between waypoints | Script-based (`Vector3.MoveTowards` with waypoint array) |
| Vehicle wheels | Rotate matching WheelCollider RPM | Handled by `VehicleController` wheel sync logic |

### Optional Visual Polish (for higher grade)
- Particle effect on successful delivery (confetti or sparkle)
- Skid marks using `TrailRenderer` on rear wheels when braking
- Speed lines or FOV shift at high speed via Cinemachine lens adjustment
- Day/night cycle or post-processing bloom

---

## 13. WINDOWS BUILD INSTRUCTIONS

The assignment requires a **playable Windows executable** submitted as a zip.

Include step-by-step instructions for:
1. Setting the build target to **Windows Standalone (x86_64)** in Build Settings
2. Adding all scenes in the correct order (MainMenu index 0, Level_01 index 1, Level_02 index 2)
3. Configuring Player Settings (company name, product name, resolution, fullscreen mode)
4. Building into an empty folder
5. Zipping the entire build folder for submission

---

## 14. DOCUMENTATION GENERATION (GRADED DELIVERABLE)

The assignment requires a **separate PDF document**. After generating all code and setup instructions, also generate a complete documentation draft that follows this exact structure:

1. **Introduction** — Brief explanation of the game, controls, and goals, written for a new player who has never seen the project
2. **Summary of Scripts** — For each C# script: name, purpose, and authorship attribution (mark which scripts were written by you vs. generated/adapted from outside sources)
3. **Important Game Objects** — Describe the key GameObjects (Vehicle, Managers, Pickup/Dropoff zones, UI Canvas, Cameras) and how they interact with each other
4. **Project Requirements Checklist** — A section that explicitly maps each assignment requirement to where/how it is satisfied:
   - What is saved and loaded (list every PlayerPrefs key and its purpose)
   - What is animated (list every animated object and its method)
   - Scene list with brief summaries
   - Player objective explanation
   - Progression/emergence elements and why they qualify
5. **Known Bugs & Incomplete Features** — Honest list of anything that doesn't fully work or is missing
6. **Referenced Material** — List all external assets, packages, tutorials, and AI tools used, with the degree of usage (as-is, modified, referenced). This is a **grading requirement** — omitting citations will lose points.

---

## 15. SOURCE CODE SUBMISSION

The assignment requires source code delivered via **git link or separate zip**.

Include instructions for:
- Organizing all `.cs` files into a clean folder structure matching the Unity project
- What to include in the zip (all scripts under `Assets/Scripts/`, the Input Actions `.inputactions` file, and any custom editor scripts)
- What to exclude (Library/, Temp/, Logs/, `.csproj` files, build output)

---

## 16. OUTPUT FORMAT (STRICT)

Structure your response in this exact order:

1. **Project Overview** — 3–5 sentence summary of the game
2. **Core Gameplay Loop** — Step-by-step player flow from start to finish
3. **Unity Project Folder Structure** — Full tree (`Assets/Scripts/`, `Assets/Prefabs/`, `Assets/Scenes/`, `Assets/Audio/`, `Assets/Materials/`, `Assets/InputSystem/`, `Assets/UI/`)
4. **Package & Project Setup Instructions** — Step-by-step: creating the project, importing packages, configuring URP, setting up Input System, defining layers
5. **Input System Configuration** — Exact steps to create the Input Actions asset and generate the C# class
6. **Full C# Scripts** — Every script listed in Section 3, complete and production-ready, with:
   - Namespace `DeliveryGame`
   - XML doc comments on all public methods
   - `[SerializeField]` on all Inspector-exposed fields
   - `[Header("Section Name")]` attributes for Inspector organization
   - No `// TODO` or placeholder stubs — all logic must be implemented
7. **Inspector Setup Guide** — For each major GameObject (Vehicle, Managers, UI Canvas, Cameras), list exactly which components to add and what values to set
8. **System Communication Diagram** — ASCII or text-based diagram showing how scripts communicate via events
9. **Edge Cases & Handling** — Table from Section 8 with code-level explanation of each guard
10. **Windows Build Instructions** — Step-by-step from Build Settings to zipped folder
11. **Documentation Draft** — Complete draft following the structure in Section 14, ready to export as PDF
12. **Referenced Material & Citations** — List of all packages, assets, and tools that should be cited in the documentation
13. **Known Limitations** — Anything not implemented, with justification
14. **Optional Polish Features** — Bulleted list of extras that could be added for a higher grade

---

## 17. QUALITY EXPECTATIONS

The final output must:

- **Compile without errors** when pasted into a Unity project with the correct packages installed
- Follow **single-responsibility principle** — no God scripts
- Use **events for decoupling** — no tight cross-references between unrelated systems
- Be **scalable** — adding a Level 3 should require only a new scene and updated delivery parameters, not code changes
- Contain **no duplicated logic** across scripts
- Be **suitable for a graded college submission** at the upper-division level
- Include enough **inline comments** that a reader unfamiliar with the codebase can follow the logic

If anything is ambiguous, choose the **simplest implementation** that fully satisfies the requirement. State any assumptions you make.
