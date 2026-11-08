using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.ViewModels;

public partial class MainWindowViewModel
{
    private bool CanApplyBiographyTemplate() => !IsImporting &&
        !string.IsNullOrWhiteSpace(StandardMarkdownOutputFolder) && _documentPeople.Count > 0;

    [RelayCommand(CanExecute = nameof(CanApplyBiographyTemplate))]
    private async Task ApplyBiographyTemplateAsync()
    {
        if (!CanApplyBiographyTemplate())
        {
            return;
        }

        var outputFolder = StandardMarkdownOutputFolder!;
        IsImporting = true;
        ErrorMessage = null;
        _importCancellation = new CancellationTokenSource();
        var cancellationToken = _importCancellation.Token;
        try
        {
            SetImportPhase("Forbereder skabelonændringer", 15);
            var source = string.IsNullOrWhiteSpace(GlobalBiographyTemplatePath)
                ? BiographyTemplateMarkdownGenerator.DefaultTemplate
                : new BiographyTemplateLoader().Load(GlobalBiographyTemplatePath).Source;
            var documents = _markdownDocumentCatalog.Load(outputFolder);
            if (documents.Any(item => item.ErrorCategory is not null) ||
                documents.GroupBy(item => item.RecordId, StringComparer.Ordinal).Any(group => group.Count() > 1))
            {
                throw new FormatException("Arbejdsområdet indeholder ugyldige eller tvetydige persondokumenter. Ret dokumentfejlene først.");
            }

            var originals = documents.ToDictionary(item => item.FilePath, item =>
                _editors.TryGetValue(item.FilePath, out var editor)
                    ? editor.CreateDocument()
                    : BiographyDocumentParser.Parse(_markdownFileStore.Read(item.FilePath)), StringComparer.Ordinal);
            var personLinks = new BiographyPersonLinks(documents.Select(item =>
                KeyValuePair.Create(item.RecordId, item.FilePath)));
            var names = originals.Values.Where(item => item.Metadata is not null)
                .ToDictionary(item => item.Metadata!.RecordId, item => item.Metadata!.Facts.FullName, StringComparer.Ordinal);
            var gedcomDirectory = string.IsNullOrWhiteSpace(SelectedGedcomFilePath)
                ? null : Path.GetDirectoryName(SelectedGedcomFilePath);
            var reviewItems = await Task.Run(() => documents.Select(info =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var document = originals[info.FilePath];
                var candidate = new BiographyTemplateDocumentUpdater().CreateCandidate(
                    document, source, outputFolder, gedcomDirectory, personLinks, names);
                var parsed = BiographyDocumentParser.Parse(candidate);
                if (document.Metadata!.TemplateHash == parsed.Metadata!.TemplateHash &&
                    string.Equals(document.Body.TrimEnd('\r', '\n'), parsed.Body.TrimEnd('\r', '\n'), StringComparison.Ordinal))
                {
                    return null;
                }

                var key = $"{info.FilePath}|template";
                return new GedcomDifferenceReviewItem(key, info.DisplayName, info.FilePath,
                    document, document.Metadata.Facts,
                    new BiographyDifference("Skabelon", document.Body, parsed.Body), true)
                {
                    CandidateContent = candidate,
                    StructuredDifference = new BiographyStructuredDifference("template", "Skabelon",
                        document.Metadata.TemplateHash, document.Metadata.TemplateHash, parsed.Metadata.TemplateHash,
                        BiographyDifferenceKind.Changed, BiographyDifferenceCause.Template),
                    CandidatePreviewFactory = choices => choices.TryGetValue(key, out var approved) && approved
                        ? candidate : null,
                };
            }).OfType<GedcomDifferenceReviewItem>().ToArray(), cancellationToken);

            if (reviewItems.Length == 0)
            {
                SetImportPhase("Skabelonen er allerede anvendt", 100);
                return;
            }

            SetImportPhase("Gennemgang af skabelonændringer", 45);
            var choices = await _gedcomDifferenceDialogService.ShowAsync(reviewItems).WaitAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var approvedItems = reviewItems.Where(item => choices?.TryGetValue(item.Key, out var approved) == true && approved)
                .ToArray();
            if (approvedItems.Length == 0)
            {
                SetImportPhase("Skabelonændringer afvist – dokumenterne er uændrede", 100);
                return;
            }

            var changes = approvedItems.Select(item => new PlannedDocumentChange(
                item.FilePath, item.CandidateContent!, _editors.GetValueOrDefault(item.FilePath))).ToArray();
            var dirtyEditors = changes.Where(change => change.Editor is { IsDirty: true }).ToHashSet();
            var originalFiles = await Task.Run(() => changes.Where(change => !dirtyEditors.Contains(change))
                .ToDictionary(change => change.FilePath, change => File.ReadAllBytes(change.FilePath), StringComparer.Ordinal),
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            EnsureTemplateInputsUnchanged(originals, approvedItems, includeFiles: true);
            SetImportPhase("Gennemførelse", 70);
            try
            {
                await Task.Run(() =>
                {
                    foreach (var change in changes.Where(change => !dirtyEditors.Contains(change)))
                    {
                        _markdownFileStore.Write(change.FilePath, change.Content);
                    }
                }, CancellationToken.None);
                EnsureTemplateInputsUnchanged(originals, approvedItems, includeFiles: false);
            }
            catch (Exception commitException)
            {
                try
                {
                    await Task.Run(() => RestoreTemplateDocuments(originalFiles), CancellationToken.None);
                }
                catch (Exception rollbackException)
                {
                    throw new ImportCommitException("Skabelonændringerne fejlede, og automatisk gendannelse kunne ikke fuldføres.",
                        new AggregateException(commitException, rollbackException));
                }
                throw;
            }

            foreach (var change in changes)
            {
                change.Editor?.ApplySerializedDocument(change.Content, markDirty: dirtyEditors.Contains(change));
            }
            SetImportPhase($"Skabelonen er anvendt på {changes.Length} dokumenter", 100);
        }
        catch (OperationCanceledException)
        {
            ImportPhaseText = "Skabelonændringer annulleret";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
            BiographyTemplateException or FormatException or InvalidOperationException)
        {
            ImportPhaseText = "Fejl";
            ErrorMessage = $"Skabelonen kunne ikke anvendes: {exception.Message}";
        }
        finally
        {
            _importCancellation.Dispose();
            _importCancellation = null;
            IsImporting = false;
        }
    }

