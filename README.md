# Artemis X

> *"Every watt, drop of water, and solar flare counts when you’re 384,400 km from Earth."*

A day-by-day lunar base strategy game built for the **NASA Space Apps Challenge 2026** — **Build a Junior Astronaut Mission Trainer** challenge by team **[Hidden Saraquaty](https://www.spaceappschallenge.org/2026/find-a-team/hidden-saraquaty/?tab=project)** (Campinas, Brazil).

---

## Overview

Most space games treat living on the Moon like effortless magic, hiding the critical engineering trade-offs that real astronauts face every single day. **Artemis X** puts young learners in command of a lunar outpost, challenging them to transform a base dependent on Earth supply deliveries into a self-sufficient habitat within a 30-day mission cap.

Guided by **Bauzer** (the base computer, named in honour of Dr. Claudia Maria Bauzer Medeiros, Professor Emerita at the Institute of Computing, Unicamp), players balance scarce resources, dispatch rover expeditions, construct critical life-support modules, and respond to real space-weather events from **NASA’s DONKI** database.

---

## How It Works

### Core Gameplay Loop
1. **Scout & Harvest (Lunar Rover):** Open the 3D top-down exploration map to dispatch the rover across 5 lunar sites (Water/Ice, Mineral/Parts, or Balanced). Longer expeditions yield larger hauls at higher Power cost and mechanical breakdown risk.
2. **Build & Process (3-Star Autonomy):** Progress through three linear self-sufficiency buildings using Spare Parts and Power:
   - **Water & O2 Plant** *(Star 1)* — Processes extracted water ice into daily Water and Oxygen.
   - **Greenhouse** *(Star 2)* — Produces sustainable Food to replace Earth supply drops.
   - **Fuel Reserve** *(Star 3)* — Generates return-rocket propellant and completes base self-sufficiency (`EarlyWin`).
3. **Survive Real NASA Space Weather:** Powered by historical 2024 **NASA DONKI** solar storm (GST) and solar radiation (SEP) records, space-weather alerts force immediate engineering trade-offs:
   - **Solar Storm:** Retract the exposed solar field (sacrificing daily Power to protect panels) or keep full Power and risk rock/debris damage requiring Spare Parts to repair.
   - **Solar Radiation:** Shelter the crew and ground the rover, or overclock shields at a high Power cost.
4. **Unexpected Demands & Earth Supplies:** Handle daily anomalies (leaks, clogged filters, Earth science requests) while scheduled Earth supply ships deliver emergency Food and Parts every 5 days.

---

## Design Philosophy & Educational Impact

- **Authentic Trade-Offs Over "Game Over" Screens:** Hitting zero Oxygen or Food never ends the run abruptly—instead, crew operations pause for a turn until life support stabilizes, and unprotected solar panels suffer cascading repair costs in Spare Parts.
- **Cognitive Load & Accessibility for Kids:** Complex lunar ECLSS and ISRU concepts are distilled into clear 3D diorama cues, icon-driven resource gauges (`Power`, `Water`, `Oxygen`, `Food`, `Parts`), and concise guidance from Bauzer.
- **Space-Weather Literacy:** Connecting in-game crises to real NASA DONKI events helps players understand how solar flares and geomagnetic storms affect human operations on the Moon.
- **Classroom & Museum Ready:** Designed as an accessible 10–15 minute WebGL experience with no account or installation required.

---

## Tech Stack & Architecture

- **Engine & Language:** Unity `6000.3.6f1` (C#) using URP, Input System, Cinemachine, and DOTween
- **Architecture:** Modular, event-driven simulation (`Assets/_Scripts/Simulation/`) decoupled from visuals and UI (`Assets/_Scripts/UI/`, `Assets/_Scripts/Visuals/`, `Assets/_Scripts/Audio/`), with tunable `ScriptableObject` configs in `Assets/_Config/`
- **Space Weather Data:** Deterministic 30-day schedule baked from NASA CCMC DONKI 2024 `GST` and `SEP` records (`Assets/_Data/NASA/DONKI/donki_2024_events.json`), avoiding runtime API failures in WebGL
- **Target Platforms:** WebGL (primary browser build) with planned iOS Mobile support
- **Leaderboard:** Optional anonymous run histogram via Supabase REST API with offline fallback

---

## Running in Unity

1. Open the project in **Unity 6000.3.6f1**.
2. Open the entry scene [`Assets/Scenes/CharacterSelect.unity`](Assets/Scenes/CharacterSelect.unity) (followed by [`Assets/Scenes/HandMadeBase.unity`](Assets/Scenes/HandMadeBase.unity) in Build Settings).
3. Press **Play** in the Unity Editor, or build for **WebGL** via **File → Build Profiles**.

---

## NASA Data & References

### NASA Resources
- [NASA CCMC DONKI (Space Weather Database Of Notifications, Knowledge, Information)](https://ccmc.gsfc.nasa.gov/tools/DONKI/)
- [NASA Open APIs — DONKI Space Weather API](https://api.nasa.gov/)
- [NASA Overview: In-Situ Resource Utilization (ISRU)](https://www.nasa.gov/overview-in-situ-resource-utilization/)
- [NASA Artemis Program & Lunar Surface Science Resources](https://science.nasa.gov/lunar-science/resources/)

### Tools, Assets & Credits
- [Unity Game Engine](https://unity.com/) & [DOTween](http://dotween.demigiant.com/license.php)
- [KayKit: Space Base Bits](https://kaylousberg.itch.io/space-base-bits), Resource Bits & Character Animations by Kay Lousberg (CC0 1.0)
- [Kenney Game Assets](https://www.kenney.nl/) — Space Kit, UI Pack 2.0, UI Pack: Sci-fi 2.0, Emotes, Interface Sounds (CC0 1.0)
- [Ultimate Space Kit](https://quaternius.com/packs/ultimatespacekit.html) by Quaternius (CC0 1.0)
- [Free Icon Pack](https://gvesster.itch.io/free-icon-pack) by @gvesster
- Custom resource icons designed in [Figma](https://www.figma.com/); typography via [Google Fonts](https://fonts.google.com/) / Kenney Future
- Full third-party asset inventory, licences, and AI-generated art disclosures are documented in [`Assets/Docs/references.md`](Assets/Docs/references.md) and [`Assets/Docs/ai-assets-log.md`](Assets/Docs/ai-assets-log.md).

---

## Team — Hidden Saraquaty

**Local Event:** Campinas, Brazil — NASA Space Apps Challenge 2026

- **Lucas Francisco Salman de Oliveira** — Team Owner, Game Design & Development
- **Beatriz Montali Pucharelli** — Team Member, Design & Ideas
