# Sdx17 Quest Framework

**Version 0.1.0 — UNTESTED IN UNITY**

A lightweight, data-driven quest framework for Unity 2022.3 LTS: quests as
ScriptableObjects, Kill / Collect / Talk / ReachLocation objectives, a manager
for Accept / Track / Complete / Abandon, chain quests, prerequisites, rewards,
events for UI, and serializable save data. No paid dependencies. URP-compatible
(it ships no rendering code at all).

> **Honest status:** this code was written without Unity in the environment.
> It has not been opened, compiled, or played in Unity. Do not ship it or call
> it production-ready until the TEST checklist below has been run in
> Unity 2022.3 LTS and every step passes.

## What it is

- A quest data model you edit in the Inspector (`QuestDefinition` assets).
- A runtime manager that owns quest state and progress routing.
- Events (C# events and UnityEvents) your UI, dialogue, NPC and inventory
  systems subscribe to — the framework draws no UI itself.
- Save/load snapshots (`QuestSaveData`) your save system persists.
- Editor helpers: a custom inspector with validation, and menu items to
  create quest definitions and a demo chain.

## What it is NOT

- Not a UI system: there are no quest-log or tracker prefabs. `QuestTracker`
  exposes the tracked quest and a formatted text string; your UI renders it.
- Not a dialogue, combat, inventory or save system: it integrates with those
  (see Integration notes) but does not implement them.
- Not a reward granter: rewards are data on the definition. Your game grants
  coins / XP / items when `OnQuestCompleted` fires.
- Not tested, not verified, not production-ready (see the header).

## Setup — under 5 minutes

1. Copy this package into your project (`Packages/com.sdx17.quest/`, or
   `Assets/Sdx17Quest/`), or add it via Package Manager &gt; Add package from disk
   using `package.json`.
2. Create quest definitions: Project window &gt; right-click &gt;
   Create &gt; Sdx17 &gt; Quest &gt; Quest Definition — or use the menu
   **Sdx17 / Quest / Create Demo Quest Chain** for a ready-made 3-quest chain.
3. Fill in each definition: Id (e.g. `quest_slime_hunt`), Title, Description,
   objectives (type, target id, required amount), rewards, `NextQuestId` for a
   chain, prerequisite quest ids.
4. Add a `QuestManager` component to a GameObject in your first scene
   (a persistent bootstrap object works best) and drag your definitions into
   its Known Definitions list.
5. Add a `QuestTracker` component to your tracker UI GameObject and assign the
   same `QuestManager`.
6. Press Play and accept a quest from code, an NPC, or the demo script
   (see `Samples~/Demo/README.md`).

## Files

| Path | What it is |
| --- | --- |
| `package.json` | UPM package manifest (`com.sdx17.quest`, Unity 2022.3) |
| `Runtime/QuestObjectiveType.cs` | `QuestObjectiveType` enum, `QuestObjectiveData`, `QuestReward`, `QuestItemReward` |
| `Runtime/QuestDefinition.cs` | `QuestDefinition` ScriptableObject (id, title, objectives, rewards, chain, prerequisites) |
| `Runtime/QuestInstance.cs` | `QuestInstance` runtime state, `QuestState`, `QuestSaveData` |
| `Runtime/QuestManager.cs` | `QuestManager` — accept / track / complete / abandon, progress, chains, save/load, events |
| `Runtime/QuestTracker.cs` | `QuestTracker` UI hook — tracked quest, formatted progress text |
| `Runtime/Sdx17.Quest.Runtime.asmdef` | Runtime assembly definition |
| `Editor/QuestDefinitionEditor.cs` | Custom inspector for `QuestDefinition` with validation |
| `Editor/QuestTools.cs` | `Sdx17 / Quest` menu: create definition, create demo chain |
| `Editor/Sdx17.Quest.Editor.asmdef` | Editor assembly definition (references Runtime) |
| `Samples~/Demo/QuestDemo.cs` | Demo driver script (accept, simulate kill/collect/talk, chain, save log) |
| `Samples~/Demo/README.md` | Demo scene setup steps (no scene file is included) |
| `Tests~/QuestSelfTest.cs` | Play-mode self-test MonoBehaviour logging PASS/FAIL |
| `CHANGELOG.md` | Version history |
| `LICENSE` | MIT, Copyright 2026 Sdx17 |

## Config options

`QuestDefinition` (per quest asset, in the Inspector):

- **Quest Id** — stable unique id, e.g. `quest_slime_hunt`. Used for save data,
  chains and prerequisites. Falls back to the asset name when empty.
- **Title / Description** — player-facing text for the quest log.
- **Objectives** — list of `QuestObjectiveData`: Type (`Kill`, `Collect`,
  `Talk`, `ReachLocation`), Target Id (enemy / item / NPC / location id your
  game reports), Description (e.g. "Defeat 3 slimes"), Required Amount (min 1).
- **Rewards** — Coins, Experience, Items (item id + amount). Data only; the
  game grants them on completion.
- **Next Quest Id** — id of the follow-up quest, or empty for the end of a chain.
- **Prerequisite Quest Ids** — quests that must be completed before this one
  can be accepted.
- **Repeatable** — when true, a completed or abandoned quest can be accepted again.

`QuestManager` (component, in the Inspector):

- **Known Definitions** — the quest definitions this manager knows about.
  `AcceptById`, chains and load all resolve against this list.
- **Persist Across Scenes** (default true) — `DontDestroyOnLoad` for the manager.
- **Auto Track On Accept** (default true) — accepting a quest also tracks it.

## Public API

```csharp
using Sdx17.Quest;

// Accept (returns null when it cannot be accepted; reason explains why, e.g. prerequisites)
QuestInstance quest = manager.Accept(definition, out string reason);
QuestInstance byId = manager.AcceptById("quest_slime_hunt");

// Track / untrack for the UI
manager.Track(quest);
manager.TrackById("quest_slime_hunt");
manager.Track(null); // clear tracking

// Report gameplay progress — the manager routes it to matching active quests
// and completes any quest whose objectives all finish.
manager.ReportKill("slime", 1);
manager.ReportCollect("herb", 3);
manager.ReportTalk("elder_mara");
manager.ReportReachLocation("meadow_gate");

// Complete / abandon
QuestReward rewards = manager.Complete(quest); // null when objectives are unfinished
bool abandoned = manager.Abandon(quest);

// Chains
QuestInstance next = manager.TryAcceptNextInChain(finishedQuest);

// Queries
bool active = manager.IsQuestActive("quest_slime_hunt");
bool done = manager.IsQuestCompleted("quest_slime_hunt");
List<QuestInstance> activeQuests = manager.GetActiveQuests();

// Events — subscribe in code, or wire the UnityEvents in the Inspector
manager.QuestAccepted += q => Debug.Log($"Accepted {q.Definition.Title}");
manager.QuestProgressChanged += q => Debug.Log(q.GetSummary());
manager.QuestCompleted += q => GrantRewards(q.Definition.Rewards);
manager.QuestAbandoned += q => Debug.Log($"Abandoned {q.Definition.Title}");
manager.TrackedQuestChanged += q => RefreshTrackerUi();

// Save / load — persist the list however your game saves (JSON, PlayerPrefs, Secure Save)
List<QuestSaveData> saved = manager.SaveAll();
manager.LoadAll(saved);

// Tracker UI hook
QuestTracker tracker = GetComponent<QuestTracker>();
string text = tracker.GetTrackedQuestText(); // "Slime Hunt\nDefeat 3 slimes 2/3"
float fraction = tracker.GetTrackedQuestFraction(); // 0..1
```

## Integration notes with other Sdx17 systems

- **NPC quest-giver:** the NPC checks `manager.CanAccept(definition, out reason)`
  and shows the reason in dialogue ("Complete 'Gather Herbs' first"), then calls
  `manager.Accept(definition, out _)` on the player's "Accept" choice.
  When a quest completes, call `manager.TryAcceptNextInChain(finished)` to offer
  the follow-up from the same NPC.
- **Dialogue:** a Talk objective completes when dialogue ends — call
  `manager.ReportTalk(npcId)` from the dialogue system's "conversation ended"
  callback, using the same NPC id the objective's Target Id holds.
- **Inventory rewards:** subscribe to `QuestCompleted` (or the Inspector
  `OnQuestCompleted` UnityEvent) and grant `instance.Definition.Rewards`
  through the inventory system: coins to the wallet, XP to the player,
  each `QuestItemReward` (item id + amount) to the inventory. The framework
  never touches the inventory itself.
- **Secure Save:** on save, store `manager.SaveAll()` (a
  `List<QuestSaveData>`, JSON-serializable) alongside the rest of the save;
  on load, after the manager's Known Definitions are assigned, call
  `manager.LoadAll(savedList)`. Unknown quest ids in a save are skipped with
  a warning, so removing a quest definition does not corrupt a save file.
- **UI tracker:** `QuestTracker` fires `OnTrackedQuestChanged`,
  `OnTrackedQuestProgressChanged` and `OnTrackedQuestCompleted`; bind those to
  your tracker panel, or poll `GetTrackedQuestText()` / `GetTrackedQuestFraction()`.
  A quest log can list `manager.GetActiveQuests()` and `manager.GetCompletedQuests()`.

## TEST checklist for Unity (run in Unity 2022.3 LTS)

1. Open the package in a Unity 2022.3 LTS project (URP template is fine).
   Confirm the Console shows no compile errors.
2. Create the demo chain via **Sdx17 / Quest / Create Demo Quest Chain**.
   Confirm three `.asset` files appear and the custom inspector shows no errors.
3. Build the demo scene exactly as `Samples~/Demo/README.md` describes.
   Press Play: the first quest ("Gather Herbs") is accepted and logged.
4. Use the demo context-menu items (or buttons): Collect Herb x3 — the first
   quest completes and the second ("Slime Hunt") is offered via the chain step.
   Defeat Slime x2, then Talk to Elder Mara — all three quests complete in order.
5. Abandon the tracked quest from the demo menu: tracking clears, the quest
   state becomes Abandoned, and re-accepting it is refused (not repeatable).
6. Attach `Tests~/QuestSelfTest` to a GameObject, press Play, and confirm the
   Console ends with "0 failed" (every check logs PASS).
7. Save/load: call `SaveAll()`, note the JSON, restart Play mode, call
   `LoadAll()` with it, and confirm the completed quests and progress come back.
8. Check the tracker: `QuestTracker.GetTrackedQuestText()` shows the tracked
   quest with per-objective progress, and updates as progress is reported.

If any step fails, record the exact Unity version, the Console error, and the
step number — do not work around it silently.

## License

MIT — see `LICENSE`. Copyright 2026 Sdx17.
