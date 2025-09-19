using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jokers
{
    // 조커 데이터를 표현하는 ScriptableObject
    // 기본값(초기 설정값)은 카테고리를 제외하고 모두 None/0/빈 문자열로 초기화한다.
    [CreateAssetMenu(fileName = "JokerData", menuName = "NewRPS/Joker Data", order = 1)]
    public class JokerData : ScriptableObject
    {
        [Header("기본 정보")]
        public string jokerName = "";      // 에셋 이름과 동일하게 맞추는 것을 권장 (예: All_In_Rock)
        [TextArea] public string description = "";

        [Header("태그 목록")]
        public List<JokerTag> tags = new List<JokerTag>();
    }
}
