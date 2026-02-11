using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using JailBreak.LRGames;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace JailBreak.Games.LRGames;

public class GolfGame : LRGame
{
    private Circle _start;
    private Circle _finish;

    private CBeam _line;

    private int _inmateDistance = -1;
    private int _guardianDistance = -1;


    public override bool DisableAllDamage { get; set; } = true;


    private SelectState _selectState = SelectState.None;

    private BasePlugin.GameEventHandler<EventPlayerPing> _eventPlayerPing;
    private BasePlugin.GameEventHandler<EventDecoyStarted> _eventDecoyStarted;
    public override string Name { get; set; } = "Гольф";

    public GolfGame(JailBreak jailBreak, LRGameController lrGameController) : base(jailBreak, lrGameController)
    {
    }

    protected override void OnSelected()
    {
        _start = new Circle(50, 1.5f, Color.Blue);
        _finish = new Circle(30, 1.5f, Color.Blue);

        _line = PaintTools.Draw(1.5f, Color.Red);

        _eventPlayerPing = OnPlayerPing;
        _jailBreak.RegisterEventHandler(_eventPlayerPing);

        var menu = new ChatMenu("Настройка гольфа");

        menu.AddMenuOption("Установить старт", (controller, option) =>
        {
            _selectState = SelectState.Start;

            MenuManager.OpenChatMenu(Inmate, menu);
            controller.PrintToCenter("Установите начальную точку на колёсико мыши");
        });

        menu.AddMenuOption("Поставить лунку", ((controller, option) =>
        {
            _selectState = SelectState.Finish;

            MenuManager.OpenChatMenu(Inmate, menu);
            controller.PrintToCenter("Установите конечную точку на колёсико мыши");
        }));

        menu.AddMenuOption("Начать", (controller, option) =>
        {
            if (_start.Center == null! || _finish.Center == null!)
            {
                controller.PrintToCenter("Сначала установите точки");
                return;
            }

            if (VectorUtils.GetVectorDistance(_start.Center, _finish.Center) < 300)
            {
                controller.PrintToCenter("Расстояние между стартом и лункой слишком маленькое");
                return;
            }

            MenuManager.CloseActiveMenu(controller);
            ChooseOpponent();
        });

        //Почему NextFrame? - Сын портовой шлюхи на B13
        MenuManager.OpenChatMenu(Inmate, menu);
    }

    protected override void OnExecute()
    {
        _eventDecoyStarted = OnDecoyStarted;
        _jailBreak.RegisterEventHandler(_eventDecoyStarted);

        var direction = VectorUtils.Normalize(_finish.Center - _start.Center);
        var perpDirection = VectorUtils.Normalize(VectorUtils.Cross(direction, new Vector(0, 0, 1)));

        var distance = 35;


        var point1 = _start.Center + perpDirection * distance;
        var point2 = _start.Center - perpDirection * distance;

        Inmate.PlayerPawn.Value!.Teleport(point1, new QAngle(), new Vector());
        Guardian.PlayerPawn.Value!.Teleport(point2, new QAngle(), new Vector());

        ProcessParticipants(player =>
        {
            player.SetMoveType(MoveType_t.MOVETYPE_NONE);
            player.GiveNamedItem(CsItem.Decoy);
        });
    }

    private HookResult OnDecoyStarted(EventDecoyStarted @event, GameEventInfo info)
    {
        var player = @event.Userid;
        var decoyPosition = new Vector(@event.X, @event.Y, @event.Z);

        if (player == Inmate)
        {
            _inmateDistance = VectorUtils.GetVectorDistance(decoyPosition, _finish.Center);
        }
        else if (player == Guardian)
        {
            _guardianDistance = VectorUtils.GetVectorDistance(decoyPosition, _finish.Center);
        }

        if (_inmateDistance != -1 && _guardianDistance != -1)
        {
            if (_guardianDistance > _inmateDistance)
            {
                Guardian.CommitSuicide(true, true);
            }
            else
            {
                Inmate.CommitSuicide(true, true);
            }

            InternalEnd();
        }

        return HookResult.Continue;
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

    protected override void OnCancel()
    {
        _jailBreak.DeregisterEventHandler("player_ping", _eventPlayerPing, true);
        _start.Dispose();
        _finish.Dispose();
        
        if (_line.IsValid)
            _line?.Remove();
    }

    protected override void OnEnd(CCSPlayerController? player)
    {
        _jailBreak.DeregisterEventHandler("decoy_started", _eventDecoyStarted, true);
        player?.SetMoveType(MoveType_t.MOVETYPE_WALK);

        OnCancel();
    }
}