using VMHud.Core.Models;
namespace VMHud.Core.Contracts;

public interface IMatrixStateProvider
{
    MatrixState GetSnapshot();
    IObservable<MatrixState> Updates { get; }
    bool IsConnected { get; }
    BackendStatus Status { get; }
}
