using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ShelterCommand.Editor
{
    /// <summary>
    /// Unity Editor window for authoring ExplorationDialogue trees.
    ///
    /// Open via: Window → ShelterCommand → Dialogue Editor
    ///
    /// Left pane  — searchable list of all ExplorationDialogue assets.
    /// Right pane — edit selected node: speaker, text, choices, events, follow-up calls.
    ///
    /// Follow-up RadioCallEvent parameters are edited inline — no need to open the asset.
    /// </summary>
    public class DialogueEditorTool : EditorWindow
    {
        // ── Menu item ─────────────────────────────────────────────────────────────
        [MenuItem("Window/ShelterCommand/Dialogue Editor")]
        public static void Open() => GetWindow<DialogueEditorTool>("Dialogue Editor");

        // ── Layout constants ──────────────────────────────────────────────────────
        private const float  ListWidth       = 240f;
        private const float  PanelPadding    = 8f;
        private static readonly Color HeaderColor   = new Color(0.15f, 0.25f, 0.15f);
        private static readonly Color SelectedColor = new Color(0.22f, 0.45f, 0.22f);
        private static readonly Color ChoiceColor   = new Color(0.12f, 0.18f, 0.28f);
        private static readonly Color CallColor     = new Color(0.18f, 0.18f, 0.28f);

        // ── State ─────────────────────────────────────────────────────────────────
        private List<ExplorationDialogue> allDialogues   = new List<ExplorationDialogue>();
        private ExplorationDialogue       selectedNode;
        private Vector2                   listScroll;
        private Vector2                   detailScroll;
        private string                    searchFilter   = "";

        // ── Foldout state ─────────────────────────────────────────────────────────
        private bool[] choiceFoldouts = System.Array.Empty<bool>();

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void OnEnable()  => Refresh();
        private void OnFocus()   => Refresh();

        // ── GUI ───────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            DrawToolbar();

            EditorGUILayout.BeginHorizontal();
            DrawList();
            DrawDetail();
            EditorGUILayout.EndHorizontal();
        }

        // ── Toolbar ───────────────────────────────────────────────────────────────

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("+ New Dialogue", EditorStyles.toolbarButton, GUILayout.Width(120)))
                CreateNewDialogue();

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
                Refresh();

            GUILayout.FlexibleSpace();
            GUILayout.Label("Search:", EditorStyles.miniLabel);
            searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(180));

            EditorGUILayout.EndHorizontal();
        }

        // ── Left list ─────────────────────────────────────────────────────────────

        private void DrawList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(ListWidth));
            DrawSectionHeader("Dialogues");

            listScroll = EditorGUILayout.BeginScrollView(listScroll);

            string filter = searchFilter.ToLower();

            foreach (ExplorationDialogue d in allDialogues)
            {
                if (d == null) continue;
                if (!string.IsNullOrEmpty(filter) &&
                    !d.dialogueID.ToLower().Contains(filter) &&
                    !d.speakerName.ToLower().Contains(filter) &&
                    !d.name.ToLower().Contains(filter)) continue;

                bool isSelected = d == selectedNode;
                GUI.backgroundColor = isSelected ? SelectedColor : Color.clear;
                EditorGUILayout.BeginHorizontal(GUI.skin.box);
                GUI.backgroundColor = Color.white;

                if (GUILayout.Button(d.dialogueID, EditorStyles.label))
                    Select(d);

                if (GUILayout.Button("⊙", GUILayout.Width(22)))
                    EditorGUIUtility.PingObject(d);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ── Right detail ──────────────────────────────────────────────────────────

        private void DrawDetail()
        {
            EditorGUILayout.BeginVertical();

            if (selectedNode == null)
            {
                EditorGUILayout.HelpBox("Select a dialogue from the list, or create a new one.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            DrawSectionHeader($"Editing: {selectedNode.name}");

            detailScroll = EditorGUILayout.BeginScrollView(detailScroll);

            SerializedObject so = new SerializedObject(selectedNode);
            so.Update();

            // ── Identity ──────────────────────────────────────────────────────────
            DrawGroup("Identity", () =>
            {
                EditorGUILayout.PropertyField(so.FindProperty("dialogueID"));
                EditorGUILayout.PropertyField(so.FindProperty("speakerName"));
                EditorGUILayout.PropertyField(so.FindProperty("dialogueText"));
            });

            EditorGUILayout.Space(4);

            // ── Node event ────────────────────────────────────────────────────────
            DrawGroup("Node Event  (fires when this node is displayed)", () =>
            {
                SerializedProperty nodeEvt = so.FindProperty("nodeEvent");
                SerializedProperty evtType = nodeEvt.FindPropertyRelative("eventType");
                EditorGUILayout.PropertyField(evtType, new GUIContent("Event Type"));

                var type = (DialogueEventType)evtType.enumValueIndex;
                if (type == DialogueEventType.AddResource || type == DialogueEventType.LoseResource)
                {
                    EditorGUILayout.PropertyField(nodeEvt.FindPropertyRelative("selectionMode"), new GUIContent("Selection Mode"));
                    EditorGUILayout.PropertyField(nodeEvt.FindPropertyRelative("resources"),     new GUIContent("Resources"), true);
                    EditorGUILayout.HelpBox("Use {amount} and {resource} tokens in the dialogue text above.", MessageType.Info);
                }
                else if (type == DialogueEventType.StartMission)
                {
                    EditorGUILayout.PropertyField(nodeEvt.FindPropertyRelative("targetMission"), new GUIContent("Target Mission"));
                }
            });

            EditorGUILayout.Space(4);

            // ── Timer ─────────────────────────────────────────────────────────────
            DrawGroup("Timer", () =>
            {
                SerializedProperty hasLimit = so.FindProperty("hasTimeLimit");
                EditorGUILayout.PropertyField(hasLimit, new GUIContent("Has Time Limit"));
                if (hasLimit.boolValue)
                {
                    EditorGUILayout.PropertyField(so.FindProperty("timeLimitSeconds"),  new GUIContent("Duration (s)"));
                    EditorGUILayout.PropertyField(so.FindProperty("timeoutChoiceIndex"), new GUIContent("Default Choice Index  (−1 = OK)"));
                    EditorGUILayout.HelpBox("When the timer hits 0, the choice at the given index is auto-selected. −1 closes the dialogue.", MessageType.Info);
                }
            });

            EditorGUILayout.Space(4);

            // ── Follow-up call (terminal node) ────────────────────────────────────
            DrawGroup("Follow-up Call  (terminal node — no choices)", () =>
            {
                SerializedProperty callProp  = so.FindProperty("followUpCall");
                SerializedProperty delayProp = so.FindProperty("followUpDelayDays");
                EditorGUILayout.PropertyField(delayProp, new GUIContent("Delay (days)"));
                DrawRadioCallInline(callProp, "follow_up_" + selectedNode.dialogueID, so);
            });

            EditorGUILayout.Space(4);

            // ── Choices ───────────────────────────────────────────────────────────
            SerializedProperty choicesProp = so.FindProperty("choices");
            DrawGroup($"Choices ({choicesProp.arraySize})", () => DrawChoices(choicesProp, so));

            EditorGUILayout.Space(4);

            // ── Quick actions ─────────────────────────────────────────────────────
            DrawGroup("Actions", () =>
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("+ Add Choice"))
                {
                    choicesProp.InsertArrayElementAtIndex(choicesProp.arraySize);
                    SyncFoldouts(choicesProp.arraySize);
                }
                if (GUILayout.Button("Duplicate Node"))
                    DuplicateNode(selectedNode);
                if (GUILayout.Button("Ping Asset"))
                    EditorGUIUtility.PingObject(selectedNode);
                EditorGUILayout.EndHorizontal();
            });

            so.ApplyModifiedProperties();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawChoices(SerializedProperty choicesProp, SerializedObject parentSO)
        {
            SyncFoldouts(choicesProp.arraySize);

            for (int i = 0; i < choicesProp.arraySize; i++)
            {
                SerializedProperty choice = choicesProp.GetArrayElementAtIndex(i);

                GUI.backgroundColor = ChoiceColor;
                EditorGUILayout.BeginVertical(GUI.skin.box);
                GUI.backgroundColor = Color.white;

                // Header row
                EditorGUILayout.BeginHorizontal();
                string label = choice.FindPropertyRelative("choiceText").stringValue;
                if (string.IsNullOrEmpty(label)) label = $"Choice {i + 1}";
                choiceFoldouts[i] = EditorGUILayout.Foldout(choiceFoldouts[i], label, true, EditorStyles.foldoutHeader);

                if (GUILayout.Button("✕", GUILayout.Width(24)))
                {
                    choicesProp.DeleteArrayElementAtIndex(i);
                    SyncFoldouts(choicesProp.arraySize);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                if (!choiceFoldouts[i]) { EditorGUILayout.EndVertical(); continue; }

                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(choice.FindPropertyRelative("choiceText"),   new GUIContent("Text"));
                EditorGUILayout.PropertyField(choice.FindPropertyRelative("nextDialogue"),  new GUIContent("Next Node"));

                // Choice event
                SerializedProperty evtProp = choice.FindPropertyRelative("eventTrigger");
                EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("eventType"), new GUIContent("Event"));
                var evtType = (DialogueEventType)evtProp.FindPropertyRelative("eventType").enumValueIndex;
                if (evtType == DialogueEventType.AddResource || evtType == DialogueEventType.LoseResource)
                {
                    EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("selectionMode"), new GUIContent("Selection Mode"));
                    EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("resources"),     new GUIContent("Resources"), true);
                }
                else if (evtType == DialogueEventType.StartMission)
                {
                    EditorGUILayout.PropertyField(evtProp.FindPropertyRelative("targetMission"), new GUIContent("Target Mission"));
                }

                // Follow-up call inline
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Follow-up (when this choice ends the conversation)", EditorStyles.boldLabel);
                SerializedProperty choiceCallProp  = choice.FindPropertyRelative("followUpCall");
                SerializedProperty choiceDelayProp = choice.FindPropertyRelative("followUpDelayDays");
                EditorGUILayout.PropertyField(choiceDelayProp, new GUIContent("Delay (days)"));
                DrawRadioCallInline(choiceCallProp, $"follow_up_{selectedNode.dialogueID}_choice{i}", parentSO);

                // Jump button
                var nextObj = choice.FindPropertyRelative("nextDialogue").objectReferenceValue as ExplorationDialogue;
                if (nextObj != null && GUILayout.Button($"→ Go to: {nextObj.dialogueID}"))
                    Select(nextObj);

                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }
        }

        // ── Inline RadioCallEvent editor ──────────────────────────────────────────

        /// <summary>
        /// Draws all RadioCallEvent parameters inline.
        /// If the property references an existing asset, its fields are edited in place.
        /// A "Create" button lets the user create a new asset without leaving the tool.
        /// </summary>
        private void DrawRadioCallInline(SerializedProperty callProp, string defaultName, SerializedObject parentSO)
        {
            RadioCallEvent call = callProp.objectReferenceValue as RadioCallEvent;

            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUI.backgroundColor = CallColor;
            Rect headerRect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.DrawRect(headerRect, CallColor);
            GUI.backgroundColor = Color.white;
            GUI.Label(new Rect(headerRect.x + 4, headerRect.y + 2, headerRect.width - 80, headerRect.height),
                      "Radio Call", EditorStyles.boldLabel);

            // Ping button
            if (call != null)
            {
                Rect pingRect = new Rect(headerRect.xMax - 74, headerRect.y + 2, 70, 16);
                if (GUI.Button(pingRect, "Ping Asset", EditorStyles.miniButton))
                    EditorGUIUtility.PingObject(call);
            }

            EditorGUILayout.Space(2);

            // Object field — lets user assign an existing asset or clear
            EditorGUI.BeginChangeCheck();
            RadioCallEvent newCall = (RadioCallEvent)EditorGUILayout.ObjectField(
                "Asset", call, typeof(RadioCallEvent), false);
            if (EditorGUI.EndChangeCheck())
            {
                callProp.objectReferenceValue = newCall;
                parentSO.ApplyModifiedProperties();
                call = newCall;
            }

            // Create button when empty
            if (call == null)
            {
                if (GUILayout.Button("+ Create new Radio Call"))
                    call = CreateRadioCallAsset(defaultName, callProp, parentSO);
            }

            // Inline parameters
            if (call != null)
            {
                SerializedObject callSO = new SerializedObject(call);
                callSO.Update();

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Mission Day", EditorStyles.miniLabel);
                EditorGUILayout.PropertyField(callSO.FindProperty("triggerDay"), new GUIContent("Trigger Day"));

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Time of Day", EditorStyles.miniLabel);

                SerializedProperty modeProp = callSO.FindProperty("timeMode");
                EditorGUILayout.PropertyField(modeProp, new GUIContent("Mode"));

                var mode = (TriggerTimeMode)modeProp.enumValueIndex;
                if (mode == TriggerTimeMode.Fixed)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(callSO.FindProperty("fixedHour"),   new GUIContent("Hour"));
                    EditorGUILayout.PropertyField(callSO.FindProperty("fixedMinute"), new GUIContent("Minute"));
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(callSO.FindProperty("randomHourMin"), new GUIContent("From (h)"));
                    EditorGUILayout.PropertyField(callSO.FindProperty("randomHourMax"), new GUIContent("To (h)"));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("Minute is also randomised (0–59).", MessageType.None);
                }

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Content", EditorStyles.miniLabel);
                EditorGUILayout.PropertyField(callSO.FindProperty("dialogue"),   new GUIContent("Dialogue Node"));
                EditorGUILayout.PropertyField(callSO.FindProperty("radioSound"), new GUIContent("Sound"));
                EditorGUILayout.PropertyField(callSO.FindProperty("fireOnce"),   new GUIContent("Fire Once"));

                callSO.ApplyModifiedProperties();
            }

            EditorGUILayout.EndVertical();
        }

        private RadioCallEvent CreateRadioCallAsset(string baseName, SerializedProperty callProp, SerializedObject parentSO)
        {
            string dir = selectedNode != null
                ? System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(selectedNode))
                : "Assets/Data/Dialogues";

            string path = AssetDatabase.GenerateUniqueAssetPath(
                System.IO.Path.Combine(dir, baseName + ".asset"));

            var asset = CreateInstance<RadioCallEvent>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            callProp.objectReferenceValue = asset;
            parentSO.ApplyModifiedProperties();

            EditorGUIUtility.PingObject(asset);
            return asset;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void Refresh()
        {
            allDialogues.Clear();
            string[] guids = AssetDatabase.FindAssets("t:ExplorationDialogue");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var d = AssetDatabase.LoadAssetAtPath<ExplorationDialogue>(path);
                if (d != null) allDialogues.Add(d);
            }
            allDialogues.Sort((a, b) => string.Compare(a.dialogueID, b.dialogueID, System.StringComparison.Ordinal));
            Repaint();
        }

        private void Select(ExplorationDialogue node)
        {
            selectedNode = node;
            SyncFoldouts(node?.choices?.Length ?? 0);
            Repaint();
        }

        private void SyncFoldouts(int count)
        {
            if (choiceFoldouts.Length != count)
            {
                bool[] newArr = new bool[count];
                for (int i = 0; i < Mathf.Min(count, choiceFoldouts.Length); i++)
                    newArr[i] = choiceFoldouts[i];
                choiceFoldouts = newArr;
            }
        }

        private void CreateNewDialogue()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "New ExplorationDialogue", "Dialogue_New", "asset",
                "Choose where to save the new dialogue asset.",
                "Assets/Data/Dialogues");

            if (string.IsNullOrEmpty(path)) return;

            var asset = CreateInstance<ExplorationDialogue>();
            asset.dialogueID   = System.IO.Path.GetFileNameWithoutExtension(path);
            asset.speakerName  = "Explorateur";
            asset.dialogueText = "…";

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Refresh();
            Select(asset);
        }

        private void DuplicateNode(ExplorationDialogue source)
        {
            string srcPath = AssetDatabase.GetAssetPath(source);
            string dir     = System.IO.Path.GetDirectoryName(srcPath);
            string newPath = AssetDatabase.GenerateUniqueAssetPath(
                System.IO.Path.Combine(dir, source.name + "_Copy.asset"));

            AssetDatabase.CopyAsset(srcPath, newPath);
            AssetDatabase.SaveAssets();
            Refresh();
            Select(AssetDatabase.LoadAssetAtPath<ExplorationDialogue>(newPath));
        }

        private static void DrawSectionHeader(string title)
        {
            Rect r = EditorGUILayout.GetControlRect(false, 22);
            EditorGUI.DrawRect(r, HeaderColor);
            GUI.Label(new Rect(r.x + PanelPadding, r.y + 3, r.width, r.height), title, EditorStyles.boldLabel);
        }

        private static void DrawGroup(string title, System.Action content)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            content();
            EditorGUILayout.EndVertical();
        }
    }
}


