using System.Collections.Generic;
using UnityEngine;

namespace Sdx17.Quest.Tests
{
    /// <summary>
    /// Play-mode self-test for the Quest Framework. Attach it to a GameObject in a
    /// test scene (or add it at runtime), press Play, and read the Console:
    /// every check logs PASS or FAIL, then a summary line ("QuestSelfTest: 12 passed, 0 failed").
    /// The test builds its quest definitions in memory with
    /// ScriptableObject.CreateInstance, so it needs no assets and no scene setup
    /// beyond a QuestManager (one is created automatically when missing).
    /// This is a smoke test, not a substitute for the Unity Test Framework or
    /// manual playtesting — see the TEST checklist in the package README.
    /// </summary>
    public class QuestSelfTest : MonoBehaviour
    {
        [SerializeField] private bool runOnStart = true;

        /// <summary>How many checks passed in the last run.</summary>
        public int PassedCount { get; private set; }

        /// <summary>How many checks failed in the last run.</summary>
        public int FailedCount { get; private set; }

        private void Start()
        {
            if (runOnStart) RunSelfTest();
        }

        /// <summary>Runs every check and logs PASS/FAIL per check plus a summary. Safe to call more than once.</summary>
        [ContextMenu("Run Quest Self-Test")]
        public void RunSelfTest()
        {
            PassedCount = 0;
            FailedCount = 0;

            QuestManager manager = GetOrCreateManager();
            if (manager == null)
            {
                LogCheck("Manager exists", false);
                LogSummary();
                return;
            }

            QuestDefinition collect = BuildQuest("quest_test_collect", "Test Collect", QuestObjectiveType.Collect, "herb", 3, "quest_test_hunt");
            QuestDefinition hunt = BuildQuest("quest_test_hunt", "Test Hunt", QuestObjectiveType.Kill, "slime", 2, string.Empty);
            hunt.PrerequisiteQuestIds.Add("quest_test_collect");
            QuestDefinition talk = BuildQuest("quest_test_talk", "Test Talk", QuestObjectiveType.Talk, "elder_mara", 1, string.Empty);
            manager.RegisterDefinition(collect);
            manager.RegisterDefinition(hunt);
            manager.RegisterDefinition(talk);

            // Accept: prerequisites block the hunt until the collect quest is done.
            QuestInstance huntEarly = manager.Accept(hunt, out string huntReason);
            LogCheck("Prerequisite blocks early accept", huntEarly == null && !string.IsNullOrEmpty(huntReason));

            QuestInstance accepted = manager.Accept(collect, out string acceptReason);
            LogCheck("Accept collect quest", accepted != null && accepted.IsActive);

            QuestInstance duplicate = manager.Accept(collect, out _);
            LogCheck("Second accept of active quest is refused", duplicate == null);

            LogCheck("Accepted quest is tracked", manager.TrackedQuest == accepted);

            // Progress: partial progress does not complete the quest.
            int advanced = manager.ReportCollect("herb", 2);
            LogCheck("Collect 2 herbs advances 1 quest", advanced == 1 && accepted.GetProgress(0) == 2);
            LogCheck("Quest not complete at 2/3", !accepted.AreAllObjectivesComplete());

            // Wrong target id must not advance anything.
            int wrong = manager.ReportCollect("stone", 5);
            LogCheck("Wrong target id advances nothing", wrong == 0 && accepted.GetProgress(0) == 2);

            // Finishing the objectives completes the quest at once.
            manager.ReportCollect("herb", 1);
            LogCheck("Quest completes at 3/3", accepted.IsCompleted);
            LogCheck("Completed quest is no longer tracked", manager.TrackedQuest == null);

            // Chain: the hunt is now acceptable (its prerequisite is done) and
            // TryAcceptNextInChain follows the collect quest's NextQuestId.
            QuestInstance chained = manager.TryAcceptNextInChain(accepted);
            LogCheck("Chain accepts next quest", chained != null && chained.QuestId == "quest_test_hunt" && chained.IsActive);

            // Kill progress, then abandon: abandon clears tracking and blocks completion.
            manager.ReportKill("slime", 1);
            LogCheck("Kill progress 1/2", chained.GetProgress(0) == 1);
            bool abandoned = manager.Abandon(chained);
            LogCheck("Abandon active quest", abandoned && chained.State == QuestState.Abandoned);
            LogCheck("Abandoned quest cannot complete", manager.Complete(chained) == null);

            // Save / load round-trip: save the current state, then load it back.
            // LoadAll clears and rebuilds the manager from the snapshots.
            QuestInstance huntAgain = manager.Accept(hunt, out _);
            LogCheck("Non-repeatable abandoned quest cannot be re-accepted", huntAgain == null);
            LogCheck("Save snapshot holds all quests", manager.SaveAll().Count >= 2);

            List<QuestSaveData> saved = manager.SaveAll();
            manager.LoadAll(saved);
            QuestInstance reloaded = manager.GetQuest("quest_test_collect");
            LogCheck("Load restores completed quest", reloaded != null && reloaded.IsCompleted);
            LogCheck("Load restores objective progress", reloaded != null && reloaded.GetProgress(0) == 3);

            // Talk objective on a fresh quest.
            QuestInstance talkInstance = manager.Accept(talk, out _);
            LogCheck("Accept talk quest", talkInstance != null && talkInstance.IsActive);
            manager.ReportTalk("elder_mara");
            LogCheck("Talk completes talk quest", talkInstance.IsCompleted);

            LogSummary();
        }

        private QuestManager GetOrCreateManager()
        {
            QuestManager manager = QuestManager.Instance;
            if (manager != null) return manager;
            var holder = new GameObject("QuestManager (Self-Test)");
            return holder.AddComponent<QuestManager>();
        }

        private QuestDefinition BuildQuest(string questId, string title, QuestObjectiveType type, string targetId, int requiredAmount, string nextQuestId)
        {
            var definition = ScriptableObject.CreateInstance<QuestDefinition>();
            definition.SetQuestId(questId);
            definition.SetTitle(title);
            definition.SetNextQuestId(nextQuestId);

            var objective = new QuestObjectiveData
            {
                Type = type,
                TargetId = targetId,
                Description = $"{title} objective",
                RequiredAmount = requiredAmount
            };
            definition.Objectives.Add(objective);
            definition.Rewards.Coins = 10;
            definition.Rewards.Experience = 5;
            return definition;
        }

        private void LogCheck(string name, bool passed)
        {
            if (passed)
            {
                PassedCount++;
                Debug.Log($"[Sdx17.Quest Self-Test] PASS: {name}");
            }
            else
            {
                FailedCount++;
                Debug.LogError($"[Sdx17.Quest Self-Test] FAIL: {name}");
            }
        }

        private void LogSummary()
        {
            if (FailedCount == 0)
            {
                Debug.Log($"[Sdx17.Quest Self-Test] Summary: {PassedCount} passed, 0 failed.");
            }
            else
            {
                Debug.LogError($"[Sdx17.Quest Self-Test] Summary: {PassedCount} passed, {FailedCount} failed.");
            }
        }
    }
}
