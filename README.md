# CSC 231 Project 2: Kip - Cavern Conduit (Platformer MVP - Level 1)

## Project Information
- **Student Name:** Sean Saba
- **Course:** CSC 231 - Introduction to Game Development
- **Instructor:** Firas El Jerdy
- **Game Title:** Kip: Cavern Conduit
- **Unity Version:** 6000.6.6f1 (Unity 6 - URP 2D)
- **Primary Gameplay Scene:** `Assets/Scenes/Level1.unity`
- **Repository:** `csc231-project2-seansaba`
- **Gameplay Video:** [`KipCavernConduit_GameplayDemo.mp4`](KipCavernConduit_GameplayDemo.mp4)

---

## Character Design & Backstory
- **Name:** Kip
- **Visual Look:** An original 16-bit pixel-art clockwork fox-rabbit scout equipped with luminous cyan brass goggles, riveted brass gauntlets, a compact steam-propeller booster pack, cream chest fur, a white-tipped orange tail, and explorer boots.
- **Short Backstory:** Kip is the youngest surveyor for the Sunken Archive—an underground botanical greenhouse whose ancient bio-reactors are collapsing in an apocalyptic meltdown. As the caverns shake and corrupt machines awaken, Kip descends through nine treacherous reactor chambers to reclaim all 20 lost Aether Crystals, ignite the Conduit Beacons, absorb the Special Orange Gem, defeat the **Dread Conduit Overseer**, and restore peace and freedom to the cavern.
- **Animations (`Assets/Animations/KipAnimator.controller` & `PlayerController.cs`):**
  - **Idle:** 6-keyframe breathing & blink cycle (`Kip_Idle.anim`)
  - **Run:** Crisp 8-frame pixel-art stride cycle (`Kip_Run.anim`, 11 FPS with `RigidbodyInterpolation2D.Interpolate`)
  - **Jump & Fall:** 3-frame jump launch & 3-frame aerial descent (`Kip_Jump.anim`, `Kip_Fall.anim`)
  - **Dash:** 3-frame horizontal cyan-energy air dash (`Kip_Dash.anim`)
  - **Hurt:** 3-frame impact recoil & recovery (`Kip_Hurt.anim`)
  - **Final Death (3 Lives Lost):** Bespoke 6-frame dramatic collapse sequence (`Kip_Death_0..5.png`) with backpack sparks, venting steam, loose clockwork gears, and rising spirit embers before the Game Over screen.
- **Signature Move — Energy Air Dash (+ Boss-Stage Double Jump):**
  - **Air Dash (`Shift` / `J`):** High-velocity horizontal burst that freezes gravity briefly to cross wide chasms, dodge traps, shatter Scrap Crawlers & Buzz Sentries, and weaken/stagger the Final Boss.
  - **Special Orange Gem Double Jump:** Strictly locked throughout the level until Kip absorbs the Special Orange Gem (`0 / 1` -> `1 / 1`) right before the Final Boss arena, and consumed upon defeating the Boss so Kip returns to his original form.

---

## Controls
| Action | Primary Key | Secondary / Alternative |
| :--- | :--- | :--- |
| **Move Left** | `A` | `Left Arrow` |
| **Move Right** | `D` | `Right Arrow` |
| **Jump** *(Double Jump after Orange Gem)* | `Space` | `W` / `Up Arrow` |
| **Air Dash (Signature Move)** | `Left Shift` | `Right Shift` / `J` |
| **Pause Menu** | `Escape` | `P` |

---

## Level Design & Mechanical Pacing (3–5 Minutes First-Time Playthrough)
The 255-unit level spans **9 progressively paced sections**, using in-world `[TIP]` tutorial signposts (`TutorialSign.cs`) to introduce every mechanic safely before testing the player:

