using System.Collections.Generic;
using UnityEngine;

namespace Jokers
{
    public class JokerManager : MonoBehaviour
    {
        private readonly Dictionary<JokerType, IJoker> _active = new Dictionary<JokerType, IJoker>();
        private readonly List<JokerType> _order = new List<JokerType>();

        // Tag/Data-driven (ScriptableObject) pipeline - Step 2 skeleton
        [SerializeField] private List<JokerData> _activeAssets = new List<JokerData>();
        private readonly List<JokerData> _assetOrder = new List<JokerData>();

        public bool AIDrawFromFront { get { return _active.ContainsKey(JokerType.Scout); } }

        public string CurrentJokerName
        {
            get
            {
                if (_order.Count == 0) return "None";
                var top = _order[_order.Count - 1];
                IJoker j;
                if (_active.TryGetValue(top, out j) && j != null) return j.Name;
                return "None";
            }
        }

        public string ActiveJokersDescription
        {
            get
            {
                if (_order.Count == 0) return "None";
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                for (int i = 0; i < _order.Count; i++)
                {
                    IJoker j;
                    if (_active.TryGetValue(_order[i], out j) && j != null)
                    {
                        if (sb.Length > 0) sb.Append(" -> ");
                        sb.Append(j.Name);
                    }
                }
                return sb.ToString();
            }
        }

        public bool IsActive(JokerType type) { return _active.ContainsKey(type); }

        public void SetJoker(JokerType type)
        {
            _active.Clear();
            _order.Clear();
            if (type != JokerType.None)
            {
                IJoker inst = CreateInstance(type);
                if (inst != null)
                {
                    _active[type] = inst;
                    _order.Add(type);
                }
            }
            Debug.Log("[JokerManager] Set Joker (single): " + ActiveJokersDescription);
        }

        public void ToggleJoker(JokerType type)
        {
            if (type == JokerType.None)
            {
                _active.Clear();
                _order.Clear();
                Debug.Log("[JokerManager] Cleared all jokers");
                return;
            }

            if (_active.ContainsKey(type))
            {
                _active.Remove(type);
                _order.Remove(type);
                Debug.Log("[JokerManager] Disabled: " + type);
            }
            else
            {
                IJoker inst = CreateInstance(type);
                if (inst != null)
                {
                    _active[type] = inst;
                    _order.Add(type);
                    Debug.Log("[JokerManager] Enabled: " + type);
                }
            }
        }

        private IJoker CreateInstance(JokerType type)
        {
            switch (type)
            {
                case JokerType.AllInRock: return new AllInRockJoker();
                case JokerType.Contrarian: return new ContrarianJoker();
                case JokerType.Scout: return new ScoutJoker();
                default: return null;
            }
        }

        public void OnRoundStart(GameManager gm)
        {
            for (int i = 0; i < _order.Count; i++)
            {
                IJoker j;
                if (_active.TryGetValue(_order[i], out j) && j != null)
                {
                    j.OnRoundStart(gm);
                }
            }

            // Tag/data-driven: placeholder log (Step 2)
            if (_assetOrder.Count > 0)
            {
                Debug.Log($"[JokerManager][Tags] OnRoundStart with {_assetOrder.Count} asset jokers (no-op)");
            }
        }

        public void OnJokerToggled(GameManager gm)
        {
            for (int i = 0; i < _order.Count; i++)
            {
                IJoker j;
                if (_active.TryGetValue(_order[i], out j) && j != null)
                {
                    j.OnRoundStart(gm);
                }
            }

            // Tag/data-driven: placeholder log (Step 2)
            if (_assetOrder.Count > 0)
            {
                Debug.Log($"[JokerManager][Tags] OnJokerToggled with {_assetOrder.Count} asset jokers (no-op)");
            }
        }

        public int ModifyScore(int baseScore, ref int currentTotalScore, Choice playerChoice, Outcome outcome)
        {
            int score = baseScore;
            for (int i = 0; i < _order.Count; i++)
            {
                IJoker j;
                if (_active.TryGetValue(_order[i], out j) && j != null)
                {
                    score = j.ApplyScoreModification(score, ref currentTotalScore, playerChoice, outcome);
                }
            }

            // Tag/data-driven: will be applied in Step 4/7
            if (_assetOrder.Count > 0)
            {
                Debug.Log("[JokerManager][Tags] ModifyScore adapter (no-op at Step 2)");
            }

            return score;
        }

        // -------- Tag/Data-driven API (Step 2 skeleton) --------
        public void SetByAsset(JokerData data)
        {
            _activeAssets.Clear();
            _assetOrder.Clear();
            if (data != null)
            {
                _activeAssets.Add(data);
                _assetOrder.Add(data);
            }
            Debug.Log($"[JokerManager][Tags] SetByAsset: {(data != null ? data.jokerName : "None")}");
        }

        public void ToggleByAsset(JokerData data)
        {
            if (data == null)
            {
                _activeAssets.Clear();
                _assetOrder.Clear();
                Debug.Log("[JokerManager][Tags] Cleared all asset jokers");
                return;
            }

            if (_activeAssets.Contains(data))
            {
                _activeAssets.Remove(data);
                _assetOrder.Remove(data);
                Debug.Log($"[JokerManager][Tags] Disabled: {data.jokerName}");
            }
            else
            {
                _activeAssets.Add(data);
                _assetOrder.Add(data);
                Debug.Log($"[JokerManager][Tags] Enabled: {data.jokerName}");
            }
        }

        public void ExecuteTurnSettlementEffects(GameContext context)
        {
            if (context == null) return;
            if (_assetOrder.Count == 0) return; // no asset jokers
            Debug.Log($"[JokerManager][Tags] ExecuteTurnSettlementEffects with {_assetOrder.Count} asset jokers (no-op)");
        }
    }
}
