namespace Jokers
{
    // Tag categories
    public enum JokerTagCategory { Timing, Condition, Effect }

    // Timing tags
    public enum JokerTimingType { RoundStart, TurnSettlement }

    // Condition tags
    public enum JokerConditionType { None, OutcomeIs, PlayerChoiceIs }

    // Effect tags
    public enum JokerEffectType { None, AddScoreDelta, ForceAIDrawFromFront, ShowInfo }
}