1. **Section 1 — Spawn Temple & Baseline Jumps (`X: -5 .. 21`):** Grounded symmetric stone temple introducing movement, coyote time, jump buffering, and the first 2 Aether Crystals.
2. **Section 2 — Abyssal Air-Dash Chasm (`X: 21 .. 47`):** Wide chasm teaching mid-air dashing, twin symmetric bridge piers, and the first recessed pop-up floor ambush spike.
3. **Section 3 — Recessed Ambush Spikes, 6-Frame Acid Pool & Sawblade (`X: 48 .. 69`):** Hidden floor spikes erupting on proximity, bubbling 6-frame animated toxic water, tracked vertical sawblade, and an upper crumbling-ledge bypass route.
4. **Section 4 — Conduit Beacon #1 & Dual Moving Floors (`X: 70 .. 97`):** First gem-gated **Checkpoint Beacon** (requires collecting all 7 prior Aether Crystals) followed by mathematically collision-validated dual moving conduit platforms (`LevelBuilder.ValidateNoPlatformCollisions()`).
5. **Section 5 — Scrap Crawler Arena (`X: 98 .. 125`):** Symmetric twin patrolling 4-frame Scrap Crawlers (`PatrolEnemy.cs`) introducing stomping and dash-shattering combat.
6. **Section 6 — Buzz Sentries, Vertical Lift & Falling Stalactites (`X: 126 .. 150`):** Hovering 4-frame aerial drones (`FlyingEnemy.cs`), high-clearance ceiling Acid Stalactites (`FallingHazard.cs`), vertical conduit lift, and a crumbling stone ledge.
7. **Section 7 — Conduit Beacon #2 & Approach Bridge (`X: 150 .. 187`):** Second gem-gated Checkpoint Beacon (requires all 15 prior crystals), ascending moving conduit, and a clean ambush-spike + crawler bridge.
8. **Section 8 — Final Gauntlet & Orange Gem Sanctuary (`X: 188 .. 221.4`):** Crumbling-ledge approach into the hardest pre-boss gauntlet block, leading to Checkpoint Beacon #3 (requires all 20 Blue Aether Crystals) and the **Special Orange Gem** (`DoubleJumpGem.cs`).
9. **Section 9 — Final Boss Arena & Animated Core Reactor Portal (`X: 223.8 .. 250.2`):**
   - **Dread Conduit Overseer (`FinalBoss.cs`, 12 HP):** Remains completely hidden until Kip takes the Orange Gem, then slams down with dark scary boss lighting, lightning flashes, a loud evil `"FINAL BOSS, FIGHT!!"` banner & voice, 4-frame normal & enraged stomping walk animations, seismic whole-game & ground shake on every footstep, real-time `LineRenderer` laser targeting (1-shot, 3-shot angry spread at half HP, 5-shot two-wave final fury barrage), knock-away head stomp bounce, and a strict head-stomp-only kill condition.
   - **Core Reactor Portal (`GoalZone.cs`) & End-of-Level Barrier:** Solid barrier prevents falling past the end of the level. Once the Boss is slain, the cavern returns to bright lighting, music & SFX shift from apocalyptic destruction to Peace & Freedom, and—once all gems are collected—the 8-frame portal opens, draws Kip into its vortex, implodes shut, and launches the Win Screen.

---

## Menus & UI System (100% Bespoke Image-Generated Steampunk Art)
Every screen, panel, button, energy bar, and icon uses custom 16-bit steampunk-cavern artwork (`Assets/Sprites/UI/`):
- **Title Screen:** Full-screen cavern reactor key-art backdrop (`UI_TitleBackdrop.png`), ornate 9-slice brass-gear menu frame (`UI_PanelFrame.png`), steampunk cyan-gem buttons (`UI_ButtonFrame.png`), Start Game, Volume slider (`UI_BarFrame.png`), Mute toggle, and Quit Game.
- **In-Game HUD:** Faceted ruby & cracked empty heart sockets (`Heart.png`, `HeartEmpty.png`), Kip scout medallion lives counter (`LivesIcon.png`, `3 lives × 3 hearts`), Blue Crystal (`0/20`) & Orange Crystal (`0/1`) counters, winged-gear Dash gauge (`DashIcon.png`), Double Jump ribbon badge (`UI_RibbonFrame.png`), Timer, Boss Health Bar (`UI_BossFrame.png`), non-overlapping top-center Tutorial/Toast ribbons, and Bottom Controls plaque.
- **Pause Menu (`ESC` / `P`):** Resume, Restart Level, Volume Slider, Mute/Unmute, Title Screen, and Quit Game.
- **Game Over Screen (`OUT OF LIVES`):** Triggered cleanly after Kip's 6-frame final death animation when all 3 lives are exhausted, offering Restart Level (full reset), Title Screen, and Quit Game.
- **Win Screen (`LEVEL COMPLETE!`):** Displays completion time, Aether Crystals collected, and Play Again / Title / Quit buttons.

---

