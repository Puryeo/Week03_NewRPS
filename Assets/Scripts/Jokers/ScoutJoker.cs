namespace Jokers
{
    public class ScoutJoker : IJoker
    {
        public string Name => "Scout";

        public void OnRoundStart(GameManager gameManager)
        {
            if (gameManager == null) return;
            var first = gameManager.PeekAIFront();
            var last = gameManager.PeekAIBack();
            gameManager.ShowInfo($"Joker Active: Scout - Opponent First: {first}, Last: {last}");
        }

        public int ApplyScoreModification(int baseScore, ref int currentTotalScore, Choice playerChoice, Outcome outcome)
        {
            return baseScore; // 점수 변경 없음
        }
    }
}
