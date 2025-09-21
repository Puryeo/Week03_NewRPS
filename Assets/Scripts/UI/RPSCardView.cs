using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RPSCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Card Role")]
    public Choice choice = Choice.Rock;
    public bool forPlayer = true;
    public bool interactable = true;

    [Header("Refs")]
    public Image cardImage;
    public TextMeshProUGUI label;
    public GameManager gameManager;

    [Header("Visuals")]
    public Color normalColor = Color.white;
    public Color hoverColor = new Color(1f, 0.95f, 0.6f, 1f);
    public float hoverLiftY = 12f;
    public float hoverDuration = 0.2f;

    private RectTransform _rt;
    private Vector2 _basePos;
    private Coroutine _hoverRoutine;
    private bool _hovered;

    private void Reset()
    {
        cardImage = GetComponent<Image>();
        _rt = transform as RectTransform;
        if (label == null) label = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Awake()
    {
        if (cardImage == null) cardImage = GetComponent<Image>();
        _rt = transform as RectTransform;
        _basePos = _rt != null ? _rt.anchoredPosition : Vector2.zero;
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        ApplyVisuals(initial:true);
    }

    public void Bind(Choice c, bool playerSide, GameManager gm)
    {
        choice = c; forPlayer = playerSide; gameManager = gm;
        ApplyVisuals(initial:true);
    }

    private void ApplyVisuals(bool initial)
    {
        if (cardImage != null)
        {
            cardImage.color = normalColor;
        }
        if (label != null)
        {
            switch (choice)
            {
                case Choice.Rock: label.text = "Rock"; break;
                case Choice.Paper: label.text = "Paper"; break;
                case Choice.Scissors: label.text = "Scissors"; break;
                default: label.text = ""; break;
            }
        }
        if (initial && _rt != null)
        {
            _basePos = _rt.anchoredPosition;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovered = true;
        if (!interactable) { HoverTo(0f); return; }
        if (cardImage != null) cardImage.color = hoverColor;
        HoverTo(hoverLiftY);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;
        if (cardImage != null) cardImage.color = normalColor;
        HoverTo(0f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!forPlayer || !interactable) return;
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager == null) return;
        int idx = choice == Choice.Rock ? 0 : (choice == Choice.Paper ? 1 : 2);
        gameManager.PlayerMakesChoice(idx);
    }

    private void HoverTo(float lift)
    {
        if (_rt == null) return;
        if (_hoverRoutine != null) StopCoroutine(_hoverRoutine);
        var target = _basePos + new Vector2(0f, lift);
        _hoverRoutine = StartCoroutine(CoHover(target));
    }

    private IEnumerator CoHover(Vector2 target)
    {
        if (_rt == null) yield break;
        Vector2 start = _rt.anchoredPosition;
        float t = 0f;
        float dur = Mathf.Max(0.01f, hoverDuration);
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / dur);
            _rt.anchoredPosition = Vector2.Lerp(start, target, a);
            yield return null;
        }
        _rt.anchoredPosition = target;
        _hoverRoutine = null;
    }
}
