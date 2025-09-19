using System;
using UnityEngine;

namespace Jokers
{
    [Serializable]
    public class JokerTag
    {
        public JokerTagCategory category;

        // One of the following is used depending on category
        public JokerTimingType timingType;     // if category == Timing
        public JokerConditionType conditionType; // if category == Condition
        public JokerEffectType effectType;     // if category == Effect

        // Parameters
        public Outcome outcomeParam;        // For OutcomeIs conditions
        public Choice choiceParam;          // For PlayerChoiceIs conditions
        public int intValue;                // For AddScoreDelta or boolean-like flags (1=true)
        public string stringValue;          // For ShowInfo text or misc
    }
}
