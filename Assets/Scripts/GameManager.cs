using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Jokers;

// ������ �ٽ� �帧�� �����ϴ� �Ŵ���
// �ʱ� ���� ����, �� ����, ���� ���, UI ������Ʈ�� ����Ѵ�.
// �ܺ� �̺�Ʈ ����
// - JokerManager.OnRoundStart(this): ���尡 ���۵� �� ��Ŀ(������) ó��
// - JokerManager.OnJokerToggled(this): HUD ��� ��Ŀ ��� �� ���� �����
// - JokerManager.ModifyScore(...): ���Ž� ��Ŀ ���� ���� ���������� ���� (�±� ��� ������ ExecuteTurnSettlementEffects�� ���� ���� ����)
public class GameManager : MonoBehaviour
{
    [Header("UI References")] 
    [SerializeField] private TextMeshProUGUI aiHandText;      // AI�� ���� ���� ���� ������ �ؽ�Ʈ�� ǥ��
    [SerializeField] private TextMeshProUGUI playerHandText;  // �÷��̾��� ���� ���� ���� ������ �ؽ�Ʈ�� ǥ��
    [SerializeField] private TextMeshProUGUI turnText;        // ���� ��/�� ��
    [SerializeField] private TextMeshProUGUI scoreText;       // ���� ���� ǥ��
    [SerializeField] private TextMeshProUGUI resultText;      // ���� �� ��� ǥ��
    [SerializeField] private TextMeshProUGUI outcomeSummaryText; // ���� ��/��/�� ���
    [SerializeField] private TextMeshProUGUI rerollText;         // ���� ���� Ƚ��
    [SerializeField] private GameObject restartButton;        // ���� ���� �� ����� ��ư

    [Header("Managers")]
    public JokerManager jokerManager; // ��Ŀ �Ŵ���. �ܺ� �̺�Ʈ ȣ�� ���(�������, ���, ���� ����)

    [Header("Turns")]
    [Tooltip("�̹� ���忡 ������ �� ��")] [SerializeField] private int turnsToPlay = 5; // �� ���� ���� ũ��� ���������� ����

    [Header("AI Hand Config")]
    [Tooltip("AI �ʱ� ���� ���")] [SerializeField] private int aiHandSize = 6; // AI ���� ũ��
    [Tooltip("���� ���� ���� AI���� Rock ���� �߰� ����")] [SerializeField] private int aiGuaranteedRocks = 1; // ���� ����: ����
    [Tooltip("���� ���� ���� AI���� Paper ���� �߰� ����")] [SerializeField] private int aiGuaranteedPapers = 1; // ���� ����: ��
    [Tooltip("���� ���� ���� AI���� Scissors ���� �߰� ����")] [SerializeField] private int aiGuaranteedScissors = 1; // ���� ����: ����

    [Header("Player Hand Config")]
    [Tooltip("�÷��̾� �ʱ� ���� ���")] [SerializeField] private int playerHandSize = 6; // �÷��̾� ���� ũ��
    [Tooltip("���� ���� ���� �÷��̾�� Rock ���� �߰� ����")] [SerializeField] private int playerGuaranteedRocks = 1; // ���� ����: ����
    [Tooltip("���� ���� ���� �÷��̾�� Paper ���� �߰� ����")] [SerializeField] private int playerGuaranteedPapers = 1; // ���� ����: ��
    [Tooltip("���� ���� ���� �÷��̾�� Scissors ���� �߰� ����")] [SerializeField] private int playerGuaranteedScissors = 1; // ���� ����: ����

    [Header("Result Colors")]
    [SerializeField] private Color winColor = Color.green;   // �¸� �ؽ�Ʈ ����
    [SerializeField] private Color drawColor = Color.yellow; // ���º� �ؽ�Ʈ ����
    [SerializeField] private Color lossColor = Color.red;    // �й� �ؽ�Ʈ ����

