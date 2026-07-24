using System.Threading.Tasks;

namespace SlaegtsAssistent.App.Services;

public interface IUnsavedChangesDialogService
{
    Task<UnsavedChangesDecision> AskAsync();
}
