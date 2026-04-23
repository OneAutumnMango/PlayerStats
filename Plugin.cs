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

            ModUIRegistry.RegisterMod(
                "PlayerStats",
                "In-game player statistics overlays",
                null,
                priority: 20
            );

            Log.LogInfo("PlayerStats loaded!");
        }
    }
}
