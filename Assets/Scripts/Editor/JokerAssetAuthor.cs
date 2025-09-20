#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Jokers;
using System.Collections.Generic;

// Editor utility to author Phase A, B, C, and D JokerData assets per Joker-Task.txt
// Menu: Tools/NewRPS/Author Phase A/B/C/D Jokers
namespace NewRPS.Editor
{
    public static class JokerAssetAuthor
    {
        private const string RootFolder = "Assets/Jokers/PhaseA";
        private const string RootFolderB = "Assets/Jokers/PhaseB";
        private const string RootFolderC = "Assets/Jokers/PhaseC";
        private const string RootFolderD = "Assets/Jokers/PhaseD";

        [MenuItem("Tools/NewRPS/Author Phase A Jokers/Create All")]
        public static void CreateAll()
        {
            EnsureFolder();
            Create_Scissors_Collector();
            Create_Blade_of_Vengeance();
            Create_Glass_Scissors();
            Create_Sturdy_Barricade();
            Create_Landslide();
            Create_Force_Of_Nature();
            Create_Devils_Contract();
            Create_Paper_Dominance_Blueprint();
            AssetDatabase.SaveAssets();
            Debug.Log("[JokerAssetAuthor] Phase A assets created/updated in Assets/Jokers/PhaseA");
        }

        [MenuItem("Tools/NewRPS/Author Phase A Jokers/Scissors_Collector")] public static void Create_Scissors_Collector()
        {
            var data = ScriptableObject.CreateInstance<JokerData>();
            data.jokerName = "Scissors_Collector";
            data.description = "If played Scissors at least 2 times in round, +10 at round end.";
            data.archetypes = JokerArchetype.Anchor;
            data.tags = new List<JokerTag>
            {
                Timing(JokerTimingType.TurnSettlement),
                Condition_PlayedAtLeastCount(Choice.Scissors, 2),
                Condition_IsLastTurn(true),
                Effect_AddScoreDelta(+10),
            };
            SaveOrUpdate(data, $"{RootFolder}/Scissors_Collector.asset");
        }

        [MenuItem("Tools/NewRPS/Author Phase A Jokers/Blade_of_Vengeance")] public static void Create_Blade_of_Vengeance()
        {
            var data = ScriptableObject.CreateInstance<JokerData>();
            data.jokerName = "Blade_of_Vengeance";
            data.description = "If Loss with Scissors, +5.";
            data.archetypes = JokerArchetype.Anchor;
            data.tags = new List<JokerTag>
            {
                Timing(JokerTimingType.TurnSettlement),
                Condition_OutcomeIs(Outcome.Loss),
                Condition_PlayerChoiceIs(Choice.Scissors),
                Effect_AddScoreDelta(+5),
            };
            SaveOrUpdate(data, $"{RootFolder}/Blade_of_Vengeance.asset");
        }

        [MenuItem("Tools/NewRPS/Author Phase A Jokers/Glass_Scissors")] public static void Create_Glass_Scissors()
        {
            var data = ScriptableObject.CreateInstance<JokerData>();
            data.jokerName = "Glass_Scissors";
            data.description = "Win with Scissors +10; Loss with Scissors -15.";
            data.archetypes = JokerArchetype.Catalyst;
            data.tags = new List<JokerTag>
            {
                Timing(JokerTimingType.TurnSettlement),
                Effect_AddScoreDelta_Filtered(+10, Outcome.Win, Choice.Scissors),
                Effect_AddScoreDelta_Filtered(-15, Outcome.Loss, Choice.Scissors),
            };
            SaveOrUpdate(data, $"{RootFolder}/Glass_Scissors.asset");
        }

