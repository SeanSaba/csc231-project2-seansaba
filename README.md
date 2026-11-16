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
- **Short Backstory:** Kip serves as the youngest surveyor for the Sunken Archive—a vast subterranean botanical greenhouse whose ancient bio-reactors have fallen into decay. When the primary conduit fails, threatening the remaining flora with eternal frost, Kip delves into the overgrown maintenance caverns to retrieve lost Aether Crystals, ignite the mid-station Conduit Beacon, and reactivate the Ancient Core Reactor.
- **Animations / Poses:**
  - `Idle`: Alert stance scanning the cavern environment
  - `Run`: Agile forward sprint cycle
  - `Jump`: Upward leap propelled by steam micro-thruster
  - `Fall`: Downward tuck with ears streamlined
  - `Hurt / Death`: Flinching knockback with impact flash
  - `Dash`: Horizontal forward propulsion enveloped by cyan energetic motion trails
- **Signature Move - Energy Air Dash:** In addition to running and jumping, Kip can trigger a high-velocity horizontal air dash (`Shift` / `J`). This move freezes downward vertical velocity momentarily, allowing Kip to span impossible chasms, evade ground hazards, dodge aerial drones, and defeat enemies directly with boosted kinetic momentum.

---

## Controls
| Action | Primary Key | Secondary / Controller |
| :--- | :--- | :--- |
| **Move Left** | `A` | `Left Arrow` |
| **Move Right** | `D` | `Right Arrow` |
| **Jump** | `Space` | `W` / `Up Arrow` |
| **Air Dash (Signature Move)** | `Left Shift` | `Right Shift` / `J` |
| **Pause Menu** | `Escape` | `P` |

---

## Level Design & Mechanical Pacing (2–5 Minutes First-Time Playthrough)
The level follows the fundamental design principle: *Introduce each mechanic safely before testing the player on it*.

1. **Section 1: Ground Traversal & Baseline Jumps (X: -4 to 22)**
   - Introduces basic acceleration, deceleration, and standard jumping across small gaps with safe stone ledges.
   - Initial 2 **Aether Crystals** reward clean jumps.
2. **Section 2: The Abyssal Chasm – Introducing the Air Dash (X: 20 to 45)**
   - Presents a 9-unit chasm that cannot be cleared by normal jumping alone.
   - Guides the player to use the **Air Dash** mid-jump; an Aether Crystal floats at the midpoint as a trajectory target.
3. **Section 3: Environmental Hazards – Tesla Spikes & Bypass Ledges (X: 45 to 65)**
   - Introduces static electrical floor hazard traps (Tesla Spikes).
   - High floating ledges provide alternative elevated routes for cautious players.
4. **Section 4: The Mid-Point Checkpoint Beacon (X: 62 to 70)**
   - An ancient Conduit Beacon pylon. Touching it ignites a brilliant cyan glow, plays a chime fanfare, and registers Kip's respawn coordinates.
5. **Section 5: Ground Enemy Introduction – Scrap Crawler (X: 72 to 92)**
   - Introduces the 4-legged patrol drone beetle.
   - Demonstrates stomping mechanics: landing on top bounces Kip into the air and neutralizes the drone; dashing through also defeats it.
6. **Section 6: Aerial Enemy & Dynamic Hazards – Buzz Sentry & Stalactites (X: 92 to 118)**
   - Introduces hovering Buzz Sentry drones that bob along sine curves.
   - Introduces ceiling Acid Stalactites that shake and plunge downward when Kip passes below.
7. **Section 7: Climax & Ancient Core Reactor Gateway (X: 118 to 142)**
   - Combines moving platforms, spike trenches, and aerial timing into a climactic sprint.
   - Entering the Ancient Core Reactor triggers the Level Complete victory sequence and win screen.

---