## Polish, Particle Systems & Audio
- **Game Feel & Controls:** Coyote time (`0.12s`), jump buffering (`0.12s`), variable jump height, fast-fall gravity (`3.2×`), squash-and-stretch, `RigidbodyInterpolation2D.Interpolate`, directional camera look-ahead, and post-smoothing seismic screen shake (`CameraController.cs`).
- **Unity Particle Systems (`ParticleFXManager.cs`):** 14+ particle effects using Trails, Noise Turbulence, Orbital/Radial Velocity, Color/Size over Lifetime (landing/footstep dust, jump steam, backpack puffs, dash plasma trails, unified gem pickup sparkles, spike telegraph/eruption, crumbling rubble, sawblade sparks, acid bubbles/mist/splash, beacon spires, boss embers/roar waves, portal vortex, victory fireworks).
- **Dynamic End-of-the-World -> Peace & Freedom Audio (`AudioManager.cs`):**
  - Pre-boss apocalyptic D-Phrygian war-drum & doom-brass soundtrack (`music_loop.wav`) plus dark, heroic/evil SFX for every action (`sfx_jump`, `sfx_land`, `sfx_dash`, `sfx_collect`, `sfx_hurt`, `sfx_checkpoint`, `sfx_impact`, `sfx_player_death`, `sfx_boss_step`, `sfx_boss_charge`, `sfx_boss_roar`, `sfx_boss_taunt`, and dry evil spoken `sfx_boss_intro` `"FINAL BOSS, FIGHT!!"`).
  - Upon defeating the Final Boss, seamlessly crossfades to F-Major **Peace & Freedom** music (`music_peace_loop.wav`) and peaceful post-boss SFX (`sfx_jump_peace`, `sfx_land_peace`, `sfx_dash_peace`, `sfx_collect_peace`, `sfx_win`).

---

## Playtest Notes (Half-Page Summary)
- **Tester 1 (Self — Physics, Visual & Audio Pass):**
  - *What went wrong:* Kip blurred slightly while running due to non-interpolated Rigidbody updates and bright foreground dust; a crash sound played on startup when `PlayerController` detected initial ground contact; jumping off disappearing crumbling ledges caused Kip's head to clip low cavern ceilings; and the top-center `TutorialBanner` slightly overlapped the right edge of `TopLeftHUD`.
  - *What changed:* Enabled `RigidbodyInterpolation2D.Interpolate`, moved footstep dust behind Kip, froze `Time.timeScale` in `UIManager.Awake()` with a spawn-silence guard in `PlayerController`/`AudioManager`, raised both cavern ceilings to `Y = 10.2` past the crumbling-ledge jump arc, and centered all top banners cleanly between `TopLeftHUD` and `TopRightHUD` with >60px clearance on both sides.
- **Tester 2 (Peer Playtest — Level Pacing & Boss Fight):**
  - *What went wrong:* The penultimate bridge felt overcrowded with side-by-side traps, Kip could chain repeated head stomps on the Final Boss without disengaging, and skipping gems earlier in the level made late backtracking confusing.
  - *What changed:* Decluttered the penultimate bridge while making the final gauntlet block the toughest pre-boss challenge, added directional knock-away `BounceAway()` on boss head-stomps, added an end-of-level barrier past the portal, and gated each Checkpoint Beacon and the final Portal on collecting prior/all gems with clear HUD toast reminders.

---

## Tools & Credits (AI & Outside Assets Disclosure)
In full accordance with CSC 231 course policy:
- **Game Engine & Tooling:** Unity 6 (`6000.6.6f1`, URP 2D), Unity CLI, Git / GitHub LFS, PowerShell / .NET (`System.Drawing` for sprite slicing & alpha matting; `System.Speech` for base spoken TTS phrase), FFmpeg (gameplay demo video encoding).
- **AI Tools Used:**
  - **Google Antigravity (AI Coding Assistant):** Used for collaborative C# gameplay architecture, Unity Editor scene-building automation (`LevelBuilder.cs`), procedural 16-bit PCM audio synthesis scripts, sprite-sheet slicing/normalization scripts, and build verification.
  - **Gemini / Imagen Image Generation:** Used to generate original, style-matched 16-bit pixel-art sprites for Kip (including the 6-frame death sheet), enemies, tileset, animated acid pool, interactive portal, cavern background, and the entire steampunk UI art suite (`UI_TitleBackdrop`, 9-slice frames, buttons, energy bars, and HUD icons).
- **Outside Assets & Licenses:**
  - All art, animations, C# scripts, and audio clips were created specifically for this project from original prompts and procedural synthesis. Zero third-party asset packs or lecture Knight assets were used.
