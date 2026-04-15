# Delivery Rush — Unity Setup Guide

> **Engine**: Unity 6000.0.66f2 (Unity 6) | **Pipeline**: URP | **Input**: New Input System

---

## 1. Project Overview

**Delivery Rush** is a 3D driving game where the player pilots a vehicle through a city environment to pick up packages and deliver them to marked drop-off zones before a countdown timer expires. The game features two levels of escalating difficulty, a full save/load system via PlayerPrefs, and a clean event-driven architecture.

### Core Gameplay Loop
1. **Main Menu** → press Start Game
2. **Level starts** → briefing fades in (objective text)
3. Drive to a **blue pickup marker** → enter trigger radius → press **E** to pick up package
4. Drive to the matching **orange dropoff zone** → enter trigger radius → automatic delivery
5. Repeat until all deliveries are complete **(5 in Level 1, 8 in Level 2)**
6. **Win** if all deliveries complete before timer (180 s / 150 s); **Lose** if timer hits 0 or vehicle HP = 0
7. Win screen → Next Level or Main Menu

---

## 2. Folder Structure

```
Assets/
├── InputSystem/
│   └── PlayerInputActions.inputactions   ← Input actions asset (generate C# class from here)
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs
│   │   ├── DeliveryManager.cs
│   │   └── ManagerBootstrap.cs
│   ├── Managers/
│   │   ├── UIManager.cs                  (contains GameplayUIRefs class)
│   │   ├── AudioManager.cs
│   │   ├── SettingsManager.cs
│   │   └── SaveManager.cs
│   ├── Vehicle/
│   │   ├── VehicleController.cs
│   │   └── VehicleHealth.cs
│   ├── Gameplay/
│   │   ├── PackagePickup.cs
│   │   └── PackageDropoff.cs
│   ├── Camera/
│   │   ├── CameraController.cs
│   │   └── MiniMapCamera.cs
│   ├── Animation/
│   │   ├── PackageBobAnimation.cs
│   │   ├── DropoffPulseAnimation.cs
│   │   └── ObstaclePatrol.cs
│   └── UI/
│       ├── MainMenuController.cs
│       ├── PauseMenuController.cs
│       └── SceneUIConnector.cs
├── Scenes/
│   ├── MainMenu.unity
│   ├── Level_01.unity
│   └── Level_02.unity
├── Prefabs/          ← Vehicle, PickupZone, DropoffZone, MovingObstacle
├── Audio/            ← engine_loop, sfx_pickup, sfx_deliver, sfx_crash, sfx_ui_click, sfx_win, sfx_lose
├── Materials/        ← Road, Building, PickupMarker (blue), DropoffMarker (orange)
└── Settings/         ← URP assets (already present)
```

---

## 3. Package Setup

All required packages are already in this project:
- `com.unity.inputsystem` 1.17.0 ✓
- `com.unity.render-pipelines.universal` 17.0.4 ✓
- TextMeshPro (bundled in `com.unity.ugui` 2.0.0) ✓

**You must add Cinemachine (optional — CameraController works without it):**
1. Window → Package Manager → Unity Registry
2. Search "Cinemachine" → Install 3.x

---

## 4. Collision Layers

In **Edit → Project Settings → Tags and Layers**, add these user layers:

| Index | Name        | Used by                       |
|-------|-------------|-------------------------------|
| 8     | Player      | Vehicle collider              |
| 9     | Pickup      | PackagePickup sphere triggers |
| 10    | Dropoff     | PackageDropoff sphere triggers|
| 11    | Obstacle    | Moving/static obstacles       |
| 12    | Environment | Roads, buildings, terrain     |

Then open **Edit → Project Settings → Physics** and set the Layer Collision Matrix:
- Pickup ✗ Dropoff, Obstacle, Environment (no physics collisions)
- Dropoff ✗ Obstacle, Environment
- Player ✓ Obstacle, Environment (does collide)

---

## 5. Input System: Generate C# Class

1. Select `Assets/InputSystem/PlayerInputActions.inputactions`
2. In the Inspector, scroll down to **Generate C# Class** and check the box
3. Set **Class Name** = `PlayerInputActions`
4. Set **Namespace** = `DeliveryGame`
5. Click **Apply**

