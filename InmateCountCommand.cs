using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace JailBreak.CommanderFunctions;

public class InmateCountCommand : CommanderFunction
{
    public override string Name { get; set; } = "Посчитать зеков";
    public override bool PlayersChoice { get; protected set; } = false;

    protected override void OnSelect(CCSPlayerController commander, CCSPlayerController inmate = null!)
    {
        var inmates = Utilities.GetPlayers()
            .Where(player => player.Team == CsTeam.Terrorist && player.IsValid && player.PawnIsAlive);

        var message = $"Зеки далеко: \n \n \n ";

        foreach (var player in inmates)
        {
            var distance = VectorUtils.GetVectorDistance(commander.Pawn.Value.AbsOrigin, player.Pawn.Value.AbsOrigin);

            if (distance > 500)
            {
                message += $"{player.PlayerName} \n \n ";
            }
        }


        commander.PrintToChat(message);
    }

    public InmateCountCommand(JailBreak jailBreak) : base(jailBreak)
    {
    }
}