using System.Collections.Generic;
using Colossal;

namespace RealisticAviationATIS
{
    public abstract class LocaleBase : IDictionarySource
    {
        public const string kPrefix = Mod.kName + ".";

        protected abstract Dictionary<string, string> Entries { get; }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return Entries;
        }

        public void Unload()
        {
        }
    }

    /// <summary>English, also the fallback of <see cref="MetarDecoder"/> in languages without their own entries.</summary>
    public sealed class LocaleEN : LocaleBase
    {
        public static readonly Dictionary<string, string> Strings = new Dictionary<string, string>
        {
            { kPrefix + "Section.Label", "Weather report" },
            { kPrefix + "Section.Empty", "No report yet" },

            { kPrefix + "Report.METAR", "Routine report (METAR) from {0}, day {1} at {2} UTC, automated station" },
            { kPrefix + "Report.SPECI", "Special report (SPECI) from {0}, day {1} at {2} UTC, automated station" },
            { kPrefix + "Wind.Calm", "Wind calm" },
            { kPrefix + "Wind", "Wind from {0}° at {1} kt" },
            { kPrefix + "Wind.Gust", "Wind from {0}° at {1} kt, gusting {2} kt" },
            { kPrefix + "CAVOK", "Ceiling and visibility OK: visibility 10 km or more, no cloud below 5,000 ft, no significant weather" },
            { kPrefix + "Visibility.Max", "Visibility 10 km or more" },
            { kPrefix + "Visibility", "Visibility {0} m" },
            { kPrefix + "Wx[-RA]", "Light rain" },
            { kPrefix + "Wx[RA]", "Rain" },
            { kPrefix + "Wx[+RA]", "Heavy rain" },
            { kPrefix + "Wx[-SN]", "Light snow" },
            { kPrefix + "Wx[SN]", "Snow" },
            { kPrefix + "Wx[+SN]", "Heavy snow" },
            { kPrefix + "Wx[TS]", "Thunderstorm" },
            { kPrefix + "Wx[-TSRA]", "Thunderstorm with light rain" },
            { kPrefix + "Wx[TSRA]", "Thunderstorm with rain" },
            { kPrefix + "Wx[+TSRA]", "Thunderstorm with heavy rain" },
            { kPrefix + "Wx[-TSSN]", "Thunderstorm with light snow" },
            { kPrefix + "Wx[TSSN]", "Thunderstorm with snow" },
            { kPrefix + "Wx[+TSSN]", "Thunderstorm with heavy snow" },
            { kPrefix + "Wx[FG]", "Fog" },
            { kPrefix + "Wx[FZFG]", "Freezing fog" },
            { kPrefix + "Wx[BR]", "Mist" },
            { kPrefix + "Clouds.None", "No clouds detected" },
            { kPrefix + "Clouds[FEW]", "Few clouds at {0} ft" },
            { kPrefix + "Clouds[SCT]", "Scattered clouds at {0} ft" },
            { kPrefix + "Clouds[BKN]", "Broken clouds at {0} ft" },
            { kPrefix + "Clouds[OVC]", "Overcast at {0} ft" },
            { kPrefix + "Clouds[VV]", "Sky obscured, vertical visibility {0} ft" },
            { kPrefix + "Clouds.CB", "{0}, cumulonimbus" },
            { kPrefix + "Temperature", "Temperature {0} °C, dew point {1} °C" },
            { kPrefix + "QNH", "QNH {0} hPa" },
            { kPrefix + "Remarks", "Remarks: automated station with a precipitation sensor (AO2)" },

            { kPrefix + "Atis.Label", "ATIS" },
            { kPrefix + "Atis.Empty", "No broadcast yet" },
            { kPrefix + "Atis.Header", "Information {0}, recorded {1} UTC, frequency {2} MHz" },
            { kPrefix + "Atis.Closed", "Airport closed: {0}" },
            { kPrefix + "Atis.Closure[tornado]", "tornado" },
            { kPrefix + "Atis.Closure[hail storm]", "hail storm" },
            { kPrefix + "Atis.Closure[thunderstorm]", "thunderstorm" },
            { kPrefix + "Atis.Closure[emergency]", "emergency" },
            { kPrefix + "Atis.Runways.ILS", "ILS approach, runway {0} in use" },
            { kPrefix + "Atis.Runways.Visual", "Visual approach, runway {0} in use" },
            { kPrefix + "Atis.Advise", "Pilots report information {0} on first contact" },
        };

        protected override Dictionary<string, string> Entries => Strings;
    }

    public sealed class LocaleKO : LocaleBase
    {
        private static readonly Dictionary<string, string> Strings = new Dictionary<string, string>
        {
            { kPrefix + "Section.Label", "기상 관측 보고" },
            { kPrefix + "Section.Empty", "아직 관측 보고가 없습니다" },

            { kPrefix + "Report.METAR", "{0} 정시 관측 보고(METAR), {1}일 {2} UTC, 자동 관측" },
            { kPrefix + "Report.SPECI", "{0} 특별 관측 보고(SPECI), {1}일 {2} UTC, 자동 관측" },
            { kPrefix + "Wind.Calm", "바람 고요" },
            { kPrefix + "Wind", "바람 {0}° 방향에서 {1}노트" },
            { kPrefix + "Wind.Gust", "바람 {0}° 방향에서 {1}노트, 돌풍 {2}노트" },
            { kPrefix + "CAVOK", "운고와 시정 양호(CAVOK): 시정 10km 이상, 5,000ft 아래 구름 없음, 주요 기상 현상 없음" },
            { kPrefix + "Visibility.Max", "시정 10km 이상" },
            { kPrefix + "Visibility", "시정 {0}m" },
            { kPrefix + "Wx[-RA]", "약한 비" },
            { kPrefix + "Wx[RA]", "비" },
            { kPrefix + "Wx[+RA]", "강한 비" },
            { kPrefix + "Wx[-SN]", "약한 눈" },
            { kPrefix + "Wx[SN]", "눈" },
            { kPrefix + "Wx[+SN]", "강한 눈" },
            { kPrefix + "Wx[TS]", "뇌우" },
            { kPrefix + "Wx[-TSRA]", "뇌우와 약한 비" },
            { kPrefix + "Wx[TSRA]", "뇌우와 비" },
            { kPrefix + "Wx[+TSRA]", "뇌우와 강한 비" },
            { kPrefix + "Wx[-TSSN]", "뇌우와 약한 눈" },
            { kPrefix + "Wx[TSSN]", "뇌우와 눈" },
            { kPrefix + "Wx[+TSSN]", "뇌우와 강한 눈" },
            { kPrefix + "Wx[FG]", "안개" },
            { kPrefix + "Wx[FZFG]", "착빙성 안개" },
            { kPrefix + "Wx[BR]", "박무" },
            { kPrefix + "Clouds.None", "탐지된 구름 없음" },
            { kPrefix + "Clouds[FEW]", "구름 조금(FEW), 운저 {0}ft" },
            { kPrefix + "Clouds[SCT]", "구름 산재(SCT), 운저 {0}ft" },
            { kPrefix + "Clouds[BKN]", "구름 많음(BKN), 운저 {0}ft" },
            { kPrefix + "Clouds[OVC]", "흐림(OVC), 운저 {0}ft" },
            { kPrefix + "Clouds[VV]", "하늘 가려짐, 수직 시정 {0}ft" },
            { kPrefix + "Clouds.CB", "{0}, 적란운" },
            { kPrefix + "Temperature", "기온 {0}°C, 이슬점 {1}°C" },
            { kPrefix + "QNH", "QNH {0}hPa" },
            { kPrefix + "Remarks", "참고: 강수 감지기가 있는 자동 관측소(AO2)" },

            { kPrefix + "Atis.Label", "ATIS" },
            { kPrefix + "Atis.Empty", "아직 방송이 없습니다" },
            { kPrefix + "Atis.Header", "정보 {0}, {1} UTC 녹음, 주파수 {2}MHz" },
            { kPrefix + "Atis.Closed", "공항 폐쇄: {0}" },
            { kPrefix + "Atis.Closure[tornado]", "토네이도" },
            { kPrefix + "Atis.Closure[hail storm]", "우박" },
            { kPrefix + "Atis.Closure[thunderstorm]", "뇌우" },
            { kPrefix + "Atis.Closure[emergency]", "비상 상황" },
            { kPrefix + "Atis.Runways.ILS", "ILS 접근, 사용 활주로 {0}" },
            { kPrefix + "Atis.Runways.Visual", "시계 접근, 사용 활주로 {0}" },
            { kPrefix + "Atis.Advise", "조종사는 첫 교신 때 정보 {0} 수신 사실을 알릴 것" },
        };

        protected override Dictionary<string, string> Entries => Strings;
    }
}
