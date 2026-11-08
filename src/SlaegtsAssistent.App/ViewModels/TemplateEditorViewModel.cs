using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using CommunityToolkit.Mvvm.Input;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.App.ViewModels;

public partial class TemplateEditorViewModel : ViewModelBase
{
    private readonly ITemplateEditorConfirmationService _confirmationService;
    private readonly ITemplateEditorPreviewService _previewService;
    private readonly TimeSpan _previewDelay;
    private readonly Stack<BiographyTemplateEditorModel> _undo = new();
    private readonly Stack<BiographyTemplateEditorModel> _redo = new();
    private BiographyTemplateEditorModel _model;
    private TemplateBlockItemViewModel? _selectedBlock;
    private BiographyTemplateBlockDescriptor? _selectedAvailableBlock;
    private BiographyTemplatePresentationDescriptor? _selectedPresentation;
    private TemplateFieldItemViewModel? _selectedField;
    private string? _errorMessage;
    private bool _refreshing;
    private TemplatePreviewPersonOption? _selectedPreviewPerson;
    private CancellationTokenSource? _previewCancellation;
    private long _previewGeneration;
    private string _previewMarkdown = string.Empty;
    private string _previewHtmlDocument = string.Empty;
    private string? _previewErrorMessage;
    private string? _previewDiagnosticsText;
    private int _previewNetworkRequestAttempts;
    private bool _isPreviewBusy;
    private Task _previewUpdateTask = Task.CompletedTask;

    public TemplateEditorViewModel(
        BiographyTemplateEditorModel model,
        ITemplateEditorConfirmationService? confirmationService = null,
        IEnumerable<Person>? previewPeople = null,
        ITemplateEditorPreviewService? previewService = null,
        TimeSpan? previewDelay = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        _model = Clone(model);
        _confirmationService = confirmationService ?? new AcceptResetConfirmationService();
        _previewService = previewService ?? new TemplateEditorPreviewService();
        _previewDelay = previewDelay ?? TimeSpan.FromMilliseconds(250);
        var example = BiographyTemplateExampleData.Create();
        PreviewPeople.Add(new TemplatePreviewPersonOption(
            "Eksempelperson – Anna Marie Jensen",
            example.Person,
            example.Submitter,
            true));
        foreach (var person in previewPeople ?? [])
        {
            PreviewPeople.Add(new TemplatePreviewPersonOption(
                $"{person.FullName ?? "Unavngivet person"} ({person.RecordId})",
                person,
                null,
                false));
        }
        _selectedPreviewPerson = PreviewPeople[0];
        Rebuild(model.Blocks.FirstOrDefault()?.Type);
    }

    public ObservableCollection<TemplateBlockItemViewModel> Blocks { get; } = [];

    public ObservableCollection<BiographyTemplateBlockDescriptor> AvailableBlocks { get; } = [];

    public ObservableCollection<BiographyTemplatePresentationDescriptor> AvailablePresentations { get; } = [];

    public ObservableCollection<TemplateFieldItemViewModel> Fields { get; } = [];

    public ObservableCollection<TemplatePreviewPersonOption> PreviewPeople { get; } = [];

    public TemplatePreviewPersonOption? SelectedPreviewPerson
    {
        get => _selectedPreviewPerson;
        set
        {
            if (ReferenceEquals(_selectedPreviewPerson, value))
            {
                return;
            }

            _selectedPreviewPerson = value;
            OnPropertyChanged();
            SchedulePreview();
        }
    }

    public string PreviewMarkdown
    {
        get => _previewMarkdown;
        private set
        {
            if (string.Equals(_previewMarkdown, value, StringComparison.Ordinal))
            {
                return;
            }

            _previewMarkdown = value;
            OnPropertyChanged();
        }
    }

    public string PreviewHtmlDocument
    {
        get => _previewHtmlDocument;
        private set
        {
            if (string.Equals(_previewHtmlDocument, value, StringComparison.Ordinal))
            {
                return;
            }

            _previewHtmlDocument = value;
            OnPropertyChanged();
        }
    }

    public string? PreviewErrorMessage
    {
        get => _previewErrorMessage;
        private set
        {
            if (string.Equals(_previewErrorMessage, value, StringComparison.Ordinal))
            {
                return;
            }

            _previewErrorMessage = value;
            OnPropertyChanged();
        }
    }

