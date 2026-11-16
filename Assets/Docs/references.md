# Artemis X: external assets and references

Every third-party asset used in the project, for the NASA Space Apps submission.

## 3D models

| Pack | Author | Licence | URL |
|---|---|---|---|
| KayKit Space Base Bits 1.0 (SOURCE) | Kay Lousberg | CC0 1.0 | https://kaylousberg.itch.io/ |
| KayKit Resource Bits 1.0 (FREE) | Kay Lousberg | CC0 1.0 | https://kaylousberg.itch.io/ |
| KayKit Character Animations 1.1 (Mannequin) | Kay Lousberg | CC0 1.0 | https://kaylousberg.itch.io/ |
| Ultimate Space Kit (characters) | Quaternius | CC0 1.0 (per quaternius.com; no licence file in the imported folder, to be verified) | https://quaternius.com/ |

## UI, icons and audio

| Pack | Author | Licence | URL |
|---|---|---|---|
| UI Pack 2.0 | Kenney (Kenney Vleugels) | CC0 1.0 | https://www.kenney.nl/ |
| UI Pack: Sci-fi 2.0 (space expansion) | Kenney | CC0 1.0 | https://www.kenney.nl/ |
| Emotes | Kenney | CC0 1.0 | https://www.kenney.nl/ |
| Space Kit (3D models, imported) | Kenney | CC0 1.0 | https://www.kenney.nl/ |
| Free Icon Pack v3.1 (Basic) (imported) | gvesster | Free Icon Pack licence (commercial use allowed, no resale; see `Assets/GameAssets/Free Icon Pack v3.1 (Basic)/License.txt`) | https://gvesster.itch.io/ |
| Interface Sounds 1.0 | Kenney | CC0 1.0 | https://www.kenney.nl/ |
| Kenney Future font (bundled with the UI packs) | Kenney | CC0 1.0 | https://www.kenney.nl/ |

## Original assets

- Resource icons (power, water, oxygen, food, parts): made by the Artemis X team in Figma.
- All code, dialogue copy, game design and the Bauzer character concept: the Artemis X team.

## AI-generated assets (declared for the NASA Space Apps rules)

Full prompts and tools are in `Assets/Docs/ai-assets-log.md`. All were made with Google `gemini-3.1-flash-image`
(SynthID invisible watermark), except `icon_weather_radiation.png` (procedural Python render).
The start screen shows an "AI-generated art" caption; the images themselves carry no visible watermark.

| Asset | Path |
|---|---|
| Bauzer portraits (neutral, happy, worried) | `Assets/_UI/Dialogue/portrait_bauzer_*.png` |
| Astronaut portraits NOVA, LEO, MAYA, KAI (+ `portrait_player.png`) | `Assets/_UI/Dialogue/portrait_astronaut_0..3.png` |
| Space weather icons (calm, storm) | `Assets/_UI/Icons/icon_weather_calm.png`, `icon_weather_storm.png` |
| Space weather icon (radiation, procedural, not AI) | `Assets/_UI/Icons/icon_weather_radiation.png` |
| Artemis X logo emblem | `Assets/_UI/Start/artemisx_logo_emblem.png` |

## Credits

- **Bauzer**, the base computer and only guide in the game, is named in honour of
  **Dr. Claudia Maria Bauzer Medeiros** (Professor Emerita, Institute of Computing, Unicamp), a pioneer of
  computer science research in Brazil.

## Code libraries

| Library | Author | Licence | URL |
|---|---|---|---|
| DOTween (free) | Demigiant (Daniele Giardini) | DOTween licence (free for commercial use) | http://dotween.demigiant.com/license.php |

## Unity packages

Unity Engine 6000.3, Universal Render Pipeline, Cinemachine, Input System: Unity Technologies, Unity Companion / Unity Terms of Service.

## Data

- **NASA DONKI** (Space Weather Database Of Notifications, Knowledge, Information), run by the Community Coordinated
  Modeling Center (CCMC) at NASA Goddard Space Flight Center: https://kauai.ccmc.gsfc.nasa.gov/DONKI/
  Data fetched from the DONKI web service (https://ccmc.gsfc.nasa.gov/DONKI/WS/get/) on 2026-11-15: all 2024 GST
  (geomagnetic storm) and SEP (solar energetic particle) records, stored in `Assets/_Data/NASA/DONKI/donki_2024_events.json`.
  The 30-day mission is 30 consecutive real days, 2024-08-01 to 2024-08-30 (Day 1 = Aug 1), baked into
  `Assets/_Config/Simulation/SpaceWeatherConfig.asset`; no live API calls at runtime.

| Game day | In-game type | DONKI date | DONKI type | Event | DONKI record |
|---|---|---|---|---|---|
| 4 | Solar Storm | 2024-08-04 | GST | Kp 6.67 (G2) | https://ccmc.gsfc.nasa.gov/DONKI/view/GST/32519/-1 |
| 5 | Solar Radiation | 2024-08-05 | SEP | STEREO A, after an X1.7 flare | https://ccmc.gsfc.nasa.gov/DONKI/view/SEP/32560/-1 |
| 12 | Solar Storm | 2024-08-12 | GST | Kp 8 (G4) | https://ccmc.gsfc.nasa.gov/DONKI/view/GST/32722/-1 |
| 17 | Solar Storm | 2024-08-17 | GST | Kp 6.67 (G2) | https://ccmc.gsfc.nasa.gov/DONKI/view/GST/32864/-1 |

- NASA Artemis program (mission context and inspiration): https://www.nasa.gov/humans-in-space/artemis/

## Online services

- Supabase (Postgres REST API) for the anonymous run histogram, via the publishable key only: https://supabase.com/

CC0 licence text: http://creativecommons.org/publicdomain/zero/1.0/

- `Assets/_Audio/Music/MainTheme.mp3`: original background track by Lucas (made in FL Studio, 126 BPM)
