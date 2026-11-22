using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace Sdx17.Quest
{
    /// <summary>
    /// UI hook for the quest tracker: the small panel that shows the tracked quest
    /// and its objectives (for example "Slime Hunt — Defeat slimes 2/3").
    /// Attach this to the tracker UI GameObject, assign the QuestManager, and either
    /// wire the UnityEvents below to your UI in the Inspector, or read
    /// <see cref="TrackedQuest"/> and <see cref="GetTrackedQuestText"/> from your own
    /// UI script each frame. This component draws no UI itself, so it works with
    /// any UI system (UGUI, UI Toolkit, or a custom renderer).
    /// </summary>
    public class QuestTracker : MonoBehaviour
    {
        [SerializeField] private QuestManager questManager;

        /// <summary>Inspector event fired when the tracked quest changes.</summary>
        public UnityEvent<QuestInstance> OnTrackedQuestChanged = new UnityEvent<QuestInstance>();
        /// <summary>Inspector event fired when any tracked quest progress changes.</summary>
        public UnityEvent<QuestInstance> OnTrackedQuestProgressChanged = new UnityEvent<QuestInstance>();
        /// <summary>Inspector event fired when the tracked quest completes.</summary>
        public UnityEvent<QuestInstance> OnTrackedQuestCompleted = new UnityEvent<QuestInstance>();

        /// <summary>The manager this tracker listens to. Falls back to QuestManager.Instance.</summary>
        public QuestManager QuestManager
        {
            get => questManager ?? QuestManager.Instance;
            set => questManager = value;
        }

        /// <summary>The quest currently shown by the tracker, or null when nothing is tracked.</summary>
        public QuestInstance TrackedQuest => QuestManager != null ? QuestManager.TrackedQuest : null;

        /// <summary>True when a quest is currently tracked and active.</summary>
        public bool HasTrackedQuest => TrackedQuest != null && TrackedQuest.IsActive;

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            QuestManager manager = QuestManager;
            if (manager == null) return;
            manager.TrackedQuestChanged += HandleTrackedQuestChanged;
            manager.QuestProgressChanged += HandleProgressChanged;
            manager.QuestCompleted += HandleQuestCompleted;
        }

        private void Unsubscribe()
        {
            // QuestManager may already be destroyed on scene teardown; guard against it.
            if (questManager == null && QuestManager.Instance == null) return;
            QuestManager manager = questManager != null ? questManager : QuestManager.Instance;
            if (manager == null) return;
            manager.TrackedQuestChanged -= HandleTrackedQuestChanged;
            manager.QuestProgressChanged -= HandleProgressChanged;
            manager.QuestCompleted -= HandleQuestCompleted;
        }

        private void HandleTrackedQuestChanged(QuestInstance instance)
        {
            OnTrackedQuestChanged.Invoke(instance);
        }

        private void HandleProgressChanged(QuestInstance instance)
        {
            if (TrackedQuest == instance) OnTrackedQuestProgressChanged.Invoke(instance);
        }

        private void HandleQuestCompleted(QuestInstance instance)
        {
            OnTrackedQuestCompleted.Invoke(instance);
        }

        /// <summary>
        /// Multi-line text for the tracker panel, e.g.
        /// "Slime Hunt\nDefeat 3 slimes 2/3\nTalk to Elder Mara 0/1".
        /// Returns "No quest tracked." when nothing is tracked.
        /// </summary>
        public string GetTrackedQuestText()
        {
            QuestInstance quest = TrackedQuest;
            if (quest == null || quest.Definition == null) return "No quest tracked.";

            var text = new StringBuilder();
            text.AppendLine(quest.Definition.Title);
            for (int i = 0; i < quest.Definition.Objectives.Count; i++)
            {
                QuestObjectiveData objective = quest.Definition.Objectives[i];
                if (objective == null) continue;
                string line = string.IsNullOrEmpty(objective.Description)
                    ? $"{objective.Type} {objective.TargetId}"
                    : objective.Description;
                string mark = quest.IsObjectiveComplete(i) ? "Done" : $"{quest.GetProgress(i)}/{objective.RequiredAmount}";
                text.AppendLine($"{line}  {mark}");
            }
            return text.ToString().TrimEnd();
        }

        /// <summary>Progress of the tracked quest as 0..1 (1 when all objectives are done, 0 with no objectives).</summary>
        public float GetTrackedQuestFraction()
        {
            QuestInstance quest = TrackedQuest;
            if (quest?.Definition == null || quest.Definition.Objectives.Count == 0) return 0f;
            int done = 0;
            for (int i = 0; i < quest.Definition.Objectives.Count; i++)
            {
                if (quest.IsObjectiveComplete(i)) done++;
            }
            return (float)done / quest.Definition.Objectives.Count;
        }
    }
}
