using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Jokers;
using NewRPS.Debugging;
using System; // for Action<>


// 게임의 핵심 흐름을 관리하는 매니저 스크립트
// 이 클래스는 다음을 책임진다:
// - 라운드 수명주기 제어: 라운드 시작(StartRound) → 각 턴 처리(PlayerMakesChoice) → 라운드 종료(EndRound 예정)
// - 손패 생성 및 유지: AI/플레이어 손패 리스트 관리, 보장 수량, 셔플
// - 점수/통계 관리: 누적 점수, 승/무/패 카운트, 턴 번호
// - 리롤(Reroll) 관리: 첫 턴 시작 전 한정, 사용 횟수 기록, UI 반영
// - 조커 시스템 훅 호출: RoundPrepare(Phase C), RoundStart, TurnStart(Phase C), TurnSettlement, RoundEnd(Phase D)
// - UI 업데이트: 각종 TextMeshProUGUI와 Restart 버튼 상태
// 외부와의 주요 상호작용:
// - JokerManager: OnRoundPrepare(this) → OnRoundStart(this) → OnTurnStart(GameContext) → ExecuteTurnSettlementEffects(GameContext) → OnRoundEnd(this, RoundResult)
// - DEBUGManager: DebugTrySetPlayerHandCounts를 통해 플레이어 손패를 디버그 오버라이드
// - UI 버튼: OnClickRock/Paper/Scissors/RerollPlayerHand/Restart로 사용자 입력 수신

[Serializable]
public class RoundResult
{
    public int totalScore;
    public int wins;
    public int draws;
    public int losses;
    public int turnsPlanned;
    public int turnsPlayed;
    public int rerollsUsed;
}

public class GameManager : MonoBehaviour
{
    [Header("UI References")] 
    [SerializeField] private TextMeshProUGUI aiHandText;      // AI 손패 구성 텍스트
    [SerializeField] private TextMeshProUGUI playerHandText;  // 플레이어 손패 구성 텍스트
    [SerializeField] private TextMeshProUGUI turnText;        // 현재 턴/총 턴 텍스트
    [SerializeField] private TextMeshProUGUI scoreText;       // 누적 점수 텍스트
    [SerializeField] private TextMeshProUGUI resultText;      // 직전 턴 결과 텍스트(추가 정보 메시지 포함)
    [SerializeField] private TextMeshProUGUI outcomeSummaryText; // 승/무/패 요약 텍스트
    [SerializeField] private TextMeshProUGUI rerollText;         // 남은 리롤 횟수 텍스트
    [SerializeField] private GameObject restartButton;        // 재시작 버튼(항상 활성 요구사항)

    [Header("Managers")]
    public JokerManager jokerManager; // 조커 매니저. 라운드 훅과 점수 파이프라인을 수행

    [Header("Flow")]
    [Tooltip("씬 시작 시 자동으로 라운드를 시작할지 여부(드래프트 플로우 사용 시 끄세요)")]
    [SerializeField] private bool startOnPlay = true;

    [Header("Turns")]
    [Tooltip("이번 라운드에 진행할 턴 수")]
    [SerializeField] private int turnsToPlay = 5; // 턴 수는 손패 크기와 독립적. 카드 소진 시 조기 종료 가능

    [Header("AI Hand Config")]
    [Tooltip("AI 초기 손패 장수")] [SerializeField] private int aiHandSize = 6; // AI 손패 크기
    [Tooltip("랜덤 지급 전에 AI에게 Rock 강제 추가 수량")] [SerializeField] private int aiGuaranteedRocks = 1; // AI 고정 지급: 바위
    [Tooltip("랜덤 지급 전에 AI에게 Paper 강제 추가 수량")] [SerializeField] private int aiGuaranteedPapers = 1; // AI 고정 지급: 보
    [Tooltip("랜덤 지급 전에 AI에게 Scissors 강제 추가 수량")] [SerializeField] private int aiGuaranteedScissors = 1; // AI 고정 지급: 가위

    [Header("Player Hand Config")]
    [Tooltip("플레이어 초기 손패 장수")] [SerializeField] private int playerHandSize = 6; // 플레이어 손패 크기
    [Tooltip("구버전 잔재: 플레이어 고정 지급 수량은 사용하지 않음")] [SerializeField] private int playerGuaranteedRocks = 0;
    [SerializeField] private int playerGuaranteedPapers = 0;
    [SerializeField] private int playerGuaranteedScissors = 0;

    [Header("Result Colors")]
    [SerializeField] private Color winColor = Color.green;   // 승리 텍스트 색상
    [SerializeField] private Color drawColor = Color.yellow; // 무승부 텍스트 색상
    [SerializeField] private Color lossColor = Color.red;    // 패배 텍스트 색상

    // 기본 점수 규칙(가산 파이프라인의 베이스)
    private const int WIN_POINTS = 5;
    private const int DRAW_POINTS = 3;
    private const int LOSS_POINTS = 0;

