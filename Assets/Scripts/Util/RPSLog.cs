using UnityEngine;

namespace RPS
{
    // Centralized logging utility for consistent, parseable logs
    // Format examples:
    // [RPS][Round] Start | aiSize=6, playerSize=6, turns=5, ai=R2P2S2, player=R2P2S2, rerolls=2
    // [RPS][Turn] Resolve | turn=1, player=Rock, ai=Scissors, outcome=Win, delta=20, total=20, aiRemain=R2P2S1, pRemain=R1P2S2
    // [RPS][Joker] Toggle | action=Enabled, name=Scout
    public static class RPSLog
    {
        public static void Info(string source, string message)
        {
            Debug.Log($"[RPS][{source}] {message}");
        }

        public static void Warn(string source, string message)
        {
            Debug.LogWarning($"[RPS][{source}] {message}");
        }

        public static void Error(string source, string message)
        {
            Debug.LogError($"[RPS][{source}] {message}");
        }

        public static void Event(string category, string name, string kvPairs)
        {
            Debug.Log($"[RPS][{category}] {name} | {kvPairs}");
        }
    }
}
