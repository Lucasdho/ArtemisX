# Prompt: replace the old Artemis X UI with the new Toy UI in Unity

You are working in the Unity project at `/Users/lucasoliveira/Developer/ArtemisX` (Unity MCP is connected). Scene: `Assets/Scenes/HandMadeBase.unity`. The NASA Space Apps submission closes **Sun 15/11 23:59**, so work fast, keep changes visual, and don't break gameplay.

## Goal

Re-skin every existing UI element with the new **Artemis X Toy UI**: new sprites, new fonts and new colors. Keep every GameObject name, every component and every serialized reference the view scripts use. This is a re-skin, not a rebuild.

## Read first

1. The design system's brand book: https://claude.ai/artifact/T19gFv4egqJT4onFqTpaRD. Read `project/README.md`, then each component's `README.md` (ResourceBar, Button, Panel, Recipe, Banner, KeplerDialogue). It holds the exact sizes, colors and rules.
2. The sprites and fonts are already in the project at `Assets/GameAssets/Free Icon Pack v3.1 (Basic)/Claude outputs/ToyUI/`. They're already imported as Sprite (Full Rect) with their 9-slice borders set. `README.md` in that folder lists every file and its use.
3. Icons: `Assets/GameAssets/Free Icon Pack v3.1 (Basic)/`, always the "1st Outline" variant. Use the 64 px PNGs up to 48 px on screen and the 256 px PNGs above that.

## Safety

- Before you change anything, duplicate the scene to `Assets/Scenes/HandMadeBase_preToyUI.unity`, or make a git commit if the repo is clean.
- Don't rename, delete or reparent objects the view scripts reference: `GameplayHud`, `ResourcePillView`, `MissionCardView`, `DialogueBoxView`, `ContextConfirmPopupView`, `RoverSiteMarkerView`, `ModalCardView`, `StartScreenView`, `AstronautCardView`, `EndScreenView`, `TutorialHighlightView`, `DayCounterView`, `SupplyTagView`, `SpaceWeatherWidgetView`, `RoverStatusButtonView`, `NightTransitionSkipView`, `DialogueChoiceButtonView`. Before changing a sub-tree, open the script that drives it and check what it sets at runtime (sprites, colors, text, sizes). Runtime code can overwrite your edits; the old pill colors are a likely example. Where it does, make the smallest code change, for example the color it sets.
- **Keep legacy `UI.Text`.** Don't migrate to TextMeshPro: the views reference `Text` fields, and swapping the component would break them. Legacy Text renders the new TTF fonts directly.
- If an element isn't covered below, change only its font and its base sprite.
- Save the scene often. After each section, enter Play mode, check the Console for errors, and take a Game view screenshot.

## Global rules

**Fonts** (in the ToyUI folder):
- `Kenney Future` → `LuckiestGuy-Regular`. Use it for titles, labels, buttons, numbers and names. Luckiest Guy has no lowercase, so everything renders in capitals. That's intended; leave the strings as they are.
- `Kenney Future Narrow` → `AtkinsonHyperlegible-Bold`. Use it for every sentence: dialogue lines, card bodies, hints, roles, subtitles and placeholders.
- Luckiest Guy sits high in its line box. If a label looks high inside a button, nudge it down about 14% of its font size, using the RectTransform offset or the Text's alignment and line spacing.
- Minimum body size is 13 px. Dialogue is 21, card body 17, buttons 26 (22 inside cards), titles 28–40.

**Text colors:**
- On white, use ink `#040435`. Secondary text is `#56657F`.
- On color (orange, green, red, dark plates), use white and add two components, **Outline** (Effect Color `#040435`, Effect Distance 2, −2) and **Shadow** (Effect Color `#040435`, Effect Distance 0, −3).
- Gains: `#2FCB4E` with ▲. Losses: `#FF3B30` with ▼, using the real minus sign "−". Both always get the ink Outline.

