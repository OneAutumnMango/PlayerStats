using System.Collections.Generic;
using UnityEngine;

namespace PlayerStats.StatsOverlay
{
    /// <summary>
    /// Accumulates damage, attempts, and hit data per spell per player.
    /// Keys: playerNumber -> SpellName (as int) -> stats.
    /// </summary>
    public static class SpellDamageTracker
    {
        public struct SpellStats
        {
            public float TotalDamage;
            public int   Attempts;   // casts via SpellManager.CastSpell
            public int   Hits;       // distinct hit events (deduped per victim; single-hit-per-cast spells use cast-victim set)
        }

        // outer key = playerNumber, inner key = (int)SpellName
        private static readonly Dictionary<int, Dictionary<int, SpellStats>> _data =
            new Dictionary<int, Dictionary<int, SpellStats>>();

        // Dedupe: last cast time per Key2(owner, spell) — prevents double-counting rapid re-triggers
        private static readonly Dictionary<long, float> _lastCastDedupe =
            new Dictionary<long, float>();
        private const float CastDedupeWindow = 0.1f; // 100ms

        // Dedupe: last hit time per Key3(owner, spell, victim) — eats multi-collision spam
        private static readonly Dictionary<long, float> _lastHitDedupe =
            new Dictionary<long, float>();
        // Fixed 300ms window for normal spells.
        private const float HitDedupeWindow = 0.3f;

        // Spells where sub-projectiles/bounces can re-hit the same target within one cast.
        // For these, we track which victims were already hit this cast rather than using a timer.
        // The victim set is reset only when that specific spell is recast — other spells are unaffected.
        private static readonly HashSet<int> _singleHitPerCastSpells = new HashSet<int>
        {
            (int)SpellName.SevenTears,
            (int)SpellName.PetRock,
            (int)SpellName.StealTrap,
            (int)SpellName.HotSwap,
            (int)SpellName.TowVine,
            (int)SpellName.NorthPull,
            (int)SpellName.Wormhole,
            (int)SpellName.IcePack,
            (int)SpellName.IceHook,
            (int)SpellName.SomerAssault,
            (int)SpellName.Urchain,
            (int)SpellName.Brrage,
        };
        // Key = Key2(player, spell) -> victims already hit in the current cast of that spell
        private static readonly Dictionary<long, HashSet<int>> _castHitVictims =
            new Dictionary<long, HashSet<int>>();

        // Spells excluded from hit tracking (show '--'): DoTs and non-aim spells.
        private static readonly HashSet<int> _noHitSpells = new HashSet<int>
        {
            // DoT sources — ticks would inflate hit counts
            (int)SpellName.Ignite,
            (int)SpellName.FlameLeash,
            (int)SpellName.Tonic,
            (int)SpellName.ChainLightning,
            (int)SpellName.LightningBug,
            65,                            // CrystalObject cold DoT secondary
            (int)SpellName.Relapse,
            (int)SpellName.Tsunami,
            (int)SpellName.WaterCannon,
            // Non-aim spells — no meaningful miss concept
            (int)SpellName.Spitfire,
            (int)SpellName.Bombshell,

            (int)SpellName.Rewind,
            (int)SpellName.Backup,
            (int)SpellName.BubbleBreaker,
            (int)SpellName.Discharge,
            (int)SpellName.Chainmail,
            (int)SpellName.Deflect,
            (int)SpellName.Preserve,
            (int)SpellName.PillarOfFire,
            (int)SpellName.Vanguard,
            (int)SpellName.RockBlock,
            // non-hit spells
            (int)SpellName.FlashFlood,
            (int)SpellName.DoubleStrike,
            (int)SpellName.Vacuum

        };
        public static bool IsHitTrackingExcluded(int spellSource) =>
            _noHitSpells.Contains(spellSource);

        public static void Clear()
        {
            _data.Clear();
            _lastCastDedupe.Clear();
            _lastHitDedupe.Clear();
            _castHitVictims.Clear();
        }

        // Combines two shorts into a long key
        private static long Key2(int a, int b) => ((long)a << 32) | (uint)b;
        private static long Key3(int a, int b, int c) => ((long)(a * 397 ^ b) << 32) | (uint)c;

        private static SpellStats GetOrCreate(int playerNumber, int spellSource)
        {
            if (!_data.TryGetValue(playerNumber, out var spellMap))
            {
                spellMap = new Dictionary<int, SpellStats>();
                _data[playerNumber] = spellMap;
            }
            spellMap.TryGetValue(spellSource, out var stats);
            return stats;
        }

        private static void Set(int playerNumber, int spellSource, SpellStats stats)
        {
            _data[playerNumber][spellSource] = stats;
        }

        public static void AddDamage(int playerNumber, int spellSource, float damage)
        {
            if (damage <= 0f) return;
            var s = GetOrCreate(playerNumber, spellSource);
            s.TotalDamage += damage;
            Set(playerNumber, spellSource, s);
        }

        public static void AddCast(int playerNumber, int spellSource)
        {
            float now = Time.unscaledTime;
            long key = Key2(playerNumber, spellSource);
            if (_lastCastDedupe.TryGetValue(key, out float last) && now - last < CastDedupeWindow)
                return;
            _lastCastDedupe[key] = now;

            // Reset victim set for this spell so the new cast starts fresh.
            // Only this spell's entry is cleared — other spells are unaffected.
            if (_singleHitPerCastSpells.Contains(spellSource))
                _castHitVictims[key] = new HashSet<int>();

            var s = GetOrCreate(playerNumber, spellSource);
            s.Attempts++;
            Set(playerNumber, spellSource, s);
        }

        /// <summary>
        /// Records a hit. Returns true if this was a new (non-deduped) hit.
        /// </summary>
        public static bool AddHit(int playerNumber, int spellSource, int victimNumber)
        {
            if (_noHitSpells.Contains(spellSource)) return false;
            if (PlayerManager.players == null || !PlayerManager.players.ContainsKey(victimNumber))
                return false;

            float now = Time.unscaledTime;

            if (_singleHitPerCastSpells.Contains(spellSource))
            {
                // At most 1 hit per victim per cast. Uses the victim set reset by AddCast.
                long castKey = Key2(playerNumber, spellSource);
                if (!_castHitVictims.TryGetValue(castKey, out var victims))
                {
                    victims = new HashSet<int>();
                    _castHitVictims[castKey] = victims;
                }
                if (!victims.Add(victimNumber))
                    return false;
            }
            else
            {
                long key = Key3(playerNumber, spellSource, victimNumber);
                if (_lastHitDedupe.TryGetValue(key, out float last) && now - last < HitDedupeWindow)
                    return false;
                _lastHitDedupe[key] = now;
            }

            var s = GetOrCreate(playerNumber, spellSource);
            s.Hits++;
            Set(playerNumber, spellSource, s);
            return true;
        }

        /// <summary>Returns spell stats for a player, sorted descending by damage.</summary>
        public static List<(string spellName, SpellStats stats, int source)> GetForPlayer(int playerNumber)
        {
            var result = new List<(string spellName, SpellStats stats, int source)>();
            if (!_data.TryGetValue(playerNumber, out var spellMap))
                return result;

            foreach (var kvp in spellMap)
            {
                string name;
                try { name = ((SpellName)kvp.Key).ToString(); }
                catch { name = "Source " + kvp.Key; }
                result.Add((spellName: name, stats: kvp.Value, source: kvp.Key));
            }

            result.Sort((a, b) => b.stats.TotalDamage.CompareTo(a.stats.TotalDamage));
            return result;
        }

        public static IEnumerable<int> GetTrackedPlayers() => _data.Keys;
    }
}

