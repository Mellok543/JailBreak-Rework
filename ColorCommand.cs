using System.Drawing;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace JailBreak.CommanderFunctions;

public class ColorCommand : CommanderFunction
{
    public override string Name { get; set; } = "Разделение по цветам";
    public override bool PlayersChoice { get; protected set; } = false;

    private bool _divisionStatus;

    protected override void OnSelect(CCSPlayerController commander, CCSPlayerController inmate = null!)
    {
        _divisionStatus = !_divisionStatus;

        var inmates = Utilities.GetPlayers().Where(player => player.Team == CsTeam.Terrorist).ToList();

        for (var i = 0; i < inmates.Count; i++)
        {
            var player = inmates[i];

            player.SetColor(_divisionStatus
                ? i % 2 == 0 ? Color.Red : Color.Blue
                : player.PlayerPawn.Value!.Render = Color.White);
        }
    }

    public ColorCommand(JailBreak jailBreak) : base(jailBreak)
    {
    }
}