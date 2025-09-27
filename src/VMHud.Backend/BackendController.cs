using VMHud.Core.Contracts;

namespace VMHud.Backend;

public sealed class BackendController : IBackendController
{
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        // Placeholder: initialize polling and connect to Voicemeeter
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        // Placeholder: stop polling and disconnect
        return Task.CompletedTask;
    }
}

