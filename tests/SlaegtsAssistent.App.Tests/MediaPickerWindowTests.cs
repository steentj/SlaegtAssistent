using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.App.Views;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.Tests;

public sealed class MediaPickerWindowTests
{
    [Fact]
    public async Task DialogBekraefterKunValideretMedie()
    {
        await HeadlessTestApplication.RunAsync(async () =>
        {
            MediaCatalogItem[] items = [new("a", "@I1@", null, "Anna", "anna.png"), new("b", "@I1@", null, "Mangler", "b.png")];
            var inspection = new Inspection();
            var model = new MediaPickerViewModel(items, "@I1@", inspection);
            var owner = new Window();
            owner.Show();
            var picker = new MediaPickerWindow(model, inspection, new MediaThumbnailCache(), true);
            var resultTask = picker.ShowDialog<MediaSelectionResult?>(owner);
            await inspection.Loaded.Task.WaitAsync(TimeSpan.FromSeconds(5));
            for (var i = 0; i < 20 && picker.FindControl<ListBox>("Gallery")!.ItemCount != 2; i++) await Task.Delay(10);
            var gallery = picker.FindControl<ListBox>("Gallery")!;
            gallery.ItemCount.Should().Be(2);
            var confirm = picker.FindControl<Button>("ConfirmButton")!;
            gallery.SelectedIndex = 1;
            confirm.IsEnabled.Should().BeFalse();
            gallery.SelectedIndex = 0;
            confirm.IsEnabled.Should().BeTrue();
            picker.FindControl<TextBox>("AlternativeTextBox")!.Text = "Portræt";
            picker.FindControl<ComboBox>("ImageWidthBox")!.SelectedIndex = 1;
            confirm.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var result = await resultTask.WaitAsync(TimeSpan.FromSeconds(5));
            result!.Reference.Should().Be("anna.png");
            result.AlternativeText.Should().Be("Portræt");
            result.Width.Should().Be(BiographyImageWidth.HalfPage);
            owner.Close();
        });
    }

    private sealed class Inspection : IMediaInspectionService
    {
        private int _calls;
        public TaskCompletionSource Loaded { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<MediaInspection> InspectAsync(MediaCatalogItem item, CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _calls) >= 2) Loaded.TrySetResult();
            return Task.FromResult(item.Key == "a" ? new MediaInspection(true, "Klar", item.Reference) : new MediaInspection(false, "Filen mangler", null));
        }
    }
}
