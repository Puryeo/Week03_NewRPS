using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jokers
{
    // ��Ŀ �����͸� ǥ���ϴ� ScriptableObject
    // �⺻��(�ʱ� ������)�� ������ �� None/0/�� ���ڿ��� �ʱ�ȭ�Ѵ�.
    [CreateAssetMenu(fileName = "JokerData", menuName = "NewRPS/Joker Data", order = 1)]
    public class JokerData : ScriptableObject
    {
        [Header("�⺻ ����")]
        public string jokerName = "";      // ��Ŀ �̸�(��: All_In_Rock)
        [TextArea] public string description = "";

        [Header("����/������ �з� (Archetypes)")]
        public JokerArchetype archetypes = JokerArchetype.None; // Anchor/Payoff/Catalyst �� ���� ����

        [Header("���� ����ġ (Draft Offer)")]
        [Tooltip("���� ��÷�� ����ġ. ���� Ŭ���� ���õ� Ȯ���� ���� (�⺻ 1)")]
        public int weight = 1;

        [Header("�±� ���")]
        public List<JokerTag> tags = new List<JokerTag>();
    }
}
