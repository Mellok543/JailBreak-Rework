using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace JailBreak.LRGames;

public class Link : IDisposable
{
    public CCSPlayerController Inmate { get; set; }
    public CCSPlayerController Guardian { get; set; }

    public JailBreak JailBreak;

    private CBeam _beam;
    private Listeners.OnTick _onTick;

    public void Start()
    {
        _beam = PaintTools.Draw(1.5f, Color.Red);

        _onTick = Update;
        JailBreak.RegisterListener(_onTick);
    }

    public void Update()
    {
        if (!Guardian.IsLegal() || !Guardian.PawnIsAlive || !Inmate.IsLegal() || !Inmate.PawnIsAlive)
        {
            return;
        }

        var guardianPosition = Guardian.PlayerPawn.Value!.AbsOrigin;
        var inmatePosition = Inmate.PlayerPawn.Value!.AbsOrigin;


        _beam.Teleport(guardianPosition!, new QAngle(), new Vector());

        _beam.EndPos.X = inmatePosition!.X;
        _beam.EndPos.Y = inmatePosition.Y;
        _beam.EndPos.Z = inmatePosition.Z + 10;
        Utilities.SetStateChanged(_beam, "CBeam", "m_vecEndPos");
    }

    public void Dispose()
    {
        if (_beam.IsValid)
            _beam.Remove();
        JailBreak.RemoveListener("OnTick", _onTick);
    }
}