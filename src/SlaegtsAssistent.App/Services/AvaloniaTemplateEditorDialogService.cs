using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using SlaegtsAssistent.App.ViewModels;
using SlaegtsAssistent.App.Views;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.App.Services;

public sealed class AvaloniaTemplateEditorDialogService(
    IClassicDesktopStyleApplicationLifetime applicationLifetime) : ITemplateEditorDialogService
{
    public async Task<BiographyTemplateEditorModel?> EditAsync(
        BiographyTemplateEditorModel model,
        IEnumerable<Person> previewPeople)
    {
        var owner = AvaloniaPickerWindowResolver.Resolve(applicationLifetime);
        if (owner is null)
        {
            return null;
        }

        var viewModel = new TemplateEditorViewModel(model,
            new AvaloniaTemplateEditorConfirmationService(applicationLifetime), previewPeople);
        return await new TemplateEditorWindow(viewModel).ShowDialog<BiographyTemplateEditorModel?>(owner);
    }
}
