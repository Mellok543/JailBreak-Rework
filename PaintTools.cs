using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace JailBreak;

public class PaintTools : IFeature
{
    private static List<PaintInfo> _paints = new();


    public PaintTools(JailBreak jailBreak)
    {
        jailBreak.AddTimer(0.25f, () =>
        {
            for (var i = 0; i < _paints.Count; i++)
            {
                var paint = _paints[i];
                if ((DateTime.Now - paint.SpawnTime).TotalSeconds > paint.Duration)
                {
                    if (paint.Beam.IsValid)
                        paint.Beam.Remove();
                    _paints.Remove(paint);
                    continue;
                }

                break;
            }
        }, TimerFlags.REPEAT);
    }

    public static CBeam Draw(float width, Color color, float duration = 0f)
    {
        var beam = Utilities.CreateEntityByName<CBeam>("beam")!;

        beam.Width = width;
        beam.Render = color;

        beam.DispatchSpawn();

        if (duration == 0f)
            return beam;

        _paints.Add(new PaintInfo
        {
            Beam = beam,
            Duration = duration
        });
        return beam;
    }

    public static void UpdateBeamPosition(CBeam beam, Vector start, Vector end)
    {
        beam.Teleport(start, new QAngle(0, 0, 0),
            new Vector(0, 0, 0));

        beam.EndPos.X = end.X;
        beam.EndPos.Y = end.Y;
        beam.EndPos.Z = end.Z;

        Utilities.SetStateChanged(beam, "CBeam", "m_vecEndPos");
    }

    public static void MakeCircle(List<CBeam> beams, Vector center, int radius)
    {
        var points = GetCirclePoints(center, beams.Count-1, radius);

        for (var i = 0; i < points.Count-1; i++)
        {
            var beam = beams[i];

            UpdateBeamPosition(beam, points[i], points[i + 1]);
        }
        UpdateBeamPosition(beams[^1], points[^1], points[0]);
        
    }

    public static List<CBeam> CreatePoints(int count, float width, Color color, float duration = 0f)
    {
        var beams = new List<CBeam>();

        for (var i = 0; i < count; i++)
        {
            var beam = Draw(width, color, duration);
            beams.Add(beam);
        }

        return beams;
    }

    public static List<Vector> GetCirclePoints(Vector center, int pointsCount, float radius)
    {
        var positions = new List<Vector>();
        var numPoints = pointsCount;

        for (var i = 0; i < numPoints; i++)
        {
            var angle = (float)i / numPoints * 2 * MathF.PI;

            var x = center.X + radius * MathF.Cos(angle);
            var y = center.Y + radius * MathF.Sin(angle);
            var z = center.Z;

            var currentPoint = new Vector(x, y, z);

            positions.Add(currentPoint);
        }

        return positions;
    }

    public static CPointWorldText SpawnText(string text, Color color, Vector center, int size)
    {
        var pointWorldText = Utilities.CreateEntityByName<CPointWorldText>("point_worldtext");
        
        var offset = Schema.GetSchemaOffset("CPointWorldText", "m_messageText");
        var bytes = Encoding.ASCII.GetBytes(text);
        for (var i = 0; i < bytes.Length; i++)
        {
            Marshal.WriteByte(pointWorldText.Handle + offset + i, bytes[i]);
        }

        pointWorldText.Enabled = true;
        pointWorldText.Color = color;
        pointWorldText.FontSize = size;
        pointWorldText.FontName = "Arial";
        pointWorldText.Fullbright = true;
        pointWorldText.WorldUnitsPerPx = 0.1f;
        pointWorldText.DepthOffset = 0.0f;
        pointWorldText.JustifyHorizontal = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_CENTER;
        pointWorldText.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_CENTER;
        pointWorldText.ReorientMode = PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_NONE;

        pointWorldText.Teleport(center, new QAngle(), new Vector());
        pointWorldText.DispatchSpawn();

        return pointWorldText;
    }
}

public class PaintInfo
{
    public required CBeam Beam { get; set; }
    public DateTime SpawnTime { get; } = DateTime.Now;
    public float Duration { get; set; }
}