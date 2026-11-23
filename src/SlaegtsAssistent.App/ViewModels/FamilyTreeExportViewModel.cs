using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using SlaegtsAssistent.Core.FamilyTrees;

namespace SlaegtsAssistent.App.ViewModels;

public partial class FamilyTreeExportViewModel : ViewModelBase
{
    private readonly FamilyTreeIndex _index;
    private readonly string _probandId;
    private bool _adjusting;

    public FamilyTreeExportViewModel(FamilyTreeIndex index, string probandId, int directionIndex)
    {
        _index = index;
        _probandId = probandId;
        _adjusting = true;
        DirectionIndex = directionIndex;
        _adjusting = false;
        UpdateDefaults();
    }

    [ObservableProperty] private string ancestorNumber = "1";
    [ObservableProperty] private int directionIndex;
    [ObservableProperty] private int ancestorDepth;
    [ObservableProperty] private int descendantDepth;
    [ObservableProperty] private int formatIndex;
    [ObservableProperty] private int paperIndex;
    [ObservableProperty] private string rootDescription = "";
    [ObservableProperty] private string summary = "";
    [ObservableProperty] private string status = "";
    [ObservableProperty] private bool isExporting;

    public bool ShowAncestors => DirectionIndex is 0 or 1;
    public bool ShowDescendants => DirectionIndex is 0 or 2;
    public bool CanStart => !IsExporting && Plan is not null;
    public FamilyTreeExportPlan? Plan { get; private set; }
    public string Paper => PaperIndex == 1 ? "A3" : "A4";
    public string Format => FormatIndex switch { 1 => "docx", 2 => "odt", _ => "pdf" };

    partial void OnAncestorNumberChanged(string value) { if (!_adjusting) UpdateDefaults(); }
    partial void OnDirectionIndexChanged(int value)
    {
        OnPropertyChanged(nameof(ShowAncestors));
        OnPropertyChanged(nameof(ShowDescendants));
        if (!_adjusting) UpdateDefaults();
    }
    partial void OnAncestorDepthChanged(int value) { if (!_adjusting) UpdatePlan(); }
    partial void OnDescendantDepthChanged(int value) { if (!_adjusting) UpdatePlan(); }
    partial void OnIsExportingChanged(bool value) => OnPropertyChanged(nameof(CanStart));

    private void UpdateDefaults()
    {
        _adjusting = true;
        try
        {
            var planner = new FamilyTreeExportPlanner(_index);
            var number = AncestorNumber ?? "";
            var root = _index.FindAncestor(_probandId, number);
            if (root is null)
            {
                AncestorDepth = 0;
                DescendantDepth = 0;
            }
            else
            {
                var maxAncestors = ShowAncestors ? -planner.Create(_probandId, number, 5, 0).Rows.Min(row => row.Generation) : 0;
                var maxDescendants = 0;
                if (ShowDescendants)
                {
                    for (var depth = 1; depth <= 5; depth++)
                    {
                        try
                        {
                            var available = planner.Create(_probandId, number, 0, depth).Rows.Max(row => row.Generation);
                            maxDescendants = available;
                            if (available < depth) break;
                        }
                        catch (ArgumentException) { break; }
                    }
                }
                var ancestors = 0;
                var descendants = 0;
                while (ancestors + descendants < 5 && (ancestors < maxAncestors || descendants < maxDescendants))
                {
                    if (ancestors < maxAncestors && ancestors + descendants < 5) ancestors++;
                    if (descendants < maxDescendants && ancestors + descendants < 5) descendants++;
                }
                AncestorDepth = ancestors;
                DescendantDepth = descendants;
            }
        }
        finally { _adjusting = false; }
        UpdatePlan();
    }

    private void UpdatePlan()
    {
        try
        {
            var ancestors = ShowAncestors ? AncestorDepth : 0;
            var descendants = ShowDescendants ? DescendantDepth : 0;
            Plan = new FamilyTreeExportPlanner(_index).Create(_probandId, AncestorNumber ?? "", ancestors, descendants);
            RootDescription = $"Rodperson: {Plan.Root.Name}";
            Summary = $"{1 + ancestors + descendants} af 6 generationer\n" +
                string.Join("\n", Plan.Rows.Select(row => $"{row.Label}: {row.Cells.Count} personer"));
            Status = "";
        }
        catch (ArgumentException exception)
        {
            Plan = null;
            RootDescription = "Rodperson: Ukendt";
            Summary = "";
            Status = exception.Message;
        }
        OnPropertyChanged(nameof(CanStart));
    }
}
