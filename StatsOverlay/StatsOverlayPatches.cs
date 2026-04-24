using HarmonyLib;
using UnityEngine;
namespace PlayerStats.StatsOverlay
{
    [HarmonyPatch]
    public static class StatsOverlayPatches
    {
        // ---------- rpcApplyDamage tracking ----------

        private static float _prevDamageDealt;
        private static int   _pendingOwner;
        private static int   _pendingSource;
        private static int   _pendingVictim;

        [HarmonyPatch(typeof(WizardStatus), "rpcApplyDamage")]
        [HarmonyPrefix]
        static void BeforeApplyDamage(WizardStatus __instance, float damage, int owner, int source)
        {
            _pendingOwner  = owner;
            _pendingSource = source;
            // 'id' is private — access via Harmony Traverse
            var identity = Traverse.Create(__instance).Field("id").GetValue<Identity>();
            _pendingVictim = identity != null ? identity.owner : -1;
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
            int victim = _pendingVictim;

            if (owner == 0 || owner == victim) return;
            if (PlayerManager.players == null) return;
            if (!PlayerManager.players.TryGetValue(owner, out var player)) return;
            if (player.roundScore == null) return;

            float delta = player.roundScore.damageDealt - _prevDamageDealt;
            if (delta <= 0f) return;

            SpellDamageTracker.AddDamage(owner, source, delta);
            // Any positive delta counts as a hit — first tick of a DoT registers once;
            // the 2-second dedup window in AddHit suppresses subsequent ticks.
            SpellDamageTracker.AddHit(owner, source, victim);
        }

        // ---------- Cast attempt tracking ----------

        [HarmonyPatch(typeof(SpellManager), nameof(SpellManager.CastSpell))]
        [HarmonyPrefix]
        static void OnCastSpell(SpellName spellName, Identity identity, int spellIndex, SpellName spellNameForCooldown)
        {
            int owner = identity?.owner ?? 0;
            if (owner == 0) return;
            // spellIndex >= 0 means this is an additional/recast (e.g. StealTrap pull, NorthPull second cast).
            // Primary casts always use spellIndex = -1.
            if (spellIndex >= 0) return;
            SpellDamageTracker.AddCast(owner, (int)spellNameForCooldown);
        }
    }
}

