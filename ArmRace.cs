using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Modules.Entities.Constants.CsItem;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace JailBreak.Games.GameDays;

public class ArmRace : GameDay
{
    public override string Name { get; set; } = "Гонка вооружений";

    private ArmRaceLevel[] _currentPlayerLevel;


    private int[] _playerKills;

    private CsTeam _winners = CsTeam.None;

    private Timer _timer;

    private static List<ArmRaceLevel> _levels = new()
    {
        new ArmRaceLevel
        {
            Weapons = [G3SG1, SCAR20],
            ExtraWeapons = [Zeus],
            Kills = 0
        },

        new ArmRaceLevel
        {
            Weapons = [AWP],
            ExtraWeapons = [Zeus],
            Kills = 2
        },

        new ArmRaceLevel
        {
            Weapons = [AK47, AUG, Famas, Galil, M4A4, M4A1, SG553],
            ExtraWeapons = [Zeus],
            Kills = 4
        },

        new ArmRaceLevel
        {
            Weapons = [Knife],
            Kills = 6
        }
    };


    private BasePlugin.GameEventHandler<EventPlayerDeath> _eventPlayerDeath;


    protected override void Start(CCSPlayerController player)
    {
        _currentPlayerLevel = new ArmRaceLevel[65];
        for (var i = 0; i < _currentPlayerLevel.Length; i++)
        {
            _currentPlayerLevel[i] = _levels[0];
        }

        _playerKills = new int[65];


        _eventPlayerDeath = EventPlayerDeath;
        _jailBreak.RegisterEventHandler(_eventPlayerDeath, HookMode.Pre);




        _timer = _jailBreak.AddTimer(3, (() =>
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (player.Team != CsTeam.Terrorist && player.Team != CsTeam.CounterTerrorist)
                    continue;

                if (!player.PawnIsAlive && player.IsLegal())
                    player.Respawn();
            }
        }), TimerFlags.REPEAT);
    }


    protected override HookResult EventPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var user = @event.Userid;

        Server.NextFrame((() => { GiveWeapon(user); }));

        return HookResult.Continue;
    }

    private HookResult EventPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var userId = @event.Userid;
        foreach (var cEntityInstance in Utilities.GetAllEntities())
        {
            if (cEntityInstance.IsValid && cEntityInstance.DesignerName.Contains("weapon"))
            {
                if (!new CBasePlayerWeapon(cEntityInstance.Handle).OwnerEntity.IsValid)
                    cEntityInstance.Remove();
            }
        }

        //ew CBasePlayerWeapon(userId.InventoryServices.ServerAuthoritativeWeaponSlots[1].Handle).Remove();
        //if (userId.IsLegal())

        var attacker = @event.Attacker;

        if (!attacker.IsLegal()) return HookResult.Continue;


        if (++_playerKills[attacker.Slot] > _levels[^1].Kills)
        {
            _winners = attacker.Team;
            InternalEnd();
            return HookResult.Continue;
        }

        var newLevel = GetAppropriateLevel(_playerKills[attacker.Slot]);

        if (newLevel != _currentPlayerLevel[attacker.Slot]!)
        {
            _currentPlayerLevel[attacker.Slot] = newLevel;
            GiveWeapon(attacker);
        }


        return HookResult.Continue;
    }

    private ArmRaceLevel GetAppropriateLevel(int kills)
    {
        var lastLevel = _levels[0];

        foreach (var armRaceLevel in _levels)
        {
            if (armRaceLevel.Kills > kills)
                break;

            lastLevel = armRaceLevel;
        }

        return lastLevel;
    }

    private void GiveWeapon(CCSPlayerController player)
    {
        if (!player.IsLegal())
            return;


        player.RemoveWeapons();
        player.GiveNamedItem(Knife);

        var playerKills = _playerKills[player.Slot];

        if (playerKills >= _levels[^1].Kills) return;


        var playerLevel = _currentPlayerLevel[player.Slot];

        foreach (var extraWeapon in playerLevel.ExtraWeapons)
        {
            player.GiveNamedItem(extraWeapon);
        }

        player.GiveNamedItem(_currentPlayerLevel[player.Slot].GetRandomItem);
    }


    protected override void End()
    {
        _jailBreak.DeregisterEventHandler("player_death", _eventPlayerDeath, false);

        Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!
            .TerminateRound(6, _winners == CsTeam.Terrorist ? RoundEndReason.TerroristsWin : RoundEndReason.CTsWin);

        _timer?.Kill();
    }

    public ArmRace(JailBreak jailBreak, GameDaysController gameDaysController) : base(jailBreak, gameDaysController)
    {
    }
}