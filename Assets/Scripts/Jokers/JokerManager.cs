using System.Collections.Generic;
using UnityEngine;

namespace Jokers
{
    // 조커 시스템의 중앙 매니저(태그/데이터 기반 전용)
    public class JokerManager : MonoBehaviour
    {
        [SerializeField] private List<JokerData> _activeData = new List<JokerData>();
        private readonly List<JokerData> _order = new List<JokerData>();

        private bool _drawFromFrontThisRound = false;
        public bool AIDrawFromFront => _drawFromFrontThisRound;

        public string GetCurrentJokerName()
        {
            if (_order.Count == 0) return "None";
            var top = _order[_order.Count - 1];
            return top != null ? (string.IsNullOrEmpty(top.jokerName) ? top.name : top.jokerName) : "None";
        }
        public string GetPipelineDescription()
        {
            if (_order.Count == 0) return "None";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < _order.Count; i++)
            {
                var d = _order[i];
                if (d == null) continue;
                string label = string.IsNullOrEmpty(d.jokerName) ? d.name : d.jokerName;
                if (sb.Length > 0) sb.Append(" -> ");
                sb.Append(label);
            }
            return sb.ToString();
        }

        public void SetJoker(JokerData data)
        {
            _activeData.Clear();
            _order.Clear();
            if (data != null)
            {
                _activeData.Add(data);
                _order.Add(data);
                ValidateAndWarn(data);
            }
            var name = data != null ? (string.IsNullOrEmpty(data.jokerName) ? data.name : data.jokerName) : "None";
            RPS.RPSLog.Event("Joker", "Set", $"name={name}");
            Debug.Log($"[JokerManager] SetJoker: {(data != null ? data.jokerName : "None")}");
        }

        public void ToggleJoker(JokerData data)
        {
            if (data == null)
            {
                _activeData.Clear();
                _order.Clear();
                RPS.RPSLog.Event("Joker", "Clear", "");
                Debug.Log("[JokerManager] Cleared all jokers");
                return;
            }

            var label = string.IsNullOrEmpty(data.jokerName) ? data.name : data.jokerName;
            if (_activeData.Contains(data))
            {
                _activeData.Remove(data);
                _order.Remove(data);
                RPS.RPSLog.Event("Joker", "Disabled", $"name={label}");
                Debug.Log($"[JokerManager] Disabled: {data.jokerName}");
            }
            else
            {
                _activeData.Add(data);
                _order.Add(data);
                RPS.RPSLog.Event("Joker", "Enabled", $"name={label}");
                Debug.Log($"[JokerManager] Enabled: {data.jokerName}");
                ValidateAndWarn(data);
            }
        }

        public void OnRoundStart(GameManager gm)
        {
            _drawFromFrontThisRound = false;
            // 전체 활성 조커 유효성 1회 점검
            for (int i = 0; i < _order.Count; i++)
            {
                var d = _order[i];
                if (d != null) ValidateAndWarn(d);
            }
            EvaluateRoundStartTags(gm, applyDrawPolicy: true);
        }

        public void OnJokerToggled(GameManager gm)
        {
            EvaluateRoundStartTags(gm, applyDrawPolicy: false);
        }

        public int ExecuteAndReturnTurnDelta(GameContext context)
        {
            if (context == null) return 0;
            ExecuteTurnSettlementEffects(context);
            return context.scoreDelta;
        }

