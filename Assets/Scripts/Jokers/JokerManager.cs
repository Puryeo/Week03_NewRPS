using System.Collections.Generic;
using UnityEngine;

namespace Jokers
{
    // Tag-based Joker system manager
    public partial class JokerManager : MonoBehaviour
    {
        [SerializeField] private List<JokerData> _activeData = new List<JokerData>();
        private readonly List<JokerData> _order = new List<JokerData>();

        private bool _drawFromFrontThisRound = false;   // 라운드 전체에 대한 앞에서부터 드로우 정책(Scout 등)
        private bool _forceNextFrontDrawOnce = false;   // 다음 1회의 드로우만 앞에서부터 강제(RevealNextAICard 신뢰성 확보)
        private bool _allowRoundStartHandMutationReapplyOnce = false; // RoundStart 손패 변형을 토글 시 1회만 재적용 허용(리롤용)
        public bool AIDrawFromFront => _drawFromFrontThisRound; // 기존 공개 프로퍼티(라운드 정책)

        // 다음 AI 드로우가 앞에서부터 진행되어야 하는지 판단하고, 일회성 플래그는 소비하며 해제한다.
        public bool ShouldDrawAIFromFront()
        {
            if (_drawFromFrontThisRound) return true;
            if (_forceNextFrontDrawOnce)
            {
                _forceNextFrontDrawOnce = false; // 일회성 소비
                return true;
            }
            return false;
        }
        // 다음 1회 드로우를 앞에서부터 진행하도록 설정(Geological_Survey 등의 Reveal 효과와 정합성 보장)
        public void ForceNextFrontDrawOnce()
        {
            _forceNextFrontDrawOnce = true;
        }

        // 외부에서 리롤 직후 RoundStart형 손패 변형을 1회 재적용하도록 허가
        public void AllowRoundStartHandMutationReapplyOnce()
        {
            _allowRoundStartHandMutationReapplyOnce = true;
        }

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
            Debug.Log($"[JokerManager] SetJoker: {(data != null ? data.jokerName : "None")} ");
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
            _drawFromFrontThisRound = false;      // 라운드 정책 초기화
            _forceNextFrontDrawOnce = false;      // 일회성 플래그 초기화
            _allowRoundStartHandMutationReapplyOnce = false; // 재적용 허가 플래그 초기화
            // validate
            for (int i = 0; i < _order.Count; i++)
            {
                var d = _order[i];
                if (d != null) ValidateAndWarn(d);
            }
            EvaluateRoundStartTags(gm, applyDrawPolicy: true);
        }

        // Re-evaluate RoundStart effects without resetting draw policy (used on reroll/toggle)
        public void OnJokerToggled(GameManager gm)
        {
            EvaluateRoundStartTags(gm, applyDrawPolicy: false);
        }

