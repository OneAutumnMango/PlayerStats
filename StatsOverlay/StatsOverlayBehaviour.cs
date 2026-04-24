using MageQuitModFramework.Data;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerStats.StatsOverlay
{
    public class StatsOverlayBehaviour : MonoBehaviour
    {
        public static StatsOverlayBehaviour Instance { get; private set; }

        private struct PlayerStatSnapshot
        {
            public string Name;
            public int    PlayerNumber;
            public int    Kills;
            public int    Deaths;
            public int    Damage;
            public int    Healing;
        }

        private readonly List<PlayerStatSnapshot> _snapshots = new List<PlayerStatSnapshot>();
        public bool IsVisible = true;

        private bool _roundActive;
        private float _nextRefresh;
        private const float RefreshInterval = 0.5f;

        // Cached GUIStyle so it isn't rebuilt every frame
        private GUIStyle _labelStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _dimStyle;

        private void Awake()
        {
            Instance = this;
            GameEventsObserver.SubscribeToRoundStart(OnRoundStart);
            GameEventsObserver.SubscribeToRoundEnd(OnRoundEnd);
        }

        private void OnDestroy()
        {
            GameEventsObserver.UnsubscribeFromRoundStart(OnRoundStart);
            GameEventsObserver.UnsubscribeFromRoundEnd(OnRoundEnd);
            if (Instance == this)
                Instance = null;
        }

        private void OnRoundStart()
        {
            // Round 1 means a brand-new game session — reset spell damage tallies.
            if (PlayerManager.round == 1)
                SpellDamageTracker.Clear();

            _roundActive = true;
            _nextRefresh = 0f;
        }

        private void OnRoundEnd()
        {
            RefreshStats();
            _roundActive = false;
        }

        private void Update()
        {
            if (_roundActive && Time.unscaledTime >= _nextRefresh)
            {
                RefreshStats();
                _nextRefresh = Time.unscaledTime + RefreshInterval;
            }
        }

        private void RefreshStats()
        {
            _snapshots.Clear();
            if (PlayerManager.players == null)
                return;

            foreach (var kvp in PlayerManager.players)
            {
                var p = kvp.Value;
                // roundScore holds the current round's in-progress stats;
                // totalScore holds all completed rounds. Sum both for the true total.
                float dmg     = (p.roundScore?.damageDealt    ?? 0f) + (p.totalScore?.damageDealt    ?? 0f);
                float heal    = (p.roundScore?.healingApplied ?? 0f) + (p.totalScore?.healingApplied ?? 0f);
                int   kills   = (p.roundScore?.SumKills()     ?? 0)  + (p.totalScore?.SumKills()     ?? 0);
                int   deaths  = (p.roundScore?.deaths         ?? 0)  + (p.totalScore?.deaths         ?? 0);

                _snapshots.Add(new PlayerStatSnapshot
                {
                    Name         = string.IsNullOrEmpty(p.name) ? "Player " + p.playerNumber : p.name,
                    PlayerNumber = p.playerNumber,
                    Kills        = kills,
                    Deaths       = deaths,
                    Damage       = (int)dmg,
                    Healing      = (int)heal,
                });
            }
        }

        private void EnsureStyles()
        {
            if (_labelStyle != null)
                return;

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 13,
                alignment = TextAnchor.MiddleLeft,
            };
            _labelStyle.normal.textColor = Color.white;

            _headerStyle = new GUIStyle(_labelStyle)
            {
                fontStyle = FontStyle.Bold,
            };
            _headerStyle.normal.textColor = new Color(1f, 0.85f, 0.35f);

            _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.normal.background = MakeTex(1, 1, new Color(0f, 0f, 0f, 0.6f));

            _dimStyle = new GUIStyle(_labelStyle)
            {
                fontSize = 11,
            };
            _dimStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        }

        private static Texture2D MakeTex(int w, int h, Color col)
        {
            var pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;
            var tex = new Texture2D(w, h);
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }

        private void OnGUI()
        {
            if (!IsVisible || _snapshots.Count == 0)
                return;

            EnsureStyles();

            const float nameW  = 160f;
            const float statW  = 54f;
            const float pad    = 8f;
            const float rowH   = 20f;
            const float spellIndent = 12f;
            const float spellNameW  = 130f;
            const float spellDmgW   = 46f;
            const float spellPctW   = 44f;

            // --- Pre-calculate total height including spell rows ---
            float totalW = pad * 2 + nameW + statW * 4;
            float totalH = pad * 2 + rowH; // header

            var spellBreakdowns = new System.Collections.Generic.List<System.Collections.Generic.List<(string, int)>>();
            foreach (var s in _snapshots)
            {
                totalH += rowH;
                var breakdown = s.PlayerNumber >= 0
                    ? SpellDamageTracker.GetForPlayer(s.PlayerNumber)
                    : new System.Collections.Generic.List<(string, int)>();
                spellBreakdowns.Add(breakdown);
                totalH += breakdown.Count * rowH;
            }

            float x = Screen.width - totalW - 10f;
            float y = 10f;

            GUI.Box(new Rect(x, y, totalW, totalH), GUIContent.none, _boxStyle);

            float cx = x + pad;
            float cy = y + pad;

            // Header row
            GUI.Label(new Rect(cx,                    cy, nameW, rowH), "Player",  _headerStyle);
            GUI.Label(new Rect(cx + nameW,            cy, statW, rowH), "Kills",   _headerStyle);
            GUI.Label(new Rect(cx + nameW + statW,    cy, statW, rowH), "Deaths",  _headerStyle);
            GUI.Label(new Rect(cx + nameW + statW*2,  cy, statW, rowH), "Damage",  _headerStyle);
            GUI.Label(new Rect(cx + nameW + statW*3,  cy, statW, rowH), "Healing", _headerStyle);
            cy += rowH;

            for (int i = 0; i < _snapshots.Count; i++)
            {
                var s = _snapshots[i];
                GUI.Label(new Rect(cx,                    cy, nameW, rowH), s.Name,              _labelStyle);
                GUI.Label(new Rect(cx + nameW,            cy, statW, rowH), s.Kills.ToString(),  _labelStyle);
                GUI.Label(new Rect(cx + nameW + statW,    cy, statW, rowH), s.Deaths.ToString(), _labelStyle);
                GUI.Label(new Rect(cx + nameW + statW*2,  cy, statW, rowH), s.Damage.ToString(), _labelStyle);
                GUI.Label(new Rect(cx + nameW + statW*3,  cy, statW, rowH), s.Healing.ToString(),_labelStyle);
                cy += rowH;

                // Spell breakdown sub-rows
                int totalDmg = s.Damage > 0 ? s.Damage : 1;
                foreach (var (spellName, dmg) in spellBreakdowns[i])
                {
                    int pct = Mathf.RoundToInt(dmg * 100f / totalDmg);
                    GUI.Label(new Rect(cx + spellIndent,                         cy, spellNameW, rowH), spellName,          _dimStyle);
                    GUI.Label(new Rect(cx + spellIndent + spellNameW,            cy, spellDmgW,  rowH), dmg.ToString(),     _dimStyle);
                    GUI.Label(new Rect(cx + spellIndent + spellNameW + spellDmgW, cy, spellPctW, rowH), pct + "%",          _dimStyle);
                    cy += rowH;
                }
            }
        }
    }
}
