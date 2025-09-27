using VMHud.Core.Contracts;
using VMHud.Core.Models;

namespace VMHud.Backend;

public sealed class MatrixStateProvider : IMatrixStateProvider
{
    private readonly EmptyObservable<MatrixState> _updates = new();

    public MatrixState GetSnapshot() => new();

    public IObservable<MatrixState> Updates => _updates;

    public bool IsConnected => false; // Placeholder
    public VMHud.Core.Models.BackendStatus Status => VMHud.Core.Models.BackendStatus.Disconnected;

    private sealed class EmptyObservable<T> : IObservable<T>
    {
        private sealed class NopDisposable : IDisposable { public void Dispose() { } }
        public IDisposable Subscribe(IObserver<T> observer) => new NopDisposable();
    }
}
