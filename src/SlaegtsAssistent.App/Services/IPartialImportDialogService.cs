using System.Threading.Tasks;
using SlaegtsAssistent.Core.Domain;

namespace SlaegtsAssistent.App.Services;

public interface IPartialImportDialogService
{
    Task<bool> ConfirmAsync(GedcomImportReport report);
}
