# CSC 231 Project 2: Kip - Cavern Conduit (Platformer MVP - Level 1)

## Project Information
- **Student Name:** Sean Saba
- **Course:** CSC 231 - Introduction to Game Development
- **Instructor:** Firas El Jerdy
- **Game Title:** Kip: Cavern Conduit
- **Unity Version:** 6000.6.6f1 (Unity 6 - URP 2D)
- **Primary Gameplay Scene:** `Assets/Scenes/Level1.unity`
- **Target Repository:** `csc231-project2-seansaba`

---

## Character Design & Backstory
- **Name:** Kip
- **Visual Look:** An agile clockwork fox-rabbit scout equipped with luminous cyan goggles, riveted brass gauntlets, a compact steam-propeller booster pack, and rugged explorer boots.
- **Short Backstory:** Kip serves as the youngest surveyor for the Sunken Archive—a vast subterranean botanical greenhouse whose ancient bio-reactors have fallen into decay. When the primary conduit fails, threatening the remaining flora with eternal frost, Kip delves into the overgrown maintenance caverns to retrieve lost Aether Crystals, ignite the Conduit Beacons, and reactivate the Ancient Core Reactor.
- **Animations (`Assets/Animations/KipAnimator.controller` & `SpriteFrameAnimator.cs`):**
  - **Kip `Idle`**: Smooth 6-keyframe breathing & blink cycle (`Kip_Idle.anim`)
  - **Kip `Run`**: Full 8-frame pixel-art stride cycle with alternating legs, arm pump, and ear bounce (`Kip_Run.anim`)
  - **Kip `Jump` & `Fall`**: Expressive 3-frame jump launch & 3-frame mid-air descent/brace (`Kip_Jump.anim`, `Kip_Fall.anim`)
  - **Kip `Dash`**: 3-frame supersonic horizontal cyan-energy air dash (`Kip_Dash.anim`)
  - **Kip `Hurt`**: 3-frame impact shock, recoil tumble, and recovery (`Kip_Hurt.anim`)
  - **World & Enemies:** 6-frame seamless animated Acid Water waves (`AcidPool_Frame_0..5`), 4-frame skittering Scrap Crawlers, 4-frame rotor-spinning Buzz Sentries, 4-frame rotating Aether Crystals, and 4-frame swirling Core Reactor Goal portal.
- **Signature Move - Energy Air Dash:** In addition to running and jumping, Kip can trigger a high-velocity horizontal air dash (`Shift` / `J`). This move freezes downward vertical velocity momentarily, allowing Kip to span impossible chasms, evade ground hazards, dodge aerial drones, and defeat enemies directly with boosted kinetic momentum.

---

## Controls
| Action | Primary Key | Secondary / Alternative |
| :--- | :--- | :--- |
| **Move Left** | `A` | `Left Arrow` |
| **Move Right** | `D` | `Right Arrow` |
| **Jump** | `Space` | `W` / `Up Arrow` |
| **Air Dash (Signature Move)** | `Left Shift` | `Right Shift` / `J` |
| **Pause Menu** | `Escape` | `P` |

---

## Level Design & Mechanical Pacing (2–5 Minutes First-Time Playthrough)
The 210-unit level is built around progressive tutorial signposts (`TutorialSign.cs`) that safely introduce each mechanic before testing the player on it:

1. **Section 1: Ground Traversal & Baseline Jumps (X: -4 to 21)**
   - Introduces basic acceleration, deceleration, coyote time, and standard jumping across small gaps.
   - Initial 2 **Aether Crystals** reward clean jumps.
2. **Section 2: The Abyssal Chasm – Introducing the Air Dash (X: 21 to 47)**
   - Presents a 9-unit chasm that cannot be cleared by normal jumping alone.
   - Guides the player to use the **Air Dash** mid-jump; an Aether Crystal floats along the optimal trajectory arc.
3. **Section 3: Environmental Hazards – Tesla Spikes & Acid Pool Basin (X: 48 to 71)**
   - Introduces crackling **Tesla Spikes** and a sunken bubbling **Acid Pool** basin with rising bubble particles.
   - High floating mossy ledges provide elevated bypass routes.
