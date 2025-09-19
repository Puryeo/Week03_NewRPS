using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Jokers;

public enum Choice { Rock, Paper, Scissors }
public enum Outcome { Win, Draw, Loss }

public class GameManager : MonoBehaviour
{
    [Header("UI References")] 
    [SerializeField] private TextMeshProUGUI aiHandText;      // AI 패 구성
    [SerializeField] private TextMeshProUGUI playerHandText;  // Player 패 구성
    [SerializeField] private TextMeshProUGUI turnText;        // 턴 표시
    [SerializeField] private TextMeshProUGUI scoreText;       // 점수
    [SerializeField] private TextMeshProUGUI resultText;      // 직전 턴 결과
    [SerializeField] private TextMeshProUGUI outcomeSummaryText; // 누적 승/무/패 요약
    [SerializeField] private TextMeshProUGUI rerollText;         // 리롤 남은 횟수 표시
    [SerializeField] private GameObject restartButton;        // 재시작 버튼 (Step3 예정)

    [Header("Managers")]
    public JokerManager jokerManager; // 조커 매니저 참조

    [Header("Config")]
    [Tooltip("초기 손패 장수")] [SerializeField] private int handSize = 6;
    [Tooltip("이번 라운드에 진행할 턴 수")] [SerializeField] private int turnsToPlay = 5;

    [Header("Guaranteed Cards (랜덤 지급 전 고정 추가 수량)")]
    [Tooltip("랜덤 지급 전에 Rock을 이 개수만큼 강제로 추가")] [SerializeField] private int guaranteedRocks = 1;
    [Tooltip("랜덤 지급 전에 Paper를 이 개수만큼 강제로 추가")] [SerializeField] private int guaranteedPapers = 1;
    [Tooltip("랜덤 지급 전에 Scissors를 이 개수만큼 강제로 추가")] [SerializeField] private int guaranteedScissors = 1;

    [Header("Result Colors")]
    [SerializeField] private Color winColor = Color.green;
    [SerializeField] private Color drawColor = Color.yellow;
    [SerializeField] private Color lossColor = Color.red;

    private const int WIN_POINTS = 5;
    private const int DRAW_POINTS = 3;
    private const int LOSS_POINTS = 0;

    private List<Choice> aiHand = new List<Choice>();
    private List<Choice> playerHand = new List<Choice>();
    private int currentTurn = 0;
    private int currentScore = 0;
    private System.Random rng;
    private bool roundActive = false;
    private bool inputLocked = false;

    private int winCount = 0;
    private int drawCount = 0;
    private int lossCount = 0;
    private int playerRerollsLeft = 0;

    private int MaxTurns => Mathf.Clamp(turnsToPlay, 1, Mathf.Max(1, handSize));

    private void OnValidate()
    {
        if (handSize < 1) handSize = 1;
        if (turnsToPlay < 1) turnsToPlay = 1;
        if (turnsToPlay > handSize) turnsToPlay = handSize; // 손패보다 많은 턴 금지
        if (guaranteedRocks < 0) guaranteedRocks = 0;
        if (guaranteedPapers < 0) guaranteedPapers = 0;
        if (guaranteedScissors < 0) guaranteedScissors = 0;
        int sum = guaranteedRocks + guaranteedPapers + guaranteedScissors;
        if (sum > handSize)
        {
            // 초과분을 Scissors -> Paper -> Rock 순으로 줄임
            int overflow = sum - handSize;
            int reduceS = Mathf.Min(guaranteedScissors, overflow);
            guaranteedScissors -= reduceS; overflow -= reduceS;
            if (overflow > 0)
            {
                int reduceP = Mathf.Min(guaranteedPapers, overflow);
                guaranteedPapers -= reduceP; overflow -= reduceP;
            }
            if (overflow > 0)
            {
                int reduceR = Mathf.Min(guaranteedRocks, overflow);
                guaranteedRocks -= reduceR; overflow -= reduceR;
            }
        }
    }

    private void Start()
    {
        StartRound();
    }

    private void StartRound()
    {
        currentTurn = 1;
        currentScore = 0;
        aiHand.Clear();
        playerHand.Clear();
        winCount = 0; drawCount = 0; lossCount = 0;
        rng = new System.Random();
        playerRerollsLeft = playerRerollMax;

        // 유효성 보정(런타임) - 에디터 외 상황 대비
        if (turnsToPlay < 1) turnsToPlay = 1;
        if (turnsToPlay > handSize) turnsToPlay = handSize;

        GenerateHand(aiHand, handSize, guaranteedRocks, guaranteedPapers, guaranteedScissors);
        GenerateHand(playerHand, handSize, guaranteedRocks, guaranteedPapers, guaranteedScissors);

        var a = CountHand(aiHand);
        var p = CountHand(playerHand);
        Debug.Log($"[StartRound] HandSize={handSize}, Turns={MaxTurns} | AI Hand: R{a.rock} P{a.paper} S{a.scissors}; Player Hand: R{p.rock} P{p.paper} S{p.scissors}; Rerolls={playerRerollsLeft}");

        roundActive = true;
        inputLocked = false;
        UpdateUI(initial: true);
        if (restartButton != null) restartButton.SetActive(false);

        // 조커 라운드 시작 훅
        if (jokerManager != null)
        {
            jokerManager.OnRoundStart(this);
        }
    }

    [Header("Reroll Config")]
    [SerializeField] private int playerRerollMax = 2; // 리롤 횟수 (위치 이동)