        [MenuItem("Tools/NewRPS/Author Phase A Jokers/Sturdy_Barricade")] public static void Create_Sturdy_Barricade()
        {
            var data = ScriptableObject.CreateInstance<JokerData>();
            data.jokerName = "Sturdy_Barricade";
            data.description = "Draw with Rock -> +5.";
            data.archetypes = JokerArchetype.Anchor;
            data.tags = new List<JokerTag>
            {
                Timing(JokerTimingType.TurnSettlement),
                Effect_AddScoreDelta_Filtered(+5, Outcome.Draw, Choice.Rock),
            };
            SaveOrUpdate(data, $"{RootFolder}/Sturdy_Barricade.asset");
        }

        [MenuItem("Tools/NewRPS/Author Phase A Jokers/Landslide")] public static void Create_Landslide()
        {
            var data = ScriptableObject.CreateInstance<JokerData>();
            data.jokerName = "Landslide";
            data.description = "If Rock used >=3 times, +20 at last turn.";
            data.archetypes = JokerArchetype.Payoff;
            data.tags = new List<JokerTag>
            {
                Timing(JokerTimingType.TurnSettlement),
                Condition_PlayedAtLeastCount(Choice.Rock, 3),
                Condition_IsLastTurn(true),
                Effect_AddScoreDelta(+20),
            };
            SaveOrUpdate(data, $"{RootFolder}/Landslide.asset");
        }

        [MenuItem("Tools/NewRPS/Author Phase A Jokers/Force_Of_Nature")] public static void Create_Force_Of_Nature()
        {
            var data = ScriptableObject.CreateInstance<JokerData>();
            data.jokerName = "Force_Of_Nature";
            data.description = "If Rock used 5 times, x2 at last turn (immediate).";
            data.archetypes = JokerArchetype.Catalyst;
            data.tags = new List<JokerTag>
            {
                Timing(JokerTimingType.TurnSettlement),
                Condition_PlayedAtLeastCount(Choice.Rock, 5),
                Condition_IsLastTurn(true),
                Effect_FinalScoreMultiplier(2),
            };
            SaveOrUpdate(data, $"{RootFolder}/Force_Of_Nature.asset");
        }

        [MenuItem("Tools/NewRPS/Author Phase A Jokers/Devils_Contract")] public static void Create_Devils_Contract()
        {
            var data = ScriptableObject.CreateInstance<JokerData>();
            data.jokerName = "Devils_Contract";
            data.description = "Win with Paper +20; Loss with Paper -30.";
            data.archetypes = JokerArchetype.Catalyst;
            data.tags = new List<JokerTag>
            {
                Timing(JokerTimingType.TurnSettlement),
                Effect_AddScoreDelta_Filtered(+20, Outcome.Win, Choice.Paper),
                Effect_AddScoreDelta_Filtered(-30, Outcome.Loss, Choice.Paper),
            };
            SaveOrUpdate(data, $"{RootFolder}/Devils_Contract.asset");
        }

        [MenuItem("Tools/NewRPS/Author Phase A Jokers/Blueprint_Paper_Dominance")] public static void Create_Paper_Dominance_Blueprint()
        {
            var data = ScriptableObject.CreateInstance<JokerData>();
            data.jokerName = "Paper_Dominance";
            data.description = "RoundStart: if Player has more Paper than AI, replace 2 AI Paper/Scissors cards to Rock.";
            data.archetypes = JokerArchetype.Utility;
            data.tags = new List<JokerTag>
            {
                Timing(JokerTimingType.RoundStart),
                Condition_PlayerHasMoreOfChoiceThanAI(Choice.Paper),
                // Use ShowInfo with stringValue as a hint for interpreter to call paper/scissors-filtered replace
                new JokerTag { category = JokerTagCategory.Effect, effectType = JokerEffectType.ReplaceAIRandomCardsToChoice, choiceParam = Choice.Rock, intValue = 2, stringValue = "Filter:PaperOrScissors" },
            };
            SaveOrUpdate(data, $"{RootFolder}/Paper_Dominance.asset");
        }