    // 내부 상태: 손패, 진행 상태, 점수, 이력, RNG 등
    private List<Choice> aiHand = new List<Choice>();       // AI 손패(덱 역할)
    private List<Choice> playerHand = new List<Choice>();   // 플레이어 손패
    private int currentTurn = 0;                            // 현재 턴 인덱스(1부터 시작)
    private int currentScore = 0;                           // 누적 점수
    private System.Random rng;                              // 손패 생성과 AI 랜덤 선택에 사용(결정적 순서 보장)
    private bool roundActive = false;                       // 라운드 진행 여부
    private bool inputLocked = false;                       // 턴 처리 중 입력 잠금
    private bool _playerActedThisRound = false;             // 플레이 액션(카드 소모) 발생 여부

    private int winCount = 0;                 // 누적 승리 수
    private int drawCount = 0;                // 누적 무승부 수
    private int lossCount = 0;                // 누적 패배 수
    private int playerRerollsLeft = 0;        // 남은 리롤 횟수
    private int playerRerollsUsed = 0;        // 사용한 리롤 횟수(조건에 사용)

    // 라운드 히스토리(턴 정산용 컨텍스트에 복제하여 전달)
    private List<Choice> playerHistory = new List<Choice>();
    private List<Outcome> outcomeHistory = new List<Outcome>();

    // TurnSettlement에서 ShowInfo로 전달된 메시지를 결과 텍스트에 덧붙이기 위한 버퍼
    private string _pendingInfoMsg;

    // UI 표기용 최대 턴수(최소 1로 보정)
    private int MaxTurns => Mathf.Max(1, turnsToPlay);

    // 라운드 종료 1회 가드 및 이벤트
    private bool _roundEnded = false;
    public event Action<RoundResult> OnRoundEnded;

    // 인스펙터 값 보정(음수 방지, 합계 초과 보정)
    private void OnValidate()
    {
        // 턴 수 최소 보정
        if (turnsToPlay < 1) turnsToPlay = 1;

        // 손패 크기 보정
        if (aiHandSize < 1) aiHandSize = 1;
        if (playerHandSize < 1) playerHandSize = 1;

        // 보장 수량 음수 방지
        if (aiGuaranteedRocks < 0) aiGuaranteedRocks = 0;
        if (aiGuaranteedPapers < 0) aiGuaranteedPapers = 0;
        if (aiGuaranteedScissors < 0) aiGuaranteedScissors = 0;
        if (playerGuaranteedRocks < 0) playerGuaranteedRocks = 0;
        if (playerGuaranteedPapers < 0) playerGuaranteedPapers = 0;
        if (playerGuaranteedScissors < 0) playerGuaranteedScissors = 0;

        // AI 보장 수량 합계가 손패 크기 초과 시 Scissors→Paper→Rock 순으로 감축
        void ReduceOverflow(ref int r, ref int p, ref int s, int size)
        {
            int sum = r + p + s;
            if (sum <= size) return;
            int overflow = sum - size;
            int reduceS = Mathf.Min(s, overflow); s -= reduceS; overflow -= reduceS;
            if (overflow > 0) { int reduceP = Mathf.Min(p, overflow); p -= reduceP; overflow -= reduceP; }
            if (overflow > 0) { int reduceR = Mathf.Min(r, overflow); r -= reduceR; overflow -= reduceR; }
        }
        ReduceOverflow(ref aiGuaranteedRocks, ref aiGuaranteedPapers, ref aiGuaranteedScissors, aiHandSize);
        // 플레이어 보장 수량은 미사용(완전 랜덤)
    }

    // 유니티 시작 시 자동으로 라운드 시작 (startOnPlay==true일 때만)
    private void Start()
    {
        if (startOnPlay)
        {
            StartRound();
        }
    }

    // 외부 플로우(드래프트 확정 등)에서 라운드를 시작할 때 호출
    public void StartRoundFromFlow()
    {
        if (roundActive) return;
        StartRound();
    }

