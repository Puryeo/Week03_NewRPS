using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jokers
{
    // 조커 데이터를 표현하는 ScriptableObject
    // 기본값(초기 생성시)은 가능한 한 None/0/빈 문자열로 초기화한다.
    [CreateAssetMenu(fileName = "JokerData", menuName = "NewRPS/Joker Data", order = 1)]
    public class JokerData : ScriptableObject
    {
        [Header("기본 정보")]
        public string jokerName = "";      // 조커 이름(예: All_In_Rock)
        [TextArea] public string description = "";

        [Header("스폰/디자인 분류 (Archetypes)")]
        public JokerArchetype archetypes = JokerArchetype.None; // Anchor/Payoff/Catalyst 등 조합 가능

        [Header("스폰 가중치 (Draft Offer)")]
        [Tooltip("오퍼 추첨시 가중치. 값이 클수록 선택될 확률이 높음 (기본 1)")]
        public int weight = 1;

        [Header("태그 목록")]
        public List<JokerTag> tags = new List<JokerTag>();
    }
}