        [MenuItem("Tools/NewRPS/Author Phase B Jokers/Create All")]
        public static void CreateAll_PhaseB()
        {
            EnsureFolderB();
            Create_Geological_Survey();
            Create_Twin_Blades();
            Create_Final_Royalty();
            Create_Iron_Heart();
            Create_Confidential_Document();
            AssetDatabase.SaveAssets();
            Debug.Log("[JokerAssetAuthor] Phase B assets created/updated in Assets/Jokers/PhaseB");
        }

        [MenuItem("Tools/NewRPS/Author Phase B Jokers/Geological_Survey")] public static void Create_Geological_Survey()
        {
            var data = ScriptableObject.CreateInstance<JokerData>();
            data.jokerName = "Geological_Survey";
            data.description = "If Loss with Rock ¡æ reveal opponent next card.";
            data.archetypes = JokerArchetype.Utility;
            data.tags = new List<JokerTag>
            {
                Timing(JokerTimingType.TurnSettlement),
                Condition_OutcomeIs(Outcome.Loss),
                Condition_PlayerChoiceIs(Choice.Rock),
                Effect_RevealNextAICard(),
            };
            SaveOrUpdate(data, $"{RootFolderB}/Geological_Survey.asset");
        }

        [MenuItem("Tools/NewRPS/Author Phase B Jokers/Twin_Blades")] public static void Create_Twin_Blades()
        {
            var data = ScriptableObject.CreateInstance<JokerData>();
            data.jokerName = "Twin_Blades";
            data.description = "Two consecutive wins with Scissors ¡æ x2 at that turn end.";
            data.archetypes = JokerArchetype.Payoff;
            data.tags = new List<JokerTag>
            {
                Timing(JokerTimingType.TurnSettlement),
                Condition_ConsecutiveOutcomeWithChoiceIs(Outcome.Win, Choice.Scissors, 2),
                Effect_FinalScoreMultiplier(2),
            };
            SaveOrUpdate(data, $"{RootFolderB}/Twin_Blades.asset");
        }

        [MenuItem("Tools/NewRPS/Author Phase B Jokers/Final_Royalty")] public static void Create_Final_Royalty()
        {
            var data = ScriptableObject.CreateInstance<JokerData>();
            data.jokerName = "Final_Royalty";
            data.description = "If turnIndex==4 or 5 and Win with Paper, +20.";
            data.archetypes = JokerArchetype.Payoff;
            data.tags = new List<JokerTag>
            {
                Timing(JokerTimingType.TurnSettlement),
                // Allow both 4th and 5th turns by adding two TurnIndexIs tags (OR semantics handled in runtime)
                Condition_TurnIndexIs(4),
                Condition_TurnIndexIs(5),
                Condition_OutcomeIs(Outcome.Win),
                Condition_PlayerChoiceIs(Choice.Paper),
                Effect_AddScoreDelta(+20),
            };
            SaveOrUpdate(data, $"{RootFolderB}/Final_Royalty.asset");
        }

        [MenuItem("Tools/NewRPS/Author Phase B Jokers/Iron_Heart")] public static void Create_Iron_Heart()
        {
            var data = ScriptableObject.CreateInstance<JokerData>();
            data.jokerName = "Iron_Heart";
            data.description = "+30 at round end if no reroll used (last-turn settlement).";
            data.archetypes = JokerArchetype.Payoff;
            data.tags = new List<JokerTag>
            {
                Timing(JokerTimingType.TurnSettlement),
                Condition_IsLastTurn(true),
                Condition_RerollUsedEquals(0),
                Effect_AddScoreDelta(+30),
            };
            SaveOrUpdate(data, $"{RootFolderB}/Iron_Heart.asset");
        }

