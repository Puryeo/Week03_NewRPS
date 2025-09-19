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

        // additive: �Է� score�� ���� �������� ��ȯ
        public int ApplyScoreModification(int score, ref int currentTotalScore, Choice playerChoice, Outcome outcome)
        {
            if (playerChoice != Choice.Rock) return score; // ���� ��� Rock��

            switch (outcome)
            {
                case Outcome.Win:
                    return score + 15; // �⺻ 5�� ���� 20
                case Outcome.Draw:
                case Outcome.Loss:
                    return score - 9999; // ū ���Ƽ
                default:
                    return score;
            }
        }
    }
}
