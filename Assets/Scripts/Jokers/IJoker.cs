using UnityEngine;

namespace Jokers
{
    public interface IJoker
    {
        // ǥ�ÿ� �̸�
        string Name { get; }

        // ���� ���� �� ȣ�� (�ʿ� �� ���� ǥ�� ��)
        void OnRoundStart(GameManager gameManager);

        // ���� ��� �� ȣ��: baseScore�� ������ ��ȯ. �ʿ��� ��� currentTotalScore�� ref�� ���� ����(��: ��ü �ʱ�ȭ).
        int ApplyScoreModification(int baseScore, ref int currentTotalScore, Choice playerChoice, Outcome outcome);
    }
}