    private void EnsureTemplateInputsUnchanged(
        IReadOnlyDictionary<string, BiographyDocument> originals,
        IEnumerable<GedcomDifferenceReviewItem> approvedItems,
        bool includeFiles)
    {
        foreach (var item in approvedItems)
        {
            BiographyDocument current;
            if (_editors.TryGetValue(item.FilePath, out var editor))
            {
                current = editor.CreateDocument();
            }
            else if (includeFiles)
            {
                current = BiographyDocumentParser.Parse(_markdownFileStore.Read(item.FilePath));
            }
            else
            {
                continue;
            }

            var original = originals[item.FilePath];
            if (current.Metadata is null || !string.Equals(
                BiographyDocumentSerializer.Serialize(original.Metadata!, original.Body),
                BiographyDocumentSerializer.Serialize(current.Metadata, current.Body), StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Dokumentet '{item.PersonName}' blev ændret under gennemgangen. Bevar dine ændringer og anvend skabelonen igen.");
            }
        }
    }

    private static void RestoreTemplateDocuments(IReadOnlyDictionary<string, byte[]> originalFiles)
    {
        var writer = new AtomicFileWriter();
        foreach (var original in originalFiles)
        {
            if (!File.Exists(original.Key) || !File.ReadAllBytes(original.Key).AsSpan().SequenceEqual(original.Value))
            {
                writer.WriteBytes(original.Key, original.Value);
            }
        }
    }
}
