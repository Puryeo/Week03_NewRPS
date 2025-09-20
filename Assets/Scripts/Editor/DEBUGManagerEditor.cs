#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Jokers;
using NewRPS.Debugging;
using System.Collections.Generic;

[CustomEditor(typeof(DEBUGManager))]
public class DEBUGManagerEditor : Editor
{
    private ReorderableList _enabledList;
    private SerializedProperty _enabledProp;
    private SerializedProperty _libraryProp;

    // Refs & options
    private SerializedProperty _jokerMgrProp;
    private SerializedProperty _gameMgrProp;
    private SerializedProperty _autoApplyOnPlayProp;

    // Debug hand props
    private SerializedProperty _dbgRocksProp;
    private SerializedProperty _dbgPapersProp;
    private SerializedProperty _dbgScissorsProp;
    private SerializedProperty _dbgShuffleProp;
    private SerializedProperty _dbgApplyOnStartProp;

    // Draft props
    private SerializedProperty _offeredProp;
    private SerializedProperty _pickedProp;
    private SerializedProperty _draftSeedProp;
    private SerializedProperty _offerCountProp;
    private SerializedProperty _pickCountProp;
    private SerializedProperty _minAnchorProp;
    private SerializedProperty _minPayoffProp;
    private SerializedProperty _minUtilityProp;
    private SerializedProperty _maxCatalystProp;
    private SerializedProperty _allowDupProp;

    private bool _showLibraryByArch = true;
    private bool _showDraft = true;

