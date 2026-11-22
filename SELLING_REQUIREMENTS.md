# Selling requirements — Sdx17 Quest Framework v0.1.0

**Do not submit to the Unity Asset Store until every box below is
checked. This is a pre-submission checklist, not a submission.
Version 0.1.0 is UNTESTED IN UNITY: the single most important gate
is the clean-project Unity test in item 1. Nothing here guarantees
sales, reviews, or income, and the suggested $25 price in
`LISTING.md` guarantees nothing either.**

Tick each item only when you have actually done it and can point to
the evidence (screenshot, log, file). Do not tick from memory.

## 1. Unity verification (the hard gate)

- [ ] Open the package in a **clean Unity 2022.3 LTS project**
      (new project, URP template is fine, no other packages).
- [ ] Console shows **zero errors and zero warnings** on import and
      on entering Play mode.
- [ ] Run the full **TEST checklist in `README.md`** (8 steps:
      demo chain creation, demo scene play-through, abandon /
      re-accept refusal, `QuestSelfTest` ending with "0 failed",
      save/load round-trip, tracker text and fraction) and record
      the exact Unity version and the result of every step.
- [ ] Fix every failure found, re-run the whole checklist from a
      clean project, and update `README.md`, `Documentation~/GUIDE.md`
      and `LISTING.md` to remove the "UNTESTED IN UNITY" status only
      when the checklist genuinely passes. Do not soften the wording
      before then.

## 2. Demo scene

- [ ] Build the demo scene exactly as `Samples~/Demo/README.md`
      describes, in the clean project from item 1.
- [ ] Press Play and play the whole three-quest chain through the
      demo context-menu actions (Collect Herb x3 → Accept Next →
      Defeat Slime x2 → Accept Next → Talk to Elder Mara).
- [ ] Confirm the Console logs match the demo README at every step,
      including the completion and save-data logs.

## 3. Docs proofread

- [ ] Proofread `README.md`, `Documentation~/GUIDE.md`,
      `LISTING.md`, `CHANGELOG.md` and `package.json` end to end.
- [ ] Every class and method name in the guide matches the real
      code in `Runtime/` (spot-check at least the `QuestManager`
      method list against `Runtime/QuestManager.cs`).
- [ ] No claim in any doc says the package is tested, verified,
      production-ready, or proven in a real game unless item 1 has
      passed — and if it has, the wording says exactly what was
      tested, on which Unity version.
- [ ] No sales, reviews, downloads, or Asset Store listing are
      claimed anywhere. (There are none yet.)
- [ ] The price justification in `LISTING.md` still matches reality
      (see item 8).

## 4. Screenshots

- [ ] At least **5 screenshots**, all **real Unity screenshots
      only** — actual captures from the Unity Editor / Play mode
      running this package. No concept art, no mockups, no AI
      images presented as screenshots.
- [ ] Every screenshot is **1920x1080**.
- [ ] The set shows, at minimum: the demo quest chain assets in
      the Project window / Inspector, the `QuestManager` component
      with Known Definitions, the demo playing with Console logs,
      the tracker UI text, and the custom inspector validation.
- [ ] No screenshot shows stand-in numbers or data that is not
      really in the demo (check the tracker text matches the real
      demo values: 3 herbs, 2 slimes, Elder Mara).

## 5. Video

- [ ] One video, **30–60 seconds**, **real Unity recording only**
      — a screen recording of the actual demo scene playing in
      Unity (accept → collect → complete → chain → talk). No AI
      video, no slideshow of still images, no footage of a different
      game or project.
- [ ] The video shows the package working, not just the Editor
      sitting still. Include the Console logs visible at least once.

## 6. Icon and store assets

- [ ] Package icon **512x512** (PNG), original artwork for this
      package. Not an AI image passed off as a screenshot, and not
      copied from another asset.
- [ ] If **any** concept art is used anywhere in the listing
      (icon, screenshots, video thumbnail), the listing carries an
      **AI-content disclosure** stating which images are AI-generated
      or concept art and that they are not screenshots of the
      package running. If no AI content is used, no disclosure is
      needed — but do not claim AI art is a screenshot either way.

## 7. Publisher account

- [ ] Unity publisher account created and verified.
- [ ] **Two-factor authentication (2FA) enabled** on the publisher
      account.
- [ ] **Tax information** completed in the publisher portal.
- [ ] **PayPal payout** set up (Unity pays publishers via PayPal).
- [ ] Publisher display name and profile are the real ones
      (Sdx17) — no fake identity, no borrowed account.

## 8. Price and comparables (re-check at listing time)

- [ ] Re-check the price against live Asset Store comparables on
      the day of listing: search quest / quest-system packages,
      note their prices, feature scope (UI prefabs? demo scene?
      editor?) and review counts.
- [ ] Confirm the suggested **$25** still sits in the
      **$15–$35 band for a new publisher's first listing** and below
      fuller-featured comparables ($30–$95). Adjust inside the band
      if comparables have moved, and update the justification in
      `LISTING.md` to match.
- [ ] Remember the net math: 70% publisher share means a $25 sale
      is **$17.50 before tax and refunds**. Do not budget on the
      $25 headline.
- [ ] Do **not** list at $95 on day one as a new publisher with
      zero reviews.

## 9. Submission rules and timing

- [ ] Category re-checked at listing time: **Tools / Game Toolkits
      (suggested)** — confirm it is still the closest category when
      submitting.
- [ ] Review takes **at least 5 business days** — plan for that;
      do not promise buyers or yourself a launch date inside the
      review window.
- [ ] **5 submissions per month cap in the first 6 months** —
      do not waste submissions on a package that has not passed
      items 1–8. A rejected submission still counts.
- [ ] The submitted package contains only the files listed in
      `LISTING.md` "What's included" — no leftover test scenes,
      personal files, or credentials.

## 10. Final honest pass (read aloud before you click submit)

- [ ] The listing says the package is v0.1.0 and says plainly
      whether it has been tested in Unity — with the true answer.
- [ ] Nothing in the listing claims sales, reviews, users, or a
      track record that does not exist.
- [ ] The support contact is the real one: Sdx17 / GitHub `sdx-rf17`
      — no fake email address.
- [ ] You have read `LISTING.md` once more as a buyer would, and
      you would still buy it at $25 knowing what it does and does
      not do.
