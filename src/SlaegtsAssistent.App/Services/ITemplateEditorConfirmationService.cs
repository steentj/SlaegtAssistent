using System.Threading.Tasks;

namespace SlaegtsAssistent.App.Services;

public interface ITemplateEditorConfirmationService
{
    Task<bool> ConfirmResetAsync();
}
