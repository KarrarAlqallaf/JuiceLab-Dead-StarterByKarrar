# SWE402 Game Feel Submission

Read [START-HERE.md](START-HERE.md) to open the starter.

## Student
Name and student ID: Karrar *(add student ID here)*

## Run
Unity version: **6000.3.2f1**

Scenes, controls, and instructions:
- **Scenes:**
  - `Assets/_Project/Scenes/Before.unity`: Original unpolished baseline scene for side-by-side comparison.
  - `Assets/_Project/Scenes/After.unity`: Enhanced "juicy" scene containing all visual, audio, UI, and gameplay loop improvements.
- **Controls:**
  - Move the paddle horizontally using the **Mouse**, **A / D**, or **Left / Right Arrow** keys.
  - When all 3 lives are exhausted, click the **RESTART** button on the **GAME OVER** menu to start a fresh run.
- **Instructions:**
  1. Open the project in Unity `6000.3.2f1`.
  2. Open `Assets/_Project/Scenes/After.unity`, set the Game view aspect ratio to **16:9**, and press **Play**.
  3. Destroy all bricks to advance to the next round. Once the ball exits the brick zone, the obstacle grid respawns and the ball speed increases slightly each round for an endless arcade loop.

## Intended feel
**Heavy, Kinetic & Arcade-Responsive ("Punchy Cyber-Breakout"):**
Every interaction is designed to convey physical weight, momentum, and escalating tension without compromising deterministic collision physics:
- **Heavy Impact Weight:** Collisions feel solid and forceful through directional camera kick/shake, multi-layered brick debris + spark bursts, and immediate squash-and-stretch recoil on the visual child transforms of the ball and paddle.
- **Escalating Momentum:** Consecutive brick hits without touching the paddle raise the combo pitch of brick destruction audio, while clearing a full board rewards the player with an endless next round at slightly higher ball speed.
- **Clear State & Consequence:** Losing a ball triggers a low-end audio drop, downward camera trauma, trail reset, and a crimson screen/HUD pulse; losing all 3 lives halts play cleanly and presents a bold **GAME OVER** overlay with a **RESTART** button.

## Improvements
| Improvement | Trigger | Implementation file/object | Purpose |
|---|---|---|---|
| **Brick impact** | `GameEvents.BrickDestroyed` | `Assets/_Project/Scripts/Feedback/BrickImpactFeedback.cs` (`JuiceFeedback` GameObject) | Spawns a pooled two-layer particle burst at the brick's position and contact point: heavy gravity-affected colored brick fragments biased along the impact vector plus bright directional sparks. |
| **Reactive animation** | `GameEvents.BallHitPaddle`, `GameEvents.BallHitWall`, `GameEvents.BrickDestroyed` | `Assets/_Project/Scripts/Feedback/CollisionReactionFeedback.cs` (`JuiceFeedback` GameObject, animating `Paddle/Visual` & `Ball/Visual`) | Applies 3-stage squash-and-stretch deformation, downward paddle recoil, orientation-aligned ball compression, and color flash strictly on child `Visual` transforms so physics colliders remain untouched. |
| **Event-based audio** | `GameEvents.BallHitPaddle`, `GameEvents.BrickDestroyed`, `GameEvents.BallLost`, `GameEvents.BallHitWall` | `Assets/_Project/Scripts/Feedback/AudioFeedback.cs` (`JuiceFeedback` GameObject & child `AudioSource` channels + `Assets/_Project/Audio/*.wav`) | Plays dedicated, cooldown-guarded sound effects for paddle hits, wall bounces, ball loss, and brick destruction with combo-driven pitch escalation (`+0.035` pitch per consecutive brick hit). |
| **Interface feedback** | `GameEvents.ScoreChanged`, `GameEvents.LivesChanged`, `GameEvents.GameOver`, `GameEvents.GameRestarted` | `Assets/_Project/Scripts/Feedback/ScoreFeedback.cs`, `Assets/_Project/Scripts/Core/HUD.cs` (`Canvas` & `JuiceFeedback`) | Takes ownership of the score display via `HUD.ScoreHandledExternally` to animate a rolling counter, scale punch, and gold highlight flash on points gained; displays a full-screen **GAME OVER** overlay with an interactive **RESTART** button when all 3 lives are exhausted. |
| **Additional technique 1: Directional Camera Shake** | `GameEvents.BrickDestroyed`, `GameEvents.BallHitPaddle`, `GameEvents.BallHitWall`, `GameEvents.BallLost` | `Assets/_Project/Scripts/Feedback/CameraShakeFeedback.cs` (`JuiceFeedback` -> `Main Camera`) | Combines directional impact kicks with Perlin-noise trauma shake and fast exponential recovery back to the camera's exact base position. |
| **Additional technique 2: Brick Shatter Ghost** | `GameEvents.BrickDestroyed` | `Assets/_Project/Scripts/Feedback/BrickDestructionFeedback.cs` (`JuiceFeedback` GameObject) | Spawns a pooled sprite flash/collapse ghost at the destroyed brick's exact center, size, and color that expands briefly with a rotational tilt before vanishing. |
| **Additional technique 3: Dynamic Ball Trail & Life-Loss Pulse** | Continuous ball motion, `GameEvents.BrickDestroyed`, `GameEvents.BallLost` | `Assets/_Project/Scripts/Feedback/BallTrailAndLifeLossFeedback.cs` (`JuiceFeedback` -> `Ball/Visual`, `Main Camera`, `LivesText`) | Renders a tapered motion trail tinted by the last destroyed brick's color (cleared across respawn frames to prevent streaking) and pulses the background/lives HUD crimson when a life is lost. |
| **Additional technique 4: Infinite Rounds & Safe Obstacle Respawn** | `GameEvents.BrickDestroyed` (all bricks cleared) + ball exiting brick zone | `Assets/_Project/Scripts/Core/GameManager.cs`, `Assets/_Project/Scripts/Gameplay/LevelBuilder.cs`, `Assets/_Project/Scripts/Gameplay/Ball.cs` | Tracks remaining bricks and waits until the ball exits below the obstacle zone (`LevelBuilder.IsBallOutsideObstacleZone`) before respawning the brick grid and increasing ball speed (`Ball.IncreaseSpeed`) each round to prevent collider overlap glitches. |

