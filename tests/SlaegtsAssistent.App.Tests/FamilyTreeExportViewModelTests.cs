using FluentAssertions;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.Core.FamilyTrees;
using SlaegtsAssistent.Core.Gedcom;
using SlaegtsAssistent.App.Views;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace SlaegtsAssistent.App.Tests;

public class FamilyTreeExportViewModelTests
{
    [Fact]
    public Task Dialog_BinderGenerationstalOgRetning() => HeadlessTestApplication.RunAsync(() =>
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".ged");
        File.WriteAllText(path, """
            0 HEAD
            0 @I1@ INDI
            1 NAME Rod /Test/
            0 TRLR
            """);
        var model = new FamilyTreeExportViewModel(new FamilyTreeIndex(new GedcomLoader().Load(path)), "@I1@", 0);
        var window = new FamilyTreeExportWindow(model, false);
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var input = window.GetVisualDescendants().OfType<NumericUpDown>().First();
            input.Value = 2;
            Dispatcher.UIThread.RunJobs();
            model.AncestorDepth.Should().Be(2);
            model.Summary.Should().Contain("3 af 6 generationer");
        }
        finally
        {
            window.Close();
            File.Delete(path);
        }
        return Task.CompletedTask;
    });

    [Fact]
    public void DialogDefaultsAndValidationFollowRootAndDirection()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".ged");
        File.WriteAllText(path, """
            0 HEAD
            0 @I1@ INDI
            1 NAME Rod /Test/
            0 @I2@ INDI
            1 NAME Far /Test/
            0 @I3@ INDI
            1 NAME Barn /Test/
            0 @F1@ FAM
            1 HUSB @I2@
            1 CHIL @I1@
            0 @F2@ FAM
            1 HUSB @I1@
            1 CHIL @I3@
            0 TRLR
            """);
        try
        {
            var index = new FamilyTreeIndex(new GedcomLoader().Load(path));
            var vm = new FamilyTreeExportViewModel(index, "@I1@", 0);
            vm.CanStart.Should().BeTrue();
            vm.AncestorDepth.Should().Be(1);
            vm.DescendantDepth.Should().Be(1);
            vm.Summary.Should().Contain("3 af 6 generationer");
            vm.AncestorNumber = "2";
            vm.Plan!.Root.Id.Should().Be("@I2@");
            vm.AncestorDepth.Should().Be(0);
            vm.DescendantDepth.Should().Be(2);
            vm.AncestorNumber = "3";
            vm.CanStart.Should().BeFalse();
            vm.Status.Should().Contain("Anenummeret");
            vm.AncestorNumber = "1";
            vm.DirectionIndex = 1;
            vm.ShowDescendants.Should().BeFalse();
            vm.DescendantDepth.Should().Be(0);
            vm.AncestorDepth = 5;
            vm.DescendantDepth = 0;
            vm.CanStart.Should().BeTrue();
            vm.DirectionIndex = 0;
            vm.AncestorDepth = 3;
            vm.DescendantDepth = 3;
            vm.CanStart.Should().BeFalse();
        }
        finally { File.Delete(path); }
    }
}