    // 라운드를 초기화하고 손패/점수/이력 초기화 후 RoundPrepare → RoundStart 순으로 조커 훅을 호출
    // Restart 버튼은 항상 활성 요구사항에 따라 켠다
    private void StartRound()
    {
        // 기본 설정 복원(Prepare에 의한 변형 이전 기준으로 되돌림)
        turnsToPlay = Mathf.Max(1, _initialTurnsToPlay);
        aiHandSize = Mathf.Max(1, _initialAIHandSize);
        playerHandSize = Mathf.Max(1, _initialPlayerHandSize);

        // 내부 상태 초기화
        currentTurn = 1;
        currentScore = 0;
        aiHand.Clear();
        playerHand.Clear();
        winCount = 0; drawCount = 0; lossCount = 0;
        playerHistory.Clear();
        outcomeHistory.Clear();
        _pendingInfoMsg = null;
        rng = new System.Random();
        playerRerollsLeft = playerRerollMax;
        playerRerollsUsed = 0;
        _playerActedThisRound = false; // 첫 액션 전으로 초기화
        if (turnsToPlay < 1) turnsToPlay = 1;
        _prepareTurnsDeltaApplied = 0; // Prepare 델타 누적 초기화
        _prepareTurnsDeltaPending = 0; _preparePassActive = false;
        _roundEnded = false; // 라운드 종료 가드 리셋

        // 손패 생성
        if (_initialHandOverride.HasValue)
        {
            // 디버그 오버라이드가 지정된 경우, 플레이어 핸드를 먼저 지정하고 AI 핸드는 정상 생성
            var ov = _initialHandOverride.Value;
            GenerateHand(aiHand, aiHandSize, aiGuaranteedRocks, aiGuaranteedPapers, aiGuaranteedScissors);
            // 플레이어 핸드는 지정값으로 설정
            var list = new System.Collections.Generic.List<Choice>(ov.r + ov.p + ov.s);
            for (int i = 0; i < ov.r; i++) list.Add(Choice.Rock);
            for (int i = 0; i < ov.p; i++) list.Add(Choice.Paper);
            for (int i = 0; i < ov.s; i++) list.Add(Choice.Scissors);
            if (ov.shuffle && list.Count > 1)
            {
                for (int i = list.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(0, i + 1);
                    var t = list[i]; list[i] = list[j]; list[j] = t;
                }
            }
            playerHand = list;
            playerHandSize = playerHand.Count > 0 ? playerHand.Count : playerHandSize;
            _initialHandOverride = null; // 일회성 적용 후 해제
        }
        else
        {
            GenerateHand(aiHand, aiHandSize, aiGuaranteedRocks, aiGuaranteedPapers, aiGuaranteedScissors);
            GenerateHand(playerHand, playerHandSize, 0, 0, 0);
        }

        // Phase C: RoundPrepare 훅(손패/턴수 변형 등의 사전 준비)
        if (jokerManager != null)
        {
            jokerManager.OnRoundPrepare(this);
        }

        // Prepare 이후 UI 초기화 반영
        UpdateUI(initial: true);

        // Restart 버튼은 항상 활성
        if (restartButton != null) restartButton.SetActive(true);

        // RoundStart 훅(정보 표시, 드로우 정책 등)
        if (jokerManager != null)
        {
            jokerManager.OnRoundStart(this);
        }

        // 라운드 진행 시작
        roundActive = true;
        inputLocked = false;
    }

    [Header("Restart Options")] 
    [Tooltip("재시작 시 현재 씬을 통째로 리로드하여 완전 초기화할 지 여부 (기본: 끔)")]
    [SerializeField] private bool restartReloadScene = false;

    [Header("Reroll Config")] 
    [SerializeField] private int playerRerollMax = 2; // 플레이어 리롤 최대 횟수(첫 턴 전만 사용 가능)

    // 라운드/씬 기준 초기 설정값 백업(Prepare 델타 등으로 변형되기 전 값)
    private int _initialTurnsToPlay;
    private int _initialAIHandSize;
    private int _initialPlayerHandSize;

    private void Awake()
    {
        // 씬 로드 시점의 초기값을 저장. Restart(비-리로드 모드)에서 복원에 사용
        _initialTurnsToPlay = turnsToPlay;
        _initialAIHandSize = aiHandSize;
        _initialPlayerHandSize = playerHandSize;
    }

    // UI 버튼 이벤트: 바위/보/가위 선택, 리롤, 재시작
    public void OnClickRock() => PlayerMakesChoice((int)Choice.Rock);
    public void OnClickPaper() => PlayerMakesChoice((int)Choice.Paper);
    public void OnClickScissors() => PlayerMakesChoice((int)Choice.Scissors);
    public void OnClickRerollPlayerHand() => RerollPlayerHand();
    public void OnClickRestart() => RestartRound();

    // 외부에서 턴 수 변경 시 호출. 필요 시 조커 정보 재출력
    public void SetTurnsToPlay(int newTurns, bool refreshJokerInfo = true)
    {
        turnsToPlay = Mathf.Max(1, newTurns);
        Debug.Log($"[Config] TurnsToPlay set to {turnsToPlay} (aiHandSize={aiHandSize}, playerHandSize={playerHandSize})");
        UpdateUI();
        if (refreshJokerInfo && jokerManager != null)
        {
            jokerManager.OnJokerToggled(this);
        }
    }

    // 조커 정보성 메시지 재출력(Scout 등)
    public void RefreshJokerInfo()
    {
        if (jokerManager != null)
        {
            jokerManager.OnJokerToggled(this);
        }
    }