**Sprites:**
- On every Image you re-skin: Image Type **Sliced**, **Pixels Per Unit Multiplier 2**, and **Image.color white**. The new sprites already have their colors baked in. Exceptions: `recipe-arrow`, `ribbon-tail-*`, `rays`, `bubble-tail` and `kepler` are Simple sprites.
- The ink outline is baked into the new sprites. Disable the old separate border children (`PanelBorder`, `Border`, `InputBorder`, `PillBorder`) by unticking the Image, not by deleting the object.

**Buttons:**
- Transition **Sprite Swap**, Pressed Sprite = the matching `-pressed` sprite, Highlighted = the normal sprite, Disabled Sprite = `button-disabled`.
- Kinds: `button-primary` (orange: the ONE main action on screen), `button-secondary` (white: back, cancel, skip, help), `button-success` (green: confirm, collect), `button-danger` (red: destructive).
- Height 64 px, or 58 px inside cards.

**Panels:**
- `panel` has the dark header strip. Use it when the card has a title: put the title in the top 58 px, in white Luckiest Guy 28 with the Outline and Shadow.
- `panel-plain` is the white card without a header.

**Canvas:** check that every Canvas Scaler is Scale With Screen Size, 1920×1080, Match 0.5.

**Character:** the base computer is now **Kepler**, a round orange helper drone (`kepler.png`). Replace every *displayed* "Bauzer" string with "Kepler": name labels, dialogue data, ScriptableObjects and story text. Don't rename C# identifiers.

## Element by element

### HUD (`------------HUD-----------------`)

**ResourcePills → resource bar.** Target look: `ResourceBar` in the design system, a 196×64 chip with a 48 px ink track, a 60 px icon breaking out on the left, the value in Luckiest Guy 30 white with Outline, and the daily change on the right in green ▲ / red ▼.
- `Pill` → sprite `hud-track`. Use `hud-track-low` when the resource is low, if `ResourcePillView` or `UIAlertPulseView` has a low state.
- `PillBorder` → disable.
- `Bubble` → disable its Image and keep the child `Icon`. Set `Icon` to the resource's 64 px icon (Lighting, Crystal Blue, Apple, Settings 1), 60×60, overlapping the track's left end.
- `Letter` → disable.
- `Amount` → Luckiest Guy 30, white, Outline 2.5 plus Shadow 0, −4.
- `Delta` → Luckiest Guy 22, green or red by sign, with ▲ or ▼ and Outline. If `ResourcePillView` sets the delta text or color, update the code to write the arrow, the real minus sign and these two colors.
- Fill: add a child `Fill` under a `RectMask2D` inside `Pill`, using `hud-fill-<power|water|food|parts>` (Sliced, already colored). Set its width to value ÷ capacity, with capacity 60 for parts and 100 for the rest; add that to `ResourcePillView`, which already gets the value. Add `hud-shine` (Simple) at the fill's right end, visible only when the fill is 45–60% long.
- `AlertBadge` → its 64 px Warning icon instead of the red circle and "!".
- **`Pill_Oxygen`:** the design says oxygen is no longer a tracked resource. Don't remove it. Leave it as is and report it so Lucas can decide.

**NextDayButton** → `button-primary`, label "Next day", with the Calendar icon. It's the day's one orange action, bottom right.

**MorningCard → Panel:**
- `Card` → `panel`, with `Banner/Text` as the header title.
- `Title` and `Body` → Atkinson Hyperlegible on white, ink color.
- `Deltas/Delta_n` → white `recipe-tray` chips, the icon 34 px and the amount in Luckiest Guy 24 green/red with Outline.
- `Bauzer` → Kepler: `Portrait` gets `kepler.png`, and `Name` says "Kepler".
- `Buttons/ButtonA` → `button-primary`; `ButtonB` → `button-secondary`.