> The generated `PlayerInputActions.cs` will appear in the same folder.  
> **All scripts compile immediately using `InputActionAsset` directly** — the generated class is optional but recommended.

---

## 6. Scene: MainMenu

### GameObject Hierarchy
```
MainMenu (scene)
├── Managers (GameObject)
│   ├── GameManager        [GameManager.cs]
│   ├── DeliveryManager    [DeliveryManager.cs]
│   ├── UIManager          [UIManager.cs]
│   ├── AudioManager       [AudioManager.cs + 2× AudioSource]
│   ├── SaveManager        [SaveManager.cs]
│   ├── SettingsManager    [SettingsManager.cs]
│   └── ManagerBootstrap   [ManagerBootstrap.cs]
├── Main Camera
├── Directional Light
└── Canvas (Screen Space — Overlay)
    └── MainMenuController [MainMenuController.cs]
        ├── MainPanel
        │   ├── TitleText (TMP)
        │   ├── StartButton → OnStartGame()
        │   ├── ControlsButton → OnOpenControls()
        │   ├── SettingsButton → OnOpenSettings()
        │   └── QuitButton → OnQuit()
        ├── SettingsPanel (inactive by default)
        │   ├── MasterVolumeSlider
        │   ├── SFXVolumeSlider
        │   └── BackButton → OnBack()
        └── ControlsPanel (inactive by default)
            ├── ControlsText (TMP): "WASD — Drive | Space — Brake | E — Pick Up | Esc — Pause"
            ├── ObjectiveText (TMP): "Pick up packages and deliver them before time runs out!"
            └── BackButton → OnBack()
```

### AudioManager Inspector Setup
| Field           | Value                                |
|-----------------|--------------------------------------|
| Engine Source   | Child AudioSource (loop enabled)     |
| SFX Source      | Child AudioSource (loop disabled)    |
| Engine Loop     | Assign `engine_loop` audio clip      |
| Sfx Pickup      | Assign `sfx_pickup`                  |
| Sfx Deliver     | Assign `sfx_deliver`                 |
| Sfx Crash       | Assign `sfx_crash`                   |
| Sfx UI Click    | Assign `sfx_ui_click`                |
| Sfx Win         | Assign `sfx_win`                     |
| Sfx Lose        | Assign `sfx_lose`                    |

---

## 7. Scene: Level_01 (also applies to Level_02)

