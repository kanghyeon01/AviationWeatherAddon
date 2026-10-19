using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;

namespace RealisticAviationATIS
{
    public sealed class Mod : IMod
    {
        public const string kName = "RealisticAviationATIS";

        public static readonly ILog Log = LogManager.GetLogger(kName).SetShowsErrorsInUI(false);

        public void OnLoad(UpdateSystem updateSystem)
        {
            Log.Info(nameof(OnLoad));
            // After a game patch, the log shows which game and mod versions ran together.
            Log.Info("Game " + Game.Version.current.fullVersion + ", Aviation Weather Add-on " + typeof(Mod).Assembly.GetName().Version
                + ", Realistic Aviation " + typeof(RealisticAviation.Mod).Assembly.GetName().Version);
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN());
            GameManager.instance.localizationManager.AddSource("ko-KR", new LocaleKO());

            updateSystem.UpdateAt<WeatherStationSystem>(SystemUpdatePhase.GameSimulation);
            // Info panel sections aren't scheduled; the selected-info system updates the ones it knows about.
            updateSystem.World.GetOrCreateSystemManaged<AtisSection>();
        }

        public void OnDispose()
        {
            Log.Info(nameof(OnDispose));
        }
    }
}
