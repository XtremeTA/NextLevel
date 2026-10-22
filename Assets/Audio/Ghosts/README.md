# Ghost effects

Original synthesized effects for the soul-brick enemies, generated locally with
`Tools/Audio/GenerateGhostAudio.cs`. No recordings or third-party samples are used.
All files are mono, 48 kHz, 16-bit PCM WAVs, with short fades and soft echo tails.

- `Ghost_Appear_1/2`: airy descending spirit wail when a large ghost appears.
- `Ghost_Burst_1/2`: muted spectral puff when it reaches the floor.
- `Ghost_Split_1/2`: short rising wisps when the two smaller ghosts emerge.

The game selects a variant for each event. Adjust their volumes and variations
under **Game Audio** in SampleScene. Effects use the existing pooled sound system
so destroying the enemy does not cut off the sound. They do not loop or add
continuous noise for each active enemy.

Regenerate from a PowerShell prompt at the repository root:

```powershell
Add-Type -Path Tools/Audio/GenerateGhostAudio.cs
[GhostSoundDesigner]::Generate((Join-Path (Get-Location) 'Assets/Audio/Ghosts'))
```
