using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.FamilyTrees;

namespace SlaegtsAssistent.App.ViewModels;

public partial class MainWindowViewModel
{
    private PersonGraphViewModel? _personGraph;
    private CancellationTokenSource? _graphCancellation;
    private long _graphRevision;
    private CancellationTokenSource? _graphDebounce;
    private bool _settingGraph;
    private string? _graphWorkspace;
    private string? _graphLayoutKey;
    private SlaegtsAssistent.Core.Graph.PersonGraphLayout? _graphLayout;
    private FamilyTreeViewModel? _familyTreeView;
    private SlaegtsAssistent.Core.Domain.FamilyTree? _treeSource;
    private FamilyTreeIndex? _treeIndex;

    public FamilyTreeViewModel FamilyTreeView => _familyTreeView ??= new FamilyTreeViewModel(
        SelectTreePerson, OpenGraphPerson, SetTreeProband);

    public FamilyTreeExportViewModel? CreateFamilyTreeExportViewModel()
    {
        var proband = PersonGraph.ProbandId;
        return _treeIndex is not null && _treeIndex.Find(proband) is not null
            ? new FamilyTreeExportViewModel(_treeIndex, proband!, FamilyTreeView.DirectionIndex)
            : null;
    }

    [ObservableProperty] private int graphViewIndex;
    public bool IsPersonNetwork => GraphViewIndex == 1;