    private void OnEnable()
    {
        _enabledProp = serializedObject.FindProperty("enabledJokers");
        _libraryProp = serializedObject.FindProperty("library");
        _jokerMgrProp = serializedObject.FindProperty("jokerManager");
        _gameMgrProp = serializedObject.FindProperty("gameManager");
        _autoApplyOnPlayProp = serializedObject.FindProperty("autoApplyOnPlay");

        _dbgRocksProp = serializedObject.FindProperty("dbgPlayerRocks");
        _dbgPapersProp = serializedObject.FindProperty("dbgPlayerPapers");
        _dbgScissorsProp = serializedObject.FindProperty("dbgPlayerScissors");
        _dbgShuffleProp = serializedObject.FindProperty("dbgShuffle");
        _dbgApplyOnStartProp = serializedObject.FindProperty("dbgApplyOnStart");

        _offeredProp = serializedObject.FindProperty("offered");
        _pickedProp = serializedObject.FindProperty("picked");
        _draftSeedProp = serializedObject.FindProperty("draftSeed");
        _offerCountProp = serializedObject.FindProperty("offerCount");
        _pickCountProp = serializedObject.FindProperty("pickCount");
        _minAnchorProp = serializedObject.FindProperty("minAnchor");
        _minPayoffProp = serializedObject.FindProperty("minPayoff");
        _minUtilityProp = serializedObject.FindProperty("minUtility");
        _maxCatalystProp = serializedObject.FindProperty("maxCatalyst");
        _allowDupProp = serializedObject.FindProperty("allowDuplicate");

        _enabledList = new ReorderableList(serializedObject, _enabledProp, true, true, true, true);
        _enabledList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Enabled Jokers (apply order = top ¡æ bottom)");
        _enabledList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            rect.y += 2;
            var elem = _enabledProp.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), elem, GUIContent.none);
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var mgr = (DEBUGManager)target;

        // Refs & options
        EditorGUILayout.PropertyField(_jokerMgrProp);
        EditorGUILayout.PropertyField(_gameMgrProp);
        EditorGUILayout.PropertyField(_libraryProp);
        EditorGUILayout.PropertyField(_autoApplyOnPlayProp);

        if (_jokerMgrProp.objectReferenceValue == null)
            EditorGUILayout.HelpBox("Assign JokerManager (scene has exactly one).", MessageType.Error);
        if (_gameMgrProp.objectReferenceValue == null)
            EditorGUILayout.HelpBox("Assign GameManager.", MessageType.Warning);
        if (_libraryProp.objectReferenceValue == null)
            EditorGUILayout.HelpBox("Assign JokerLibrary to enable Library/Draft sections.", MessageType.Info);

        EditorGUILayout.Space(8);

        // Library by Archetype (sectioned toggles)
        if (mgr.library != null)
        {
            _showLibraryByArch = EditorGUILayout.Foldout(_showLibraryByArch, "Library (Sectioned by Archetype)", true);
            if (_showLibraryByArch)
            {
                DrawLibrarySection(mgr, JokerArchetype.Anchor, "Anchor");
                DrawLibrarySection(mgr, JokerArchetype.Payoff, "Payoff");
                DrawLibrarySection(mgr, JokerArchetype.Catalyst, "Catalyst");
                DrawLibrarySection(mgr, JokerArchetype.Utility, "Utility");
            }
        }

        EditorGUILayout.Space(6);
        _enabledList.DoLayoutList();

        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Apply Selection")) mgr.ApplySelection();
            if (GUILayout.Button("Enable All From Library")) mgr.EnableAllFromLibrary();
            if (GUILayout.Button("Disable All")) mgr.DisableAll();
        }

        EditorGUILayout.Space(12);
        // Draft section
        if (mgr.library != null)
        {
            _showDraft = EditorGUILayout.Foldout(_showDraft, "Draft (Offer 10 ¡æ Pick 5)", true);
            if (_showDraft)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(_draftSeedProp, new GUIContent("Seed"));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(_offerCountProp, new GUIContent("Offer Count"));
                        EditorGUILayout.PropertyField(_pickCountProp, new GUIContent("Pick Count"));
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(_minAnchorProp, new GUIContent("Min Anchor"));
                        EditorGUILayout.PropertyField(_minPayoffProp, new GUIContent("Min Payoff"));
                    }
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(_minUtilityProp, new GUIContent("Min Utility"));
                        EditorGUILayout.PropertyField(_maxCatalystProp, new GUIContent("Max Catalyst"));
                    }
                    EditorGUILayout.PropertyField(_allowDupProp, new GUIContent("Allow Duplicate"));

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Generate Offer"))
                        {
                            mgr.GenerateOffer();
                        }
                        if (GUILayout.Button("Commit Picked ¡æ Enabled"))
                        {
                            mgr.CommitPickedAsEnabled();
                        }
                    }

                    // Offered list display (read-only)
                    EditorGUILayout.LabelField($"Offered ({mgr.offered.Count})", EditorStyles.boldLabel);
                    using (new EditorGUI.IndentLevelScope())
                    {
                        for (int i = 0; i < mgr.offered.Count; i++)
                        {
                            var d = mgr.offered[i];
                            if (d == null) continue;
                            EditorGUILayout.LabelField($"- {GetLabel(d)} [{GetArch(d)} | w={Mathf.Max(1, d.weight)}]");
                        }
                    }

                    // Picked editor (limit pickCount)
                    EditorGUILayout.Space(6);
                    EditorGUILayout.LabelField($"Picked (max {mgr.pickCount})", EditorStyles.boldLabel);
                    for (int i = 0; i < mgr.offered.Count; i++)
                    {
                        var d = mgr.offered[i]; if (d == null) continue;
                        bool has = mgr.picked != null && mgr.picked.Contains(d);
                        bool next = EditorGUILayout.ToggleLeft($"{GetLabel(d)} [{GetArch(d)}]", has);
                        if (next != has)
                        {
                            if (next)
                            {
                                if (mgr.picked.Count < mgr.pickCount) mgr.picked.Add(d);
                                else EditorUtility.DisplayDialog("Pick Limit", $"You can pick up to {mgr.pickCount}.", "OK");
                            }
                            else
                            {
                                mgr.picked.Remove(d);
                            }
                            EditorUtility.SetDirty(mgr);
                        }
                    }
                    // Allow ordering of picked
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Picked Order (applied order)");
                    if (mgr.picked != null)
                    {
                        for (int i = 0; i < mgr.picked.Count; i++)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                EditorGUILayout.LabelField($"{i + 1}. {GetLabel(mgr.picked[i])}");
                                if (GUILayout.Button("¡ã", GUILayout.Width(28)))
                                {
                                    if (i > 0) Swap(mgr.picked, i, i - 1);
                                }
                                if (GUILayout.Button("¡å", GUILayout.Width(28)))
                                {
                                    if (i < mgr.picked.Count - 1) Swap(mgr.picked, i, i + 1);
                                }
                            }
                        }
                    }
                }
            }
        }

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Debug Player Hand", EditorStyles.boldLabel);
        using (new EditorGUI.IndentLevelScope())
        {
            EditorGUILayout.PropertyField(_dbgRocksProp, new GUIContent("Rocks"));
            EditorGUILayout.PropertyField(_dbgPapersProp, new GUIContent("Papers"));
            EditorGUILayout.PropertyField(_dbgScissorsProp, new GUIContent("Scissors"));
            EditorGUILayout.PropertyField(_dbgShuffleProp, new GUIContent("Shuffle"));
            EditorGUILayout.PropertyField(_dbgApplyOnStartProp, new GUIContent("Apply On Start"));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply Player Hand Override")) mgr.ApplyPlayerHandOverride(false);
                if (GUILayout.Button("Force Apply Now")) mgr.ApplyPlayerHandOverride(true);
            }
            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Hand override applies at runtime. Use 'Apply On Start' or run Play and press the buttons.", MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private static void Swap<T>(List<T> list, int a, int b)
    {
        var t = list[a]; list[a] = list[b]; list[b] = t;
    }

    private static string GetLabel(JokerData d) => string.IsNullOrEmpty(d.jokerName) ? d.name : d.jokerName;
    private static string GetArch(JokerData d)
    {
        var a = d.archetypes;
        if ((a & JokerArchetype.Anchor) != 0) return "Anchor";
        if ((a & JokerArchetype.Payoff) != 0) return "Payoff";
        if ((a & JokerArchetype.Catalyst) != 0) return "Catalyst";
        if ((a & JokerArchetype.Utility) != 0) return "Utility";
        return "None";
    }

    private void DrawLibrarySection(DEBUGManager mgr, JokerArchetype arch, string title)
    {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        using (new EditorGUI.IndentLevelScope())
        {
            for (int i = 0; i < mgr.library.jokers.Count; i++)
            {
                var d = mgr.library.jokers[i]; if (d == null) continue;
                if ((d.archetypes & arch) == 0) continue;
                bool isEnabled = Contains(mgr, d);
                bool next = EditorGUILayout.ToggleLeft(GetLabel(d), isEnabled);
                if (next != isEnabled)
                {
                    if (next) AddOnce(mgr, d);
                    else Remove(mgr, d);
                    EditorUtility.SetDirty(mgr);
                    Repaint();
                }
            }
        }
    }

    private static bool Contains(DEBUGManager mgr, JokerData d)
        => mgr.enabledJokers != null && mgr.enabledJokers.Contains(d);
    private static void AddOnce(DEBUGManager mgr, JokerData d)
    {
        if (mgr.enabledJokers == null) mgr.enabledJokers = new System.Collections.Generic.List<JokerData>();
        if (!mgr.enabledJokers.Contains(d)) mgr.enabledJokers.Add(d);
    }
    private static void Remove(DEBUGManager mgr, JokerData d)
    {
        if (mgr.enabledJokers == null) return;
        mgr.enabledJokers.Remove(d);
    }
}
#endif