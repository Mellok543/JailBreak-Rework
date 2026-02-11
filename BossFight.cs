using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;


namespace JailBreak.Games.GameDays;

public class BossFight : GameDay
{
    private CCSPlayerController Boss;

    public override string Name { get; set; } = "Босфайт";

    private bool BossFightStarted = false;

    private Func<DynamicHook, HookResult> _onTakeDamage;
    private BasePlugin.GameEventHandler<EventPlayerDeath> _eventPlayerDeath;

    public BossFight(JailBreak jailBreak, GameDaysController gameDaysController) : base(jailBreak, gameDaysController)
    {
    }

    protected override void Start(CCSPlayerController initiator)
    {
        BossFightStarted = false;

        var players = Utilities.GetPlayers().Where(player => player.IsLegal()).ToList();
        Boss = players[Random.Shared.Next(0, players.Count)];

        Server.PrintToChatAll($"Босс - {Boss.PlayerName}");
        Boss.SetColor(Color.Red);
        Boss.SetHealth(10000);

        foreach (var player in players)
        {
            if (!player.PawnIsAlive || player == Boss) continue;
            player.PrintToCenter("У вас 30 секунд на выбор оружия");
            player.OpenWeaponMenu();
        }

        _jailBreak.AddTimer(30, () =>
        {
            ConVar.Find("mp_teammates_are_enemies")!.SetValue(true);
            BossFightStarted = true;

            foreach (var player in Utilities.GetPlayers())
            {
                player.PrintToCenter("Огонь по боссу разрешен");
            }
        });

        _onTakeDamage = OnTakeDamage;
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(_onTakeDamage, HookMode.Pre);

        _eventPlayerDeath = EventPlayerDeath;
        _jailBreak.RegisterEventHandler(_eventPlayerDeath);
    }


    private HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        if (@event.Userid == Boss)
        {
            Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!
                .TerminateRound(5, RoundEndReason.RoundDraw);
            InternalEnd();

            return HookResult.Continue;
        }


        int alivePlayers = 0;

        foreach (var player in Utilities.GetPlayers())
        {
            if (player.PawnIsAlive) alivePlayers++;
        }

        if (alivePlayers <= 2)
        {
            Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!
                .TerminateRound(5, RoundEndReason.RoundDraw);

            InternalEnd();
        }

        return HookResult.Continue;
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


        if (attacker != Boss && userId != Boss || !BossFightStarted)
        {
            return HookResult.Handled;
        }

        return HookResult.Continue;
    }

    protected override HookResult EventPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var user = @event.Userid!;

        if (!user.IsLegal()) return HookResult.Continue;

        Server.NextFrame((() =>
        {
            user.RemoveWeapons();
            user.GiveNamedItem(CsItem.Knife);
        }));

        return HookResult.Continue;
    }

    protected override void End()
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(_onTakeDamage, HookMode.Pre);
        _jailBreak.DeregisterEventHandler("player_death", _eventPlayerDeath, true);

        ConVar.Find("mp_teammates_are_enemies")!.SetValue(false);
    }
}