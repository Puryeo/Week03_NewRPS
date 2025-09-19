using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Jokers;

// 게임의 핵심 흐름을 관리하는 매니저
// 초기 손패 생성, 턴 진행, 점수 계산, UI 업데이트를 담당한다.
// 외부 이벤트 연동
// - JokerManager.OnRoundStart(this): 라운드가 시작될 때 조커(정보성) 처리
// - JokerManager.OnJokerToggled(this): HUD 등에서 조커 토글 시 정보 재출력
// - JokerManager.ModifyScore(...): 레거시 조커 점수 가산 파이프라인 적용 (태그 기반 가산은 ExecuteTurnSettlementEffects로 별도 적용 예정)
public class GameManager : MonoBehaviour
{
    [Header("UI References")] 
    [SerializeField] private TextMeshProUGUI aiHandText;      // AI의 남은 손패 구성 정보를 텍스트로 표시
    [SerializeField] private TextMeshProUGUI playerHandText;  // 플레이어의 남은 손패 구성 정보를 텍스트로 표시
    [SerializeField] private TextMeshProUGUI turnText;        // 현재 턴/총 턴
    [SerializeField] private TextMeshProUGUI scoreText;       // 누적 점수 표시
    [SerializeField] private TextMeshProUGUI resultText;      // 직전 턴 결과 표시
    [SerializeField] private TextMeshProUGUI outcomeSummaryText; // 누적 승/무/패 요약
    [SerializeField] private TextMeshProUGUI rerollText;         // 남은 리롤 횟수
    [SerializeField] private GameObject restartButton;        // 라운드 종료 후 재시작 버튼

    [Header("Managers")]
    public JokerManager jokerManager; // 조커 매니저. 외부 이벤트 호출 대상(라운드시작, 토글, 점수 가산)

    [Header("Turns")]
    [Tooltip("이번 라운드에 진행할 턴 수")] [SerializeField] private int turnsToPlay = 5; // 턴 수는 손패 크기와 독립적으로 설정

    [Header("AI Hand Config")]
    [Tooltip("AI 초기 손패 장수")] [SerializeField] private int aiHandSize = 6; // AI 손패 크기
    [Tooltip("랜덤 지급 전에 AI에게 Rock 강제 추가 수량")] [SerializeField] private int aiGuaranteedRocks = 1; // 고정 지급: 바위
    [Tooltip("랜덤 지급 전에 AI에게 Paper 강제 추가 수량")] [SerializeField] private int aiGuaranteedPapers = 1; // 고정 지급: 보
    [Tooltip("랜덤 지급 전에 AI에게 Scissors 강제 추가 수량")] [SerializeField] private int aiGuaranteedScissors = 1; // 고정 지급: 가위

    [Header("Player Hand Config")]
    [Tooltip("플레이어 초기 손패 장수")] [SerializeField] private int playerHandSize = 6; // 플레이어 손패 크기
    [Tooltip("랜덤 지급 전에 플레이어에게 Rock 강제 추가 수량")] [SerializeField] private int playerGuaranteedRocks = 1; // 고정 지급: 바위
    [Tooltip("랜덤 지급 전에 플레이어에게 Paper 강제 추가 수량")] [SerializeField] private int playerGuaranteedPapers = 1; // 고정 지급: 보
    [Tooltip("랜덤 지급 전에 플레이어에게 Scissors 강제 추가 수량")] [SerializeField] private int playerGuaranteedScissors = 1; // 고정 지급: 가위

    [Header("Result Colors")]
    [SerializeField] private Color winColor = Color.green;   // 승리 텍스트 색상
    [SerializeField] private Color drawColor = Color.yellow; // 무승부 텍스트 색상
    [SerializeField] private Color lossColor = Color.red;    // 패배 텍스트 색상

    // 점수 룰: 가산 파이프라인의 기본 점수
    private const int WIN_POINTS = 5;
    private const int DRAW_POINTS = 3;
    private const int LOSS_POINTS = 0;

