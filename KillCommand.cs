using CounterStrikeSharp.API.Core;

namespace JailBreak.CommanderFunctions;

public class KillCommand : CommanderFunction
{
    public override string Name { get; set; } = "Убить игрока";
    
    public override bool PlayersChoice { get; protected set; } = true;

    protected override void OnSelect(CCSPlayerController commander, CCSPlayerController inmate)
    {
        inmate.CommitSuicide(true, true);
        commander.PrintToChat($"{inmate.PlayerName} убит");
    }

    public KillCommand(JailBreak jailBreak) : base(jailBreak)
    {
    }
}