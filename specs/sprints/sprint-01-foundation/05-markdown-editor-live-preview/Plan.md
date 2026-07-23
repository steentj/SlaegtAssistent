# Plan: Markdown Editor & Live Preview (TDD)

1. **Write failing test: `PreviewHtml` reflects `MarkdownText`**
   - Test: setting `EditorViewModel.MarkdownText = "# Hello"` results in `PreviewHtml` containing
     `<h1>Hello</h1>` (via `Markdig`).
   - Run tests → confirm failure (`EditorViewModel` doesn't exist yet).

2. **Implement minimal `EditorViewModel` with live HTML computation**
   - `[ObservableProperty] string MarkdownText`; `PreviewHtml` recomputed on change (either a
     computed property read on demand, or updated in the `OnMarkdownTextChanged` partial method).
   - Add `Markdig` NuGet reference to `SlaegtsAssistent.App` (or `Core`, if HTML conversion is
     considered domain-agnostic enough to live there — decide once and keep consistent).
   - Run tests → confirm pass.

3. **Write failing test: `PreviewHtml` updates when `MarkdownText` changes again**
   - Test: change `MarkdownText` twice, assert `PreviewHtml` matches the second value each time
     (guards against stale caching).
   - Run tests → confirm failure if caching is naive; otherwise keep as regression guard.

4. **Implement/confirm live recomputation**
   - Run tests → confirm pass.

5. **Write failing test: `IMarkdownFileStore` fake — `Load()` populates `MarkdownText`**
   - Define `IMarkdownFileStore { string Read(string path); void Write(string path, string
     content); }`. Test: `EditorViewModel.Load()` with a fake store returning `"# Test"` for a
     given path results in `MarkdownText == "# Test"`.
   - Run tests → confirm failure (interface/method don't exist yet).

6. **Implement `IMarkdownFileStore` interface and `EditorViewModel.Load()`**
   - Run tests → confirm pass.

7. **Write failing test: `Save()` writes current `MarkdownText` via the file store**
   - Test: after setting `MarkdownText` and calling `Save()`, the fake store's `Write` was called
     with the correct path and content (use a spy/fake, not a mock framework, to keep dependencies
     minimal — or add a lightweight mocking library if already conventional for the project).
   - Run tests → confirm failure.

8. **Implement `EditorViewModel.Save()`**
   - Run tests → confirm pass.

9. **Write failing test: empty/whitespace Markdown produces empty/minimal HTML, not an exception**
   - Test: `MarkdownText = ""` → `PreviewHtml` is empty or whitespace-only, no exception thrown.
   - Run tests → confirm failure only if an edge case isn't already handled; otherwise keep as a
     regression guard.

10. **Implement/confirm empty-input handling**
    - Run tests → confirm pass.

11. **Build the Editor/Preview tab UI** *(no unit test — visual layout, wired into feature 01's
    `TabControl` placeholder)*
    - "Editor" tab: multi-line `TextBox` bound (two-way) to `MarkdownText`.
    - "Preview" tab: HTML-rendering control bound (one-way) to `PreviewHtml`.
    - Wire `EditorViewModel` as the `DataContext` for these tabs, constructed with the real
      `IMarkdownFileStore` implementation (simple `File.ReadAllText`/`File.WriteAllText` wrapper)
      at the composition root.

12. **Manual smoke test**
    - Run the app, select a person (once feature 02–04 data is available, or a manually placed
      `.md` file for isolated testing), type in the Editor tab, switch to Preview, and confirm the
      rendered HTML updates to match.

13. **Refactor pass**
    - Ensure `EditorViewModel` has no direct `System.IO` calls (all routed through
      `IMarkdownFileStore`) so tests never touch the real filesystem.
    - Re-run the full test suite to confirm no regressions.
