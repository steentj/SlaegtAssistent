using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using FluentAssertions;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.App.Views;

namespace SlaegtsAssistent.App.Tests;

public sealed class MainWindowMenuTests
{
    [Fact]
    public Task MenuenBrugerSammeKommandoerOgRedigererDetFokuseredeFelt() => HeadlessTestApplication.RunAsync(() =>
    {
        var model = new MainWindowViewModel();
        var appMenu = NativeMenu.GetMenu(Application.Current!);
        var window = new MainWindow();
        var webView = window.FindControl<NativeWebView>("PreviewWebView")!;
        ((Panel)webView.Parent!).Children.Remove(webView);
        try
        {
            window.DataContext = model;
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var search = window.GetLogicalDescendants().OfType<TextBox>()
                .Single(box => box.PlaceholderText == "Søg navn eller person-id");
            search.Text = "Anna";
            search.Focus();
            Dispatcher.UIThread.RunJobs();
            if (OperatingSystem.IsMacOS())
            {
                var menu = NativeMenu.GetMenu(window)!;
                var file = ((NativeMenuItem)menu.Items[0]).Menu!;
                ((NativeMenuItem)file.Items[0]).Command.Should().BeSameAs(model.SelectGedcomFileCommand);
                ((NativeMenuItem)file.Items[1]).Command.Should().BeSameAs(model.SaveAllCommand);
                var edit = ((NativeMenuItem)menu.Items[1]).Menu!;
                ((NativeMenuItem)edit.Items[5]).Command!.Execute(null);
                NativeMenu.GetMenu(Application.Current!).Should().BeSameAs(appMenu);
                var settings = appMenu!.Items.OfType<NativeMenuItem>()
                    .Single(item => item.Header == "Indstillinger…");
                settings.Gesture!.Key.Should().Be(Key.OemComma);
                settings.Command.Should().BeSameAs(model.OpenSettingsCommand);
            }
            else
            {
                var menu = window.FindControl<Menu>("WindowMenu")!;
                var file = (MenuItem)menu.Items[0]!;
                ((MenuItem)file.Items[0]!).Command.Should().BeSameAs(model.SelectGedcomFileCommand);
                ((MenuItem)file.Items[1]!).Command.Should().BeSameAs(model.SaveAllCommand);
                var edit = (MenuItem)menu.Items[1]!;
                ((MenuItem)edit.Items[5]!).Command!.Execute(null);
                window.KeyBindings.Should().Contain(binding => binding.Gesture!.Key == Key.O && binding.Gesture.KeyModifiers == KeyModifiers.Control);
            }
            Math.Abs(search.SelectionEnd - search.SelectionStart).Should().Be(4);
        }
        finally { window.Close(); }
        return Task.CompletedTask;
    });
}
