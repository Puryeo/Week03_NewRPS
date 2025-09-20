// 조커 시스템에서 사용되는 태그 열거형 정의
// 이 파일은 태그 기반 데이터 주도 조커 구조의 핵심 분류를 담는다.
// 각 값은 태그가 언제 작동하는지(시점), 어떤 조건에서 작동하는지(조건), 무엇을 하는지(효과)를 표현한다.

namespace Jokers
{
    // 태그 대분류: 시점(Timing), 조건(Condition), 효과(Effect)
    public enum JokerTagCategory { Timing, Condition, Effect }

    // 시점 태그: 라운드시작, 턴결산 등 실행 타이밍 지정
    // None을 두어 인스펙터에서 사용하지 않는 값을 명시적으로 선택할 수 있도록 한다.
    public enum JokerTimingType { None, RoundStart, TurnSettlement, RoundPrepare, TurnStart }

    // 조건 태그: 결과/선택 등에 기반한 발동 제한
    public enum JokerConditionType
    {
        None,
        OutcomeIs,
        PlayerChoiceIs,
        // 라운드 히스토리/턴 메타 기반 확장 조건
        PlayedAtLeastCount,          // choiceParam를 라운드에서 intValue 이상 사용
        ConsecutiveDrawWithChoiceIs, // choiceParam로 연속 intValue번 비김 (히스토리 끝 기준)
        IsLastTurn,                  // 마지막 턴 여부 (intValue==1일 때 true)
        // 핸드 비교 기반 조건
        PlayerHasMoreOfChoiceThanAI, // RoundStart 등에서 사용: choiceParam의 보유량 Player > AI
        // Phase B additions
        TurnIndexIs,                 // 턴 인덱스가 일치하는지 여부 (1-based)
        ConsecutiveOutcomeWithChoiceIs, // 연속으로 outcomeParam과 choiceParam이 intValue 회 일치하는지 여부
        RerollUsedEquals,            // 사용된 리롤 수가 intValue와 같은지 여부
        // Phase C additions
        PlayerHasAtLeastCountInHand  // 플레이어 손패에 choiceParam 카드가 intValue 이상 보유
    }

    // 효과 태그: 점수 가산, 정보 출력, AI 드로우 정책 강제 등 구체 효과 지정
    public enum JokerEffectType
    {
        None,
        AddScoreDelta,
        ForceAIDrawFromFront,
        ShowInfo,
        // 라운드 종료 시 최종 점수 배수 적용(조건 만족 시 즉시 적용)
        FinalScoreMultiplier,
        // AI 패 수정: 무작위 count장을 choiceParam으로 변경
        ReplaceAIRandomCardsToChoice,
        // Phase B addition
        RevealNextAICard,
        // Phase C additions
        ModifyTurnsToPlayDelta,      // 라운드 시작 전 턴 수 변경(델타)
        AddCardsToPlayerHand,        // 플레이어 손패에 카드 추가
        AddCardsToAIHand,            // AI 손패에 카드 추가
        AddScorePerPlayerHandCount   // TurnStart: 플레이어 손패 내 choiceParam 1장당 intValue 점수 가산
    }

    // 스폰/디자인 분류용 메타 태그(아키타입). 조커 데이터에 부착하여 분류/필터링에 활용한다.
    // 플래그 조합 가능: Anchor | Payoff 등
    [System.Flags]
    public enum JokerArchetype
    {
        None = 0,
        Anchor = 1 << 0,
        Payoff = 1 << 1,
        Catalyst = 1 << 2,
        Utility = 1 << 3,
        Risk = 1 << 4,
    }
}
