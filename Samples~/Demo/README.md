# Demo — Sdx17 Quest Framework

**Version 0.1.0 — UNTESTED IN UNITY.** No `.unity` scene file is included in
this package. Build the demo scene yourself with the steps below (about 5 minutes).

## What the demo does

A three-quest chain, created by **Sdx17 / Quest / Create Demo Quest Chain**:

1. `quest_demo_collect` — "Gather Herbs": Collect 3 `herb`.
2. `quest_demo_hunt` — "Slime Hunt": Defeat 2 `slime`. Prerequisite: quest 1.
3. `quest_demo_report` — "Report Back": Talk to `elder_mara`. Prerequisite: quest 2.

`QuestDemo` accepts the first quest on Play, logs every quest event to the
Console, and offers context-menu actions to simulate collecting, killing and
talking — so the whole chain can be played through without any gameplay code.

## Scene setup

1. Create a new scene (File &gt; New Scene, Basic 3D or empty) and save it
   as `QuestDemoScene` in your project.
2. Create an empty GameObject named `QuestManager`, add the `QuestManager`
   component, and drag the three demo quest assets into its Known Definitions list.
3. Create an empty GameObject named `QuestDemo`, add the `QuestDemo` component,
   assign the `QuestManager` GameObject to its Quest Manager field, and assign
   the `quest_demo_collect` asset to its First Quest field.
4. (Optional, tracker UI) Create an empty GameObject named `QuestTracker`,
   add the `QuestTracker` component, assign the same `QuestManager`, and
   (optional) add a UI Text element whose text you set from
   `tracker.GetTrackedQuestText()` — see the README's Public API section.
5. (Optional, self-test) Create an empty GameObject named `QuestSelfTest`,
   add the `Tests~/QuestSelfTest` component, and press Play to see PASS/FAIL
   lines in the Console. Remove this GameObject from the demo scene afterwards
   if you keep the scene as a showcase — the self-test creates its own manager
   when none exists, so it is safe to run in the same scene.

## Playing the demo

1. Press Play. The Console logs: `Accepted: Gather Herbs: 0/1 objectives done`.
2. Right-click the `QuestDemo` component header and choose
   **Demo: Collect 1 Herb** three times. The first quest completes.
3. Right-click the component header and choose
   **Demo: Accept Next Quest In Chain**. "Slime Hunt" is accepted (its
   prerequisite is now complete).
4. Choose **Demo: Defeat 1 Slime** twice. The second quest completes.
5. Choose **Demo: Accept Next Quest In Chain**, then
   **Demo: Talk to Elder Mara**. The third quest completes.
6. Try **Demo: Abandon Tracked Quest** on an active quest, and
   **Demo: Log Save Data** to see what a save file would hold for each quest.

## Wiring the demo actions to UI buttons (optional)

Each `QuestDemo` public method (`CollectHerb`, `DefeatSlime`,
`TalkToElderMara`, `AcceptNextInChain`, `AbandonTrackedQuest`, `LogSaveData`)
is a plain public method, so any UI Button's On Click event can call it.
No extra code is needed.
