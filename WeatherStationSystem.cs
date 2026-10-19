using System;
using System.Collections.Generic;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game;
using Game.Simulation;
using Game.UI;
using RealisticAviation;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace RealisticAviationATIS
{
    /// <summary>The latest report of one airport's station.</summary>
    public sealed class StationReport
    {
        public string Station;
        public string Text;
        public bool Special;
        public Observation Observation;

        /// <summary>The airport's ATIS broadcast, made from this station's reports.</summary>
        public readonly AtisBroadcast Atis = new AtisBroadcast();
    }

    /// <summary>
    /// An automated weather station at every airport. Each game minute it observes the weather at the airport; it sends a
    /// routine report (METAR) at :00 and :30 and a special report (SPECI) in between when the weather changes
    /// significantly (<see cref="Observation.DiffersSignificantlyFrom"/>). Wind is local (the game's wind map at the
    /// airport); temperature, precipitation, fog and clouds are the city's.
    /// </summary>
    public partial class WeatherStationSystem : GameSystemBase
    {
        /// <summary>Wind speed of an average spot of the map, in knots: the game's wind has no unit, so it is scaled to this.</summary>
        private const float kAverageWindKnots = 8f;

        private const float kMaxWindKnots = 60f;

        /// <summary>Above this share of the average the wind is gusty, by half the excess (at least 10 kt, as in METAR).</summary>
        private const float kGustyShare = 1.5f;

        /// <summary>Precipitation (0–1) at and above which it is reported; light below 0.3, heavy from 0.7.</summary>
        private const float kPrecipitationThreshold = 0.02f;
        private const float kLightPrecipitation = 0.3f;
        private const float kHeavyPrecipitation = 0.7f;

        /// <summary>Thunder (0–1) at and above which there is a thunderstorm with cumulonimbus.</summary>
        private const float kThunderThreshold = 0.3f;

        private const float kClearVisibility = 12000f;

        private ClimateSystem m_ClimateSystem;
        private WindSystem m_WindSystem;
        private TimeSystem m_TimeSystem;
        private NameSystem m_NameSystem;
        private AtcSystem m_AtcSystem;
        private Game.Prefabs.PrefabSystem m_PrefabSystem;
        private readonly List<string> m_Runways = new List<string>();
        private EntityQuery m_AirplaneStopQuery;
        private NativeList<Entity> m_Airports;
        private readonly Dictionary<Entity, StationReport> m_Reports = new Dictionary<Entity, StationReport>();
        private long m_LastMinute = long.MinValue;
        private float m_AverageWind;

        /// <summary>Bumped on every new report, so that an open info panel knows to refresh.</summary>
        public uint Version { get; private set; }

        public bool TryGetReport(Entity airport, out StationReport report) => m_Reports.TryGetValue(airport, out report);

        // A game minute is about 182 frames: checking every 64 frames reports each minute's weather in time.
        public override int GetUpdateInterval(SystemUpdatePhase phase) => 64;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_ClimateSystem = World.GetOrCreateSystemManaged<ClimateSystem>();
            m_WindSystem = World.GetOrCreateSystemManaged<WindSystem>();
            m_TimeSystem = World.GetOrCreateSystemManaged<TimeSystem>();
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            m_AtcSystem = World.GetOrCreateSystemManaged<AtcSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<Game.Prefabs.PrefabSystem>();
            m_AirplaneStopQuery = Airports.CreateAirplaneStopQuery(EntityManager);
            m_Airports = new NativeList<Entity>(4, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            m_Airports.Dispose();
            base.OnDestroy();
        }

        protected override void OnGameLoaded(Context serializationContext)
        {
            base.OnGameLoaded(serializationContext);
            m_Reports.Clear();
            m_LastMinute = long.MinValue;
        }

        protected override void OnUpdate()
        {
            DateTime now = m_TimeSystem.GetCurrentDateTime();
            long minute = now.Ticks / TimeSpan.TicksPerMinute;
            if (minute == m_LastMinute)
            {
                return;
            }
            m_LastMinute = minute;
            Airports.Collect(EntityManager, m_AirplaneStopQuery, m_Airports);
            if (m_Airports.Length == 0)
            {
                m_Reports.Clear();
                return;
            }
            DateTime time = new DateTime(minute * TimeSpan.TicksPerMinute);
            bool routine = time.Minute % 30 == 0;

            NativeArray<Wind> windMap = m_WindSystem.GetMap(true, out JobHandle dependencies);
            dependencies.Complete();
            m_AverageWind = AverageWind(windMap);

            RemoveGoneAirports();
            for (int i = 0; i < m_Airports.Length; i++)
            {
                Entity airport = m_Airports[i];
                if (!EntityManager.HasComponent<Game.Objects.Transform>(airport))
                {
                    continue;
                }
                float3 position = EntityManager.GetComponentData<Game.Objects.Transform>(airport).m_Position;
                float2 wind = WindSystem.GetWind(position, windMap).m_Wind;
                Observation observation = Observe(time, wind);
                bool known = m_Reports.TryGetValue(airport, out StationReport report);
                bool issue = !known || routine || observation.DiffersSignificantlyFrom(report.Observation);
                if (!known)
                {
                    report = new StationReport();
                    m_Reports.Add(airport, report);
                }
                if (issue)
                {
                    report.Station = AirportCodes.Get(EntityManager, m_NameSystem, airport);
                    // A station's first report is a routine one, whatever the minute.
                    report.Special = known && !routine;
                    report.Observation = observation;
                    report.Text = observation.ToMetar(report.Station, report.Special);
                    Version++;
                    LogReport(report, wind);
                }
                UpdateAtis(airport, report, issue, time);
            }
        }

        /// <summary>
        /// A new ATIS (the next letter) when the first report came, at each hourly METAR, at a SPECI, at a half-hourly METAR
        /// that differs significantly from the broadcast's weather, when the runways in use change, or when the airport closes
        /// or opens (or the reason changes). Otherwise the broadcast stays as it is, as a recording does.
        /// </summary>
        private void UpdateAtis(Entity airport, StationReport report, bool issued, DateTime time)
        {
            AtisBroadcast atis = report.Atis;
            m_Runways.Clear();
            m_AtcSystem.GetLandingRunways(airport, m_Runways);
            string runways = string.Join(",", m_Runways);
            string closure = null;
            if (m_AtcSystem.IsClosed(airport, out Entity closedBy))
            {
                string prefabName = closedBy != airport && EntityManager.TryGetComponent(closedBy, out Game.Prefabs.PrefabRef prefab)
                    ? m_PrefabSystem.GetPrefabName(prefab.m_Prefab)
                    : null;
                closure = Atis.ClosureReason(prefabName);
            }
            string reason = atis.Letter < 0 ? "first report"
                : issued && report.Special ? "special report"
                : issued && time.Minute == 0 ? "hourly report"
                : issued && report.Observation.DiffersSignificantlyFrom(atis.Observation) ? "weather change"
                : runways != atis.RunwayKey ? "runway change"
                : closure != atis.Closure ? (closure != null ? "airport closed" : "airport reopened")
                : null;
            if (reason == null)
            {
                return;
            }
            atis.Letter = (atis.Letter + 1) % Atis.kPhonetic.Length;
            atis.Time = time;
            atis.Observation = report.Observation;
            atis.AirportName = m_NameSystem.GetRenderedLabelName(airport);
            atis.AirspaceClass = m_AtcSystem.TryGetAirspaceClass(airport, out AirspaceClass airspaceClass) ? airspaceClass : AirspaceClass.D;
            atis.Runways.Clear();
            atis.Runways.AddRange(m_Runways);
            atis.Closure = closure;
            atis.Frequency = Atis.Frequency(airport);
            atis.Text = Atis.Compose(atis);
            Version++;
            Mod.Log.Info($"ATIS {report.Station} {Atis.kPhonetic[atis.Letter]} ({reason}): {atis.Text}");
        }

        private void LogReport(StationReport report, float2 wind)
        {
            Mod.Log.Info($"{report.Text} (wind raw {math.length(wind):0.###}, map average {m_AverageWind:0.###}; precipitation {(float)m_ClimateSystem.precipitation:0.##}, cloudiness {(float)m_ClimateSystem.cloudiness:0.##}, fog {(float)m_ClimateSystem.fog:0.##}, thunder {(float)m_ClimateSystem.thunder:0.##})");
        }

        private void RemoveGoneAirports()
        {
            List<Entity> gone = null;
            foreach (Entity airport in m_Reports.Keys)
            {
                if (!m_Airports.Contains(airport))
                {
                    (gone ??= new List<Entity>()).Add(airport);
                }
            }
            if (gone != null)
            {
                for (int i = 0; i < gone.Count; i++)
                {
                    m_Reports.Remove(gone[i]);
                }
            }
        }

        private static float AverageWind(NativeArray<Wind> windMap)
        {
            float sum = 0f;
            for (int i = 0; i < windMap.Length; i++)
            {
                sum += math.length(windMap[i].m_Wind);
            }
            return windMap.Length != 0 ? sum / windMap.Length : 0f;
        }

        private Observation Observe(DateTime time, float2 wind)
        {
            float temperature = m_ClimateSystem.temperature;
            float precipitation = math.saturate(m_ClimateSystem.precipitation);
            float cloudiness = math.saturate(m_ClimateSystem.cloudiness);
            float fog = math.saturate(m_ClimateSystem.fog);
            float thunder = math.saturate(m_ClimateSystem.thunder);
            bool snow = m_ClimateSystem.isSnowing;
            bool precipitating = precipitation >= kPrecipitationThreshold;
            bool thunderstorm = thunder >= kThunderThreshold;

            Observation observation = new Observation { Time = time };

            // Wind: the map's vector is where the air goes, METAR gives where it comes from (degrees, 360 for north).
            float speed = m_AverageWind > 0f ? math.length(wind) / m_AverageWind : 0f;
            observation.WindKnots = (int)math.round(math.min(speed * kAverageWindKnots, kMaxWindKnots));
            float from = math.degrees(math.atan2(-wind.x, -wind.y));
            int direction = (int)math.round((from + 360f) % 360f / 10f) * 10;
            observation.WindDirection = direction == 0 ? 360 : direction;
            if (speed > kGustyShare || thunderstorm)
            {
                int gust = observation.WindKnots + math.max(10, (int)math.round((speed - 1f) * kAverageWindKnots * 0.5f));
                observation.GustKnots = math.min(gust, (int)kMaxWindKnots);
            }

            // Visibility: the lower of what the precipitation (snow hides more than rain) and the fog leave.
            float precipitationVisibility = precipitating ? kClearVisibility * (1f - (snow ? 0.94f : 0.85f) * precipitation) : float.MaxValue;
            float fogVisibility = fog > 0.05f ? math.max(100f, kClearVisibility * (1f - fog) * (1f - fog)) : float.MaxValue;
            observation.VisibilityMetres = RoundVisibility(math.min(math.min(precipitationVisibility, fogVisibility), kClearVisibility));

            // Present weather: intensity and precipitation (with TS in a thunderstorm), then fog or mist.
            if (precipitating || thunderstorm)
            {
                string intensity = !precipitating ? string.Empty : precipitation < kLightPrecipitation ? "-" : precipitation >= kHeavyPrecipitation ? "+" : string.Empty;
                string kind = !precipitating ? string.Empty : snow ? "SN" : "RA";
                observation.Weather.Add(intensity + (thunderstorm ? "TS" : string.Empty) + kind);
            }
            bool fogLimits = fogVisibility <= precipitationVisibility && observation.VisibilityMetres < 5000;
            if (fogLimits)
            {
                observation.Weather.Add(observation.VisibilityMetres < 1000 ? (temperature < 0f ? "FZFG" : "FG") : "BR");
            }

            // Clouds: one layer from the cloudiness, lower the worse the weather; a thunderstorm makes it cumulonimbus.
            // Thick fog hides the sky: vertical visibility instead.
            if (fogLimits && observation.VisibilityMetres < 500)
            {
                observation.Clouds.Add(new CloudLayer { Cover = "VV", BaseHundreds = math.max(1, (int)math.round(observation.VisibilityMetres * 3.28084f / 100f)) });
            }
            else if (cloudiness >= 0.1f || thunderstorm)
            {
                float severity = math.max(math.max(precipitation, fog * 0.8f), math.saturate((cloudiness - 0.5f) * 2f) * 0.5f);
                int baseHundreds = (int)math.round(math.lerp(50f, 8f, severity));
                string cover = cloudiness < 0.3f ? "FEW" : cloudiness < 0.55f ? "SCT" : cloudiness < 0.85f ? "BKN" : "OVC";
                observation.Clouds.Add(new CloudLayer { Cover = cover, BaseHundreds = baseHundreds, Cumulonimbus = thunderstorm });
            }

            // Dew point: close to the temperature in precipitation and fog, further apart in dry weather.
            observation.Temperature = (int)math.round(temperature);
            float spread = math.lerp(8f, 0.5f, math.max(math.max(precipitation, fog), cloudiness * 0.4f));
            observation.DewPoint = (int)math.round(temperature - spread);

            // Pressure: lower under cloud, precipitation and thunderstorms.
            float qnh = 1021f - 8f * cloudiness - 12f * precipitation - 8f * thunder;
            observation.Qnh = (int)math.clamp(math.round(qnh), 960f, 1040f);
            return observation;
        }

        /// <summary>METAR steps: 50 m below 800 m, 100 m below 5 km, 1 km below 10 km, 9999 from 10 km.</summary>
        private static int RoundVisibility(float metres)
        {
            if (metres >= 10000f)
            {
                return 10000;
            }
            int step = metres < 800f ? 50 : metres < 5000f ? 100 : 1000;
            return math.max(step, (int)(metres / step) * step);
        }

        /// <summary>
        /// Puts the airport's current code into its latest report when the player changed it (also while paused, when no
        /// new report goes out): the same observation, sent again under the new code.
        /// </summary>
        public void RefreshStation(Entity airport)
        {
            if (!m_Reports.TryGetValue(airport, out StationReport report))
            {
                return;
            }
            string station = AirportCodes.Get(EntityManager, m_NameSystem, airport);
            if (station != report.Station)
            {
                report.Station = station;
                report.Text = report.Observation.ToMetar(station, report.Special);
                Version++;
            }
        }
    }
}
