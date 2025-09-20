using UnityEngine;
using TMPro;
using Jokers;

// 디버그 HUD: 조커 토글 및 파이프라인 표시(에셋/파이프라인 연결 확인)
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

    // Phase A authored assets (optional wiring for quick tests)
    [SerializeField] private JokerData scissorsCollector;
    [SerializeField] private JokerData bladeOfVengeance;
    [SerializeField] private JokerData glassScissors;
    [SerializeField] private JokerData sturdyBarricade;
    [SerializeField] private JokerData landslide;
    [SerializeField] private JokerData forceOfNature;
    [SerializeField] private JokerData devilsContract;
    [SerializeField] private JokerData paperDominance;

    // 버튼 바인딩: 조커 토글
    public void ToggleNone() { jokerManager?.ToggleJoker(null); jokerManager?.OnJokerToggled(gameManager); }
    public void ToggleAllInRock() { if (allInRock != null) { jokerManager?.ToggleJoker(allInRock); jokerManager?.OnJokerToggled(gameManager); } }
    public void ToggleContrarian() { if (theContrarian != null) { jokerManager?.ToggleJoker(theContrarian); jokerManager?.OnJokerToggled(gameManager); } }
    public void ToggleScout() { if (scout != null) { jokerManager?.ToggleJoker(scout); jokerManager?.OnJokerToggled(gameManager); } }

    // Phase A quick toggles (optional)
    public void ToggleScissorsCollector() { if (scissorsCollector != null) { jokerManager?.ToggleJoker(scissorsCollector); jokerManager?.OnJokerToggled(gameManager); } }
    public void ToggleBladeOfVengeance() { if (bladeOfVengeance != null) { jokerManager?.ToggleJoker(bladeOfVengeance); jokerManager?.OnJokerToggled(gameManager); } }
    public void ToggleGlassScissors() { if (glassScissors != null) { jokerManager?.ToggleJoker(glassScissors); jokerManager?.OnJokerToggled(gameManager); } }
    public void ToggleSturdyBarricade() { if (sturdyBarricade != null) { jokerManager?.ToggleJoker(sturdyBarricade); jokerManager?.OnJokerToggled(gameManager); } }
    public void ToggleLandslide() { if (landslide != null) { jokerManager?.ToggleJoker(landslide); jokerManager?.OnJokerToggled(gameManager); } }
    public void ToggleForceOfNature() { if (forceOfNature != null) { jokerManager?.ToggleJoker(forceOfNature); jokerManager?.OnJokerToggled(gameManager); } }
    public void ToggleDevilsContract() { if (devilsContract != null) { jokerManager?.ToggleJoker(devilsContract); jokerManager?.OnJokerToggled(gameManager); } }
    public void TogglePaperDominance() { if (paperDominance != null) { jokerManager?.ToggleJoker(paperDominance); jokerManager?.OnJokerToggled(gameManager); } }

    private void Update()
    {
        if (jokerManager == null) return;
        if (CurrentText != null) CurrentText.text = $"Current: {jokerManager.GetCurrentJokerName()}";
        if (PipelineText != null) PipelineText.text = $"Pipeline: {jokerManager.GetPipelineDescription()}";
    }
}
