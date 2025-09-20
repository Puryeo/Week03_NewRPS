#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Jokers;
using NewRPS.Debugging;

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

        // Validation/warnings
        if (_jokerMgrProp.objectReferenceValue == null)
            EditorGUILayout.HelpBox("Assign JokerManager (scene has exactly one).", MessageType.Error);
        if (_gameMgrProp.objectReferenceValue == null)
            EditorGUILayout.HelpBox("Assign GameManager.", MessageType.Warning);
        if (_libraryProp.objectReferenceValue == null)
            EditorGUILayout.HelpBox("Assign JokerLibrary to enable Library Jokers section.", MessageType.Info);

        EditorGUILayout.Space(8);
        // Library list with checkboxes
        if (mgr.library != null)
        {
            EditorGUILayout.LabelField("Library Jokers", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                for (int i = 0; i < mgr.library.jokers.Count; i++)
                {
                    var d = mgr.library.jokers[i];
                    if (d == null) continue;
                    bool isEnabled = Contains(mgr, d);
                    bool next = EditorGUILayout.ToggleLeft(d.jokerName == "" ? d.name : d.jokerName, isEnabled);
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