        public void ExecuteTurnSettlementEffects(GameContext context)
        {
            if (context == null) return;
            if (_order.Count == 0) return;

            for (int i = 0; i < _order.Count; i++)
            {
                var data = _order[i];
                if (data == null || data.tags == null) continue;

                bool timingOk = false;
                foreach (var tag in data.tags)
                {
                    if (tag == null) continue;
                    if (tag.category == JokerTagCategory.Timing)
                    {
                        if (tag.timingType == JokerTimingType.None)
                            Debug.LogWarning($"[JokerManager][Validate] {data.jokerName}: Timing tag has None");
                        if (tag.timingType == JokerTimingType.TurnSettlement) { timingOk = true; }
                    }
                }
                if (!timingOk) continue;

                bool conditionsOk = true;
                foreach (var tag in data.tags)
                {
                    if (tag == null) continue;
                    if (tag.category != JokerTagCategory.Condition) continue;
                    switch (tag.conditionType)
                    {
                        case JokerConditionType.None:
                            Debug.LogWarning($"[JokerManager][Validate] {data.jokerName}: Condition has None");
                            break;
                        case JokerConditionType.OutcomeIs:
                            if (context.outcome == Outcome.None)
                                Debug.LogWarning($"[JokerManager][Validate] {data.jokerName}: OutcomeIs with Outcome.None");
                            if (context.outcome != tag.outcomeParam) conditionsOk = false;
                            break;
                        case JokerConditionType.PlayerChoiceIs:
                            if (context.playerChoice == Choice.None)
                                Debug.LogWarning($"[JokerManager][Validate] {data.jokerName}: PlayerChoiceIs with Choice.None");
                            if (context.playerChoice != tag.choiceParam) conditionsOk = false;
                            break;
                        default:
                            Debug.LogWarning($"[JokerManager][Validate] {data.jokerName}: Unknown condition type {tag.conditionType}");
                            break;
                    }
                    if (!conditionsOk) break;
                }
                if (!conditionsOk) continue;

                foreach (var eff in data.tags)
                {
                    if (eff == null || eff.category != JokerTagCategory.Effect) continue;

                    if (eff.effectType == JokerEffectType.None)
                    {
                        Debug.LogWarning($"[JokerManager][Validate] {data.jokerName}: Effect has None");
                        continue;
                    }

                    if (eff.filterByOutcome && eff.outcomeParam == Outcome.None)
                        Debug.LogWarning($"[JokerManager][Validate] {data.jokerName}: filterByOutcome=true with Outcome.None");
                    if (eff.filterByChoice && eff.choiceParam == Choice.None)
                        Debug.LogWarning($"[JokerManager][Validate] {data.jokerName}: filterByChoice=true with Choice.None");

                    if (eff.filterByOutcome && context.outcome != eff.outcomeParam) continue;
                    if (eff.filterByChoice && context.playerChoice != eff.choiceParam) continue;

                    switch (eff.effectType)
                    {
                        case JokerEffectType.AddScoreDelta:
                            context.scoreDelta += eff.intValue;
                            break;
                        case JokerEffectType.ShowInfo:
                            if (context.gameManager != null)
                            {
                                string msg = string.IsNullOrEmpty(eff.stringValue) ? $"Joker: {data.jokerName}" : eff.stringValue;
                                context.gameManager.ShowInfo(msg);
                            }
                            break;
                        case JokerEffectType.ForceAIDrawFromFront:
                            // 결산 타이밍에서는 무시
                            break;
                        default:
                            Debug.LogWarning($"[JokerManager][Validate] {data.jokerName}: Unknown effect type {eff.effectType}");
                            break;
                    }
                }
            }
        }

        private void EvaluateRoundStartTags(GameManager gm, bool applyDrawPolicy)
        {
            if (_order.Count == 0) return;
            for (int i = 0; i < _order.Count; i++)
            {
                var data = _order[i];
                if (data == null || data.tags == null) continue;

                bool timingOk = false;
                foreach (var tag in data.tags)
                {
                    if (tag == null) continue;
                    if (tag.category == JokerTagCategory.Timing)
                    {
                        if (tag.timingType == JokerTimingType.None)
                            Debug.LogWarning($"[JokerManager][Validate] {data.jokerName}: Timing tag has None");
                        if (tag.timingType == JokerTimingType.RoundStart) { timingOk = true; }
                    }
                }
                if (!timingOk) continue;

                foreach (var eff in data.tags)
                {
                    if (eff == null || eff.category != JokerTagCategory.Effect) continue;

                    switch (eff.effectType)
                    {
                        case JokerEffectType.ShowInfo:
                            if (gm != null)
                            {
                                var first = gm.PeekAIFront();
                                var last = gm.PeekAIBack();
                                string msg = string.IsNullOrEmpty(eff.stringValue)
                                    ? $"Joker: {data.jokerName} - Opponent First: {first}, Last: {last}"
                                    : eff.stringValue;
                                gm.ShowInfo(msg);
                            }
                            break;
                        case JokerEffectType.ForceAIDrawFromFront:
                            if (applyDrawPolicy && eff.intValue != 0)
                            {
                                _drawFromFrontThisRound = true;
                            }
                            break;
                        case JokerEffectType.None:
                            Debug.LogWarning($"[JokerManager][Validate] {data.jokerName}: Effect has None");
                            break;
                        default:
                            // RoundStart에서 처리하지 않는 다른 효과 유형은 무시
                            break;
                    }
                }
            }
        }