## Menus & UI System
- **Title Screen:** Mission briefing, Start Game button, Audio Mute toggle, and Quit Game button.
- **HUD:** Real-time health display (3 hearts), Aether Crystal counter (`X / 10`), Air Dash cooldown status (`READY` / cooldown bar), level elapsed timer (`MM:SS`), and pause hint.
- **Pause Menu:** Activated via `ESC` or `P`; features Resume, Restart Level, Audio Mute/Volume, and Quit options with frozen game time.
- **Game Over Screen:** Displays upon depletion of health; offers instantaneous **Respawn at Checkpoint** (preserving progress) or **Restart Level**.
- **Win Screen:** Displays upon reaching the Core Reactor; presents total completion time, gathered Aether Crystals, and options to replay.

---

## Polish & Game Feel
- **Controls & Physics Tuning:**
  - **Coyote Time (`0.12s`):** Allows a jump input shortly after walking off a ledge, preventing frustrating missed jumps.
  - **Jump Buffering (`0.12s`):** Queues jump input if pressed just prior to touching the ground.
  - **Variable Jump Height:** Releasing the jump button early cuts vertical velocity for precise platforming.
  - **Fast-Fall Gravity (`3.2x`):** Ensures a snappy, responsive descent without feeling floaty.
- **Visual Effects:**
  - **Landing Dust Particles:** Particle burst spawned at Kip's feet upon impact.
  - **Footstep Dust:** Subtle puffs emitted during high-speed running.
  - **Air Dash Trails:** Cyan energetic burst and trailing sparks.
  - **Pickup Sparkles:** Sparkling explosion on Aether Crystal collection.
  - **Camera Screen Shake:** Dynamic impulse shake on taking damage, dashing, or landing stomps.
  - **Damage Hit Flash:** Red/white transparency flicker on player sprite during invincibility frames.
- **Audio & Sound Effects:**
  - Complete custom 16-bit PCM synthesized audio bank:
    - `sfx_jump.wav`: Upward frequency chirp
    - `sfx_land.wav`: Low-frequency impact thud
    - `sfx_dash.wav`: High-velocity whoosh and air rush
    - `sfx_collect.wav`: 4-note ascending crystal arpeggio
    - `sfx_hurt.wav`: Distorted punch impact
    - `sfx_checkpoint.wav`: Celestial resonant chime
    - `sfx_win.wav`: Triumphant major chord fanfare
    - `music_loop.wav`: Atmospheric subterranean synth melodic loop with bass, pulse, and percussion.

---

## Playtest Notes (Half-Page Summary)
- **Tester 1 (Self - Initial Whitebox Test):**
  - *Observation:* The jump apex felt slightly loose, and Kip occasionally overshot narrow floating ledges.
  - *Adjustment:* Increased `fallMultiplier` to `3.2x` and `lowJumpMultiplier` to `2.2x`. Added `deceleration` braking (`70 units/s²`) so releasing directional keys halts Kip promptly.
- **Tester 2 (Peer Playtest):**
  - *Observation:* In Section 2, the tester hesitated before attempting the wide gap, unsure if the air dash had sufficient horizontal reach.
  - *Adjustment:* Placed an Aether Crystal precisely along the optimal parabolic dash arc at `X: 23, Y: 2.5` to visually telegraph the trajectory.
  - *Observation:* After dying in Section 6, respawning at the checkpoint felt seamless, and the remaining enemies provided the right amount of challenge on the second attempt.

---

## Tools & Credits (AI & Outside Assets Disclosure)
In full accordance with course policy:
- **Game Engine:** Unity 6 (Version `6000.6.6f1`) using Universal Render Pipeline (URP 2D).
- **Development CLI & Tools:** Unity CLI (`unity`), Git, GitHub CLI (`gh`), Microsoft .NET 10 SDK.
- **AI Tools Used:**
  - **DeepMind / Google Antigravity Assistant:** Used for pairing on C# architecture, Unity Editor automation scripts, mathematical audio synthesis algorithms (PCM WAV generators), and asset coordination.
  - **Imagen (Image Generation API):** Used to generate source concept sprites for Kip, cavern parallax background, enemies, hazards, and tileset. Sprites were programmatically processed, sliced, and formatted into alpha-transparent PNG assets.
- **Outside Assets & Licenses:**
  - All sprites and audio assets were generated specifically for this project under original creative prompts. No third-party commercial asset packs or copyrighted knight assets were used. All assets are free and licensed for academic use under the MIT/Creative Commons attribution.
