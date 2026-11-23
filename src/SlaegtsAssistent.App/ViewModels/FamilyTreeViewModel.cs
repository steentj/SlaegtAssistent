using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SlaegtsAssistent.Core.FamilyTrees;

namespace SlaegtsAssistent.App.ViewModels;

public enum FamilyTreeDirection { Both, Ancestors, Descendants }

public partial class FamilyTreeNodeViewModel : ViewModelBase
{
    private readonly Func<IReadOnlyList<FamilyTreeNodeViewModel>>? _load;
    private bool _loaded;

    public FamilyTreeNodeViewModel(string title, string details, string? personId,
        Func<IReadOnlyList<FamilyTreeNodeViewModel>>? load = null, bool hasChildren = false)
    {
        Title = title;
        Details = details;
        PersonId = personId;
        _load = load;
        if (hasChildren) Children.Add(new FamilyTreeNodeViewModel("Indlæses…", "", null));
    }

    public string Title { get; }
    public string Details { get; }
    public string? PersonId { get; }
    public ObservableCollection<FamilyTreeNodeViewModel> Children { get; } = [];
    [ObservableProperty] private bool isExpanded;

    partial void OnIsExpandedChanged(bool value)
    {
        if (!value || _loaded || _load is null) return;
        _loaded = true;
        var children = _load();
        Children.Clear();
        foreach (var child in children) Children.Add(child);
    }
}

public partial class FamilyTreeViewModel : ViewModelBase
{
    private FamilyTreeIndex? _index;
    private readonly Action<string> _select;
    private readonly Action<string> _open;
    private readonly Action<string> _setProband;
    private FamilyTreeNodeViewModel? _root;
    private FamilyTreeNodeViewModel? _ancestors;
    private FamilyTreeNodeViewModel? _descendants;

    public FamilyTreeViewModel(Action<string> select, Action<string> open, Action<string> setProband)
    {
        _select = select;
        _open = open;
        _setProband = setProband;
    }

    public ObservableCollection<FamilyTreeNodeViewModel> Roots { get; } = [];
    [ObservableProperty] private FamilyTreeNodeViewModel? selectedNode;
    [ObservableProperty] private int directionIndex;
    [ObservableProperty] private string status = "Indlæs en GEDCOM-fil for at vise slægtstræet.";
    public string? ProbandId { get; private set; }
    public string ProbandText => _index?.Find(ProbandId) is { } person ? $"Proband: {person.Name}" : "Ingen proband valgt";

    partial void OnSelectedNodeChanged(FamilyTreeNodeViewModel? value)
    {
        UseAsProbandCommand.NotifyCanExecuteChanged();
        OpenBiographyCommand.NotifyCanExecuteChanged();
        if (value?.PersonId is { } id) _select(id);
    }

    partial void OnDirectionIndexChanged(int value) => UpdateGroups();

    public void SetTree(FamilyTreeIndex? index, string? probandId)
    {
        var state = ProbandId == probandId && _root is not null
            ? CaptureState() : null;
        _index = index;
        ProbandId = probandId;
        Roots.Clear();
        SelectedNode = null;
        _root = null;
        _ancestors = null;
        _descendants = null;
        OnPropertyChanged(nameof(ProbandText));
        var person = index?.Find(probandId);
        if (person is null)
        {
            Status = index is null || index.People.Count == 0
                ? "Indlæs en GEDCOM-fil for at vise slægtstræet."
                : "Vælg en person fra GEDCOM-filen som proband.";
            return;
        }
        Status = "";
        var path = new HashSet<string>(StringComparer.Ordinal) { person.Id };
        _ancestors = Group("Forfædre", () => AncestorChildren(person, BigInteger.One, path), index!.Parents(person.Id).HasCandidates);
        _descendants = Group("Efterkommere", () => DescendantChildren(person, path), index!.Children(person.Id).Count > 0);
        _root = new FamilyTreeNodeViewModel($"#1 {person.Name}",
            $"Født: {person.BirthDate}   Død: {person.DeathDate}", person.Id);
        _root.IsExpanded = true;
        UpdateGroups();
        Roots.Add(_root);
        if (state is not null)
        {
            RestoreState(_root, "rod", state);
            if (_ancestors is not null) RestoreState(_ancestors, "forfædre", state);
            if (_descendants is not null) RestoreState(_descendants, "efterkommere", state);
        }
    }

    private Dictionary<string, bool> CaptureState()
    {
        var state = new Dictionary<string, bool>(StringComparer.Ordinal);
        if (_root is not null) CaptureState(_root, "rod", state, false);
        if (_ancestors is not null) CaptureState(_ancestors, "forfædre", state, true);
        if (_descendants is not null) CaptureState(_descendants, "efterkommere", state, true);
        return state;
    }

