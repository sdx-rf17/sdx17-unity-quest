# Sdx17 Quest Framework — Asset Store Listing (DRAFT)

**Status: DRAFT ONLY. No Asset Store listing exists yet. Do not submit
this text until every item in `SELLING_REQUIREMENTS.md` has passed.
Version 0.1.0 is UNTESTED IN UNITY — this listing must not be
published before the package has been verified in Unity 2022.3 LTS
and the listing text updated to say so honestly.**

---

## Title

Sdx17 Quest Framework

## Short description (under 200 characters)

Data-driven quests for Unity: ScriptableObject quests, Kill / Collect / Talk / Reach objectives, chains, prerequisites, rewards and save data. No paid dependencies.

*(Count: 164 characters, including spaces.)*

## Long description

**Sdx17 Quest Framework** is a lightweight, data-driven quest system
for Unity 2022.3 LTS. Design quests in the Inspector — no code needed
for the quests themselves — and drive them from your NPCs, combat,
pickups, dialogue and triggers with four simple reporting calls.

**What you get**

- **QuestDefinition ScriptableObjects** — id, title, description,
  objectives, rewards, chain links and prerequisites, all editable in
  the Inspector with a custom editor that warns you about invalid
  quests.
- **Four objective types** — Kill, Collect, Talk and ReachLocation —
  each with a target id and a required amount. Your game reports
  progress with `ReportKill`, `ReportCollect`, `ReportTalk` and
  `ReportReachLocation`.
- **QuestManager** — accept, track, complete and abandon, with
  prerequisite checks that return a human-readable reason
  ("Complete 'Gather Herbs' first.") you can show in dialogue.
- **Chain quests** — a quest names its follow-up with `NextQuestId`;
  `TryAcceptNextInChain` offers it when the previous quest completes.
- **Repeatable quests** — a flag on the definition lets a quest be
  accepted again after completion or abandonment.
- **Rewards as data** — coins, experience and items on the
  definition. The framework fires `OnQuestCompleted` and your game
  grants the reward through its own inventory system, so the package
  stays free of any currency or inventory implementation.
- **Events for your UI** — C# events and UnityEvents for accepted,
  progress changed, completed, abandoned and tracked-quest changed.
  `QuestTracker` gives your UI the tracked quest, a formatted
  progress string and a 0–1 progress fraction.
- **Save / load** — `SaveAll()` returns a serializable
  `List<QuestSaveData>` your save system persists; `LoadAll()`
  restores it, skipping unknown quest ids with a warning instead of
  corrupting the save.
- **Demo included** — a three-quest chain (Gather Herbs → Slime Hunt
  → Report Back) with a demo driver script, context-menu actions to
  play the whole chain without gameplay code, and a Play-mode
  self-test that logs PASS/FAIL for the core paths.

**What it is NOT**

- Not a UI system: there are no quest-log or tracker prefabs.
  `QuestTracker` exposes the data; your UI renders it.
- Not a dialogue, combat, inventory or save system: it integrates
  with those, but does not implement them.
- Not a reward granter: rewards are data on the definition. Your
  game grants coins / XP / items when `OnQuestCompleted` fires.
- Not networked: single-player by design. Multiplayer needs a
  server-authoritative wrapper you write yourself.

**Honest status**

Version 0.1.0 was written and reviewed without Unity in the
environment and has **not yet been opened, compiled, or played in
Unity**. The package will only be listed once the TEST checklist in
the README has been run in Unity 2022.3 LTS and every step passes.
This listing will be updated at that point with the verified result.
If you buy version 0.1.0, you are buying the code as described above
— please run the demo and the self-test first, and report any issue
through the support contact below.

**Requirements**

- Unity 2022.3 LTS.
- Works with URP and Built-in (the package ships no rendering code).
- No paid dependencies. No third-party code, fonts, audio or assets
  included — all code is original by Sdx17, MIT licensed
  (see `ThirdPartyNotices.txt`).

**Support**

Sdx17 — GitHub: `sdx-rf17` (github.com/sdx-rf17). No email support
address is offered: please open an issue on the repository and
include your Unity version, the exact Console error, and the steps
to reproduce.

## Keywords (10)

1. quest
2. quests
3. quest system
4. rpg
5. objectives
6. scriptableobject
7. quest log
8. chain quest
9. rpg quests
10. mission system

## Category

Tools / Game Toolkits **(suggested — re-check at listing time)**.
Unity's category list changes; confirm the closest category when
submitting, and do not force the package into a category it does not
fit.

## Suggested price

**$25**

### Why $25 — justification

- **A new publisher should price $15–$35 to get first reviews, NOT
  $95 on day one.** A $95 price from a publisher with zero reviews
  and zero sales asks buyers to take all the risk, and it will
  suppress the first sales that produce the reviews a listing needs.
  $25 sits inside the $15–$35 first-listing band: low enough for a
  buyer to try a new publisher, high enough to signal that this is a
  real system and not a throwaway snippet.
