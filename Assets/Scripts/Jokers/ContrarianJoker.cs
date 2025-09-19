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

        // additive: �Է� score�� ���� �������� ��ȯ
        public int ApplyScoreModification(int score, ref int currentTotalScore, Choice playerChoice, Outcome outcome)
        {
            switch (outcome)
            {
                case Outcome.Win:
                    return score - 5; // �⺻ 5�� ����� 0
                case Outcome.Draw:
                    return score - 1; // �⺻ 3�� ���� 2
                case Outcome.Loss:
                    return score + 8; // �⺻ 0�� ���� 8
                default:
                    return score;
            }
        }
    }
}
