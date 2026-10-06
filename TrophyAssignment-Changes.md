# Trophy League Assignment — Change Summary

**Date:** 2026-06-11
**Area:** RRCServices league scoring / trophy assignment
**Affected apps:** `PublicApp` (public League page), `rrcapi` (league endpoint), `RRCApp` (admin League Summary)

---

## 1. The problem

Runners were being placed in the wrong trophy league. Two concrete examples in the
2026 season:

- **Lauren Gordon** was showing in the **JR** league (2nd) when she should be **DB** 1st.
- **Paul McDermott** was showing in **JR** 9th, inflated by runners who did not belong
  in the JR table.

Two root causes were found:

1. **Assignment compared raw points.** A runner's JR points are almost always higher
   than their DB points simply because JR counts the best **8** races and DB only **6**.
   Comparing raw points therefore had a built-in bias toward JR, regardless of where a
   runner actually *ranked*.
2. **The JR eligibility rule did not exist in code.** The club rule "you only qualify
   for the JR trophy if you have run a race longer than 10k" was never enforced, so
   short-distance-only runners were appearing in the JR table.

---

## 2. The agreed rules (end of season)

These are the rules the corrected code implements.

### Scoring (unchanged — already correct)
- A race counts if the runner is active, the time entry is active, there is a real
  finish time, the date is in season, and there is either a valid target **or** the
  race is in 2026 (the 2026 no-target allowance).
- **Beat or equalled** target → **10 points**; **missed** → **6 points**;
  **2026 race with no target** → flat **6 points**.
- Tie-break "trophy time" = seconds beaten by, capped at 120 per race.

### Trophies
| Trophy | Distances counted | Best races scored |
|--------|-------------------|-------------------|
| **DB** | Short only: 1m, 5km, 5m, 10km (all ≤ 10k) | top **6** |
| **JR** | All distances | top **8** |

### Eligibility (rule D — newly enforced)
- A runner may only be placed in **JR** if they have run **at least one race longer
  than 10k** (10m, half, 20m, marathon).
- A runner may only be placed in **DB** if they have at least one qualifying
  short-distance race.

### Ranking within a trophy (rule E)
Points (high → low), then trophy-time (high → low), then name (A → Z).

### Assignment (rule F — the fix)
Each runner appears in exactly **one** league — the one giving their **best final
*settled* position**. An **equal position goes to JR**.

> **"Settled" matters.** A runner's position depends on who else is in that table,
> which depends on everyone's assignment. Moving a strong DB runner into DB pushes
> weaker DB runners down a place, which can change where *they* belong. So the
> assignment is iterated until it is stable (no runner would improve by switching),
> rather than decided in a single pass.

---

## 3. What changed in the code

### New file — single source of truth
**`RRCServices/League/Trophy/TrophyAssignmentResolver.cs`**
- `TrophyCandidate` — a plain data carrier holding a runner's JR/DB points, tie-break
  times, and a `JrEligible` flag.
- `TrophyAssignmentResolver.Resolve(...)` — pure, database-free logic that applies
  rules D, E and F and returns each runner's assigned trophy. It seeds an assignment,
  then iterates best-responses to a stable result. The outcome is independent of input
  order. This is now the **only** place the assignment rule lives.

### Rewired to use the resolver
**`RRCServices/League/LeagueTableService.cs`** *(public tables — used by `PublicApp` and `rrcapi`)*
- Added the JR-qualifying distance set `{10m, 13m, 20m, 26m}` and an
  `IsJrQualifyingDistance` helper.
- Captures each runner's JR eligibility while scoring.
- Replaced the old raw-points "Step 5" assignment block with a single call to
  `TrophyAssignmentResolver.Resolve(...)`.

**`RRCServices/League/LeagueSummaryService.cs`** *(admin League Summary screen)*
- Same JR-qualifying distance set + helper.
- Scores everyone, then calls the shared resolver once instead of the old per-runner
  `DetermineAssignment` (which was removed).

### Documentation only
**`RRCServices/League/DTO/LeagueSummaryDto.cs`** — updated the `AssignedTrophy` comment
to describe the new rule.

### Tests
**`RRCServiceTest/TrophyAssignmentResolverTests.cs`** — 8 unit tests, no database:
- Not-JR-eligible runner forced into DB even when JR points are higher.
- Long-races-only runner → JR; scored-nowhere runner omitted.
- Eligible-for-both → better position wins; equal position → JR.
- The cascade/settle case (a strong runner moving into DB pushes another to their
  true trophy).
- Result is independent of input order.

> The JR-qualifying distances are listed explicitly in code. If a new long-distance
> code is ever added to the `distance` table, it must also be added to the
> `JrQualifyingDistanceCodes` set in both services.

---

## 4. Effect on the league (2026 season)

| Runner | Before | After |
|--------|--------|-------|
| **Lauren Gordon** | JR 2nd | **DB 1st** |
| **Paul McDermott** | JR 9th | **JR 7th** |
| JR-ineligible runners (no >10k race) | shown in JR | **removed from JR** |

### Note on Paul McDermott
After the fix Paul is **7th in JR and also 7th in DB**, so DB would *not* give him a
higher position. He appears 5th in DB only in the *old* table, because Lauren and
Danielle Rigg were still sitting in JR there. Once they correctly move into DB they
rank above Paul, leaving him 7th in DB — equal to his JR 7th — so the tie-break places
him in JR (up from 9th). There is no assignment that places both Lauren 1st *and* Paul
5th in DB; those expectations are in direct conflict.

---

## 5. Verification

- **Full solution builds** (`RRCApp.sln`) with no errors.
- **8/8** new resolver unit tests pass.
- Results above were reproduced against the live database for the 2026 season window.

---

## 6. To do / notes

- **Redeploy** `PublicApp` and `rrcapi` to see the corrected tables on the published site.
- **Pre-existing (not caused by this change):** 14 tests in `ScoreRunnerTests` fail under
  a 2026 system clock — their test helper omits `HasTarget`, so the 2026 no-target rule
  awards a flat 6 points instead of running the calculator. Left as-is for now.
- **Security:** the Azure SQL connection string (with password) is committed in
  `RRCApp/appsettings.json`. Recommend rotating it and moving it to a secret store.
- The currently-unused admin assignment method `DetermineAssignment` was removed; the
  shared resolver is now the only assignment logic.
