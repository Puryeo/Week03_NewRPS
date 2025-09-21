using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class ScoreAnimator : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("Score text prefix. Must match GameManager's text format.")]
    public string prefix = "Score: ";
    [Tooltip("Animation duration in seconds.")]
    public float duration = 0.6f;
    [Tooltip("Auto-detect external text change and animate.")]
    public bool autoDetectTextChanges = true;
    [Tooltip("Delay before starting auto animation (for sequencing jokers first)." )]
    public float startDelaySeconds = 0f;

    private TextMeshProUGUI _tmp;
    private Coroutine _routine;
    private Coroutine _delayRoutine;
    private int _lastShown;
    private int _delayedTarget;

    private void Awake()
    {
        _tmp = GetComponent<TextMeshProUGUI>();
        if (_tmp != null)
        {
            _lastShown = ParseCurrentText(_tmp.text);
        }
    }

    // Public entry to animate to a new total. Call this if you wire a GameManager event later.
    public void AnimateTo(int newTotal)
    {
        if (_tmp == null) return;
        if (_delayRoutine != null) { StopCoroutine(_delayRoutine); _delayRoutine = null; }
        if (_routine != null) StopCoroutine(_routine);
        int from = _lastShown;
        _routine = StartCoroutine(CoAnimate(from, newTotal));
    }

    public void SetStartDelay(float seconds)
    {
        startDelaySeconds = Mathf.Max(0f, seconds);
    }

    private IEnumerator CoAnimate(int from, int to)
    {
        float t = 0f;
        if (duration <= 0f) duration = 0.01f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / duration);
            int val = Mathf.RoundToInt(Mathf.Lerp(from, to, a));
            _tmp.text = prefix + val.ToString();
            yield return null;
        }
        _tmp.text = prefix + to.ToString();
        _lastShown = to;
        _routine = null;
    }

    private IEnumerator CoDelayThenAnimate()
    {
        float t = 0f;
        float d = Mathf.Max(0f, startDelaySeconds);
        while (t < d)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        _delayRoutine = null;
        // target might have changed while waiting; animate to latest delayed target
        AnimateTo(_delayedTarget);
    }

    // Auto-detect external writes and animate from previous, but only when not already animating
    private void LateUpdate()
    {
        if (!autoDetectTextChanges) return;
        if (_tmp == null) return;
        if (_routine != null) return; // let current animation finish

        int parsed = ParseCurrentText(_tmp.text);
        if (parsed != _lastShown)
        {
            if (startDelaySeconds > 0f)
            {
                _delayedTarget = parsed;
                if (_delayRoutine != null) StopCoroutine(_delayRoutine);
                _delayRoutine = StartCoroutine(CoDelayThenAnimate());
            }
            else
            {
                _routine = StartCoroutine(CoAnimate(_lastShown, parsed));
            }
        }
    }

    private int ParseCurrentText(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        // Fast path: exact prefix match
        if (!string.IsNullOrEmpty(prefix) && text.StartsWith(prefix))
        {
            string num = text.Substring(prefix.Length).Trim();
            if (int.TryParse(num, out int v)) return v;
        }
        // Fallback: extract first integer in the string (handles localized labels or extra suffix like "+5")
        var m = Regex.Match(text, @"-?\d+");
        if (m.Success && int.TryParse(m.Value, out int val)) return val;
        return 0;
    }
}
