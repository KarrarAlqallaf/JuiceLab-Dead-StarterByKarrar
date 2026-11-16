# SWE402 Game Feel Submission

Read [START-HERE.md](START-HERE.md) to open the starter.

## Student
Name and student ID: Karrar *(add student ID here)*

## Run
Unity version: **6000.3.2f1**

Scenes, controls, and instructions:
- **Scenes:**
  - `Assets/_Project/Scenes/Before.unity`: Original unpolished baseline scene (preserved unchanged for side-by-side comparison — no sound, particles, trail, camera shake, squash/stretch, streak effects, animated HUD, Game Over menu, or infinite round respawn).
  - `Assets/_Project/Scenes/After.unity`: Enhanced "juicy" scene containing all visual, audio, post-processing, UI, and endless gameplay loop improvements.
- **Controls:**
  - Move the paddle horizontally using the **Mouse**, **A / D**, or **Left / Right Arrow** keys.
  - When all 3 lives are exhausted in `After.unity`, click the **RESTART** button on the **GAME OVER** menu to start a fresh run.
- **Instructions:**
  1. Open the project in Unity `6000.3.2f1`.
  2. Open `Assets/_Project/Scenes/After.unity` (or `Before.unity` to compare the baseline), set the Game view aspect ratio to **16:9**, and press **Play**.
  3. Destroy bricks to build combos/streaks (`STREAK` counter on the HUD) and trigger tiered full-screen visual effects at 3, 4, and 5 consecutive brick hits on a single paddle rebound.
  4. Clear all bricks to advance to the next round. Once the ball exits the brick zone, the obstacle grid respawns and the ball speed increases slightly each round for an endless arcade loop.

## Intended feel
**Heavy, Kinetic & High-Intensity Arcade ("Punchy Cyber-Breakout"):**
Compared to the silent, static `Before.unity` baseline, `After.unity` is designed to convey physical weight, momentum, and escalating tension without altering core collision shapes:
- **Heavy Impact Weight:** Collisions feel solid and forceful through directional camera kick/shake, multi-layered brick debris + spark bursts, pooled brick shatter ghosts, and immediate 3-stage squash-and-stretch recoil on the child `Visual` transforms of the ball and paddle.
- **Escalating Combo & Streak Intensity:** Consecutive brick hits in a single rebound from the player's paddle raise the pitch of brick destruction audio, update and punch-animate the HUD `STREAK` counter (`STREAK 0`, `STREAK 1`, ...), and trigger tiered `0.3s` full-screen visual effects:
  - **3rd Streak:** Inverts all screen colors for `0.3` seconds.
  - **4th Streak:** Immediately stops the previous visual effect and turns the screen White & Black (monochrome) for `0.3` seconds.
  - **5th Streak:** Immediately stops the previous visual effect and paints everything on screen Blood-Like Red for `0.3` seconds.
  - **6th+ Streak:** Does not trigger or interrupt any visual effect — only increments the `STREAK` counter.
  - **Rebound / Ball Lost Reset:** Resets the `STREAK` counter back to `0` as soon as the ball rebounds from the player's paddle or is lost.
- **Endless Progression & Clear Consequence:** Clearing a board rewards the player with a seamless next round at higher ball speed (spawned safely once the ball leaves the obstacle zone). Losing a ball triggers a low-end audio drop, downward camera trauma, trail reset, and a crimson screen/HUD pulse; losing all 3 lives halts play and presents a **GAME OVER** overlay with a **RESTART** button.

