using Avalonia.Controls;
using FluentAssertions;

namespace SlaegtsAssistent.App.Tests;

public sealed class MediaEditorAdapterTests
{
    [Fact]
    public async Task UdskiftningAfBufferKanFortrydesOgGentagesSomEnHandling()
    {
        await HeadlessTestApplication.RunAsync(() =>
        {
            var textBox = new TextBox { AcceptsReturn = true, Text = "Første tekst" };
            var window = new Window { Content = textBox };
            window.Show();
            textBox.Focus();
            textBox.SelectionStart = 0;
            textBox.SelectionEnd = textBox.Text!.Length;
            textBox.SelectedText = "Ny tekst med billede";
            textBox.Text.Should().Be("Ny tekst med billede");
            textBox.Undo();
            textBox.Text.Should().Be("Første tekst");
            textBox.Redo();
            textBox.Text.Should().Be("Ny tekst med billede");
            window.Close();
            return Task.CompletedTask;
        });
    }
}