        [MenuItem("Tools/NewRPS/Author Phase B Jokers/Confidential_Document")] public static void Create_Confidential_Document()
        {
            var data = ScriptableObject.CreateInstance<JokerData>();
            data.jokerName = "Confidential_Document";
            data.description = "If Win with Paper ¡æ reveal opponent next card.";
            data.archetypes = JokerArchetype.Utility;
            data.tags = new List<JokerTag>
            {
                Timing(JokerTimingType.TurnSettlement),
                Condition_OutcomeIs(Outcome.Win),
                Condition_PlayerChoiceIs(Choice.Paper),
                Effect_RevealNextAICard(),
            };
            SaveOrUpdate(data, $"{RootFolderB}/Confidential_Document.asset");
        }

        // Phase C menus
        [MenuItem("Tools/NewRPS/Author Phase C Jokers/Create All")]
        public static void CreateAll_PhaseC()
        {
            EnsureFolderC();
            Create_Tailors_Pride();
            Create_Mass_Production_Scissors();
            Create_Ore_Vein();
            Create_Resource_Explosion();
            Create_Inflation();
            Create_Slave_Contract();
            AssetDatabase.SaveAssets();
            Debug.Log("[JokerAssetAuthor] Phase C assets created/updated in Assets/Jokers/PhaseC");
        }

        [MenuItem("Tools/NewRPS/Author Phase C Jokers/Tailors_Pride")] public static void Create_Tailors_Pride()
        {
            var data = ScriptableObject.CreateInstance<JokerData>();
            data.jokerName = "Tailors_Pride";
            data.description = "RoundPrepare: if player has >=4 Scissors, increase turnsToPlay by 1.";
            data.archetypes = JokerArchetype.Payoff;
            data.tags = new List<JokerTag>
            {
                Timing(JokerTimingType.RoundPrepare),
                Condition_PlayerHasAtLeastCountInHand(Choice.Scissors, 4),
                Effect_ModifyTurnsToPlayDelta(+1),
            };
            SaveOrUpdate(data, $"{RootFolderC}/Tailors_Pride.asset");
        }

        [MenuItem("Tools/NewRPS/Author Phase C Jokers/Mass_Production_Scissors")] public static void Create_Mass_Production_Scissors()
        {
            var data = ScriptableObject.CreateInstance<JokerData>();
            data.jokerName = "Mass_Production_Scissors";
            data.description = "RoundPrepare: Add +2 Scissors to both Player and AI hands.";
            data.archetypes = JokerArchetype.Catalyst;
            data.tags = new List<JokerTag>
            {
                Timing(JokerTimingType.RoundPrepare),
                Effect_AddCardsToPlayerHand(Choice.Scissors, 2),
                Effect_AddCardsToAIHand(Choice.Scissors, 2),
            };
            SaveOrUpdate(data, $"{RootFolderC}/Mass_Production_Scissors.asset");
        }

        [MenuItem("Tools/NewRPS/Author Phase C Jokers/Ore_Vein")] public static void Create_Ore_Vein()
        {
            var data = ScriptableObject.CreateInstance<JokerData>();
            data.jokerName = "Ore_Vein";
            data.description = "TurnStart: +2 per Rock in player's hand.";
            data.archetypes = JokerArchetype.Payoff;
            data.tags = new List<JokerTag>
            {
                Timing(JokerTimingType.TurnStart),
                Condition_PlayerHasAtLeastCountInHand(Choice.Rock, 1),
                Effect_AddScorePerPlayerHandCount(Choice.Rock, 2),
            };
            SaveOrUpdate(data, $"{RootFolderC}/Ore_Vein.asset");
        }

        [MenuItem("Tools/NewRPS/Author Phase C Jokers/Resource_Explosion")] public static void Create_Resource_Explosion()
        {
            var data = ScriptableObject.CreateInstance<JokerData>();
            data.jokerName = "Resource_Explosion";
            data.description = "RoundPrepare: Player +2 Rock, AI +1 random card.";
            data.archetypes = JokerArchetype.Catalyst;
            data.tags = new List<JokerTag>
            {
                Timing(JokerTimingType.RoundPrepare),
                Effect_AddCardsToPlayerHand(Choice.Rock, 2),
                Effect_AddRandomCardsToAIHand(1),
            };
            SaveOrUpdate(data, $"{RootFolderC}/Resource_Explosion.asset");
        }

