# Title-screen artwork

## Game-over screen

The game-over background is `Assets/PixelArt/IMG_1775.jpeg`. Transparent
cutouts of the supplied logo (`IMG_1776.jpeg`) and brighter Retry button
(`IMG_1778.jpeg`) are saved as `Assets/UI/GameOverLogo.png` and
`Assets/UI/RetryButton.png`. Originals remain intact. Unity sprite import
rectangles trim empty margins without modifying the PNG files.

Both cutouts used the built-in imagegen tool with this prompt, substituting
`GAME OVER` or `RETRY` for the quoted text:

> Use case: background-extraction. Asset type: transparent Unity game menu artwork.
> Input image is the EDIT TARGET. Remove ONLY the exterior white paper/background
> and replace it with true transparency. Preserve the supplied painted artwork,
> all brush strokes, splashes, black edges, red color, geometry, and original
> lettering exactly. Text (verbatim): "GAME OVER" / "RETRY". Do not redesign,
> redraw, reinterpret, add, or relayout any content. Keep white/cream highlights
> that belong inside the lettering and artwork. Keep the full artwork unclipped
> with a small transparent margin; no white matte, no checkerboard baked into
> pixels, no new text, no watermark. The result must be a clean transparent PNG
> cutout of this exact supplied artwork.

## Title screen

Original supplied assets are preserved:

- Background: `Assets/PixelArt/IMG_1767.JPG`
- Logo source: `Assets/PixelArt/IMG_1768.JPG`
- Start button source: `Assets/PixelArt/IMG_1769.JPG`

Transparent derivatives saved in this project:

- `Assets/UI/TenCourtsLogo.png`
- `Assets/UI/StartButton.png`

These two PNGs were produced with the built-in imagegen tool in
background-extraction edit mode. Unity's sprite import rectangles trim empty
margins for layout; the PNG files keep the generated resolution and alpha.

The final prompt for each derivative was the following, with `TEN COURTS` for
the logo and `START` for the button:

> Use case: background-extraction. Asset type: transparent Unity game menu
> artwork. Input image is the EDIT TARGET. Remove ONLY the exterior white
> paper/background and replace it with true transparency. Preserve the supplied
> painted artwork, all brush strokes, splashes, black edges, color, geometry, and
> original lettering exactly. Text (verbatim): "TEN COURTS" / "START". Do not
> redesign, redraw, reinterpret, add, or relayout any content. Keep white
> highlights that belong inside the artwork. Keep the full artwork unclipped
> with a small transparent margin; no white matte, no checkerboard baked into
> pixels, no new text, no watermark. The result must be a clean transparent PNG
> cutout of this exact supplied artwork.