    partial void OnGraphViewIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsPersonNetwork));
        if (value == 1) QueueGraphRefresh();
    }

    private void SelectTreePerson(string id)
    {
        var person = _allPeople.Where(item => item.RecordId == id).ToArray();
        if (person.Length != 1) return;
        if (!People.Contains(person[0])) PersonFilterText = string.Empty;
        SelectedPerson = person[0];
    }

    private void SetTreeProband(string id)
    {
        PersonGraph.ProbandId = id;
    }

    [RelayCommand]
    private void UseFamilyTreeProband()
    {
        var id = FamilyTreeView.SelectedNode?.PersonId ?? SelectedPerson?.RecordId;
        if (id is not null && _treeIndex?.Find(id) is not null) SetTreeProband(id);
        else FamilyTreeView.Status = "Vælg en person fra GEDCOM-filen som proband.";
    }

    private void ClearGraphWorkspace()
    {
        _graphCancellation?.Cancel();
        _graphDebounce?.Cancel();
        _graphRevision++;
        _treeSource = null;
        _displayedTreeSource = null;
        _treeIndex = null;
        _graphWorkspace = null;
        _graphLayout = null;
        _graphLayoutKey = null;
        FamilyTreeView.SetTree(null, null);
        _settingGraph = true;
        try { PersonGraph.SetGraph(new SlaegtsAssistent.Core.Graph.PersonGraphBuilder().Build([], []), null); }
        finally { _settingGraph = false; }
    }

    public PersonGraphViewModel PersonGraph
    {
        get
        {
            if (_personGraph is not null) return _personGraph;
            _personGraph = new PersonGraphViewModel(OpenGraphPerson);
            _personGraph.PropertyChanged += GraphPropertyChanged;
            _personGraph.Select(SelectedPerson?.RecordId);
            return _personGraph;
        }
    }

    [ObservableProperty] private int documentTabIndex = 1;
    public bool IsBiographyTab => DocumentTabIndex != 0;
    public bool IsGraphTab => DocumentTabIndex == 0;

    partial void OnDocumentTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsBiographyTab));
        OnPropertyChanged(nameof(IsGraphTab));
        if (value == 0) QueueGraphRefresh();
    }

    private async void QueueGraphRefresh()
    {
        if (DocumentTabIndex != 0 || IsImporting) return;
        _graphCancellation?.Cancel();
        _graphRevision++;
        _graphDebounce?.Cancel();
        var debounce = _graphDebounce = new CancellationTokenSource();
        try
        {
            await Task.Delay(300, debounce.Token);
            await RefreshGraphAsync();
        }
        catch (OperationCanceledException) { }
    }

    private void GraphPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (_settingGraph || _personGraph is null) return;
        if (args.PropertyName == nameof(PersonGraphViewModel.SelectedPerson) && _personGraph.SelectedPerson is { } selected)
        {
            var matches = _allPeople.Where(person => person.RecordId == selected.RecordId).ToArray();
            if (matches.Length == 1 && SelectedPerson != matches[0])
            {
                if (!People.Contains(matches[0])) PersonFilterText = string.Empty;
                SelectedPerson = matches[0];
            }
            return;
        }
        if (args.PropertyName != nameof(PersonGraphViewModel.ProbandId) ||
            _personGraph.ProbandId is null) return;
        FamilyTreeView.SetTree(_treeIndex, _personGraph.ProbandId);
        if (string.IsNullOrWhiteSpace(StandardMarkdownOutputFolder)) return;
        try { new GraphSettingsStore(new AtomicFileWriter()).Save(StandardMarkdownOutputFolder, _personGraph.ProbandId); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        { _personGraph.Status = $"Probanden kunne ikke gemmes: {exception.Message}"; }
    }

    [RelayCommand]
    private async Task RefreshGraphAsync()
    {
        if (IsImporting) return;
        _graphCancellation?.Cancel();
        _graphCancellation = new CancellationTokenSource();
        var token = _graphCancellation.Token;
        var revision = ++_graphRevision;
        var workspace = StandardMarkdownOutputFolder;
        if (_graphWorkspace != workspace)
        {
            _graphWorkspace = workspace;
            _graphLayoutKey = null;
            _graphLayout = null;
            _treeSource = null;
            _treeIndex = null;
            _displayedTreeSource = null;
            FamilyTreeView.SetTree(null, null);
            _settingGraph = true;
            PersonGraph.SetGraph(new SlaegtsAssistent.Core.Graph.PersonGraphBuilder().Build([], []), null);
            _settingGraph = false;
        }
        PersonGraph.Status = "Grafen indlæses…";
        try
        {
            var tree = _familyTree;
            if (tree is null && _treeSource is not null)
                tree = _treeSource;
            if (tree is null && !string.IsNullOrWhiteSpace(workspace))
            {
                var path = _gedcomSnapshotStore.GetValidatedSourceCopyPath(workspace);
                if (path is not null) tree = await Task.Run(() => _gedcomLoader.Load(path, null, token), token);
            }
            token.ThrowIfCancellationRequested();
            if (revision != _graphRevision || workspace != StandardMarkdownOutputFolder) return;
            string? savedProband = null;
            try { savedProband = new GraphSettingsStore(new AtomicFileWriter()).Load(workspace); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            { FamilyTreeView.Status = $"Grafens indstillinger kunne ikke læses: {exception.Message}"; }
            if (!ReferenceEquals(tree, _treeSource))
            {
                _treeSource = tree;
                _treeIndex = tree is null ? null : new FamilyTreeIndex(tree);
            }
            bool IsKnown(string? id) => id is not null &&
                (_treeIndex?.Find(id) is not null || _allPeople.Any(person => person.RecordId == id));
            var preferred = IsKnown(PersonGraph.ProbandId) ? PersonGraph.ProbandId
                : IsKnown(savedProband) ? savedProband
                : IsKnown(SelectedPerson?.RecordId) ? SelectedPerson?.RecordId
                :
                _treeIndex?.People.OrderBy(person => person.Name, StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("da-DK"), false))
                    .ThenBy(person => person.Id, StringComparer.Ordinal).FirstOrDefault()?.Id;
            if (PersonGraph.ProbandId != preferred)
                PersonGraph.ProbandId = preferred;
            if (FamilyTreeView.ProbandId != PersonGraph.ProbandId || !ReferenceEquals(tree, _displayedTreeSource))
            {
                _displayedTreeSource = tree;
                FamilyTreeView.SetTree(_treeIndex, PersonGraph.ProbandId);
            }
            if (!IsPersonNetwork) return;
            var graph = new WorkspaceGraphSource(_markdownFileStore).CreateSnapshot(_allPeople.ToArray(), _editors, tree);
            var executable = Path.Combine(AppContext.BaseDirectory, "tools", OperatingSystem.IsWindows() ? "sfdp.exe" : "sfdp");
#if DEBUG
            var developmentExecutable = Environment.GetEnvironmentVariable("SLAEGTSASSISTENT_SFDP");
            if (!string.IsNullOrWhiteSpace(developmentExecutable) && Path.IsPathFullyQualified(developmentExecutable))
                executable = developmentExecutable;
#endif
            if (graph.Nodes.Count > 0 && !File.Exists(executable))
            {
                _settingGraph = true;
                try { PersonGraph.SetGraph(graph, null, PersonGraph.ProbandId ?? SelectedPerson?.RecordId); }
                finally { _settingGraph = false; }
                PersonGraph.Select(SelectedPerson?.RecordId);
                PersonGraph.Status = "Grafens lokale Graphviz-værktøj mangler i programpakken.";
                return;
            }
            var dot = new SlaegtsAssistent.Core.Graph.GraphDotWriter().Write(graph).Dot;
            var version = File.Exists(executable) ? File.GetLastWriteTimeUtc(executable).Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture) : "";
            var identities = string.Concat(graph.Nodes.OrderBy(node => node.RecordId, StringComparer.Ordinal)
                .Select(node => $"{node.RecordId.Length}:{node.RecordId}"));
            var key = executable + "\n" + version + "\n" + identities + "\n" + dot;
            var layout = _graphLayoutKey == key && _graphLayout is not null
                ? _graphLayout
                : await new GraphvizLayoutService(new LocalProcessRunner(), executable).LayoutAsync(graph, token);
            if (revision != _graphRevision || workspace != StandardMarkdownOutputFolder) return;
            _graphLayoutKey = key;
            _graphLayout = layout;
            _settingGraph = true;
            try { PersonGraph.SetGraph(graph, layout, PersonGraph.ProbandId ?? savedProband ?? SelectedPerson?.RecordId); }
            finally { _settingGraph = false; }
            PersonGraph.Select(SelectedPerson?.RecordId);
        }
        catch (OperationCanceledException) { }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or FormatException or InvalidOperationException or GedcomSnapshotException or System.ComponentModel.Win32Exception)
        {
            if (revision == _graphRevision)
            {
                PersonGraph.Status = $"Grafen kunne ikke indlæses: {exception.Message}";
                FamilyTreeView.Status = PersonGraph.Status;
            }
        }
    }

    private SlaegtsAssistent.Core.Domain.FamilyTree? _displayedTreeSource;

    [RelayCommand]
    private void OpenSelectedBiography()
    {
        if (SelectedPerson is not null) OpenGraphPerson(SelectedPerson.RecordId);
    }

    private void OpenGraphPerson(string recordId)
    {
        if (IsImporting) return;
        var matches = _allPeople.Where(person => person.RecordId == recordId).ToArray();
        if (matches.Length != 1 || matches[0].HasDocumentDiagnostic || matches[0].SyncStatus == SlaegtsAssistent.Core.Biography.BiographySyncStatus.Tvetydig || !File.Exists(matches[0].MarkdownFilePath))
        {
            PersonGraph.Status = "Personens dokument kan ikke åbnes. Det mangler eller er ikke entydigt.";
            FamilyTreeView.Status = PersonGraph.Status;
            return;
        }
        OpenPersonDocument(new PersonDocumentLink(recordId, matches[0].DisplayName, matches[0].MarkdownFilePath));
        if (Editor is not null && SelectedPerson?.RecordId == recordId) DocumentTabIndex = 1;
    }
}
