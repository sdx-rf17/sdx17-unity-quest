using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sdx17.Quest
{
    /// <summary>
    /// The four objective kinds the framework tracks.
    /// Kill: defeat <see cref="QuestObjectiveData.TargetId"/> that many times.
    /// Collect: pick up / own that many of the item with <see cref="QuestObjectiveData.TargetId"/>.
    /// Talk: speak to the NPC whose id is <see cref="QuestObjectiveData.TargetId"/>.
    /// ReachLocation: enter the trigger / area whose id is <see cref="QuestObjectiveData.TargetId"/>.
    /// Target ids are plain strings so the game decides its own id scheme
    /// (enemy prefab name, item id, NPC id, location id) and reports it back
    /// through QuestManager.ReportKill / ReportCollect / ReportTalk / ReportReachLocation.
    /// </summary>
    public enum QuestObjectiveType
    {
        Kill,
        Collect,
        Talk,
        ReachLocation
    }

    /// <summary>
    /// One item granted by a quest reward.
    /// </summary>
    [Serializable]
    public class QuestItemReward
    {
        [SerializeField] private string itemId = string.Empty;
        [SerializeField] private int amount = 1;

        /// <summary>Id of the item, as used by the game's inventory system.</summary>
        public string ItemId { get => itemId; set => itemId = value ?? string.Empty; }

        /// <summary>How many of the item to grant. Clamped to at least 1.</summary>
        public int Amount { get => amount; set => amount = Mathf.Max(1, value); }
    }

    /// <summary>
    /// The reward block of a quest. Rewards are data only: QuestManager fires
    /// OnQuestCompleted with the finished instance, and the game grants the
    /// coins / XP / items itself (for example through the Sdx17 Inventory system).
    /// That keeps this package free of any currency or inventory implementation.
    /// </summary>
    [Serializable]
    public class QuestReward
    {
        [SerializeField] private int coins;
        [SerializeField] private int experience;
        [SerializeField] private List<QuestItemReward> items = new List<QuestItemReward>();

        /// <summary>Coins granted on completion. Zero means no coin reward.</summary>
        public int Coins { get => coins; set => coins = Mathf.Max(0, value); }

        /// <summary>Experience granted on completion. Zero means no XP reward.</summary>
        public int Experience { get => experience; set => experience = Mathf.Max(0, value); }

        /// <summary>Items granted on completion. Never null; may be empty.</summary>
        public List<QuestItemReward> Items => items ?? (items = new List<QuestItemReward>());

        /// <summary>True when this reward grants anything at all.</summary>
        public bool HasAnyReward => coins > 0 || experience > 0 || Items.Count > 0;
    }

    /// <summary>
    /// A single objective inside a <see cref="QuestDefinition"/>.
    /// Serialized as part of the definition asset so designers edit quests
    /// in the Inspector without writing code.
    /// </summary>
    [Serializable]
    public class QuestObjectiveData
    {
        [SerializeField] private QuestObjectiveType type = QuestObjectiveType.Kill;
        [SerializeField] private string targetId = string.Empty;
        [SerializeField] private string description = string.Empty;
        [SerializeField] private int requiredAmount = 1;

        /// <summary>What kind of action advances this objective.</summary>
        public QuestObjectiveType Type { get => type; set => type = value; }

        /// <summary>
        /// Id of the thing to kill / collect / talk to / reach.
        /// Must match the id the game passes to QuestManager progress calls.
        /// </summary>
        public string TargetId { get => targetId; set => targetId = value ?? string.Empty; }

        /// <summary>Player-facing line, e.g. "Defeat 3 slimes". Shown by the tracker UI.</summary>
        public string Description { get => description; set => description = value ?? string.Empty; }

        /// <summary>How many times the action is needed. Clamped to at least 1.</summary>
        public int RequiredAmount { get => requiredAmount; set => requiredAmount = Mathf.Max(1, value); }

        /// <summary>True when this objective entry is usable (has a target id).</summary>
        public bool IsValid => !string.IsNullOrEmpty(targetId) && requiredAmount >= 1;
    }
}
