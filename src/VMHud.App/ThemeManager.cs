using System;
using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace VMHud.App;

public static class ThemeManager
{
    public sealed record ThemeDto(string OverlayBackground, string OverlayBorder, string ABusOn, string BBusOn, string OffTile, string HardwareName, string VirtualName);

    private static string ConfigPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VMHud", "theme.json");

    public static void LoadAndApply()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var dto = JsonSerializer.Deserialize<ThemeDto>(File.ReadAllText(ConfigPath));
                if (dto is not null)
                {
                    Apply(dto);
                    return;
                }
            }
        }
        catch { }
        // Apply defaults if missing
        Apply(DefaultDarkTranslucent());
    }

    public static void ApplyAndSave(ThemeDto theme)
    {
        Apply(theme);
        try
        {
            var dir = Path.GetDirectoryName(ConfigPath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(theme, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    public static ThemeDto DefaultDarkTranslucent() => new("#B0000000", "#20FFFFFF", "#3DD38E", "#499BF5", "#60AAAAAA", "#CCFFFFFF", "#CCFFFFFF");
    public static ThemeDto DarkOpaque() => new("#FF101010", "#40FFFFFF", "#22AA77", "#2277CC", "#50888888", "#FFFFFFFF", "#FFDDDDDD");
    public static ThemeDto LightTranslucent() => new("#C0FFFFFF", "#20000000", "#1E8F62", "#1E62AF", "#50888888", "#FF000000", "#FF222222");
    public static ThemeDto LightOpaque() => new("#FFFFFFFF", "#20000000", "#2DBB83", "#2D7BD9", "#33888888", "#FF000000", "#FF222222");
    public static ThemeDto HighContrast() => new("#FF000000", "#FFFFFFFF", "#FFFFD700", "#FF00FFFF", "#FF444444", "#FFFFFFFF", "#FFAAAAAA");

    private static void Apply(ThemeDto t)
    {
        var res = System.Windows.Application.Current.Resources;
        res["OverlayBackgroundBrush"] = new SolidColorBrush(Parse(t.OverlayBackground));
        res["OverlayBorderBrush"] = new SolidColorBrush(Parse(t.OverlayBorder));
        res["ABusOnBrush"] = new SolidColorBrush(Parse(t.ABusOn));
        res["BBusOnBrush"] = new SolidColorBrush(Parse(t.BBusOn));
        res["OffTileBrush"] = new SolidColorBrush(Parse(t.OffTile));
        res["HardwareNameBrush"] = new SolidColorBrush(Parse(t.HardwareName));
        res["VirtualNameBrush"] = new SolidColorBrush(Parse(t.VirtualName));
    }

    private static System.Windows.Media.Color Parse(string hex)
    {
        // Supports #AARRGGBB or #RRGGBB
        if (string.IsNullOrWhiteSpace(hex)) return System.Windows.Media.Colors.White;
        hex = hex.Trim();
        if (hex.StartsWith("#")) hex = hex[1..];
        if (hex.Length == 6)
        {
            var r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            var g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            var b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return System.Windows.Media.Color.FromArgb(0xFF, r, g, b);
        }
        if (hex.Length == 8)
        {
            var a = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            var r = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            var g = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            var b = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
            return System.Windows.Media.Color.FromArgb(a, r, g, b);
        }
        return System.Windows.Media.Colors.White;
    }

    // Expose current theme snapshot for UI use (optional)
    public static (string OverlayBackground, string OverlayBorder, string ABusOn, string BBusOn, string OffTile, string HardwareName, string VirtualName) GetCurrent()
    {
        var res = System.Windows.Application.Current.Resources;
        string Hex(System.Windows.Media.Brush b) => ((System.Windows.Media.SolidColorBrush)b).Color.ToString();
        return (
            Hex((System.Windows.Media.Brush)res["OverlayBackgroundBrush"]),
            Hex((System.Windows.Media.Brush)res["OverlayBorderBrush"]),
            Hex((System.Windows.Media.Brush)res["ABusOnBrush"]),
            Hex((System.Windows.Media.Brush)res["BBusOnBrush"]),
            Hex((System.Windows.Media.Brush)res["OffTileBrush"]),
            Hex((System.Windows.Media.Brush)res["HardwareNameBrush"]),
            Hex((System.Windows.Media.Brush)res["VirtualNameBrush"]) );
    }
}
