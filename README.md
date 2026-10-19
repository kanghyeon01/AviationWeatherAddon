# Realistic Aviation ATIS

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

## Building

Set `CS2ManagedPath` in `RealisticAviationATIS.csproj.user` to the game's `Cities2_Data\Managed` folder, then build it:

```
dotnet build -c Release
```

The DLL and the UI module are copied to the local `Mods\RealisticAviationATIS` folder.

## License

MIT
