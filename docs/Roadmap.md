# Roadmap

Follow-ups from the program-organization review. A (one shared exercise history used by every lookup) and D (Pause/End for PPL programs) are already in place.

## B. Stable exercise identity on logged sets

**Problem:** History, suggestions, and e1RM still match logged work by exercise **name**. `ExerciseObservations.MatchName` adds fuzzy matching, but renaming an exercise or using a close variant can still split or merge history incorrectly.

**Plan:**
- Add a nullable `ExerciseDefinitionId` to `PplSessionExercise`, `AdditionalStrengthExercise`, and `Lift`. `Accessory` already has it.
- Fill it in when sessions are generated or swapped, from the slot's or accessory's definition.
- Add a migration that backfills existing rows by exact normalized name (`ExerciseHistoryService.BuildKey`) against the user's visible definitions, leaving unmatched rows null. Name snapshots stay unchanged so history is never rewritten.
- In `ExerciseObservations`, group by definition id when it is set and fall back to the name otherwise.
- Tests: the backfill links only exact matches, a renamed custom exercise keeps its history, and cross-user definitions are never linked.

## C. Guided 5/3/1 ↔ PPL switch flow

**Problem:** Switching programs is manual. `CycleService.CreateNextCycleAsync` adds +5/+10 to the previous training max and ignores newer PPL work. Only the first cycle is seeded from e1RM. The Workout button (`WorkoutRedirect`) only opens 5/3/1.

**Plan:**
- Add a "Switch program" action on the dashboard and history page:
  - Choosing PPL → 5/3/1 ends or pauses the active PPL program. A new-cycle form shows, for each lift, the suggested TM (90% of the most recent e1RM from `EstimatedStrengthService`) next to the previous cycle's TM plus the increment. The user picks one or edits it.
  - Choosing 5/3/1 → PPL marks the current cycle complete and creates a PPL program seeded from recent e1RM, which already works.
- Make `WorkoutRedirect` open whichever program is active, or offer a choice when both are.
- Tests: suggested TMs use PPL work newer than the last cycle, explicit user choices are kept, and the flow is user-scoped.
