# NextLevel
Next Level Arcade Project

Open with Unity **6000.6.3f1** and play `Assets/Scenes/SampleScene.unity`.
Move with A/D or the arrow keys; press W to jump and Space to parry.
Jumping requires a supporting ground contact. Adjust **Jump Height** on the
Player to change the jump's height (default: 2.5 world units).

The ball bounces off all four edges of the main camera's view, with clearance for
its collider and sprite. These bounds follow camera size/aspect changes and keep
the ball in view even after floor blocks break. Adjust **Screen Padding** on the
Ball to leave additional space at the edges.

## Audio

Select **Game Audio** in SampleScene to adjust the overall effects volume and
each sound's clips, volume, pitch variation, volume variation, and minimum
interval between plays. Variation is centered on the original pitch and volume.

| Event | Clips | Pitch variation |
| --- | --- | --- |
| Ball hits a wall or another ordinary collider | Ball_Hit | +/-3% |
| Brick breaks or a floor block takes its final hit | Brick_Break_1, 2, 3 | +/-4% |
| Floor block takes a non-final hit | Tile_Crack | +/-3% |
| Ball is successfully parried | Parry_1, 2 | +/-2.5% |

Clip groups avoid immediately repeating the same clip. Each effect plays on its
own pooled 2D AudioSource under Game Audio, so overlapping sounds keep their own
pitch and destroying a brick does not cut off its sound. Short per-cue intervals
prevent duplicate contact sounds; if all 16 voices are busy, further effects are
skipped until a voice finishes. Audio variation does not change gameplay's random
sequence.

Main_Theme, Hurt SFX, and Death Sound are intentionally not connected yet.
