using UnityEngine;

namespace Sdx17.Quest.Demo
{
    /// <summary>
    /// Minimal demo driver for the Quest Framework. Attach it to a GameObject in the
    /// demo scene (see Samples~/Demo/README.md), assign the QuestManager and the three
    /// demo quest definitions, then press Play: the first quest is accepted
    /// automatically and the public methods below can be wired to UI buttons or
    /// called from the Inspector context menu (right-click the component).
    /// The demo logs every quest event to the Console so the flow is visible
    /// without any UI at all.
    /// </summary>
    public class QuestDemo : MonoBehaviour
    {
        [SerializeField] private QuestManager questManager;
        [SerializeField] private QuestDefinition firstQuest;
        [SerializeField] private bool autoAcceptFirstQuest = true;

        /// <summary>The manager driving this demo. Falls back to QuestManager.Instance.</summary>
        public QuestManager QuestManager => questManager != null ? questManager : QuestManager.Instance;

        private void Start()
        {
            QuestManager manager = QuestManager;
            if (manager == null)
            {
                Debug.LogError("[Sdx17.Quest Demo] No QuestManager in the scene. Add one and assign it to QuestDemo.");
                return;
            }

            manager.QuestAccepted += instance => Debug.Log($"[Sdx17.Quest Demo] Accepted: {instance.GetSummary()}");
            manager.QuestProgressChanged += instance => Debug.Log($"[Sdx17.Quest Demo] Progress: {instance.GetSummary()}");
            manager.QuestCompleted += instance => Debug.Log($"[Sdx17.Quest Demo] Completed: {instance.Definition.Title} — rewards: {instance.Definition.Rewards.Coins} coins, {instance.Definition.Rewards.Experience} XP.");
            manager.QuestAbandoned += instance => Debug.Log($"[Sdx17.Quest Demo] Abandoned: {instance.Definition.Title}");

            if (autoAcceptFirstQuest && firstQuest != null)
            {
                QuestInstance accepted = manager.Accept(firstQuest, out string reason);
                if (accepted == null) Debug.LogWarning($"[Sdx17.Quest Demo] Could not accept first quest: {reason}");
            }
        }

        /// <summary>Simulates collecting one herb (demo quest 1 objective).</summary>
        [ContextMenu("Demo: Collect 1 Herb")]
        public void CollectHerb()
        {
            QuestManager?.ReportCollect("herb", 1);
        }

        /// <summary>Simulates defeating one slime (demo quest 2 objective).</summary>
        [ContextMenu("Demo: Defeat 1 Slime")]
        public void DefeatSlime()
        {
            QuestManager?.ReportKill("slime", 1);
        }

        /// <summary>Simulates talking to Elder Mara (demo quest 3 objective).</summary>
        [ContextMenu("Demo: Talk to Elder Mara")]
        public void TalkToElderMara()
        {
            QuestManager?.ReportTalk("elder_mara");
        }

        /// <summary>Accepts the quest after the tracked one, if the chain defines one.</summary>
        [ContextMenu("Demo: Accept Next Quest In Chain")]
        public void AcceptNextInChain()
        {
            QuestManager manager = QuestManager;
            if (manager == null) return;
            QuestInstance tracked = manager.TrackedQuest;
            if (tracked == null)
            {
                Debug.Log("[Sdx17.Quest Demo] Nothing is tracked, so there is no chain to continue. Accept a quest first.");
                return;
            }
            QuestInstance next = manager.TryAcceptNextInChain(tracked);
            if (next == null) Debug.Log("[Sdx17.Quest Demo] No next quest in the chain (or it could not be accepted yet).");
        }

        /// <summary>Abandons the currently tracked quest.</summary>
        [ContextMenu("Demo: Abandon Tracked Quest")]
        public void AbandonTrackedQuest()
        {
            QuestManager manager = QuestManager;
            if (manager == null) return;
            if (manager.TrackedQuest == null)
            {
                Debug.Log("[Sdx17.Quest Demo] Nothing is tracked to abandon.");
                return;
            }
            manager.Abandon(manager.TrackedQuest);
        }

        /// <summary>Logs the save snapshot for the tracked quest, to show what a save file would hold.</summary>
        [ContextMenu("Demo: Log Save Data")]
        public void LogSaveData()
        {
            QuestManager manager = QuestManager;
            if (manager == null) return;
            foreach (QuestSaveData entry in manager.SaveAll())
            {
                Debug.Log($"[Sdx17.Quest Demo] Save: {entry.QuestId} state={entry.State} tracked={entry.IsTracked} progress=[{string.Join(",", entry.ObjectiveProgress)}]");
            }
        }
    }
}
