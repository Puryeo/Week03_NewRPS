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

        [Header("Options")]
        public bool autoApplyOnPlay = true;

        [Header("Debug Player Hand (apply on Start or call ApplyPlayerHandOverride)")]
        [Min(0)] public int dbgPlayerRocks = 0;
        [Min(0)] public int dbgPlayerPapers = 0;
        [Min(0)] public int dbgPlayerScissors = 0;
        public bool dbgShuffle = true;
        public bool dbgApplyOnStart = false; // Start()���� �ڵ� ����(�� ���� ���� ����)

        private bool _appliedEarly = false; // Awake���� ��Ŀ�� �������ߴ��� ����

        private void Awake()
        {
            // �� �ε�� ���� ����: ��� Awake �� ��� Start
            // RoundPrepare�� GameManager.Start���� ȣ��ǹǷ�, ��Ŀ Ȱ�� ���´� Awake���� �̸� �����ؾ� �Ѵ�.
            if (autoApplyOnPlay)
            {
                ApplySelectionInternal(skipNotify: true);
                _appliedEarly = true;
            }
            // �÷��̾� ���� ����� �������̵�� RoundPrepare ��, ���� ���� ���� �ݿ��Ǿ�� �Ѵ�.
            if (dbgApplyOnStart && gameManager != null)
            {
                gameManager.SetInitialHandOverride(dbgPlayerRocks, dbgPlayerPapers, dbgPlayerScissors, dbgShuffle);
            }
        }

        private void Start()
        {
            // Start������ ������ ����¸� ����(��Ŀ ����� Awake���� ������ �Ϸ�)
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

        // Awake���� ȣ�� ������ ���� ������. skipNotify=true�� OnJokerToggled�� �����Ѵ�(�ڵ�/�� �̻��� ���� ��ȣ)
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
    }
}