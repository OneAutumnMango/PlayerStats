using HarmonyLib;

namespace PlayerStats.StatsOverlay
{
    [HarmonyPatch]
    public static class StatsOverlayPatches
    {
        // Stores the player's damageDalt value before rpcApplyDamage executes
        // so the postfix can compute the delta that was actually credited.
        private static float _prevDamageDealt;
        private static int   _pendingOwner;
        private static int   _pendingSource;

        [HarmonyPatch(typeof(WizardStatus), "rpcApplyDamage")]
        [HarmonyPrefix]
        static void BeforeApplyDamage(float damage, int owner, int source)
        {
            _pendingOwner  = owner;
            _pendingSource = source;
            _prevDamageDealt = 0f;

            if (owner != 0 &&
                PlayerManager.players != null &&
                PlayerManager.players.TryGetValue(owner, out var player) &&
                player.roundScore != null)
            {
                _prevDamageDealt = player.roundScore.damageDealt;
            }
        }

        [HarmonyPatch(typeof(WizardStatus), "rpcApplyDamage")]
        [HarmonyPostfix]
        static void AfterApplyDamage()
        {
            int owner  = _pendingOwner;
            int source = _pendingSource;

            if (owner == 0) return;
            if (PlayerManager.players == null) return;
            if (!PlayerManager.players.TryGetValue(owner, out var player)) return;
            if (player.roundScore == null) return;

            float delta = player.roundScore.damageDealt - _prevDamageDealt;
            if (delta <= 0f) return;

            // source < 0 means environmental/special; still track with that key
            SpellDamageTracker.Add(owner, source, delta);
        }


    }
}
