using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Utils;

namespace JailBreak.CommanderFunctions;

public class FriendlyFireCommand : CommanderFunctionCvar<bool>
{
    public override string Name { get; set; } = "Огонь по своим";
    public override bool PlayersChoice { get; protected set; } = false;


    public FriendlyFireCommand(JailBreak jailBreak) : base(jailBreak)
    {
        ConVars = new List<string>()
        {
            "mp_teammates_are_enemies"
        };

        jailBreak.RegisterEventHandler<EventPlayerDeath>(((@event, info) =>
        {
            _jailBreak.AddTimer(0.5f, () =>
            {
                int ctPlayers = Utilities.GetPlayers()
                    .Count(player => player.Team == CsTeam.CounterTerrorist && player.PawnIsAlive);
                int tPlayers = Utilities.GetPlayers()
                    .Count(player => player.Team == CsTeam.Terrorist && player.PawnIsAlive);

                
                if (ConVarState && (tPlayers == 0 || ctPlayers == 0))
                {
                    Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First().GameRules!
                        .TerminateRound(5, tPlayers == 0 ? RoundEndReason.CTsWin : RoundEndReason.TerroristsWin);
                }
            });

            return HookResult.Continue;
        }), HookMode.Post);
    }
}