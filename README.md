# NextLevel
Next Level Arcade Project

Open with Unity **6000.6.3f1** and play `Assets/Scenes/TitleScreen.unity`.
Click the painted Start button or press Enter/Space to enter the game. Builds
start at the title screen; opening SampleScene directly still supports gameplay
testing.
Move with A/D or the arrow keys; press W to jump and Space to parry.
Jumping requires a supporting ground contact. Adjust **Jump Height** on the
Player to change the jump's height (default: 2.5 world units).

Falling through a gap in the floor costs a life and plays the hurt flash while the player is
lifted back up through the gap and set down on the last floor tile they stood
on, or the nearest surviving tile if it broke (the start position if none are
left). Adjust **Catch Pause**, **Lift Speed**, and **Hover Height** under the
Player's **Fall Recovery** settings.

The ball bounces off all four edges of the main camera's view, with clearance for
its collider and sprite. These bounds follow camera size/aspect changes and keep
the ball in view even after floor blocks break. Adjust **Screen Padding** on the
Ball to leave additional space at the edges.

Parries send the ball upward toward the direction the player is facing, keeping
that direction when the player stops moving. Standing shots aim 25 degrees away
from vertical; holding a movement direction widens the shot to 50 degrees.
Launches, resets, and body contacts use the same aim. Adjust **Player Shot Angle**
and **Moving Shot Angle** on the Ball to tune these angles.
Side-wall and left/right screen-edge bounces are silent.

The ball avoids flat or vertical traps by keeping bounces at least 18 degrees
away from either axis. If it returns to the same bounce position and heading
after four or more unproductive bounces, it gets a small 12-degree angle nudge.
Brick/floor hits and player shots reset this detection, keeping ordinary rallies
and aimed parries predictable. Speed and perfect-parry boosts are preserved.
Adjust the Ball's **Prevent Repeating Paths** settings to tune the minimum angle,
repeat threshold, position tolerance, and escape nudge.

Catch the ball within the first 0.06 seconds of pressing Space for a perfect
parry: it gets a 35% speed boost for 0.85 seconds, easing back to normal over the
final 0.25 seconds. Its glow and trail brighten during the boost. Regular parries
use normal speed; boosts refresh rather than stack. Adjust **Perfect Parry Window**
on the Player and the boost settings on the Ball. Each successful parry gives a
brief expanding impact flash and a small camera bump; perfect parries have a
stronger flash. **Camera Feedback** on Main Camera controls the bump and affects
rendering only, keeping physics and brick positions fixed.

The ball uses a neon green material and a short, tapering green trail. Adjust
**Trail Duration** and **Trail Width** on the Ball's **Ball Visuals** component.
The **Ball Glow** scene object uses `Assets/Settings/BallGlowProfile.asset`;
adjust its Bloom intensity to change the glow strength. The trail clears on reset.

## Scene atmosphere

SampleScene's **Scene Atmosphere** adds slowly drifting haze and 24 small warm
motes behind the player, ball, and bricks. The effect fits the camera when the
window changes size. Adjust **Haze Opacity**, **Haze Drift**, **Mote Count**, and
**Mote Opacity** on that object, or disable the component to switch it off.
The motion freezes while paused or on game over and resets with a new game.

## Title screen and soundtrack

TitleScreen uses the supplied temple background, Ten Courts logo, and painted
Start button. It supports mouse and keyboard input, scales with the window, and
fades into SampleScene. Original artwork is retained; transparent logo/button
copies and their generation notes are in `Assets/UI/ARTWORK.md`.

`Assets/Prefabs/BackgroundAudio.prefab` is present in both scenes and keeps one
persistent theme source and one ambience source. The main theme and loopable
ambience play separately: the theme plays on the title screen and stops as soon
as Start is pressed. Ambience starts when gameplay loads, stops on pause or game
over, and starts again when gameplay resumes or restarts. Adjust **Title Theme
Volume**, **Gameplay Ambience Volume**, and the fade-in's **Fade Duration** on the
prefab. The loops use 2D, pitch-1 playback and PCM streaming, independently of
gameplay sound effects.

## Brick rows

SampleScene's **Brick Rows** object automatically spawns four rows of nine bricks
when play starts. Once every brick in the bottom row has finished breaking, the
remaining rows slide down one row while a fresh row slides in from above the
screen. The top row's sprite is flush with the main camera's top edge. Clearing an
upper row does not advance the board until it reaches the bottom; partially
cleared rows keep their gaps as they descend.

Adjust **Row Count**, **Bricks Per Row**, **Column Spacing**, **Row Spacing**, and
**Row Shift Duration** on its **Brick Row Manager** component before playing.
The default slide takes 0.35 seconds and uses interpolated kinematic physics bodies
for smooth movement. **Play Area Camera** controls the top edge; **Top Left
Position** controls the leftmost X and depth, with Y used as a fallback if no
orthographic camera is available. With the default camera, brick centers run from
Y = 7.5 at the top to Y = 4.5 at the bottom.

