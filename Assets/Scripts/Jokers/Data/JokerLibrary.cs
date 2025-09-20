using System.Collections.Generic;
using UnityEngine;
using Jokers;

[CreateAssetMenu(fileName = "JokerLibrary", menuName = "NewRPS/Joker Library", order = 10)]
public class JokerLibrary : ScriptableObject
{
    [Tooltip("디버그/테스트 대상으로 노출할 조커 목록")]
    public List<JokerData> jokers = new List<JokerData>();
}