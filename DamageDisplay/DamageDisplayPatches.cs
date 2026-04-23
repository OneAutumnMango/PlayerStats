using HarmonyLib;
using System.Collections.Generic;

namespace PlayerStats.DamageDisplay
{
    [HarmonyPatch]
    public static class DamageDisplayPatches
    {
        // Persists across rounds — updated by OnAddScore
        private static readonly Dictionary<int, float> _knownDamage = new();
        // Player name per number so we can recompose nameText.text
        private static readonly Dictionary<int, string> _playerNames = new();

        [HarmonyPatch(typeof(RoundRecapManager), nameof(RoundRecapManager.Initialize))]
        [HarmonyPrefix]
        static void OnRecapInit()
        {
            if (PlayerManager.round <= 1)
            {
                _knownDamage.Clear();
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
            // Store the canonical name so OnAddScore can recompose later
            _playerNames[owner] = __instance.nameText.text;

            float damage = _knownDamage.TryGetValue(owner, out float d) ? d : 0f;
            __instance.nameText.text += $"\n{(int)damage} dmg";
            Plugin.Log.LogInfo($"[DamageDisplay] UpdateName: player={owner}, damage={damage}");
        }

        /// <summary>
        /// Fires when totalScore.AddScore(roundScore) runs — scores are real here.
        /// </summary>
        [HarmonyPatch(typeof(Score), nameof(Score.AddScore))]
        [HarmonyPostfix]
        static void OnAddScore(Score __instance, Score score)
        {
            Plugin.Log.LogInfo($"[DamageDisplay] OnAddScore fired: damageDealt={__instance.damageDealt}, added={score.damageDealt}");

            if (PlayerManager.players == null) return;

            foreach (var kvp in PlayerManager.players)
            {
                if (kvp.Value.totalScore != __instance) continue;

                int playerNum = kvp.Key;
                _knownDamage[playerNum] = __instance.damageDealt;

                if (_playerNames.TryGetValue(playerNum, out string baseName))
                {
                    var recap = UnityEngine.Object.FindObjectOfType<RoundRecapManager>();
                    if (recap == null) break;

                    var playerCards = Traverse.Create(recap).Field("playerCards")
                        .GetValue<System.Collections.Generic.Dictionary<int, RecapCard>>();
                    if (playerCards == null || !playerCards.TryGetValue(playerNum, out var card)) break;

                    card.nameText.text = baseName + $"\n{(int)__instance.damageDealt} dmg";
                    Plugin.Log.LogInfo($"[DamageDisplay] OnAddScore: {kvp.Value.name} => {(int)__instance.damageDealt} dmg");
                }
                break;
            }
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

            if (_playerNames.TryGetValue(player.playerNumber, out string baseName))
            {
                __instance.nameText.text = baseName + $"\n{__instance.statsDamage.text} dmg";
                Plugin.Log.LogInfo($"[DamageDisplay] ShowStats: {player.name} => {__instance.statsDamage.text} dmg");
            }
        }
    }
}
