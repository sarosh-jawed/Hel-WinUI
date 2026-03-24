# Hel Test Cases

This document describes the synthetic CSV fixtures used to validate Hel behavior after Phase 8.

## Fixture files

Location in repo:

- `Hel/Hel.Tests/Fixtures/hel_dummy_phase8.csv`
- `Hel/Hel.Tests/Fixtures/hel_dummy_missing_header.csv`

## Purpose

These fixtures validate:

- WAWL library scoping
- location filtering
- location-rule override routing
- call-number prefix routing
- Dewey range routing
- fallback from `items.effective_call_number` to `holdings.call_number`
- unreadable call number handling
- unassigned routing
- friendly missing-header error dialogs
- export output generation
- preview summary correctness

---

## General setup for all cases

1. Run the Hel app.
2. Click **Choose Output Folder** and select a normal user folder, for example:
   - `Desktop\Library Project Testing\Hel\Hel Test Output`
3. Click **Select CSV**.
4. Choose the correct fixture file for the case.
5. Select the required locations for the case.
6. Click **Preview**.
7. Verify the summary values shown in the app.
8. Click **Export** if output file verification is needed.

---

## Case 1 - All WAWL locations selected

### Input file
- `hel_dummy_phase8.csv`

### Location selection
- Select all loaded WAWL locations.

### Expected preview / summary
- Total rows loaded: 20
- Rows after WAWL filter: 18
- Rows after location filter: 18
- Assigned count: 12
- Unassigned count: 6
- Fallback count: 5
- Parse failures count: 2

### Expected bucket counts
- `wawl: 12`

### Expected export files
- `wawl.txt`
- `Unassigned.txt`
- `RunSummary.txt`

---

## Case 2 - Only `stacks`

### Input file
- `hel_dummy_phase8.csv`

### Location selection
- Select only:
  - `stacks`

### Expected preview / summary
- Total rows loaded: 20
- Rows after WAWL filter: 18
- Rows after location filter: 3
- Assigned count: 3
- Unassigned count: 0
- Fallback count: 1
- Parse failures count: 0

### Expected bucket counts
- `wawl: 3`

### Expected export files
- `wawl.txt`
- `RunSummary.txt`

### Validation focus
This verifies that the location rule overrides call-number logic.

---

## Case 3 - Only `fiction`

### Input file
- `hel_dummy_phase8.csv`

### Location selection
- Select only:
  - `fiction`

### Expected preview / summary
- Total rows loaded: 20
- Rows after WAWL filter: 18
- Rows after location filter: 6
- Assigned count: 4
- Unassigned count: 2
- Fallback count: 0
- Parse failures count: 1

### Expected bucket counts
- `wawl: 4`

### Expected export files
- `wawl.txt`
- `Unassigned.txt`
- `RunSummary.txt`

### Validation focus
This verifies:
- `M` prefix routing
- `REF` prefix trimming
- `Q` prefix trimming
- readable but unmatched call numbers
- unreadable call number handling

---

## Case 4 - Only `dvds`

### Input file
- `hel_dummy_phase8.csv`

### Location selection
- Select only:
  - `dvds`

### Expected preview / summary
- Total rows loaded: 20
- Rows after WAWL filter: 18
- Rows after location filter: 4
- Assigned count: 2
- Unassigned count: 2
- Fallback count: 4
- Parse failures count: 1

### Expected bucket counts
- `wawl: 2`

### Expected export files
- `wawl.txt`
- `Unassigned.txt`
- `RunSummary.txt`

### Validation focus
This is the main fallback test case. All rows in this case use `holdings.call_number` because `items.effective_call_number` is empty.

---

## Case 5 - Only `graphic` and `reserve`

### Input file
- `hel_dummy_phase8.csv`

### Location selection
- Select only:
  - `graphic`
  - `reserve`

### Expected preview / summary
- Total rows loaded: 20
- Rows after WAWL filter: 18
- Rows after location filter: 4
- Assigned count: 2
- Unassigned count: 2
- Fallback count: 0
- Parse failures count: 0

### Expected bucket counts
- `wawl: 2`

### Expected export files
- `wawl.txt`
- `Unassigned.txt`
- `RunSummary.txt`

### Validation focus
This verifies the Dewey range boundary:
- `99.999` matches
- `100.0` does not match

---

## Case 6 - Only `kids`

### Input file
- `hel_dummy_phase8.csv`

### Location selection
- Select only:
  - `kids`

### Expected preview / summary
- Total rows loaded: 20
- Rows after WAWL filter: 18
- Rows after location filter: 1
- Assigned count: 1
- Unassigned count: 0
- Fallback count: 0
- Parse failures count: 0

### Expected bucket counts
- `wawl: 1`

### Expected export files
- `wawl.txt`
- `RunSummary.txt`

### Validation focus
This verifies plain numeric Dewey routing without any prefix trimming or fallback behavior.

---

## Case 7 - Missing required header

### Input file
- `hel_dummy_missing_header.csv`

### Expected behavior
When the file is loaded, Hel should show a friendly error dialog.

### Expected error
- Missing required columns
- `items.effective_call_number`

### Validation focus
This verifies header validation and user-friendly error handling.

---

## Notes

### Export behavior
Exports should be written to the user-chosen output folder, not a hidden packaged-app path.

### Log behavior
Logs should be written to:

- `Documents\Hel\Logs`

### Files that should not be committed
The following are generated artifacts and must not be checked into source control:

- `wawl.txt`
- `Unassigned.txt`
- `RunSummary.txt`
- any `.log` files
- temporary output folders used during manual testing