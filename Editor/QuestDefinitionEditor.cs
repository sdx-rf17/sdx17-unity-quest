using UnityEditor;
using UnityEngine;

namespace Sdx17.Quest.Editor
{
    /// <summary>
    /// Custom inspector for <see cref="QuestDefinition"/>: draws the normal fields,
    /// then adds validation warnings (missing id / title, empty or invalid objectives,
    /// a chain link to an empty id) and a one-click chain helper.
    /// </summary>
    [CustomEditor(typeof(QuestDefinition))]
    public class QuestDefinitionEditor : UnityEditor.Editor
    {
        private SerializedProperty questIdProperty;
        private SerializedProperty titleProperty;
        private SerializedProperty descriptionProperty;
        private SerializedProperty objectivesProperty;
        private SerializedProperty rewardsProperty;
        private SerializedProperty nextQuestIdProperty;
        private SerializedProperty prerequisiteQuestIdsProperty;
        private SerializedProperty repeatableProperty;

        private void OnEnable()
        {
            questIdProperty = serializedObject.FindProperty("questId");
            titleProperty = serializedObject.FindProperty("title");
            descriptionProperty = serializedObject.FindProperty("description");
            objectivesProperty = serializedObject.FindProperty("objectives");
            rewardsProperty = serializedObject.FindProperty("rewards");
            nextQuestIdProperty = serializedObject.FindProperty("nextQuestId");
            prerequisiteQuestIdsProperty = serializedObject.FindProperty("prerequisiteQuestIds");
            repeatableProperty = serializedObject.FindProperty("repeatable");
        }

        public override void OnInspectorGUI()
        {
            var definition = (QuestDefinition)target;
            serializedObject.Update();

            EditorGUILayout.PropertyField(questIdProperty);
            EditorGUILayout.PropertyField(titleProperty);
            EditorGUILayout.PropertyField(descriptionProperty);
            EditorGUILayout.PropertyField(objectivesProperty, true);
            EditorGUILayout.PropertyField(rewardsProperty, true);
            EditorGUILayout.PropertyField(nextQuestIdProperty);
            EditorGUILayout.PropertyField(prerequisiteQuestIdsProperty, true);
            EditorGUILayout.PropertyField(repeatableProperty);

            EditorGUILayout.Space();
            DrawValidation(definition);
            EditorGUILayout.Space();

            if (GUILayout.Button("Validate Quest"))
            {
                ValidateAndReport(definition);
            }

            if (GUILayout.Button("Create Next Quest In Chain"))
            {
                CreateNextQuestInChain(definition);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawValidation(QuestDefinition definition)
        {
            if (definition == null) return;

            if (string.IsNullOrEmpty(definition.QuestId))
            {
                EditorGUILayout.HelpBox("Quest Id is empty. It falls back to the asset name on validate, but set an explicit id for save data and chains.", MessageType.Warning);
            }
            if (string.IsNullOrEmpty(definition.Title))
            {
                EditorGUILayout.HelpBox("Title is empty. Players see this in the quest log.", MessageType.Warning);
            }
            if (definition.Objectives.Count == 0)
            {
                EditorGUILayout.HelpBox("No objectives. The quest completes immediately on accept — intended only for instant / talk-only hand-ins.", MessageType.Info);
            }
            foreach (QuestObjectiveData objective in definition.Objectives)
            {
                if (objective == null)
                {
                    EditorGUILayout.HelpBox("An objective entry is null. Remove it from the list.", MessageType.Error);
                    continue;
                }
                if (!objective.IsValid)
                {
                    EditorGUILayout.HelpBox($"Objective '{objective.Description}' has no target id or an amount below 1.", MessageType.Error);
                }
            }
            if (!string.IsNullOrEmpty(definition.NextQuestId) && definition.NextQuestId == definition.QuestId)
            {
                EditorGUILayout.HelpBox("Next Quest Id points back to this quest. A chain must not loop onto itself.", MessageType.Error);
            }
        }

        private void ValidateAndReport(QuestDefinition definition)
        {
            if (definition == null) return;
            if (definition.IsValid)
            {
                EditorUtility.DisplayDialog("Sdx17 Quest", $"'{definition.Title}' looks valid: {definition.Objectives.Count} objective(s), chain to '{definition.NextQuestId}' or end of chain.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Sdx17 Quest", $"'{definition.Title}' has validation problems. See the warnings above the buttons.", "OK");
            }
        }

        private void CreateNextQuestInChain(QuestDefinition definition)
        {
            if (definition == null) return;

            string folder = "Assets";
            string assetPath = AssetDatabase.GetAssetPath(definition);
            if (!string.IsNullOrEmpty(assetPath))
            {
                folder = System.IO.Path.GetDirectoryName(assetPath)?.Replace('\\', '/') ?? "Assets";
            }

            string newId = $"{definition.QuestId}_part2";
            var next = CreateInstance<QuestDefinition>();
            next.SetQuestId(newId);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{newId}.asset");
            AssetDatabase.CreateAsset(next, path);
            AssetDatabase.SaveAssets();

            definition.SetNextQuestId(newId);
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();

            Selection.activeObject = next;
            Debug.Log($"[Sdx17.Quest] Created chained quest '{newId}' at {path} and linked it from '{definition.QuestId}'.");
        }
    }
}