    public string? PreviewDiagnosticsText
    {
        get => _previewDiagnosticsText;
        private set
        {
            if (string.Equals(_previewDiagnosticsText, value, StringComparison.Ordinal))
            {
                return;
            }

            _previewDiagnosticsText = value;
            OnPropertyChanged();
        }
    }

    public int PreviewNetworkRequestAttempts
    {
        get => _previewNetworkRequestAttempts;
        private set
        {
            if (_previewNetworkRequestAttempts == value)
            {
                return;
            }

            _previewNetworkRequestAttempts = value;
            OnPropertyChanged();
        }
    }

    public bool IsPreviewBusy
    {
        get => _isPreviewBusy;
        private set
        {
            if (_isPreviewBusy == value)
            {
                return;
            }

            _isPreviewBusy = value;
            OnPropertyChanged();
        }
    }

    public Task PreviewUpdateTask
    {
        get => _previewUpdateTask;
        private set
        {
            _previewUpdateTask = value;
            OnPropertyChanged();
        }
    }

    public TemplateBlockItemViewModel? SelectedBlock
    {
        get => _selectedBlock;
        set
        {
            if (ReferenceEquals(_selectedBlock, value))
            {
                return;
            }

            _selectedBlock = value;
            OnPropertyChanged();
            RefreshSelectedBlock();
        }
    }

