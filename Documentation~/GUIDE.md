# Sdx17 Quest Framework — Guide

**Version 0.1.0 — UNTESTED IN UNITY**

> **Read this first.** This code was written without Unity in the
> environment. It has **not** been opened, compiled, or played in
> Unity 2022.3 LTS. Nothing in this guide is a verified claim about
> behaviour in Unity until the TEST checklist in `README.md` has been
> run and every step passes. Do not ship it or call it
> production-ready before then.

- Package: `com.sdx17.quest` — "Sdx17 Quest Framework", version `0.1.0`
- Requires: Unity 2022.3 LTS (`unity: 2022.3`, `unityRelease: 0f1` in
  `package.json`). No paid dependencies. No rendering code, so it works
  with URP and Built-in.
- Namespace: `Sdx17.Quest` (demo code lives in `Sdx17.Quest.Demo`)
- License: MIT, Copyright 2026 Sdx17
- Companion files: `README.md` (setup + TEST checklist),
  `CHANGELOG.md`, `Samples~/Demo/README.md` (demo scene setup),
  `LISTING.md`, `SELLING_REQUIREMENTS.md`, `ThirdPartyNotices.txt`

---

## Contents

1. [Quickstart (5 minutes)](#1-quickstart-5-minutes)
2. [Concepts](#2-concepts)
3. [API reference](#3-api-reference)
4. [Config reference](#4-config-reference)
5. [Integration with the other Sdx17 systems](#5-integration-with-the-other-sdx17-systems)
6. [Troubleshooting / FAQ](#6-troubleshooting--faq)
7. [Performance notes](#7-performance-notes)
8. [Limitations](#8-limitations)

---

## 1. Quickstart (5 minutes)

### Step 1 — Install the package

Copy the package into your project (`Packages/com.sdx17.quest/`, or
`Assets/Sdx17Quest/`), or add it via Package Manager → Add package
from disk using `package.json`.

### Step 2 — Create the demo quest chain

Unity menu: **Sdx17 / Quest / Create Demo Quest Chain**
(`QuestTools.CreateDemoQuestChain` in `Editor/QuestTools.cs`).
This creates three `QuestDefinition` assets:

| Id | Title | Objective |
| --- | --- | --- |
| `quest_demo_collect` | Gather Herbs | Collect 3 `herb` |
| `quest_demo_hunt` | Slime Hunt | Defeat 2 `slime` (prerequisite: quest 1) |
| `quest_demo_report` | Report Back | Talk to `elder_mara` (prerequisite: quest 2) |

Or create a quest by hand: Project window → right-click →
Create → Sdx17 → Quest → Quest Definition
(menu **Sdx17 / Quest / Create Quest Definition**,
`QuestTools.CreateQuestDefinition`).

### Step 3 — Add the manager and the demo driver

1. Create an empty GameObject named `QuestManager`, add the
   `QuestManager` component, and drag the three demo quest assets
   into its **Known Definitions** list.
2. Create an empty GameObject named `QuestDemo`, add the `QuestDemo`
   component, assign the `QuestManager` GameObject to its Quest
   Manager field, and assign the `quest_demo_collect` asset to its
   First Quest field.
3. (Optional) Add a `QuestTracker` component to your tracker UI
   GameObject and assign the same `QuestManager`.

Full scene steps are in `Samples~/Demo/README.md`. Note: **no
`.unity` scene file is included** — you build the demo scene yourself
with the steps above.

### Step 4 — Play the demo

1. Press Play. The Console logs:
   `Accepted: Gather Herbs: 0/1 objectives done`.
2. Right-click the `QuestDemo` component header → **Demo: Collect 1
   Herb** three times. The first quest completes.
3. Choose **Demo: Accept Next Quest In Chain**. "Slime Hunt" is
   accepted (its prerequisite is now complete).
4. Choose **Demo: Defeat 1 Slime** twice. The second quest completes.
5. Choose **Demo: Accept Next Quest In Chain**, then **Demo: Talk to
   Elder Mara**. The third quest completes.
6. Try **Demo: Abandon Tracked Quest** and **Demo: Log Save Data**.

### Step 5 — Accept a quest from your own code

```csharp
using Sdx17.Quest;

public class NpcQuestGiver : MonoBehaviour
{
    [SerializeField] private QuestManager questManager;
    [SerializeField] private QuestDefinition quest;

    public void OnPlayerAccepts()
    {
        QuestInstance instance = questManager.Accept(quest, out string reason);
        if (instance == null)
            Debug.Log($"Cannot accept: {reason}");
    }
}
```

### Step 6 — Grant rewards on completion

Rewards are **data only** — the framework never grants them itself.
Subscribe to the completion event and grant them through your own
inventory / currency system:

```csharp
void Start()
{
    questManager.QuestCompleted += OnQuestCompleted;
}

void OnQuestCompleted(QuestInstance finished)
{
    QuestReward rewards = finished.Definition.Rewards;
    // Your game grants these — coins, XP, items — here.
    Debug.Log($"Completed {finished.Definition.Title}: " +
              $"{rewards.Coins} coins, {rewards.Experience} XP, " +
              $"{rewards.Items.Count} item types.");
}
```

Or wire the Inspector UnityEvent `QuestManager.OnQuestCompleted`
to a method that takes a `QuestInstance`.

---

## 2. Concepts

### QuestDefinition — the authoring asset

A `QuestDefinition` is a ScriptableObject you edit in the Inspector.
It is pure data: id, title, description, objectives, rewards, the
next quest in a chain (`NextQuestId`), prerequisite quest ids, and a
`Repeatable` flag. The asset **never changes at runtime**, so the same
definition can be shared by every player and every save. Runtime state
lives in `QuestInstance`.

### QuestInstance — one run of a quest

Created by `QuestManager` when a quest is accepted (never create one
directly except in tests — the constructor `new QuestInstance(definition)`
exists for that). It holds the definition, the current `QuestState`,
per-objective progress, and whether it is tracked. Progress is clamped
at each objective's required amount and never counts after completion.

### QuestState — the lifecycle

| Value | Meaning |
| --- | --- |
| `NotAccepted` | Never accepted, or abandoned and not repeatable. |
| `Active` | Accepted and in progress. |
| `Completed` | All objectives done and the completion event fired. |
| `Abandoned` | Accepted, then given up. Repeatable quests can be accepted again. |

### QuestManager — the owner of quest state

One `QuestManager` MonoBehaviour per scene (a persistent bootstrap
object works best). It owns accept / track / complete / abandon,
prerequisite and chain checks, progress routing, and save/load
snapshots. It exposes a singleton-style `QuestManager.Instance` —
set on `Awake`; the **first manager in the scene wins**, and a second
one logs a warning and disables itself (`enabled = false`) so quests
stay in one place. `Instance` is cleared to `null` in `OnDestroy`.

### Objectives — four kinds, string target ids

`QuestObjectiveType` has exactly four values: `Kill`, `Collect`,
`Talk`, `ReachLocation`. Each objective has a `TargetId` — a plain
string (enemy prefab name, item id, NPC id, location id). Your game
decides its own id scheme and reports progress back through
`QuestManager.ReportKill` / `ReportCollect` / `ReportTalk` /
`ReportReachLocation`. Target-id matching is **case-sensitive**
(`StringComparison.Ordinal` in `QuestInstance.AddProgress`), so
`"Slime"` and `"slime"` are different targets.

### Tracking — one quest at a time

At most one quest is tracked. Accepting a quest also tracks it when
`AutoTrackOnAccept` is true (the default). `QuestTracker` shows the
tracked quest; your UI renders it — the framework draws no UI itself.

### Rewards — data, not granting

`QuestReward` holds `Coins`, `Experience`, and a list of
`QuestItemReward` (item id + amount). `HasAnyReward` is true when any
of those is non-zero / non-empty. The game grants the reward when
`QuestCompleted` / `OnQuestCompleted` fires, or from the `QuestReward`
returned by `QuestManager.Complete`. If you never subscribe, nothing
is ever granted — that is by design, not a bug.

### Save data — snapshots only

`QuestSaveData` stores only the quest id, its `QuestState`,
per-objective progress (one integer per objective, in definition
order), and whether it was tracked. `QuestManager.SaveAll()` returns
a `List<QuestSaveData>`; **your game persists that list itself**
(JSON, PlayerPrefs, or the Sdx17 Secure Save system). `LoadAll`
restores it — unknown quest ids in a save are skipped with a warning,
so removing a quest definition does not corrupt a save file.

### Chains and prerequisites

- **Chain:** a definition's `NextQuestId` names its follow-up.
  After a quest completes, call
  `manager.TryAcceptNextInChain(finished)` to offer the follow-up.
  It returns `null` when there is no next quest, the next quest is
  unknown to the manager, or it cannot be accepted yet.
- **Prerequisites:** `PrerequisiteQuestIds` lists quests that must be
  completed before this one can be accepted. `CanAccept` returns
  `false` with a human-readable reason (e.g.
  `Complete 'Gather Herbs' first.`) you can show in dialogue.
- **Repeatable:** when `Repeatable` is true, a completed or abandoned
  quest can be accepted again (fresh progress). When false, a
  completed quest cannot be re-accepted, and an abandoned quest stays
  abandoned.

---

## 3. API reference

All public types live in namespace `Sdx17.Quest`. Every class and
method below was read from the files in `Runtime/` — nothing here is
invented. Private members and the editor classes are listed separately
at the end of this section.

### 3.1 `QuestObjectiveType` (enum) — `Runtime/QuestObjectiveType.cs`

| Value | Meaning |
| --- | --- |
| `Kill` | Defeat `TargetId` that many times. |
| `Collect` | Pick up / own that many of the item `TargetId`. |
| `Talk` | Speak to the NPC whose id is `TargetId`. |
| `ReachLocation` | Enter the trigger / area whose id is `TargetId`. |

### 3.2 `QuestObjectiveData` — `Runtime/QuestObjectiveType.cs`

One objective inside a `QuestDefinition`. `[Serializable]`, edited in
the Inspector as part of the definition asset.

| Member | Type | Notes |
| --- | --- | --- |
| `Type` | `QuestObjectiveType` | What kind of action advances this objective. Get/set. |
| `TargetId` | `string` | Id of the thing to kill / collect / talk to / reach. Must match the id the game passes to the `QuestManager` progress calls. Get/set; `null` becomes `string.Empty`. |
| `Description` | `string` | Player-facing line, e.g. `"Defeat 3 slimes"`. Shown by the tracker UI. Get/set; `null` becomes `string.Empty`. |
| `RequiredAmount` | `int` | How many times the action is needed. Get/set; clamped to at least 1 (`Mathf.Max(1, value)`). |
| `IsValid` | `bool` (get) | True when the objective has a target id and `RequiredAmount >= 1`. |

### 3.3 `QuestItemReward` — `Runtime/QuestObjectiveType.cs`

One item granted by a quest reward. `[Serializable]`.

| Member | Type | Notes |
| --- | --- | --- |
| `ItemId` | `string` | Id of the item, as used by the game's inventory system. Get/set; `null` becomes `string.Empty`. |
| `Amount` | `int` | How many of the item to grant. Get/set; clamped to at least 1. |

### 3.4 `QuestReward` — `Runtime/QuestObjectiveType.cs`

The reward block of a quest. `[Serializable]`. Rewards are data only
(see Concepts).

| Member | Type | Notes |
| --- | --- | --- |
| `Coins` | `int` | Coins granted on completion. Zero means no coin reward. Get/set; clamped to at least 0. |
| `Experience` | `int` | Experience granted on completion. Zero means no XP reward. Get/set; clamped to at least 0. |
| `Items` | `List<QuestItemReward>` | Items granted on completion. Get only; never null, may be empty. |
| `HasAnyReward` | `bool` (get) | True when this reward grants anything at all (`Coins > 0 \|\| Experience > 0 \|\| Items.Count > 0`). |

### 3.5 `QuestDefinition` — `Runtime/QuestDefinition.cs`

Authoring data for one quest, stored as a ScriptableObject asset.
Create from the Project window: right-click → Create → Sdx17 →
Quest → Quest Definition
(`[CreateAssetMenu(fileName = "New Quest", menuName = "Sdx17/Quest/Quest Definition", order = 0)]`).

| Member | Type | Notes |
| --- | --- | --- |
| `QuestId` | `string` (get) | Stable unique id, e.g. `"quest_slime_hunt"`. Used for save data, chains and prerequisites. Defaults to the asset name when left empty (see `OnValidate`). |
| `Title` | `string` (get) | Player-facing quest name. |
| `Description` | `string` (get) | Player-facing quest description shown in the quest log. `[TextArea(2, 4)]` in the Inspector. |
| `Objectives` | `List<QuestObjectiveData>` (get) | Objectives in order. Never null. A quest with no objectives completes on accept. |
| `Rewards` | `QuestReward` (get) | Rewards granted on completion. Never null. |
| `NextQuestId` | `string` (get) | Id of the quest that follows this one, or empty for the end of a chain. Used by `QuestManager.TryAcceptNextInChain`. |
| `PrerequisiteQuestIds` | `List<string>` (get) | Ids of quests that must be completed before this one can be accepted. Never null. |
| `Repeatable` | `bool` (get) | When true, an abandoned or completed quest may be accepted again. |
| `IsValid` | `bool` (get) | True when the definition has an id, a title, and every listed objective is valid. Note: a quest with **zero** objectives passes the objective loop, so `IsValid` can be true for an empty quest — and such a quest completes the moment it is accepted. |
| `SetQuestId(string id)` | `void` | Sets the quest id. Empty / null falls back to the asset name. Used by the editor tools and the self-test; prefer editing in the Inspector. |
| `SetTitle(string value)` | `void` | Sets the player-facing title. `null` becomes `string.Empty`. Used by the editor tools and the self-test. |
| `SetNextQuestId(string id)` | `void` | Sets the follow-up quest id for a chain. Empty clears the chain link. |

`OnValidate` (private, runs in the Editor): when `QuestId` is empty
it is set to the asset name, and every objective's `RequiredAmount`
is clamped to at least 1 so progress math never divides by zero.

### 3.6 `QuestState` (enum) — `Runtime/QuestInstance.cs`

`NotAccepted`, `Active`, `Completed`, `Abandoned` — see the lifecycle
table in Concepts.

### 3.7 `QuestSaveData` — `Runtime/QuestInstance.cs`

Serializable snapshot of one quest, for save files. `[Serializable]`.
Only plain data is stored.

| Member | Type | Notes |
| --- | --- | --- |
| `QuestId` | `string` | Id of the quest this snapshot belongs to. Get/set; `null` becomes `string.Empty`. |
| `State` | `QuestState` | State the quest was in when saved. Get/set. |
| `ObjectiveProgress` | `int[]` | Progress per objective index, matching the definition's objective order. Get/set; `null` becomes an empty array. |
| `IsTracked` | `bool` | Whether this quest was the tracked one when saved. Get/set. |

### 3.8 `QuestInstance` — `Runtime/QuestInstance.cs`

Runtime state for one accepted quest. `[Serializable]`. Created by
`QuestManager` on accept; never create one directly except in tests.

| Member | Type | Notes |
| --- | --- | --- |
| `QuestInstance(QuestDefinition definition)` | constructor | Creates an instance bound to a definition, with zeroed progress. State starts `Active`. |
| `Definition` | `QuestDefinition` (get) | The definition asset this instance runs. Null only when built without one (tests). |
| `QuestId` | `string` (get) | Shortcut for `Definition.QuestId`; empty when there is no definition. |
| `State` | `QuestState` (get) | Current lifecycle state. |
| `IsActive` | `bool` (get) | True while the quest is accepted and not yet finished or abandoned. |
| `IsCompleted` | `bool` (get) | True once the quest has been completed. |
| `IsTracked` | `bool` | True while this quest is the one the tracker UI shows. Get is public; set is `internal` (the manager owns it). |
| `GetProgress(int index)` | `int` | Progress (0..`RequiredAmount`) for the objective at `index`; 0 for a bad index. |
| `IsObjectiveComplete(int index)` | `bool` | True when the objective at `index` reached its required amount. False for a bad index or no definition. |
| `AreAllObjectivesComplete()` | `bool` | True when every objective is complete. A quest with no objectives is complete as soon as it is accepted. False when there is no definition. |
| `AddProgress(QuestObjectiveType type, string targetId, int amount)` | `int` | Adds progress to every matching, still-incomplete objective of the given type and target id. Returns the total amount actually added (0 when nothing matched). Progress is clamped at the required amount and never counts after completion. Returns 0 when the instance is not `Active`, the target id is empty, or `amount <= 0`. |
| `MarkCompleted()` | `bool` | Marks the instance completed. Only an active instance can complete; returns `false` otherwise. |
| `MarkAbandoned()` | `bool` | Marks the instance abandoned and clears tracking. Only an active instance can be abandoned; returns `false` otherwise. |
| `ToSaveData()` | `QuestSaveData` | Builds a save snapshot of this instance (progress array is cloned). |
| `ApplySaveData(QuestSaveData saveData)` | `void` | Restores runtime state from a snapshot. Does nothing when the snapshot or the definition is null. Progress is clamped to each objective's required amount and padded/truncated to the objective count. |
| `GetSummary()` | `string` | One-line progress summary, e.g. `"Slime Hunt: 2/3 objectives done"`. Returns `"Unknown quest"` when there is no definition. |

### 3.9 `QuestManager` — `Runtime/QuestManager.cs`

Central quest runtime. `MonoBehaviour` in namespace `Sdx17.Quest`.
Add one to a scene (or a persistent bootstrap GameObject), assign the
quest definitions it should know about, and drive it from NPCs,
triggers, combat and pickups.

**Properties**

| Member | Type | Notes |
| --- | --- | --- |
| `Instance` | `static QuestManager` (get, private set) | Singleton-style access. Set on `Awake`; the first manager in the scene wins. Cleared on `OnDestroy`. |
| `KnownDefinitions` | `List<QuestDefinition>` (get) | All quest definitions this manager knows about. Never null. `AcceptById`, chains and load all resolve against this list. |
| `PersistAcrossScenes` | `bool` | When true (default), this manager survives scene loads via `DontDestroyOnLoad`. Get/set. |
| `AutoTrackOnAccept` | `bool` | When true (default), accepting a quest also makes it the tracked quest. Get/set. |
| `TrackedQuest` | `QuestInstance` (get) | Currently tracked quest, or `null` when nothing is tracked. |
| `AllQuests` | `IReadOnlyCollection<QuestInstance>` (get) | All instances the manager holds (active, completed and abandoned). |

**C# events** (subscribe in code — game systems, save system):

| Event | Fired when |
| --- | --- |
| `QuestAccepted` (`Action<QuestInstance>`) | After a quest is accepted. |
| `QuestCompleted` (`Action<QuestInstance>`) | After a quest is completed (all objectives done). |
| `QuestAbandoned` (`Action<QuestInstance>`) | After a quest is abandoned. |
| `QuestProgressChanged` (`Action<QuestInstance>`) | Whenever any objective progress changes. |
| `TrackedQuestChanged` (`Action<QuestInstance>`) | When the tracked quest changes (including tracking nothing — the argument is `null`). |

**UnityEvents** (wire in the Inspector — UI, audio, dialogue
triggers). Same five moments, as `UnityEvent<QuestInstance>` fields,
each initialised to a new event:

`OnQuestAccepted`, `OnQuestCompleted`, `OnQuestAbandoned`,
`OnQuestProgressChanged`, `OnTrackedQuestChanged`.

**Methods — setup and lookup**

| Method | Returns | Notes |
| --- | --- | --- |
| `RegisterDefinition(QuestDefinition definition)` | `void` | Registers a definition at runtime (for quests loaded from DLC, addressables or a mod). Does nothing for a null definition or one with an empty id; does not add duplicates. |
| `FindDefinition(string questId)` | `QuestDefinition` | Finds a known definition by id, or `null` when unknown / id empty. |
| `GetQuest(string questId)` | `QuestInstance` | Returns the instance for a quest id, or `null` when never accepted / id empty. |
| `IsQuestActive(string questId)` | `bool` | True when the quest is currently active. |
| `IsQuestCompleted(string questId)` | `bool` | True when the quest has been completed at least once. |

**Methods — accept**

| Method | Returns | Notes |
| --- | --- | --- |
| `CanAccept(QuestDefinition definition, out string reason)` | `bool` | True when a quest may be accepted: the definition exists, it is not already active or (when not repeatable) completed, and every prerequisite quest is completed. Returns `false` with a `reason` string explaining why, for NPC dialogue (`"You must finish X first"`). Reasons used by the code: `"No quest definition was given."`, `"That quest is already active."`, `"That quest is already completed."`, `"Complete '<name>' first."` (prerequisite title, or its id when the prerequisite definition is unknown). |
| `Accept(QuestDefinition definition, out string reason)` | `QuestInstance` | Accepts a quest when `CanAccept` allows it. Returns the instance, or `null` with a `reason`. Re-accepting a repeatable abandoned or completed quest starts it fresh. Auto-tracks when `AutoTrackOnAccept` is true. Fires `QuestAccepted` / `OnQuestAccepted`. A quest with no objectives is completed at once (see `Complete` below). |
| `AcceptById(string questId)` | `QuestInstance` | Accepts a quest by id. Returns `null` when the id is unknown (logs a warning: `AcceptById: unknown quest id '<id>'. Register the definition first.`) or cannot be accepted. The `reason` from `Accept` is discarded here — use `Accept` + `CanAccept` when you need to show the player why. |

**Methods — track**

| Method | Returns | Notes |
| --- | --- | --- |
| `Track(QuestInstance instance)` | `void` | Makes the given quest the tracked one shown by the UI. Pass `null` to clear tracking — there is **no separate `Untrack` method**; `Track(null)` is how tracking is cleared. Tracking a non-active quest logs a warning (`Only an active quest can be tracked...`) and does nothing. Fires `TrackedQuestChanged` / `OnTrackedQuestChanged`. |
| `TrackById(string questId)` | `void` | Tracks an active quest by id. Does nothing (with a warning) when there is no accepted quest with that id, and `Track` itself refuses a non-active quest. |

**Methods — complete and abandon**

| Method | Returns | Notes |
| --- | --- | --- |
| `Complete(QuestInstance instance)` | `QuestReward` | Completes an active quest whose objectives are all done, fires the completion events, and returns its rewards so the game can grant them. Returns `null` when the quest cannot complete yet: `null` instance, not active (warning logged), or objectives unfinished (warning logged). On success it clears tracking if this quest was tracked (firing the tracked-changed events with `null`) and returns `instance.Definition.Rewards` — or a new empty `QuestReward` when the definition is null. |
| `CompleteById(string questId)` | `QuestReward` | Completes an active quest by id. Returns its rewards, or `null` when it cannot complete. |
| `Abandon(QuestInstance instance)` | `bool` | Abandons an active quest. Its progress is discarded. Repeatable quests can be accepted again; non-repeatable ones stay abandoned and cannot be re-accepted. Returns `true` when a quest was actually abandoned, `false` for a null or non-active instance. Clears tracking if this quest was tracked. Fires `QuestAbandoned` / `OnQuestAbandoned`. |
| `AbandonById(string questId)` | `bool` | Abandons an active quest by id. Returns `true` when a quest was actually abandoned. |

**Methods — progress**

| Method | Returns | Notes |
| --- | --- | --- |
| `AddProgress(QuestObjectiveType type, string targetId, int amount = 1)` | `int` | Routes progress to every active quest with a matching objective. Returns how many **active quests** received progress (not the amount added). When a quest's objectives all finish, it completes at once. Returns 0 for an empty target id or `amount <= 0`. Fires `QuestProgressChanged` / `OnQuestProgressChanged` per quest advanced, then completes finished quests. |
| `ReportKill(string enemyId, int count = 1)` | `int` | Reports kills of `enemyId` (count defaults to 1). Calls `AddProgress(QuestObjectiveType.Kill, ...)`. |
| `ReportCollect(string itemId, int count = 1)` | `int` | Reports collecting `itemId` (count defaults to 1). |
| `ReportTalk(string npcId)` | `int` | Reports talking to the NPC with `npcId`. Always amount 1. |
| `ReportReachLocation(string locationId)` | `int` | Reports reaching the location with `locationId`. Always amount 1. |

**Methods — chains, save/load, queries**

| Method | Returns | Notes |
| --- | --- | --- |
| `TryAcceptNextInChain(QuestInstance finished)` | `QuestInstance` | After a quest completes, accepts its follow-up (its `NextQuestId`) when the follow-up is known and can be accepted. Returns the new instance, or `null` (with a warning when the next quest is not registered on this manager). |
| `SaveAll()` | `List<QuestSaveData>` | A serializable snapshot of every quest this manager holds. The game persists this list itself. |
| `LoadAll(List<QuestSaveData> saved)` | `void` | Restores quests from snapshots. Does nothing for a null list. Clears the manager's current quests first. Unknown quest ids are skipped with a warning. The tracked quest is restored too (only when its saved state is still `Active`), and the tracked-changed events fire. Call this after the manager's known definitions are assigned, before gameplay starts. |
| `GetActiveQuests()` | `List<QuestInstance>` | Active quests only, in no guaranteed order. |
| `GetCompletedQuests()` | `List<QuestInstance>` | Completed quests only, in no guaranteed order. |

### 3.10 `QuestTracker` — `Runtime/QuestTracker.cs`

UI hook for the quest tracker panel. `MonoBehaviour`. Attach it to
the tracker UI GameObject, assign the `QuestManager`, and either wire
the UnityEvents below to your UI in the Inspector, or read
`TrackedQuest` and `GetTrackedQuestText()` from your own UI script.
**This component draws no UI itself**, so it works with any UI system
(UGUI, UI Toolkit, or a custom renderer).

| Member | Type | Notes |
| --- | --- | --- |
| `QuestManager` | `QuestManager` | The manager this tracker listens to. Falls back to `QuestManager.Instance` when unassigned. Get/set. |
| `TrackedQuest` | `QuestInstance` (get) | The quest currently shown by the tracker, or `null` when nothing is tracked (or no manager exists). |
| `HasTrackedQuest` | `bool` (get) | True when a quest is currently tracked and active. |
| `OnTrackedQuestChanged` | `UnityEvent<QuestInstance>` | Inspector event fired when the tracked quest changes. |
| `OnTrackedQuestProgressChanged` | `UnityEvent<QuestInstance>` | Inspector event fired when any tracked quest progress changes. Only fires for the tracked quest. |
| `OnTrackedQuestCompleted` | `UnityEvent<QuestInstance>` | Inspector event fired when the tracked quest completes. |
| `GetTrackedQuestText()` | `string` | Multi-line text for the tracker panel, e.g. `"Slime Hunt\nDefeat 3 slimes 2/3\nTalk to Elder Mara 0/1"`. Each objective line shows its `Description` (or `"<Type> <TargetId>"` when the description is empty) plus `"<progress>/<required>"`, or `"Done"` when that objective is complete. Returns `"No quest tracked."` when nothing is tracked. |
| `GetTrackedQuestFraction()` | `float` | Progress of the tracked quest as 0..1 — completed objectives divided by total objectives (1 when all objectives are done, 0 with no objectives, 0 when nothing is tracked). |

Lifecycle: subscribes to the manager's `TrackedQuestChanged`,
`QuestProgressChanged` and `QuestCompleted` C# events in `OnEnable`,
unsubscribes in `OnDisable` (guarded so scene teardown does not throw
when the manager is already destroyed).

### 3.11 Editor and demo code (not runtime API)

| File / type | Members | Notes |
| --- | --- | --- |
| `Editor/QuestDefinitionEditor.cs` — `QuestDefinitionEditor : UnityEditor.Editor` | `OnInspectorGUI()` (override) | Custom inspector for `QuestDefinition` with validation warnings. Editor-only. |
| `Editor/QuestTools.cs` — `QuestTools` (static) | `CreateQuestDefinition()` — menu `Sdx17/Quest/Create Quest Definition`; `CreateDemoQuestChain()` — menu `Sdx17/Quest/Create Demo Quest Chain` | Editor-only menu items. |
| `Samples~/Demo/QuestDemo.cs` — `QuestDemo : MonoBehaviour` (namespace `Sdx17.Quest.Demo`) | `QuestManager` property (falls back to `QuestManager.Instance`); `CollectHerb()`, `DefeatSlime()`, `TalkToElderMara()`, `AcceptNextInChain()`, `AbandonTrackedQuest()`, `LogSaveData()` — all public, all `[ContextMenu("Demo: ...")]`, so any UI Button's On Click event can call them | Demo driver only. Accepts the first quest on `Start` when `autoAcceptFirstQuest` is true (default) and logs every quest event to the Console. `LogSaveData` logs each entry as `Save: <id> state=<state> tracked=<bool> progress=[...]`. |
| `Tests~/QuestSelfTest.cs` — `QuestSelfTest : MonoBehaviour` | Play-mode self-test logging PASS/FAIL | Attach to a GameObject, press Play, confirm the Console ends with "0 failed". Creates its own manager when none exists. Remove it from a showcase demo scene afterwards. |

Assembly definitions: Runtime `Sdx17.Quest.Runtime`, Editor
`Sdx17.Quest.Editor` (references Runtime).

---

## 4. Config reference

### 4.1 `QuestDefinition` fields (per quest asset, Inspector)

| Field | Type | Default / rule | Notes |
| --- | --- | --- | --- |
| Quest Id | `string` | Empty → asset name (`OnValidate`) | Stable unique id, e.g. `quest_slime_hunt`. Used for save data, chains and prerequisites. **Do not rename an id after players have saves** — `LoadAll` looks saves up by this id and skips unknown ids. |
| Title | `string` | — | Player-facing quest name. Required for `IsValid`. |
| Description | `string` | `[TextArea(2, 4)]` | Player-facing text for the quest log. |
| Objectives | `List<QuestObjectiveData>` | Empty list | Each entry: Type (`Kill`, `Collect`, `Talk`, `ReachLocation`), Target Id, Description (e.g. `"Defeat 3 slimes"`), Required Amount (min 1, clamped by `OnValidate` and by the property setter). An objective with an empty Target Id makes `IsValid` false. A quest with no objectives completes on accept. |
| Rewards | `QuestReward` | `Coins 0, Experience 0, Items empty` | Data only; the game grants them on completion. |
| Next Quest Id | `string` | Empty = end of chain | Id of the follow-up quest. |
| Prerequisite Quest Ids | `List<string>` | Empty list | Quests that must be completed before this one can be accepted. Empty entries are skipped by `CanAccept`. |
| Repeatable | `bool` | `false` | When true, a completed or abandoned quest can be accepted again. |

### 4.2 `QuestManager` fields (component, Inspector)

| Field | Type | Default | Notes |
| --- | --- | --- | --- |
| Known Definitions | `List<QuestDefinition>` | Empty list | The quest definitions this manager knows about. `AcceptById`, chains and load all resolve against this list. A quest not in this list cannot be accepted by id and its save entry is skipped on load. Add definitions at runtime with `RegisterDefinition`. |
| Persist Across Scenes | `bool` | `true` | `DontDestroyOnLoad` for the manager. Set it false if each scene owns its own manager — but remember only the first `QuestManager` in a scene becomes `Instance`, and a second one disables itself. |
| Auto Track On Accept | `bool` | `true` | Accepting a quest also tracks it. Set false if your UI lets the player pick which quest to track (`Track` / `TrackById`). |

### 4.3 `QuestTracker` fields (component, Inspector)

| Field | Type | Default | Notes |
| --- | --- | --- | --- |
| Quest Manager | `QuestManager` | Unassigned → `QuestManager.Instance` | The manager this tracker listens to. |

### 4.4 `QuestDemo` fields (demo scene only)

| Field | Type | Default | Notes |
| --- | --- | --- | --- |
| Quest Manager | `QuestManager` | Unassigned → `QuestManager.Instance` | The manager driving the demo. |
| First Quest | `QuestDefinition` | — | Quest accepted on Play (demo chain: `quest_demo_collect`). |
| Auto Accept First Quest | `bool` | `true` | Accept the first quest on `Start`. |

---

## 5. Integration with the other Sdx17 systems

There are 11 sibling Sdx17 systems (see the `sdx17-unity-*` repos):
NPC, Dialogue, Inventory, Secure Save, UI, Combat, Terrain Tools,
Multiplayer, Matchmaking, Admin, and the Developer Plugin. The Quest
Framework integrates with all of them **through its own public API
below** — C# events, UnityEvents, the four `Report*` methods, and
`SaveAll` / `LoadAll`. It never calls into those systems itself, and
it holds no reference to any of them.

> Honesty note: the integration patterns in this section describe
> what to wire on the **Quest side** (method and event names verified
> in `Runtime/`). The sibling systems' own class and method names are
> **not** listed here, because they were not verified against those
> repos while writing this guide — check each sibling repo's README
> for its exact API before wiring the other end. Do not treat any
> sibling-system name below as a verified API.

### 5.1 NPC — quest-giver

The NPC checks `manager.CanAccept(definition, out reason)` and shows
the reason in dialogue (`"Complete 'Gather Herbs' first"`), then
calls `manager.Accept(definition, out _)` on the player's "Accept"
choice. When a quest completes, call
`manager.TryAcceptNextInChain(finished)` to offer the follow-up from
the same NPC. Use `manager.IsQuestActive(id)` /
`manager.IsQuestCompleted(id)` to decide which dialogue branch the
NPC shows (available / in progress / done / repeatable).

```csharp
if (questManager.CanAccept(quest, out string reason))
    ShowAcceptDialogue(quest);
else
    ShowRefusalDialogue(reason);
```

### 5.2 Dialogue — Talk objectives

A Talk objective completes when dialogue ends — call
`manager.ReportTalk(npcId)` from the dialogue system's
"conversation ended" callback, using the same NPC id the objective's
`TargetId` holds. `ReportTalk` always reports amount 1. If your
dialogue can be skipped or exited early, only call `ReportTalk` when
the conversation actually reached its end, or a skipped talk will
count as completed.

### 5.3 Inventory — rewards

Subscribe to `QuestCompleted` (C# event) or wire the Inspector
`OnQuestCompleted` UnityEvent, and grant
`instance.Definition.Rewards` through the inventory system: coins to
the wallet, XP to the player, each `QuestItemReward` (`ItemId` +
`Amount`) to the inventory. The framework never touches the
inventory itself. The same `QuestReward` is returned by
`manager.Complete(quest)` if you complete quests explicitly instead
of relying on auto-completion.

```csharp
questManager.QuestCompleted += finished =>
{
    QuestReward r = finished.Definition.Rewards;
    if (r.HasAnyReward) GrantRewards(r); // your inventory code
};
```

### 5.4 Secure Save — persistence

On save, store `manager.SaveAll()` (a `List<QuestSaveData>`,
JSON-serializable) alongside the rest of the save; on load, after the
manager's Known Definitions are assigned, call
`manager.LoadAll(savedList)`. Unknown quest ids in a save are skipped
with a warning, so removing a quest definition does not corrupt a
save file. Call `LoadAll` before gameplay starts — it clears the
manager's current quests first, so loading twice discards the first
load. The tracked quest is restored only when its saved state is
still `Active`.

### 5.5 UI — tracker and quest log

`QuestTracker` fires `OnTrackedQuestChanged`,
`OnTrackedQuestProgressChanged` and `OnTrackedQuestCompleted`; bind
those to your tracker panel, or poll `GetTrackedQuestText()` /
`GetTrackedQuestFraction()`. A quest log can list
`manager.GetActiveQuests()` and `manager.GetCompletedQuests()`
(order is not guaranteed — sort them yourself if the log needs a
stable order). The framework draws no UI: no quest-log or tracker
prefabs are included.

```csharp
// In your UI script, every frame or on the tracker events:
trackerText.text = questTracker.GetTrackedQuestText();
progressBar.fillAmount = questTracker.GetTrackedQuestFraction();
```

### 5.6 Combat — Kill objectives

When combat defeats an enemy, call
`manager.ReportKill(enemyId, count)` with the same id the objective's
`TargetId` holds (for example the enemy prefab name). `ReportKill`
defaults `count` to 1. Progress is routed to every active quest with
a matching Kill objective, so one kill can advance several quests at
once — and `AddProgress` returns how many quests advanced, which is
useful for logging, not for assuming a single quest advanced.

### 5.7 Terrain Tools — ReachLocation objectives

When the player enters a trigger or area, call
`manager.ReportReachLocation(locationId)` with the location id the
objective's `TargetId` holds. `ReportReachLocation` always reports
amount 1. Guard the trigger so it fires once per entry (or once ever
for a one-time location) — repeated trigger entries re-report, though
progress is clamped at the required amount and a completed objective
ignores further reports.

### 5.8 Multiplayer

The framework is single-player by design: `QuestManager` is a
scene-local `MonoBehaviour` with a static `Instance`, it keeps quest
state in a plain `Dictionary<string, QuestInstance>`, and nothing in
it is networked. For multiplayer, run one `QuestManager` per player
on the authoritative side (server or host) and replicate quest state
yourself — for example by sending `SaveAll()` snapshots
(`List<QuestSaveData>`) to clients, or by forwarding the same
`ReportKill` / `ReportCollect` / `ReportTalk` /
`ReportReachLocation` calls the server already makes. Do **not**
assume two clients with separate managers will stay in sync, and do
not trust progress reported by a client without server validation.
This pattern is a design suggestion, not a tested multiplayer
implementation.

### 5.9 Matchmaking

Quests do not interact with matchmaking directly. The only sensible
touchpoints are gating and rewards: check
`manager.IsQuestCompleted(id)` before letting a player queue for a
mode a quest unlocks, or grant a quest reward (via `QuestCompleted`)
when a match ends. Keep matchmaking and quest state in separate
systems and pass only quest ids and completion booleans between them.

### 5.10 Admin

Admin tooling can inspect and override quest state through the same
public API players use: `GetQuest(id)` / `GetActiveQuests()` /
`GetCompletedQuests()` to inspect, `CompleteById(id)` to force a
completion (returns `null` — and logs a warning — when objectives
are unfinished, so an admin "complete" must either finish the
objectives first or go through `AddProgress` instead), and
`AbandonById(id)` to reset a stuck quest. There is no built-in admin
UI and no remote admin API in this package.

### 5.11 Developer Plugin

The Developer Plugin (or any debug tooling) can drive the framework
without gameplay code, exactly as the demo does: call the `Report*`
methods, `AcceptById`, `CompleteById`, `AbandonById`, and `SaveAll`
/ `LoadAll` from debug buttons or console commands. `QuestDemo`
(`Samples~/Demo/QuestDemo.cs`) is the reference implementation of
that pattern — its public methods (`CollectHerb`, `DefeatSlime`,
`TalkToElderMara`, `AcceptNextInChain`, `AbandonTrackedQuest`,
`LogSaveData`) are plain public methods wired to `[ContextMenu]`
entries and usable from any UI Button On Click event.

---

## 6. Troubleshooting / FAQ

### Q1. I call `AcceptById("quest_slime_hunt")` and get `null`. Why?

The quest id is unknown to this manager, or the quest cannot be
accepted. `AcceptById` logs a warning for an unknown id
(`AcceptById: unknown quest id '<id>'. Register the definition
first.`) — check that the definition asset is in the manager's
**Known Definitions** list (or registered with
`RegisterDefinition`). If the id is known, the quest may already be
active, already completed and not repeatable, or blocked by a
prerequisite. `AcceptById` discards the reason — call
`CanAccept(definition, out reason)` first to see why; the reason
strings are listed in §3.9.

### Q2. Progress is reported but the objective never advances. Why?

Three usual causes, in order of likelihood:

1. **Target id mismatch.** Matching is case-sensitive
   (`StringComparison.Ordinal`). `"Slime"` does not match `"slime"`,
   and a trailing space breaks the match. Make the id your game
   passes to `ReportKill` / `ReportCollect` / `ReportTalk` /
   `ReportReachLocation` byte-identical to the objective's `TargetId`.
2. **Wrong objective type.** A `Kill` report only advances `Kill`
   objectives; reporting a kill for a `Collect` objective does
   nothing. Check the objective's `Type` in the Inspector.
3. **The quest is not active.** Progress only routes to `Active`
   instances. A quest that was never accepted, already completed, or
   abandoned receives nothing. Check `manager.IsQuestActive(id)`.

`AddProgress` returns how many active quests received progress — if
it returns 0, nothing matched. Log that value while debugging.

### Q3. The quest completes but the player gets no coins / XP / items. Why?

Rewards are **data only**. `QuestManager` never grants anything — it
fires `QuestCompleted` / `OnQuestCompleted` and returns the
`QuestReward` from `Complete`. If nothing subscribes to the event,
nothing is granted. Subscribe as shown in §1 Step 6 and grant the
reward through your inventory / currency system. Also check
`QuestReward.HasAnyReward` — a reward block of 0 coins, 0 XP and no
items grants nothing by definition.

### Q4. The tracker shows "No quest tracked." — where did my quest go?

Either nothing was ever tracked, or tracking was cleared. Accepting
a quest only auto-tracks it when `AutoTrackOnAccept` is true
(default). Tracking is cleared when the tracked quest completes or
is abandoned (the tracked-changed events fire with `null`), and
`Track(null)` clears it explicitly. Also, `Track` refuses a
non-active quest with a warning — you cannot track a quest before
accepting it. Check `manager.TrackedQuest` (null means nothing is
tracked) and `questTracker.HasTrackedQuest`.

### Q5. I loaded a save and a quest is missing / its progress reset. Why?

`LoadAll` skips save entries whose quest id is unknown to the
manager, with a warning (`LoadAll: skipping unknown quest id
'<id>'.`). That happens when the definition was removed, renamed
(its `QuestId` changed), or simply not added to **Known
Definitions** before loading. Call `LoadAll` only after the
manager's Known Definitions are assigned, and never rename a
`QuestId` after players have saves. Also note `LoadAll` clears the
manager's current quests first — loading twice discards the first
load. Progress is clamped to each objective's required amount and
padded/truncated to the objective count, so a save from an older
definition with a different objective count loads with adjusted
progress rather than crashing.

### Q6. I have two `QuestManager`s (for example after a scene load) and quests behave strangely. Why?

Only the first manager in a scene becomes `QuestManager.Instance`;
a second one logs a warning and disables itself, so its quests and
events do nothing — but the GameObject and its Known Definitions
still exist and can confuse you in the Inspector. Keep exactly one
manager: use `PersistAcrossScenes` (default `true`,
`DontDestroyOnLoad`) on a bootstrap object, and do not place a
second `QuestManager` in later scenes. If you need a fresh manager
per scene, set `PersistAcrossScenes` to `false` and make sure the old
scene's manager is destroyed on unload.

### Q7. `Complete` returns `null` even though the objectives look done. Why?

`Complete` returns `null` (with a warning) when the instance is not
`Active` or when `AreAllObjectivesComplete()` is false. The usual
cause is a second objective you forgot — `GetSummary()` shows
`"<done>/<total> objectives done"`, so log it. Another cause: the
quest already auto-completed. `AddProgress` completes a quest the
moment its last objective finishes, so by the time you call
`Complete` manually the instance is already `Completed` and
`Complete` returns `null`. In that case the rewards were already
handed to your `QuestCompleted` subscribers.

### Q8. My quest log order changes between runs. Is that a bug?

No — `GetActiveQuests()` and `GetCompletedQuests()` document that
their order is not guaranteed (they iterate a dictionary). Sort the
returned lists yourself (by title, id, or accept order) if the log
needs a stable order.

### Q9. Can I re-accept a quest I abandoned?

Only when the definition's `Repeatable` flag is true. A repeatable
abandoned (or completed) quest starts fresh on re-accept — its old
progress is discarded. A non-repeatable abandoned quest stays
abandoned and `CanAccept` refuses it. The demo's quests are not
repeatable, so abandoning one in the demo means it cannot be
re-accepted in that Play session.

### Q10. The Console shows a warning when I call `TrackById` / `CompleteById` with an id that was never accepted. Is something broken?

No. `TrackById` warns (`TrackById: no accepted quest with id
'<id>'.`) and does nothing; `CompleteById` delegates to
`Complete(null)`, which returns `null`. Both are safe to call —
guard the call with `manager.GetQuest(id) != null` or
`manager.IsQuestActive(id)` if you want silence.

---

## 7. Performance notes

- **No per-frame work.** `QuestManager` and `QuestTracker` do no
  work in `Update`. All cost happens on events: accept, progress
  reports, completion, save/load.
- **Progress routing is a linear scan.** `AddProgress` iterates every
  quest instance the manager holds and, per instance, every
  objective. That is trivial for a normal quest count (tens of
  quests, a few objectives each). If a game ever held thousands of
  active quests, the scan would grow linearly — but that is not a
  realistic quest design, and no pooling or indexing is provided.
- **Events fire per quest advanced.** One `ReportKill` that advances
  three quests fires `QuestProgressChanged` /
  `OnQuestProgressChanged` three times (once per quest), then
  completes any finished quests. Keep event subscribers cheap; do
  not rebuild an entire quest-log UI from scratch on every progress
  event — update only the changed line, or throttle UI refreshes.
- **Save snapshots allocate.** `SaveAll()` builds a new
  `List<QuestSaveData>` and `ToSaveData()` clones each progress
  array. That is fine for save/load moments; do not call `SaveAll()`
  every frame.
- **No rendering, no physics, no assets.** The package ships no
  textures, audio, prefabs or rendering code, so it adds no draw
  calls, no memory for art, and no GPU cost. Its cost is a handful of
  small C# objects per quest.
- **Strings are the ids.** Target ids and quest ids are plain
  strings compared with `StringComparison.Ordinal`. That is fast
  enough for quest volumes, but keep ids short and stable.

---

## 8. Limitations

Honest limits — read before buying or building on this package:

1. **UNTESTED IN UNITY (v0.1.0).** The code has not been opened,
   compiled, or played in Unity. Every claim in this guide is
   unverified until the TEST checklist in `README.md` passes in
   Unity 2022.3 LTS. Treat the package as a starting point, not a
   proven asset.
2. **Rewards are data only.** Coins, XP and items are never granted
   by this package. Your game must subscribe to `QuestCompleted` /
   `OnQuestCompleted` (or use the `QuestReward` returned by
   `Complete`) and grant them through its own systems. If you never
   wire that up, quests complete and nothing is awarded.
3. **No UI.** There are no quest-log or tracker prefabs.
   `QuestTracker` exposes the tracked quest, a formatted text string
   (`GetTrackedQuestText()`) and a progress fraction
   (`GetTrackedQuestFraction()`); your UI renders them.
4. **No dialogue, combat, inventory or save system.** Those are
   integration points only (§5). This package does not implement
   them, and the sibling Sdx17 systems are separate packages.
5. **No built-in demo scene.** `Samples~/Demo` ships a driver script
   (`QuestDemo`) and a setup README only — you build the scene
   yourself. The demo's context-menu actions simulate kills,
   collects and talks; they are not gameplay.
6. **Single-player by design.** Nothing is networked. Multiplayer
   needs a server-authoritative wrapper you write yourself (§5.8).
7. **One tracked quest.** Only one quest can be tracked at a time.
   There is no multi-track, no quest sorting (query order is not
   guaranteed), and no built-in quest-log UI.
8. **Four objective types only.** `Kill`, `Collect`, `Talk`,
   `ReachLocation`. There is no timed objective, no escort objective,
   no "reach level X" objective, and no custom objective type — a
   new kind needs a code change to the enum and to the reporting
   methods.
9. **String ids are the contract.** Quest ids, target ids, chain
   links and prerequisites are all plain strings. Typos fail
   silently-ish (warnings in the Console, `null` returns) rather
   than at compile time. Renaming a `QuestId` after players have
   saves orphans those saves' entries.
10. **No validation beyond the inspector.** `QuestDefinition.IsValid`
    checks id, title and objective validity, and the custom inspector
    shows validation warnings — but nothing stops you from shipping a
    quest whose `NextQuestId` points at a quest the manager does not
    know, or whose prerequisite can never be completed. Check chains
    and prerequisites by hand (or with the self-test) before release.
11. **No versioning / migration of save data.** `QuestSaveData`
    stores progress per objective index. If you reorder, add or
    remove objectives in a definition, old saves load with progress
    mapped to the new order (clamped and padded/truncated) — which
    may not mean what you intended.
12. **Editor tooling is minimal.** One custom inspector with
    validation and two menu items. There is no quest graph editor, no
    bulk import, and no localisation support — titles and
    descriptions are plain strings on the asset.
