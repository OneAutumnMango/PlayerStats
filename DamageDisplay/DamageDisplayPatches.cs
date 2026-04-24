using HarmonyLib;
using System.Collections.Generic;

namespace PlayerStats.DamageDisplay
{
    [HarmonyPatch]
    public static class DamageDisplayPatches
    {
        // Player name per number so we can recompose nameText.text
        private static readonly Dictionary<int, string> _playerNames = new();

        [HarmonyPatch(typeof(RoundRecapManager), nameof(RoundRecapManager.Initialize))]
        [HarmonyPrefix]
        static void OnRecapInit()
        {
            if (PlayerManager.round <= 1)
            {
                _playerNames.Clear();
            }
        }

        /// <summary>
        /// Postfix on UpdateName — the method that writes to nameText.
        /// Append the accumulated damage on a second line.
        /// </summary>
        [HarmonyPatch(typeof(RecapCard), nameof(RecapCard.UpdateName))]
        [HarmonyPostfix]
        static void OnUpdateName(RecapCard __instance, string name, int owner)
        {
            // Store the canonical name so we can recompose later
            _playerNames[owner] = __instance.nameText.text;

            // Read damage from the player's live scores.
            // At UpdateName time (called during RoundRecapManager.Initialize),
            // the current round's damage is still in roundScore — it only gets
            // merged into totalScore later during the CondenseKills state.
            float damage = 0f;
            if (PlayerManager.players != null &&
                PlayerManager.players.TryGetValue(owner, out var player))
            {
                damage += player.roundScore?.damageDealt ?? 0f;
                damage += player.totalScore?.damageDealt ?? 0f;
            }

            __instance.nameText.text += $"\n{(int)damage} dmg";
            Plugin.Log.LogInfo($"[DamageDisplay] UpdateName: player={owner}, damage={damage}");
        }

        /// <summary>
        /// End-of-match when ShowStats runs — update nameText from statsDamage.
        /// </summary>
        [HarmonyPatch(typeof(RecapCard), nameof(RecapCard.ShowStats))]
        [HarmonyPostfix]
        static void OnShowStats(RecapCard __instance)
        {
            var player = Traverse.Create(__instance).Field("player").GetValue<Player>();
            if (player == null) return;

            // Read fresh damage from the live score
            float damage = player.totalScore?.damageDealt ?? 0f;

            if (_playerNames.TryGetValue(player.playerNumber, out string baseName))
            {
                __instance.nameText.text = baseName + $"\n{(int)damage} dmg";
            }
            else
            {
                __instance.nameText.text += $"\n{(int)damage} dmg";
            }
            Plugin.Log.LogInfo($"[DamageDisplay] ShowStats: {player.name} => {(int)damage} dmg");
        }
    }
}
