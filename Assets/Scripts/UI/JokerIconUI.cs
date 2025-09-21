using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Jokers;

namespace NewRPS.UI
{
    // Handles a single joker icon: sprite assignment, hover tooltip, and trigger FX
    public class JokerIconUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Refs")]
        public Image image;
        public JokerData data;

        [Header("Tooltip")]
        public GameObject tooltipPrefab;
        private GameObject _tooltipInstance;

        [Header("FX")]
        public float shakeDuration = 0.2f;
        public float shakeIntensity = 8f;
        public Color triggerColor = new Color(1f, 0.9f, 0.2f, 1f);
        public float colorLerpBackSeconds = 1f;

        [Header("Scale FX")]
        public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Fallback")]
        public Sprite defaultSprite;

        private Color _originalColor;
        private RectTransform _rt;
        private Coroutine _fxRoutine;

        private void Awake()
        {
            if (image == null) image = GetComponent<Image>();
            _rt = transform as RectTransform;
            if (image != null) _originalColor = image.color;
        }

        public void Bind(JokerData d, GameObject tooltipPrefabRef)
        {
            data = d;
            tooltipPrefab = tooltipPrefabRef;

            // lazy-acquire Image in case it was added just before Bind() was called
            if (image == null) image = GetComponent<Image>();

            if (image == null)
            {
                Debug.LogWarning("[JokerIconUI] Missing Image component.");
                return;
            }

            // choose sprite: data thumbnail or default
            Sprite sprite = null;
            if (d != null && d.thumbnail != null)
            {
                sprite = d.thumbnail;
            }
            else if (defaultSprite != null)
            {
                sprite = defaultSprite;
            }

            if (sprite == null)
            {
                // no sprite available -> visible hint + log to help diagnose
                image.sprite = null;
                image.color = new Color(1f, 0f, 1f, 0.5f); // magenta hint
                Debug.LogWarning($"[JokerIconUI] No sprite found for '{(d!=null?(string.IsNullOrEmpty(d.jokerName)?d.name:d.jokerName):"<null>")}'. Assign JokerPipelineHUD.defaultThumbnail or JokerIconUI.defaultSprite on prefab, or set JokerData.thumbnail.");
            }
            else
            {
                image.color = _originalColor == default ? Color.white : _originalColor; // reset color if previously hinted
                image.sprite = sprite;
                image.preserveAspect = true;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ShowTooltip();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HideTooltip();
        }

        private void ShowTooltip()
        {
            if (tooltipPrefab == null || _tooltipInstance != null || data == null) return;
            // instantiate as child so it can follow layout; keep small overlay near icon
            _tooltipInstance = Instantiate(tooltipPrefab, transform);
            var txt = _tooltipInstance.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                string title = string.IsNullOrEmpty(data.jokerName) ? data.name : data.jokerName;
                txt.text = title + "\n" + data.description;
            }
            _tooltipInstance.SetActive(true);
        }

        private void HideTooltip()
        {
            if (_tooltipInstance != null)
            {
                Destroy(_tooltipInstance);
                _tooltipInstance = null;
            }
        }

        public void PlayTriggeredFx()
        {
            if (_fxRoutine != null) StopCoroutine(_fxRoutine);
            _fxRoutine = StartCoroutine(CoFx());
        }

        private IEnumerator CoFx()
        {
            // shake + color blink then revert over colorLerpBackSeconds
            float t = 0f;
            var startPos = _rt.anchoredPosition;
            if (image != null) image.color = triggerColor;

            while (t < shakeDuration)
            {
                t += Time.unscaledDeltaTime;
                float offX = (Random.value * 2f - 1f) * shakeIntensity;
                float offY = (Random.value * 2f - 1f) * shakeIntensity;
                _rt.anchoredPosition = startPos + new Vector2(offX, offY);
                yield return null;
            }
            _rt.anchoredPosition = startPos;

            // lerp color back
            float ct = 0f;
            while (ct < colorLerpBackSeconds)
            {
                ct += Time.unscaledDeltaTime;
                if (image != null)
                {
                    float a = Mathf.Clamp01(ct / colorLerpBackSeconds);
                    image.color = Color.Lerp(triggerColor, _originalColor, a);
                }
                yield return null;
            }
            if (image != null) image.color = _originalColor;
            _fxRoutine = null;
        }

        public IEnumerator CoScaleFx(float upScale, float upTime, float downTime)
        {
            if (_rt == null) yield break;
            Vector3 baseScale = _rt.localScale;
            Vector3 target = baseScale * upScale;
            float t = 0f;
            while (t < upTime)
            {
                t += Time.unscaledDeltaTime;
                float a = upTime <= 0 ? 1f : Mathf.Clamp01(t / upTime);
                float eval = scaleCurve != null ? scaleCurve.Evaluate(a) : a;
                _rt.localScale = Vector3.LerpUnclamped(baseScale, target, eval);
                yield return null;
            }
            // down
            t = 0f;
            while (t < downTime)
            {
                t += Time.unscaledDeltaTime;
                float a = downTime <= 0 ? 1f : Mathf.Clamp01(t / downTime);
                float eval = scaleCurve != null ? scaleCurve.Evaluate(a) : a;
                _rt.localScale = Vector3.LerpUnclamped(target, baseScale, eval);
                yield return null;
            }
            _rt.localScale = baseScale;
        }

        private void OnDisable()
        {
            HideTooltip();
        }
    }
}
