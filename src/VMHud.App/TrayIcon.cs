using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using Forms = System.Windows.Forms;
using VMHud.Backend;

namespace VMHud.App;

public sealed class TrayIcon : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Window _window;
    private readonly Forms.ContextMenuStrip _menu;

    public TrayIcon(Window window)
    {
        _window = window;
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "VMHud",
            Visible = true
        };
        _menu = new Forms.ContextMenuStrip();
        _notifyIcon.ContextMenuStrip = _menu;
        _notifyIcon.DoubleClick += (_, __) => ToggleShow();
        _window.IsVisibleChanged += (_, __) => UpdateMenu();
        UpdateMenu();
    }

    private void ToggleShow()
    {
        if (_window.Visibility == Visibility.Visible)
        {
            _window.Hide();
        }
        else
        {
            _window.Show();
            _window.Activate();
        }
        UpdateMenu();
    }

    private void UpdateMenu()
    {
        _menu.Items.Clear();
        var toggle = new Forms.ToolStripMenuItem(_window.Visibility == Visibility.Visible ? "Hide" : "Show");
        toggle.Click += (_, __) => ToggleShow();
        var openVm = new Forms.ToolStripMenuItem("Open Voicemeeter");
        openVm.Click += (_, __) => TryOpenVoicemeeter();
        var showVolumes = new Forms.ToolStripMenuItem("Show Volume Sliders") { CheckOnClick = true };
        showVolumes.Checked = (_window as MainWindow)?.DataContext is VMHud.Core.ViewModels.MatrixViewModel mvm && mvm.ShowVolumes;
        showVolumes.CheckedChanged += (_, __) =>
        {
            if (_window is MainWindow mw && mw.DataContext is VMHud.Core.ViewModels.MatrixViewModel vm)
            {
                vm.ShowVolumes = showVolumes.Checked;
                mw.SetShowVolumes(vm.ShowVolumes);
            }
        };
        var openLogs = new Forms.ToolStripMenuItem("Open Logs Folder");
        openLogs.Click += (_, __) => OpenLogsFolder();
        var openSettings = new Forms.ToolStripMenuItem("Settings…");
        openSettings.Click += (_, __) => OpenSettingsWindow();
        var exit = new Forms.ToolStripMenuItem("Exit");
        exit.Click += (_, __) => System.Windows.Application.Current.Shutdown();
        _menu.Items.Add(toggle);
        _menu.Items.Add(openVm);
        _menu.Items.Add(showVolumes);
        _menu.Items.Add(openLogs);
        _menu.Items.Add(openSettings);
        _menu.Items.Add(new Forms.ToolStripSeparator());
        _menu.Items.Add(exit);
    }

    private static Icon LoadIcon()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "icon.ico");
            if (File.Exists(path)) return new Icon(path);
        }
        catch { }
        return SystemIcons.Application;
    }

    private static void TryOpenVoicemeeter()
    {
        try
        {
            VoicemeeterControl.Open();
        }
        catch (DllNotFoundException)
        {
            System.Windows.MessageBox.Show("VoicemeeterRemote DLL not found. Please install Voicemeeter.", "VMHud", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to open Voicemeeter: {ex.Message}", "VMHud", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Clicks always on – removed toggle functionality

    private static void OpenLogsFolder()
    {
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VMHud", "logs");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to open logs: {ex.Message}", "VMHud", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenSettingsWindow()
    {
        try
        {
            if (_window is MainWindow mw)
            {
                var win = new SettingsWindow(mw);
                win.ShowDialog();
                UpdateMenu();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to open settings: {ex.Message}", "VMHud", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menu.Dispose();
    }
}