    // 내부 상태: 양측 손패, 현재 턴/점수, 입력 잠금 등
    private List<Choice> aiHand = new List<Choice>();
    private List<Choice> playerHand = new List<Choice>();
    private int currentTurn = 0;              // 현재 턴 인덱스(1부터 시작)
    private int currentScore = 0;             // 누적 점수
    private System.Random rng;                // 손패 생성과 AI 랜덤 선택에 사용
    private bool roundActive = false;         // 라운드 진행 중 여부
    private bool inputLocked = false;         // 턴 처리 중 입력 잠금

    private int winCount = 0;                 // 누적 승리 수
    private int drawCount = 0;                // 누적 무승부 수
    private int lossCount = 0;                // 누적 패배 수
    private int playerRerollsLeft = 0;        // 남은 리롤 횟수

    // 턴 수는 손패와 별개로 설정되지만, 실제 플레이는 카드 소진 시 조기 종료될 수 있다.
    private int MaxTurns => Mathf.Max(1, turnsToPlay);

    // 인스펙터 값의 런타임 전 유효성 보정 로직
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

        // 보장 수량 합계가 해당 손패 크기를 초과하지 않도록 자동 감축한다. (Scissors -> Paper -> Rock 순서)
        void ReduceOverflow(ref int r, ref int p, ref int s, int size)
        {
            int sum = r + p + s;
            if (sum <= size) return;
            int overflow = sum - size;
            int reduceS = Mathf.Min(s, overflow);
            s -= reduceS; overflow -= reduceS;
            if (overflow > 0)
            {
                int reduceP = Mathf.Min(p, overflow);
                p -= reduceP; overflow -= reduceP;
            }
            if (overflow > 0)
            {
                int reduceR = Mathf.Min(r, overflow);
                r -= reduceR; overflow -= reduceR;
            }
        }