    // 플레이어 손패 리롤. 첫 턴 시작 전, 남은 리롤이 있을 때만 가능
    // 리롤 시 RoundStart 계열 변형(Paper_Dominance 등)을 재적용하기 위해 OnJokerToggled 호출
    private void RerollPlayerHand()
    {
        if (!roundActive)
        {
            Debug.Log("[Reroll] 라운드 비활성");
            return;
        }
        // 변경: 플레이 액션(카드 소모) 전까지만 허용
        if (_playerActedThisRound)
        {
            Debug.Log("[Reroll] 이미 턴 진행 - 플레이 액션 전까지만 가능");
            if (resultText != null) { resultText.text = "Result: Reroll unavailable (action started)"; resultText.color = Color.white; }
            return;
        }
        if (playerRerollsLeft <= 0)
        {
            Debug.Log("[Reroll] 리롤 없음");
            if (resultText != null) { resultText.text = "Result: No rerolls left"; resultText.color = Color.white; }
            return;
        }

        // 현재 손패 장수를 유지한 채 완전 랜덤 리롤(Prepare로 증가한 장수 보존)
        int newSize = Mathf.Max(0, playerHand.Count);
        if (newSize == 0) newSize = playerHandSize; // 안전 장치
        GenerateHand(playerHand, newSize, 0, 0, 0);
        playerRerollsLeft--;
        playerRerollsUsed++;

        // 리롤 후 Prepare 계열의 턴 수 변형(ModifyTurnsToPlayDelta)만 재평가하여 반영
        if (jokerManager != null)
        {
            jokerManager.OnRoundPrepareReapply(this); // 카드 추가 효과는 제외, 턴 수만 재평가
        }

        // RoundStart 변형 재평가(드로우 정책은 유지). Paper_Dominance류 손패 변형은 1회만 허용
        if (jokerManager != null)
        {
            jokerManager.AllowRoundStartHandMutationReapplyOnce();
            jokerManager.OnJokerToggled(this);
        }

        var p = CountHand(playerHand);
        Debug.Log($"[Reroll] Player Hand New: R{p.rock} P{p.paper} S{p.scissors} | Remaining Rerolls={playerRerollsLeft}");
        UpdateUI();
        if (resultText != null)
        {
            resultText.text = $"Result: Player hand rerolled (Left: {playerRerollsLeft})";
            resultText.color = Color.white;
        }
    }

