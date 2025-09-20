namespace NewRPS.Debugging
{
    using System.Collections.Generic;
    using UnityEngine;
    using Jokers;

    public class DEBUGManager : MonoBehaviour
    {
        [Header("Refs")]
        public JokerManager jokerManager;
        public GameManager gameManager;
        public JokerLibrary library;

        [Header("Selection (apply order = list order)")]
        public List<JokerData> enabledJokers = new List<JokerData>();

        [Header("Draft (Offer/Pick)")]
        public List<JokerData> offered = new List<JokerData>();
        public List<JokerData> picked = new List<JokerData>();
        public int draftSeed = 12345;
        public int offerCount = 10;
        public int pickCount = 5;
        public int minAnchor = 2;
        public int minPayoff = 2;
        public int minUtility = 1;
        public int maxCatalyst = 3;
        public bool allowDuplicate = false;

        [Header("Options")]
        public bool autoApplyOnPlay = true;

        [Header("Debug Player Hand (apply on Start or call ApplyPlayerHandOverride)")]
        [Min(0)] public int dbgPlayerRocks = 0;
        [Min(0)] public int dbgPlayerPapers = 0;
        [Min(0)] public int dbgPlayerScissors = 0;
        public bool dbgShuffle = true;
        public bool dbgApplyOnStart = false; // Start()에서 자동 적용(씬 최초 시작 전용)

        private void Awake()
        {
            // 씬 로드시 실행 순서: 모든 Awake → 모든 Start
            // RoundPrepare가 GameManager.Start에서 호출되므로, 조커 활성 상태는 Awake에서 미리 적용해야 한다.
            if (autoApplyOnPlay)
            {
                ApplySelectionInternal(skipNotify: true);
            }
            // 플레이어 손패 디버그 오버라이드는 RoundPrepare 전, 손패 생성 전에 반영되어야 한다.
            if (dbgApplyOnStart && gameManager != null)
            {
                gameManager.SetInitialHandOverride(dbgPlayerRocks, dbgPlayerPapers, dbgPlayerScissors, dbgShuffle);
            }
        }

        private void Start()
        {
            // Start에서는 정보성 재출력만 수행(조커 토글은 Awake에서 선적용 완료)
            if (autoApplyOnPlay)
            {
                if (jokerManager != null && gameManager != null)
                {
                    jokerManager.OnJokerToggled(gameManager);
                }
            }
        }

        public void ClearAll()
        {
            if (jokerManager != null)
            {
                jokerManager.ToggleJoker(null); // clear
                if (gameManager != null) jokerManager.OnJokerToggled(gameManager);
            }
        }

        public void ApplySelection()
        {
            ApplySelectionInternal(skipNotify: false);
        }

        // Awake에서 호출 가능한 내부 진입점. skipNotify=true면 OnJokerToggled를 생략한다(핸드/턴 미생성 상태 보호)
        private void ApplySelectionInternal(bool skipNotify)
        {
            if (jokerManager == null) { Debug.LogWarning("[DEBUGManager] JokerManager not assigned"); return; }
            // Clear first to control order deterministically
            jokerManager.ToggleJoker(null);
            var seen = new HashSet<JokerData>();
            for (int i = 0; i < enabledJokers.Count; i++)
            {
                var d = enabledJokers[i];
                if (d == null || seen.Contains(d)) continue;
                seen.Add(d);
                jokerManager.ToggleJoker(d); // add in order
            }
            if (!skipNotify && gameManager != null) jokerManager.OnJokerToggled(gameManager);
            Debug.Log($"[DEBUGManager] Applied {seen.Count} jokers (skipNotify={skipNotify})");
        }

        public void EnableAllFromLibrary()
        {
            if (library == null) return;
            enabledJokers.Clear();
            for (int i = 0; i < library.jokers.Count; i++)
            {
                var d = library.jokers[i];
                if (d != null) enabledJokers.Add(d);
            }
            ApplySelection();
        }

        public void DisableAll()
        {
            enabledJokers.Clear();
            ClearAll();
        }

        public void ApplyPlayerHandOverride(bool force = false)
        {
            if (gameManager == null) { Debug.LogWarning("[DEBUGManager] GameManager not assigned"); return; }
            gameManager.DebugTrySetPlayerHandCounts(dbgPlayerRocks, dbgPlayerPapers, dbgPlayerScissors, dbgShuffle, force);
        }

        // Draft helpers
        public void GenerateOffer()
        {
            offered.Clear(); picked.Clear();
            if (library == null)
            {
                Debug.LogWarning("[DEBUGManager] JokerLibrary not assigned");
                return;
            }
            var cfg = new JokerLibrary.OfferConfig
            {
                offerCount = offerCount,
                pickCount = pickCount,
                minAnchor = minAnchor,
                minPayoff = minPayoff,
                minUtility = minUtility,
                maxCatalyst = maxCatalyst,
                allowDuplicate = allowDuplicate,
                seed = draftSeed
            };
            var list = library.PickOffer(cfg);
            if (list != null) offered.AddRange(list);
            Debug.Log($"[Draft] Offered {offered.Count} (seed={draftSeed})");
        }

        public void CommitPickedAsEnabled()
        {
            enabledJokers.Clear();
            for (int i = 0; i < picked.Count; i++)
            {
                var d = picked[i];
                if (d != null && !enabledJokers.Contains(d)) enabledJokers.Add(d);
            }
            ApplySelection();
        }
    }
}