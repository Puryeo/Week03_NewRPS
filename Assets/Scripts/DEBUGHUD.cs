using UnityEngine;
using TMPro;
using Jokers;

// ����� HUD: ��Ŀ ��� �� ���������� ǥ��(�±�/������ ��� ����)
public class DEBUGHUD : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private JokerManager jokerManager;
    [SerializeField] private GameManager gameManager;

    [Header("UI Texts")]
    [SerializeField] private TextMeshProUGUI CurrentText;   // ���� �ֿ켱 ��Ŀ �̸�
    [SerializeField] private TextMeshProUGUI PipelineText;  // Ȱ�� ��Ŀ ����������

    [Header("JokerData (assign in Inspector)")]
    [SerializeField] private JokerData allInRock;
    [SerializeField] private JokerData theContrarian;
    [SerializeField] private JokerData scout;

    // ��ư ���ε�: ��Ŀ ���
    public void ToggleNone() { jokerManager?.ToggleJoker(null); jokerManager?.OnJokerToggled(gameManager); }
    public void ToggleAllInRock() { if (allInRock != null) { jokerManager?.ToggleJoker(allInRock); jokerManager?.OnJokerToggled(gameManager); } }
    public void ToggleContrarian() { if (theContrarian != null) { jokerManager?.ToggleJoker(theContrarian); jokerManager?.OnJokerToggled(gameManager); } }
    public void ToggleScout() { if (scout != null) { jokerManager?.ToggleJoker(scout); jokerManager?.OnJokerToggled(gameManager); } }

    private void Update()
    {
        if (jokerManager == null) return;
        if (CurrentText != null) CurrentText.text = $"Current: {jokerManager.GetCurrentJokerName()}";
        if (PipelineText != null) PipelineText.text = $"Pipeline: {jokerManager.GetPipelineDescription()}";
    }
}
