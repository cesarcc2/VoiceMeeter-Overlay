namespace VMHud.Core.Contracts;

public interface IBackendController
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync();
}