        // Phase C: RoundPrepare hook - called after hand generation and before OnRoundStart
        public void OnRoundPrepare(GameManager gm)
        {
            if (gm == null || _order.Count == 0) return;

            // 턴 수 변형은 누적 패스로 계산하여, Reroll 재평가 시 중복 가산되지 않도록 한다.
            gm.BeginPreparePass(reapply: false);

            for (int i = 0; i < _order.Count; i++)
            {
                var data = _order[i];
                if (data == null || data.tags == null) continue;

                bool timingOk = false;
                foreach (var tag in data.tags)
                {
                    if (tag == null) continue;
                    if (tag.category == JokerTagCategory.Timing && tag.timingType == JokerTimingType.RoundPrepare)
                    { timingOk = true; break; }
                }
                if (!timingOk) continue;

                bool condOk = true;
                foreach (var c in data.tags)
                {
                    if (c == null || c.category != JokerTagCategory.Condition) continue;
                    switch (c.conditionType)
                    {
                        case JokerConditionType.PlayerHasMoreOfChoiceThanAI:
                            {
                                int playerCount = gm.CountPlayerChoice(c.choiceParam);
                                int aiCount = gm.CountAIChoice(c.choiceParam);
                                if (!(playerCount > aiCount)) condOk = false;
                            }
                            break;
                        case JokerConditionType.PlayerHasAtLeastCountInHand:
                            {
                                int playerCount = gm.CountPlayerChoice(c.choiceParam);
                                if (playerCount < c.intValue) condOk = false;
                            }
                            break;
                        case JokerConditionType.IsLastTurn:
                        case JokerConditionType.PlayedAtLeastCount:
                        case JokerConditionType.ConsecutiveDrawWithChoiceIs:
                        case JokerConditionType.OutcomeIs:
                        case JokerConditionType.PlayerChoiceIs:
                        case JokerConditionType.TurnIndexIs:
                        case JokerConditionType.ConsecutiveOutcomeWithChoiceIs:
                        case JokerConditionType.RerollUsedEquals:
                        case JokerConditionType.None:
                            // Not applicable or ignored at Prepare
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
                        case JokerEffectType.ModifyTurnsToPlayDelta:
                            if (eff.intValue != 0)
                            {
                                gm.AccumulatePrepareTurnsDelta(eff.intValue);
                                RPS.RPSLog.Event("TurnsMut", "PrepareEval", $"name={data.jokerName}, delta={eff.intValue}");
                            }
                            break;
                        case JokerEffectType.AddCardsToPlayerHand:
                            if (eff.intValue > 0 && eff.choiceParam != Choice.None)
                            {
                                gm.AddCardsToPlayerHand(eff.choiceParam, eff.intValue);
                                RPS.RPSLog.Event("HandMut", "Prepare", $"name={data.jokerName}, target=Player, add={eff.intValue}, choice={eff.choiceParam}");
                            }
                            break;
                        case JokerEffectType.AddCardsToAIHand:
                            if (eff.intValue > 0 && eff.choiceParam != Choice.None)
                            {
                                gm.AddCardsToAIHand(eff.choiceParam, eff.intValue);
                                RPS.RPSLog.Event("HandMut", "Prepare", $"name={data.jokerName}, target=AI, add={eff.intValue}, choice={eff.choiceParam}");
                            }
                            break;
                        case JokerEffectType.ShowInfo:
                            if (gm != null)
                            {
                                string msg = string.IsNullOrEmpty(eff.stringValue) ? $"Joker Prepare: {data.jokerName}" : eff.stringValue;
                                gm.ShowInfo(msg);
                            }
                            break;
                        default:
                            // ignore at Prepare
                            break;
                    }
                }
            }

            // 한 번에 커밋하여 UI와 상태 반영, applied 누적값 기록
            gm.CommitPreparePass();
        }

        // Reroll 이후 RoundPrepare 재평가: 턴수 변형만 재적용. 카드 추가/치환은 재적용하지 않음(중복 방지)
        public void OnRoundPrepareReapply(GameManager gm)
        {
            if (gm == null || _order.Count == 0) return;
            gm.BeginPreparePass(reapply: true);

            for (int i = 0; i < _order.Count; i++)
            {
                var data = _order[i];
                if (data == null || data.tags == null) continue;

                bool timingOk = false;
                foreach (var tag in data.tags)
                {
                    if (tag == null) continue;
                    if (tag.category == JokerTagCategory.Timing && tag.timingType == JokerTimingType.RoundPrepare)
                    { timingOk = true; break; }
                }
                if (!timingOk) continue;

                bool condOk = true;
                foreach (var c in data.tags)
                {
                    if (c == null || c.category != JokerTagCategory.Condition) continue;
                    switch (c.conditionType)
                    {
                        case JokerConditionType.PlayerHasAtLeastCountInHand:
                            {
                                int playerCount = gm.CountPlayerChoice(c.choiceParam);
                                if (playerCount < c.intValue) condOk = false;
                            }
                            break;
                        default:
                            // 재평가 대상 아님
                            break;
                    }
                    if (!condOk) break;
                }
                if (!condOk) continue;

                foreach (var eff in data.tags)
                {
                    if (eff == null || eff.category != JokerTagCategory.Effect) continue;
                    if (eff.effectType == JokerEffectType.ModifyTurnsToPlayDelta && eff.intValue != 0)
                    {
                        gm.AccumulatePrepareTurnsDelta(eff.intValue);
                    }
                }
            }

            gm.CommitPreparePass();
        }

        public int ExecuteAndReturnTurnDelta(GameContext context)
        {
            if (context == null) return 0;
            ExecuteTurnSettlementEffects(context);
            return context.scoreDelta;
        }

