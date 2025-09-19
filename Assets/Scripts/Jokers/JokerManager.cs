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
                            if (context.outcome != tag.outcomeParam) conditionsOk = false;
                            break;
                        case JokerConditionType.PlayerChoiceIs:
                            if (context.playerChoice != tag.choiceParam) conditionsOk = false;
                            break;
                        case JokerConditionType.PlayedAtLeastCount:
                            {
                                if (tag.choiceParam == Choice.None)
                                    Debug.LogWarning($"[JokerManager][Validate] {data.jokerName}: PlayedAtLeastCount requires choiceParam");
                                int cnt = 0;
                                var hist = context.playerHistory;
                                if (hist != null)
                                {
                                    for (int hi = 0; hi < hist.Count; hi++) if (hist[hi] == tag.choiceParam) cnt++;
                                }
                                if (cnt < tag.intValue) conditionsOk = false;
                            }
                            break;
                        case JokerConditionType.ConsecutiveDrawWithChoiceIs:
                            {
                                if (tag.choiceParam == Choice.None)
                                    Debug.LogWarning($"[JokerManager][Validate] {data.jokerName}: ConsecutiveDrawWithChoiceIs requires choiceParam");
                                var ph = context.playerHistory;
                                var oh = context.outcomeHistory;
                                int needed = tag.intValue > 0 ? tag.intValue : 1;
                                bool ok = true;
                                if (ph == null || oh == null || ph.Count != oh.Count || ph.Count < needed)
                                {
                                    ok = false;
                                }
                                else
                                {
                                    for (int k = 0; k < needed; k++)
                                    {
                                        int idx = ph.Count - 1 - k;
                                        if (idx < 0 || ph[idx] != tag.choiceParam || oh[idx] != Outcome.Draw) { ok = false; break; }
                                    }
                                }
                                if (!ok) conditionsOk = false;
                            }
                            break;
                        case JokerConditionType.IsLastTurn:
                            {
                                bool expect = (tag.intValue != 0);
                                if (context.isLastTurn != expect) conditionsOk = false;
                            }
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
                        case JokerEffectType.FinalScoreMultiplier:
                            // 조건 충족 시 즉시 적용: 이번 턴 점수Delta에 배수 적용
                            if (eff.intValue > 1)
                            {
                                context.scoreDelta *= eff.intValue;
                            }
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

                // RoundStart 전역 조건 검사 (현재는 핸드 비교 조건만 지원)
                bool condOk = true;
                foreach (var c in data.tags)
                {
                    if (c == null || c.category != JokerTagCategory.Condition) continue;
                    switch (c.conditionType)
                    {
                        case JokerConditionType.PlayerHasMoreOfChoiceThanAI:
                            if (gm == null)
                            { condOk = false; break; }
                            // GameManager에 헬퍼가 없으면 기본 비교 구현 필요
                            int playerCount = gm.CountPlayerChoice(c.choiceParam);
                            int aiCount = gm.CountAIChoice(c.choiceParam);
                            if (!(playerCount > aiCount)) condOk = false;
                            break;
                        case JokerConditionType.IsLastTurn:
                        case JokerConditionType.PlayedAtLeastCount:
                        case JokerConditionType.ConsecutiveDrawWithChoiceIs:
                        case JokerConditionType.OutcomeIs:
                        case JokerConditionType.PlayerChoiceIs:
                            // RoundStart에서는 적용 대상 아님 -> 무시
                            break;
                        case JokerConditionType.None:
                            Debug.LogWarning($"[JokerManager][Validate] {data.jokerName}: Condition has None");
                            break;
                        default:
                            Debug.LogWarning($"[JokerManager][Validate] {data.jokerName}: Unknown RoundStart condition {c.conditionType}");
                            break;
                    }
                    if (!condOk) break;
                }
                if (!condOk) continue;

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
                        case JokerEffectType.ReplaceAIRandomCardsToChoice:
                            if (gm != null && eff.intValue > 0 && eff.choiceParam != Choice.None)
                            {
                                int changed = gm.ReplaceAIRandomCardsTo(eff.choiceParam, eff.intValue);
                                RPS.RPSLog.Event("Joker", "AIHandMutate", $"name={data.jokerName}, to={eff.choiceParam}, count={changed}");
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
