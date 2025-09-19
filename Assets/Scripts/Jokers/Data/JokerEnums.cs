// 조커 시스템에서 사용되는 태그 열거형 정의
// 이 파일은 태그 기반 데이터 주도 조커 구조의 핵심 분류를 담는다.
// 각 값은 태그가 언제 작동하는지(시점), 어떤 조건에서 작동하는지(조건), 무엇을 하는지(효과)를 표현한다.

namespace Jokers
{
    // 태그 대분류: 시점(Timing), 조건(Condition), 효과(Effect)
    public enum JokerTagCategory { Timing, Condition, Effect }

    // 시점 태그: 라운드시작, 턴결산 등 실행 타이밍 지정
    // None을 두어 인스펙터에서 사용하지 않는 값을 명시적으로 선택할 수 있도록 한다.
    public enum JokerTimingType { None, RoundStart, TurnSettlement }

    // 조건 태그: 결과/선택 등에 기반한 발동 제한
    public enum JokerConditionType { None, OutcomeIs, PlayerChoiceIs }

    // 효과 태그: 점수 가산, 정보 출력, AI 드로우 정책 강제 등 구체 효과 지정
    public enum JokerEffectType { None, AddScoreDelta, ForceAIDrawFromFront, ShowInfo }
}
