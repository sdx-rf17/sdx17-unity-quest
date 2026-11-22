using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sdx17.Quest
{
    /// <summary>Runtime lifecycle of a quest instance.</summary>
    public enum QuestState
    {
        /// <summary>Never accepted, or abandoned and not repeatable.</summary>
        NotAccepted,
        /// <summary>Accepted and in progress.</summary>
        Active,
        /// <summary>All objectives done and rewards handed out (by the game, via the completion event).</summary>
        Completed,
        /// <summary>Accepted, then given up by the player. Repeatable quests can be accepted again.</summary>
        Abandoned
    }

    /// <summary>
    /// Serializable snapshot of one quest, for save files.
    /// The game persists the list returned by <see cref="QuestManager.SaveAll"/>
    /// however it likes (JSON, PlayerPrefs, the Sdx17 Secure Save system) and
    /// hands it back to <see cref="QuestManager.LoadAll"/>. Only plain data is stored:
    /// the quest id, its state, per-objective progress and whether it is tracked.
    /// </summary>
    [Serializable]
    public class QuestSaveData
    {
        [SerializeField] private string questId = string.Empty;
        [SerializeField] private QuestState state = QuestState.NotAccepted;
        [SerializeField] private int[] objectiveProgress = Array.Empty<int>();
        [SerializeField] private bool isTracked;

        /// <summary>Id of the quest this snapshot belongs to.</summary>
        public string QuestId { get => questId; set => questId = value ?? string.Empty; }

        /// <summary>State the quest was in when saved.</summary>
        public QuestState State { get => state; set => state = value; }

        /// <summary>Progress per objective index, matching the definition's objective order.</summary>
        public int[] ObjectiveProgress { get => objectiveProgress; set => objectiveProgress = value ?? Array.Empty<int>(); }

        /// <summary>Whether this quest was the tracked one when saved.</summary>
        public bool IsTracked { get => isTracked; set => isTracked = value; }
    }

    /// <summary>
    /// Runtime state for one accepted quest: its definition plus live progress.
    /// Created by <see cref="QuestManager"/> on accept; never create one directly
    /// except in tests, where the constructor below is used.
    /// </summary>
    [Serializable]
    public class QuestInstance
    {
        [SerializeField] private QuestDefinition definition;
        [SerializeField] private QuestState state = QuestState.NotAccepted;
        [SerializeField] private int[] objectiveProgress = Array.Empty<int>();
        [SerializeField] private bool isTracked;

        /// <summary>Creates an instance bound to a definition, with zeroed progress.</summary>
        public QuestInstance(QuestDefinition definition)
        {
            this.definition = definition;
            state = QuestState.Active;
            int count = definition != null ? definition.Objectives.Count : 0;
            objectiveProgress = new int[count];
        }

        /// <summary>The definition asset this instance runs. Null only when built without one (tests).</summary>
        public QuestDefinition Definition => definition;

        /// <summary>Shortcut for <c>Definition.QuestId</c>; empty when there is no definition.</summary>
        public string QuestId => definition != null ? definition.QuestId : string.Empty;

        /// <summary>Current lifecycle state.</summary>
        public QuestState State => state;

        /// <summary>True while the quest is accepted and not yet finished or abandoned.</summary>
        public bool IsActive => state == QuestState.Active;

        /// <summary>True once the quest has been completed.</summary>
        public bool IsCompleted => state == QuestState.Completed;

        /// <summary>True while this quest is the one the tracker UI shows.</summary>
        public bool IsTracked { get => isTracked; internal set => isTracked = value; }

        /// <summary>Progress (0..RequiredAmount) for the objective at <paramref name="index"/>; 0 for a bad index.</summary>
        public int GetProgress(int index)
        {
            if (objectiveProgress == null || index < 0 || index >= objectiveProgress.Length) return 0;
            return objectiveProgress[index];
        }

        /// <summary>True when the objective at <paramref name="index"/> reached its required amount.</summary>
        public bool IsObjectiveComplete(int index)
        {
            if (definition == null || index < 0 || index >= definition.Objectives.Count) return false;
            return GetProgress(index) >= definition.Objectives[index].RequiredAmount;
        }

        /// <summary>
        /// True when every objective is complete. A quest with no objectives is
        /// complete as soon as it is accepted, so the manager can finish it at once.
        /// </summary>
        public bool AreAllObjectivesComplete()
        {
            if (definition == null) return false;
            for (int i = 0; i < definition.Objectives.Count; i++)
            {
                if (!IsObjectiveComplete(i)) return false;
            }
            return true;
        }

        /// <summary>
        /// Adds progress to every matching, still-incomplete objective of the given
        /// type and target id. Returns the total amount actually added (0 when nothing matched).
        /// Progress is clamped at the required amount and never counts after completion.
        /// </summary>
        public int AddProgress(QuestObjectiveType type, string targetId, int amount)
        {
            if (definition == null || state != QuestState.Active) return 0;
            if (string.IsNullOrEmpty(targetId) || amount <= 0) return 0;

            int added = 0;
            for (int i = 0; i < definition.Objectives.Count; i++)
            {
                QuestObjectiveData objective = definition.Objectives[i];
                if (objective == null) continue;
                if (objective.Type != type) continue;
                if (!string.Equals(objective.TargetId, targetId, StringComparison.Ordinal)) continue;
                if (objectiveProgress == null || i >= objectiveProgress.Length) continue;

                int before = objectiveProgress[i];
                int after = Mathf.Min(objective.RequiredAmount, before + amount);
                objectiveProgress[i] = after;
                added += after - before;
            }
            return added;
        }

        /// <summary>Marks the instance completed. Only an active instance can complete.</summary>
        public bool MarkCompleted()
        {
            if (state != QuestState.Active) return false;
            state = QuestState.Completed;
            return true;
        }

        /// <summary>Marks the instance abandoned. Only an active instance can be abandoned.</summary>
        public bool MarkAbandoned()
        {
            if (state != QuestState.Active) return false;
            state = QuestState.Abandoned;
            isTracked = false;
            return true;
        }

        /// <summary>Builds a save snapshot of this instance.</summary>
        public QuestSaveData ToSaveData()
        {
            return new QuestSaveData
            {
                QuestId = QuestId,
                State = state,
                ObjectiveProgress = objectiveProgress != null ? (int[])objectiveProgress.Clone() : Array.Empty<int>(),
                IsTracked = isTracked
            };
        }

        /// <summary>
        /// Restores runtime state from a snapshot. The definition must already be
        /// set (the manager looks it up by id); progress is clamped to each
        /// objective's required amount and padded/truncated to the objective count.
        /// </summary>
        public void ApplySaveData(QuestSaveData saveData)
        {
            if (saveData == null || definition == null) return;

            state = saveData.State;
            isTracked = saveData.IsTracked;

            int count = definition.Objectives.Count;
            objectiveProgress = new int[count];
            int[] saved = saveData.ObjectiveProgress ?? Array.Empty<int>();
            for (int i = 0; i < count; i++)
            {
                int value = i < saved.Length ? saved[i] : 0;
                objectiveProgress[i] = Mathf.Clamp(value, 0, definition.Objectives[i].RequiredAmount);
            }
        }

        /// <summary>One-line progress summary, e.g. "Slime Hunt: 2/3 objectives done".</summary>
        public string GetSummary()
        {
            if (definition == null) return "Unknown quest";
            int done = 0;
            for (int i = 0; i < definition.Objectives.Count; i++)
            {
                if (IsObjectiveComplete(i)) done++;
            }
            return $"{definition.Title}: {done}/{definition.Objectives.Count} objectives done";
        }
    }
}