- **Comparable quest systems on the Asset Store are typically
  $30–$95** for full-featured packages with UI prefabs, editors and
  demo scenes. This package is deliberately smaller — no UI prefabs,
  no dialogue/inventory/save implementation, no built-in demo scene
  file — so it should price **below** those comparables, not beside
  them. $25 reflects that scope honestly.
- **Revenue math (before tax and refunds):** Unity's standard
  publisher share is 70% net, so a $25 sale is **$17.50** to the
  publisher before tax and refunds. Price the package on that net
  figure, not on the $25 headline.
- **This price guarantees nothing.** $25 does not guarantee sales,
  reviews, or any income. Sales depend on the listing quality, the
  screenshots and video, the demo working first time, and buyers
  trusting a new publisher. Re-check the price against live
  comparables immediately before listing — if comparable quest
  frameworks have moved, adjust inside the $15–$35 band and say why
  in the version notes.

## What's included

- `Runtime/` — `QuestDefinition.cs`, `QuestInstance.cs`,
  `QuestManager.cs`, `QuestObjectiveType.cs`, `QuestTracker.cs`
  (namespace `Sdx17.Quest`), plus the runtime assembly definition
  `Sdx17.Quest.Runtime.asmdef`.
- `Editor/` — `QuestDefinitionEditor.cs` (custom inspector with
  validation) and `QuestTools.cs` (menu items **Sdx17 / Quest /
  Create Quest Definition** and **Sdx17 / Quest / Create Demo Quest
  Chain**), plus the editor assembly definition
  `Sdx17.Quest.Editor.asmdef`.
- `Samples~/Demo/` — `QuestDemo.cs` demo driver script and
  `README.md` with the demo scene setup steps (no `.unity` scene
  file is included — the buyer builds the scene in about 5 minutes).
- `Tests~/QuestSelfTest.cs` — Play-mode self-test MonoBehaviour
  logging PASS/FAIL for accept, progress, complete, abandon, chain,
  prerequisites and save/load.
- `Documentation~/GUIDE.md` — full guide: quickstart, concepts,
  API reference, config reference, integration notes, FAQ,
  performance notes and limitations.
- `README.md`, `CHANGELOG.md`, `LICENSE` (MIT), `package.json`
  (`com.sdx17.quest`, Unity 2022.3).

## Requirements / Dependencies

- **Unity 2022.3 LTS** (package declares `unity: 2022.3`,
  `unityRelease: 0f1`).
- **Render pipeline:** URP and Built-in compatible — the package
  ships no rendering code at all.
- **No paid dependencies. No bundled third-party code, fonts, audio
  or assets.** All code is original by Sdx17, MIT licensed
  (see `ThirdPartyNotices.txt`).
- **Not included:** a `.unity` demo scene file, UI prefabs, dialogue
  / combat / inventory / save implementations, networking. See
  "What it is NOT" above.

## Demo scene description

The included demo is a three-quest chain built with the menu item
**Sdx17 / Quest / Create Demo Quest Chain** and the `QuestDemo`
driver script:

1. **Gather Herbs** (`quest_demo_collect`) — Collect 3 `herb`.
2. **Slime Hunt** (`quest_demo_hunt`) — Defeat 2 `slime`.
   Prerequisite: quest 1.
3. **Report Back** (`quest_demo_report`) — Talk to `elder_mara`.
   Prerequisite: quest 2.

Build the scene by following `Samples~/Demo/README.md` (about
5 minutes: a `QuestManager` GameObject with the three definitions, a
`QuestDemo` GameObject, optionally a `QuestTracker` GameObject).
Press Play: the first quest is accepted and logged. The demo's
context-menu actions (**Demo: Collect 1 Herb**, **Demo: Defeat 1
Slime**, **Demo: Talk to Elder Mara**, **Demo: Accept Next Quest In
Chain**, **Demo: Abandon Tracked Quest**, **Demo: Log Save Data**)
play the whole chain and show the save snapshot — no gameplay code
needed. Every quest event is logged to the Console, so the flow is
visible without any UI at all.

## Support contact

- **Sdx17 / GitHub: sdx-rf17** — github.com/sdx-rf17
- No fake email address is given. For support, open an issue on the
  repository with your Unity version, the exact Console error, and
  the steps to reproduce.

## Version history

- **0.1.0** (2026-11-21) — Initial release. **UNTESTED IN UNITY.**
  `QuestDefinition` ScriptableObjects; Kill / Collect / Talk /
  ReachLocation objectives; `QuestManager` (accept / track /
  complete / abandon, prerequisites, chains, save/load, C# events
  and UnityEvents); `QuestTracker` UI hook; `QuestInstance` runtime
  state and `QuestSaveData`; editor inspector with validation and
  menu items; demo chain and demo driver; Play-mode self-test.
  Known limitations: not yet opened, compiled or played in Unity;
  no UI prefabs; no dialogue / inventory / save implementation;
  rewards are data only; save/load stores quest state only.

---

*End of draft. Nothing in this file is a published listing, and no
sales or reviews are claimed anywhere in it.*
