using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Jokers;
using System.Reflection;

namespace NewRPS.UI
{
    public class DraftFlowController : MonoBehaviour
    {
        [Header("Refs")]
        public GameManager gameManager;
        public JokerManager jokerManager;
        public JokerLibrary library;

        [Header("Panels")]
        public GameObject jokerPanel;
        public GameObject pipelinePanel;
        public GameObject gamePanel; // 기존 게임 UI 루트 패널

        [Header("Offer UI")]
        public Transform offerContainer; // parent with GridLayoutGroup
        public JokerOfferButton offerButtonPrefab;
        public TextMeshProUGUI pickCounterText;
        public Button offerConfirmButton;

        [Header("Pipeline UI")]
        public Transform pipelineContainer; // parent for ordered items
        public GameObject pipelineItemPrefab; // expects component JokerPipelineItem with Init(JokerData,int,DraftFlowController)
        public Button pipelineConfirmButton;

        [Header("Config")]
        public int offerCount = 10;     // 현재 10 고정 (향후 확장 가능)
        public int pickCount = 5;       // 선택 개수(가변)
        public int seed = 12345;
        public int minAnchor = 2;
        public int minPayoff = 2;
        public int minUtility = 1;
        public int maxCatalyst = 3;
        public bool allowDuplicate = false;

        private readonly List<JokerData> _offered = new List<JokerData>();
        private readonly List<JokerData> _picked = new List<JokerData>();
        private readonly List<JokerOfferButton> _offerButtons = new List<JokerOfferButton>();

        private void Start()
        {
            if (gameManager == null || jokerManager == null || library == null)
            {
                Debug.LogError("[DraftFlow] Missing refs. Assign GameManager/JokerManager/JokerLibrary.");
                enabled = false; return;
            }

            // 드래프트 시작: 패널 상태 설정
            if (jokerPanel != null) jokerPanel.SetActive(true);
            if (pipelinePanel != null) pipelinePanel.SetActive(false);
            if (gamePanel != null) gamePanel.SetActive(false);

            // 씬의 GameManager는 startOnPlay=false 권장
            BuildOffer();
        }

        private void BuildOffer()
        {
            _offered.Clear();
            _picked.Clear();
            ClearChildren(offerContainer);
            ClearChildren(pipelineContainer);
            _offerButtons.Clear();
            UpdateOfferConfirmInteractable();

            var cfg = new JokerLibrary.OfferConfig
            {
                offerCount = offerCount,
                pickCount = pickCount,
                minAnchor = minAnchor,
                minPayoff = minPayoff,
                minUtility = minUtility,
                maxCatalyst = maxCatalyst,
                allowDuplicate = allowDuplicate,
                seed = seed
            };
            var list = library.PickOffer(cfg);
            if (list == null || list.Count == 0)
            {
                Debug.LogWarning("[DraftFlow] PickOffer returned empty list.");
                return;
            }
            _offered.AddRange(list);

            for (int i = 0; i < _offered.Count; i++)
            {
                var data = _offered[i]; if (data == null) continue;
                var item = Instantiate(offerButtonPrefab, offerContainer);
                item.Init(data, this);
                _offerButtons.Add(item);
            }
            UpdatePickCounter();
        }

        public void OnOfferToggle(JokerData data, bool on, JokerOfferButton source)
        {
            if (data == null) return;
            if (on)
            {
                if (_picked.Contains(data)) { /* already */ }
                else if (_picked.Count >= pickCount)
                {
                    // 초과 선택 방지: 토글 원복
                    if (source != null) source.SetIsOn(false, silent: true);
                }
                else
                {
                    _picked.Add(data);
                }
            }
            else
            {
                _picked.Remove(data);
            }
            UpdatePickCounter();
            UpdateOfferConfirmInteractable();
        }

        private void UpdatePickCounter()
        {
            if (pickCounterText != null)
                pickCounterText.text = $"{_picked.Count} / {pickCount}";
        }

        private void UpdateOfferConfirmInteractable()
        {
            if (offerConfirmButton != null)
                offerConfirmButton.interactable = (_picked.Count == pickCount);
        }

        public void OnClickOfferConfirm()
        {
            if (_picked.Count != pickCount)
            {
                Debug.LogWarning("[DraftFlow] Pick exactly required count before confirming.");
                return;
            }
            if (jokerPanel != null) jokerPanel.SetActive(false);
            if (pipelinePanel != null) pipelinePanel.SetActive(true);
            if (gamePanel != null) gamePanel.SetActive(false);
            BuildPipelineUI();
        }

        private void BuildPipelineUI()
        {
            ClearChildren(pipelineContainer);
            for (int i = 0; i < _picked.Count; i++)
            {
                var data = _picked[i]; if (data == null) continue;
                var go = Instantiate(pipelineItemPrefab, pipelineContainer);
                var comp = go != null ? go.GetComponent("JokerPipelineItem") as MonoBehaviour : null;
                if (comp != null)
                {
                    var mi = comp.GetType().GetMethod("Init", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (mi != null)
                    {
                        mi.Invoke(comp, new object[] { data, i, this });
                    }
                }
            }
            if (pipelineConfirmButton != null) pipelineConfirmButton.interactable = _picked.Count == pickCount;
        }

        public void MovePipelineItem(int index, int delta)
        {
            int newIndex = index + delta;
            if (newIndex < 0 || newIndex >= _picked.Count) return;
            var tmp = _picked[index];
            _picked[index] = _picked[newIndex];
            _picked[newIndex] = tmp;
            BuildPipelineUI();
        }

        public void OnClickPipelineConfirm()
        {
            // 조커 적용
            jokerManager.ToggleJoker(null);
            var seen = new HashSet<JokerData>();
            for (int i = 0; i < _picked.Count; i++)
            {
                var d = _picked[i];
                if (d == null || seen.Contains(d)) continue; // 안전
                seen.Add(d);
                jokerManager.ToggleJoker(d);
            }
            // 정보 재출력 및 라운드 시작
            jokerManager.OnJokerToggled(gameManager);
            if (pipelinePanel != null) pipelinePanel.SetActive(false);
            if (gamePanel != null) gamePanel.SetActive(true);
            gameManager.StartRoundFromFlow();
        }

        private static void ClearChildren(Transform t)
        {
            if (t == null) return;
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var go = t.GetChild(i);
                if (Application.isPlaying) Object.Destroy(go.gameObject);
                else Object.DestroyImmediate(go.gameObject);
            }
        }
    }
}