    // ���� ��: ���� ������������ �⺻ ����
    private const int WIN_POINTS = 5;
    private const int DRAW_POINTS = 3;
    private const int LOSS_POINTS = 0;

    // ���� ����: ���� ����, ���� ��/����, �Է� ��� ��
    private List<Choice> aiHand = new List<Choice>();
    private List<Choice> playerHand = new List<Choice>();
    private int currentTurn = 0;              // ���� �� �ε���(1���� ����)
    private int currentScore = 0;             // ���� ����
    private System.Random rng;                // ���� ������ AI ���� ���ÿ� ���
    private bool roundActive = false;         // ���� ���� �� ����
    private bool inputLocked = false;         // �� ó�� �� �Է� ���

    private int winCount = 0;                 // ���� �¸� ��
    private int drawCount = 0;                // ���� ���º� ��
    private int lossCount = 0;                // ���� �й� ��
    private int playerRerollsLeft = 0;        // ���� ���� Ƚ��

    // �� ���� ���п� ������ ����������, ���� �÷��̴� ī�� ���� �� ���� ����� �� �ִ�.
    private int MaxTurns => Mathf.Max(1, turnsToPlay);

    // �ν����� ���� ��Ÿ�� �� ��ȿ�� ���� ����
    private void OnValidate()
    {
        // �� �� �ּ� ����
        if (turnsToPlay < 1) turnsToPlay = 1;

        // ���� ũ�� ����
        if (aiHandSize < 1) aiHandSize = 1;
        if (playerHandSize < 1) playerHandSize = 1;

        // ���� ���� ���� ����
        if (aiGuaranteedRocks < 0) aiGuaranteedRocks = 0;
        if (aiGuaranteedPapers < 0) aiGuaranteedPapers = 0;
        if (aiGuaranteedScissors < 0) aiGuaranteedScissors = 0;
        if (playerGuaranteedRocks < 0) playerGuaranteedRocks = 0;
        if (playerGuaranteedPapers < 0) playerGuaranteedPapers = 0;
        if (playerGuaranteedScissors < 0) playerGuaranteedScissors = 0;

        // ���� ���� �հ谡 �ش� ���� ũ�⸦ �ʰ����� �ʵ��� �ڵ� �����Ѵ�. (Scissors -> Paper -> Rock ����)
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

    // ���� ���� �� ���带 �ʱ�ȭ�ϰ� �����Ѵ�.
    private void Start()
    {
        StartRound();
    }

    // ���带 �ʱ�ȭ�ϰ� ���и� ����, UI�� �ʱ�ȭ�Ѵ�.
    // �ܺ� �̺�Ʈ: JokerManager.OnRoundStart(this)�� ȣ���Ͽ� ��Ŀ�� ������ ���� �����Ѵ�.
    private void StartRound()
    {
        currentTurn = 1;
        currentScore = 0;
        aiHand.Clear();
        playerHand.Clear();
        winCount = 0; drawCount = 0; lossCount = 0;
        rng = new System.Random();
        playerRerollsLeft = playerRerollMax;

        // ��ȿ�� ����(��Ÿ��)
        if (turnsToPlay < 1) turnsToPlay = 1;

        // ���� ����: ���� ������ ���� ä���, �������� �������� ä�� �� ����
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

        // ��Ŀ ���� ���� �� ȣ�� (���� ǥ��, ��ο� ��å ��)
        if (jokerManager != null)
        {
            jokerManager.OnRoundStart(this);
        }
    }

    [Header("Reroll Config")]
    [SerializeField] private int playerRerollMax = 2; // �÷��̾� ���� �ִ� Ƚ��

    // UI ��ư ���ε�: ����/��/���� ����, ���� ��û
    public void OnClickRock() => PlayerMakesChoice((int)Choice.Rock);
    public void OnClickPaper() => PlayerMakesChoice((int)Choice.Paper);
    public void OnClickScissors() => PlayerMakesChoice((int)Choice.Scissors);
    public void OnClickRerollPlayerHand() => RerollPlayerHand();

    // �ܺο��� �� ���� �������� ������ �� ���. UI ���� �� ������ ��Ŀ ����� ����
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

    // ������ ��Ŀ ����� ��û
    public void RefreshJokerInfo()
    {
        if (jokerManager != null)
        {
            jokerManager.OnJokerToggled(this);
        }
    }

    // ���� ó��: ù �� ������ ����. ���� ��ü�� ������ϰ� ���� ������ ���ҽ�Ų��.
    private void RerollPlayerHand()
    {
        if (!roundActive)
        {
            Debug.Log("[Reroll] ���� ��Ȱ��");
            return;
        }
        if (currentTurn > 1 || playerHand.Count != playerHandSize)
        {
            Debug.Log("[Reroll] �̹� �� ���� - ù �� ������ ����");
            if (resultText != null) { resultText.text = "Result: Reroll unavailable (turn started)"; resultText.color = Color.white; }
            return;
        }
        if (playerRerollsLeft <= 0)
        {
            Debug.Log("[Reroll] ���� ����");
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

    // �÷��̾ ��ư�� ���� ����/��/������ �������� �� ȣ��Ǵ� �ٽ� ó��
    // ��� ���
    // - �÷��̾� ���п��� ������ ī�� ����
    // - AI�� �տ������� �Ǵ� �������� ī�� ����(Scout ��Ŀ ���¿� ���� �޶���)
    // - ��� ���� �� �⺻ ���� ���
    // - ���Ž� ��Ŀ ���� ����(ModifyScore) ����. (���� �±� ��� ExecuteTurnSettlementEffects �߰� ����)
    // - UI ���� �� ���� ����/���� �б�
    public void PlayerMakesChoice(int choiceIndex)
    {
        if (!roundActive) { Debug.Log("[PlayerMakesChoice] ���� ��Ȱ��"); return; }
        if (inputLocked) { Debug.Log("[PlayerMakesChoice] ó�� ��"); return; }
        if (currentTurn > MaxTurns) { Debug.Log("[PlayerMakesChoice] ��� �� ����"); return; }
        if (choiceIndex < 0 || choiceIndex > 2) { Debug.LogWarning("�߸��� choiceIndex"); return; }

        // �÷��̾� ���п��� ���ϴ� ī�� ���� �� ����
        Choice desired = (Choice)choiceIndex;
        int playerCardIndex = playerHand.FindIndex(c => c == desired);
        if (playerCardIndex == -1)
        {
            if (resultText != null)
            {
                resultText.text = $"Result: You have no {desired} card left.";
                resultText.color = Color.white;
            }
            Debug.Log("[PlayerMakesChoice] �ش� ī�� ����");
            return;
        }

        inputLocked = true;
        Choice playerChoice = desired;
        playerHand.RemoveAt(playerCardIndex);

        // AI ī�尡 ��� �����Ǿ����� ���� ����
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

        // AI ����: Scout ��Ŀ�� Ȱ���� ���� �տ�������, �ƴϸ� ����
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

        // ��� ���� �� �⺻ ���� ����
        Outcome outcome = JudgeOutcome(playerChoice, aiChoice);
        int baseScore = ScoreFor(outcome);

        // �±� ��� ���� ���������� ����: GameContext ���� �� ExecuteTurnSettlementEffects ȣ��
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

        // ���� ��� ī��Ʈ
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

        // ���� ���� ����: ������ �� ���� �Ѿ��ų�, AI ���а� �����Ǿ��� �� ����
        if (currentTurn > MaxTurns || aiHand.Count == 0)
        {
            roundActive = false;
            if (resultText != null)
                resultText.text += "\n(Round finished - Step3���� ���� ó��)";
            if (restartButton != null) restartButton.SetActive(true);
            Debug.Log("[Round] �� ���� - EndRound ����");
        }
        else
        {
            UpdateUI();
            inputLocked = false;
        }
    }

    // Scout ���� ����: AI ù ī��� ������ �÷��� �Ͽ� ���� ī�带 �̸� �����ش�.
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

    // HUD �� ���� �ؽ�Ʈ ���ſ�. JokerManager�� ShowInfo ȣ�⿡ ���� ���ȴ�.
    public void ShowInfo(string msg)
    {
        Debug.Log("[Info] " + msg);
        if (resultText != null)
        {
            resultText.text = msg;
            resultText.color = Color.white;
        }
    }

    // ���� ���� ���� ó��(���� ����, ���� ��) ? ���� Ȯ�� ����
    private void EndRound()
    {
        Debug.LogWarning("EndRound�� Step3���� ���� �����Դϴ�.");
    }

    // ���������� ��Ģ���� ��� ����
    private Outcome JudgeOutcome(Choice player, Choice ai)
    {
        if (player == ai) return Outcome.Draw;
        if ((player == Choice.Rock && ai == Choice.Scissors) ||
            (player == Choice.Paper && ai == Choice.Rock) ||
            (player == Choice.Scissors && ai == Choice.Paper))
            return Outcome.Win;
        return Outcome.Loss;
    }

    // ����� ���� �⺻ ���� ��ȯ
    private int ScoreFor(Outcome o)
    {
        switch (o)
        {
            case Outcome.Win: return WIN_POINTS;
            case Outcome.Draw: return DRAW_POINTS;
            default: return LOSS_POINTS;
        }
    }

    // HUD �ؽ�Ʈ���� ���� ���·� �����Ѵ�.
    private void UpdateUI(bool initial = false)
    {
        if (aiHandText != null)
        {
            var c = CountHand(aiHand);
            aiHandText.text = $"AI Hand: Rock x{c.rock} / Paper x{c.paper} / Scissors x{c.scissors}";
        }
        else Debug.LogWarning("aiHandText ���Ҵ�");

        if (playerHandText != null)
        {
            var p = CountHand(playerHand);
            playerHandText.text = $"Player Hand: Rock x{p.rock} / Paper x{p.paper} / Scissors x{p.scissors}";
        }
        else Debug.LogWarning("playerHandText ���Ҵ�");

        if (turnText != null) turnText.text = $"Turn {Mathf.Min(currentTurn, MaxTurns)} / {MaxTurns}"; else Debug.LogWarning("turnText ���Ҵ�");
        if (scoreText != null) scoreText.text = $"Score: {currentScore}"; else Debug.LogWarning("scoreText ���Ҵ�");

        if (outcomeSummaryText != null)
            outcomeSummaryText.text = $"W:{winCount} / D:{drawCount} / L:{lossCount}";
        else
            Debug.LogWarning("outcomeSummaryText ���Ҵ�");

        if (rerollText != null)
            rerollText.text = $"Rerolls: {playerRerollsLeft}";
        else
            Debug.LogWarning("rerollText ���Ҵ� (���� ���� Ƚ�� Text ���� �� ����)");

        if (resultText != null && initial)
        {
            resultText.text = "Result: -";
            resultText.color = Color.white;
        }
    }

    // ���� ����: ���� ������ ���� ä���, �������� �������� ä�� �� �����Ѵ�.
    private void GenerateHand(List<Choice> target, int size, int baseRock, int basePaper, int baseScissors)
    {
        target.Clear();
        if (size <= 0) return;

        // ���� ���� ���� ä��
        for (int i = 0; i < baseRock && target.Count < size; i++) target.Add(Choice.Rock);
        for (int i = 0; i < basePaper && target.Count < size; i++) target.Add(Choice.Paper);
        for (int i = 0; i < baseScissors && target.Count < size; i++) target.Add(Choice.Scissors);

        // �������� ���� ä��
        while (target.Count < size)
        {
            target.Add((Choice)rng.Next(0, 3));
        }

        // ����(Fisher-Yates)
        for (int i = target.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            (target[i], target[j]) = (target[j], target[i]);
        }
    }

    // ���� �� ī�� ����(����/��/����) ����
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