    public void OnClickRock() => PlayerMakesChoice((int)Choice.Rock);
    public void OnClickPaper() => PlayerMakesChoice((int)Choice.Paper);
    public void OnClickScissors() => PlayerMakesChoice((int)Choice.Scissors);
    public void OnClickRerollPlayerHand() => RerollPlayerHand();

    // 권장사항: 외부 시스템에서 턴 수를 변경하고, 필요 시 조커 안내를 재출력
    public void SetTurnsToPlay(int newTurns, bool refreshJokerInfo = true)
    {
        turnsToPlay = Mathf.Clamp(newTurns, 1, handSize);
        Debug.Log($"[Config] TurnsToPlay set to {turnsToPlay} (handSize={handSize})");
        UpdateUI();
        if (refreshJokerInfo && jokerManager != null)
        {
            jokerManager.OnJokerToggled(this);
        }
    }

    public void RefreshJokerInfo()
    {
        if (jokerManager != null)
        {
            jokerManager.OnJokerToggled(this);
        }
    }

    private void RerollPlayerHand()
    {
        if (!roundActive)
        {
            Debug.Log("[Reroll] 라운드 비활성");
            return;
        }
        if (currentTurn > 1 || playerHand.Count != handSize)
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

        GenerateHand(playerHand, handSize, guaranteedRocks, guaranteedPapers, guaranteedScissors);
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

    public void PlayerMakesChoice(int choiceIndex)
    {
        if (!roundActive) { Debug.Log("[PlayerMakesChoice] 라운드 비활성"); return; }
        if (inputLocked) { Debug.Log("[PlayerMakesChoice] 처리 중"); return; }
        if (currentTurn > MaxTurns) { Debug.Log("[PlayerMakesChoice] 모든 턴 소진"); return; }
        if (choiceIndex < 0 || choiceIndex > 2) { Debug.LogWarning("잘못된 choiceIndex"); return; }

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

        if (aiHand.Count == 0)
        {
            Debug.LogWarning("AI 핸드 비어 있음 - 논리 오류");
            inputLocked = false;
            return;
        }

        Choice aiChoice;
        if (jokerManager != null && jokerManager.AIDrawFromFront)
        {
            // 앞에서부터 사용: Scout 정보 일치성 보장
            aiChoice = aiHand[0];
            aiHand.RemoveAt(0);
        }
        else
        {
            int aiIndex = rng.Next(0, aiHand.Count);
            aiChoice = aiHand[aiIndex];
            aiHand.RemoveAt(aiIndex);
        }

        Outcome outcome = JudgeOutcome(playerChoice, aiChoice);
        int baseScore = ScoreFor(outcome);

        // 조커 점수 변형 적용(가산형 파이프라인)
        int modified = (jokerManager != null) ? jokerManager.ModifyScore(baseScore, ref currentScore, playerChoice, outcome) : baseScore;
        currentScore += modified;

        switch (outcome)
        {
            case Outcome.Win: winCount++; break;
            case Outcome.Draw: drawCount++; break;
            case Outcome.Loss: lossCount++; break;
        }

        var remainAI = CountHand(aiHand);
        var remainP = CountHand(playerHand);
        Debug.Log($"[Turn {currentTurn}] P:{playerChoice} vs AI:{aiChoice} => {outcome} (+{modified}) | Remain AI R{remainAI.rock}P{remainAI.paper}S{remainAI.scissors} | Player R{remainP.rock}P{remainP.paper}S{remainP.scissors} | Total={currentScore}");

        UpdateUI();
        if (resultText != null)
        {
            resultText.text = $"Result: {outcome} (You: {playerChoice} vs AI: {aiChoice}) +{modified}";
            resultText.color = outcome == Outcome.Win ? winColor : (outcome == Outcome.Draw ? drawColor : lossColor);
        }

        currentTurn++;

        if (currentTurn > MaxTurns)
        {
            roundActive = false;
            if (resultText != null)
                resultText.text += "\n(Round finished - Step3에서 최종 처리)";
            Debug.Log("[Round] 턴 종료 - EndRound 예정");
        }
        else
        {
            UpdateUI();
            inputLocked = false;
        }
    }

    // Scout용 정보 제공: 첫/마지막(실제 마지막 턴, 진행 상황 반영) 카드 미리보기
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

    // HUD 등에 메시지 노출
    public void ShowInfo(string msg)
    {
        Debug.Log("[Info] " + msg);
        if (resultText != null)
        {
            resultText.text = msg;
            resultText.color = Color.white;
        }
    }

    private void EndRound()
    {
        Debug.LogWarning("EndRound는 Step3에서 구현 예정입니다.");
    }

    private Outcome JudgeOutcome(Choice player, Choice ai)
    {
        if (player == ai) return Outcome.Draw;
        if ((player == Choice.Rock && ai == Choice.Scissors) ||
            (player == Choice.Paper && ai == Choice.Rock) ||
            (player == Choice.Scissors && ai == Choice.Paper))
            return Outcome.Win;
        return Outcome.Loss;
    }

    private int ScoreFor(Outcome o)
    {
        switch (o)
        {
            case Outcome.Win: return WIN_POINTS;
            case Outcome.Draw: return DRAW_POINTS;
            default: return LOSS_POINTS;
        }
    }

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

        // 섞기 (Fisher-Yates)
        for (int i = target.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            (target[i], target[j]) = (target[j], target[i]);
        }
    }

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
