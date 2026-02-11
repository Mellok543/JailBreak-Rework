using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace JailbreakCommander;

// Оставлено в этом файле для совместимости структуры проекта.
// Ключевые названия переменных/методов сохранены по вашему шаблону.
public class PaintFunction
{
    private readonly JailBreak _jailBreak;
    private DateTime _lastDrawTime;
    private Vector? _lastPosition = null;

    private bool _valid;

    // Сохраняем исходные имена полей, но не используем Memory hooks,
    // чтобы исключить краши из-за сигнатур.
    public object? FirstPingCondition;
    public object? SecondPingCondition;
    public object? UserId2Event;

    public PaintFunction(JailBreak jailBreak)
    {
        _jailBreak = jailBreak;
        _valid = true;
    }

    public void LaserTick()
    {
        if (!_valid)
            return;

        if (_jailBreak.Commander?.CurrentCommander == null || !_jailBreak.Commander.CurrentCommander.IsValid)
            return;

        var commander = _jailBreak.Commander.CurrentCommander;
        if (commander == null || !commander.PawnIsAlive)
            return;

        if (!commander.Buttons.HasFlag(PlayerButtons.Use))
        {
            _lastPosition = null;
            return;
        }

        var pawn = commander.PlayerPawn.Value;
        var pingServices = pawn?.PingServices;
        if (pingServices == null)
            return;

        for (int i = 0; i < 5; i++)
        {
            pingServices.PlayerPingTokens[i] = 0;
        }

        commander.ExecuteClientCommandFromServer("player_ping");
    }

    public HookResult OnPlayerPing(EventPlayerPing @event, GameEventInfo info)
    {
        if (!_valid || _jailBreak.Commander?.CurrentCommander == null)
        {
            return HookResult.Continue;
        }

        var commander = _jailBreak.Commander.CurrentCommander;
        if (commander == null || !commander.IsValid || !commander.PawnIsAlive)
        {
            return HookResult.Continue;
        }

        if (@event.Userid == null || @event.Userid.SteamID != commander.SteamID)
        {
            return HookResult.Continue;
        }

        if (!commander.Buttons.HasFlag(PlayerButtons.Use))
        {
            _lastPosition = null;
            return HookResult.Continue;
        }

        for (int i = 0; i < 5; i++)
        {
            commander.PlayerPawn.Value!.PingServices!.PlayerPingTokens[i] = 0;
        }

        if (_lastPosition == null)
        {
            _lastPosition = new Vector(@event.X, @event.Y, @event.Z);
            RemoveNativePing(commander);
            return HookResult.Handled;
        }

        var newPosition = new Vector(@event.X, @event.Y, @event.Z);
        var distance = VectorUtils.GetVectorDistance(_lastPosition, newPosition);

        if (distance > 20 || (DateTime.UtcNow - _lastDrawTime).TotalMilliseconds > 80)
        {
            var beam = Utilities.CreateEntityByName<CEnvBeam>("env_beam");
            if (beam != null)
            {
                beam.Render = Color.DeepSkyBlue;
                beam.Width = 2.0f;
                beam.Teleport(_lastPosition, new QAngle(0, 0, 0), new Vector(0, 0, 0));
                beam.EndPos.X = newPosition.X;
                beam.EndPos.Y = newPosition.Y;
                beam.EndPos.Z = newPosition.Z;
                Utilities.SetStateChanged(beam, "CBeam", "m_vecEndPos");
                beam.DispatchSpawn();

                _jailBreak.AddTimer(1.2f, () =>
                {
                    if (beam.IsValid)
                    {
                        beam.Remove();
                    }
                }, TimerFlags.STOP_ON_MAPCHANGE);
            }

            _lastPosition = newPosition;
            _lastDrawTime = DateTime.UtcNow;
        }

        RemoveNativePing(commander);
        return HookResult.Handled;
    }

    private static void RemoveNativePing(CCSPlayerController commander)
    {
        var ping = commander.PlayerPawn.Value?.PingServices?.PlayerPing.Value;
        if (ping != null && ping.IsValid)
        {
            ping.Remove();
        }
    }
}