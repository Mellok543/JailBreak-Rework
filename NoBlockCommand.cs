using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Cvars;

namespace JailBreak.CommanderFunctions;

public class NoBlockCommand : CommanderFunctionCvar<int>
{
    public override string Name { get; set; } = "NoBlock";
    public override bool PlayersChoice { get; protected set; } = false;


    public NoBlockCommand(JailBreak jailBreak) : base(jailBreak)
    {
        ConVars = new List<string>()
        {
            "mp_solid_teammates"
        };
    }
}