### Scene Hierarchy
```
Level_01 (scene)
├── SceneConnector (GameObject)   [SceneUIConnector.cs]
│   └── Inspector: Total Deliveries=5, Level Time=180, all UI refs assigned
├── Vehicle (Prefab)
│   ├── CarBody (mesh + Rigidbody + VehicleController + VehicleHealth)
│   │   ├── WheelFL (WheelCollider) + WheelMeshFL (mesh Transform)
│   │   ├── WheelFR + WheelMeshFR
│   │   ├── WheelRL + WheelMeshRL
│   │   └── WheelRR + WheelMeshRR
│   └── BodyCollider (BoxCollider, layer = Player, tag = Player)
├── Environment
│   ├── Road (Plane, layer = Environment)
│   ├── Buildings (Cube array, layer = Environment)
│   └── Obstacles/
│       ├── StaticCone_01 (Cylinder, layer = Obstacle)
│       └── MovingBarrier_01 [ObstaclePatrol.cs] (layer = Obstacle)
│           └── Waypoints (empty GameObjects)
├── Deliveries/
│   ├── Pickup_0 (empty) [PackagePickup.cs, deliveryId=0]
│   │   └── PackageVisual (Cube, PackageBobAnimation.cs)
│   ├── Dropoff_0 (empty) [PackageDropoff.cs, deliveryId=0]
│   │   └── MarkerVisual (Cylinder flat, DropoffPulseAnimation.cs)
│   ├── Pickup_1 ... Dropoff_4 (same pattern, IDs 1-4 for Level 1)
│   └── (8 pairs for Level 2)
├── Cameras/
│   ├── MainCamera [CameraController.cs]  ← assign Vehicle transform
│   └── MiniMapCamera (Camera, orthographic) [MiniMapCamera.cs]
│       └── Assign a RenderTexture (256×256) to Camera.targetTexture
└── Canvas (Screen Space — Overlay) [PauseMenuController.cs + SceneUIConnector refs]
    ├── HUDPanel
    │   ├── DeliveryCounterText (TMP)
    │   ├── TimerText (TMP)
    │   ├── HealthBarBackground (Image)
    │   │   └── HealthBarFill (Image, Image Type = Filled, Fill Method = Horizontal)
    │   ├── MinimapDisplay (RawImage) ← assign the MiniMapCamera RenderTexture
    │   ├── ObjectiveText (TMP)
    │   ├── NotificationText (TMP) + CanvasGroup (NotificationGroup)
    │   └── BriefingPanel (CanvasGroup) → BriefingText (TMP)
    ├── PausePanel (inactive)
    │   ├── ResumeButton → PauseMenuController.OnResume()
    │   ├── RestartButton → PauseMenuController.OnRestartLevel()
    │   ├── SettingsButton → PauseMenuController.OnOpenSettings()
    │   ├── QuitToMenuButton → PauseMenuController.OnQuitToMenu()
    │   └── SettingsSubPanel (inactive) [sliders]
    ├── WinPanel (inactive)
    │   ├── "Level Complete!" (TMP)
    │   ├── WinTimeText (TMP)
    │   ├── WinDeliveriesText (TMP)
    │   ├── NextLevelButton → PauseMenuController.OnNextLevel()
    │   └── WinToMenuButton → PauseMenuController.OnWinToMenu()
    └── LosePanel (inactive)
        ├── LoseReasonText (TMP)  ← "Time's Up!" or "Vehicle Destroyed!"
        ├── RetryButton → PauseMenuController.OnRetry()
        └── LoseToMenuButton → PauseMenuController.OnLoseToMenu()
```

### Vehicle Inspector Setup (VehicleController)

| Field              | Value / Object                    |
|--------------------|-----------------------------------|
| Wheel FL           | WheelFL WheelCollider component   |
| Wheel FR           | WheelFR WheelCollider component   |
| Wheel RL           | WheelRL WheelCollider component   |
| Wheel RR           | WheelRR WheelCollider component   |
| Mesh FL/FR/RL/RR   | The corresponding wheel mesh Transforms |
| Max Motor Torque   | 400                               |
| Max Steering Angle | 35                                |
| Max Speed          | 25                                |
| Brake Torque       | 2000                              |
| Anti Roll Force    | 5000                              |
| Input Actions Asset| PlayerInputActions.inputactions   |

### WheelCollider Settings (each wheel)
| Parameter              | Value    |
|------------------------|----------|
| Mass                   | 20       |
| Radius                 | 0.35     |
| Wheel Damping Rate     | 0.25     |
| Suspension Distance    | 0.2      |
| Spring                 | 35000    |
| Damper                 | 4500     |
| Target Position        | 0.5      |
| Forward Friction Stiffness | 1.0  |
| Sideways Friction Stiffness | 1.0 |

### Rigidbody (on vehicle root)
| Parameter               | Value       |
|-------------------------|-------------|
| Mass                    | 1400        |
| Drag                    | 0.05        |
| Angular Drag            | 0.05        |
| Interpolation           | Interpolate |
| Collision Detection     | Continuous  |

---

## 8. SceneUIConnector Inspector Setup

On the SceneConnector GameObject in each level:

| Field             | Level 1 | Level 2 |
|-------------------|---------|---------|
| Total Deliveries  | 5       | 8       |
| Level Time        | 180     | 150     |
| Briefing Message  | "Complete 5 deliveries before time runs out! Drive to the blue markers to pick up packages, then deliver them to the orange zones." | "Complete 8 deliveries in 150 seconds — distances are longer and more obstacles block your path!" |
| All UI Refs       | Drag in the corresponding TMP/Image/RawImage/Button objects from the Canvas hierarchy |
| Camera Controller | Drag MainCamera |
| Mini Map Camera   | Drag MiniMapCamera |
| Vehicle Transform | Drag the Vehicle root Transform |

