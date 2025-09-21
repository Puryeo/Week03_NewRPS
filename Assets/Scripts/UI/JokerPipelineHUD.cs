using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Jokers;

namespace NewRPS.UI
{
    // Displays active jokers (in pipeline order) at the top of the GamePanel
    // - Instantiates an Image for each joker using its JokerData.thumbnail
    // - Shows a small tooltip on hover with joker description (simple overlay)
    // - Listens to JokerManager events to refresh list and play trigger effects
    public class JokerPipelineHUD : MonoBehaviour
    {
        [Header("Refs")]
        public JokerManager jokerManager;
        public Transform container; // Horizontal Layout Group parent
        public GameObject iconPrefab; // prefab with Image + JokerIconUI component
        public GameObject tooltipPrefab; // small overlay popup with TMP text
        public Sprite defaultThumbnail; // used when JokerData.thumbnail is null
        public TextMeshProUGUI scoreText; // optional: to set ScoreAnimator delay

        [Header("FX Sequencing")]
        public float scaleUp = 1.2f;
        public float scaleUpTime = 0.12f;
        public float scaleDownTime = 0.12f;
        public bool sequenceTriggeredFx = true; // if true, play one-by-one

        // runtime map from data to icon driver
        private readonly Dictionary<JokerData, JokerIconUI> _icons = new Dictionary<JokerData, JokerIconUI>();
        private readonly Queue<JokerData> _queue = new Queue<JokerData>();
        private bool _playing;

        private void Awake()
        {
            TryAutoAssign();
        }

        private void OnEnable()
        {
            if (jokerManager == null) TryAutoAssign();
            if (jokerManager != null)
            {
                jokerManager.PipelineChanged += Rebuild;
                jokerManager.OnJokerTriggered += HandleJokerTriggered;
            }
            Rebuild();
        }

        private void OnDisable()
        {
            if (jokerManager != null)
            {
                jokerManager.PipelineChanged -= Rebuild;
                jokerManager.OnJokerTriggered -= HandleJokerTriggered;
            }
        }

        private void TryAutoAssign()
        {
            if (jokerManager == null) jokerManager = Object.FindFirstObjectByType<JokerManager>();
        }

        public void Rebuild()
        {
            // clear existing
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                var child = container.GetChild(i);
                Destroy(child.gameObject);
            }
            _icons.Clear();

            if (jokerManager == null) return;
            var list = jokerManager.GetPipelineOrdered();
            if (list == null) return;

            for (int i = 0; i < list.Count; i++)
            {
                var data = list[i];
                if (data == null) continue;
                var go = Instantiate(iconPrefab, container);
                var icon = go.GetComponent<JokerIconUI>();
                if (icon == null) icon = go.AddComponent<JokerIconUI>();
                icon.defaultSprite = defaultThumbnail; // ensure fallback provided at runtime

                // be defensive: ensure Image exists
                var img = go.GetComponent<Image>();
                if (img == null)
                {
                    img = go.AddComponent<Image>();
                    img.raycastTarget = true;
                    img.preserveAspect = true;
                }

                icon.Bind(data, tooltipPrefab);
                _icons[data] = icon;
            }
        }

        private void HandleJokerTriggered(JokerData data)
        {
            if (data == null) return;
            if (!_icons.ContainsKey(data)) return;

            if (!sequenceTriggeredFx)
            {
                // Play immediately in parallel
                var icon = _icons[data];
                if (icon != null)
                {
                    StartCoroutine(icon.CoScaleFx(scaleUp, scaleUpTime, scaleDownTime));
                    icon.PlayTriggeredFx();
                }
                return;
            }

            // Queue and run sequentially
            _queue.Enqueue(data);
            if (!_playing)
            {
                StartCoroutine(CoPlayQueueThenReleaseScoreDelay());
            }
        }

        private IEnumerator CoPlayQueueThenReleaseScoreDelay()
        {
            _playing = true;

            // If score animator exists, delay its auto animation until effects finish
            ScoreAnimator sa = null;
            if (scoreText != null) sa = scoreText.GetComponent<ScoreAnimator>();
            float effectPerItem = Mathf.Max(0f, scaleUpTime + scaleDownTime);
            if (sa != null)
            {
                sa.SetStartDelay(effectPerItem * Mathf.Max(1, _queue.Count));
            }

            while (_queue.Count > 0)
            {
                var data = _queue.Dequeue();
                if (_icons.TryGetValue(data, out var icon) && icon != null)
                {
                    icon.PlayTriggeredFx();
                    yield return StartCoroutine(icon.CoScaleFx(scaleUp, scaleUpTime, scaleDownTime));
                }
            }

            // release: remove delay so next external score changes animate immediately
            if (sa != null) sa.SetStartDelay(0f);
            _playing = false;
        }

        // Expose icon rect for sequence cursor or visualizers
        public bool TryGetIconRect(JokerData data, out RectTransform rect)
        {
            rect = null;
            if (data == null) return false;
            if (_icons.TryGetValue(data, out var icon) && icon != null)
            {
                rect = icon.transform as RectTransform;
                return rect != null;
            }
            return false;
        }
    }
}
