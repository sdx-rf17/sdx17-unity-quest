using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sdx17.Quest.Editor
{
    /// <summary>
    /// Sdx17 / Quest menu items: create a single quest definition, or a ready-made
    /// three-quest demo chain (collect, kill, talk) that matches Samples~/Demo.
    /// All created assets land next to the currently selected folder in the Project window.
    /// </summary>
    public static class QuestTools
    {
        [MenuItem("Sdx17/Quest/Create Quest Definition")]
        public static void CreateQuestDefinition()
        {
            var definition = ScriptableObject.CreateInstance<QuestDefinition>();
            string folder = GetSelectedFolder();
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/New Quest.asset");
            AssetDatabase.CreateAsset(definition, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = definition;
            Debug.Log($"[Sdx17.Quest] Created quest definition at {path}. Set its Id, Title and objectives in the Inspector.");
        }

        [MenuItem("Sdx17/Quest/Create Demo Quest Chain")]
        public static void CreateDemoQuestChain()
        {
            string folder = GetSelectedFolder();

            QuestDefinition collect = CreateQuestAsset(folder, "quest_demo_collect", "Gather Herbs",
                "Collect 3 herbs for Elder Mara.", QuestObjectiveType.Collect, "herb", "Collect herbs 0/3", 3, 50, 20, "quest_demo_hunt");
            QuestDefinition hunt = CreateQuestAsset(folder, "quest_demo_hunt", "Slime Hunt",
                "Defeat 2 slimes in the meadow.", QuestObjectiveType.Kill, "slime", "Defeat slimes 0/2", 2, 100, 40, "quest_demo_report");
            QuestDefinition report = CreateQuestAsset(folder, "quest_demo_report", "Report Back",
                "Return and talk to Elder Mara.", QuestObjectiveType.Talk, "elder_mara", "Talk to Elder Mara 0/1", 1, 150, 60, string.Empty);

            collect.PrerequisiteQuestIds.Clear();
            hunt.PrerequisiteQuestIds.Add("quest_demo_collect");
            report.PrerequisiteQuestIds.Add("quest_demo_hunt");

            foreach (QuestDefinition definition in new[] { collect, hunt, report })
            {
                EditorUtility.SetDirty(definition);
            }
            AssetDatabase.SaveAssets();

            Selection.activeObject = collect;
            Debug.Log("[Sdx17.Quest] Created demo chain: quest_demo_collect -> quest_demo_hunt -> quest_demo_report. Add all three to a QuestManager's Known Definitions.");
        }

        private static QuestDefinition CreateQuestAsset(string folder, string questId, string title, string description,
            QuestObjectiveType objectiveType, string targetId, string objectiveDescription, int requiredAmount,
            int coins, int experience, string nextQuestId)
        {
            var definition = ScriptableObject.CreateInstance<QuestDefinition>();
            definition.SetQuestId(questId);
            definition.SetNextQuestId(nextQuestId);

            // Serialized fields are private, so set the public-facing data through the
            // serialized object: title / description / objectives / rewards.
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("title").stringValue = title;
            serialized.FindProperty("description").stringValue = description;
            SerializedProperty objectives = serialized.FindProperty("objectives");
            objectives.arraySize = 1;
            SerializedProperty objective = objectives.GetArrayElementAtIndex(0);
            objective.FindPropertyRelative("type").enumValueIndex = (int)objectiveType;
            objective.FindPropertyRelative("targetId").stringValue = targetId;
            objective.FindPropertyRelative("description").stringValue = objectiveDescription;
            objective.FindPropertyRelative("requiredAmount").intValue = requiredAmount;
            serialized.FindProperty("rewards").FindPropertyRelative("coins").intValue = coins;
            serialized.FindProperty("rewards").FindPropertyRelative("experience").intValue = experience;
            serialized.ApplyModifiedProperties();

            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{questId}.asset");
            AssetDatabase.CreateAsset(definition, path);
            return definition;
        }

        private static string GetSelectedFolder()
        {
            string folder = "Assets";
            Object selected = Selection.activeObject;
            if (selected != null)
            {
                string path = AssetDatabase.GetAssetPath(selected);
                if (!string.IsNullOrEmpty(path))
                {
                    folder = AssetDatabase.IsValidFolder(path) ? path : Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "Assets";
                }
            }
            return folder;
        }
    }
}
