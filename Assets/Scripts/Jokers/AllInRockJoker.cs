using UnityEngine;

namespace Jokers
{
    public class AllInRockJoker : IJoker
    {
        public string Name => "All-In: Rock";

        public void OnRoundStart(GameManager gameManager)
        {
            if (gameManager != null)
            {
                gameManager.ShowInfo("Joker Active: All-In: Rock - Rock Win +15, Rock Draw/Loss -9999 (additive)");
            }
        }

        // additive: 입력 score에 더한 누적값을 반환
        public int ApplyScoreModification(int score, ref int currentTotalScore, Choice playerChoice, Outcome outcome)
        {
            if (playerChoice != Choice.Rock) return score; // 적용 대상 Rock만

            switch (outcome)
            {
                case Outcome.Win:
                    return score + 15; // 기본 5와 합쳐 20
                case Outcome.Draw:
                case Outcome.Loss:
                    return score - 9999; // 큰 페널티
                default:
                    return score;
            }
        }
    }
}
