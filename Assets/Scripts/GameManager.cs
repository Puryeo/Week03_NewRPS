using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Jokers;
using NewRPS.Debugging;
using System; // for Action<>


// ������ �ٽ� �帧�� �����ϴ� �Ŵ��� ��ũ��Ʈ
// �� Ŭ������ ������ å������:
// - ���� �����ֱ� ����: ���� ����(StartRound) �� �� �� ó��(PlayerMakesChoice) �� ���� ����(EndRound ����)
// - ���� ���� �� ����: AI/�÷��̾� ���� ����Ʈ ����, ���� ����, ����
// - ����/��� ����: ���� ����, ��/��/�� ī��Ʈ, �� ��ȣ
// - ����(Reroll) ����: ù �� ���� �� ����, ��� Ƚ�� ���, UI �ݿ�
// - ��Ŀ �ý��� �� ȣ��: RoundPrepare(Phase C), RoundStart, TurnStart(Phase C), TurnSettlement, RoundEnd(Phase D)
// - UI ������Ʈ: ���� TextMeshProUGUI�� Restart ��ư ����
// �ܺο��� �ֿ� ��ȣ�ۿ�:
// - JokerManager: OnRoundPrepare(this) �� OnRoundStart(this) �� OnTurnStart(GameContext) �� ExecuteTurnSettlementEffects(GameContext) �� OnRoundEnd(this, RoundResult)
// - DEBUGManager: DebugTrySetPlayerHandCounts�� ���� �÷��̾� ���и� ����� �������̵�
// - UI ��ư: OnClickRock/Paper/Scissors/RerollPlayerHand/Restart�� ����� �Է� ����

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
    [SerializeField] private TextMeshProUGUI aiHandText;      // AI ���� ���� �ؽ�Ʈ
    [SerializeField] private TextMeshProUGUI playerHandText;  // �÷��̾� ���� ���� �ؽ�Ʈ
    [SerializeField] private TextMeshProUGUI turnText;        // ���� ��/�� �� �ؽ�Ʈ
    [SerializeField] private TextMeshProUGUI scoreText;       // ���� ���� �ؽ�Ʈ
    [SerializeField] private TextMeshProUGUI resultText;      // ���� �� ��� �ؽ�Ʈ(�߰� ���� �޽��� ����)
    [SerializeField] private TextMeshProUGUI outcomeSummaryText; // ��/��/�� ��� �ؽ�Ʈ
    [SerializeField] private TextMeshProUGUI rerollText;         // ���� ���� Ƚ�� �ؽ�Ʈ
    [SerializeField] private GameObject restartButton;        // ����� ��ư(�׻� Ȱ�� �䱸����)

    [Header("Managers")]
    public JokerManager jokerManager; // ��Ŀ �Ŵ���. ���� �Ű� ���� ������������ ����

    [Header("Flow")]
    [Tooltip("�� ���� �� �ڵ����� ���带 �������� ����(�巡��Ʈ �÷ο� ��� �� ������)")]
    [SerializeField] private bool startOnPlay = true;

    [Header("Turns")]
    [Tooltip("�̹� ���忡 ������ �� ��")]
    [SerializeField] private int turnsToPlay = 5; // �� ���� ���� ũ��� ������. ī�� ���� �� ���� ���� ����

    [Header("AI Hand Config")]
    [Tooltip("AI �ʱ� ���� ���")] [SerializeField] private int aiHandSize = 6; // AI ���� ũ��
    [Tooltip("���� ���� ���� AI���� Rock ���� �߰� ����")] [SerializeField] private int aiGuaranteedRocks = 1; // AI ���� ����: ����
    [Tooltip("���� ���� ���� AI���� Paper ���� �߰� ����")] [SerializeField] private int aiGuaranteedPapers = 1; // AI ���� ����: ��
    [Tooltip("���� ���� ���� AI���� Scissors ���� �߰� ����")] [SerializeField] private int aiGuaranteedScissors = 1; // AI ���� ����: ����

    [Header("Player Hand Config")]
    [Tooltip("�÷��̾� �ʱ� ���� ���")] [SerializeField] private int playerHandSize = 6; // �÷��̾� ���� ũ��
    [Tooltip("������ ����: �÷��̾� ���� ���� ������ ������� ����")] [SerializeField] private int playerGuaranteedRocks = 0;
    [SerializeField] private int playerGuaranteedPapers = 0;
    [SerializeField] private int playerGuaranteedScissors = 0;

    [Header("Result Colors")]
    [SerializeField] private Color winColor = Color.green;   // �¸� �ؽ�Ʈ ����
    [SerializeField] private Color drawColor = Color.yellow; // ���º� �ؽ�Ʈ ����
    [SerializeField] private Color lossColor = Color.red;    // �й� �ؽ�Ʈ ����

    // �⺻ ���� ��Ģ(���� ������������ ���̽�)
    private const int WIN_POINTS = 5;
    private const int DRAW_POINTS = 3;
    private const int LOSS_POINTS = 0;

    // ���� ����: ����, ���� ����, ����, �̷�, RNG ��
    private List<Choice> aiHand = new List<Choice>();       // AI ����(�� ����)
    private List<Choice> playerHand = new List<Choice>();   // �÷��̾� ����
    private int currentTurn = 0;                            // ���� �� �ε���(1���� ����)
    private int currentScore = 0;                           // ���� ����
    private System.Random rng;                              // ���� ������ AI ���� ���ÿ� ���(������ ���� ����)
    private bool roundActive = false;                       // ���� ���� ����
    private bool inputLocked = false;                       // �� ó�� �� �Է� ���
    private bool _playerActedThisRound = false;             // �÷��� �׼�(ī�� �Ҹ�) �߻� ����

    private int winCount = 0;                 // ���� �¸� ��
    private int drawCount = 0;                // ���� ���º� ��
    private int lossCount = 0;                // ���� �й� ��
    private int playerRerollsLeft = 0;        // ���� ���� Ƚ��
    private int playerRerollsUsed = 0;        // ����� ���� Ƚ��(���ǿ� ���)

    // ���� �����丮(�� ����� ���ؽ�Ʈ�� �����Ͽ� ����)
    private List<Choice> playerHistory = new List<Choice>();
    private List<Outcome> outcomeHistory = new List<Outcome>();

    // TurnSettlement���� ShowInfo�� ���޵� �޽����� ��� �ؽ�Ʈ�� �����̱� ���� ����
    private string _pendingInfoMsg;

    // UI ǥ��� �ִ� �ϼ�(�ּ� 1�� ����)
    private int MaxTurns => Mathf.Max(1, turnsToPlay);

    // ���� ���� 1ȸ ���� �� �̺�Ʈ
    private bool _roundEnded = false;
    public event Action<RoundResult> OnRoundEnded;

    // �ν����� �� ����(���� ����, �հ� �ʰ� ����)
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

        // AI ���� ���� �հ谡 ���� ũ�� �ʰ� �� Scissors��Paper��Rock ������ ����
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
        // �÷��̾� ���� ������ �̻��(���� ����)
    }

    // ����Ƽ ���� �� �ڵ����� ���� ���� (startOnPlay==true�� ����)
    private void Start()
    {
        if (startOnPlay)
        {
            StartRound();
        }
    }

    // �ܺ� �÷ο�(�巡��Ʈ Ȯ�� ��)���� ���带 ������ �� ȣ��
    public void StartRoundFromFlow()
    {
        if (roundActive) return;
        StartRound();
    }

    // ���带 �ʱ�ȭ�ϰ� ����/����/�̷� �ʱ�ȭ �� RoundPrepare �� RoundStart ������ ��Ŀ ���� ȣ��
    // Restart ��ư�� �׻� Ȱ�� �䱸���׿� ���� �Ҵ�
    private void StartRound()
    {
        // �⺻ ���� ����(Prepare�� ���� ���� ���� �������� �ǵ���)
        turnsToPlay = Mathf.Max(1, _initialTurnsToPlay);
        aiHandSize = Mathf.Max(1, _initialAIHandSize);
        playerHandSize = Mathf.Max(1, _initialPlayerHandSize);

        // ���� ���� �ʱ�ȭ
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
        _playerActedThisRound = false; // ù �׼� ������ �ʱ�ȭ
        if (turnsToPlay < 1) turnsToPlay = 1;
        _prepareTurnsDeltaApplied = 0; // Prepare ��Ÿ ���� �ʱ�ȭ
        _prepareTurnsDeltaPending = 0; _preparePassActive = false;
        _roundEnded = false; // ���� ���� ���� ����

        // ���� ����
        if (_initialHandOverride.HasValue)
        {
            // ����� �������̵尡 ������ ���, �÷��̾� �ڵ带 ���� �����ϰ� AI �ڵ�� ���� ����
            var ov = _initialHandOverride.Value;
            GenerateHand(aiHand, aiHandSize, aiGuaranteedRocks, aiGuaranteedPapers, aiGuaranteedScissors);
            // �÷��̾� �ڵ�� ���������� ����
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
            _initialHandOverride = null; // ��ȸ�� ���� �� ����
        }
        else
        {
            GenerateHand(aiHand, aiHandSize, aiGuaranteedRocks, aiGuaranteedPapers, aiGuaranteedScissors);
            GenerateHand(playerHand, playerHandSize, 0, 0, 0);
        }

        // Phase C: RoundPrepare ��(����/�ϼ� ���� ���� ���� �غ�)
        if (jokerManager != null)
        {
            jokerManager.OnRoundPrepare(this);
        }

        // Prepare ���� UI �ʱ�ȭ �ݿ�
        UpdateUI(initial: true);

        // Restart ��ư�� �׻� Ȱ��
        if (restartButton != null) restartButton.SetActive(true);

        // RoundStart ��(���� ǥ��, ��ο� ��å ��)
        if (jokerManager != null)
        {
            jokerManager.OnRoundStart(this);
        }

        // ���� ���� ����
        roundActive = true;
        inputLocked = false;
    }

    [Header("Restart Options")] 
    [Tooltip("����� �� ���� ���� ��°�� ���ε��Ͽ� ���� �ʱ�ȭ�� �� ���� (�⺻: ��)")]
    [SerializeField] private bool restartReloadScene = false;

    [Header("Reroll Config")] 
    [SerializeField] private int playerRerollMax = 2; // �÷��̾� ���� �ִ� Ƚ��(ù �� ���� ��� ����)

    // ����/�� ���� �ʱ� ������ ���(Prepare ��Ÿ ������ �����Ǳ� �� ��)
    private int _initialTurnsToPlay;
    private int _initialAIHandSize;
    private int _initialPlayerHandSize;

    private void Awake()
    {
        // �� �ε� ������ �ʱⰪ�� ����. Restart(��-���ε� ���)���� ������ ���
        _initialTurnsToPlay = turnsToPlay;
        _initialAIHandSize = aiHandSize;
        _initialPlayerHandSize = playerHandSize;
    }

    // UI ��ư �̺�Ʈ: ����/��/���� ����, ����, �����
    public void OnClickRock() => PlayerMakesChoice((int)Choice.Rock);
    public void OnClickPaper() => PlayerMakesChoice((int)Choice.Paper);
    public void OnClickScissors() => PlayerMakesChoice((int)Choice.Scissors);
    public void OnClickRerollPlayerHand() => RerollPlayerHand();
    public void OnClickRestart() => RestartRound();

    // �ܺο��� �� �� ���� �� ȣ��. �ʿ� �� ��Ŀ ���� �����
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

    // ��Ŀ ������ �޽��� �����(Scout ��)
    public void RefreshJokerInfo()
    {
        if (jokerManager != null)
        {
            jokerManager.OnJokerToggled(this);
        }
    }

    // �÷��̾� ���� ����. ù �� ���� ��, ���� ������ ���� ���� ����
    // ���� �� RoundStart �迭 ����(Paper_Dominance ��)�� �������ϱ� ���� OnJokerToggled ȣ��
    private void RerollPlayerHand()
    {
        if (!roundActive)
        {
            Debug.Log("[Reroll] ���� ��Ȱ��");
            return;
        }
        // ����: �÷��� �׼�(ī�� �Ҹ�) �������� ���
        if (_playerActedThisRound)
        {
            Debug.Log("[Reroll] �̹� �� ���� - �÷��� �׼� �������� ����");
            if (resultText != null) { resultText.text = "Result: Reroll unavailable (action started)"; resultText.color = Color.white; }
            return;
        }
        if (playerRerollsLeft <= 0)
        {
            Debug.Log("[Reroll] ���� ����");
            if (resultText != null) { resultText.text = "Result: No rerolls left"; resultText.color = Color.white; }
            return;
        }

        // ���� ���� ����� ������ ä ���� ���� ����(Prepare�� ������ ��� ����)
        int newSize = Mathf.Max(0, playerHand.Count);
        if (newSize == 0) newSize = playerHandSize; // ���� ��ġ
        GenerateHand(playerHand, newSize, 0, 0, 0);
        playerRerollsLeft--;
        playerRerollsUsed++;

        // ���� �� Prepare �迭�� �� �� ����(ModifyTurnsToPlayDelta)�� �����Ͽ� �ݿ�
        if (jokerManager != null)
        {
            jokerManager.OnRoundPrepareReapply(this); // ī�� �߰� ȿ���� ����, �� ���� ����
        }

        // RoundStart ���� ����(��ο� ��å�� ����). Paper_Dominance�� ���� ������ 1ȸ�� ���
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

    // �÷��̾ ��ư���� ����/��/������ �������� �� ���� ó���ϴ� �ٽ� ����
    // ó�� ����: �Է� ���� �� �÷��̾� ī�� �Ҹ� �� (TurnStart ��Ŀ ����) �� AI ���� �� ���� �� Settlement ��Ŀ ���� �� ����/��� �ݿ� �� UI ������Ʈ
    public void PlayerMakesChoice(int choiceIndex)
    {
        // �Է� �� ���� ����
        if (!roundActive) { Debug.Log("[PlayerMakesChoice] ���� ��Ȱ��"); return; }
        if (inputLocked) { Debug.Log("[PlayerMakesChoice] ó�� ��"); return; }
        if (currentTurn > MaxTurns) { Debug.Log("[PlayerMakesChoice] ��� �� ����"); return; }
        if (choiceIndex < 0 || choiceIndex > 2) { Debug.LogWarning("�߸��� choiceIndex"); return; }

        // �÷��̾� ���п��� ���� ī�� �Ҹ�
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
        _playerActedThisRound = true; // ù �׼� ����� �� ���� ���� �Ұ�
        Choice playerChoice = desired;
        playerHand.RemoveAt(playerCardIndex);

        // TurnStart ��: ��� ���� ���� ���� ��� ���� ���� �� ���ݿ�
        if (jokerManager != null)
        {
            var startCtx = new Jokers.GameContext
            {
                gameManager = this,
                playerChoice = Choice.None,     // TurnStart������ ���� ����
                outcome = Outcome.None,         // TurnStart������ ���� ����
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
                currentScore += startCtx.scoreDelta; // ������ ������ ��� ����
                RPS.RPSLog.Event("Turn", "StartDelta", $"turn={currentTurn}, add={startCtx.scoreDelta}, total={currentScore}");
            }
        }

        // AI ī�尡 ��� �����Ǿ����� ���� ���� �˻� ������ AI ���� �б�
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

        // AI ����: ��ȸ�� �յ�ο� ���� �Ǵ� ���� ��å �ݿ�
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

        // ���������� ���� �� �⺻ ���� ����
        Outcome outcome = JudgeOutcome(playerChoice, aiChoice);
        int baseScore = ScoreFor(outcome);

        // ���� �����丮�� �̹� �� �߰�(���ؽ�Ʈ ���忡 ����)
        playerHistory.Add(playerChoice);
        outcomeHistory.Add(outcome);

        // TurnSettlement ���������� ����: ��Ŀ ����/ȿ���� ���� ����/��� ���� ��
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
                scoreDelta = baseScore,         // Settlement ���� �� ���̽� ������ �ʱ�ȭ
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

        // ��/��/�� ī��Ʈ ����
        switch (outcome)
        {
            case Outcome.Win: winCount++; break;
            case Outcome.Draw: drawCount++; break;
            case Outcome.Loss: lossCount++; break;
        }

        // �α� �� �ܿ� ���� ī��Ʈ ���
        var remainAI = CountHand(aiHand);
        var remainP = CountHand(playerHand);
        Debug.Log($"[Turn {currentTurn}] P:{playerChoice} vs AI:{aiChoice} => {outcome} (+{turnDelta}) | Remain AI R{remainAI.rock}P{remainAI.paper}S{remainAI.scissors} | Player R{remainP.rock}P{remainP.paper}S{remainP.scissors} | Total={currentScore}");
        RPS.RPSLog.Event("Turn", "Resolve", $"turn={currentTurn}, player={playerChoice}, ai={aiChoice}, outcome={outcome}, delta={turnDelta}, total={currentScore}, aiRemain=R{remainAI.rock}P{remainAI.paper}S{remainAI.scissors}, pRemain=R{remainP.rock}P{remainP.paper}S{remainP.scissors}");

        // ��� �ؽ�Ʈ �� Settlement �޽��� ���� ���
        UpdateUI();
        if (resultText != null)
        {
            resultText.text = $"Result: {outcome} (You: {playerChoice} vs AI: {aiChoice}) +{turnDelta}";
            resultText.color = outcome == Outcome.Win ? winColor : (outcome == Outcome.Draw ? drawColor : lossColor);
            if (!string.IsNullOrEmpty(_pendingInfoMsg))
            {
                resultText.text += "\n" + _pendingInfoMsg; // Geological_Survey �� �޽��� �ΰ� ���
                _pendingInfoMsg = null;                      // �� �Ͽ� 1ȸ �Һ� �� �ʱ�ȭ
            }
        }

        // ���� ������ ����
        currentTurn++;

        // ���� ���� ����: ������ �� �� �ʰ� �Ǵ� AI ���� ����
        if (currentTurn > MaxTurns || aiHand.Count == 0)
        {
            roundActive = false;
            if (resultText != null)
                resultText.text += "\n(Round finished)"; // ���� ǥ�⸸ ����(UI�� �ļ���)
            if (restartButton != null) restartButton.SetActive(true); // �׻� Ȱ�� ����
            Debug.Log("[Round] �� ���� - EndRound ó��");
            EndRound();
        }
        else
        {
            UpdateUI();
            inputLocked = false; // ���� �Է� ���
        }
    }

    // AI ���� ù ī�� �̸�����(Scout ��). AI ���� ������� �⺻�� Rock ��ȯ
    public Choice PeekAIFront()
    {
        if (aiHand != null && aiHand.Count > 0) return aiHand[0];
        return Choice.Rock;
    }

    // AI�� ���������� �÷����� �S�� �ش��ϴ� ī�� �̸�����. ���� ����� ��ȹ �i ���� �������� �ε��� ���
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

    // TurnSettlement���� ShowInfo�� ���޵� ������ �� �� ��� �Ʒ��� �̾ �����ֱ� ���� ���۸�
    public void ShowInfo(string msg)
    {
        Debug.Log("[Info] " + msg);
        _pendingInfoMsg = msg; // PlayerMakesChoice ���̿��� UI�� ���
    }

    // ���� ���� ���� ó��(����/��Ÿ ��) - �鿣�常 ����, UI�� �ļ���
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

        // Phase D: RoundEnd ��Ŀ ����������(���� ���� ���� ����). �̺�Ʈ/�α� ���� ȣ��
        if (jokerManager != null)
        {
            try { jokerManager.OnRoundEnd(this, result); }
            catch (Exception ex) { Debug.LogError($"[RoundEnd] Joker pipeline error: {ex}"); }
        }

        // ��Ŀ ���� �� ���� ������ ���� ���¿� �ݿ��ϰ� UI ����
        currentScore = result.totalScore;
        UpdateUI();

        // ���� ������ �α� ���
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

    // UI���� ����� ��ư�� ������ �� ���� �����
    public void RestartRound()
    {
        Debug.Log("[Round] Restart requested by UI");
        if (restartReloadScene)
        {
            // �� ���ε带 ���� ���� �ʱ�ȭ(������Ʈ/���� ���� ����). Build Settings�� �� ��� �ʿ�
            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(scene.buildIndex);
            return;
        }

        // ��-���ε� ���: ����/���� ���� �ʱ�ȭ �� ���� �����
        StartRound();
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

    // Prepare �ܰ迡�� �ϼ� ���� ���� ������
    private int _prepareTurnsDeltaApplied = 0;   // ������� Prepare ȿ���� ����� �ϼ� ��Ÿ(���� ��)
    private int _prepareTurnsDeltaPending = 0;   // �̹� Prepare �н����� ��� ���� ��Ÿ ��
    private bool _preparePassActive = false;

    // HUD �ؽ�Ʈ�� ���� ���·� ����. ���� ���� �� initial=true�� �⺻ �ؽ�Ʈ/�� �ʱ�ȭ
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

    // ���� ����: ���� ������ ���� ä��� �������� �������� ä�� �� Fisher-Yates ����
    private void GenerateHand(List<Choice> target, int size, int baseRock, int basePaper, int baseScissors)
    {
        target.Clear();
        if (size <= 0) return;

        // ���� ���� ä���
        for (int i = 0; i < baseRock && target.Count < size; i++) target.Add(Choice.Rock);
        for (int i = 0; i < basePaper && target.Count < size; i++) target.Add(Choice.Paper);
        for (int i = 0; i < baseScissors && target.Count < size; i++) target.Add(Choice.Scissors);

        // ������ ���� ä���
        while (target.Count < size)
        {
            target.Add((Choice)rng.Next(0, 3));
        }

        // Fisher-Yates ���÷� ���� ����(������ RNG ���)
        for (int i = target.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            (target[i], target[j]) = (target[j], target[i]);
        }
    }

    // ���� �� Rock/Paper/Scissors ���� ����
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

    // ��Ŀ RoundStart ����/ȿ������ ����ϴ� ���۵�
    public int CountAIChoice(Choice c)
    {
        int cnt = 0; for (int i = 0; i < aiHand.Count; i++) if (aiHand[i] == c) cnt++; return cnt;
    }
    public int CountPlayerChoice(Choice c)
    {
        int cnt = 0; for (int i = 0; i < playerHand.Count; i++) if (playerHand[i] == c) cnt++; return cnt;
    }

    // AI ������ ������ �ε����� choice�� ����(�ߺ� ���õ� �� ����). Paper_Dominance���� �Ʒ� ���� ������ ���
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

    // Paper_Dominance�� ���� ����: �ĺ��� Paper/Scissors�� �����ϰ� �ߺ� ���� ��Ȯ�� ���� ���� ����
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
            candidates.RemoveAt(last); // �ߺ� ����
            aiHand[idx] = Choice.Rock;
            changed++;
        }
        UpdateUI();
        return changed;
    }

    // ����� ����: �÷��̾� ���и� ���� ������ ���� ����(�� �S �� ����). �ʿ� �� ����(force) ���� ����
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
        // ���� ������ �ϰ����� ���� ���� ����� ����ȭ(����)
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

    // TurnSettlement ���ǿ��� ����� ���� ��� Ƚ�� ����
    public int GetPlayerRerollsUsed() => playerRerollsUsed;

    // Phase C RoundPrepare ȿ������ ����ϴ� ���� API��
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

    // Prepare �ܰ� ���򰡿� API: JokerManager���� �н� ����/����/Ŀ�� ȣ��
    public void BeginPreparePass(bool reapply)
    {
        _preparePassActive = true;
        _prepareTurnsDeltaPending = 0; // �� �н��� 0���� ����
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

    // ����: �� ���� ���� �� �÷��̾� �ʱ� �ڵ带 ����(����� ����). StartRound���� �켱 �ݿ� �� null�� ����
    private (int r, int p, int s, bool shuffle)? _initialHandOverride;

    public void SetInitialHandOverride(int rock, int paper, int scissors, bool shuffle)
    {
        if (rock < 0) rock = 0; if (paper < 0) paper = 0; if (scissors < 0) scissors = 0;
        _initialHandOverride = (rock, paper, scissors, shuffle);
    }

    // RoundEnd/authoring���� ���� �����丮 ���� �뵵
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
