using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;

namespace SlaegtsAssistent.App.Views;

public enum GraphExportFormat { Svg, Png, Pdf, Print }

public sealed record GraphExportSelection(GraphExportFormat Format, bool Viewport, bool Labels, int Resolution, int PaperIndex);

public sealed class GraphExportOptionsWindow : Window
{
    private readonly ComboBox _format;
    private readonly ComboBox _resolution;
    private readonly ComboBox _paper;
    private readonly CheckBox _viewport;
    private readonly CheckBox _labels;
    private readonly StackPanel _resolutionPanel;
    private readonly StackPanel _paperPanel;
    private readonly Button _submit;

    public GraphExportOptionsWindow(GraphExportFormat initialFormat = GraphExportFormat.Png)
    {
        Title = "Eksportér personnetværk";
        Width = 440;
        SizeToContent = SizeToContent.Height;
        MinWidth = 360;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var root = new StackPanel { Margin = new Thickness(20), Spacing = 12 };
        _format = new ComboBox { Name = "Format", HorizontalAlignment = HorizontalAlignment.Stretch, SelectedIndex = (int)initialFormat };
        _format.Items.Add("SVG-billede");
        _format.Items.Add("PNG-billede");
        _format.Items.Add("PDF-dokument");
        _format.Items.Add("Udskriv via PDF");
        _format.SelectedIndex = (int)initialFormat;
        AutomationProperties.SetName(_format, "Format");
        root.Children.Add(new TextBlock { Text = "Format" });
        root.Children.Add(_format);
        _viewport = new CheckBox { Name = "Viewport", Content = "Kun det aktuelle udsnit" };
        _labels = new CheckBox { Name = "Labels", Content = "Vis navne og datoer", IsChecked = true };
        root.Children.Add(_viewport);
        root.Children.Add(_labels);
        _resolution = new ComboBox { Name = "Resolution", HorizontalAlignment = HorizontalAlignment.Stretch };
        _resolution.Items.Add("2048 px"); _resolution.Items.Add("4096 px"); _resolution.Items.Add("8192 px");
        _resolution.SelectedIndex = 1;
        AutomationProperties.SetName(_resolution, "Opløsning");
        _resolutionPanel = new StackPanel { Name = "ResolutionPanel", Spacing = 4 };
        _resolutionPanel.Children.Add(new TextBlock { Text = "Opløsning" });
        _resolutionPanel.Children.Add(_resolution);
        root.Children.Add(_resolutionPanel);
        _paper = new ComboBox { Name = "Paper", HorizontalAlignment = HorizontalAlignment.Stretch };
        _paper.Items.Add("A4 liggende"); _paper.Items.Add("A4 stående"); _paper.Items.Add("A3 liggende"); _paper.Items.Add("A3 stående");
        _paper.SelectedIndex = 0;
        AutomationProperties.SetName(_paper, "Papirformat");
        _paperPanel = new StackPanel { Name = "PaperPanel", Spacing = 4 };
        _paperPanel.Children.Add(new TextBlock { Text = "Papirformat" });
        _paperPanel.Children.Add(_paper);
        root.Children.Add(_paperPanel);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8 };
        var cancel = new Button { Content = "Annullér", IsCancel = true };
        cancel.Click += (_, _) => Close(null);
        _submit = new Button { Name = "Submit", IsDefault = true };
        _submit.Click += (_, _) => Close(new GraphExportSelection((GraphExportFormat)_format.SelectedIndex,
            _viewport.IsChecked == true, _labels.IsChecked == true,
            _resolution.SelectedIndex switch { 0 => 2048, 2 => 8192, _ => 4096 }, _paper.SelectedIndex));
        actions.Children.Add(cancel);
        actions.Children.Add(_submit);
        root.Children.Add(actions);
        Content = root;
        _format.SelectionChanged += (_, _) => UpdateFormat();
        UpdateFormat();
    }

    private void UpdateFormat()
    {
        var format = (GraphExportFormat)_format.SelectedIndex;
        _resolutionPanel.IsVisible = format == GraphExportFormat.Png;
        _paperPanel.IsVisible = format is GraphExportFormat.Pdf or GraphExportFormat.Print;
        _submit.Content = format == GraphExportFormat.Print ? "Udskriv…" : "Eksportér…";
    }
}
