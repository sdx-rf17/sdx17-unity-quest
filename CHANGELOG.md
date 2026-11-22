# Changelog

All notable changes to the Sdx17 Quest Framework are documented here.
The format follows Keep a Changelog, and versioning follows Semantic Versioning.

## [0.1.0] - 2026-11-21

### Added

- Initial release, v0.1.0, **UNTESTED IN UNITY** — no Unity run has verified this code yet.
- `QuestDefinition` ScriptableObject: id, title, description, objectives list,
  rewards (coins / experience / items), chain `nextQuestId`, prerequisite quest ids.
- Objective types `Kill`, `Collect`, `Talk`, `ReachLocation`, each with a target id,
  description and required amount, with per-objective progress tracking.
- `QuestManager`: `Accept`, `Track` / `Untrack`, `Complete`, `Abandon`, progress
  reporting (`ReportKill`, `ReportCollect`, `ReportTalk`, `ReportReachLocation`),
  prerequisite and chain checks, UnityEvent and C# events, save/load via
  serializable `QuestSaveData`.
- `QuestTracker`: UI hook MonoBehaviour that subscribes to `QuestManager` events,
  exposes the tracked quest and a formatted progress string for any UI system.
- `QuestInstance`: runtime quest state, objective progress, completion checks,
  save/load conversion.
- Editor: custom inspector for `QuestDefinition` with validation warnings, plus
  `Sdx17 / Quest` menu items to create a quest definition and a demo quest chain.
- `Samples~/Demo`: `QuestDemo` script and setup README for a minimal demo scene.
- `Tests~`: `QuestSelfTest` MonoBehaviour that logs PASS/FAIL for the core paths
  in Play mode (accept, progress, complete, abandon, chain, prerequisites, save/load).
- Assembly definitions for Runtime (`Sdx17.Quest.Runtime`) and Editor
  (`Sdx17.Quest.Editor`, references Runtime).
- `package.json` (`com.sdx17.quest`, Unity 2022.3), MIT license, Unity `.gitignore`.

### Known limitations

- Not yet opened, compiled or played in Unity — treat every claim as unverified
  until the TEST checklist in `README.md` has been run in Unity 2022.3 LTS.
- No built-in UI prefabs, no dialogue or inventory implementation — those systems
  are integration points only (see `README.md`).
- Rewards are data only: granting coins, XP or items is the consuming game's job,
  via the `OnQuestCompleted` event or the `QuestReward` returned by `Complete`.
- Save/load stores quest state only; the game must persist the returned
  `List<QuestSaveData>` itself (PlayerPrefs, file, or the Sdx17 Secure Save system).
