using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Sdx17.Quest
{
    /// <summary>
    /// Central quest runtime. Add one <see cref="QuestManager"/> to a scene (or a
    /// persistent bootstrap GameObject), assign the quest definitions it should know
    /// about, and drive it from NPCs, triggers, combat and pickups.
    /// The manager owns accept / track / complete / abandon, prerequisite and chain
    /// checks, progress routing and save/load snapshots. It never grants rewards
    /// itself: it fires <see cref="OnQuestCompleted"/> and the game grants the
    /// coins / XP / items from the instance's <see cref="QuestDefinition.Rewards"/>.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        [SerializeField] private List<QuestDefinition> knownDefinitions = new List<QuestDefinition>();
        [SerializeField] private bool persistAcrossScenes = true;
        [SerializeField] private bool autoTrackOnAccept = true;

        // C# events for code subscribers (game systems, save system).
        /// <summary>Fired after a quest is accepted.</summary>
        public event Action<QuestInstance> QuestAccepted;
        /// <summary>Fired after a quest is completed (all objectives done).</summary>
        public event Action<QuestInstance> QuestCompleted;
        /// <summary>Fired after a quest is abandoned.</summary>
        public event Action<QuestInstance> QuestAbandoned;
        /// <summary>Fired whenever any objective progress changes.</summary>
        public event Action<QuestInstance> QuestProgressChanged;
        /// <summary>Fired when the tracked quest changes (including tracking nothing).</summary>
        public event Action<QuestInstance> TrackedQuestChanged;

        // UnityEvents for Inspector wiring (UI, audio, dialogue triggers).
        /// <summary>Inspector event fired after a quest is accepted.</summary>
        public UnityEvent<QuestInstance> OnQuestAccepted = new UnityEvent<QuestInstance>();
        /// <summary>Inspector event fired after a quest is completed.</summary>
        public UnityEvent<QuestInstance> OnQuestCompleted = new UnityEvent<QuestInstance>();
        /// <summary>Inspector event fired after a quest is abandoned.</summary>
        public UnityEvent<QuestInstance> OnQuestAbandoned = new UnityEvent<QuestInstance>();
        /// <summary>Inspector event fired whenever any objective progress changes.</summary>
        public UnityEvent<QuestInstance> OnQuestProgressChanged = new UnityEvent<QuestInstance>();
        /// <summary>Inspector event fired when the tracked quest changes.</summary>
        public UnityEvent<QuestInstance> OnTrackedQuestChanged = new UnityEvent<QuestInstance>();

        private readonly Dictionary<string, QuestInstance> questsById = new Dictionary<string, QuestInstance>();
        private QuestInstance trackedQuest;

        /// <summary>All quest definitions this manager knows about. Never null.</summary>
        public List<QuestDefinition> KnownDefinitions => knownDefinitions ?? (knownDefinitions = new List<QuestDefinition>());

        /// <summary>When true, this manager survives scene loads via DontDestroyOnLoad.</summary>
        public bool PersistAcrossScenes { get => persistAcrossScenes; set => persistAcrossScenes = value; }

        /// <summary>When true, accepting a quest also makes it the tracked quest.</summary>
        public bool AutoTrackOnAccept { get => autoTrackOnAccept; set => autoTrackOnAccept = value; }

        /// <summary>Currently tracked quest, or null when nothing is tracked.</summary>
        public QuestInstance TrackedQuest => trackedQuest;

        /// <summary>All instances the manager holds (active, completed and abandoned).</summary>
        public IReadOnlyCollection<QuestInstance> AllQuests => questsById.Values;

        /// <summary>Singleton-style access. Set on Awake; the first manager in the scene wins.</summary>
        public static QuestManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[Sdx17.Quest] A second QuestManager was created; the extra one is disabled so quests stay in one place.");
                enabled = false;
                return;
            }

            Instance = this;
            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Registers a definition at runtime (for quests loaded from DLC, addressables or a mod).</summary>
        public void RegisterDefinition(QuestDefinition definition)
        {
            if (definition == null || string.IsNullOrEmpty(definition.QuestId)) return;
            if (!knownDefinitions.Contains(definition)) knownDefinitions.Add(definition);
        }

        /// <summary>Finds a known definition by id, or null when unknown.</summary>
        public QuestDefinition FindDefinition(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return null;
            foreach (QuestDefinition definition in KnownDefinitions)
            {
                if (definition != null && definition.QuestId == questId) return definition;
            }
            return null;
        }

        /// <summary>Returns the instance for a quest id, or null when never accepted.</summary>
        public QuestInstance GetQuest(string questId)
        {
            if (string.IsNullOrEmpty(questId)) return null;
            questsById.TryGetValue(questId, out QuestInstance instance);
            return instance;
        }

        /// <summary>True when the quest is currently active.</summary>
        public bool IsQuestActive(string questId) => GetQuest(questId)?.IsActive ?? false;

        /// <summary>True when the quest has been completed at least once.</summary>
        public bool IsQuestCompleted(string questId) => GetQuest(questId)?.IsCompleted ?? false;

        /// <summary>
        /// True when a quest may be accepted: the definition exists, it is not already
        /// active or (when not repeatable) completed, and every prerequisite quest is completed.
        /// Returns false with a reason string explaining why, for NPC dialogue ("You must finish X first").
        /// </summary>
        public bool CanAccept(QuestDefinition definition, out string reason)
        {
            reason = string.Empty;
            if (definition == null)
            {
                reason = "No quest definition was given.";
                return false;
            }

            QuestInstance existing = GetQuest(definition.QuestId);
            if (existing != null && existing.IsActive)
            {
                reason = "That quest is already active.";
                return false;
            }
            if (existing != null && existing.IsCompleted && !definition.Repeatable)
            {
                reason = "That quest is already completed.";
                return false;
            }

            foreach (string prerequisiteId in definition.PrerequisiteQuestIds)
            {
                if (string.IsNullOrEmpty(prerequisiteId)) continue;
                if (!IsQuestCompleted(prerequisiteId))
                {
                    QuestDefinition prerequisite = FindDefinition(prerequisiteId);
                    string name = prerequisite != null ? prerequisite.Title : prerequisiteId;
                    reason = $"Complete '{name}' first.";
                    return false;
                }
            }

            return true;
        }

        /// <summary>Accepts a quest when <see cref="CanAccept"/> allows it. Returns the instance, or null with a reason.</summary>
        public QuestInstance Accept(QuestDefinition definition, out string reason)
        {
            if (!CanAccept(definition, out reason)) return null;

            QuestInstance existing = GetQuest(definition.QuestId);
            QuestInstance instance;
            if (existing != null && existing.State == QuestState.Abandoned && definition.Repeatable)
            {
                // Re-accepting a repeatable abandoned quest starts it fresh.
                instance = new QuestInstance(definition);
                questsById[definition.QuestId] = instance;
            }
            else if (existing != null && existing.State == QuestState.Completed && definition.Repeatable)
            {
                instance = new QuestInstance(definition);
                questsById[definition.QuestId] = instance;
            }
            else
            {
                instance = new QuestInstance(definition);
                questsById[definition.QuestId] = instance;
            }

            if (autoTrackOnAccept) Track(instance);

            QuestAccepted?.Invoke(instance);
            OnQuestAccepted.Invoke(instance);

            // A quest with no objectives is done the moment it is accepted.
            if (instance.AreAllObjectivesComplete()) Complete(instance);

            return instance;
        }

        /// <summary>Accepts a quest by id. Returns null when the id is unknown or cannot be accepted.</summary>
        public QuestInstance AcceptById(string questId)
        {
            QuestDefinition definition = FindDefinition(questId);
            if (definition == null)
            {
                Debug.LogWarning($"[Sdx17.Quest] AcceptById: unknown quest id '{questId}'. Register the definition first.");
                return null;
            }
            return Accept(definition, out _);
        }

        /// <summary>Makes the given quest the tracked one shown by the UI. Pass null to clear tracking.</summary>
        public void Track(QuestInstance instance)
        {
            if (instance != null && !instance.IsActive)
            {
                Debug.LogWarning($"[Sdx17.Quest] Only an active quest can be tracked ('{instance.QuestId}' is {instance.State}).");
                return;
            }

            if (trackedQuest != null) trackedQuest.IsTracked = false;
            trackedQuest = instance;
            if (trackedQuest != null) trackedQuest.IsTracked = true;

            TrackedQuestChanged?.Invoke(trackedQuest);
            OnTrackedQuestChanged.Invoke(trackedQuest);
        }

        /// <summary>Tracks an active quest by id. Does nothing when the quest is not active.</summary>
        public void TrackById(string questId)
        {
            QuestInstance instance = GetQuest(questId);
            if (instance == null)
            {
                Debug.LogWarning($"[Sdx17.Quest] TrackById: no accepted quest with id '{questId}'.");
                return;
            }
            Track(instance);
        }

        /// <summary>
        /// Completes an active quest whose objectives are all done, fires the completion
        /// events, and returns its rewards so the game can grant them. Returns null
        /// when the quest cannot complete yet (objectives unfinished, or not active).
        /// </summary>
        public QuestReward Complete(QuestInstance instance)
        {
            if (instance == null) return null;
            if (!instance.IsActive)
            {
                Debug.LogWarning($"[Sdx17.Quest] Complete: '{instance.QuestId}' is not active (state {instance.State}).");
                return null;
            }
            if (!instance.AreAllObjectivesComplete())
            {
                Debug.LogWarning($"[Sdx17.Quest] Complete: '{instance.QuestId}' still has unfinished objectives.");
                return null;
            }
            if (!instance.MarkCompleted()) return null;

            if (trackedQuest == instance)
            {
                trackedQuest = null;
                TrackedQuestChanged?.Invoke(null);
                OnTrackedQuestChanged.Invoke(null);
            }

            QuestCompleted?.Invoke(instance);
            OnQuestCompleted.Invoke(instance);
            return instance.Definition != null ? instance.Definition.Rewards : new QuestReward();
        }

        /// <summary>Completes an active quest by id. Returns its rewards, or null when it cannot complete.</summary>
        public QuestReward CompleteById(string questId) => Complete(GetQuest(questId));

        /// <summary>
        /// Abandons an active quest. Its progress is discarded. Repeatable quests can be
        /// accepted again; non-repeatable ones stay abandoned and cannot be re-accepted.
        /// Returns true when a quest was actually abandoned.
        /// </summary>
        public bool Abandon(QuestInstance instance)
        {
            if (instance == null || !instance.IsActive) return false;
            if (!instance.MarkAbandoned()) return false;

            if (trackedQuest == instance)
            {
                trackedQuest = null;
                TrackedQuestChanged?.Invoke(null);
                OnTrackedQuestChanged.Invoke(null);
            }

            QuestAbandoned?.Invoke(instance);
            OnQuestAbandoned.Invoke(instance);
            return true;
        }

        /// <summary>Abandons an active quest by id. Returns true when a quest was actually abandoned.</summary>
        public bool AbandonById(string questId) => Abandon(GetQuest(questId));

        /// <summary>
        /// Routes progress to every active quest with a matching objective.
        /// Call this from combat, pickups, dialogue and triggers, or use the typed
        /// helpers below. When a quest's objectives all finish, it completes at once.
        /// Returns how many active quests received progress.
        /// </summary>
        public int AddProgress(QuestObjectiveType type, string targetId, int amount = 1)
        {
            if (string.IsNullOrEmpty(targetId) || amount <= 0) return 0;

            int questsAdvanced = 0;
            var finished = new List<QuestInstance>();
            foreach (QuestInstance instance in questsById.Values)
            {
                if (!instance.IsActive) continue;
                int added = instance.AddProgress(type, targetId, amount);
                if (added <= 0) continue;
                questsAdvanced++;
                QuestProgressChanged?.Invoke(instance);
                OnQuestProgressChanged.Invoke(instance);
                if (instance.AreAllObjectivesComplete()) finished.Add(instance);
            }

            foreach (QuestInstance instance in finished) Complete(instance);
            return questsAdvanced;
        }

        /// <summary>Reports kills of <paramref name="enemyId"/> (count defaults to 1).</summary>
        public int ReportKill(string enemyId, int count = 1) => AddProgress(QuestObjectiveType.Kill, enemyId, count);

        /// <summary>Reports collecting <paramref name="itemId"/> (count defaults to 1).</summary>
        public int ReportCollect(string itemId, int count = 1) => AddProgress(QuestObjectiveType.Collect, itemId, count);

        /// <summary>Reports talking to the NPC with <paramref name="npcId"/>.</summary>
        public int ReportTalk(string npcId) => AddProgress(QuestObjectiveType.Talk, npcId, 1);

        /// <summary>Reports reaching the location with <paramref name="locationId"/>.</summary>
        public int ReportReachLocation(string locationId) => AddProgress(QuestObjectiveType.ReachLocation, locationId, 1);

        /// <summary>
        /// After a quest completes, accepts its follow-up (its <c>NextQuestId</c>) when
        /// the follow-up is known and can be accepted. Returns the new instance, or null.
        /// </summary>
        public QuestInstance TryAcceptNextInChain(QuestInstance finished)
        {
            if (finished?.Definition == null) return null;
            string nextId = finished.Definition.NextQuestId;
            if (string.IsNullOrEmpty(nextId)) return null;
            QuestDefinition next = FindDefinition(nextId);
            if (next == null)
            {
                Debug.LogWarning($"[Sdx17.Quest] Chain: next quest '{nextId}' is not registered on this manager.");
                return null;
            }
            return Accept(next, out _);
        }

        /// <summary>
        /// Returns a serializable snapshot of every quest this manager holds.
        /// The game persists this list itself (JSON, PlayerPrefs, Sdx17 Secure Save).
        /// </summary>
        public List<QuestSaveData> SaveAll()
        {
            var data = new List<QuestSaveData>(questsById.Count);
            foreach (QuestInstance instance in questsById.Values) data.Add(instance.ToSaveData());
            return data;
        }

        /// <summary>
        /// Restores quests from snapshots. Unknown quest ids are skipped with a warning.
        /// Pass the definitions this manager knows; the tracked quest is restored too.
        /// Call this after the manager's known definitions are assigned, before gameplay starts.
        /// </summary>
        public void LoadAll(List<QuestSaveData> saved)
        {
            if (saved == null) return;

            questsById.Clear();
            trackedQuest = null;

            foreach (QuestSaveData entry in saved)
            {
                if (entry == null || string.IsNullOrEmpty(entry.QuestId)) continue;
                QuestDefinition definition = FindDefinition(entry.QuestId);
                if (definition == null)
                {
                    Debug.LogWarning($"[Sdx17.Quest] LoadAll: skipping unknown quest id '{entry.QuestId}'.");
                    continue;
                }

                var instance = new QuestInstance(definition);
                instance.ApplySaveData(entry);
                questsById[entry.QuestId] = instance;
                if (entry.IsTracked && instance.IsActive) trackedQuest = instance;
            }

            if (trackedQuest != null)
            {
                trackedQuest.IsTracked = true;
                TrackedQuestChanged?.Invoke(trackedQuest);
                OnTrackedQuestChanged.Invoke(trackedQuest);
            }
        }

        /// <summary>Active quests only, in no guaranteed order.</summary>
        public List<QuestInstance> GetActiveQuests()
        {
            var result = new List<QuestInstance>();
            foreach (QuestInstance instance in questsById.Values)
            {
                if (instance.IsActive) result.Add(instance);
            }
            return result;
        }

        /// <summary>Completed quests only, in no guaranteed order.</summary>
        public List<QuestInstance> GetCompletedQuests()
        {
            var result = new List<QuestInstance>();
            foreach (QuestInstance instance in questsById.Values)
            {
                if (instance.IsCompleted) result.Add(instance);
            }
            return result;
        }
    }
}
