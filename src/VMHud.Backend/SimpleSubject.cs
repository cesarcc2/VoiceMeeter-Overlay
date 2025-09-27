using System;
using System.Collections.Generic;

namespace VMHud.Backend;

internal sealed class SimpleSubject<T> : IObservable<T>
{
    private readonly object _gate = new();
    private readonly List<IObserver<T>> _observers = new();

    public IDisposable Subscribe(IObserver<T> observer)
    {
        lock (_gate) _observers.Add(observer);
        return new Unsubscriber(_observers, _gate, observer);
    }

    public void OnNext(T value)
    {
        IObserver<T>[] snapshot;
        lock (_gate) snapshot = _observers.ToArray();
        foreach (var o in snapshot) o.OnNext(value);
    }

    private sealed class Unsubscriber : IDisposable
    {
        private readonly List<IObserver<T>> _observers;
        private readonly object _gate;
        private readonly IObserver<T> _observer;
        public Unsubscriber(List<IObserver<T>> observers, object gate, IObserver<T> observer)
        { _observers = observers; _gate = gate; _observer = observer; }
        public void Dispose()
        { lock (_gate) _observers.Remove(_observer); }
    }
}

