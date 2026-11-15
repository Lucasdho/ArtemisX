# Artemis X Toy UI: Unity sprites

All sprites are rendered at 2x. Import as Sprite (2D and UI), Mesh Type Full Rect, Pixels Per Unit 100. On the Image, use Image Type Sliced and Pixels Per Unit Multiplier 2 (Simple for the sprites marked Simple). Borders are Left, Top, Right, Bottom in PNG pixels.

Fonts: make TMP SDF assets from LuckiestGuy-Regular.ttf and AtkinsonHyperlegible-Bold.ttf (90 px sampling, 9 px padding, include U+2212). White-on-color material: Outline 0.25 #040435, Underlay Offset Y -0.6 #040435. Nudge Luckiest Guy labels down 14% of the font size.

| File | PNG | Borders L, T, R, B | Use |
| --- | --- | --- | --- |
| `button-primary.png` | 256x142 | 64, 36, 36, 50 | Button face 64 px tall + lip. Sprite Swap: normal → pressed. |
| `button-primary-pressed.png` | 256x142 | 64, 46, 36, 40 | Button face 64 px tall + lip. Sprite Swap: normal → pressed. |
| `button-secondary.png` | 256x142 | 64, 36, 36, 50 | Button face 64 px tall + lip. Sprite Swap: normal → pressed. |
| `button-secondary-pressed.png` | 256x142 | 64, 46, 36, 40 | Button face 64 px tall + lip. Sprite Swap: normal → pressed. |
| `button-danger.png` | 256x142 | 64, 36, 36, 50 | Button face 64 px tall + lip. Sprite Swap: normal → pressed. |
| `button-danger-pressed.png` | 256x142 | 64, 46, 36, 40 | Button face 64 px tall + lip. Sprite Swap: normal → pressed. |
| `button-success.png` | 256x142 | 64, 36, 36, 50 | Button face 64 px tall + lip. Sprite Swap: normal → pressed. |
| `button-success-pressed.png` | 256x142 | 64, 46, 36, 40 | Button face 64 px tall + lip. Sprite Swap: normal → pressed. |
| `button-disabled.png` | 256x142 | 64, 36, 36, 50 | Button face 64 px tall + lip. Sprite Swap: normal → pressed. |
| `button-disabled-pressed.png` | 256x142 | 64, 46, 36, 40 | Button face 64 px tall + lip. Sprite Swap: normal → pressed. |
| `button-next.png` | 200x112 | 56, 32, 32, 44 | Kepler bubble Next button, 50 px tall. |
| `loss-sticker.png` | 96x70 | 32, 30, 32, 36 | Cost sticker on a button: put a 26 px icon + Luckiest Guy 19 number inside. |
| `panel.png` | 240x292 | 48, 128, 48, 58 | Header-band panel: 58 px header on top (do not stretch it vertically), white body stretches. |
| `recipe-tray.png` | 96x96 | 32, 32, 32, 32 | Tray behind a recipe. |
| `recipe-arrow.png` | 92x60 | Simple | Simple sprite. |
| `banner-day.png` | 400x182 | 84, 88, 84, 90 | Toy pill banner, 84 px tall: stretch horizontally only. Icon breaks out on the left. |
| `banner-alert.png` | 400x182 | 84, 88, 84, 90 | Toy pill banner, 84 px tall: stretch horizontally only. Icon breaks out on the left. |
| `ribbon-plate.png` | 240x212 | 40, 100, 40, 110 | Achievement plate: stretch horizontally only. Tails behind it, star on top, rays behind the star. |
| `ribbon-tail-left.png` | 152x132 | Simple | Simple sprite, 34 px outside the plate, 24 px below its top. |
| `ribbon-tail-right.png` | 152x132 | Simple | Simple sprite, 34 px outside the plate, 24 px below its top. |
| `rays.png` | 440x440 | Simple | Simple sprite behind the star; rotate one turn per 8 s. |
| `bubble.png` | 240x254 | 60, 60, 60, 84 | Kepler speech bubble. |
| `bubble-tail.png` | 80x108 | Simple | Simple sprite, overlap the bubble's left edge by 6 px, 30 px above its bottom. |
| `name-plate.png` | 160x86 | 28, 40, 28, 44 | Kepler name plate: stretch horizontally only. |
| `kepler.png` | 512x512 | Simple | Kepler at 256 (512 px PNG). Simple sprite. |
| `hud-track.png` | 192x106 | 48, 48, 48, 58 | Resource bar track, 48 px tall. |
| `hud-track-low.png` | 192x106 | 48, 48, 48, 58 | Track with the loss outline, for a low resource. |
| `hud-fill-power.png` | 128x84 | 42, 42, 42, 42 | Power fill, already colored: width = value / capacity, under a RectMask2D. |
| `hud-fill-water.png` | 128x84 | 42, 42, 42, 42 | Water fill, already colored: width = value / capacity, under a RectMask2D. |
| `hud-fill-food.png` | 128x84 | 42, 42, 42, 42 | Food fill, already colored: width = value / capacity, under a RectMask2D. |
| `hud-fill-parts.png` | 128x84 | 42, 42, 42, 42 | Parts fill, already colored: width = value / capacity, under a RectMask2D. |
| `hud-shine.png` | 32x20 | Simple | Shine at the fill end when the fill is 45-60% long. |

Icons stay in the Free Icon Pack folder ("1st Outline", 64 px up to 48 px on screen, 256 px above).
Design system: https://claude.ai/artifact/T19gFv4egqJT4onFqTpaRD
