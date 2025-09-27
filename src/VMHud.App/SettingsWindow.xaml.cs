using System.Windows;
using Forms = System.Windows.Forms;

namespace VMHud.App;

public partial class SettingsWindow : Window
{
    private readonly MainWindow _main;
    private bool _initialized;
    public SettingsWindow(MainWindow owner)
    {
        InitializeComponent();
        Owner = owner;
        _main = owner;
        // Initialize values from current window
        OpacitySlider.Value = owner.Opacity;
        ScaleSlider.Value = 1.0; // updated below if available
        // Try to read current scale via RootBorder.LayoutTransform if set
        if (owner.FindName("RootBorder") is System.Windows.Controls.Border b && b.LayoutTransform is System.Windows.Media.ScaleTransform st)
        {
            ScaleSlider.Value = st.ScaleX;
        }
        if (owner.DataContext is VMHud.Core.ViewModels.MatrixViewModel mvm)
        {
            ShowVolumesCheck.IsChecked = mvm.ShowVolumes;
        }
        StartupCheck.IsChecked = StartupManager.IsEnabled();
        if (StartupManager.IsDevHost())
        {
            StartupCheck.IsEnabled = false;
            StartupCheck.ToolTip = "Disabled in development (dotnet run). Publish and run the EXE to enable.";
        }
        _initialized = true;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        _main.ApplyOpacity(OpacitySlider.Value);
        _main.ApplyScale(ScaleSlider.Value);
        var wantStartup = StartupCheck.IsChecked == true;
        var ok = wantStartup ? StartupManager.Enable() : StartupManager.Disable();
        if (!ok)
        {
            System.Windows.MessageBox.Show("Could not update 'Start with Windows'. Try running the app once as Administrator or install the published EXE.", "VMHud", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        DialogResult = true;
        Close();
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _main.Left = 16;
        _main.Top = 16;
        _main.PersistSettings();
    }

    private void ApplyPreset_Click(object sender, RoutedEventArgs e)
    {
        var idx = PresetCombo.SelectedIndex;
        var preset = idx switch
        {
            0 => ThemeManager.DefaultDarkTranslucent(),
            1 => ThemeManager.DarkOpaque(),
            2 => ThemeManager.LightTranslucent(),
            3 => ThemeManager.LightOpaque(),
            4 => ThemeManager.HighContrast(),
            _ => ThemeManager.DefaultDarkTranslucent(),
        };
        ThemeManager.ApplyAndSave(preset);
        RefreshPreviews();
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_initialized) return;
        _main.ApplyOpacity(e.NewValue);
    }

    private void ScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_initialized) return;
        _main.ApplyScale(e.NewValue);
    }

    

    private void StartupCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;
        var want = StartupCheck.IsChecked == true;
        var ok = want ? StartupManager.Enable() : StartupManager.Disable();
        if (!ok)
        {
            System.Windows.MessageBox.Show("Could not update 'Start with Windows'. Try running the app once as Administrator or install the published EXE.", "VMHud", MessageBoxButton.OK, MessageBoxImage.Warning);
            StartupCheck.IsChecked = !want; // revert
        }
    }

    private void ShowVolumesCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;
        if (_main.DataContext is VMHud.Core.ViewModels.MatrixViewModel mvm)
        {
            mvm.ShowVolumes = ShowVolumesCheck.IsChecked == true;
            _main.SetShowVolumes(mvm.ShowVolumes);
        }
    }

    private static string PickColor(string initialHex)
    {
        using var dlg = new Forms.ColorDialog();
        try
        {
            if (!string.IsNullOrWhiteSpace(initialHex))
            {
                var c = System.Drawing.ColorTranslator.FromHtml(initialHex);
                dlg.Color = c;
            }
        }
        catch { }
        dlg.FullOpen = true;
        return dlg.ShowDialog() == Forms.DialogResult.OK
            ? System.Drawing.ColorTranslator.ToHtml(dlg.Color).Replace("#", "#FF") // ensure opaque if 6-digit
            : initialHex;
    }

    private void RefreshPreviews()
    {
        // Updating Application resources already updates rectangles via DynamicResource
        // No-op here.
    }

    private void PickBg_Click(object sender, RoutedEventArgs e)
    {
        var cur = ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["OverlayBackgroundBrush"]).Color.ToString();
        var hex = PickColor(cur);
        ThemeManager.ApplyAndSave(new("#" + hex.TrimStart('#'),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["OverlayBorderBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["ABusOnBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["BBusOnBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["OffTileBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["HardwareNameBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["VirtualNameBrush"]).Color.ToString()));
    }

    private void PickBorder_Click(object sender, RoutedEventArgs e)
    {
        var hex = PickColor(((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["OverlayBorderBrush"]).Color.ToString());
        ThemeManager.ApplyAndSave(new(((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["OverlayBackgroundBrush"]).Color.ToString(),
                                      "#" + hex.TrimStart('#'),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["ABusOnBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["BBusOnBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["OffTileBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["HardwareNameBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["VirtualNameBrush"]).Color.ToString()));
    }

    private void PickAOn_Click(object sender, RoutedEventArgs e)
    {
        var hex = PickColor(((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["ABusOnBrush"]).Color.ToString());
        ThemeManager.ApplyAndSave(new(((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["OverlayBackgroundBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["OverlayBorderBrush"]).Color.ToString(),
                                      "#" + hex.TrimStart('#'),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["BBusOnBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["OffTileBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["HardwareNameBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["VirtualNameBrush"]).Color.ToString()));
    }

    private void PickBOn_Click(object sender, RoutedEventArgs e)
    {
        var hex = PickColor(((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["BBusOnBrush"]).Color.ToString());
        ThemeManager.ApplyAndSave(new(((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["OverlayBackgroundBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["OverlayBorderBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["ABusOnBrush"]).Color.ToString(),
                                      "#" + hex.TrimStart('#'),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["OffTileBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["HardwareNameBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["VirtualNameBrush"]).Color.ToString()));
    }

    private void PickOff_Click(object sender, RoutedEventArgs e)
    {
        var hex = PickColor(((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["OffTileBrush"]).Color.ToString());
        ThemeManager.ApplyAndSave(new(((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["OverlayBackgroundBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["OverlayBorderBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["ABusOnBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["BBusOnBrush"]).Color.ToString(),
                                      "#" + hex.TrimStart('#'),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["HardwareNameBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["VirtualNameBrush"]).Color.ToString()));
    }

    private void PickHwName_Click(object sender, RoutedEventArgs e)
    {
        var hex = PickColor(((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["HardwareNameBrush"]).Color.ToString());
        ThemeManager.ApplyAndSave(new(((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["OverlayBackgroundBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["OverlayBorderBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["ABusOnBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["BBusOnBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["OffTileBrush"]).Color.ToString(),
                                      "#" + hex.TrimStart('#'),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["VirtualNameBrush"]).Color.ToString()));
    }

    private void PickVirtName_Click(object sender, RoutedEventArgs e)
    {
        var hex = PickColor(((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["VirtualNameBrush"]).Color.ToString());
        ThemeManager.ApplyAndSave(new(((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["OverlayBackgroundBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["OverlayBorderBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["ABusOnBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["BBusOnBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["OffTileBrush"]).Color.ToString(),
                                      ((System.Windows.Media.SolidColorBrush)System.Windows.Application.Current.Resources["HardwareNameBrush"]).Color.ToString(),
                                      "#" + hex.TrimStart('#')));
    }
}
