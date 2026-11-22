using System.Collections.Generic;
using UnityEngine;

namespace Sdx17.Quest
{
    /// <summary>
    /// Authoring data for one quest, stored as a ScriptableObject asset.
    /// Create one from the Project window: right-click &gt; Create &gt; Sdx17 &gt; Quest &gt; Quest Definition,
    /// or use the Sdx17 / Quest menu in the Editor.
    /// Runtime state lives in <see cref="QuestInstance"/>; this asset never changes at runtime,
    /// so the same definition can be shared by every player and every save.
    /// </summary>
    [CreateAssetMenu(fileName = "New Quest", menuName = "Sdx17/Quest/Quest Definition", order = 0)]
    public class QuestDefinition : ScriptableObject
    {
        [SerializeField] private string questId = string.Empty;
        [SerializeField] private string title = string.Empty;
        [SerializeField, TextArea(2, 4)] private string description = string.Empty;
        [SerializeField] private List<QuestObjectiveData> objectives = new List<QuestObjectiveData>();
        [SerializeField] private QuestReward rewards = new QuestReward();
        [SerializeField] private string nextQuestId = string.Empty;
        [SerializeField] private List<string> prerequisiteQuestIds = new List<string>();
        [SerializeField] private bool repeatable;

        /// <summary>
        /// Stable unique id, e.g. "quest_slime_hunt". Used for save data, chains and
        /// prerequisites. Defaults to the asset name when left empty (see OnValidate).
        /// </summary>
        public string QuestId => questId;

        /// <summary>Player-facing quest name.</summary>
        public string Title => title;

        /// <summary>Player-facing quest description shown in the quest log.</summary>
        public string Description => description;

        /// <summary>Objectives in order. Never null; a quest with no objectives completes on accept.</summary>
        public List<QuestObjectiveData> Objectives => objectives ?? (objectives = new List<QuestObjectiveData>());

        /// <summary>Rewards granted on completion. Never null.</summary>
        public QuestReward Rewards => rewards ?? (rewards = new QuestReward());

        /// <summary>
        /// Id of the quest that follows this one, or empty for the end of a chain.
        /// QuestManager.TryAcceptNextInChain uses this to offer the follow-up quest.
        /// </summary>
        public string NextQuestId => nextQuestId;

        /// <summary>Ids of quests that must be completed before this one can be accepted.</summary>
        public List<string> PrerequisiteQuestIds => prerequisiteQuestIds ?? (prerequisiteQuestIds = new List<string>());

        /// <summary>When true, an abandoned or completed quest may be accepted again.</summary>
        public bool Repeatable => repeatable;

        /// <summary>True when the definition has an id, a title and at least one valid objective.</summary>
        public bool IsValid
        {
            get
            {
                if (string.IsNullOrEmpty(questId) || string.IsNullOrEmpty(title)) return false;
                foreach (QuestObjectiveData objective in Objectives)
                {
                    if (objective == null || !objective.IsValid) return false;
                }
                return true;
            }
        }

        private void OnValidate()
        {
            // Keep the id stable and usable: fall back to the asset name when empty,
            // and clamp every objective amount to at least 1 so progress math never divides by zero.
            if (string.IsNullOrEmpty(questId))
            {
                questId = name;
            }

            foreach (QuestObjectiveData objective in Objectives)
            {
                if (objective == null) continue;
                if (objective.RequiredAmount < 1) objective.RequiredAmount = 1;
            }
        }

        /// <summary>Sets the quest id. Used by the editor tools and the self-test; prefer editing in the Inspector.</summary>
        public void SetQuestId(string id)
        {
            questId = string.IsNullOrEmpty(id) ? name : id;
        }

        /// <summary>Sets the player-facing title. Used by the editor tools and the self-test; prefer editing in the Inspector.</summary>
        public void SetTitle(string value)
        {
            title = value ?? string.Empty;
        }

        /// <summary>Sets the follow-up quest id for a chain. Empty clears the chain link.</summary>
        public void SetNextQuestId(string id)
        {
            nextQuestId = id ?? string.Empty;
        }
    }
}
