using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using JailBreak.CommanderFunctions;
using JailBreak.Games.GameDays;
using JailBreak.Games.LRGames;
using JailBreak.LRGames;
using JailBreak.Menus;
using Microsoft.Extensions.DependencyInjection;
using BasePlugin = CounterStrikeSharp.API.Core.BasePlugin;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace JailBreak;

public class JailBreak : BasePlugin
{
    public override string ModuleName => "JailBreak";
    public override string ModuleVersion => "1.0";

    public override string ModuleAuthor => "ART";

    public CCSPlayerController? Commander { get; private set; } = null!;

    private IServiceProvider _serviceProvider;

    private JailBreakMenusManager _menusManager;
    private LRGamesFactory _lrGamesFactory;
    private LRGameController _lrGameController;
    private GameDaysController _gameDaysController;
    private CtAccessService _ctAccessService;

    private Timer? _muteTimer;
    private bool _lrAvailableAnnounced;

    public JailBreak(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override void Load(bool hotReload)
    {
        IFeature.Instantiate(_serviceProvider);

        _menusManager = _serviceProvider.GetRequiredService<JailBreakMenusManager>();
        _lrGamesFactory = _serviceProvider.GetRequiredService<LRGamesFactory>();
        _lrGameController = _serviceProvider.GetRequiredService<LRGameController>();

        _gameDaysController = _serviceProvider.GetRequiredService<GameDaysController>();
        _ctAccessService = _serviceProvider.GetRequiredService<CtAccessService>();

        _menusManager.RegisterMenu(typeof(GameDay), "Игровые дни",
            ((controller, menuItem, type) => { menuItem.InternalSelect(controller); }));

        _menusManager.RegisterMenu(typeof(CommanderFunction), "Меню коммандира",
            ((controller, menuItem, type) => { menuItem.InternalSelect(controller); }));

        _menusManager.RegisterMenu(typeof(LRGame), "Игровое меню", ((controller, menuItem, type) =>
        {
            var game = _lrGamesFactory.Create(controller, type);
            game.InternalSelect(controller);
        }));

        RegisterListener<Listeners.OnClientDisconnect>((slot =>
        {
            if (Commander?.Slot == slot)
                Commander = null;
        }));

        RegisterEventHandler<EventPlayerDeath>(((@event, info) =>
        {
            var player = @event.Userid;

            MenuManager.CloseActiveMenu(player);
            
            if (player == Commander)
            {
                ResetCommander();
            }

            if (Utilities.GetPlayers().Count(p => p.Team == CsTeam.Terrorist && p.PawnIsAlive) <= 2)
            {
                Server.ExecuteCommand("css_vip_enable false");

                if (!_lrAvailableAnnounced)
                {
                    _lrAvailableAnnounced = true;
                    AnnounceLrAvailable();
                }
            }

            return HookResult.Continue;
        }));
    }


    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Server.ExecuteCommand("mp_force_pick_time 3000");
        Server.ExecuteCommand("mp_autoteambalance 0");
        Server.ExecuteCommand("sv_human_autojoin_team 2");
        Server.ExecuteCommand("mp_warmuptime 0");

        Server.ExecuteCommand("css_vip_enable true");

        if (Commander != null && Commander.IsLegal())
        {
            MenuManager.CloseActiveMenu(Commander);
        }

        Commander = null;
        _lrAvailableAnnounced = false;

        Server.PrintToChatAll("Голосовой чат для Т отключен на 30 секунд");
        foreach (var player in Utilities.GetPlayers())
        {
            var playerPawn = player.PlayerPawn.Value;
            if (!player.IsLegal() || !player.PawnIsAlive) continue;

            MenuManager.CloseActiveMenu(player);

            player.PlayerPawn.Value!.Render = Color.White;
            Utilities.SetStateChanged(playerPawn, "CBaseModelEntity", "m_clrRender");

            if (player.Team == CsTeam.Terrorist)
            {
                player.VoiceFlags = VoiceFlags.Muted;
            }
        }


        _muteTimer?.Kill();

        _muteTimer = AddTimer(30.0f, () =>
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (!player.IsLegal() || !player.PawnIsAlive) continue;
                player.VoiceFlags = VoiceFlags.Normal;
            }

            _muteTimer = null!;
        });

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (!player.IsLegal() || !player.PawnIsAlive)
        {
            return HookResult.Continue;
        }

        player.RemoveWeapons(false);

        if (player.Team == CsTeam.CounterTerrorist)
        {
            player.GiveNamedItem("weapon_usp_silencer");
            player.GiveNamedItem("weapon_ak47");
        }

        return HookResult.Continue;
    }


    [ConsoleCommand("css_ct")]
    public void OnCtAccessCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        _ctAccessService.HandleCtCommand(player);
    }

    [ConsoleCommand("css_w")]
    public void OnCommanderCommand(CCSPlayerController player, CommandInfo commandInfo)
    {
        if (!player.PawnIsAlive || !player.IsLegal() || player.Team != CsTeam.CounterTerrorist)
        {
            player.PrintToChat("This command is only for CT team players");
            return;
        }

        if (_lrGameController.PlayerAlreadyPlay(player))
        {
            player.PrintToChat("Идёт lr");
            return;
        }

        if (_gameDaysController.IsGame())
        {
            player.PrintToChat("Идёт игровой день");
            return;
        }

        if (Commander == null)
        {
            Commander = player;

            Commander.SetHealth(150);
            Commander.SetArmor(150);

            Commander.PlayerPawn.Value!.Render = Color.Blue;
            Utilities.SetStateChanged(Commander.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");

            player.PrintToChat("Вы теперь коммандир!");
        }
        else if (Commander != player)
        {
            player.PrintToChat("Коммандир уже существует!");
            return;
        }

        var menu = _menusManager.GetMenu(typeof(CommanderFunction));
        if (menu != null)
            MenuManager.OpenChatMenu(player, menu);
    }

    [ConsoleCommand("css_uw")]
    public void OnRemoveCommanderCommand(CCSPlayerController? player, CommandInfo commandInfo)
    {
        if (player != Commander)
            return;
        ResetCommander();
    }

    public void ResetCommander()
    {
        if (Commander == null)
            return;

        if (!Commander.IsLegal())
            return;

        Commander!.PrintToChat("Вы ушли в отставку");
        Commander.SetColor(Color.White);
        MenuManager.CloseActiveMenu(Commander);

        Commander = null;
    }


    private void AnnounceLrAvailable()
    {
        Server.PrintToChatAll("LR доступен! Все игроки получают бессмертие на 3 секунды.");

        var protectedPlayers = Utilities.GetPlayers()
            .Where(player => player.IsLegal() && player.PawnIsAlive)
            .ToList();

        foreach (var player in protectedPlayers)
        {
            player.PlayerPawn.Value!.TakesDamage = false;
        }

        AddTimer(3.0f, () =>
        {
            foreach (var player in protectedPlayers)
            {
                if (!player.IsLegal() || !player.PawnIsAlive)
                    continue;

                player.PlayerPawn.Value!.TakesDamage = true;
            }
        });
    }

    [ConsoleCommand("css_lr")]
    public void OnGamesCommand(CCSPlayerController player, CommandInfo commandInfo)
    {
        if (!player.PawnIsAlive)
        {
            player.PrintToChat("Только для живых");
            return;
        }

        if (_gameDaysController.IsGame())
        {
            player.PrintToChat("Нельзя играть лр во время игровых дней");
            return;
        }

        if (_lrGameController.PlayerAlreadyPlay(player))
        {
            player.PrintToChat("Ты уже играешь!");
            return;
        }

        if (player.Team != CsTeam.Terrorist)
        {
            player.PrintToChat("This command is only for inmates");
            return;
        }

        var players = Utilities.GetPlayers();
        var inmates = players.Where(player => player.Team == CsTeam.Terrorist && player.PawnIsAlive).ToList();

        if (inmates.Count > 2)
        {
            player.PrintToChat("Слишком много игроков для лр");
            return;
        }

        var menu = _menusManager.GetMenu(typeof(LRGame));

        if (menu != null)
            MenuManager.OpenChatMenu(player, menu);
    }
}