## Improvements
| Improvement | Trigger | Implementation file/object | Purpose |
|---|---|---|---|
| **Brick impact** | `GameEvents.BrickDestroyed` | `Assets/_Project/Scripts/Feedback/BrickImpactFeedback.cs` (`JuiceFeedback` GameObject) | Spawns a pooled two-layer particle burst at the destroyed brick's position and contact point: heavy gravity-affected colored brick fragments biased along the impact normal plus bright directional sparks. |
| **Reactive animation** | `GameEvents.BallHitPaddle`, `GameEvents.BallHitWall`, `GameEvents.BrickDestroyed` | `Assets/_Project/Scripts/Feedback/CollisionReactionFeedback.cs` (`JuiceFeedback` GameObject, animating `Paddle/Visual` & `Ball/Visual`) | Applies 3-stage squash-and-stretch deformation (compression -> elastic rebound -> settle), downward paddle recoil, normal-aligned ball deformation, and impact color flash strictly on child `Visual` transforms so physics colliders remain untouched. |
| **Event-based audio** | `GameEvents.BallHitPaddle`, `GameEvents.BrickDestroyed`, `GameEvents.BallLost`, `GameEvents.BallHitWall` | `Assets/_Project/Scripts/Feedback/AudioFeedback.cs` (`JuiceFeedback` GameObject & child `AudioSource` channels + `Assets/_Project/Audio/*.wav`) | Plays dedicated, cooldown-guarded sound effects for paddle hits, wall bounces, ball loss, and brick destruction with pitch variation and combo-driven pitch escalation (`+0.035` pitch per consecutive brick hit) plus procedural synthesized fallbacks. |
| **Interface feedback (Score & Game Over UI)** | `GameEvents.ScoreChanged`, `GameEvents.LivesChanged`, `GameEvents.GameOver`, `GameEvents.GameRestarted` | `Assets/_Project/Scripts/Feedback/ScoreFeedback.cs`, `Assets/_Project/Scripts/Core/HUD.cs` (`Canvas` & `JuiceFeedback`) | Takes ownership of the score text via `HUD.ScoreHandledExternally` for rolling number interpolation, scale punch (`1.36x`), and gold highlight flash; displays a full-screen **GAME OVER** overlay with an interactive **RESTART** button when all 3 lives are exhausted. |
| **Single-Rebound Streak Counter UI** | `GameEvents.BrickDestroyed`, `GameEvents.BallHitPaddle`, `GameEvents.BallLost`, `GameEvents.GameOver`, `GameEvents.GameRestarted` | `Assets/_Project/Scripts/Feedback/StreakFeedback.cs` (`JuiceFeedback` -> `Canvas/StreakText`) | Displays how many objects (bricks) have been destroyed in a single rebound from the player (`STREAK X`) with a `1.38x` punch-scale and tier-colored pop animation on each hit, resetting to `STREAK 0` when the ball hits the paddle or is lost. |
| **Additional technique 1: Directional Camera Shake** | `GameEvents.BrickDestroyed`, `GameEvents.BallHitPaddle`, `GameEvents.BallHitWall`, `GameEvents.BallLost` | `Assets/_Project/Scripts/Feedback/CameraShakeFeedback.cs` (`JuiceFeedback` -> `Main Camera`) | Combines directional impact kicks with Perlin-noise trauma shake (`42 Hz`) and fast recovery back to the camera's exact base local position. |
| **Additional technique 2: Brick Shatter Ghost** | `GameEvents.BrickDestroyed` | `Assets/_Project/Scripts/Feedback/BrickDestructionFeedback.cs` (`JuiceFeedback` GameObject) | Spawns a pooled sprite flash/collapse ghost at the destroyed brick's exact center, size, and color that expands briefly with directional tilt before collapsing and fading out. |
| **Additional technique 3: Dynamic Ball Trail & Life-Loss Pulse** | Continuous ball motion, `GameEvents.BrickDestroyed`, `GameEvents.BallLost` | `Assets/_Project/Scripts/Feedback/BallTrailAndLifeLossFeedback.cs` (`JuiceFeedback` -> `Ball/Visual`, `Main Camera`, `LivesText`) | Renders a tapered `TrailRenderer` on `Ball/Visual` dynamically tinted by the last destroyed brick's color (cleared for 2 frames on `BallLost` so respawn never streaks) and pulses the camera background and `LIVES` text crimson when a life is lost. |
| **Additional technique 4: Tiered Streak Screen Visual Effects (3rd / 4th / 5th Streak)** | `GameEvents.BrickDestroyed` (single-rebound streak milestones 3, 4, 5, and >5) | `Assets/_Project/Scripts/Feedback/StreakFeedback.cs` (`JuiceFeedback` -> runtime URP `Volume` profiles & `Main Camera` post-processing) | Triggers `0.3s` full-screen visual effects on single-rebound streaks: **3rd streak** inverts screen colors (`ColorCurves`); **4th streak** immediately stops the previous effect and turns the screen White & Black (`ColorAdjustments`); **5th streak** immediately stops the previous effect and paints everything Blood-Like Red (`ChannelMixer` + `ColorCurves` + `Vignette`); **streaks > 5** do nothing to visual effects and only increase the streak counter. |
| **Additional technique 5: Infinite Rounds & Safe Obstacle Respawn** | `GameEvents.BrickDestroyed` (all bricks cleared) + ball exiting brick zone | `Assets/_Project/Scripts/Core/GameManager.cs`, `Assets/_Project/Scripts/Gameplay/LevelBuilder.cs`, `Assets/_Project/Scripts/Gameplay/Ball.cs`, `Assets/_Project/Scripts/Gameplay/Brick.cs` | Tracks remaining bricks and waits until the ball exits below the obstacle zone (`LevelBuilder.IsBallOutsideObstacleZone`) before respawning the full brick grid and increasing ball speed (`Ball.IncreaseSpeed`) each round to prevent collider overlap glitches. |

