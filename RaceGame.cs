using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using JailBreak.Games.LRGames;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace JailBreak.LRGames;

public class RaceGame : LRGame
{
    private Circle _start;
    private Circle _finish;

    private CBeam _line;
    private Timer _timer;

    public override string Name { get; set; } = "Гонка";

    private BasePlugin.GameEventHandler<EventPlayerPing> _eventPlayerPing;

    private SelectState _selectState = SelectState.None;

    public override bool DisableAllDamage { get; set; } = true;

    public RaceGame(JailBreak jailBreak, LRGameController lrGameController) : base(jailBreak, lrGameController)
    {
    }

    protected override void OnSelected()
    {
        _start = new Circle(50, 1.5f, Color.Blue);
        _finish = new Circle(50, 1.5f, Color.Blue);

        _line = PaintTools.Draw(1.5f, Color.Red);

        _eventPlayerPing = OnPlayerPing;
        _jailBreak.RegisterEventHandler(_eventPlayerPing);


        var menu = new ChatMenu("Настройка гонки");

        menu.AddMenuOption("Установить старт", (controller, option) =>
        {
            _selectState = SelectState.Start;

            MenuManager.OpenChatMenu(Inmate, menu);
            controller.PrintToCenter("Установите начальную точку на колёсико мыши");
        });

        menu.AddMenuOption("Установить финиш", ((controller, option) =>
        {
            _selectState = SelectState.Finish;

            MenuManager.OpenChatMenu(Inmate, menu);
            controller.PrintToCenter("Установите конечную точку на колёсико мыши");
        }));
        menu.AddMenuOption("Начать", (controller, option) =>
        {
            if (!_start.IsDrawn || !_finish.IsDrawn)
            {
                controller.PrintToCenter("Сначала установите точки");
                return;
            }

            if (VectorUtils.GetVectorDistance(_start.Center, _finish.Center) < 300)
            {
                controller.PrintToCenter("Расстояние между стартом и финишом слишком маленькое");
                return;
            }

            MenuManager.CloseActiveMenu(controller);
            ChooseOpponent();
        });
        
        MenuManager.OpenChatMenu(Inmate, menu);
    }

    private HookResult OnPlayerPing(EventPlayerPing @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player != Inmate || !player.IsLegal() || _selectState == SelectState.None)
            return HookResult.Continue;


        var playerPawn = player.PlayerPawn.Value!;

        if (!((PlayerFlags)playerPawn.Flags).HasFlag(PlayerFlags.FL_ONGROUND))
        {
            @event.Userid.PrintToCenter("Вы должны находиться на земле");
            return HookResult.Continue;
        }

        var newVector = new Vector(@event.X, @event.Y, @event.Z);

        if (VectorUtils.GetVectorDistance(playerPawn.AbsOrigin!, newVector) > 300)
        {
            @event.Userid.PrintToCenter("Точка находится слишком далеко от вас");
            return HookResult.Continue;
        }


        newVector.Z += 10;
        
        if (_selectState == SelectState.Start)
        {
            _start.Draw(newVector);
        }
        else if (_selectState == SelectState.Finish)
        {
            _finish.Draw(newVector);
        }
        
        if (_start.IsDrawn && _finish.IsDrawn)
        {
            PaintTools.UpdateBeamPosition(_line, _start.Center, _finish.Center);
        }
        

        _selectState = SelectState.None;

        return HookResult.Continue;
    }

    protected override void OnExecute()
    {
        _start.DrawText("START", 150, Color.Aqua);
        _finish.DrawText("FINISH", 150, Color.Aqua);


        Inmate.PlayerPawn.Value!.Teleport(_start.Center, new QAngle(), new Vector());
        Guardian.PlayerPawn.Value!.Teleport(_start.Center, new QAngle(), new Vector());

        ProcessParticipants(player =>
        {
            player.SetCollisionGroup(CollisionGroup.COLLISION_GROUP_DISSOLVING);
            player.SetMoveType(MoveType_t.MOVETYPE_NONE);
        });

        Timer counterTimer = null!;
        int counter = 3;

        counterTimer = _jailBreak.AddTimer(1f, () =>
        {
            ProcessParticipants(player => { player.PrintToCenter(counter.ToString()); });

            if (--counter < 0)
            {
                ProcessParticipants((player =>
                {
                    player.PrintToCenter("Беги!");
                }));

                ProcessParticipants(player => { player.SetMoveType(MoveType_t.MOVETYPE_WALK); });

                counterTimer.Kill();
            }
        }, TimerFlags.REPEAT);

        _timer = _jailBreak.AddTimer(0.1f, () =>
        {
            if (Inmate.IsLegal() && VectorUtils.GetVectorDistance(Inmate.PlayerPawn.Value.AbsOrigin!, _finish.Center, -50) < 50)
            {
                Guardian.CommitSuicide(true, true);
                InternalEnd();
            }

            else if (Guardian.IsLegal() && VectorUtils.GetVectorDistance(Guardian.PlayerPawn.Value.AbsOrigin!, _finish.Center, -50) < 50)
            {
                Inmate.CommitSuicide(true, true);
                InternalEnd();
            }
        }, TimerFlags.REPEAT);
    }

    protected override void OnCancel()
    {
        _jailBreak.DeregisterEventHandler("player_ping", _eventPlayerPing, true);
        _start.Dispose();
        _finish.Dispose();
        
        if (_line.IsValid)
            _line?.Remove();
    }

    protected override void OnEnd(CCSPlayerController? winner)
    {
        winner?.SetCollisionGroup(CollisionGroup.COLLISION_GROUP_PLAYER);
        _timer?.Kill();

        OnCancel();
    }
}

public enum SelectState
{
    Start = 0,
    Finish = 1,
    None = 2
}