## Special bricks and enemies

Rows start with one soul brick (the red Yokai brick). New rows get two after
90 seconds of gameplay and three after three minutes, capped at three out of
nine bricks. Existing rows keep their bricks; the increase appears as new rows
arrive. Pausing and game over stop the progression clock; restarting resets it.
Breaking a soul brick plays its own
animation and releases one falling enemy. When that enemy reaches a surviving
floor tile, it plays the supplied explosion/split animation and releases two
smaller enemies running in opposite directions 40% through the animation
(0.3 seconds with the default frames and frame rate). The explosion finishes
behind them. **Split Animation Progress** on **FallingEnemy** controls the release
point. Jump over them with W. Small
enemies that reach a side wall turn upward and break the first intact brick they reach,
awarding its usual point and disappearing. Special bricks keep their normal
enemy release behavior when broken this way. Enemies fall through broken floor
tiles and despawn after leaving the screen or reaching their maximum lifetime.

**Special Bricks Per Row** on **Brick Rows** sets the starting count; set it to 0
to use only regular bricks. **Special Brick Increase Interval** and **Maximum
Special Bricks Per Row** control the gentle difficulty ramp. The **YokaiBrick**, **FallingEnemy**, and **FloorEnemy**
prefabs hold the break frames, fall speed, explosion timing, run speed, and
lifetime settings. Enemy sprites use complete, evenly spaced animation frames
with point filtering and 32 pixels per world unit.
Default speeds are 9 units/second for falling, 8 for running, and 10 for rising.

The player starts with three lives. Enemy contact removes one life, plays the
hurt animation, and briefly knocks the player away.
The enemy that dealt the damage disappears immediately, including a falling
enemy, which will then never split. Contacts during invulnerability do not cost
another life. Hurt acts as a stun: movement, jumping, and parrying stay locked
and the player flashes red/white for as long as the Hurt SFX is audible (about
1.9 seconds; trailing silence in the file is ignored, so replacing the clip
updates the stun automatically). This applies to both enemy hits and falls.
The animation cannot be interrupted by run/parry input. **Enemy Hits** on the
Player controls how long knockback pushes (0.25 seconds), extra invulnerability
after the stun ends (1 second), and knockback strength.
Hurt applies its first sprite immediately on the hit and keeps priority over
queued movement/parry transitions through the final displayed frame.
Lives show as hearts under the scoreboard: a full heart per remaining life and
an empty heart per lost one. Set the heart images and full/empty sprites on
**GameManager**.
The third hit lets the full stun finish, then shows the final score and pauses
gameplay. Click **Restart** or
press R/Enter to start again with three lives and a fresh board. **Starting Lives**
on **GameManager** changes the initial life count.

The score shows on the pixel-art **Scoreboard** in the top-left corner as three
digits (up to 999). When points are added, the board quickly counts up one at a
time to the new total. Adjust **Tick Interval** and **Max Tick Duration** on
**Score Counter**; large jumps speed up so a count-up never takes longer than
the max duration.

## Audio

Select **Game Audio** in SampleScene to adjust the overall effects volume and
each sound's clips, volume, pitch variation, volume variation, and minimum
interval between plays. Variation is centered on the original pitch and volume.

| Event | Clips | Pitch variation |
| --- | --- | --- |
| Ball hits the top/bottom screen edge or a horizontal surface | Ball_Hit | +/-3% |
| Brick breaks or a floor block takes its final hit | Brick_Break_1, 2, 3 | +/-4% |
| Floor block takes a non-final hit | Tile_Crack | +/-3% |
| Ball is successfully parried | Parry_1, 2 | +/-2.5% |
| Player falls through the floor | Hurt SFX | +/-2% |
| Enemy hits the player | Hurt SFX | +/-2% |
| Player loses the final life | Death Sound | +/-2% |
| Large ghost appears | Ghost_Appear_1, 2 | +/-2.5% |
| Large ghost reaches the floor | Ghost_Burst_1, 2 | +/-2.5% |
| Two smaller ghosts emerge | Ghost_Split_1, 2 | +/-2.5% |

Clip groups avoid immediately repeating the same clip. Each effect plays on its
own pooled 2D AudioSource under Game Audio, so overlapping sounds keep their own
pitch and destroying a brick does not cut off its sound. Short per-cue intervals
prevent duplicate contact sounds; if all 16 voices are busy, further effects are
skipped until a voice finishes. Audio variation does not change gameplay's random
sequence.

Ghost effects are soft, locally synthesized spirit sounds, with two variants per
event and short echo tails. **Ghost Effects** on **Game Audio** controls their
volumes; they play once per event and use no additional looping sources. Their
WAVs and generation notes are in `Assets/Audio/Ghosts`; the reproducible sound
generator is `Tools/Audio/GenerateGhostAudio.cs`.

Main_Theme and Loopable_Ambience are controlled by the persistent
**Background Audio** prefab rather than the effects pool.