    // 플레이어가 버튼으로 바위/보/가위를 선택했을 때 턴을 처리하는 핵심 로직
    // 처리 순서: 입력 검증 → 플레이어 카드 소모 → (TurnStart 조커 가산) → AI 선택 → 판정 → Settlement 조커 적용 → 점수/통계 반영 → UI 업데이트
    public void PlayerMakesChoice(int choiceIndex)
    {
        // 입력 및 상태 검증
        if (!roundActive) { Debug.Log("[PlayerMakesChoice] 라운드 비활성"); return; }
        if (inputLocked) { Debug.Log("[PlayerMakesChoice] 처리 중"); return; }
        if (currentTurn > MaxTurns) { Debug.Log("[PlayerMakesChoice] 모든 턴 소진"); return; }
        if (choiceIndex < 0 || choiceIndex > 2) { Debug.LogWarning("잘못된 choiceIndex"); return; }

        // 플레이어 손패에서 선택 카드 소모
        Choice desired = (Choice)choiceIndex;
        int playerCardIndex = playerHand.FindIndex(c => c == desired);
        if (playerCardIndex == -1)
        {
            if (resultText != null)
            {
                resultText.text = $"Result: You have no {desired} card left.";
                resultText.color = Color.white;
            }
            Debug.Log("[PlayerMakesChoice] 해당 카드 없음");
            return;
        }
        inputLocked = true;
        _playerActedThisRound = true; // 첫 액션 수행됨 → 이후 리롤 불가
        Choice playerChoice = desired;
        playerHand.RemoveAt(playerCardIndex);

        // TurnStart 훅: 결과 판정 전에 손패 기반 가산 점수 등 선반영
        if (jokerManager != null)
        {
            var startCtx = new Jokers.GameContext
            {
                gameManager = this,
                playerChoice = Choice.None,     // TurnStart에서는 아직 미정
                outcome = Outcome.None,         // TurnStart에서는 아직 미정
                baseScore = 0,
                currentTotal = currentScore,
                scoreDelta = 0,
                turnIndex = currentTurn,
                turnsPlanned = MaxTurns,
                isLastTurn = (currentTurn >= MaxTurns),
                playerHistory = new List<Choice>(playerHistory),
                outcomeHistory = new List<Outcome>(outcomeHistory),
                rerollsUsed = GetPlayerRerollsUsed(),
            };
            jokerManager.OnTurnStart(startCtx);
            if (startCtx.scoreDelta != 0)
            {
                currentScore += startCtx.scoreDelta; // 선가산 점수는 즉시 누적
                RPS.RPSLog.Event("Turn", "StartDelta", $"turn={currentTurn}, add={startCtx.scoreDelta}, total={currentScore}");
            }
        }

        // AI 카드가 모두 소진되었으면 조기 종료 검사 이전에 AI 선택 분기
        if (aiHand.Count == 0)
        {
            roundActive = false;
            Debug.Log("[PlayerMakesChoice] AI has no cards left - ending round early");
            if (resultText != null)
            {
                resultText.text = "Result: AI has no cards left. Round finished.";
                resultText.color = Color.white;
            }
            if (restartButton != null) restartButton.SetActive(true);
            UpdateUI();
            EndRound();
            return;
        }

        // AI 선택: 일회성 앞드로우 강제 또는 라운드 정책 반영
        Choice aiChoice;
        if (jokerManager != null && jokerManager.ShouldDrawAIFromFront())
        {
            aiChoice = aiHand[0];
            aiHand.RemoveAt(0);
        }
        else
        {
            int aiIndex = rng.Next(0, aiHand.Count);
            aiChoice = aiHand[aiIndex];
            aiHand.RemoveAt(aiIndex);
        }

        // 가위바위보 판정 및 기본 점수 산출
        Outcome outcome = JudgeOutcome(playerChoice, aiChoice);
        int baseScore = ScoreFor(outcome);

        // 라운드 히스토리에 이번 턴 추가(컨텍스트 빌드에 포함)
        playerHistory.Add(playerChoice);
        outcomeHistory.Add(outcome);

        // TurnSettlement 파이프라인 실행: 조커 조건/효과로 점수 가산/배수 적용 등
        int turnDelta;
        if (jokerManager != null)
        {
            var context = new Jokers.GameContext
            {
                gameManager = this,
                playerChoice = playerChoice,
                outcome = outcome,
                baseScore = baseScore,
                currentTotal = currentScore,
                scoreDelta = baseScore,         // Settlement 시작 시 베이스 점수로 초기화
                turnIndex = currentTurn,
                turnsPlanned = MaxTurns,
                isLastTurn = (currentTurn >= MaxTurns),
                playerHistory = new List<Choice>(playerHistory),
                outcomeHistory = new List<Outcome>(outcomeHistory),
                rerollsUsed = GetPlayerRerollsUsed(),
            };
            jokerManager.ExecuteTurnSettlementEffects(context);
            turnDelta = context.scoreDelta;
        }
        else
        {
            turnDelta = baseScore;
        }
        currentScore += turnDelta;

        // 승/무/패 카운트 누적
        switch (outcome)
        {
            case Outcome.Win: winCount++; break;
            case Outcome.Draw: drawCount++; break;
            case Outcome.Loss: lossCount++; break;
        }

        // 로그 및 잔여 손패 카운트 출력
        var remainAI = CountHand(aiHand);
        var remainP = CountHand(playerHand);
        Debug.Log($"[Turn {currentTurn}] P:{playerChoice} vs AI:{aiChoice} => {outcome} (+{turnDelta}) | Remain AI R{remainAI.rock}P{remainAI.paper}S{remainAI.scissors} | Player R{remainP.rock}P{remainP.paper}S{remainP.scissors} | Total={currentScore}");
        RPS.RPSLog.Event("Turn", "Resolve", $"turn={currentTurn}, player={playerChoice}, ai={aiChoice}, outcome={outcome}, delta={turnDelta}, total={currentScore}, aiRemain=R{remainAI.rock}P{remainAI.paper}S{remainAI.scissors}, pRemain=R{remainP.rock}P{remainP.paper}S{remainP.scissors}");

        // 결과 텍스트 및 Settlement 메시지 병합 출력
        UpdateUI();
        if (resultText != null)
        {
            resultText.text = $"Result: {outcome} (You: {playerChoice} vs AI: {aiChoice}) +{turnDelta}";
            resultText.color = outcome == Outcome.Win ? winColor : (outcome == Outcome.Draw ? drawColor : lossColor);
            if (!string.IsNullOrEmpty(_pendingInfoMsg))
            {
                resultText.text += "\n" + _pendingInfoMsg; // Geological_Survey 등 메시지 부가 출력
                _pendingInfoMsg = null;                      // 한 턴에 1회 소비 후 초기화
            }
        }

        // 다음 턴으로 전진
        currentTurn++;

        // 라운드 종료 판정: 설정된 턴 수 초과 또는 AI 손패 소진
        if (currentTurn > MaxTurns || aiHand.Count == 0)
        {
            roundActive = false;
            if (resultText != null)
                resultText.text += "\n(Round finished)"; // 간단 표기만 유지(UI는 후순위)
            if (restartButton != null) restartButton.SetActive(true); // 항상 활성 유지
            Debug.Log("[Round] 턴 종료 - EndRound 처리");
            EndRound();
        }
        else
        {
            UpdateUI();
            inputLocked = false; // 다음 입력 허용
        }
    }

