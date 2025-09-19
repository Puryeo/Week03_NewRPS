using UnityEngine;
using TMPro;
using Jokers;

public class DEBUGHUD : MonoBehaviour
{
    [SerializeField] private JokerManager jokerManager;
    [SerializeField] private GameManager gameManager; // 토글 직후 안내 재출력용 (선택)
    [SerializeField] private TextMeshProUGUI currentJokerText;

    // 토글형 API 호출
    public void ToggleNone() { jokerManager?.ToggleJoker(JokerType.None); jokerManager?.OnJokerToggled(gameManager); }
    public void ToggleAllInRock() { jokerManager?.ToggleJoker(JokerType.AllInRock); jokerManager?.OnJokerToggled(gameManager); }
    public void ToggleContrarian() { jokerManager?.ToggleJoker(JokerType.Contrarian); jokerManager?.OnJokerToggled(gameManager); }
    public void ToggleScout() { jokerManager?.ToggleJoker(JokerType.Scout); jokerManager?.OnJokerToggled(gameManager); }

    private void Update()
    {
        if (jokerManager != null && currentJokerText != null)
        {
            currentJokerText.text = $"Current: {jokerManager.CurrentJokerName}\nPipeline: {jokerManager.ActiveJokersDescription}";
        }
    }
}
