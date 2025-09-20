using System;
using UnityEngine;
using TMPro;

// Simple UI helper that subscribes to GameManager.OnRoundEnded and displays a summary
public class RoundEndUI : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TextMeshProUGUI summaryText;

    private void Reset()
    {
        if (gameManager == null) gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        if (summaryText == null) summaryText = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        if (gameManager == null) gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnRoundEnded += HandleRoundEnded;
        }
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.OnRoundEnded -= HandleRoundEnded;
        }
    }

    private void HandleRoundEnded(RoundResult result)
    {
        if (summaryText == null) return;
        summaryText.text = $"Round Summary\nScore: {result.totalScore}\nW/D/L: {result.wins}/{result.draws}/{result.losses}\nTurns: {result.turnsPlayed}/{result.turnsPlanned}\nRerolls Used: {result.rerollsUsed}";
    }
}
