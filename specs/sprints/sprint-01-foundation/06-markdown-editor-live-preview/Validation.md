# Validation: Markdown Editor & Live Preview

## Definition of Done
- [ ] `dotnet test tests/SlaegtsAssistent.App.Tests` passes, including all `EditorViewModel` tests
      listed in `Plan.md` (live preview computation, load, save, empty-input handling).
- [ ] `EditorViewModel` contains no direct filesystem access (`System.IO.File.*` calls) — all I/O
      goes through `IMarkdownFileStore`, verified by code review, enabling fully offline unit
      tests.
- [ ] The Editor tab (from feature 01's `TabControl` placeholder) allows typing Markdown text, and
      the Preview tab renders it as HTML, updating without requiring an app restart or manual
      refresh action.
- [ ] Saving from the Editor tab persists content to the correct person's `.md` file on disk
      (verified manually against a real file, and via unit test against the fake store).
- [ ] Empty or whitespace-only Markdown input does not throw an exception in either the ViewModel
      or the rendering control.

## How to Verify
1. Run:
   ```
   dotnet test tests/SlaegtsAssistent.App.Tests
   ```
   All tests pass, exit code 0.
2. Run the app (`dotnet run --project src/SlaegtsAssistent.App`), open a person's generated
   biography (from feature 04's output), type additional Markdown (e.g. a new heading or list),
   switch to the Preview tab, and visually confirm the HTML reflects the new content.
3. Click "Save", close and reopen the app (or reload the file), and confirm the edited content
   persisted to disk.
4. Clear all text in the Editor tab and switch to Preview — confirm no crash and an empty/blank
   preview is shown.

## Merge Criteria
Merge when all Definition of Done items are checked, `dotnet test` is green, and a reviewer
confirms the live preview updates correctly for representative Markdown (headings, lists, bold/
italic text) — completing the last Trin 1 roadmap task and making the app usable end-to-end
(load GEDCOM → generate biographies → edit → preview) without any AI features.
