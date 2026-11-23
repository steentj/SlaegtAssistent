using FluentAssertions;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.Core.FamilyTrees;
using SlaegtsAssistent.Core.Gedcom;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using SlaegtsAssistent.App.Views;

namespace SlaegtsAssistent.App.Tests;

public class FamilyTreeViewModelTests
{
    [Fact]
    public Task TreeView_BindsExpansionToNodes() => HeadlessTestApplication.RunAsync(() =>
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".ged");
        File.WriteAllText(path, """
            0 HEAD
            0 @I1@ INDI
            1 NAME Barn /Test/
            0 @I2@ INDI
            1 NAME Far /Test/
            0 @I3@ INDI
            1 NAME Bedstefar /Test/
            0 @F1@ FAM
            1 HUSB @I2@
            1 CHIL @I1@
            0 @F2@ FAM
            1 HUSB @I3@
            1 CHIL @I2@
            0 TRLR
            """);
        var vm = new MainWindowViewModel();
        var window = new MainWindow { DataContext = vm };
        var webView = window.FindControl<NativeWebView>("PreviewWebView")!;
        ((Panel)webView.Parent!).Children.Remove(webView);
        try
        {
            vm.DocumentTabIndex = 0;
            window.Show();
            vm.FamilyTreeView.SetTree(new FamilyTreeIndex(new GedcomLoader().Load(path)), "@I1@");
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            var father = vm.FamilyTreeView.Roots[0].Children[0].Children[0];
            var item = window.GetVisualDescendants().OfType<TreeViewItem>()
                .Single(control => ReferenceEquals(control.DataContext, father));
            item.IsExpanded.Should().BeFalse();
            item.IsExpanded = true;
            Dispatcher.UIThread.RunJobs();
            father.IsExpanded.Should().BeTrue();
            father.Children[0].Title.Should().Contain("Anenummer: 4");
        }
        finally
        {
            window.Close();
            File.Delete(path);
        }
        return Task.CompletedTask;
    });

    [Fact]
    public void Tree_StartsAtTwoLevelsAndPreservesExpandedBranchesWhenDirectionChanges()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".ged");
        File.WriteAllText(path, """
            0 HEAD
            0 @I1@ INDI
            1 NAME Rod /Person/
            0 @I2@ INDI
            1 NAME Far /Person/
            0 @I3@ INDI
            1 NAME Bedstefar /Person/
            0 @I4@ INDI
            1 NAME Barn /Person/
            0 @F1@ FAM
            1 HUSB @I2@
            1 CHIL @I1@
            0 @F2@ FAM
            1 HUSB @I3@
            1 CHIL @I2@
            0 @F3@ FAM
            1 HUSB @I1@
            1 CHIL @I4@
            0 TRLR
            """);
        try
        {
            var index = new FamilyTreeIndex(new GedcomLoader().Load(path));
            var vm = new FamilyTreeViewModel(_ => { }, _ => { }, _ => { });
            vm.SetTree(index, "@I1@");
            var root = vm.Roots.Should().ContainSingle().Subject;
            root.IsExpanded.Should().BeTrue();
            root.Children.Select(node => node.Title).Should().Equal("Forfædre", "Efterkommere");
            var ancestorGroup = root.Children[0];
            ancestorGroup.IsExpanded.Should().BeTrue();
            var father = ancestorGroup.Children[0];
            father.Title.Should().Contain("Anenummer: 2");
            father.IsExpanded.Should().BeFalse();
            father.IsExpanded = true;
            father.Children[0].Title.Should().Contain("Anenummer: 4");
            vm.DirectionIndex = 2;
            root.Children.Should().ContainSingle().Which.Title.Should().Be("Efterkommere");
            vm.DirectionIndex = 0;
            root.Children[0].Children[0].IsExpanded.Should().BeTrue();
            vm.SetTree(new FamilyTreeIndex(new GedcomLoader().Load(path)), "@I1@");
            vm.Roots[0].Children[0].Children[0].IsExpanded.Should().BeTrue();
            vm.SetTree(index, "@I2@");
            vm.Roots[0].Children[0].Children[0].IsExpanded.Should().BeFalse();
        }
        finally { File.Delete(path); }
    }
}
