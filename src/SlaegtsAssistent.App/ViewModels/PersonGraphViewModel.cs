using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SlaegtsAssistent.Core.Graph;

namespace SlaegtsAssistent.App.ViewModels;

public sealed class GraphPersonItemViewModel(PersonGraphNode person)
{
    public string RecordId => person.RecordId;
    public string Name => string.IsNullOrWhiteSpace(person.Name) ? $"Unavngiven ({person.RecordId})" : person.Name;
    public string Details => string.Join("\n", new[]
    {
        Name,
        string.IsNullOrWhiteSpace(person.BirthDate) ? null : $"Født: {person.BirthDate}",
        string.IsNullOrWhiteSpace(person.DeathDate) ? null : $"Død: {person.DeathDate}",
    }.Where(value => value is not null));
}

public partial class PersonGraphViewModel : ViewModelBase
{
    private readonly Action<string> _openPerson;
    private PersonGraph _graph = new PersonGraphBuilder().Build([], []);
    private Dictionary<string, GraphPersonItemViewModel> _items = new(StringComparer.Ordinal);
    private Dictionary<(string, string), PersonGraphEdge> _edges = new();

    public PersonGraphViewModel(Action<string> openPerson) => _openPerson = openPerson ?? throw new ArgumentNullException(nameof(openPerson));

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string? probandId;
    [ObservableProperty] private GraphPersonItemViewModel? selectedPerson;
    [ObservableProperty] private GraphPersonItemViewModel? hoveredPerson;
    [ObservableProperty] private PersonGraphLayout? layout;
    [ObservableProperty] private string status = "Ingen graf indlæst.";

    public PersonGraph Graph => _graph;
    public string ProbandText => ProbandId is not null && _items.TryGetValue(ProbandId, out var person)
        ? $"Proband: {person.Name}" : "Ingen proband valgt";
    public ObservableCollection<GraphPersonItemViewModel> SearchResults { get; } = [];
    public ObservableCollection<GraphPersonItemViewModel> DirectConnections { get; } = [];
    public string ConnectionSummary { get; private set; } = "Ingen person valgt.";

    public void SetGraph(PersonGraph graph, PersonGraphLayout? layout, string? preferredProbandId = null)
    {
        ArgumentNullException.ThrowIfNull(graph);
        _graph = graph;
        _edges = graph.Edges.ToDictionary(edge => (edge.FirstId, edge.SecondId));
        _items = graph.Nodes.ToDictionary(node => node.RecordId, node => new GraphPersonItemViewModel(node), StringComparer.Ordinal);
        Layout = layout;
        ProbandId = preferredProbandId is not null && _items.ContainsKey(preferredProbandId) ? preferredProbandId
            : ProbandId is not null && _items.ContainsKey(ProbandId) ? ProbandId
            : _items.Values.OrderBy(item => item.Name, StringComparer.Create(CultureInfo.GetCultureInfo("da-DK"), false))
                .ThenBy(item => item.RecordId, StringComparer.Ordinal).FirstOrDefault()?.RecordId;
        SelectedPerson = SelectedPerson is not null && _items.TryGetValue(SelectedPerson.RecordId, out var selected) ? selected : null;
        HoveredPerson = null;
        Status = graph.Diagnostics.Count == 0 ? $"{graph.Nodes.Count} personer, {graph.Edges.Count} forbindelser." : string.Join(" ", graph.Diagnostics.Distinct());
        RefreshSearch();
        OnPropertyChanged(nameof(Graph));
        OnPropertyChanged(nameof(ProbandText));
        UpdateConnections();
    }

    public void Hover(string? id) => HoveredPerson = id is not null && _items.TryGetValue(id, out var person) ? person : null;
    public void Select(string? id) => SelectedPerson = id is not null && _items.TryGetValue(id, out var person) ? person : null;
    partial void OnSearchTextChanged(string value) => RefreshSearch();
    partial void OnProbandIdChanged(string? value) => OnPropertyChanged(nameof(ProbandText));
    partial void OnSelectedPersonChanged(GraphPersonItemViewModel? value)
    {
        UpdateConnections();
        UseAsProbandCommand.NotifyCanExecuteChanged();
        OpenBiographyCommand.NotifyCanExecuteChanged();
    }
    partial void OnHoveredPersonChanged(GraphPersonItemViewModel? value) => UpdateConnections();

    private bool HasSelectedPerson() => SelectedPerson is not null;

    [RelayCommand(CanExecute = nameof(HasSelectedPerson))]
    private void UseAsProband()
    {
        if (SelectedPerson is not null) ProbandId = SelectedPerson.RecordId;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedPerson))]
    private void OpenBiography()
    {
        if (SelectedPerson is not null) _openPerson(SelectedPerson.RecordId);
    }

    private void RefreshSearch()
    {
        var query = SearchText?.Trim() ?? string.Empty;
        SearchResults.Clear();
        foreach (var item in _items.Values.Where(item => item.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) || item.RecordId.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Name, StringComparer.Create(CultureInfo.GetCultureInfo("da-DK"), false)).ThenBy(item => item.RecordId, StringComparer.Ordinal))
            SearchResults.Add(item);
    }

    private void UpdateConnections()
    {
        DirectConnections.Clear();
        var active = SelectedPerson ?? HoveredPerson;
        if (active is null || !_graph.Neighbours.TryGetValue(active.RecordId, out var neighbours))
        {
            ConnectionSummary = "Ingen person valgt.";
        }
        else
        {
            DirectConnections.Add(active);
            var descriptions = new List<string>();
            foreach (var id in neighbours)
            {
                if (!_items.TryGetValue(id, out var item)) continue;
                DirectConnections.Add(item);
                var pair = StringComparer.Ordinal.Compare(active.RecordId, id) < 0
                    ? (active.RecordId, id) : (id, active.RecordId);
                var relations = _edges[pair].Relations
                    .Select(relation => relation.Kind switch
                    {
                        GraphRelationKind.Parent => relation.SourceId == active.RecordId ? "barn" : "forælder",
                        GraphRelationKind.Partner => "partner",
                        _ => "dokumentlink",
                    }).Distinct();
                descriptions.Add($"{item.Name} ({string.Join(", ", relations)})");
            }
            var origin = SelectedPerson is null ? "Person under markøren" : "Valgt person";
            ConnectionSummary = descriptions.Count == 0
                ? $"{origin}: {active.Name}. Ingen direkte forbindelser."
                : $"{origin}: {active.Name}. Direkte forbindelser: {string.Join("; ", descriptions)}.";
        }
        OnPropertyChanged(nameof(ConnectionSummary));
    }
}
