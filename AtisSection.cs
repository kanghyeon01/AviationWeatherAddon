using System.Collections.Generic;
using Colossal.UI.Binding;
using Game.UI.InGame;
using Unity.Entities;

namespace RealisticAviationATIS
{
    /// <summary>
    /// The airport's latest weather report in its info panel, with the report in plain words for the tooltip.
    /// RealisticAviationATIS.mjs draws it.
    /// </summary>
    public partial class AtisSection : InfoSectionBase
    {
        private WeatherStationSystem m_StationSystem;
        private EntityQuery m_AirplaneStopQuery;
        private Entity m_Airport;
        private uint m_Version;
        private string m_Metar;
        private readonly List<string> m_Decoded = new List<string>();

        protected override string group => nameof(AtisSection);

        protected override void OnCreate()
        {
            base.OnCreate();
            m_StationSystem = World.GetOrCreateSystemManaged<WeatherStationSystem>();
            m_AirplaneStopQuery = Airports.CreateAirplaneStopQuery(EntityManager);
            m_InfoUISystem.AddMiddleSection(this);
        }

        protected override void OnUpdate()
        {
            m_Airport = Airports.GetAirport(EntityManager, m_AirplaneStopQuery, selectedEntity);
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
        }

        protected override void OnProcess()
        {
            m_StationSystem.RefreshStation(m_Airport);
            if (m_StationSystem.TryGetReport(m_Airport, out StationReport report))
            {
                m_Metar = report.Text;
                MetarDecoder.Decode(report, m_Decoded);
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
        }
    }
}
