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
    public enum JokerConditionType { None, OutcomeIs, PlayerChoiceIs }

    // ȿ�� �±�: ���� ����, ���� ���, AI ��ο� ��å ���� �� ��ü ȿ�� ����
    public enum JokerEffectType { None, AddScoreDelta, ForceAIDrawFromFront, ShowInfo }
}