## Playtest and revision
Observation, change, and result:
1. **Respawn Trail Streaking:** During initial testing, teleporting the ball from the `DeathZone` back to `startPosition` caused the `TrailRenderer` to draw a sharp streak across the screen. **Change:** Added a 2-frame trail emission disable and `.Clear()` cycle on `GameEvents.BallLost` in `BallTrailAndLifeLossFeedback.cs`. **Result:** Respawns and restarts are visually clean with zero across-screen artifacts.
2. **Obstacle Respawn Collider Overlap:** When the last brick was destroyed, respawning the brick grid immediately could trap the ball inside or above newly spawned bricks. **Change:** Added `LevelBuilder.IsBallOutsideObstacleZone()` so `GameManager` waits until the ball travels safely below the bottom row of the brick zone before rebuilding the obstacles and incrementing ball speed. **Result:** Seamless infinite round transitions with no physics glitches.
3. **End-of-Run Flow:** Originally, losing all 3 lives silently reset the score without rebuilding the board or notifying the player. **Change:** Added a `GameOver` state that stops the ball and paddle and shows a **GAME OVER** menu with a **RESTART** button that resets lives, score, ball speed, and the full brick grid. **Result:** Clear player feedback and a complete, repeatable arcade loop.

## Demonstration video
Link: *(Insert video demonstration link here)*

## AI use
Tools, contributions, and how you tested/revised the output (or state no AI used):
- **Tools used:** Antigravity AI assistant.
- **Contributions:** Assisted in structuring the event-driven feedback components (`BrickImpactFeedback`, `CollisionReactionFeedback`, `AudioFeedback`, `ScoreFeedback`, `CameraShakeFeedback`, `BrickDestructionFeedback`, `BallTrailAndLifeLossFeedback`), the `HUD` Game Over overlay menu, and the infinite round progression logic in `GameManager` and `LevelBuilder`.
- **Testing & Revision:** Playtested in Unity `6000.3.2f1` across multiple rounds and life-loss scenarios to verify that visual deformations only affect child `Visual` objects, audio cooldowns prevent clipping, obstacle respawns wait until the ball leaves the brick zone, and the Game Over restart button cleanly resets all game state.

## Credits
Starter adapted from [JuiceLab](https://github.com/serkoceyhan/JuiceLab) by Serkan Ceyhan, MIT. See LICENSE and SOURCE.md.
