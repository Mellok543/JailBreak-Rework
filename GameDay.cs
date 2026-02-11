using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using JailBreak.Menus;

namespace JailBreak.Games.GameDays;

public class GameDay : IFeatureTransit, IMenuItem
{
    public virtual string Name { get; set; }
    public JailBreak _jailBreak;

    public bool GameStarted { get; private set; }

    private BasePlugin.GameEventHandler<EventRoundStart> _eventRoundStart;
    private BasePlugin.GameEventHandler<EventPlayerSpawn> _eventPlayerSpawn;

    private CCSPlayerController? Initiator;

    private GameDaysController _gameDaysController;


    public GameDay(JailBreak jailBreak, GameDaysController gameDaysController)
    {
        _jailBreak = jailBreak;
        _gameDaysController = gameDaysController;
    }

    public virtual HookResult EventRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (!GameStarted)
        {
            GameStarted = true;
            Start(Initiator);

            return HookResult.Continue;
        }

        InternalEnd();
        return HookResult.Continue;
    }

    public void InternalSelect(CCSPlayerController player)
    {
        _gameDaysController.RegisterGame(this);

        Initiator = player;

        Server.ExecuteCommand("mp_restartgame 1");
        Server.ExecuteCommand("mp_ignore_round_win_conditions 1");

        InternalStart();
    }

    protected virtual void InternalStart()
    {
        GameStarted = false;

        _eventRoundStart = EventRoundStart;
        _jailBreak.RegisterEventHandler(_eventRoundStart);

        _eventPlayerSpawn = EventPlayerSpawn;
        _jailBreak.RegisterEventHandler(_eventPlayerSpawn);
    }

    protected virtual HookResult EventPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        return HookResult.Continue;
    }

    protected virtual void Start(CCSPlayerController player)
    {
    }

    protected virtual void End()
    {
    }

    protected void InternalEnd()
    {
        GameStarted = false;
        _jailBreak.DeregisterEventHandler("round_start", _eventRoundStart, true);
        _jailBreak.DeregisterEventHandler("player_spawn", _eventPlayerSpawn, true);

        Server.ExecuteCommand("mp_ignore_round_win_conditions 0");

        End();
    }
}