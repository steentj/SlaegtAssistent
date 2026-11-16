using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;

namespace SlaegtsAssistent.App.Tests;

public sealed class MediaPickerViewModelTests
{
    [Fact]
    public async Task HoegstToFilerUndersoegesSamtidigt()
    {
        var inspector = new LimitedInspection();
        var items = Enumerable.Range(0, 8).Select(i => new MediaCatalogItem(i.ToString(), "@I1@", null, null, $"{i}.png")).ToArray();
        using var model = new MediaPickerViewModel(items, "@I1@", inspector);
        var running = model.RefreshAsync();
        await inspector.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        inspector.Maximum.Should().Be(2);
        inspector.Release.TrySetResult();
        await running;
        inspector.Maximum.Should().Be(2);
    }

    private sealed class LimitedInspection : IMediaInspectionService
    {
        private int _active;
        private int _maximum;
        public int Maximum => _maximum;
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<MediaInspection> InspectAsync(MediaCatalogItem item, CancellationToken cancellationToken)
        {
            var active = Interlocked.Increment(ref _active);
            int previous;
            do { previous = _maximum; if (active <= previous) break; } while (Interlocked.CompareExchange(ref _maximum, active, previous) != previous);
            if (active == 2) Started.TrySetResult();
            await Release.Task;
            Interlocked.Decrement(ref _active);
            return new(true, "Klar", item.Reference);
        }
    }

    [Fact]
    public async Task EnUlæseligFilForhindrerIkkeAndreResultater()
    {
        MediaCatalogItem[] items = [new("a", "@I1@", null, "Anna", "a.jpg"), new("b", "@I1@", null, "Bo", "b.jpg")];
        using var model = new MediaPickerViewModel(items, "@I1@", new FailingInspection());
        await model.RefreshAsync();
        model.Items.Should().HaveCount(2);
        model.Items.Single(item => item.Item.Key == "a").Inspection.CanSelect.Should().BeFalse();
        model.Items.Single(item => item.Item.Key == "b").Inspection.CanSelect.Should().BeTrue();
    }

    private sealed class FailingInspection : IMediaInspectionService
    {
        public Task<MediaInspection> InspectAsync(MediaCatalogItem item, CancellationToken cancellationToken) =>
            item.Key == "a" ? Task.FromException<MediaInspection>(new IOException("Adgang nægtet")) :
                Task.FromResult(new MediaInspection(true, "Klar", item.Reference));
    }

    [Fact]
    public async Task PersonfilterSoegningOgSiderViserHoegst60()
    {
        var items = Enumerable.Range(0, 75).Select(i => new MediaCatalogItem($"a{i}", "@I1@", null, $"Anna {i}", $"foto{i}.jpg"))
            .Append(new MediaCatalogItem("b", "@I2@", null, "Bo", "bo.jpg")).ToArray();
        using var model = new MediaPickerViewModel(items, "@I1@", new Inspection());
        await model.RefreshAsync();
        model.Items.Should().HaveCount(60);
        model.TotalPages.Should().Be(2);
        model.Page = 1;
        await model.RefreshAsync();
        model.Items.Should().HaveCount(15);
        model.ShowAll = true;
        model.SearchText = "bo.jpg";
        await model.RefreshAsync();
        model.Items.Should().ContainSingle().Which.Item.OwnerId.Should().Be("@I2@");
    }

    [Fact]
    public async Task BekraeftelseKraeverValideretFilOgBevarerAlternativTekst()
    {
        MediaCatalogItem[] items = [new("a", "@I1@", null, "Foto", "foto.jpg"), new("b", "@I1@", null, "Mangler", "b.jpg")];
        using var model = new MediaPickerViewModel(items, "@I1@", new Inspection());
        await model.RefreshAsync();
        model.SelectedItem = model.Items[1];
        model.CreateResult().Should().BeNull();
        model.SelectedItem = model.Items[0];
        model.AlternativeText = "Et portræt";
        model.Caption = "Omkring 1900";
        var result = model.CreateResult();
        result!.Reference.Should().Be("foto.jpg");
        result.AlternativeText.Should().Be("Et portræt");
        result.Caption.Should().Be("Omkring 1900");
    }

    [Fact]
    public async Task ForaeldetBaggrundsresultatKanIkkeErstatteNytFilter()
    {
        var inspector = new DelayedInspection();
        MediaCatalogItem[] items = [new("a", "@I1@", null, "Anna", "a.jpg"), new("b", "@I2@", null, "Bo", "b.jpg")];
        using var model = new MediaPickerViewModel(items, "@I1@", inspector);
        var previous = model.RefreshAsync();
        await inspector.Started.Task;
        model.ShowAll = true;
        model.SearchText = "Bo";
        var current = model.RefreshAsync();
        inspector.Complete.TrySetResult();
        await Task.WhenAll(previous, current);
        model.Items.Should().ContainSingle().Which.Item.Key.Should().Be("b");
    }

    private sealed class Inspection : IMediaInspectionService
    {
        public Task<MediaInspection> InspectAsync(MediaCatalogItem item, CancellationToken cancellationToken) =>
            Task.FromResult(item.Key == "b" ? new MediaInspection(false, "Filen mangler", null) : new MediaInspection(true, "Klar", item.Reference));
    }

    private sealed class DelayedInspection : IMediaInspectionService
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Complete { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<MediaInspection> InspectAsync(MediaCatalogItem item, CancellationToken cancellationToken)
        {
            Started.TrySetResult();
            await Complete.Task;
            return new MediaInspection(true, "Klar", item.Reference);
        }
    }
}
