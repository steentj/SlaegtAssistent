using CommunityToolkit.Mvvm.ComponentModel;
using SlaegtsAssistent.Core.Versioning;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SlaegtsAssistent.App.ViewModels;

public sealed partial class VersionDiffViewModel : ViewModelBase
{
    private const int MaxVisibleLines = 20_000;

    [ObservableProperty]
    private string title = "Ændringer";

    [ObservableProperty]
    private string? notice;

    [ObservableProperty]
    private DiffFileViewModel? selectedFile;

    public ObservableCollection<DiffFileViewModel> Files { get; } = [];
    public ObservableCollection<DiffLineViewModel> VisibleLines { get; } = [];

    public void SetContent(string newTitle, IReadOnlyList<DiffFile> files, string? suppliedNotice)
    {
        Title = newTitle;
        Files.Clear();
        var totalLines = files.Sum(file => file.Lines.Count);
        var truncated = totalLines > MaxVisibleLines;
        var remaining = MaxVisibleLines;
        foreach (var file in files)
        {
            var lines = new List<DiffLineViewModel>();
            foreach (var line in file.Lines)
            {
                if (remaining <= 0)
                {
                    break;
                }

                lines.Add(new DiffLineViewModel(line));
                remaining--;
            }

            Files.Add(new DiffFileViewModel(file.Path, KindToText(file.Kind), lines));
            if (remaining <= 0)
            {
                break;
            }
        }

        Notice = truncated
            ? "Visningen er afkortet. Åbn dokumentet eller brug Git til at se resten."
            : suppliedNotice;
        SelectedFile = Files.FirstOrDefault();
        RefreshVisibleLines();
    }

    partial void OnSelectedFileChanged(DiffFileViewModel? value)
    {
        RefreshVisibleLines();
    }

    private void RefreshVisibleLines()
    {
        VisibleLines.Clear();
        if (SelectedFile is null)
        {
            return;
        }

        foreach (var line in SelectedFile.Lines)
        {
            VisibleLines.Add(line);
        }
    }

    private static string KindToText(WorkingTreeChangeKind kind) => kind switch
    {
        WorkingTreeChangeKind.Ny => "Ny",
        WorkingTreeChangeKind.Slettet => "Slettet",
        _ => "Ændret",
    };
}

public sealed class DiffFileViewModel(string path, string kindText, IReadOnlyList<DiffLineViewModel> lines)
{
    public string Path { get; } = path;
    public string KindText { get; } = kindText;
    public IReadOnlyList<DiffLineViewModel> Lines { get; } = lines;
}

public sealed class DiffLineViewModel(DiffLine source)
{
    public DiffLineKind Kind { get; } = source.Kind;
    public string Text { get; } = source.Text;
    public string BackgroundHex { get; } = source.Kind switch
    {
        DiffLineKind.Added => "#3322AA44",
        DiffLineKind.Removed => "#33CC4455",
        DiffLineKind.HunkHeader => "#334477CC",
        _ => "Transparent",
    };
}
