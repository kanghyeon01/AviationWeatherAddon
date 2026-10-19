using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using RealisticAviation;
using Unity.Entities;
using Unity.Mathematics;

namespace RealisticAviationATIS
{
    /// <summary>
    /// An airport's ATIS broadcast: its information letter, the observation and airport state it was made from, and its text
    /// as the recording reads it ("PortLand Airport information Bravo, 1230 Zulu. Wind 270 at 8, ...").
    /// </summary>
    public sealed class AtisBroadcast
    {
        /// <summary>0 = Alpha ... 25 = Zulu; -1 before the first broadcast.</summary>
        public int Letter = -1;

        public DateTime Time;
        public Observation Observation;
        public string AirportName;
        public AirspaceClass AirspaceClass;
        public readonly List<string> Runways = new List<string>();

        /// <summary>Why the airport is closed (the event's kind, in English), or null while it is open.</summary>
        public string Closure;

        public string Frequency;
        public string Text;

        /// <summary>The runways as one string, to notice a change.</summary>
        public string RunwayKey => string.Join(",", Runways);
    }

    /// <summary>Builds the ATIS text and its frequency.</summary>
    internal static class Atis
    {
        public static readonly string[] kPhonetic =
        {
            "Alpha", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot", "Golf", "Hotel", "India", "Juliett", "Kilo", "Lima", "Mike",
            "November", "Oscar", "Papa", "Quebec", "Romeo", "Sierra", "Tango", "Uniform", "Victor", "Whiskey", "X-ray", "Yankee", "Zulu",
        };

        /// <summary>
        /// The ATIS frequency, made up from the airport like the tower's (Realistic Aviation) but in 128.000–135.975 MHz, a
        /// range none of the airport's other frequencies use (25 kHz steps).
        /// </summary>
        public static string Frequency(Entity airport)
        {
            uint hash = math.hash(new int3(airport.Index, airport.Version, 0x41544953));
            return (128f + hash % 320u * 0.025f).ToString("F3", CultureInfo.InvariantCulture);
        }

        /// <summary>The broadcast text, in the order and words of a recorded ATIS.</summary>
        public static string Compose(AtisBroadcast atis)
        {
            CultureInfo invariant = CultureInfo.InvariantCulture;
            Observation o = atis.Observation;
            string letter = kPhonetic[atis.Letter];
            StringBuilder text = new StringBuilder();
            text.Append(atis.AirportName).Append(" information ").Append(letter).Append(", ")
                .Append(atis.Time.ToString("HHmm", invariant)).Append(" Zulu. ");
            if (atis.Closure != null)
            {
                text.Append("Airport closed, ").Append(atis.Closure).Append(". ");
            }
            if (o.WindKnots < 3)
            {
                text.Append("Wind calm");
            }
            else
            {
                text.Append("Wind ").Append(o.WindDirection.ToString("000", invariant)).Append(" at ").Append(o.WindKnots.ToString(invariant));
                if (o.GustKnots > o.WindKnots)
                {
                    text.Append(" gusts ").Append(o.GustKnots.ToString(invariant));
                }
            }
            if (o.IsCavok)
            {
                text.Append(", CAVOK");
            }
            else
            {
                text.Append(", visibility ").Append(Visibility(o.VisibilityMetres));
                for (int i = 0; i < o.Weather.Count; i++)
                {
                    text.Append(", ").Append(Weather(o.Weather[i]));
                }
                if (o.Clouds.Count == 0)
                {
                    text.Append(", no cloud detected");
                }
                for (int i = 0; i < o.Clouds.Count; i++)
                {
                    text.Append(", ").Append(Cloud(o.Clouds[i]));
                }
            }
            text.Append(", temperature ").Append(Signed(o.Temperature)).Append(", dew point ").Append(Signed(o.DewPoint))
                .Append(", QNH ").Append(o.Qnh.ToString(invariant)).Append(". ");
            if (atis.Closure == null && atis.Runways.Count != 0)
            {
                bool instrument = atis.AirspaceClass != AirspaceClass.D;
                bool several = atis.Runways.Count > 1;
                text.Append(instrument ? "ILS approach" : "Visual approach").Append(several ? "es runways " : " runway ")
                    .Append(JoinRunways(atis.Runways)).Append(" in use. ");
            }
            text.Append("Advise on initial contact you have information ").Append(letter).Append('.');
            return text.ToString();
        }

        private static string JoinRunways(List<string> runways)
        {
            if (runways.Count == 1)
            {
                return runways[0];
            }
            return string.Join(", ", runways.GetRange(0, runways.Count - 1)) + " and " + runways[runways.Count - 1];
        }

        private static string Signed(int value) => value < 0 ? "minus " + (-value).ToString(CultureInfo.InvariantCulture) : value.ToString(CultureInfo.InvariantCulture);

        private static string Visibility(int metres)
        {
            if (metres >= 10000)
            {
                return "10 kilometres or more";
            }
            return metres >= 5000
                ? (metres / 1000).ToString(CultureInfo.InvariantCulture) + " kilometres"
                : metres.ToString(CultureInfo.InvariantCulture) + " metres";
        }

        private static string Weather(string code)
        {
            string intensity = code.StartsWith("-", StringComparison.Ordinal) ? "light " : code.StartsWith("+", StringComparison.Ordinal) ? "heavy " : string.Empty;
            string body = code.TrimStart('-', '+');
            switch (body)
            {
                case "RA": return intensity + "rain";
                case "SN": return intensity + "snow";
                case "TS": return "thunderstorm";
                case "TSRA": return "thunderstorm with " + intensity + "rain";
                case "TSSN": return "thunderstorm with " + intensity + "snow";
                case "FG": return "fog";
                case "FZFG": return "freezing fog";
                case "BR": return "mist";
                default: return code;
            }
        }

        private static string Cloud(CloudLayer layer)
        {
            string height = (layer.BaseHundreds * 100).ToString("N0", CultureInfo.InvariantCulture);
            if (layer.Cover == "VV")
            {
                return "vertical visibility " + height + " feet";
            }
            string cover = layer.Cover == "FEW" ? "few" : layer.Cover == "SCT" ? "scattered" : layer.Cover == "BKN" ? "broken" : "overcast";
            return cover + " " + height + (layer.Cumulonimbus ? " cumulonimbus" : string.Empty);
        }

        /// <summary>The kind of disaster that closed the airport, from its prefab's name, in the broadcast's words.</summary>
        public static string ClosureReason(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
            {
                return "emergency";
            }
            string name = prefabName.ToLowerInvariant();
            return name.Contains("tornado") ? "tornado" : name.Contains("hail") ? "hail storm" : name.Contains("storm") || name.Contains("thunder") ? "thunderstorm" : "emergency";
        }
    }
}