4. **Section 4: Checkpoint Beacon #1 & Moving Conduit Lift (X: 71 to 92)**
   - Touching the first **Conduit Beacon** swaps it to its glowing cyan state, ignites beacon aura particles, and registers Kip's respawn coordinates.
   - Introduces the **Moving Conduit Platform** carrying Kip cleanly across a chasm.
5. **Section 5: Ground Enemy Introduction – Scrap Crawlers (X: 93 to 120)**
   - Introduces 4-legged patrol drone beetles (`PatrolEnemy.cs`).
   - Demonstrates stomping (bouncing upward) and air-dashing through enemies to defeat them.
6. **Section 6: Aerial Sentries & Falling Acid Stalactites (X: 121 to 144)**
   - Introduces hovering **Buzz Sentry** drones (`FlyingEnemy.cs`) oscillating along sine curves.
   - Introduces ceiling **Acid Stalactites** (`FallingHazard.cs`) that shake as a telegraph before plunging downward.
7. **Section 7: Checkpoint Beacon #2 & Reactor Gauntlet (X: 144 to 179)**
   - Second mid-late **Conduit Beacon** followed by an ascending diagonal Moving Conduit Platform and a multi-hazard gauntlet bridge.
8. **Section 8: Climax & Ancient Core Reactor Gateway (X: 180 to 210)**
   - Final aerial sentry jump, chamber guardian crawler, and the swirling **Core Reactor Goal** portal (`GoalZone.cs`) triggering the victory sequence and Win Screen.

---

## Menus & UI System
- **Title Screen:** Mission briefing, Start Game button, Master Volume slider, Audio Mute toggle, and Quit Game button.
- **HUD:** Real-time graphical health hearts (`Heart.png`), Aether Crystal counter (`AetherCrystal.png`, `X / 18`), Air Dash cooldown icon & progress bar (`DashIcon.png`), elapsed timer (`MM:SS`), context-sensitive `[TIP]` tutorial prompts, and checkpoint toast alerts.
- **Pause Menu:** Activated via `ESC` or `P`; features Resume, Restart Level, Volume Slider, Mute/Unmute toggle, Title Screen, and Quit Game.
- **Game Over Screen:** Displays upon depletion of health; offers instantaneous **Respawn at Checkpoint**, **Restart Level**, **Title Screen**, or **Quit Game**.
- **Win Screen:** Displays upon entering the Core Reactor; presents completion time, collected Aether Crystals, and navigation options.

---

## Polish, Particle Systems & Game Feel
- **Controls & Physics Tuning:**
  - **Coyote Time (`0.12s`)**, **Jump Buffering (`0.12s`)**, **Variable Jump Height**, and **Fast-Fall Gravity (`3.2x`)**, plus procedural visual squash-and-stretch on jump, land, and dash.
- **Unity Particle Systems (`ParticleFXManager.cs` & Ambient Emitters):**
  - Showcases **Color over Lifetime**, **Size over Lifetime**, **Velocity over Lifetime (Orbital & Radial)**, **Noise Turbulence**, **Trails Module**, **Rotation over Lifetime**, and multiple emitter shapes (**Cone, SingleSidedEdge, Circle, Box**):
  - **Player FX:** Twin-sided landing dust plume, downward jump steam-thruster cone, running footstep dust, Kip steam-backpack puffs, Air Dash plasma ribbon trails, and hurt/death shatter bursts.
  - **Trap & Hazard FX:** Pop-Up Spike electric telegraph sparks & vertical eruption cone, Crumbling Platform falling rubble & collapse dust, Sawblade continuous grinding spark shower, Falling Stalactite ceiling acid drips & shatter bursts, and Acid Pool dual-layer bubbles + toxic mist + acid splash.
  - **World & Goal FX:** Noise-driven bioluminescent cavern spores, cavern depth mist, Moving Conduit Platform hover thrusters, Aether Crystal shimmer auras + orbital pickup spirals, Checkpoint Beacon orbital energy spires, and Core Reactor Portal dual-layer vortex + multi-stage Victory Fireworks with Trails.
