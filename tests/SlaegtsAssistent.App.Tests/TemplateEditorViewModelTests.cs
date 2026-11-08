using FluentAssertions;
using SlaegtsAssistent.App.Services;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.Core.Biography;

namespace SlaegtsAssistent.App.Tests;

public sealed class TemplateEditorViewModelTests
{
    [Fact]
    public void ContextualHelp_ShouldFollowTheSelectedBlock()
    {
        var viewModel = new TemplateEditorViewModel(BiographyTemplateEditorModel.CreateDefault());
        foreach (var block in viewModel.Blocks)
        {
            viewModel.SelectedBlock = block;
            var descriptor = BiographyTemplateEditorCatalog.GetBlock(block.Type);
            viewModel.SelectedBlockHelp.Should().Be(descriptor.Help);
            viewModel.SelectedBlockExample.Should().Be("Eksempel: " + descriptor.Example);
        }
        viewModel.SelectedBlock = null;
        viewModel.SelectedBlockHelp.Should().Contain("Vælg eller tilføj");
        viewModel.SelectedBlockExample.Should().BeEmpty();
        viewModel.StopPreview();
    }

    [Fact]
    public void TogglingFields_ShouldKeepVisualOrderAndRenderInThatOrder()
    {
        var viewModel = new TemplateEditorViewModel(BiographyTemplateEditorModel.CreateDefault());
        viewModel.SelectedBlock = viewModel.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Facts);
        var fields = viewModel.Fields.ToArray();
        var birth = fields.Single(field => field.Field == BiographyTemplateField.Birth);
        viewModel.ToggleFieldCommand.Execute(birth);
        viewModel.ToggleFieldCommand.Execute(birth);
        viewModel.Fields.Should().Equal(fields);
        viewModel.CreateModel().Blocks.Single(block => block.Type == BiographyTemplateBlockType.Facts).Fields
            .Should().Equal(viewModel.Fields.Where(field => field.IsSelected).Select(field => field.Field));
        viewModel.StopPreview();
    }

    [Fact]
    public void EditingHeading_ShouldKeepSelectedBlockAndFieldInstances()
    {
        var viewModel = new TemplateEditorViewModel(BiographyTemplateEditorModel.CreateDefault());
        viewModel.SelectedBlock = viewModel.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Facts);
        var selectedBlock = viewModel.SelectedBlock;
        var fields = viewModel.Fields.ToArray();
        var collectionChanges = 0;
        viewModel.Blocks.CollectionChanged += (_, _) => collectionChanges++;

        foreach (var heading in new[] { "M", "Mi", "Min", "Mine fakta" })
        {
            viewModel.SelectedBlockHeading = heading;
            viewModel.SelectedBlock.Should().BeSameAs(selectedBlock);
            viewModel.Fields.Should().Equal(fields);
        }

        collectionChanges.Should().Be(0);
        viewModel.CreateModel().Blocks.Single(block => block.Type == BiographyTemplateBlockType.Facts)
            .Heading.Should().Be("Mine fakta");
        viewModel.UndoCommand.Execute(null);
        viewModel.SelectedBlockHeading.Should().Be("Min");
    }

    [Fact]
    public void Constructor_ShouldExposeDanishTaskOrientedBlocksWithoutSyntax()
    {
        var viewModel = new TemplateEditorViewModel(BiographyTemplateEditorModel.CreateDefault());

        viewModel.Blocks.Select(block => block.Label).Should().Equal(
            "Titel", "Fakta", "Hændelser", "Folketællinger", "Kilder", "Medier", "Afsender", "Biografi");
        viewModel.SelectedBlock.Should().Be(viewModel.Blocks[0]);
        viewModel.CanEditHeading.Should().BeFalse();
        viewModel.HasFields.Should().BeFalse();
        var visibleText = string.Join(" ", viewModel.Blocks.SelectMany(block => new[] { block.Label, block.Description }));
        visibleText.Should().NotContain("{{");
        visibleText.Should().NotContain("person.");

        viewModel.SelectedBlock = viewModel.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Facts);
        viewModel.CanEditHeading.Should().BeTrue();
        viewModel.HasFields.Should().BeTrue();
    }

    [Fact]
    public void BlockCommands_ShouldHideMoveRemoveAndAddABlock()
    {
        var viewModel = new TemplateEditorViewModel(BiographyTemplateEditorModel.CreateDefault());
        viewModel.SelectedBlock = viewModel.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Sources);
        var originalIndex = viewModel.Blocks.IndexOf(viewModel.SelectedBlock);

        viewModel.SelectedBlockIsVisible = false;
        viewModel.MoveSelectedBlockUpCommand.Execute(null);
        viewModel.RemoveSelectedBlockCommand.Execute(null);

        viewModel.Blocks.Should().NotContain(block => block.Type == BiographyTemplateBlockType.Sources);
        viewModel.AvailableBlocks.Should().Contain(option => option.Type == BiographyTemplateBlockType.Sources);

        viewModel.SelectedAvailableBlock = viewModel.AvailableBlocks.Single(option => option.Type == BiographyTemplateBlockType.Sources);
        viewModel.AddBlockCommand.Execute(null);

        viewModel.Blocks.Should().ContainSingle(block => block.Type == BiographyTemplateBlockType.Sources);
        viewModel.Blocks.IndexOf(viewModel.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Events))
            .Should().BeLessThan(originalIndex);
    }

    [Fact]
    public void FieldCommands_ShouldSelectAndOrderFactsWithoutTechnicalNames()
    {
        var viewModel = new TemplateEditorViewModel(BiographyTemplateEditorModel.CreateDefault());
        viewModel.SelectedBlock = viewModel.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Facts);
        var birth = viewModel.Fields.Single(field => field.Field == BiographyTemplateField.Birth);
        var death = viewModel.Fields.Single(field => field.Field == BiographyTemplateField.Death);

        viewModel.ToggleFieldCommand.Execute(birth);
        viewModel.SelectedField = death;
        viewModel.MoveSelectedFieldDownCommand.Execute(null);

        viewModel.Fields.Single(field => field.Field == BiographyTemplateField.Birth).IsSelected.Should().BeFalse();
        viewModel.CreateModel().Blocks.Single(block => block.Type == BiographyTemplateBlockType.Facts).Fields
            .Should().NotContain(BiographyTemplateField.Birth);
        viewModel.Fields.Select(field => field.Label).Should().NotContain(label => label.Contains("person.", StringComparison.Ordinal));
    }

    [Fact]
    public void PresentationOptions_ShouldOnlyOfferValidChoicesForTheSelectedBlock()
    {
        var viewModel = new TemplateEditorViewModel(BiographyTemplateEditorModel.CreateDefault());
        viewModel.SelectedBlock = viewModel.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Events);

        viewModel.AvailablePresentations.Select(option => option.Presentation).Should().Equal(
            BiographyTemplatePresentation.List,
            BiographyTemplatePresentation.Table);
        viewModel.SelectedPresentation = viewModel.AvailablePresentations.Single(
            option => option.Presentation == BiographyTemplatePresentation.Table);

        viewModel.CreateModel().Blocks.Single(block => block.Type == BiographyTemplateBlockType.Events).Presentation
            .Should().Be(BiographyTemplatePresentation.Table);

        viewModel.SelectedBlock = viewModel.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Title);
        viewModel.AvailablePresentations.Should().ContainSingle()
            .Which.Presentation.Should().Be(BiographyTemplatePresentation.Standard);
    }

    [Fact]
    public void UndoAndRedo_ShouldCoverModelChanges()
    {
        var viewModel = new TemplateEditorViewModel(BiographyTemplateEditorModel.CreateDefault());
        viewModel.SelectedBlock = viewModel.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Media);

        viewModel.SelectedBlockIsVisible = false;
        viewModel.SelectedBlockHeading = "Billeder";

        viewModel.UndoCommand.Execute(null);
        viewModel.SelectedBlockHeading.Should().Be("Medier");
        viewModel.SelectedBlockIsVisible.Should().BeFalse();
        viewModel.UndoCommand.Execute(null);
        viewModel.SelectedBlockIsVisible.Should().BeTrue();

        viewModel.RedoCommand.Execute(null);
        viewModel.SelectedBlockIsVisible.Should().BeFalse();
        viewModel.RedoCommand.Execute(null);
        viewModel.SelectedBlockHeading.Should().Be("Billeder");
    }

    [Fact]
    public async Task Reset_ShouldRequireConfirmationAndSupportCancellation()
    {
        var confirmation = new StubTemplateEditorConfirmationService(false, true);
        var model = BiographyTemplateEditorModel.CreateDefault();
        var viewModel = new TemplateEditorViewModel(model, confirmation);
        viewModel.SelectedBlock = viewModel.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Media);
        viewModel.SelectedBlockIsVisible = false;

        await viewModel.ResetCommand.ExecuteAsync(null);
        viewModel.SelectedBlockIsVisible.Should().BeFalse();

        await viewModel.ResetCommand.ExecuteAsync(null);
        viewModel.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Media).IsVisible.Should().BeTrue();
        confirmation.CallCount.Should().Be(2);
    }

    [Fact]
    public void SaveAndCancel_ShouldReturnModelOrNullWithoutMutatingTheInput()
    {
        var input = BiographyTemplateEditorModel.CreateDefault();
        var viewModel = new TemplateEditorViewModel(input);
        BiographyTemplateEditorModel? result = null;
        var closeCount = 0;
        viewModel.CloseRequested += (_, model) =>
        {
            closeCount++;
            result = model;
        };
        viewModel.SelectedBlock = viewModel.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Media);
        viewModel.SelectedBlockIsVisible = false;

        viewModel.SaveCommand.Execute(null);

        result!.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Media).IsVisible.Should().BeFalse();
        input.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Media).IsVisible.Should().BeTrue();

        viewModel.CancelCommand.Execute(null);
        closeCount.Should().Be(2);
        result.Should().BeNull();
    }

    [Fact]
    public void Save_ShouldKeepTheEditorOpenAndShowDanishValidationError()
    {
        var viewModel = new TemplateEditorViewModel(BiographyTemplateEditorModel.CreateDefault());
        var wasClosed = false;
        viewModel.CloseRequested += (_, _) => wasClosed = true;
        viewModel.SelectedBlock = viewModel.Blocks.Single(block => block.Type == BiographyTemplateBlockType.Facts);
        viewModel.SelectedBlockHeading = string.Empty;

        viewModel.SaveCommand.Execute(null);

        wasClosed.Should().BeFalse();
        viewModel.ErrorMessage.Should().Contain("mangler en overskrift");
    }

    private sealed class StubTemplateEditorConfirmationService(params bool[] answers)
        : ITemplateEditorConfirmationService
    {
        private readonly Queue<bool> _answers = new(answers);

        public int CallCount { get; private set; }

        public Task<bool> ConfirmResetAsync()
        {
            CallCount++;
            return Task.FromResult(_answers.Dequeue());
        }
    }
}
