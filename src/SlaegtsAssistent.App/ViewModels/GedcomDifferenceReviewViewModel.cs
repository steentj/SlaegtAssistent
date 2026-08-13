using System;
using System.Collections.Generic;
using System.Linq;
using SlaegtsAssistent.App.Services;

namespace SlaegtsAssistent.App.ViewModels;

public sealed class GedcomDifferenceReviewViewModel
{
    private readonly IReadOnlyList<GedcomDifferenceReviewItem> _differences;
    private readonly Dictionary<string, bool> _choices;

    public GedcomDifferenceReviewViewModel(IReadOnlyList<GedcomDifferenceReviewItem> differences)
    {
        ArgumentNullException.ThrowIfNull(differences);
        _differences = differences;
        _choices = differences.ToDictionary(
            item => item.Key,
            item => item.UseGedcomByDefault,
            StringComparer.Ordinal);
    }

    public string? PreviewContent =>
        _differences.FirstOrDefault()?.CandidatePreviewFactory?.Invoke(_choices);

    public bool UsesImportedValue(string key) =>
        _choices.TryGetValue(key, out var selected) && selected;

    public void SetChoice(string key, bool useImported)
    {
        if (!_choices.ContainsKey(key))
        {
            throw new ArgumentException("Valget findes ikke i gennemgangen.", nameof(key));
        }

        _choices[key] = useImported;
    }

    public void UseAllImported()
    {
        foreach (var key in _choices.Keys.ToArray())
        {
            _choices[key] = true;
        }
    }

    public void KeepAllDocumentValues()
    {
        foreach (var key in _choices.Keys.ToArray())
        {
            _choices[key] = false;
        }
    }

    public IReadOnlyDictionary<string, bool> CreateDecision() =>
        new Dictionary<string, bool>(_choices, StringComparer.Ordinal);
}
