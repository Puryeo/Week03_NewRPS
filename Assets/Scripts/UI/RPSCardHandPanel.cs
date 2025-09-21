using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Renders counts of Rock/Paper/Scissors as overlapping cards using a Horizontal Layout Group.
// Create two panels in GamePanel: PlayerHandPanel and AIHandPanel, each with a Horizontal Layout Group.
// Assign this component to each and call Refresh with current counts.
public class RPSCardHandPanel : MonoBehaviour
{
    [Header("Config")]
    public bool forPlayer = true;
    public GameManager gameManager;

    [Header("Layout")]
    public HorizontalLayoutGroup hLayout;
    public GameObject cardPrefab; // prefab with Image + RPSCardView + TMP label
    [Tooltip("Extra negative spacing to visually overlap cards (e.g., -20)")]
    public int overlapSpacing = -20;

    private readonly List<GameObject> _spawned = new List<GameObject>();

    private void Reset()
    {
        if (hLayout == null) hLayout = GetComponent<HorizontalLayoutGroup>();
    }

    private void Awake()
    {
        if (hLayout == null) hLayout = GetComponent<HorizontalLayoutGroup>();
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        if (hLayout != null) hLayout.spacing = overlapSpacing;
    }

    // Call this to rebuild cards from counts
    public void Refresh(int rockCount, int paperCount, int scissorsCount)
    {
        Clear();
        Spawn(Choice.Rock, rockCount);
        Spawn(Choice.Paper, paperCount);
        Spawn(Choice.Scissors, scissorsCount);
    }

    private void Spawn(Choice c, int count)
    {
        if (count <= 0 || cardPrefab == null) return;
        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(cardPrefab, transform);
            var view = go.GetComponent<RPSCardView>();
            if (view == null) view = go.AddComponent<RPSCardView>();
            view.Bind(c, forPlayer, gameManager);
            // Player side: interactable true; AI side: false
            view.interactable = forPlayer;
        }
    }

    private void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
        _spawned.Clear();
    }
}
