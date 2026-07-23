using Avalonia.Controls.ApplicationLifetimes;

namespace SlaegtsAssistent.App.Services;

public sealed class AvaloniaApplicationControlService : IApplicationControlService
{
    private readonly IClassicDesktopStyleApplicationLifetime _applicationLifetime;

    public AvaloniaApplicationControlService(IClassicDesktopStyleApplicationLifetime applicationLifetime)
    {
        _applicationLifetime = applicationLifetime;
    }

    public void Exit()
    {
        _applicationLifetime.Shutdown();
    }
}
