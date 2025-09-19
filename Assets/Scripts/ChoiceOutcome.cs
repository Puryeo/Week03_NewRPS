// 가위바위보 선택과 결과를 표현하는 공용 열거형
// 인스펙터에서 "None"을 선택할 수 있도록 음수 값(-1)을 추가해 기본 로직에 영향을 주지 않는다.

public enum Choice
{
    None = -1, // 선택 안 함(태그 파라미터 등에서 미사용을 의미)
    Rock = 0,  // 바위
    Paper = 1, // 보
    Scissors = 2 // 가위
}

public enum Outcome
{
    None = -1, // 결과 없음(태그 파라미터 등에서 미사용을 의미)
    Win = 0,   // 승리
    Draw = 1,  // 무승부
    Loss = 2   // 패배
}
