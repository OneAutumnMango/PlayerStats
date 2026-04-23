using HarmonyLib;
using MageQuitModFramework.Modding;

namespace PlayerStats.DamageDisplay
{
    public class DamageDisplayModule : BaseModule
    {
        public override string ModuleName => "DamageDisplay";

        protected override void OnLoad(Harmony harmony)
        {
            PatchGroup(harmony, typeof(DamageDisplayPatches));
        }

        protected override void OnUnload(Harmony harmony)
        {
            harmony.UnpatchSelf();
        }
    }
}
