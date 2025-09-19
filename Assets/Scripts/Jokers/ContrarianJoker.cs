namespace Jokers
{
    public class ContrarianJoker : IJoker
    {
        public string Name => "The Contrarian";

        public void OnRoundStart(GameManager gameManager)
        {
            if (gameManager != null)
            {
                gameManager.ShowInfo("Joker Active: Contrarian - Win -5, Draw -1, Loss +8 (additive)");
            }
        }

        // additive: 입력 score에 더한 누적값을 반환
        public int ApplyScoreModification(int score, ref int currentTotalScore, Choice playerChoice, Outcome outcome)
        {
            switch (outcome)
            {
                case Outcome.Win:
                    return score - 5; // 기본 5를 상쇄해 0
                case Outcome.Draw:
                    return score - 1; // 기본 3과 합쳐 2
                case Outcome.Loss:
                    return score + 8; // 기본 0과 합쳐 8
                default:
                    return score;
            }
        }
    }
}