## Playtest and revision
Observation, change, and result:
1. **Physics Integrity During Squash & Stretch:** Applying scale animations directly to `Paddle` or `Ball` would alter their `BoxCollider2D`/`CircleCollider2D` bounds and cause erratic bounces. **Change:** Restricted all deformation and recoil in `CollisionReactionFeedback.cs` to the child `Visual` transforms (`Paddle/Visual` and `Ball/Visual`). **Result:** Snappy elastic visuals with 100% deterministic physics compared to `Before.unity`.
2. **Respawn Trail Streaking:** Teleporting the ball from `DeathZone` back to `startPosition` caused the `TrailRenderer` to draw a sharp line across the screen. **Change:** Added a 2-frame trail emission disable and `.Clear()` cycle on `GameEvents.BallLost` in `BallTrailAndLifeLossFeedback.cs`. **Result:** Respawns and restarts are visually clean with zero across-screen artifacts.
3. **Streak Screen Effect Overlapping & Readability:** Rapidly hitting 3, 4, and 5 bricks in a single rebound could stack post-processing volumes or make HUD text hard to read during color inversion. **Change:** Configured `StreakFeedback.cs` so higher streak tiers (up to 5) immediately disable the previous URP `Volume` before activating the new `0.3s` effect, temporarily override HUD text contrast colors, and ignore screen triggers for streaks `>= 6`. **Result:** Crisp, readable, non-conflicting screen transitions.
4. **Obstacle Respawn Collider Overlap & End-of-Run Flow:** Respawning bricks the instant the last brick broke could trap the ball inside newly spawned colliders, and losing all 3 lives in the starter silently reset score without rebuilding the board. **Change:** Added `LevelBuilder.IsBallOutsideObstacleZone()` to wait until the ball drops below the brick zone before rebuilding obstacles and increasing ball speed, and added the `GameOver` state + **GAME OVER / RESTART** menu in `HUD.cs` and `GameManager.cs`. **Result:** Glitch-free infinite round transitions and a complete arcade loop in `After.unity` while keeping `Before.unity` unchanged.

## Demonstration video
Link: *https://youtu.be/JCIchFUbuGA*

## AI use
Tools, contributions, and how you tested/revised the output (or state no AI used):
- **Tools used:** Antigravity AI assistant.
- **Contributions:** Assisted in implementing the event-driven feedback scripts (`BrickImpactFeedback`, `CollisionReactionFeedback`, `AudioFeedback`, `ScoreFeedback`, `CameraShakeFeedback`, `BrickDestructionFeedback`, `BallTrailAndLifeLossFeedback`, `StreakFeedback`), the `HUD` Game Over overlay menu, and the infinite round progression logic in `GameManager`, `LevelBuilder`, and `Ball`.
- **Testing & Revision:** Tested side-by-side against `Before.unity` in Unity `6000.3.2f1` across multiple rounds, high-combo rebounds, and life-loss scenarios to verify child-only visual deformations, audio cooldowns, URP post-processing streak transitions, safe post-zone obstacle respawns, and full state reset on Game Over restart.

## Credits
Starter adapted from [JuiceLab](https://github.com/serkoceyhan/JuiceLab) by Serkan Ceyhan, MIT. See LICENSE and SOURCE.md.