---

## 9. System Communication Diagram

```
[GameManager] ──OnGameStateChanged──► [UIManager]    (show/hide panels)
     │                               ► [AudioManager] (start/stop engine, play SFX)
     │                               ► [VehicleController] (enable/disable input map)
     │
     ├──◄ OnAllDeliveriesComplete── [DeliveryManager]
     └──◄ OnVehicleDestroyed──────  [VehicleHealth]

[DeliveryManager] ──OnPackagePickedUp──► [AudioManager]  (sfx_pickup)
                                       ► [PackagePickup]  (hide itself)
                  ──OnDeliverySuccessful► [AudioManager]  (sfx_deliver)
                                       ► [PackageDropoff] (hide itself)
                  ──OnDeliveryCountChanged► [UIManager]   (update HUD counter)

[VehicleController] ──Interact pressed──► [DeliveryManager.PlayerInteracted()]
[PackagePickup]     ──OnTriggerEnter──►  [DeliveryManager.RegisterPickupZone()]
[PackageDropoff]    ──OnTriggerEnter──►  [DeliveryManager.TryDeliver()]

[VehicleHealth]     ──OnHealthChanged──► [UIManager] (health bar)
[GameManager]       ──OnTimerUpdated───► [UIManager] (timer display)

[SceneUIConnector.Start()] ──► DeliveryManager.InitializeLevel()
                           ──► UIManager.Initialize(uiRefs)
                           ──► GameManager.InitializeLevel()
```

---

## 10. Edge Cases — Code-Level Summary

| Edge Case | Guard Location |
|-----------|----------------|
| Player picks up second package | `DeliveryManager.TryPickup` — `IsCarryingPackage` check |
| Player enters dropoff without package | `DeliveryManager.TryDeliver` — `!IsCarryingPackage` check |
| Spam trigger in same frame | `DeliveryManager._isProcessing` flag, reset next frame via coroutine |
| Timer hits 0 during active delivery | `GameManager.Update` — sets Lost immediately, no partial credit |
| Vehicle flips upside down | `VehicleController.TrackFlip` — 3 s timer, auto-reset to last valid pose |
| Scene reload duplicates Singletons | All managers: `if (Instance != null && Instance != this) { Destroy(gameObject); return; }` |
| Input fires during pause | `VehicleController.HandleGameStateChanged` disables Player action map |
| Corrupted PlayerPrefs | All `PlayerPrefs.Get*` calls use the default-value overload |
| Multi-hit per crash | `VehicleHealth._invincibilityTimer` — 0.5 s window after each `OnCollisionEnter` |
| Quit during gameplay | `GameManager.TogglePause` calls `SaveManager.SaveProgress` before pausing |

---

## 11. Windows Build Instructions

1. **File → Build Settings**
2. **Platform**: Windows, Mac, Linux → switch to **Windows Standalone (x86_64)**
3. **Add Open Scenes** or drag scenes in this order:
   - Index 0: `Scenes/MainMenu`
   - Index 1: `Scenes/Level_01`
   - Index 2: `Scenes/Level_02`
4. **Player Settings** (click the button):
   - Company Name: your name
   - Product Name: `Delivery Rush`
   - Default Screen Width: 1920, Height: 1080
   - Fullscreen Mode: Windowed (for submission testing)
5. Click **Build**, choose an **empty folder** (e.g., `Build/`)
6. Zip the entire `Build/` folder for submission

---

## 12. Source Code Submission

**Include in zip:**
- All `.cs` files under `Assets/Scripts/`
- `Assets/InputSystem/PlayerInputActions.inputactions`
- `Assets/InputSystem/PlayerInputActions.cs` (generated file)
- `ProjectSettings/TagManager.asset` (layer definitions)
- `Packages/manifest.json`

**Exclude:**
- `Library/`
- `Temp/`
- `Logs/`
- `Build/`
- All `.csproj` and `.sln` files

---

## 13. Known Limitations

