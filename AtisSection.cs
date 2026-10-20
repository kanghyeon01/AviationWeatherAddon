using System.Collections.Generic;
using Colossal.UI.Binding;
using Game.UI.InGame;
using RealisticAviation;
using Unity.Entities;

namespace RealisticAviationATIS
{
    /// <summary>
    /// The airport's latest weather report and its ATIS broadcast in its info panel, each with its plain words for the tooltip.
    /// RealisticAviationATIS.mjs draws it.
    /// </summary>
    public partial class AtisSection : InfoSectionBase
    {
        private WeatherStationSystem m_StationSystem;
        private Entity m_Airport;
        private uint m_Version;
        private string m_Metar;
        private readonly List<string> m_Decoded = new List<string>();
        private string m_AtisLetter;
        private string m_AtisFrequency;
        private string m_AtisText;
        private readonly List<string> m_AtisDecoded = new List<string>();

        protected override string group => nameof(AtisSection);

        protected override void OnCreate()
        {
            base.OnCreate();
            m_StationSystem = World.GetOrCreateSystemManaged<WeatherStationSystem>();
            m_InfoUISystem.AddMiddleSection(this);
        }

        protected override void OnUpdate()
        {
            m_Airport = Api.Airports.Find(selectedEntity);
            visible = m_Airport != Entity.Null;
        }

        /// <summary>Refreshes the panel as soon as there is a new report, not only when the panel next updates by itself.</summary>
        protected override void OnPreUpdate()
        {
            if (visible && m_StationSystem.Version != m_Version)
            {
                m_Version = m_StationSystem.Version;
                m_InfoUISystem.RequestUpdate();
            }
        }

        protected override void Reset()
        {
            m_Metar = string.Empty;
            m_Decoded.Clear();
            m_AtisLetter = string.Empty;
            m_AtisFrequency = string.Empty;
            m_AtisText = string.Empty;
            m_AtisDecoded.Clear();
        }

        protected override void OnProcess()
        {
            m_StationSystem.RefreshStation(m_Airport);
            if (m_StationSystem.TryGetReport(m_Airport, out StationReport report))
            {
                m_Metar = report.Text;
                MetarDecoder.Decode(report, m_Decoded);
                AtisBroadcast atis = report.Atis;
                if (atis.Letter >= 0)
                {
                    m_AtisLetter = Atis.kPhonetic[atis.Letter];
                    m_AtisFrequency = atis.Frequency;
                    m_AtisText = atis.Text;
                    MetarDecoder.DecodeAtis(atis, m_AtisDecoded);
                }
            }
        }

        public override void OnWriteProperties(IJsonWriter writer)
        {
            // Always the same properties: the UI engine crashes natively on a property that comes and goes.
            writer.PropertyName("metar");
            writer.Write(m_Metar ?? string.Empty);
            writer.PropertyName("decoded");
            writer.ArrayBegin(m_Decoded.Count);
            for (int i = 0; i < m_Decoded.Count; i++)
            {
                writer.Write(m_Decoded[i]);
            }
            writer.ArrayEnd();
            writer.PropertyName("atisLetter");
            writer.Write(m_AtisLetter ?? string.Empty);
            writer.PropertyName("atisFrequency");
            writer.Write(m_AtisFrequency ?? string.Empty);
            writer.PropertyName("atisText");
            writer.Write(m_AtisText ?? string.Empty);
            writer.PropertyName("atisDecoded");
            writer.ArrayBegin(m_AtisDecoded.Count);
            for (int i = 0; i < m_AtisDecoded.Count; i++)
            {
                writer.Write(m_AtisDecoded[i]);
            }
            writer.ArrayEnd();
        }
    }
}
