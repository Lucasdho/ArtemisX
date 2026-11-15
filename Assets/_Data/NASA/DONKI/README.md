# NASA DONKI data

`donki_2024_events.json` holds every 2024 record from two NASA DONKI feeds, fetched from the CCMC web service
(https://ccmc.gsfc.nasa.gov/DONKI/WS/get/) on 2026-11-15:

- `storms`: GST (geomagnetic storms) with the highest Kp observed and the DONKI link. 20 records.
- `particles`: SEP (solar energetic particle events) with the instrument that saw them, the linked flare class (from FLR) and the DONKI link. 119 records.

How the game uses it: `SpaceWeatherConfig.missionStartDate` (2024-08-01) becomes mission Day 1, and each mission day is the next real day.
On each real day, a GST with max Kp >= `stormMinKp` (6, NOAA G2) becomes a Solar Storm; otherwise an observed SEP event
(not a `MODEL:` forecast) becomes Solar Radiation; everything else is Calm. Run `Artemis X > Data > Bake DONKI Schedule`
after changing the dataset or those settings. The test `DonkiScheduleBakerTests` fails if the schedule and the data drift apart.

Current window (2024-08-01 to 2024-08-30): Day 4 storm (Aug 4, Kp 6.67), Day 5 radiation (Aug 5, after an X1.7 flare),
Day 12 storm (Aug 12, Kp 8), Day 17 storm (Aug 17, Kp 6.67).