        // Phase C: TurnStart hook - called before outcome is determined
        public void OnTurnStart(GameContext context)
        {
            if (context == null || _order.Count == 0) return;

            for (int i = 0; i < _order.Count; i++)
            {
                var data = _order[i];
                if (data == null || data.tags == null) continue;

                bool timingOk = false;
                foreach (var tag in data.tags)
                {
                    if (tag == null) continue;
                    if (tag.category == JokerTagCategory.Timing && tag.timingType == JokerTimingType.TurnStart)
                    { timingOk = true; break; }
                }
                if (!timingOk) continue;

                bool conditionsOk = true;
                foreach (var tag in data.tags)
                {
                    if (tag == null || tag.category != JokerTagCategory.Condition) continue;

                    switch (tag.conditionType)
                    {
                        case JokerConditionType.PlayerHasAtLeastCountInHand:
                            {
                                if (context.gameManager == null) { conditionsOk = false; break; }
                                int cnt = context.gameManager.CountPlayerChoice(tag.choiceParam);
                                if (cnt < tag.intValue) conditionsOk = false;
                            }
                            break;
                        case JokerConditionType.TurnIndexIs:
                            if (context.turnIndex != tag.intValue) conditionsOk = false;
                            break;
                        case JokerConditionType.RerollUsedEquals:
                            if (context.rerollsUsed != tag.intValue) conditionsOk = false;
                            break;
                        case JokerConditionType.OutcomeIs:
                        case JokerConditionType.PlayerChoiceIs:
                        case JokerConditionType.PlayedAtLeastCount:
                        case JokerConditionType.ConsecutiveDrawWithChoiceIs:
                        case JokerConditionType.IsLastTurn:
                        case JokerConditionType.ConsecutiveOutcomeWithChoiceIs:
                        case JokerConditionType.PlayerHasMoreOfChoiceThanAI:
                        case JokerConditionType.None:
                            // Ignore or not applicable at TurnStart
                            break;
                    }
                    if (!conditionsOk) break;
                }
                if (!conditionsOk) continue;

                foreach (var eff in data.tags)
                {
                    if (eff == null || eff.category != JokerTagCategory.Effect) continue;

                    switch (eff.effectType)
                    {
                        case JokerEffectType.AddScorePerPlayerHandCount:
                            if (context.gameManager != null && eff.intValue != 0 && eff.choiceParam != Choice.None)
                            {
                                int cnt = context.gameManager.CountPlayerChoice(eff.choiceParam);
                                int add = eff.intValue * cnt;
                                context.scoreDelta += add;
                            }
                            break;
                        case JokerEffectType.ShowInfo:
                            if (context.gameManager != null)
                            {
                                string msg = string.IsNullOrEmpty(eff.stringValue) ? $"Joker TurnStart: {data.jokerName}" : eff.stringValue;
                                context.gameManager.ShowInfo(msg);
                            }
                            break;
                        default:
                            // ignore other effects at TurnStart
                            break;
                    }
                }
            }
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

                // OR semantics for multiple TurnIndexIs tags: if any present, at least one must match
                int turnIndexCondCount = 0; bool turnIndexAnyMatch = false;
                for (int ti = 0; ti < data.tags.Count; ti++)
                {
                    var t = data.tags[ti];
                    if (t == null || t.category != JokerTagCategory.Condition) continue;
                    if (t.conditionType == JokerConditionType.TurnIndexIs)
                    {
                        turnIndexCondCount++;
                        if (context.turnIndex == t.intValue) turnIndexAnyMatch = true;
                    }
                }

                bool conditionsOk = true;
                if (turnIndexCondCount > 0 && !turnIndexAnyMatch)
                {
                    conditionsOk = false;
                }

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
                                int cnt = 0; var hist = context.playerHistory;
                                if (hist != null)
                                { for (int hi = 0; hi < hist.Count; hi++) if (hist[hi] == tag.choiceParam) cnt++; }
                                if (cnt < tag.intValue) conditionsOk = false;
                            }
                            break;
                        case JokerConditionType.ConsecutiveDrawWithChoiceIs:
                            {
                                if (tag.choiceParam == Choice.None)
                                    Debug.LogWarning($"[JokerManager][Validate] {data.jokerName}: ConsecutiveDrawWithChoiceIs requires choiceParam");
                                var ph = context.playerHistory; var oh = context.outcomeHistory;
                                int needed = Mathf.Max(1, tag.intValue); bool ok = true;
                                if (ph == null || oh == null || ph.Count != oh.Count || ph.Count < needed) ok = false;
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
                            if (context.isLastTurn != (tag.intValue != 0)) conditionsOk = false;
                            break;
                        // Skip per-tag TurnIndexIs check because handled with OR above
                        case JokerConditionType.TurnIndexIs:
                            break;
                        case JokerConditionType.ConsecutiveOutcomeWithChoiceIs:
                            {
                                var ph = context.playerHistory; var oh = context.outcomeHistory;
                                int needed = Mathf.Max(1, tag.intValue); bool ok = true;
                                if (ph == null || oh == null || ph.Count != oh.Count || ph.Count < needed) ok = false;
                                else
                                {
                                    Outcome wantO = tag.outcomeParam; Choice wantC = tag.choiceParam;
                                    for (int k = 0; k < needed; k++)
                                    {
                                        int idx = ph.Count - 1 - k;
                                        if (idx < 0 || ph[idx] != wantC || oh[idx] != wantO) { ok = false; break; }
                                    }
                                }
                                if (!ok) conditionsOk = false;
                            }
                            break;
                        case JokerConditionType.RerollUsedEquals:
                            if (context.rerollsUsed != tag.intValue) conditionsOk = false;
                            break;
                        case JokerConditionType.PlayerHasAtLeastCountInHand:
                            {
                                if (context.gameManager == null) { conditionsOk = false; break; }
                                int cnt = context.gameManager.CountPlayerChoice(tag.choiceParam);
                                if (cnt < tag.intValue) conditionsOk = false;
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
                            break;
                        case JokerEffectType.FinalScoreMultiplier:
                            {
                                int mul = eff.intValue; if (mul > 1)
                                { int before = context.currentTotal; int after = (before + context.scoreDelta) * mul; context.scoreDelta = after - before; }
                            }
                            break;
                        case JokerEffectType.RevealNextAICard:
                            {
                                if (context.gameManager != null)
                                {
                                    var peek = context.gameManager.PeekAIFront();
                                    context.gameManager.ShowInfo($"Next AI Card (front): {peek}");
                                }
                                // 다음 턴에서 실제로 앞에서부터 드로우하도록 보증(정보와 실제 불일치 방지)
                                _forceNextFrontDrawOnce = true;
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

                bool condOk = true;
                foreach (var c in data.tags)
                {
                    if (c == null || c.category != JokerTagCategory.Condition) continue;
                    switch (c.conditionType)
                    {
                        case JokerConditionType.PlayerHasMoreOfChoiceThanAI:
                            if (gm == null)
                            { condOk = false; break; }
                            int playerCount = gm.CountPlayerChoice(c.choiceParam);
                            int aiCount = gm.CountAIChoice(c.choiceParam);
                            if (!(playerCount > aiCount)) condOk = false;
                            break;
                        case JokerConditionType.IsLastTurn:
                        case JokerConditionType.PlayedAtLeastCount:
                        case JokerConditionType.ConsecutiveDrawWithChoiceIs:
                        case JokerConditionType.OutcomeIs:
                        case JokerConditionType.PlayerChoiceIs:
                        case JokerConditionType.None:
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
                        case JokerEffectType.ForceAIDrawFromFront:
                            if (applyDrawPolicy && eff.intValue != 0)
                            {
                                _drawFromFrontThisRound = true;
                            }
                            break;
                        case JokerEffectType.ReplaceAIRandomCardsToChoice:
                            // RoundStart 손패 변형은 라운드 시작 시 1회만 실행, 또는 리롤 직후 1회 재적용만 허용
                            if (gm != null && eff.intValue > 0 && eff.choiceParam != Choice.None)
                            {
                                bool canApply = applyDrawPolicy || _allowRoundStartHandMutationReapplyOnce;
                                if (canApply)
                                {
                                    int changed;
                                    if (eff.choiceParam == Choice.Rock)
                                        changed = gm.ReplaceAIPaperOrScissorsToRock(eff.intValue);
                                    else
                                        changed = gm.ReplaceAIRandomCardsTo(eff.choiceParam, eff.intValue);
                                    RPS.RPSLog.Event("Joker", "AIHandMutate", $"name={data.jokerName}, to={eff.choiceParam}, count={changed}, reapply={(applyDrawPolicy?0:1)}");
                                }
                            }
                            break;
                        case JokerEffectType.None:
                            Debug.LogWarning($"[JokerManager][Validate] {data.jokerName}: Effect has None");
                            break;
                        default:
                            break;
                    }
                }
            }
            // 재적용 일회성 플래그는 패스 종료 시 소모
            _allowRoundStartHandMutationReapplyOnce = false;
        }

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
