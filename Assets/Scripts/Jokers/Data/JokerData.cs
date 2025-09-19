using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jokers
{
    // ��Ŀ �����͸� ǥ���ϴ� ScriptableObject
    // �⺻��(�ʱ� ������)�� ī�װ��� �����ϰ� ��� None/0/�� ���ڿ��� �ʱ�ȭ�Ѵ�.
    [CreateAssetMenu(fileName = "JokerData", menuName = "NewRPS/Joker Data", order = 1)]
    public class JokerData : ScriptableObject
    {
        [Header("�⺻ ����")]
        public string jokerName = "";      // ���� �̸��� �����ϰ� ���ߴ� ���� ���� (��: All_In_Rock)
        [TextArea] public string description = "";

        [Header("�±� ���")]
        public List<JokerTag> tags = new List<JokerTag>();
    }
}
