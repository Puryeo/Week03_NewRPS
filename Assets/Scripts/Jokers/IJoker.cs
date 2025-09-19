using UnityEngine;

namespace Jokers
{
    public interface IJoker
    {
        // 표시용 이름
        string Name { get; }

        // 라운드 시작 시 호출 (필요 시 정보 표시 등)
        void OnRoundStart(GameManager gameManager);

        // 점수 계산 시 호출: baseScore를 조정해 반환. 필요한 경우 currentTotalScore를 ref로 수정 가능(예: 전체 초기화).
        int ApplyScoreModification(int baseScore, ref int currentTotalScore, Choice playerChoice, Outcome outcome);
    }
}
