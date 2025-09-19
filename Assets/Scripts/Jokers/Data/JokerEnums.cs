// ��Ŀ �ý��ۿ��� ���Ǵ� �±� ������ ����
// �� ������ �±� ��� ������ �ֵ� ��Ŀ ������ �ٽ� �з��� ��´�.
// �� ���� �±װ� ���� �۵��ϴ���(����), � ���ǿ��� �۵��ϴ���(����), ������ �ϴ���(ȿ��)�� ǥ���Ѵ�.

namespace Jokers
{
    // �±� ��з�: ����(Timing), ����(Condition), ȿ��(Effect)
    public enum JokerTagCategory { Timing, Condition, Effect }

    // ���� �±�: �������, �ϰ�� �� ���� Ÿ�̹� ����
    // None�� �ξ� �ν����Ϳ��� ������� �ʴ� ���� ��������� ������ �� �ֵ��� �Ѵ�.
    public enum JokerTimingType { None, RoundStart, TurnSettlement }

    // ���� �±�: ���/���� � ����� �ߵ� ����
    public enum JokerConditionType
    {
        None,
        OutcomeIs,
        PlayerChoiceIs,
        // ���� �����丮/�� ��Ÿ ��� Ȯ�� ����
        PlayedAtLeastCount,          // choiceParam�� ���忡�� intValue �̻� ���
        ConsecutiveDrawWithChoiceIs, // choiceParam�� ���� intValue�� ��� (�����丮 �� ����)
        IsLastTurn,                  // ������ �� ���� (intValue==1�� �� true)
        // �ڵ� �� ��� ����
        PlayerHasMoreOfChoiceThanAI  // RoundStart ��� ���: choiceParam�� ������ Player > AI
    }

    // ȿ�� �±�: ���� ����, ���� ���, AI ��ο� ��å ���� �� ��ü ȿ�� ����
    public enum JokerEffectType
    {
        None,
        AddScoreDelta,
        ForceAIDrawFromFront,
        ShowInfo,
        // ���� ���� �� ���� ���� ��� ����(���� ���� �� ��� ����)
        FinalScoreMultiplier,
        // AI �� ����: ������ count���� choiceParam���� ����
        ReplaceAIRandomCardsToChoice
    }

    // ����/������ �з��� ��Ÿ �±�(��ŰŸ��). ��Ŀ �����Ϳ� �����Ͽ� �з�/���͸��� Ȱ���Ѵ�.
    // �÷��� ���� ����: Anchor | Payoff ��
    [System.Flags]
    public enum JokerArchetype
    {
        None = 0,
        Anchor = 1 << 0,
        Payoff = 1 << 1,
        Catalyst = 1 << 2,
        Utility = 1 << 3,
        Risk = 1 << 4,
    }
}