    // AI 덱의 첫 카드 미리보기(Scout 용). AI 덱이 비었으면 기본값 Rock 반환
    public Choice PeekAIFront()
    {
        if (aiHand != null && aiHand.Count > 0) return aiHand[0];
        return Choice.Rock;
    }

    // AI가 마지막으로 플레이할 턘에 해당하는 카드 미리보기. 남은 장수와 계획 턨 수를 바탕으로 인덱스 계산
    public Choice PeekAIBack()
    {
        int count = (aiHand != null) ? aiHand.Count : 0;
        if (count > 0)
        {
            int finalIndex = Mathf.Clamp(MaxTurns - currentTurn, 0, count - 1);
            return aiHand[finalIndex];
        }
        return Choice.Rock;
    }

    // TurnSettlement에서 ShowInfo로 전달된 정보를 한 턴 결과 아래에 이어서 보여주기 위해 버퍼링
    public void ShowInfo(string msg)
    {
        Debug.Log("[Info] " + msg);
        _pendingInfoMsg = msg; // PlayerMakesChoice 말미에서 UI로 출력
    }

    // 라운드 종료 최종 처리(보상/메타 등) - 백엔드만 구현, UI는 후순위
    private void EndRound()
    {
        if (_roundEnded) return;
        _roundEnded = true;

        inputLocked = true;
        roundActive = false;

        var result = new RoundResult
        {
            totalScore = currentScore,
            wins = winCount,
            draws = drawCount,
            losses = lossCount,
            turnsPlanned = MaxTurns,
            turnsPlayed = Mathf.Clamp(currentTurn - 1, 0, MaxTurns),
            rerollsUsed = playerRerollsUsed
        };

        // Phase D: RoundEnd 조커 파이프라인(최종 점수 변형 가능). 이벤트/로그 전에 호출
        if (jokerManager != null)
        {
            try { jokerManager.OnRoundEnd(this, result); }
            catch (Exception ex) { Debug.LogError($"[RoundEnd] Joker pipeline error: {ex}"); }
        }

        // 조커 적용 후 최종 점수를 내부 상태에 반영하고 UI 갱신
        currentScore = result.totalScore;
        UpdateUI();

        // 최종 값으로 로그 출력
        RPS.RPSLog.Event("Round", "Ended", $"score={result.totalScore}, W={result.wins}, D={result.draws}, L={result.losses}, planned={result.turnsPlanned}, played={result.turnsPlayed}, rerollsUsed={result.rerollsUsed}");

        try
        {
            OnRoundEnded?.Invoke(result);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Round] OnRoundEnded error: {ex}");
        }
    }

    // UI에서 재시작 버튼을 눌렀을 때 라운드 재시작
    public void RestartRound()
    {
        Debug.Log("[Round] Restart requested by UI");
        if (restartReloadScene)
        {
            // 씬 리로드를 통한 완전 초기화(컴포넌트/정적 변수 포함). Build Settings에 씬 등록 필요
            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex);
            return;
        }

        // 비-리로드 모드: 상태/설정 수동 초기화 후 라운드 재시작
        StartRound();
    }

    // 가위바위보 규칙으로 결과 판정
    private Outcome JudgeOutcome(Choice player, Choice ai)
    {
        if (player == ai) return Outcome.Draw;
        if ((player == Choice.Rock && ai == Choice.Scissors) ||
            (player == Choice.Paper && ai == Choice.Rock) ||
            (player == Choice.Scissors && ai == Choice.Paper))
            return Outcome.Win;
        return Outcome.Loss;
    }

    // 결과에 따른 기본 점수 반환
    private int ScoreFor(Outcome o)
    {
        switch (o)
        {
            case Outcome.Win: return WIN_POINTS;
            case Outcome.Draw: return DRAW_POINTS;
            default: return LOSS_POINTS;
        }
    }

    // Prepare 단계에서 턴수 가감 누적 관리용
    private int _prepareTurnsDeltaApplied = 0;   // 현재까지 Prepare 효과로 적용된 턴수 델타(누적 값)
    private int _prepareTurnsDeltaPending = 0;   // 이번 Prepare 패스에서 계산 중인 델타 합
    private bool _preparePassActive = false;

    // HUD 텍스트를 현재 상태로 갱신. 라운드 시작 시 initial=true로 기본 텍스트/색 초기화
    private void UpdateUI(bool initial = false)
    {
        if (aiHandText != null)
        {
            var c = CountHand(aiHand);
            aiHandText.text = $"AI Hand: Rock x{c.rock} / Paper x{c.paper} / Scissors x{c.scissors}";
        }
        else Debug.LogWarning("aiHandText 미할당");

        if (playerHandText != null)
        {
            var p = CountHand(playerHand);
            playerHandText.text = $"Player Hand: Rock x{p.rock} / Paper x{p.paper} / Scissors x{p.scissors}";
        }
        else Debug.LogWarning("playerHandText 미할당");

        if (turnText != null) turnText.text = $"Turn {Mathf.Min(currentTurn, MaxTurns)} / {MaxTurns}"; else Debug.LogWarning("turnText 미할당");
        if (scoreText != null) scoreText.text = $"Score: {currentScore}"; else Debug.LogWarning("scoreText 미할당");

        if (outcomeSummaryText != null)
            outcomeSummaryText.text = $"W:{winCount} / D:{drawCount} / L:{lossCount}";
        else
            Debug.LogWarning("outcomeSummaryText 미할당");

        if (rerollText != null)
            rerollText.text = $"Rerolls: {playerRerollsLeft}";
        else
            Debug.LogWarning("rerollText 미할당 (리롤 남은 횟수 Text 생성 후 연결)");

        if (resultText != null && initial)
        {
            resultText.text = "Result: -";
            resultText.color = Color.white;
        }
    }

    // 손패 생성: 고정 수량을 먼저 채우고 부족분은 랜덤으로 채운 뒤 Fisher-Yates 셔플
    private void GenerateHand(List<Choice> target, int size, int baseRock, int basePaper, int baseScissors)
    {
        target.Clear();
        if (size <= 0) return;

        // 고정 수량 채우기
        for (int i = 0; i < baseRock && target.Count < size; i++) target.Add(Choice.Rock);
        for (int i = 0; i < basePaper && target.Count < size; i++) target.Add(Choice.Paper);
        for (int i = 0; i < baseScissors && target.Count < size; i++) target.Add(Choice.Scissors);

        // 나머지 랜덤 채우기
        while (target.Count < size)
        {
            target.Add((Choice)rng.Next(0, 3));
        }

        // Fisher-Yates 셔플로 순서 섞기(결정적 RNG 사용)
        for (int i = target.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            (target[i], target[j]) = (target[j], target[i]);
        }
    }

    // 손패 내 Rock/Paper/Scissors 개수 집계
    private (int rock, int paper, int scissors) CountHand(List<Choice> hand)
    {
        int r = 0, p = 0, s = 0;
        for (int i = 0; i < hand.Count; i++)
        {
            switch (hand[i])
            {
                case Choice.Rock: r++; break;
                case Choice.Paper: p++; break;
                case Choice.Scissors: s++; break;
            }
        }
        return (r, p, s);
    }

    // 조커 RoundStart 조건/효과에서 사용하는 헬퍼들
    public int CountAIChoice(Choice c)
    {
        int cnt = 0; for (int i = 0; i < aiHand.Count; i++) if (aiHand[i] == c) cnt++; return cnt;
    }
    public int CountPlayerChoice(Choice c)
    {
        int cnt = 0; for (int i = 0; i < playerHand.Count; i++) if (playerHand[i] == c) cnt++; return cnt;
    }

    // AI 손패의 무작위 인덱스를 choice로 변경(중복 선택될 수 있음). Paper_Dominance에는 아래 전용 버전을 사용
    public int ReplaceAIRandomCardsTo(Choice to, int count)
    {
        if (aiHand == null || aiHand.Count == 0 || count <= 0) return 0;
        int changed = 0;
        for (int n = 0; n < count && aiHand.Count > 0; n++)
        {
            int idx = rng.Next(0, aiHand.Count);
            if (aiHand[idx] != to)
            {
                aiHand[idx] = to;
                changed++;
            }
        }
        UpdateUI();
        return changed;
    }

    // Paper_Dominance용 전용 변형: 후보를 Paper/Scissors로 제한하고 중복 없이 정확히 지정 수량 변경
    public int ReplaceAIPaperOrScissorsToRock(int count)
    {
        if (aiHand == null || aiHand.Count == 0 || count <= 0) return 0;
        var candidates = new List<int>();
        for (int i = 0; i < aiHand.Count; i++)
        {
            if (aiHand[i] == Choice.Paper || aiHand[i] == Choice.Scissors) candidates.Add(i);
        }
        int toChange = Mathf.Min(count, candidates.Count);
        int changed = 0;
        for (int n = 0; n < toChange; n++)
        {
            int pick = rng.Next(0, candidates.Count);
            int idx = candidates[pick];
            int last = candidates.Count - 1;
            candidates[pick] = candidates[last];
            candidates.RemoveAt(last); // 중복 방지
            aiHand[idx] = Choice.Rock;
            changed++;
        }
        UpdateUI();
        return changed;
    }

    // 디버그 지원: 플레이어 손패를 지정 개수로 강제 세팅(앞 턘 전 권장). 필요 시 강제(force) 적용 가능
    public bool DebugTrySetPlayerHandCounts(int rock, int paper, int scissors, bool shuffle = true, bool force = false)
    {
        if (!roundActive && !force) { Debug.LogWarning("[Debug] Round inactive"); return false; }
        if (currentTurn > 1 && !force) { Debug.LogWarning("[Debug] Cannot override after turn started"); return false; }
        if (rock < 0) rock = 0; if (paper < 0) paper = 0; if (scissors < 0) scissors = 0;
        var list = new List<Choice>(rock + paper + scissors);
        for (int i = 0; i < rock; i++) list.Add(Choice.Rock);
        for (int i = 0; i < paper; i++) list.Add(Choice.Paper);
        for (int i = 0; i < scissors; i++) list.Add(Choice.Scissors);
        if (shuffle && list.Count > 1)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                var t = list[i]; list[i] = list[j]; list[j] = t;
            }
        }
        playerHand = list;
        // 리롤 조건의 일관성을 위해 현재 사이즈를 동기화(선택)
        playerHandSize = playerHand.Count > 0 ? playerHand.Count : playerHandSize;
        UpdateUI();
        RPS.RPSLog.Event("Debug", "PlayerHandOverride", $"R={rock},P={paper},S={scissors}, shuffle={shuffle}");
        if (resultText != null)
        {
            resultText.text = $"Result: Debug override applied (R{rock}/P{paper}/S{scissors})";
            resultText.color = Color.white;
        }
        return true;
    }

    // TurnSettlement 조건에서 사용할 리롤 사용 횟수 노출
    public int GetPlayerRerollsUsed() => playerRerollsUsed;

    // Phase C RoundPrepare 효과에서 사용하는 안전 API들
    public void ModifyTurnsToPlayDelta(int delta)
    {
        int newTurns = Mathf.Max(1, turnsToPlay + delta);
        SetTurnsToPlay(newTurns, refreshJokerInfo: false);
    }

    public void AddCardsToPlayerHand(Choice c, int count)
    {
        if (count <= 0) return;
        for (int i = 0; i < count; i++) playerHand.Add(c);
        UpdateUI();
    }

    public void AddCardsToAIHand(Choice c, int count)
    {
        if (count <= 0) return;
        for (int i = 0; i < count; i++) aiHand.Add(c);
        UpdateUI();
    }

    // Prepare 단계 재평가용 API: JokerManager에서 패스 시작/누적/커밋 호출
    public void BeginPreparePass(bool reapply)
    {
        _preparePassActive = true;
        _prepareTurnsDeltaPending = 0; // 매 패스는 0부터 재계산
    }
    public void AccumulatePrepareTurnsDelta(int delta)
    {
        if (!_preparePassActive) return;
        _prepareTurnsDeltaPending += delta;
    }
    public void CommitPreparePass()
    {
        if (!_preparePassActive) return;
        int deltaToApply = _prepareTurnsDeltaPending - _prepareTurnsDeltaApplied;
        if (deltaToApply != 0)
        {
            int newTurns = Mathf.Max(1, turnsToPlay + deltaToApply);
            SetTurnsToPlay(newTurns, refreshJokerInfo: false);
            _prepareTurnsDeltaApplied = _prepareTurnsDeltaPending;
            RPS.RPSLog.Event("TurnsMut", "PrepareCommit", $"applyDelta={deltaToApply}, appliedTotal={_prepareTurnsDeltaApplied}, turns={turnsToPlay}");
        }
        _preparePassActive = false;
        _prepareTurnsDeltaPending = 0;
    }

    // 선택: 씬 최초 시작 시 플레이어 초기 핸드를 지정(디버그 전용). StartRound에서 우선 반영 후 null로 리셋
    private (int r, int p, int s, bool shuffle)? _initialHandOverride;

    public void SetInitialHandOverride(int rock, int paper, int scissors, bool shuffle)
    {
        if (rock < 0) rock = 0; if (paper < 0) paper = 0; if (scissors < 0) scissors = 0;
        _initialHandOverride = (rock, paper, scissors, shuffle);
    }

    // RoundEnd/authoring에서 라운드 히스토리 접근 용도
    public System.Collections.Generic.IReadOnlyList<Choice> GetPlayerHistory() => playerHistory;
    public System.Collections.Generic.IReadOnlyList<Outcome> GetOutcomeHistory() => outcomeHistory;

    // Phase C RoundPrepare helpers for random card adds
    public void AddRandomCardsToPlayerHand(int count)
    {
        if (count <= 0) return;
        for (int i = 0; i < count; i++)
        {
            playerHand.Add((Choice)rng.Next(0, 3));
        }
        UpdateUI();
    }
    public void AddRandomCardsToAIHand(int count)
    {
        if (count <= 0) return;
        for (int i = 0; i < count; i++)
        {
            aiHand.Add((Choice)rng.Next(0, 3));
        }
        UpdateUI();
    }
}