        // 태그 유효성 검사 및 경고 로그 출력
        private void ValidateAndWarn(JokerData data)
        {
            if (data == null) return;
            if (data.tags == null || data.tags.Count == 0)
            {
                RPS.RPSLog.Warn("Validate", $"{data.jokerName}: No tags defined");
                Debug.LogWarning($"[JokerManager][Validate] {data.jokerName}: No tags defined");
                return;
            }
            foreach (var t in data.tags)
            {
                if (t == null)
                {
                    RPS.RPSLog.Warn("Validate", $"{data.jokerName}: Null tag entry");
                    Debug.LogWarning($"[JokerManager][Validate] {data.jokerName}: Null tag entry");
                    continue;
                }
                switch (t.category)
                {
                    case JokerTagCategory.Timing:
                        if (t.timingType == JokerTimingType.None)
                        {
                            RPS.RPSLog.Warn("Validate", $"{data.jokerName}: Timing tag with None");
                            Debug.LogWarning($"[JokerManager][Validate] {data.jokerName}: Timing tag with None");
                        }
                        break;
                    case JokerTagCategory.Condition:
                        if (t.conditionType == JokerConditionType.None)
                        {
                            RPS.RPSLog.Warn("Validate", $"{data.jokerName}: Condition tag with None");
                            Debug.LogWarning($"[JokerManager][Validate] {data.jokerName}: Condition tag with None");
                        }
                        if (t.conditionType == JokerConditionType.OutcomeIs && t.outcomeParam == Outcome.None)
                        {
                            RPS.RPSLog.Warn("Validate", $"{data.jokerName}: OutcomeIs with Outcome.None");
                            Debug.LogWarning($"[JokerManager][Validate] {data.jokerName}: OutcomeIs with Outcome.None");
                        }
                        if (t.conditionType == JokerConditionType.PlayerChoiceIs && t.choiceParam == Choice.None)
                        {
                            RPS.RPSLog.Warn("Validate", $"{data.jokerName}: PlayerChoiceIs with Choice.None");
                            Debug.LogWarning($"[JokerManager][Validate] {data.jokerName}: PlayerChoiceIs with Choice.None");
                        }
                        break;
                    case JokerTagCategory.Effect:
                        if (t.effectType == JokerEffectType.None)
                        {
                            RPS.RPSLog.Warn("Validate", $"{data.jokerName}: Effect tag with None");
                            Debug.LogWarning($"[JokerManager][Validate] {data.jokerName}: Effect tag with None");
                        }
                        if (t.filterByOutcome && t.outcomeParam == Outcome.None)
                        {
                            RPS.RPSLog.Warn("Validate", $"{data.jokerName}: Effect filterByOutcome with Outcome.None");
                            Debug.LogWarning($"[JokerManager][Validate] {data.jokerName}: Effect filterByOutcome with Outcome.None");
                        }
                        if (t.filterByChoice && t.choiceParam == Choice.None)
                        {
                            RPS.RPSLog.Warn("Validate", $"{data.jokerName}: Effect filterByChoice with Choice.None");
                            Debug.LogWarning($"[JokerManager][Validate] {data.jokerName}: Effect filterByChoice with Choice.None");
                        }
                        break;
                }
            }
        }
    }
}
