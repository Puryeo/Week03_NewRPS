using UnityEngine;

namespace Jokers
{
    // Minimum context passed to JokerManager for tag evaluation at turn settlement
    public class GameContext
    {
        public GameManager gameManager;
        public Choice playerChoice;
        public Outcome outcome;
        public int baseScore;
        public int currentTotal;
        public int scoreDelta; // initialized with baseScore
    }
}
