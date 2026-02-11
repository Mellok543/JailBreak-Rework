using System.Drawing;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace JailBreak.LRGames;

public class Circle : IDisposable
{
    public Vector Center { get; private set; }
    private List<CBeam> _beams;

    private int _radius;
    private float _width;
    private Color _color;

    private CPointWorldText? _text;

    public bool IsDrawn { get; private set; }

    public Circle(int radius, float width, Color color)
    {
        _radius = radius;
        _width = width;
        _color = color;

        _beams = PaintTools.CreatePoints(_radius / 2, _width, _color);
    }

    public void DrawText(string text, int size, Color color)
    {
        if (_text != null)
        {
            _text.Remove();
        }

        _text = PaintTools.SpawnText(text, color, Center, size);
    }

    private void RemoveBeams(List<CBeam> cBeams)
    {
        foreach (var cBeam in cBeams)
        {
            if (!cBeam.IsValid) continue;

            cBeam.Remove();
        }
    }

    public void Draw(Vector center)
    {
        Center = center;
        PaintTools.MakeCircle(_beams, center, _radius);

        IsDrawn = true;
    }

    public void Dispose()
    {
        RemoveBeams(_beams);
        _beams.Clear();

        if (_text != null && _text.IsValid)
            _text.Remove();
        
    }
}