# Aviation Weather Add-on

An expansion for [Realistic Aviation](https://github.com/kanghyeon01/RealisticAviation) (Cities: Skylines II): weather
reports and ATIS at every airport.

## Phase 1: automated weather station

- Every airport observes the weather each game minute. Wind is local, read from the game's wind map at the airport.
  Temperature, precipitation, fog and clouds are the city's.
- A routine report (METAR) goes out at :00 and :30. A special report (SPECI) goes out in between when the weather
  changes significantly: the visibility band (800 / 1,500 / 3,000 / 5,000 m / 10 km), the present weather, the ceiling
  band, cumulonimbus, or the wind by 10 kt.
- The airport's info panel shows the latest report. Hover over it to read it decoded, in English or Korean.

Example: `METAR RIVE 191230Z AUTO 27008KT 5000 -SN BKN040 M02/M08 Q1012 RMK AO2`

The station code is the airport's ICAO code, entered in its info panel (Realistic Aviation), or else an automatic one
from its name.

The game's wind has no unit, so it is scaled so that an average spot of the map has 8 kt. Each report is written to
`Logs/RealisticAviationATIS.log` together with the raw climate values, for calibration.

## Phase 3: ATIS broadcast

- Each airport has an ATIS recording with an information letter (Alpha to Zulu, then Alpha again) on its own
  frequency (128.000–135.975 MHz, made up from the airport).
- A new letter is recorded:
  - at the first report;
  - at every hourly METAR and every SPECI;
  - at a half-hourly METAR that differs significantly from the recording;
  - when the runways in use change;
  - when the airport closes or reopens.

  Otherwise the recording stays as it is.
- The text follows a recorded ATIS:
  - `PortLand Airport information Bravo, 1230 Zulu. Wind 270 at 8, visibility 5 kilometres, light snow, broken 4,000,`
    `temperature minus 2, dew point minus 8, QNH 1012. ILS approach runway 27 in use. Advise on initial contact you`
    `have information Bravo.`
  - Class B and C airports read "ILS approach", class D "visual approach".
  - A closed airport reads "Airport closed, tornado." (or hail storm, thunderstorm, emergency) instead of the runways.
  - The runways in use and the closure come from Realistic Aviation (`AtcSystem.GetLandingRunways`, `IsClosed`).
- The airport panel shows "ATIS Bravo 128.475" with the recording below it. Hover over it to read it in plain words,
  in English or Korean.
- Each new recording is logged with its reason.

## Phase 4: "with information X"

This mod tells Realistic Aviation each airport's current ATIS (`AtisLink.Current`). Pilots then name the letter on
their first call:

- the pushback request: "PortLand Ground, X, ready for pushback, with information Bravo.";
- a departure's first taxi request without pushback;
- an arrival's first call to approach, or to the tower at class D.

Traffic only passing through does not. A pilot who listened before the latest change names the old letter, and the
controller answers "information Charlie now current, QNH 1009".

## Phase 5: weather disasters

Thunderstorms, hail storms and tornadoes (the game's weather events, told apart by their prefab names as Realistic
Aviation's tower does) show in each airport's METAR by their distance:

- Over the airport (inside the event's radius):
  - thunderstorm: `+TSRA` (`+TSSN` in snow), at least 20 kt gusting 40, visibility at most 3,000 m;
  - hail: `TSGR`, 25 kt gusting 45, visibility 2,000 m;
  - tornado: `+FC +TSRA`, 35 kt gusting 60, visibility 1,000 m.

  Each also brings a cumulonimbus layer (the lowest layer, at least broken, or `BKN030CB`).
- Within 8 km beyond the radius: `VCTS`, or `VCFC` for a tornado, with cumulonimbus.
- Within 30 km: the remark `CB DSNT NE` (direction from the airport).

A disaster arriving or leaving changes the weather group, so a SPECI goes out, and with it a new ATIS letter. The ATIS
reads the same, for example "tornado" or "thunderstorm in the vicinity", and adds "cumulonimbus distant northeast". A
closure is announced in the ATIS, not in the METAR. The decoded tooltip covers the new codes in English and Korean.

## Building

Set `CS2ManagedPath` in `RealisticAviationATIS.csproj.user` to the game's `Cities2_Data\Managed` folder, then build it:

```
dotnet build -c Release
```

The DLL and the UI module are copied to the local `Mods\RealisticAviationATIS` folder.

## License

MIT
