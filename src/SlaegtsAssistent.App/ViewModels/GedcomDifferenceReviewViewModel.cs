using System;
using System.Collections.Generic;
using System.Linq;
using SlaegtsAssistent.App.Services;

namespace SlaegtsAssistent.App.ViewModels;

public sealed class GedcomDifferenceReviewViewModel
{
    private readonly Dictionary<string, bool> _choices;
    public IReadOnlyList<GedcomDifferencePerson> People { get; }
    public GedcomDifferencePerson? SelectedPerson { get; set; }
    public int SelectedCount => _choices.Values.Count(value => value);

    public GedcomDifferenceReviewViewModel(IReadOnlyList<GedcomDifferenceReviewItem> differences)
    {
        ArgumentNullException.ThrowIfNull(differences);
        _choices = differences.ToDictionary(
            item => item.Key,
            item => item.UseGedcomByDefault,
            StringComparer.Ordinal);
        People = differences.GroupBy(item => item.FilePath, StringComparer.Ordinal)
            .Select(group => new GedcomDifferencePerson(group.Key, group.First().PersonName, group.ToArray()))
            .ToArray();
        SelectedPerson = People.FirstOrDefault();
    }

    public string? PreviewContent =>
        SelectedPerson?.Differences.FirstOrDefault()?.CandidatePreviewFactory?.Invoke(_choices);

    public IReadOnlyList<GedcomDifferencePerson> FilterPeople(string? filter) =>
        string.IsNullOrWhiteSpace(filter) ? People : People.Where(person =>
            person.Name.Contains(filter.Trim(), StringComparison.CurrentCultureIgnoreCase) ||
            person.FilePath.Contains(filter.Trim(), StringComparison.CurrentCultureIgnoreCase)).ToArray();

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

public sealed record GedcomDifferencePerson(
    string FilePath, string Name, IReadOnlyList<GedcomDifferenceReviewItem> Differences)
{
    public string DisplayText => $"{Name} ({Differences.Count} ændringer)";
}
