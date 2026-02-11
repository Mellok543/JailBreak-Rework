using System.Collections.Generic;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;

namespace JailBreak.CommanderFunctions;

public class DoorsCommand : CommanderFunction
{
    private List<string> _doorNames = new()
    {
        "func_door",
        "func_movelinear",
        "func_door_rotating",
        "prop_door_rotating",
    };
    //func_breakable


    public override string Name { get; set; } = "Открыть/Закрыть двери";
    public override bool PlayersChoice { get; protected set; }

    protected override void OnSelect(CCSPlayerController commander, CCSPlayerController inmate = null!)
    {
        var menu = new ChatMenu("Действие");
        menu.AddMenuOption("Открыть все", (controller, option) => ActionDoors("Open"));
        menu.AddMenuOption("Закрыть все", (controller, option) => ActionDoors("Close"));

        MenuManager.OpenChatMenu(commander, menu);
    }

    private void ActionDoors(string action)
    {
        foreach (var doorName in _doorNames)
        {
            ForceEntInput(doorName, action);
        }

        if (action == "Open")
        {
            ForceEntInput("func_breakable", "Break");
        }
    }

    private void ForceEntInput(String name, String input)
    {
        var target = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>(name);

        foreach (var ent in target)
        {
            if (!ent.IsValid) continue;

            ent.AcceptInput(input);
        }
    }

    public DoorsCommand(JailBreak jailBreak) : base(jailBreak)
    {
    }
}