using BepInEx;
using BepInEx.Logging;
using MageQuitModFramework.Modding;
using MageQuitModFramework.UI;

namespace PlayerStats
{
    [BepInPlugin("com.magequit.playerstats", "PlayerStats", "1.0.0")]
    [BepInDependency("com.magequit.modframework", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log;

        private ModuleManager _moduleManager;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Log.LogInfo("PlayerStats loading...");

            _moduleManager = ModManager.RegisterMod("PlayerStats", "com.magequit.playerstats");
            _moduleManager.RegisterModule(new DamageDisplay.DamageDisplayModule());
            _moduleManager.RegisterModule(new StatsOverlay.StatsOverlayModule());

            ModUIRegistry.RegisterMod(
                "PlayerStats",
                "In-game player statistics overlays",
                BuildModUI,
                priority: 20
            );

            Log.LogInfo("PlayerStats loaded!");
        }

        private void BuildModUI()
        {
            var behaviour = StatsOverlay.StatsOverlayBehaviour.Instance;
            bool visible = behaviour != null && behaviour.IsVisible;
            string label = $"Stats Overlay: {(visible ? "ON" : "OFF")}";
            if (UIComponents.Button(label))
            {
                if (behaviour != null)
                    behaviour.IsVisible = !behaviour.IsVisible;
            }

            bool useTab = behaviour != null && behaviour.UseTabToShow;
            string tabLabel = $"Use Tab to Show: {(useTab ? "ON" : "OFF")}";
            if (UIComponents.Button(tabLabel))
            {
                if (behaviour != null)
                    behaviour.UseTabToShow = !behaviour.UseTabToShow;
            }
        }
    }
}
