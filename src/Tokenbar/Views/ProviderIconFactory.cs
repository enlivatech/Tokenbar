using System.Globalization;
using System.Xml.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Path = Microsoft.UI.Xaml.Shapes.Path;

namespace Tokenbar.Views;

/// <summary>
/// Turns the monochrome provider SVGs into vector XAML drawn with a theme brush, so icons follow
/// light/dark theme and the selected-tab colour like Fluent symbol icons do.
/// </summary>
internal static class ProviderIconFactory
{
    private sealed record IconPath(string Data, bool EvenOdd, double? StrokeWidth);
    private sealed record IconDef(double Width, double Height, IReadOnlyList<IconPath> Paths);

    private static readonly Dictionary<string, IconDef?> Cache = new();

    public static FrameworkElement Create(string iconName, double size, Brush foreground)
    {
        var def = Load(iconName);
        if (def is null) return new FontIcon { Glyph = "\uE9CE", FontSize = size, Foreground = foreground };

        var canvas = new Canvas { Width = def.Width, Height = def.Height };
        foreach (var p in def.Paths)
        {
            var path = new Path { Data = (Geometry)XamlBindingHelper.ConvertValue(typeof(Geometry), (p.EvenOdd ? "F0 " : "F1 ") + p.Data) };
            if (p.StrokeWidth is double sw)
            {
                path.StrokeThickness = sw;
                path.Stroke = foreground;
            }
            else
            {
                path.Fill = foreground;
            }
            canvas.Children.Add(path);
        }
        return new Viewbox { Width = size, Height = size, Child = canvas };
    }

    private static IconDef? Load(string name)
    {
        if (Cache.TryGetValue(name, out var cached)) return cached;
        IconDef? def = null;
        var file = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "ProviderIcons", name + ".svg");
        if (File.Exists(file))
        {
            var svg = XDocument.Load(file).Root!;
            var vb = ((string?)svg.Attribute("viewBox") ?? "0 0 100 100")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(v => double.Parse(v, CultureInfo.InvariantCulture)).ToArray();
            var paths = svg.Descendants()
                .Where(e => e.Name.LocalName == "path" && e.Attribute("d") is not null)
                .Select(e => new IconPath(
                    (string)e.Attribute("d")!,
                    (string?)e.Attribute("fill-rule") == "evenodd",
                    double.TryParse((string?)e.Attribute("stroke-width"), CultureInfo.InvariantCulture, out var sw) ? sw : null))
                .ToList();
            def = new IconDef(vb[2], vb[3], paths);
        }
        Cache[name] = def;
        return def;
    }
}
