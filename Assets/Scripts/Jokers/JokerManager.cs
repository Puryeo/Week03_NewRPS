using System.Collections.Generic;
using UnityEngine;

namespace Jokers
{
    public class JokerManager : MonoBehaviour
    {
        private readonly Dictionary<JokerType, IJoker> _active = new Dictionary<JokerType, IJoker>();
        private readonly List<JokerType> _order = new List<JokerType>();

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
            return score;
        }
    }
}
