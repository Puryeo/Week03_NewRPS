using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jokers
{
    // 태그 한 항목을 표현하는 데이터 구조
    // category와 각 type 필드 조합으로 태그의 의미를 정의하고, 부가 파라미터로 세부 값을 제공한다.
    // 이 클래스는 ScriptableObject(JokerData)의 tags 목록에 포함되어 사용된다.
    [Serializable]
    public class JokerTag
    {
        // 태그의 대분류 (시점/조건/효과)
        public JokerTagCategory category;

        // category가 Timing/Condition/Effect일 때 사용하는 세부 타입
        public JokerTimingType timingType;       // 시점 태그일 때 사용
        public JokerConditionType conditionType; // 조건 태그일 때 사용
        public JokerEffectType effectType;       // 효과 태그일 때 사용

        // 공통 파라미터
        // outcomeParam, choiceParam은 조건 비교에 사용하거나, 효과에 필터로 사용할 수 있다.
        public Outcome outcomeParam;        // OutcomeIs 비교용 또는 효과 필터용
        public Choice choiceParam;          // PlayerChoiceIs 비교용 또는 효과 필터용
        public int intValue;                // AddScoreDelta 등 정수형 파라미터, 또는 플래그(1=true)
        public string stringValue;          // ShowInfo 등 문자열 파라미터

        // 효과 태그에서만 선택적으로 사용하는 필터 옵션
        // true일 때, 해당 효과는 지정된 값(Outcome/Choice)에 일치하는 경우에만 적용된다.
        public bool filterByOutcome;        // 효과 적용을 특정 결과에 한정
        public bool filterByChoice;         // 효과 적용을 특정 선택(가위바위보)에 한정

        // Phase B: optional flags for new conditions
        public bool useBetweenRange; // TurnIndexIs: use intValue as exact when false; when true use intValue (min) and stringValue as max (parse)
        public string extra;         // For future extensibility
    }
}
