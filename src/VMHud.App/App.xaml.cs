using System;
using System.Windows;
using VMHud.Backend;
using VMHud.Core.ViewModels;
using VMHud.Core.Models;
using VMHud.Core.Contracts;
using VMHud.Core.Diagnostics;

namespace VMHud.App;

public partial class App : System.Windows.Application
{
    private IBackendController? _controller;
    private TrayIcon? _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Log.Init();
        AppDomain.CurrentDomain.UnhandledException += (_, args) => Log.Error("UnhandledException", args.ExceptionObject as Exception);
        this.DispatcherUnhandledException += (_, args) => { Log.Error("DispatcherUnhandledException", args.Exception); args.Handled = true; };

        // Apply theme before creating UI
        ThemeManager.LoadAndApply();

        // Initialize Voicemeeter backend (no simulator fallback)
        IMatrixStateProvider provider;
        var vmBackend = new VoicemeeterBackend();
        try { vmBackend.StartAsync(); }
        catch (DllNotFoundException ex)
        {
            Log.Error("Voicemeeter DLL not found.", ex);
            System.Windows.MessageBox.Show("VoicemeeterRemote DLL not found. Please install Voicemeeter and restart.", "VMHud", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }
        provider = vmBackend;
        _controller = vmBackend;

        // Create UI and bind to backend updates
        var viewModel = new MatrixViewModel();
        var win = new MainWindow();
        if (vmBackend is VMHud.Core.Contracts.IMatrixController mc)
        {
            win.MatrixController = mc;
        }
        win.DataContext = viewModel;

        // Subscribe to updates and marshal to UI thread
        provider.Updates.Subscribe(new ActionObserver<MatrixState>(state =>
        {
            if (win.Dispatcher.CheckAccess()) viewModel.Update(state);
            else win.Dispatcher.Invoke(() => viewModel.Update(state));
        }));

        Log.Info($"Backend connected: {provider.IsConnected}");

        // Poll backend status to update UI label
        var statusTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        statusTimer.Tick += (_, _) =>
        {
            if (win.DataContext is MatrixViewModel mvm)
                mvm.Status = provider.Status;
        };
        statusTimer.Start();

        win.Show();
        _tray = new TrayIcon(win);
        Log.Info("MainWindow shown");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _controller?.StopAsync();
        base.OnExit(e);
    }

    private sealed class ActionObserver<T> : IObserver<T>
    {
        private readonly Action<T> _onNext;
        public ActionObserver(Action<T> onNext) => _onNext = onNext;
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(T value) => _onNext(value);
    }
}
