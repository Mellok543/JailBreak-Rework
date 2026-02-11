using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Menu;

namespace JailBreak.Games.GameDays;

public class HungerGames : GameDay
{
    private BasePlugin.GameEventHandler<EventPlayerDeath> _eventPlayerDeath;

    public HungerGames(JailBreak jailBreak, GameDaysController gameDaysController) : base(jailBreak, gameDaysController)
    {
    }

    public override string Name { get; set; } = "Голодные игры";


    protected override void Start(CCSPlayerController initiator)
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.PawnIsAlive) continue;

            player.PrintToCenter("У вас 1 минута на выбор оружия");

            player.PlayerPawn.Value!.TakesDamage = false;
            player.OpenWeaponMenu();
        }

        _jailBreak.AddTimer(30, () =>
        {
            foreach (var player in Utilities.GetPlayers())
            {
                player.PrintToCenter("Огонь разрешён");

                player.PlayerPawn.Value!.TakesDamage = true;
            }

            ConVar.Find("mp_teammates_are_enemies")!.SetValue(true);
        });

        _eventPlayerDeath = EventPlayerDeath;
        _jailBreak.RegisterEventHandler(_eventPlayerDeath);
    }

    protected override HookResult EventPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var user = @event.Userid!;

        Server.NextFrame((() =>
        {
            user.RemoveWeapons();
            user.GiveNamedItem(CsItem.Knife);
        }));

        return HookResult.Continue;
    }

    private HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var attacker = @event.Attacker!;

        attacker.SetHealth(attacker.PlayerPawn.Value.Health + 100);

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

    protected override void End()
    {
        _jailBreak.DeregisterEventHandler("player_death", _eventPlayerDeath, true);

        ConVar.Find("mp_teammates_are_enemies")!.SetValue(false);
    }
}