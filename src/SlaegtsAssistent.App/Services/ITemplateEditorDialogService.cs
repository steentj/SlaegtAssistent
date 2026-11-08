using System.Collections.Generic;
using System.Threading.Tasks;
using SlaegtsAssistent.Core.Biography;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.App.Services;

public interface ITemplateEditorDialogService
{
    Task<BiographyTemplateEditorModel?> EditAsync(
        BiographyTemplateEditorModel model,
        IEnumerable<Person> previewPeople);
}
