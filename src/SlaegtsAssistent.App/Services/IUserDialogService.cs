using System.Threading.Tasks;

namespace SlaegtsAssistent.App.Services;

public interface IUserDialogService
{
    Task ShowInformationAsync(string title, string message);
}