**DialogueCanvas → KeplerDialogue:**
- `BottomBox` → `bubble`. Add `bubble-tail` (Simple) on its left edge, pointing at the portrait.
- `PanelBorder` → disable.
- `PortraitFrame` → disable its Image and show `kepler.png` at about 170 px to the left of the bubble.
- `SpeakerNameBanner` → `name-plate`, on the bubble's top-left edge, its text "KEPLER" in white Luckiest Guy 22 with Outline.
- `BodyTextLabel` → Atkinson Hyperlegible Bold 21, ink color, **left-aligned**.
- `ContinuePrompt` → make it read "Next ▶" in white Luckiest Guy 22. Put a new parent Image with `button-next` (Sliced, 50 px tall) behind it, on the bubble's bottom-right edge, half outside the bubble.
- `ChoicesRoot` buttons → `button-primary` / `button-secondary`.

**ContextConfirmPopup → Panel + loss stickers:**
- Root → `panel`, with `Title` in the header.
- `Description` → Atkinson Hyperlegible 17 in ink.
- `ConfirmButton` → `button-primary`; `CancelButton` → `button-secondary`.
- `CostRow`: show each cost as a **loss sticker**, a red pill (`loss-sticker`) with a 26 px icon and "−20" in white Luckiest Guy 19 with Outline. Place the stickers on the ConfirmButton's **top-right edge** (offset about right −12 px, top −26 px), side by side and growing to the left. Move `CostRow` there with a layout change only, keeping it as a child of the popup if a script relies on its parent. A free choice shows no sticker.

**These HUD pieces get fonts and base sprites only**, because their new designs are still in review:
- `MissionCard` → `panel` with "Missions" in the header. Progress tracks stay as they are, recolored orange `#FF5A1F` on `#E3E8F0`.
- `WeatherWidget` → `panel-plain`.
- `SupplyNotice` → `name-plate`, in white text.
- `DayPill` → `banner-day`.
- `RoverButton` and `HelpButton` → `button-secondary`.
- `RoverMap/Markers/*` → `panel-plain`, with `Stem` left as is. The chips inside → `recipe-tray`.
- `RoverMap/Title` → `name-plate`.
- `BackButton` → `button-secondary`.
- `NightSkip/SkipButton` → `button-secondary`.

### StartScreenCanvas
- `MainPanel` → `panel-plain`, with `PanelBorder` disabled.
- `TitleText` → Luckiest Guy in ink; `SubtitleText` → Atkinson Hyperlegible in `#56657F`.
- `AstronautCard_n` → `panel-plain`. `SelectionBorder` keeps its on/off logic but tinted orange `#FF5A1F`. `CallsignLabel` → Luckiest Guy; `RoleLabel` → Atkinson Hyperlegible.
- `SelectedTagPill` → `button-success`, Image only.
- `NicknameInputField` → `panel-plain`, with `InputBorder` disabled.
- `LaunchMissionButton` → `button-primary`.
- `AiArtCredit` → Atkinson Hyperlegible 13.

### Run4_TutorialCanvas
- `HintBanner` → `banner-day`, its text in white Luckiest Guy with Outline.
- `SkipButton` → `button-secondary`.
- `HighlightFrame` → leave the sprite and tint it orange `#FF5A1F`.

### Run4_EndScreenCanvas
- `Card` → `panel`, with `Title` in the header; `Border` → disable.
- `Star1–3` → Golden Star "1st Outline" at 256 px for earned stars, and the black star at 20% opacity for unearned ones.
- `BauzerPortrait` → `kepler.png`; `BauzerName` → "KEPLER" on a `name-plate`.
- `BauzerLine` → Atkinson Hyperlegible.
- `Histogram` → `panel-plain`.
- `PlayAgainButton` → `button-primary`.

## Done when

- No Kenney Future font is left in the scene. Search with `t:Font` references, or check every `Text.font`.
- No `UICircle`, `space_panel_base`, `space_frame_border`, `btn_green`, `btn_blue` or `pill_green` sprite is left on a visible UI Image, apart from the tracks deliberately left in Mission goals.
- Play mode runs from the start screen through day 2 with no new Console errors.
- Exactly one orange button is visible at a time on the HUD.
- You save the scene and send Lucas Game view screenshots of: the start screen, the HUD on day 1, a Kepler dialogue, the confirm popup with costs, the morning card and the end screen.
- You list anything you couldn't do or left as is, including `Pill_Oxygen`.
