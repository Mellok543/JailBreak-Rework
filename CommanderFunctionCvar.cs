using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Cvars;

namespace JailBreak.CommanderFunctions;

public abstract class CommanderFunctionCvar<T> : CommanderFunction
{
    public CommanderFunctionCvar(JailBreak jailBreak) : base(jailBreak)
    {
        jailBreak.RegisterEventHandler<EventRoundStart>(OnRoundStart);
    }

    public override string Name { get; set; }
    public override bool PlayersChoice { get; protected set; }

    public bool ConVarState { get; protected set; }

    public List<string> ConVars { get; protected set; } = new();

    public virtual HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        ConVarState = false;
        UpdateConVars();

        return HookResult.Continue;
    }

    protected override void OnSelect(CCSPlayerController commander, CCSPlayerController inmate = null)
    {
        ConVarState = !ConVarState;
        UpdateConVars();

        commander.PrintToChat($"{Name}: {ConVarState}");
    }

    private void UpdateConVars()
    {
        foreach (var conVar in ConVars)
        {
            if (typeof(T) == typeof(bool))
                ConVar.Find(conVar)!.SetValue(ConVarState);
            else if (typeof(T) == typeof(int))
                ConVar.Find(conVar)!.SetValue(Convert.ToInt32(ConVarState));
        }
    }
}