    private static void CaptureState(FamilyTreeNodeViewModel node, string key, Dictionary<string, bool> state, bool descendants)
    {
        state[key] = node.IsExpanded;
        if (!descendants) return;
        for (var i = 0; i < node.Children.Count; i++)
        {
            var child = node.Children[i];
            CaptureState(child, ChildKey(key, child, i), state, true);
        }
    }

    private static void RestoreState(FamilyTreeNodeViewModel node, string key, Dictionary<string, bool> state)
    {
        if (!state.TryGetValue(key, out var expanded)) return;
        if (!node.IsExpanded && node.Children.Count > 0 &&
            state.Keys.Any(candidate => candidate.StartsWith(key + "/", StringComparison.Ordinal)))
            node.IsExpanded = true;
        for (var i = 0; i < node.Children.Count; i++)
        {
            var child = node.Children[i];
            RestoreState(child, ChildKey(key, child, i), state);
        }
        node.IsExpanded = expanded;
    }

    private static string ChildKey(string parent, FamilyTreeNodeViewModel node, int position) =>
        parent + "/" + (node.PersonId ?? position + ":" + node.Title);

    public void SelectVisible(string? id)
    {
        if (id is null) return;
        if (SelectedNode?.PersonId == id) return;
        var match = Visible(Roots).FirstOrDefault(node => node.PersonId == id);
        SelectedNode = match;
    }

    private static IEnumerable<FamilyTreeNodeViewModel> Visible(IEnumerable<FamilyTreeNodeViewModel> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            if (node.IsExpanded)
                foreach (var child in Visible(node.Children)) yield return child;
        }
    }

    private void UpdateGroups()
    {
        if (_root is null) return;
        _root.Children.Clear();
        if (DirectionIndex is 0 or 1 && _ancestors is not null && _ancestors.Children.Count > 0) _root.Children.Add(_ancestors);
        if (DirectionIndex is 0 or 2 && _descendants is not null && _descendants.Children.Count > 0) _root.Children.Add(_descendants);
    }

    private static FamilyTreeNodeViewModel Group(string label, Func<IReadOnlyList<FamilyTreeNodeViewModel>> load, bool hasChildren)
    {
        var group = new FamilyTreeNodeViewModel(label, "", null, load, hasChildren);
        if (hasChildren) group.IsExpanded = true;
        return group;
    }

    private IReadOnlyList<FamilyTreeNodeViewModel> AncestorChildren(FamilyTreePerson person, BigInteger number, HashSet<string> path)
    {
        if (_index is null) return [];
        var parents = _index.Parents(person.Id);
        if (!parents.HasCandidates) return [];
        return [Ancestor(parents.Father, number * 2, path, parents.Explanation),
                Ancestor(parents.Mother, number * 2 + 1, path, parents.Explanation)];
    }

    private FamilyTreeNodeViewModel Ancestor(FamilyTreePerson? person, BigInteger number, HashSet<string> path, string? explanation)
    {
        if (person is null) return new($"#{number} Ukendt", explanation ?? "", null);
        if (path.Contains(person.Id)) return new($"#{number} Ukendt", "Slægtslinjen indeholder en cirkel.", null);
        var next = new HashSet<string>(path, StringComparer.Ordinal) { person.Id };
        return new($"#{number} {person.Name}", $"Født: {person.BirthDate}   Død: {person.DeathDate}", person.Id,
            () => AncestorChildren(person, number, next), _index!.Parents(person.Id).HasCandidates);
    }

    private IReadOnlyList<FamilyTreeNodeViewModel> DescendantChildren(FamilyTreePerson person, HashSet<string> path)
    {
        if (_index is null) return [];
        return _index.Children(person.Id).Select(child =>
        {
            if (path.Contains(child.Person.Id))
                return new FamilyTreeNodeViewModel($"#Ukendt {child.Person.Name}",
                    $"Født: {child.Person.BirthDate}   Død: {child.Person.DeathDate}   Slægtslinjen indeholder en cirkel.", null);
            var next = new HashSet<string>(path, StringComparer.Ordinal) { child.Person.Id };
            var relation = child.Relation is null ? "" : $"  ({child.Relation})";
            return new FamilyTreeNodeViewModel($"#Ukendt {child.Person.Name}{relation}",
                $"Født: {child.Person.BirthDate}   Død: {child.Person.DeathDate}", child.Person.Id,
                () => DescendantChildren(child.Person, next), _index.Children(child.Person.Id).Count > 0);
        }).ToArray();
    }

    private bool HasSelectedPerson() => SelectedNode?.PersonId is not null;

    [RelayCommand(CanExecute = nameof(HasSelectedPerson))]
    private void UseAsProband()
    {
        if (SelectedNode?.PersonId is { } id) _setProband(id);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedPerson))]
    private void OpenBiography()
    {
        if (SelectedNode?.PersonId is { } id) _open(id);
    }
}
