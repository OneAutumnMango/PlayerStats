using System.Collections.Generic;

namespace PlayerStats.StatsOverlay
{
    /// <summary>
    /// Accumulates damage-dealt per spell per player across the whole game session.
    /// Keys: playerNumber -> SpellName (as int) -> total damage.
    /// </summary>
    public static class SpellDamageTracker
    {
        // outer key = playerNumber, inner key = (int)SpellName
        private static readonly Dictionary<int, Dictionary<int, float>> _data =
            new Dictionary<int, Dictionary<int, float>>();

        public static void Clear()
        {
            _data.Clear();
        }

        public static void Add(int playerNumber, int spellSource, float damage)
        {
            if (damage <= 0f) return;

            if (!_data.TryGetValue(playerNumber, out var spellMap))
            {
                spellMap = new Dictionary<int, float>();
                _data[playerNumber] = spellMap;
            }

            if (spellMap.TryGetValue(spellSource, out float existing))
                spellMap[spellSource] = existing + damage;
            else
                spellMap[spellSource] = damage;
        }

        /// <summary>Returns a snapshot of spell damage for a player, sorted descending by damage.</summary>
        public static List<(string spellName, int damage)> GetForPlayer(int playerNumber)
        {
            var result = new List<(string spellName, int damage)>();
            if (!_data.TryGetValue(playerNumber, out var spellMap))
                return result;

            foreach (var kvp in spellMap)
            {
                // Try to cast the int back to SpellName; fall back to the raw int.
                string name;
                try { name = ((SpellName)kvp.Key).ToString(); }
                catch { name = "Source " + kvp.Key; }

                result.Add((name, (int)kvp.Value));
            }

            result.Sort((a, b) => b.damage.CompareTo(a.damage));
            return result;
        }

        public static IEnumerable<int> GetTrackedPlayers() => _data.Keys;
    }
}
