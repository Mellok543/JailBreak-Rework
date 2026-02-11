using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace JailBreak.CommanderFunctions;

public class HealCommand : CommanderFunction
{
    public override string Name { get; set; } = "Вылечить игрока";
    public override bool PlayersChoice { get; protected set; } = true;

    public override Func<CCSPlayerController, bool> PlayerChoiceExpression { get; protected set; } =
        player => player.PawnIsAlive;

    protected override void OnSelect(CCSPlayerController commander, CCSPlayerController player)
    {
        player.SetHealth(player.Team == CsTeam.Terrorist ? 100 : 150);
    }

    public HealCommand(JailBreak jailBreak) : base(jailBreak)
    {
    }
}