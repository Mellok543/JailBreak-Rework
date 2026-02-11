using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using JailBreak.LRGames;
using JailBreak.Menus;

namespace JailBreak.Games.LRGames;

public abstract class LRGame : IFeatureTransit, IMenuItem
{
    private readonly LRGameController _lrGameController;
    public virtual string Name { get; set; }
    public virtual bool DisableAllDamage { get; set; }

    public GameState GameState { get; private set; } = GameState.Create;

    public JailBreak _jailBreak;

    public CCSPlayerController Guardian { get; set; }
    public CCSPlayerController Inmate { get; set; }

    public event Action<LRGame> OnGameEnd;

    private Listeners.OnClientDisconnect _onClientDisconnect;
    private BasePlugin.GameEventHandler<EventPlayerDeath> _eventPlayerDeath;
    private BasePlugin.GameEventHandler<EventRoundEnd> _eventRoundEnd;
    private BasePlugin.GameEventHandler<EventRoundStart> _eventRoundStart;

    private Func<DynamicHook, HookResult> _onTakeDamage;

    public LRGame(JailBreak jailBreak, LRGameController lrGameController)
    {
        _lrGameController = lrGameController;
        _jailBreak = jailBreak;
    }

    public HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (player == Guardian || player == Inmate)
        {
            ProcessEvent();
        }

        return HookResult.Continue;
    }

    public HookResult EventRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        ProcessEvent();
        return HookResult.Continue;
    }

    public HookResult EventRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        ProcessEvent();
        return HookResult.Continue;
    }

    private void OnClientDisconnect(int slot)
    {
        if (slot == Inmate.Slot || slot == Guardian.Slot)
        {
            ProcessEvent();
        }
    }

    private HookResult OnTakeDamage(DynamicHook arg)
    {
        var userId = arg.GetParam<CCSPlayerPawn>(0).Controller.Value?.As<CCSPlayerController>();

        var dmgInfo = arg.GetParam<CTakeDamageInfo>(1);
        var attacker = dmgInfo.Attacker.Value?.As<CCSPlayerPawn>().Controller.Value?.As<CCSPlayerController>();

        if (!userId.IsLegal() || !attacker.IsLegal())
        {
            return HookResult.Continue;
        }

        if (!(attacker == Inmate || attacker == Guardian || userId == Inmate || userId == Guardian))
        {
            return HookResult.Continue;
        }

        if (DisableAllDamage)
        {
            return HookResult.Handled;
        }

        if (attacker == Guardian && userId == Inmate || attacker == Inmate && userId == Guardian)
        {
            return HookResult.Continue;
        }

        return HookResult.Handled;
    }

    private void ProcessEvent()
    {
        if (GameState == GameState.Process)
        {
            InternalEnd();
        }

        else if (GameState == GameState.Create)
        {
            InternalCancel();
        }
    }

    private void InternalExecute()
    {
        GameState = GameState.Process;

        _jailBreak.ResetCommander();

        _lrGameController.RegisterGuardian(this, Guardian);
        MenuManager.CloseActiveMenu(Inmate);


        ProcessParticipants((player =>
        {
            player.RemoveWeapons(false);
            player.SetHealth(100);
            player.SetArmor(100);
        }));


        _onClientDisconnect = OnClientDisconnect;
        _onTakeDamage = OnTakeDamage;
        _eventPlayerDeath = EventPlayerDeath;
        _eventRoundEnd = EventRoundEnd;

        _jailBreak.RegisterEventHandler(_eventRoundEnd);
        _jailBreak.RegisterListener(_onClientDisconnect);
        _jailBreak.RegisterEventHandler(_eventPlayerDeath);

        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(_onTakeDamage, HookMode.Pre);

        OnExecute();
    }


    public void ChooseOpponent()
    {
        var menu = new ChatMenu("Выберите кт");

        var players = Utilities.GetPlayers();

        var guardians = players.Where(player =>
                player.Team == CsTeam.CounterTerrorist && player.IsLegal() &&
                !_lrGameController.PlayerAlreadyPlay(player) &&
                player.PawnIsAlive)
            .ToList();

        foreach (var guardian in guardians)
        {
            menu.AddMenuOption(guardian.PlayerName, (controller, option) =>
            {
                if (_lrGameController.PlayerAlreadyPlay(guardian))
                {
                    controller.PrintToCenter("Игрок уже играет лр");
                    ProcessEvent();

                    MenuManager.CloseActiveMenu(controller);
                    return;
                }

                MenuManager.CloseActiveMenu(controller);
                Guardian = guardian;
                InternalExecute();
            });
        }

        MenuManager.OpenChatMenu(Inmate, menu);
    }


    public virtual void InternalSelect(CCSPlayerController player)
    {
        _eventRoundStart = EventRoundStart;
        _jailBreak.RegisterEventHandler(_eventRoundStart);

        Inmate = player;
        OnSelected();
    }

    public void InternalCancel()
    {
        if (GameState == GameState.End)
            return;

        _jailBreak.DeregisterEventHandler("round_start", _eventRoundStart, true);
        MenuManager.CloseActiveMenu(Inmate);

        GameState = GameState.End;
        OnCancel();
    }

    protected void InternalEnd()
    {
        if (GameState == GameState.End)
            return;

        OnGameEnd?.Invoke(this);

        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(_onTakeDamage, HookMode.Pre);

        _jailBreak.DeregisterEventHandler("player_death", _eventPlayerDeath, true);
        _jailBreak.DeregisterEventHandler("round_end", _eventRoundEnd, true);
        _jailBreak.DeregisterEventHandler("round_start", _eventRoundStart, true);
        _jailBreak.RemoveListener("OnClientDisconnect", _onClientDisconnect);


        GameState = GameState.End;
        MenuManager.CloseActiveMenu(Inmate);

        ProcessParticipants(player => player.RemoveWeapons(false));

        _jailBreak.AddTimer(0.1f, () =>
        {
            if (Inmate.IsLegal() && Inmate.PawnIsAlive)
            {
                OnEnd(Inmate);
            }
            else if (Guardian.IsLegal() && Guardian.PawnIsAlive)
            {
                Guardian.GiveNamedItem("weapon_usp_silencer");
                Guardian.GiveNamedItem("weapon_ak47");

                OnEnd(Guardian);
            }
            else
            {
                OnEnd(null);
            }
        });
    }

    protected void ProcessParticipants(Action<CCSPlayerController> action)
    {
        if (Inmate.IsLegal() && Inmate.PawnIsAlive)
        {
            action.Invoke(Inmate);
        }

        if (Inmate.IsLegal() && Inmate.PawnIsAlive)
        {
            action.Invoke(Guardian);
        }
    }

    protected virtual void OnSelected()
    {
    }

    protected virtual void OnExecute()
    {
    }

    protected virtual void OnCancel()
    {
    }

    protected virtual void OnEnd(CCSPlayerController? winner)
    {
    }
}

public enum GameState
{
    Create = 0,
    Process = 1,
    End = 2,
}