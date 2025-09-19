using System;
using UnityEngine;

namespace Jokers
{
    // �±� �� �׸��� ǥ���ϴ� ������ ����
    // category�� �� type �ʵ� �������� �±��� �ǹ̸� �����ϰ�, �ΰ� �Ķ���ͷ� ���� ���� �����Ѵ�.
    // �� Ŭ������ ScriptableObject(JokerData)�� tags ��Ͽ� ���ԵǾ� ���ȴ�.
    [Serializable]
    public class JokerTag
    {
        // �±��� ��з� (����/����/ȿ��)
        public JokerTagCategory category;

        // category�� Timing/Condition/Effect�� �� ����ϴ� ���� Ÿ��
        public JokerTimingType timingType;       // ���� �±��� �� ���
        public JokerConditionType conditionType; // ���� �±��� �� ���
        public JokerEffectType effectType;       // ȿ�� �±��� �� ���

        // ���� �Ķ����
        // outcomeParam, choiceParam�� ���� �񱳿� ����ϰų�, ȿ���� ���ͷ� ����� �� �ִ�.
        public Outcome outcomeParam;        // OutcomeIs �񱳿� �Ǵ� ȿ�� ���Ϳ�
        public Choice choiceParam;          // PlayerChoiceIs �񱳿� �Ǵ� ȿ�� ���Ϳ�
        public int intValue;                // AddScoreDelta �� ������ �Ķ����, �Ǵ� �÷���(1=true)
        public string stringValue;          // ShowInfo �� ���ڿ� �Ķ����

        // ȿ�� �±׿����� ���������� ����ϴ� ���� �ɼ�
        // true�� ��, �ش� ȿ���� ������ ��(Outcome/Choice)�� ��ġ�ϴ� ��쿡�� ����ȴ�.
        public bool filterByOutcome;        // ȿ�� ������ Ư�� ����� ����
        public bool filterByChoice;         // ȿ�� ������ Ư�� ����(����������)�� ����
    }
}
