using HarmonyLib;
using MageQuitModFramework.Modding;
using UnityEngine;

namespace PlayerStats.StatsOverlay
{
    public class StatsOverlayModule : BaseModule
    {
        public override string ModuleName => "StatsOverlay";

        private GameObject _overlayObject;

        protected override void OnLoad(Harmony harmony)
        {
            PatchGroup(harmony, typeof(StatsOverlayPatches));
            _overlayObject = new GameObject("PlayerStats_StatsOverlay");
            Object.DontDestroyOnLoad(_overlayObject);
            _overlayObject.AddComponent<StatsOverlayBehaviour>();
            Plugin.Log.LogInfo("[StatsOverlay] Loaded.");
        }

        protected override void OnUnload(Harmony harmony)
        {
            harmony.UnpatchSelf();
            if (_overlayObject != null)
            {
                Object.Destroy(_overlayObject);
                _overlayObject = null;
            }
            SpellDamageTracker.Clear();
            Plugin.Log.LogInfo("[StatsOverlay] Unloaded.");
        }
    }
}