| Limitation | Justification |
|------------|---------------|
| No Cinemachine dependency in CameraController | CameraController uses SmoothDamp follow for immediate compilation without Cinemachine 3.x API changes. Add a CinemachineCamera component manually for look-orbit. |
| No particle effects on delivery | Optional polish feature; not required by spec. |
| No skid marks | Optional polish feature. |
| Audio clips not included | Placeholder names documented; assign `.wav`/`.mp3` clips in AudioManager Inspector. |
| Scene content uses Unity primitives | No 3D modelling software required; cubes/planes per spec Section 7. |
| No navmesh AI traffic | Moving obstacles use waypoint patrol only. |

---

## 14. Documentation Draft (for PDF submission)

### 1. Introduction
Delivery Rush is a 3D vehicle delivery game built in Unity 6. The player drives a car through a city built from geometric primitives, picking up packages at blue-marked zones and delivering them to orange zones before a countdown timer reaches zero. Vehicle health decreases on hard collisions, adding a second fail state. Two levels escalate difficulty by increasing the delivery count and tightening the time limit.

**Controls:**
| Input | Action |
|-------|--------|
| W / S | Accelerate / Reverse |
| A / D | Steer left / right |
| Space | Brake |
| E     | Pick up package (when in range) |
| Esc   | Pause / Resume |

**Win:** Complete all deliveries before time runs out.  
**Lose:** Timer reaches 0, or vehicle health reaches 0.

---

### 2. Summary of Scripts

| Script | Purpose | Authorship |
|--------|---------|-----------|
| `GameManager.cs` | Game state machine, timer, level loading | AI-generated (Claude) |
| `DeliveryManager.cs` | Delivery loop logic, pickup/dropoff validation | AI-generated (Claude) |
| `UIManager.cs` | All UI updates, event-driven, no game logic | AI-generated (Claude) |
| `AudioManager.cs` | SFX playback and engine loop | AI-generated (Claude) |
| `SettingsManager.cs` | Volume/quality settings with SaveManager integration | AI-generated (Claude) |
| `SaveManager.cs` | PlayerPrefs wrapper with safe defaults | AI-generated (Claude) |
| `VehicleController.cs` | WheelCollider RWD physics, New Input System | AI-generated (Claude) |
| `VehicleHealth.cs` | Collision damage with invincibility window | AI-generated (Claude) |
| `PackagePickup.cs` | Trigger zone — pickup interaction | AI-generated (Claude) |
| `PackageDropoff.cs` | Trigger zone — delivery completion | AI-generated (Claude) |
| `CameraController.cs` | SmoothDamp third-person follow camera | AI-generated (Claude) |
| `MiniMapCamera.cs` | Orthographic top-down camera for HUD minimap | AI-generated (Claude) |
| `PackageBobAnimation.cs` | Hovering + rotating package visual | AI-generated (Claude) |
| `DropoffPulseAnimation.cs` | Pulsing scale on dropoff zone markers | AI-generated (Claude) |
| `ObstaclePatrol.cs` | Waypoint-based moving obstacle | AI-generated (Claude) |
| `MainMenuController.cs` | Main menu panel management | AI-generated (Claude) |
| `PauseMenuController.cs` | Pause/Win/Lose button callbacks | AI-generated (Claude) |
| `SceneUIConnector.cs` | Scene → Singleton manager bridge | AI-generated (Claude) |
| `ManagerBootstrap.cs` | Singleton presence validation on startup | AI-generated (Claude) |

---

### 3. Important Game Objects

**Vehicle** — The player-controlled car. Has `Rigidbody` (1400 kg, Continuous, Interpolate), four `WheelCollider` components (FL/FR/RL/RR, rear-wheel drive), `VehicleController` (input + physics), and `VehicleHealth` (collision damage). Tagged `Player`, layer `Player`.

**Managers GameObject** (MainMenu scene, DontDestroyOnLoad) — Parent of all Singleton managers. Persists across all scene loads. Contains `GameManager`, `DeliveryManager`, `UIManager`, `AudioManager`, `SaveManager`, `SettingsManager`.

**PackagePickup zones** — Empty GameObjects with `SphereCollider (trigger, r=3)`, `PackagePickup` script, and a child `PackageVisual` mesh with `PackageBobAnimation`. Layer `Pickup`. Each has a unique `deliveryId` matching its corresponding dropoff.

