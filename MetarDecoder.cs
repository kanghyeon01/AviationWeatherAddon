using System.Collections.Generic;
using System.Globalization;
using Game.SceneFlow;

namespace RealisticAviationATIS
{
    /// <summary>A report in plain words, one line per group, in the game's language (English where it has no entries).</summary>
    internal static class MetarDecoder
    {
        public static void Decode(StationReport report, List<string> lines)
        {
            lines.Clear();
            Observation o = report.Observation;
            CultureInfo invariant = CultureInfo.InvariantCulture;
            lines.Add(Format(report.Special ? "Report.SPECI" : "Report.METAR", report.Station, o.Time.Day.ToString(invariant), o.Time.ToString("HH:mm", invariant)));
            if (o.WindKnots < 3)
            {
                lines.Add(Text("Wind.Calm"));
            }
            else if (o.GustKnots > o.WindKnots)
            {
                lines.Add(Format("Wind.Gust", o.WindDirection.ToString("000", invariant), o.WindKnots.ToString(invariant), o.GustKnots.ToString(invariant)));
            }
            else
            {
                lines.Add(Format("Wind", o.WindDirection.ToString("000", invariant), o.WindKnots.ToString(invariant)));
            }
            if (o.IsCavok)
            {
                lines.Add(Text("CAVOK"));
            }
            else
            {
                lines.Add(o.VisibilityMetres >= 10000 ? Text("Visibility.Max") : Format("Visibility", o.VisibilityMetres.ToString("N0", invariant)));
                for (int i = 0; i < o.Weather.Count; i++)
                {
                    lines.Add(Text("Wx[" + o.Weather[i] + "]"));
                }
                if (o.Clouds.Count == 0)
                {
                    lines.Add(Text("Clouds.None"));
                }
                for (int i = 0; i < o.Clouds.Count; i++)
                {
                    string layer = Format("Clouds[" + o.Clouds[i].Cover + "]", (o.Clouds[i].BaseHundreds * 100).ToString("N0", invariant));
                    lines.Add(o.Clouds[i].Cumulonimbus ? Format("Clouds.CB", layer) : layer);
                }
            }
            lines.Add(Format("Temperature", o.Temperature.ToString(invariant), o.DewPoint.ToString(invariant)));
            lines.Add(Format("QNH", o.Qnh.ToString(invariant)));
            lines.Add(Text("Remarks"));
        }

        private static string Text(string key)
        {
            key = LocaleBase.kPrefix + key;
            if (GameManager.instance.localizationManager.activeDictionary.TryGetValue(key, out string text)
                || LocaleEN.Strings.TryGetValue(key, out text))
            {
                return text;
            }
            return key;
        }

        private static string Format(string key, params object[] args) => string.Format(CultureInfo.InvariantCulture, Text(key), args);
    }
}
