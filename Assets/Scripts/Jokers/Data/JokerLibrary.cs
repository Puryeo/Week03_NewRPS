using System.Collections.Generic;
using UnityEngine;
using Jokers;

[CreateAssetMenu(fileName = "JokerLibrary", menuName = "NewRPS/Joker Library", order = 10)]
public class JokerLibrary : ScriptableObject
{
    [Tooltip("�����/�׽�Ʈ ������� ������ ��Ŀ ���")]
    public List<JokerData> jokers = new List<JokerData>();
}