**PackageDropoff zones** — Empty GameObjects with `SphereCollider (trigger, r=4)`, `PackageDropoff` script, and a child `MarkerVisual` mesh with `DropoffPulseAnimation`. Layer `Dropoff`. Matching `deliveryId` with its pickup.

**Canvas** — Screen Space Overlay. Contains HUD (always visible during play), PausePanel, WinPanel, LosePanel (toggled by UIManager). `SceneUIConnector` and `PauseMenuController` live on this or nearby objects.

**MiniMapCamera** — Orthographic Camera, `MiniMapCamera` script, renders to a 256×256 `RenderTexture` displayed in the HUD via `RawImage`.

---

### 4. Project Requirements Checklist

**Saved & Loaded (PlayerPrefs):**
| Key | Type | Purpose |
|-----|------|---------|
| `MasterVolume` | float | Persist audio master volume |
| `SFXVolume` | float | Persist SFX volume |
| `HighestLevelCompleted` | int | Track progression unlock |
| `BestTime_Level_01` | float | Best completion time for Level 1 |
| `BestTime_Level_02` | float | Best completion time for Level 2 |
| `LastPlayedLevel` | int | Auto-save on pause/quit |

**Animated Objects:**
| Object | Method | Script |
|--------|--------|--------|
| Package pickup visuals | Sine-wave bob + Y-rotation | `PackageBobAnimation.cs` |
| Dropoff zone markers | Uniform scale pulse | `DropoffPulseAnimation.cs` |
| Moving obstacles | `Vector3.MoveTowards` patrol | `ObstaclePatrol.cs` |
| Vehicle wheels | WheelCollider `GetWorldPose` sync | `VehicleController.cs` |

**Scene List:**
- `MainMenu` (index 0) — Title screen with start, settings, controls, quit
- `Level_01` (index 1) — 5 deliveries, 180 s, simple layout
- `Level_02` (index 2) — 8 deliveries, 150 s, longer routes and more obstacles

**Player Objective:** Displayed via HUD objective text and briefing panel. Win/lose conditions shown in on-screen text.

**Progression/Emergence:** Level 2 increases delivery count (5 → 8) and tightens the timer (180 → 150 s), forcing the player to optimise routing. Vehicle health adds emergent risk-reward when cutting through obstacles for shorter paths.

---

### 5. Known Bugs & Incomplete Features

- No particle effects on delivery (optional polish).
- No skid mark TrailRenderers.
- CameraController does not support look-orbiting; camera always trails the vehicle.
- Audio clips are not bundled — empty AudioClip slots will silently skip playback.
- Moving obstacles do not react to player collisions (kinematic is not set — add a `Rigidbody` with `isKinematic = true` to obstacles with `ObstaclePatrol`).

---

### 6. Referenced Material

| Resource | Usage |
|----------|-------|
| Unity 6 Documentation (docs.unity3d.com) | WheelCollider, InputSystem, URP APIs — referenced |
| Unity New Input System package 1.17.0 | As-is |
| Unity URP 17.0.4 | As-is |
| TextMeshPro (com.unity.ugui 2.0.0) | As-is |
| Claude AI (Anthropic) | All C# scripts generated — heavily referenced and modified per spec |

---

## 15. Optional Polish (for higher grade)

- **Particle effect on delivery**: Spawn a `ParticleSystem` prefab (confetti) at the dropoff zone when `OnDeliverySuccessful` fires.
- **Skid marks**: Add a `TrailRenderer` to each rear wheel; enable it when brake input is held and speed > 5 m/s.
- **FOV speed shift**: In `CameraController.Update`, read vehicle speed and lerp `Camera.fieldOfView` between 60° and 75° based on speed ratio.
- **Day/night cycle**: Slowly rotate the Directional Light around the X-axis over time.
- **Post-processing bloom**: Add a Volume component with a Bloom override to the scene for the URP Bloom effect.
- **Traffic AI**: Use `com.unity.ai.navigation` (already installed) with NavMeshAgents for ambient pedestrian/car traffic.
