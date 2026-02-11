using CounterStrikeSharp.API.Core;

namespace JailBreak.CommanderFunctions;

public class RespawnCommand : CommanderFunction
{
    public override string Name { get; set; } = "Возродить игрока";
    public override bool PlayersChoice { get; protected set; } = true;

    public override Func<CCSPlayerController, bool> PlayerChoiceExpression { get; protected set; } =
        player => !player.PawnIsAlive;

    protected override void OnSelect(CCSPlayerController commander, CCSPlayerController? inmate = null)
    {
        inmate!.Respawn();
    }

    public RespawnCommand(JailBreak jailBreak) : base(jailBreak)
    {
    }
}