        ReduceOverflow(ref aiGuaranteedRocks, ref aiGuaranteedPapers, ref aiGuaranteedScissors, aiHandSize);
        ReduceOverflow(ref playerGuaranteedRocks, ref playerGuaranteedPapers, ref playerGuaranteedScissors, playerHandSize);
    }

    // 게임 시작 시 라운드를 초기화하고 시작한다.
    private void Start()
    {
        StartRound();
    }

    // 라운드를 초기화하고 손패를 생성, UI를 초기화한다.
    // 외부 이벤트: JokerManager.OnRoundStart(this)를 호출하여 조커의 정보성 훅을 실행한다.
    private void StartRound()
    {
        currentTurn = 1;
        currentScore = 0;
        aiHand.Clear();
        playerHand.Clear();
        winCount = 0; drawCount = 0; lossCount = 0;
        rng = new System.Random();
        playerRerollsLeft = playerRerollMax;

        // 유효성 보정(런타임)
        if (turnsToPlay < 1) turnsToPlay = 1;

        // 손패 생성: 고정 수량을 먼저 채우고, 나머지는 랜덤으로 채운 뒤 셔플
        GenerateHand(aiHand, aiHandSize, aiGuaranteedRocks, aiGuaranteedPapers, aiGuaranteedScissors);
        GenerateHand(playerHand, playerHandSize, playerGuaranteedRocks, playerGuaranteedPapers, playerGuaranteedScissors);

        var a = CountHand(aiHand);
        var p = CountHand(playerHand);
        Debug.Log($"[StartRound] AI HandSize={aiHandSize}, Player HandSize={playerHandSize}, Turns={MaxTurns} | AI: R{a.rock} P{a.paper} S{a.scissors} | Player: R{p.rock} P{p.paper} S{p.scissors} | Rerolls={playerRerollsLeft}");
        RPS.RPSLog.Event("Round", "Start", $"aiSize={aiHandSize}, playerSize={playerHandSize}, turns={MaxTurns}, ai=R{a.rock}P{a.paper}S{a.scissors}, player=R{p.rock}P{p.paper}S{p.scissors}, rerolls={playerRerollsLeft}");

        roundActive = true;
        inputLocked = false;
        UpdateUI(initial: true);
        if (restartButton != null) restartButton.SetActive(false);

        // 조커 라운드 시작 훅 호출 (정보 표시, 드로우 정책 등)
        if (jokerManager != null)
        {
            jokerManager.OnRoundStart(this);
        }
    }

    [Header("Reroll Config")]
    [SerializeField] private int playerRerollMax = 2; // 플레이어 리롤 최대 횟수

    // UI 버튼 바인딩: 바위/보/가위 선택, 리롤 요청
    public void OnClickRock() => PlayerMakesChoice((int)Choice.Rock);
    public void OnClickPaper() => PlayerMakesChoice((int)Choice.Paper);
    public void OnClickScissors() => PlayerMakesChoice((int)Choice.Scissors);
    public void OnClickRerollPlayerHand() => RerollPlayerHand();

    // 외부에서 턴 수를 동적으로 변경할 때 사용. UI 갱신 및 정보성 조커 재출력 가능
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

    // 정보성 조커 재출력 요청
    public void RefreshJokerInfo()
    {
        if (jokerManager != null)
        {
            jokerManager.OnJokerToggled(this);
        }
    }

    // 리롤 처리: 첫 턴 전에만 가능. 손패 전체를 재생성하고 남은 리롤을 감소시킨다.
    private void RerollPlayerHand()
    {
        if (!roundActive)
        {
            Debug.Log("[Reroll] 라운드 비활성");
            return;
        }
        if (currentTurn > 1 || playerHand.Count != playerHandSize)
        {
            Debug.Log("[Reroll] 이미 턴 진행 - 첫 턴 전에만 가능");
            if (resultText != null) { resultText.text = "Result: Reroll unavailable (turn started)"; resultText.color = Color.white; }
            return;
        }
        if (playerRerollsLeft <= 0)
        {
            Debug.Log("[Reroll] 리롤 없음");
            if (resultText != null) { resultText.text = "Result: No rerolls left"; resultText.color = Color.white; }
            return;
        }

        GenerateHand(playerHand, playerHandSize, playerGuaranteedRocks, playerGuaranteedPapers, playerGuaranteedScissors);
        playerRerollsLeft--;
        var p = CountHand(playerHand);
        Debug.Log($"[Reroll] Player Hand New: R{p.rock} P{p.paper} S{p.scissors} | Remaining Rerolls={playerRerollsLeft}");
        UpdateUI();
        if (resultText != null)
        {
            resultText.text = $"Result: Player hand rerolled (Left: {playerRerollsLeft})";
            resultText.color = Color.white;
        }
    }

    // 플레이어가 버튼을 통해 바위/보/가위를 선택했을 때 호출되는 핵심 처리
    // 기능 요약
    // - 플레이어 손패에서 선택한 카드 제거
    // - AI는 앞에서부터 또는 랜덤으로 카드 선택(Scout 조커 상태에 따라 달라짐)
    // - 결과 판정 후 기본 점수 계산
    // - 레거시 조커 점수 가산(ModifyScore) 적용. (추후 태그 기반 ExecuteTurnSettlementEffects 추가 예정)
    // - UI 갱신 및 라운드 종료/진행 분기
    public void PlayerMakesChoice(int choiceIndex)
    {
        if (!roundActive) { Debug.Log("[PlayerMakesChoice] 라운드 비활성"); return; }
        if (inputLocked) { Debug.Log("[PlayerMakesChoice] 처리 중"); return; }
        if (currentTurn > MaxTurns) { Debug.Log("[PlayerMakesChoice] 모든 턴 소진"); return; }
        if (choiceIndex < 0 || choiceIndex > 2) { Debug.LogWarning("잘못된 choiceIndex"); return; }

        // 플레이어 손패에서 원하는 카드 선택 및 제거
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
        Choice playerChoice = desired;
        playerHand.RemoveAt(playerCardIndex);

        // AI 카드가 모두 소진되었으면 조기 종료
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
            return;
        }

        // AI 선택: Scout 조커가 활성일 때는 앞에서부터, 아니면 랜덤
        Choice aiChoice;
        if (jokerManager != null && jokerManager.AIDrawFromFront)
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

        // 결과 판정 및 기본 점수 산출
        Outcome outcome = JudgeOutcome(playerChoice, aiChoice);
        int baseScore = ScoreFor(outcome);

        // 태그 기반 점수 파이프라인 적용: GameContext 구성 후 ExecuteTurnSettlementEffects 호출
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
                scoreDelta = baseScore
            };
            jokerManager.ExecuteTurnSettlementEffects(context);
            turnDelta = context.scoreDelta;
        }
        else
        {
            turnDelta = baseScore;
        }
        currentScore += turnDelta;

        // 누적 결과 카운트
        switch (outcome)
        {
            case Outcome.Win: winCount++; break;
            case Outcome.Draw: drawCount++; break;
            case Outcome.Loss: lossCount++; break;
        }

        var remainAI = CountHand(aiHand);
        var remainP = CountHand(playerHand);
        Debug.Log($"[Turn {currentTurn}] P:{playerChoice} vs AI:{aiChoice} => {outcome} (+{turnDelta}) | Remain AI R{remainAI.rock}P{remainAI.paper}S{remainAI.scissors} | Player R{remainP.rock}P{remainP.paper}S{remainP.scissors} | Total={currentScore}");
        RPS.RPSLog.Event("Turn", "Resolve", $"turn={currentTurn}, player={playerChoice}, ai={aiChoice}, outcome={outcome}, delta={turnDelta}, total={currentScore}, aiRemain=R{remainAI.rock}P{remainAI.paper}S{remainAI.scissors}, pRemain=R{remainP.rock}P{remainP.paper}S{remainP.scissors}");

        UpdateUI();
        if (resultText != null)
        {
            resultText.text = $"Result: {outcome} (You: {playerChoice} vs AI: {aiChoice}) +{turnDelta}";
            resultText.color = outcome == Outcome.Win ? winColor : (outcome == Outcome.Draw ? drawColor : lossColor);
        }

        currentTurn++;

        // 라운드 종료 판정: 설정된 턴 수를 넘었거나, AI 손패가 소진되었을 때 종료
        if (currentTurn > MaxTurns || aiHand.Count == 0)
        {
            roundActive = false;
            if (resultText != null)
                resultText.text += "\n(Round finished - Step3에서 최종 처리)";
            if (restartButton != null) restartButton.SetActive(true);
            Debug.Log("[Round] 턴 종료 - EndRound 예정");
        }
        else
        {
            UpdateUI();
            inputLocked = false;
        }
    }

    // Scout 정보 제공: AI 첫 카드와 마지막 플레이 턴에 사용될 카드를 미리 보여준다.
    public Choice PeekAIFront()
    {
        if (aiHand != null && aiHand.Count > 0) return aiHand[0];
        return Choice.Rock;
    }
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

    // HUD 등 정보 텍스트 갱신용. JokerManager의 ShowInfo 호출에 의해 사용된다.
    public void ShowInfo(string msg)
    {
        Debug.Log("[Info] " + msg);
        if (resultText != null)
        {
            resultText.text = msg;
            resultText.color = Color.white;
        }
    }

    // 라운드 종료 최종 처리(점수 정산, 보상 등) ? 향후 확장 지점
    private void EndRound()
    {
        Debug.LogWarning("EndRound는 Step3에서 구현 예정입니다.");
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

    // HUD 텍스트들을 현재 상태로 갱신한다.
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

    // 손패 생성: 고정 수량을 먼저 채우고, 부족분은 랜덤으로 채운 후 셔플한다.
    private void GenerateHand(List<Choice> target, int size, int baseRock, int basePaper, int baseScissors)
    {
        target.Clear();
        if (size <= 0) return;

        // 고정 수량 먼저 채움
        for (int i = 0; i < baseRock && target.Count < size; i++) target.Add(Choice.Rock);
        for (int i = 0; i < basePaper && target.Count < size; i++) target.Add(Choice.Paper);
        for (int i = 0; i < baseScissors && target.Count < size; i++) target.Add(Choice.Scissors);

        // 나머지는 랜덤 채움
        while (target.Count < size)
        {
            target.Add((Choice)rng.Next(0, 3));
        }

        // 셔플(Fisher-Yates)
        for (int i = target.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            (target[i], target[j]) = (target[j], target[i]);
        }
    }

    // 손패 내 카드 개수(바위/보/가위) 집계
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
}
