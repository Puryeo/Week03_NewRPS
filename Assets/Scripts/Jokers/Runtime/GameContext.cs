using System.Collections.Generic;
using UnityEngine;

namespace Jokers
{
    // Context passed to JokerManager for tag evaluation at turn settlement
    public class GameContext
    {
        // References
        public GameManager gameManager;

        // Current turn data
        public Choice playerChoice;
        public Outcome outcome;
        public int baseScore;
        public int currentTotal;
        public int scoreDelta; // initialize with baseScore

        // Turn meta
        public int turnIndex;     // 1-based index for current turn
        public int turnsPlanned;  // configured turns to play
        public bool isLastTurn;   // true when this is the final turn of the round

        // Round history (including this turn when context is built)
        public List<Choice> playerHistory = new List<Choice>();
        public List<Outcome> outcomeHistory = new List<Outcome>();
    }
}
