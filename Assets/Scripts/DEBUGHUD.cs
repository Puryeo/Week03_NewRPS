using UnityEngine;
using TMPro;
using Jokers;

// 디버그 HUD: 조커 토글 및 파이프라인 표시(태그/데이터 기반 전용)
public class DEBUGHUD : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private JokerManager jokerManager;
    [SerializeField] private GameManager gameManager;

    [Header("UI Texts")]
    [SerializeField] private TextMeshProUGUI CurrentText;   // 현재 최우선 조커 이름
    [SerializeField] private TextMeshProUGUI PipelineText;  // 활성 조커 파이프라인

    [Header("JokerData (assign in Inspector)")]
    [SerializeField] private JokerData allInRock;
    [SerializeField] private JokerData theContrarian;
    [SerializeField] private JokerData scout;

    // 버튼 바인딩: 조커 토글
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
