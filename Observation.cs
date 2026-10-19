using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RealisticAviationATIS
{
    /// <summary>
    /// One cloud layer: cover FEW, SCT, BKN or OVC and base in hundreds of feet, or "VV" with the vertical visibility
    /// when fog hides the sky.
    /// </summary>
    public struct CloudLayer
    {
        public string Cover;
        public int BaseHundreds;
        public bool Cumulonimbus;
    }

    /// <summary>
    /// What the airport's automated weather station observes at one moment, already in METAR units: wind from (degrees
    /// true) and in knots, visibility in metres, present weather codes, cloud layers, temperature and dew point in whole
    /// degrees Celsius, QNH in hectopascals.
    /// </summary>
    public sealed class Observation
    {
        public DateTime Time;
        public int WindDirection;
        public int WindKnots;
        public int GustKnots;
        public int VisibilityMetres;
        public readonly List<string> Weather = new List<string>();
        public readonly List<CloudLayer> Clouds = new List<CloudLayer>();
        public int Temperature;
        public int DewPoint;
        public int Qnh;

        /// <summary>Visibility 10 km or more, no cloud below 5,000 ft and no cumulonimbus, no weather: "CAVOK".</summary>
        public bool IsCavok
        {
            get
            {
                if (VisibilityMetres < 10000 || Weather.Count != 0)
                {
                    return false;
                }
                for (int i = 0; i < Clouds.Count; i++)
                {
                    if (Clouds[i].BaseHundreds < 50 || Clouds[i].Cumulonimbus)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// What makes a special report (SPECI) between the half-hourly ones: a change of visibility band, of present
        /// weather, of the ceiling band, of cumulonimbus, or of the wind by 10 knots.
        /// </summary>
        public bool DiffersSignificantlyFrom(Observation other)
        {
            if (other == null)
            {
                return true;
            }
            return VisibilityBand(VisibilityMetres) != VisibilityBand(other.VisibilityMetres)
                || string.Join(" ", Weather) != string.Join(" ", other.Weather)
                || CeilingBand(Ceiling()) != CeilingBand(other.Ceiling())
                || HasCumulonimbus() != other.HasCumulonimbus()
                || Math.Abs(WindKnots - other.WindKnots) >= 10
                || Math.Abs(GustKnots - other.GustKnots) >= 10;
        }

        /// <summary>Base of the lowest broken or overcast layer (or the vertical visibility) in hundreds of feet, or int.MaxValue.</summary>
        public int Ceiling()
        {
            int ceiling = int.MaxValue;
            for (int i = 0; i < Clouds.Count; i++)
            {
                if ((Clouds[i].Cover == "BKN" || Clouds[i].Cover == "OVC" || Clouds[i].Cover == "VV") &&Clouds[i].BaseHundreds < ceiling)
                {
                    ceiling = Clouds[i].BaseHundreds;
                }
            }
            return ceiling;
        }

        public bool HasCumulonimbus()
        {
            for (int i = 0; i < Clouds.Count; i++)
            {
                if (Clouds[i].Cumulonimbus)
                {
                    return true;
                }
            }
            return false;
        }

        private static int VisibilityBand(int metres) => metres < 800 ? 0 : metres < 1500 ? 1 : metres < 3000 ? 2 : metres < 5000 ? 3 : metres < 10000 ? 4 : 5;

        private static int CeilingBand(int hundreds) => hundreds < 5 ? 0 : hundreds < 10 ? 1 : hundreds < 15 ? 2 : hundreds < 30 ? 3 : hundreds < 50 ? 4 : 5;

        /// <summary>
        /// The report as an automated station sends it, e.g.
        /// "METAR RKSS 191230Z AUTO 27012G22KT 4000 -SN BR BKN015 OVC030 M02/M04 Q1009 RMK AO2".
        /// </summary>
        public string ToMetar(string station, bool special)
        {
            StringBuilder text = new StringBuilder();
            text.Append(special ? "SPECI " : "METAR ").Append(station).Append(' ');
            text.Append(Time.ToString("ddHHmm", CultureInfo.InvariantCulture)).Append("Z AUTO ");
            text.Append(WindGroup()).Append(' ');
            if (IsCavok)
            {
                text.Append("CAVOK");
            }
            else
            {
                text.Append(VisibilityGroup());
                for (int i = 0; i < Weather.Count; i++)
                {
                    text.Append(' ').Append(Weather[i]);
                }
                if (Clouds.Count == 0)
                {
                    text.Append(" NCD");
                }
                for (int i = 0; i < Clouds.Count; i++)
                {
                    text.Append(' ').Append(Clouds[i].Cover).Append(Clouds[i].BaseHundreds.ToString("000", CultureInfo.InvariantCulture));
                    if (Clouds[i].Cumulonimbus)
                    {
                        text.Append("CB");
                    }
                }
            }
            text.Append(' ').Append(TemperatureGroup(Temperature)).Append('/').Append(TemperatureGroup(DewPoint));
            text.Append(" Q").Append(Qnh.ToString("0000", CultureInfo.InvariantCulture));
            text.Append(" RMK AO2");
            return text.ToString();
        }

        private string WindGroup()
        {
            if (WindKnots < 3)
            {
                return "00000KT";
            }
            string group = WindDirection.ToString("000", CultureInfo.InvariantCulture) + WindKnots.ToString("00", CultureInfo.InvariantCulture);
            if (GustKnots > WindKnots)
            {
                group += "G" + GustKnots.ToString("00", CultureInfo.InvariantCulture);
            }
            return group + "KT";
        }

        private string VisibilityGroup()
        {
            return VisibilityMetres >= 10000 ? "9999" : VisibilityMetres.ToString("0000", CultureInfo.InvariantCulture);
        }

        private static string TemperatureGroup(int celsius)
        {
            return (celsius < 0 ? "M" : string.Empty) + Math.Abs(celsius).ToString("00", CultureInfo.InvariantCulture);
        }
    }
}
