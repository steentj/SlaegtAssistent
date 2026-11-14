using System.Threading.Tasks;

namespace SlaegtsAssistent.App.Services;

public interface IVersionDialogService
{
    Task<string?> AskCommitMessageAsync(string suggestedMessage);
    Task<bool> ConfirmAsync(string title, string message, string confirmText);
}