- **Unpredictable Traps & Grounded Level Design:**
  - **Embedded Pop-Up Ground Spikes (`PopUpSpikes.cs`):** Concealed inside flush recessed floor slots behind the stone platform layer; telegraph with rattling tips + electric sparks before snapping upward when Kip approaches (plus staggered rhythmic wave traps).
  - **Crumbling Ancient Ledges (`CrumblingPlatform.cs`):** Tremble, shed rubble particles, collapse under Kip's weight, and reassemble with conduit energy.
  - **Tracked Sawblades & Validated Moving Floors:** Sawblades ride visible mechanical rails, every stone bridge rests on grounded masonry piers, stalactites anchor to continuous cavern ceilings, and `LevelBuilder.ValidateNoPlatformCollisions()` guarantees zero moving-platform overlaps at build time.
- **Camera & Depth:** Smooth-tracking follow camera (`CameraController.cs`) with directional velocity look-ahead, impact screen shake, sprite damage hit flash, and two-layer parallax cavern backgrounds (`ParallaxLayer.cs`).
- **Lives, Auto-Respawn & Traps:** 3-life checkpoint auto-respawn system (`GameManager.cs` / `PlayerHealth.cs`), spinning oscillating sawblade traps (`MovingTrap.cs`), and native UGUI HUD/menus (`UIManager.cs`).
- **Audio & Sound Effects (`AudioManager.cs`):**
  - Custom 16-bit PCM synthesized SFX bank (`sfx_jump`, `sfx_land`, `sfx_dash`, `sfx_collect`, `sfx_hurt`, `sfx_checkpoint`, `sfx_win`) and 105 BPM ambient synthwave `music_loop.wav` configured for 2D non-pausing preloaded playback with runtime mute and volume controls.

---

## Playtest Notes (Half-Page Summary)
- **Tester 1 (Self - Initial Whitebox & Visual Verification):**
  - *Observation:* Early sprite slicing included small label text and ground check hit Kip's own collider; platforms with `Tiled` draw mode clipped vertically when height was smaller than sprite PPU height.
  - *Adjustment:* Re-sliced all sprites cleanly with flood-fill alpha matting, set `FullRect` mesh mode, fixed `CheckGround()` to ignore Kip's own collider and trigger volumes, matched collider height flush to Kip's boots, and verified all 6 sections via rendered camera screenshots.
- **Tester 2 (Peer Playtest):**
  - *Observation:* Section 2's 9-unit chasm felt daunting at first without clear air-dash guidance, and Section 3 needed a clear safe route above the spikes and acid basin.
  - *Adjustment:* Added in-world `[TIP]` Tutorial Signs before each new mechanic, placed a mid-air Aether Crystal along the ideal parabolic dash arc (`X: 25, Y: 3.1`), and built elevated mossy floating ledges above the Tesla Spikes and sunken Acid Pool.

---

## Tools & Credits (AI & Outside Assets Disclosure)
In full accordance with course policy:
- **Game Engine:** Unity 6 (Version `6000.6.6f1`) using Universal Render Pipeline (URP 2D).
- **Development CLI & Tools:** Unity CLI (`unity`), Git, GitHub CLI (`gh`), Microsoft .NET 10 SDK.
- **AI Tools Used:**
  - **Google Antigravity Assistant:** Used for pairing on C# architecture, Unity Editor automation scripts (`LevelBuilder.cs`), mathematical audio synthesis (PCM WAV generators), sprite slicing/normalization scripts, and verification.
  - **Imagen (Image Generation API):** Used to generate original pixel-art sprite sheets for Kip, cavern parallax background, enemies, hazards, and tileset. Sprites were programmatically sliced, normalized, and formatted into alpha-transparent PNG assets.
- **Outside Assets & Licenses:**
  - All sprites and audio assets were created specifically for this project from original prompts and procedural synthesis. No third-party copyrighted asset packs or lecture Knight assets were used.