        [MenuItem("Tools/NewRPS/Author Phase C Jokers/Inflation")] public static void Create_Inflation()
        {
            var data = ScriptableObject.CreateInstance<JokerData>();
            data.jokerName = "Inflation";
            data.description = "RoundPrepare: Player +2 Paper, AI +2 Rock.";
            data.archetypes = JokerArchetype.Catalyst;
            data.tags = new List<JokerTag>
            {
                Timing(JokerTimingType.RoundPrepare),
                Effect_AddCardsToPlayerHand(Choice.Paper, 2),
                Effect_AddCardsToAIHand(Choice.Rock, 2),
            };
            SaveOrUpdate(data, $"{RootFolderC}/Inflation.asset");
        }

        [MenuItem("Tools/NewRPS/Author Phase C Jokers/Slave_Contract")] public static void Create_Slave_Contract()
        {
            var data = ScriptableObject.CreateInstance<JokerData>();
            data.jokerName = "Slave_Contract";
            data.description = "RoundPrepare: Player +3 random cards, AI +1 random card.";
            data.archetypes = JokerArchetype.Catalyst;
            data.tags = new List<JokerTag>
            {
                Timing(JokerTimingType.RoundPrepare),
                Effect_AddRandomCardsToPlayerHand(3),
                Effect_AddRandomCardsToAIHand(1),
            };
            SaveOrUpdate(data, $"{RootFolderC}/Slave_Contract.asset");
        }

        // Phase D menus
        [MenuItem("Tools/NewRPS/Author Phase D Jokers/Create All")]
        public static void CreateAll_PhaseD()
        {
            EnsureFolderD();
            Create_Great_Earthquake();
            Create_Blank_Check();
            Create_Perfect_Tailoring();
            AssetDatabase.SaveAssets();
            Debug.Log("[JokerAssetAuthor] Phase D assets created/updated in Assets/Jokers/PhaseD");
        }

        [MenuItem("Tools/NewRPS/Author Phase D Jokers/Great_Earthquake")] public static void Create_Great_Earthquake()
        {
            var data = ScriptableObject.CreateInstance<JokerData>();
            data.jokerName = "Great_Earthquake";
            data.description = "If played Rock on turn 5, final total x-1.";
            data.archetypes = JokerArchetype.Catalyst;
            data.tags = new List<JokerTag>
            {
                new JokerTag { category = JokerTagCategory.Timing, timingType = JokerTimingType.RoundEnd },
                new JokerTag { category = JokerTagCategory.Condition, conditionType = JokerConditionType.ChoiceUsedOnTurnIndex, choiceParam = Choice.Rock, intValue = 5 },
                new JokerTag { category = JokerTagCategory.Effect, effectType = JokerEffectType.FinalTotalMultiplier, intValue = -1 },
            };
            SaveOrUpdate(data, $"{RootFolderD}/Great_Earthquake.asset");
        }

        [MenuItem("Tools/NewRPS/Author Phase D Jokers/Blank_Check")] public static void Create_Blank_Check()
        {
            var data = ScriptableObject.CreateInstance<JokerData>();
            data.jokerName = "Blank_Check";
            data.description = "If won with Paper at least 3 times, final total x2.";
            data.archetypes = JokerArchetype.Payoff;
            data.tags = new List<JokerTag>
            {
                new JokerTag { category = JokerTagCategory.Timing, timingType = JokerTimingType.RoundEnd },
                new JokerTag { category = JokerTagCategory.Condition, conditionType = JokerConditionType.WinWithChoiceAtLeastCount, choiceParam = Choice.Paper, intValue = 3 },
                new JokerTag { category = JokerTagCategory.Effect, effectType = JokerEffectType.FinalTotalMultiplier, intValue = 2 },
            };
            SaveOrUpdate(data, $"{RootFolderD}/Blank_Check.asset");
        }