    public BiographyTemplateBlockDescriptor? SelectedAvailableBlock
    {
        get => _selectedAvailableBlock;
        set
        {
            if (Equals(_selectedAvailableBlock, value))
            {
                return;
            }

            _selectedAvailableBlock = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanAddBlock));
        }
    }

    public BiographyTemplatePresentationDescriptor? SelectedPresentation
    {
        get => _selectedPresentation;
        set
        {
            if (Equals(_selectedPresentation, value))
            {
                return;
            }

            _selectedPresentation = value;
            OnPropertyChanged();
            if (!_refreshing && value is not null)
            {
                UpdateSelectedBlock(block => block with { Presentation = value.Presentation });
            }
        }
    }

    public TemplateFieldItemViewModel? SelectedField
    {
        get => _selectedField;
        set
        {
            if (ReferenceEquals(_selectedField, value))
            {
                return;
            }

            _selectedField = value;
            OnPropertyChanged();
            NotifyStateChanged();
        }
    }

    public string SelectedBlockHeading
    {
        get => GetSelectedModelBlock()?.Heading ?? string.Empty;
        set
        {
            var block = GetSelectedModelBlock();
            if (block is null || string.Equals(block.Heading, value, StringComparison.Ordinal))
            {
                return;
            }

            UpdateSelectedBlock(item => item with { Heading = value ?? string.Empty });
        }
    }

    public bool SelectedBlockIsVisible
    {
        get => GetSelectedModelBlock()?.IsVisible ?? false;
        set
        {
            var block = GetSelectedModelBlock();
            if (block is null || block.IsVisible == value)
            {
                return;
            }

            UpdateSelectedBlock(item => item with { IsVisible = value });
        }
    }

    public bool HasSelectedBlock => SelectedBlock is not null;

    public bool HasFields => Fields.Count > 0 && SelectedBlock?.Type != BiographyTemplateBlockType.Title;

    public bool CanEditHeading => SelectedBlock?.Type is not null and
        not BiographyTemplateBlockType.Title and
        not BiographyTemplateBlockType.Submitter;

    public bool HasMultiplePresentations => AvailablePresentations.Count > 1;

    public bool CanAddBlock => SelectedAvailableBlock is not null;

    public bool CanMoveSelectedBlockUp => SelectedBlock is not null && Blocks.IndexOf(SelectedBlock) > 0;

    public bool CanMoveSelectedBlockDown =>
        SelectedBlock is not null && Blocks.IndexOf(SelectedBlock) >= 0 && Blocks.IndexOf(SelectedBlock) < Blocks.Count - 1;

    public bool CanMoveSelectedFieldUp => CanMoveField(-1);

    public bool CanMoveSelectedFieldDown => CanMoveField(1);

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (string.Equals(_errorMessage, value, StringComparison.Ordinal))
            {
                return;
            }

            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public event EventHandler<BiographyTemplateEditorModel?>? CloseRequested;

    public BiographyTemplateEditorModel CreateModel() => Clone(_model);

    [RelayCommand]
    private void AddBlock()
    {
        if (SelectedAvailableBlock is null || _model.Blocks.Any(block => block.Type == SelectedAvailableBlock.Type))
        {
            return;
        }

        var defaultBlock = BiographyTemplateEditorModel.CreateDefault().Blocks
            .Single(block => block.Type == SelectedAvailableBlock.Type);
        ApplyChange(new BiographyTemplateEditorModel(
            BiographyTemplateEditorModel.CurrentVersion,
            _model.Blocks.Append(defaultBlock).ToArray()), defaultBlock.Type);
    }

    [RelayCommand]
    private void RemoveSelectedBlock()
    {
        if (SelectedBlock is null)
        {
            return;
        }

        var index = Blocks.IndexOf(SelectedBlock);
        var remaining = _model.Blocks.Where(block => block.Type != SelectedBlock.Type).ToArray();
        var next = remaining.ElementAtOrDefault(Math.Min(index, remaining.Length - 1))?.Type;
        ApplyChange(new BiographyTemplateEditorModel(BiographyTemplateEditorModel.CurrentVersion, remaining), next);
    }

    [RelayCommand]
    private void MoveSelectedBlockUp() => MoveSelectedBlock(-1);

    [RelayCommand]
    private void MoveSelectedBlockDown() => MoveSelectedBlock(1);

    [RelayCommand]
    private void ToggleField(TemplateFieldItemViewModel? field)
    {
        if (field is null)
        {
            return;
        }

        UpdateSelectedBlock(block =>
        {
            var fields = block.Fields.ToList();
            if (fields.Remove(field.Field))
            {
                return new BiographyTemplateBlock(
                    block.Type,
                    block.IsVisible,
                    block.Heading,
                    block.Presentation,
                    fields);
            }

            fields.Add(field.Field);
            return new BiographyTemplateBlock(
                block.Type,
                block.IsVisible,
                block.Heading,
                block.Presentation,
                fields);
        });
    }

    [RelayCommand]
    private void MoveSelectedFieldUp() => MoveSelectedField(-1);

    [RelayCommand]
    private void MoveSelectedFieldDown() => MoveSelectedField(1);

    [RelayCommand]
    private void Undo()
    {
        if (_undo.Count == 0)
        {
            return;
        }

        var selectedType = SelectedBlock?.Type;
        _redo.Push(_model);
        _model = _undo.Pop();
        Rebuild(selectedType);
    }

    [RelayCommand]
    private void Redo()
    {
        if (_redo.Count == 0)
        {
            return;
        }

        var selectedType = SelectedBlock?.Type;
        _undo.Push(_model);
        _model = _redo.Pop();
        Rebuild(selectedType);
    }

    [RelayCommand]
    private async Task ResetAsync()
    {
        if (!await _confirmationService.ConfirmResetAsync())
        {
            return;
        }

        ApplyChange(BiographyTemplateEditorModel.CreateDefault(), BiographyTemplateBlockType.Title);
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            var model = CreateModel();
            _ = new BiographyTemplateEditorCompiler().Compile(model);
            ErrorMessage = null;
            CancelPreview();
            CloseRequested?.Invoke(this, model);
        }
        catch (BiographyTemplateEditorException exception)
        {
            ErrorMessage = exception.Message;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        CancelPreview();
        CloseRequested?.Invoke(this, null);
    }

    private void MoveSelectedBlock(int offset)
    {
        if (SelectedBlock is null)
        {
            return;
        }

        var blocks = _model.Blocks.ToList();
        var index = blocks.FindIndex(block => block.Type == SelectedBlock.Type);
        var target = index + offset;
        if (index < 0 || target < 0 || target >= blocks.Count)
        {
            return;
        }

        (blocks[index], blocks[target]) = (blocks[target], blocks[index]);
        ApplyChange(new BiographyTemplateEditorModel(BiographyTemplateEditorModel.CurrentVersion, blocks), SelectedBlock.Type);
    }

    private void MoveSelectedField(int offset)
    {
        if (SelectedField is null || !SelectedField.IsSelected)
        {
            return;
        }

        UpdateSelectedBlock(block =>
        {
            var fields = block.Fields.ToList();
            var index = fields.IndexOf(SelectedField.Field);
            var target = index + offset;
            if (index < 0 || target < 0 || target >= fields.Count)
            {
                return block;
            }

            (fields[index], fields[target]) = (fields[target], fields[index]);
            return new BiographyTemplateBlock(
                block.Type,
                block.IsVisible,
                block.Heading,
                block.Presentation,
                fields);
        });
    }

    private bool CanMoveField(int offset)
    {
        if (SelectedField is null || !SelectedField.IsSelected)
        {
            return false;
        }

        var selected = GetSelectedModelBlock();
        if (selected is null)
        {
            return false;
        }

        var index = selected.Fields.ToList().IndexOf(SelectedField.Field);
        var target = index + offset;
        return index >= 0 && target >= 0 && target < selected.Fields.Count;
    }

    private void UpdateSelectedBlock(Func<BiographyTemplateBlock, BiographyTemplateBlock> update)
    {
        var selected = GetSelectedModelBlock();
        if (selected is null)
        {
            return;
        }

        var updated = update(selected);
        if (Equals(updated, selected))
        {
            return;
        }

        var blocks = _model.Blocks.Select(block => block.Type == selected.Type ? updated : block).ToArray();
        ApplyChange(new BiographyTemplateEditorModel(BiographyTemplateEditorModel.CurrentVersion, blocks), selected.Type);
    }

    private BiographyTemplateBlock? GetSelectedModelBlock() => SelectedBlock is null
        ? null
        : _model.Blocks.SingleOrDefault(block => block.Type == SelectedBlock.Type);

    private void ApplyChange(BiographyTemplateEditorModel changed, BiographyTemplateBlockType? selectedType)
    {
        _undo.Push(_model);
        _redo.Clear();
        _model = changed;
        ErrorMessage = null;
        Rebuild(selectedType);
    }

    private void Rebuild(BiographyTemplateBlockType? selectedType)
    {
        _refreshing = true;
        try
        {
            Blocks.Clear();
            foreach (var block in _model.Blocks)
            {
                var descriptor = BiographyTemplateEditorCatalog.GetBlock(block.Type);
                Blocks.Add(new TemplateBlockItemViewModel(
                    block.Type,
                    descriptor.Label,
                    descriptor.Description,
                    block.IsVisible));
            }

            AvailableBlocks.Clear();
            foreach (var descriptor in BiographyTemplateEditorCatalog.Blocks
                         .Where(descriptor => _model.Blocks.All(block => block.Type != descriptor.Type)))
            {
                AvailableBlocks.Add(descriptor);
            }

            SelectedAvailableBlock = AvailableBlocks.FirstOrDefault();
            SelectedBlock = Blocks.FirstOrDefault(block => block.Type == selectedType) ?? Blocks.FirstOrDefault();
        }
        finally
        {
            _refreshing = false;
        }

        RefreshSelectedBlock();
        NotifyStateChanged();
        SchedulePreview();
    }

    private void SchedulePreview()
    {
        var personOption = SelectedPreviewPerson;
        if (personOption is null)
        {
            return;
        }

        _previewCancellation?.Cancel();
        _previewCancellation?.Dispose();
        _previewCancellation = new CancellationTokenSource();
        var generation = Interlocked.Increment(ref _previewGeneration);
        var model = CreateModel();
        PreviewUpdateTask = UpdatePreviewAsync(
            model,
            personOption,
            generation,
            _previewCancellation.Token);
    }

    private async Task UpdatePreviewAsync(
        BiographyTemplateEditorModel model,
        TemplatePreviewPersonOption personOption,
        long generation,
        CancellationToken cancellationToken)
    {
        IsPreviewBusy = true;
        try
        {
            if (_previewDelay > TimeSpan.Zero)
            {
                await Task.Delay(_previewDelay, cancellationToken);
            }

            var result = await _previewService.RenderAsync(
                model,
                personOption.Person,
                personOption.Submitter,
                cancellationToken);
            if (generation != Volatile.Read(ref _previewGeneration) || cancellationToken.IsCancellationRequested)
            {
                return;
            }

            PreviewMarkdown = result.Markdown;
            PreviewHtmlDocument = result.SafePreview.HtmlDocument;
            PreviewDiagnosticsText = result.SafePreview.Diagnostics.Count == 0
                ? null
                : string.Join(Environment.NewLine, result.SafePreview.Diagnostics);
            PreviewNetworkRequestAttempts = result.SafePreview.NetworkRequestAttempts;
            PreviewErrorMessage = null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (
            exception is BiographyTemplateEditorException or
                BiographyTemplateException or
                IOException or
                UnauthorizedAccessException)
        {
            if (generation == Volatile.Read(ref _previewGeneration))
            {
                PreviewErrorMessage = $"Forhåndsvisningen kunne ikke opdateres: {exception.Message}";
            }
        }
        finally
        {
            if (generation == Volatile.Read(ref _previewGeneration))
            {
                IsPreviewBusy = false;
            }
        }
    }

    private void CancelPreview()
    {
        _previewCancellation?.Cancel();
        _previewCancellation?.Dispose();
        _previewCancellation = null;
    }

    private void RefreshSelectedBlock()
    {
        _refreshing = true;
        try
        {
            var block = GetSelectedModelBlock();
            AvailablePresentations.Clear();
            Fields.Clear();
            if (block is not null)
            {
                foreach (var option in BiographyTemplateEditorCatalog.GetPresentations(block.Type))
                {
                    AvailablePresentations.Add(option);
                }

                _selectedPresentation = AvailablePresentations.Single(
                    option => option.Presentation == block.Presentation);
                OnPropertyChanged(nameof(SelectedPresentation));

                var descriptors = BiographyTemplateEditorCatalog.GetFields(block.Type);
                foreach (var field in block.Fields)
                {
                    var descriptor = descriptors.Single(item => item.Field == field);
                    Fields.Add(new TemplateFieldItemViewModel(
                        field,
                        descriptor.Label,
                        true,
                        () => ToggleFieldCommand.Execute(
                            Fields.FirstOrDefault(item => item.Field == field))));
                }

                foreach (var descriptor in descriptors.Where(
                             descriptor => block.Fields.All(field => field != descriptor.Field)))
                {
                    var field = descriptor.Field;
                    Fields.Add(new TemplateFieldItemViewModel(
                        field,
                        descriptor.Label,
                        false,
                        () => ToggleFieldCommand.Execute(
                            Fields.FirstOrDefault(item => item.Field == field))));
                }
            }

            _selectedField = Fields.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedField));
            OnPropertyChanged(nameof(SelectedBlockHeading));
            OnPropertyChanged(nameof(SelectedBlockIsVisible));
        }
        finally
        {
            _refreshing = false;
        }

        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(HasSelectedBlock));
        OnPropertyChanged(nameof(HasFields));
        OnPropertyChanged(nameof(CanEditHeading));
        OnPropertyChanged(nameof(HasMultiplePresentations));
        OnPropertyChanged(nameof(CanAddBlock));
        OnPropertyChanged(nameof(CanMoveSelectedBlockUp));
        OnPropertyChanged(nameof(CanMoveSelectedBlockDown));
        OnPropertyChanged(nameof(CanMoveSelectedFieldUp));
        OnPropertyChanged(nameof(CanMoveSelectedFieldDown));
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }

    private static BiographyTemplateEditorModel Clone(BiographyTemplateEditorModel model) =>
        new(model.Version, model.Blocks.Select(block => new BiographyTemplateBlock(
            block.Type,
            block.IsVisible,
            block.Heading,
            block.Presentation,
            block.Fields)).ToArray());

    private sealed class AcceptResetConfirmationService : ITemplateEditorConfirmationService
    {
        public Task<bool> ConfirmResetAsync() => Task.FromResult(true);
    }
}

public sealed record TemplateBlockItemViewModel(
    BiographyTemplateBlockType Type,
    string Label,
    string Description,
    bool IsVisible);

public sealed class TemplateFieldItemViewModel
{
    public TemplateFieldItemViewModel(
        BiographyTemplateField field,
        string label,
        bool isSelected,
        Action toggle)
    {
        Field = field;
        Label = label;
        IsSelected = isSelected;
        ToggleCommand = new RelayCommand(toggle);
    }

    public BiographyTemplateField Field { get; }

    public string Label { get; }

    public bool IsSelected { get; }

    public IRelayCommand ToggleCommand { get; }
}

public sealed record TemplatePreviewPersonOption(
    string Label,
    Person Person,
    Submitter? Submitter,
    bool IsExample);
