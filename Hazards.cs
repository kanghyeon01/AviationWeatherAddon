using System;
using System.Collections.Generic;
using Game.Common;
using WeatherPhenomenon = Game.Events.WeatherPhenomenon;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace RealisticAviationATIS
{
    /// <summary>A weather disaster: a thunderstorm, a hail storm or a tornado, as the game's weather event places it.</summary>
    internal struct Hazard
    {
        public float2 m_Position;
        public float m_Radius;
        public HazardKind m_Kind;
    }

    /// <summary>Most severe last, so that the highest value wins.</summary>
    internal enum HazardKind : byte
    {
        Thunderstorm,
        Hail,
        Tornado,
    }

    /// <summary>
    /// Weather disasters in the METAR, as an automated station with a lightning sensor reports them:
    /// <list type="bullet">
    /// <item>over the airport (within the event's radius): <c>+TSRA</c> (or <c>+TSSN</c>) for a thunderstorm, <c>TSGR</c> for hail,
    /// <c>+FC +TSRA</c> for a tornado, with strong gusts, low visibility and cumulonimbus;</item>
    /// <item>in the vicinity (within <see cref="kVicinity"/> beyond the radius): <c>VCTS</c>, or <c>VCFC</c> for a tornado,
    /// with cumulonimbus;</item>
    /// <item>in the distance (within <see cref="kDistant"/> beyond the radius): the remark <c>CB DSNT NE</c>.</item>
    /// </list>
    /// The weather group changes when a disaster arrives or leaves, so a SPECI goes out at both. Kinds follow the event
    /// prefab's name, as Realistic Aviation's tower tells them apart.
    /// </summary>
    internal static class Hazards
    {
        private const float kVicinity = 8000f;
        private const float kDistant = 30000f;

        private static readonly string[] kDirections = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

        public static EntityQuery CreateQuery(EntityManager entityManager)
        {
            return entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<WeatherPhenomenon>(), ComponentType.ReadOnly<PrefabRef>() },
                None = new[] { ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Temp>() },
            });
        }

        /// <summary>The disasters on the map now.</summary>
        public static void Collect(EntityManager entityManager, PrefabSystem prefabSystem, EntityQuery query, List<Hazard> hazards)
        {
            hazards.Clear();
            if (query.IsEmptyIgnoreFilter)
            {
                return;
            }
            NativeArray<Entity> events = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < events.Length; i++)
            {
                WeatherPhenomenon phenomenon = entityManager.GetComponentData<WeatherPhenomenon>(events[i]);
                string name = prefabSystem.GetPrefabName(entityManager.GetComponentData<PrefabRef>(events[i]).m_Prefab);
                hazards.Add(new Hazard
                {
                    m_Position = phenomenon.m_PhenomenonPosition.xz,
                    m_Radius = phenomenon.m_PhenomenonRadius,
                    m_Kind = name.IndexOf("tornado", StringComparison.OrdinalIgnoreCase) >= 0 ? HazardKind.Tornado
                        : name.IndexOf("hail", StringComparison.OrdinalIgnoreCase) >= 0 ? HazardKind.Hail
                        : HazardKind.Thunderstorm,
                });
            }
            events.Dispose();
        }

        /// <summary>Adds what the airport's station sees of the disasters to its observation.</summary>
        public static void Apply(Observation observation, float2 airport, List<Hazard> hazards, bool snowing)
        {
            int over = -1, near = -1;
            float distantRange = float.MaxValue;
            float2 distantAt = default;
            for (int i = 0; i < hazards.Count; i++)
            {
                Hazard hazard = hazards[i];
                float beyond = math.distance(airport, hazard.m_Position) - hazard.m_Radius;
                if (beyond <= 0f)
                {
                    over = math.max(over, (int)hazard.m_Kind);
                }
                else if (beyond <= kVicinity)
                {
                    near = math.max(near, (int)hazard.m_Kind);
                }
                else if (beyond <= kDistant && beyond < distantRange)
                {
                    distantRange = beyond;
                    distantAt = hazard.m_Position;
                }
            }
            if (over >= 0)
            {
                ApplyOver(observation, (HazardKind)over, snowing);
            }
            else if (near >= 0)
            {
                observation.Weather.Add((HazardKind)near == HazardKind.Tornado ? "VCFC" : "VCTS");
                MarkCumulonimbus(observation);
            }
            if (over < 0 && near < 0 && distantRange < float.MaxValue)
            {
                observation.DistantCumulonimbus = Direction(distantAt - airport);
            }
        }

        private static void ApplyOver(Observation observation, HazardKind kind, bool snowing)
        {
            // The storm's own weather replaces the city's precipitation and thunder.
            observation.Weather.RemoveAll(code => code.Contains("RA") || code.Contains("SN") || code.Contains("TS"));
            int wind, gust, visibility;
            switch (kind)
            {
                case HazardKind.Tornado:
                    observation.Weather.Insert(0, "+FC");
                    observation.Weather.Insert(1, "+TSRA");
                    wind = 35;
                    gust = 60;
                    visibility = 1000;
                    break;
                case HazardKind.Hail:
                    observation.Weather.Insert(0, "TSGR");
                    wind = 25;
                    gust = 45;
                    visibility = 2000;
                    break;
                default:
                    observation.Weather.Insert(0, snowing ? "+TSSN" : "+TSRA");
                    wind = 20;
                    gust = 40;
                    visibility = 3000;
                    break;
            }
            observation.WindKnots = math.max(observation.WindKnots, wind);
            observation.GustKnots = math.max(observation.GustKnots, gust);
            observation.VisibilityMetres = math.min(observation.VisibilityMetres, visibility);
            MarkCumulonimbus(observation);
        }

        /// <summary>A thunderstorm brings cumulonimbus: the lowest layer becomes one (at least broken), or one is added at 3,000 ft.</summary>
        private static void MarkCumulonimbus(Observation observation)
        {
            for (int i = 0; i < observation.Clouds.Count; i++)
            {
                CloudLayer layer = observation.Clouds[i];
                if (layer.Cover == "VV")
                {
                    return;
                }
                if (i == 0)
                {
                    layer.Cumulonimbus = true;
                    if (layer.Cover == "FEW" || layer.Cover == "SCT")
                    {
                        layer.Cover = "BKN";
                    }
                    observation.Clouds[i] = layer;
                    return;
                }
            }
            observation.Clouds.Add(new CloudLayer { Cover = "BKN", BaseHundreds = 30, Cumulonimbus = true });
        }

        /// <summary>The compass point of a direction on the map (north +z, east +x).</summary>
        private static string Direction(float2 offset)
        {
            float bearing = math.degrees(math.atan2(offset.x, offset.y));
            int index = (int)math.round(((bearing + 360f) % 360f) / 45f) % 8;
            return kDirections[index];
        }
    }
}