        [MenuItem("Tools/NewRPS/Author Phase D Jokers/Perfect_Tailoring")] public static void Create_Perfect_Tailoring()
        {
            var data = ScriptableObject.CreateInstance<JokerData>();
            data.jokerName = "Perfect_Tailoring";
            data.description = "If all wins were with Scissors, final total x2.";
            data.archetypes = JokerArchetype.Payoff;
            data.tags = new List<JokerTag>
            {
                new JokerTag { category = JokerTagCategory.Timing, timingType = JokerTimingType.RoundEnd },
                new JokerTag { category = JokerTagCategory.Condition, conditionType = JokerConditionType.WinsOnlyWithChoice, choiceParam = Choice.Scissors },
                new JokerTag { category = JokerTagCategory.Effect, effectType = JokerEffectType.FinalTotalMultiplier, intValue = 2 },
            };
            SaveOrUpdate(data, $"{RootFolderD}/Perfect_Tailoring.asset");
        }

        // Helpers: ensure folders
        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Jokers")) AssetDatabase.CreateFolder("Assets", "Jokers");
            if (!AssetDatabase.IsValidFolder(RootFolder)) AssetDatabase.CreateFolder("Assets/Jokers", "PhaseA");
        }
        private static void EnsureFolderB()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Jokers")) AssetDatabase.CreateFolder("Assets", "Jokers");
            if (!AssetDatabase.IsValidFolder(RootFolderB)) AssetDatabase.CreateFolder("Assets/Jokers", "PhaseB");
        }
        private static void EnsureFolderC()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Jokers")) AssetDatabase.CreateFolder("Assets", "Jokers");
            if (!AssetDatabase.IsValidFolder(RootFolderC)) AssetDatabase.CreateFolder("Assets/Jokers", "PhaseC");
        }
        private static void EnsureFolderD()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Jokers")) AssetDatabase.CreateFolder("Assets", "Jokers");
            if (!AssetDatabase.IsValidFolder(RootFolderD)) AssetDatabase.CreateFolder("Assets/Jokers", "PhaseD");
        }

        private static void SaveOrUpdate(JokerData data, string path)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Jokers")) AssetDatabase.CreateFolder("Assets", "Jokers");
            var existing = AssetDatabase.LoadAssetAtPath<JokerData>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(data, path);
            }
            else
            {
                existing.jokerName = data.jokerName;
                existing.description = data.description;
                existing.archetypes = data.archetypes;
                existing.tags = data.tags;
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(data);
            }
        }

        // Tag builders (shared)
        private static JokerTag Timing(JokerTimingType t) => new JokerTag { category = JokerTagCategory.Timing, timingType = t };
        private static JokerTag Condition_OutcomeIs(Outcome o) => new JokerTag { category = JokerTagCategory.Condition, conditionType = JokerConditionType.OutcomeIs, outcomeParam = o };
        private static JokerTag Condition_PlayerChoiceIs(Choice c) => new JokerTag { category = JokerTagCategory.Condition, conditionType = JokerConditionType.PlayerChoiceIs, choiceParam = c };
        private static JokerTag Condition_PlayedAtLeastCount(Choice c, int n) => new JokerTag { category = JokerTagCategory.Condition, conditionType = JokerConditionType.PlayedAtLeastCount, choiceParam = c, intValue = n };
        private static JokerTag Condition_IsLastTurn(bool yes) => new JokerTag { category = JokerTagCategory.Condition, conditionType = JokerConditionType.IsLastTurn, intValue = yes ? 1 : 0 };
        private static JokerTag Condition_PlayerHasMoreOfChoiceThanAI(Choice c) => new JokerTag { category = JokerTagCategory.Condition, conditionType = JokerConditionType.PlayerHasMoreOfChoiceThanAI, choiceParam = c };
        private static JokerTag Condition_TurnIndexIs(int index) => new JokerTag { category = JokerTagCategory.Condition, conditionType = JokerConditionType.TurnIndexIs, intValue = index };
        private static JokerTag Condition_ConsecutiveOutcomeWithChoiceIs(Outcome o, Choice c, int n) => new JokerTag { category = JokerTagCategory.Condition, conditionType = JokerConditionType.ConsecutiveOutcomeWithChoiceIs, outcomeParam = o, choiceParam = c, intValue = n };
        private static JokerTag Condition_RerollUsedEquals(int n) => new JokerTag { category = JokerTagCategory.Condition, conditionType = JokerConditionType.RerollUsedEquals, intValue = n };
        // Phase C builders
        private static JokerTag Condition_PlayerHasAtLeastCountInHand(Choice c, int n) => new JokerTag { category = JokerTagCategory.Condition, conditionType = JokerConditionType.PlayerHasAtLeastCountInHand, choiceParam = c, intValue = n };
        private static JokerTag Effect_ModifyTurnsToPlayDelta(int delta) => new JokerTag { category = JokerTagCategory.Effect, effectType = JokerEffectType.ModifyTurnsToPlayDelta, intValue = delta };
        private static JokerTag Effect_AddCardsToPlayerHand(Choice c, int n) => new JokerTag { category = JokerTagCategory.Effect, effectType = JokerEffectType.AddCardsToPlayerHand, choiceParam = c, intValue = n };
        private static JokerTag Effect_AddCardsToAIHand(Choice c, int n) => new JokerTag { category = JokerTagCategory.Effect, effectType = JokerEffectType.AddCardsToAIHand, choiceParam = c, intValue = n };
        private static JokerTag Effect_AddScorePerPlayerHandCount(Choice c, int per) => new JokerTag { category = JokerTagCategory.Effect, effectType = JokerEffectType.AddScorePerPlayerHandCount, choiceParam = c, intValue = per };
        private static JokerTag Effect_AddRandomCardsToPlayerHand(int n) => new JokerTag { category = JokerTagCategory.Effect, effectType = JokerEffectType.AddRandomCardsToPlayerHand, intValue = n };
        private static JokerTag Effect_AddRandomCardsToAIHand(int n) => new JokerTag { category = JokerTagCategory.Effect, effectType = JokerEffectType.AddRandomCardsToAIHand, intValue = n };

        private static JokerTag Effect_AddScoreDelta(int v) => new JokerTag { category = JokerTagCategory.Effect, effectType = JokerEffectType.AddScoreDelta, intValue = v };
        private static JokerTag Effect_AddScoreDelta_Filtered(int v, Outcome o, Choice c) => new JokerTag { category = JokerTagCategory.Effect, effectType = JokerEffectType.AddScoreDelta, intValue = v, filterByOutcome = true, outcomeParam = o, filterByChoice = true, choiceParam = c };
        private static JokerTag Effect_FinalScoreMultiplier(int mul) => new JokerTag { category = JokerTagCategory.Effect, effectType = JokerEffectType.FinalScoreMultiplier, intValue = mul };
        private static JokerTag Effect_ReplaceAIRandomCardsToChoice(Choice to, int count) => new JokerTag { category = JokerTagCategory.Effect, effectType = JokerEffectType.ReplaceAIRandomCardsToChoice, choiceParam = to, intValue = count };
        private static JokerTag Effect_RevealNextAICard() => new JokerTag { category = JokerTagCategory.Effect, effectType = JokerEffectType.RevealNextAICard };

        // Phase D builder
        private static JokerTag Effect_FinalTotalMultiplier_RoundEnd(int mul) => new JokerTag { category = JokerTagCategory.Effect, effectType = JokerEffectType.FinalTotalMultiplier, intValue = mul };
    }
}
#endif
