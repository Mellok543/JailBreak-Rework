using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;

namespace JailBreak.CommanderFunctions;

public class FreeDayCommand : CommanderFunction
{
    public override string Name { get; set; } = "Free Day";
    public override bool PlayersChoice { get; protected set; } = false;

    public bool IsFriday { get; set; }


    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        IsFriday = false;
        return HookResult.Continue;
    }

    private HookResult OnTakeDamage(DynamicHook arg)
    {
        if (!IsFriday)
            return HookResult.Continue;

        var userId = arg.GetParam<CCSPlayerPawn>(0).Controller.Value?.As<CCSPlayerController>();
        var dmgInfo = arg.GetParam<CTakeDamageInfo>(1);
        var attacker = dmgInfo.Attacker.Value?.As<CCSPlayerPawn>().Controller.Value?.As<CCSPlayerController>();

        if (!attacker.IsLegal() || !userId.IsLegal())
            return HookResult.Continue;

        if (userId.TeamNum != (int)CsTeam.CounterTerrorist || attacker.Team != CsTeam.Terrorist)
            return HookResult.Continue;


        return HookResult.Handled;
    }


    protected override void OnSelect(CCSPlayerController commander, CCSPlayerController inmate = null!)
    {
        IsFriday = !IsFriday;
        commander.PrintToChat($"Free Day: {IsFriday}");
        var inmates = Utilities.GetPlayers().Where(player => player.Team == CsTeam.Terrorist);

        foreach (var player in inmates)
        {
            player.SetColor(IsFriday ? Color.Green : Color.White);
        }
    }

    public FreeDayCommand(JailBreak jailBreak) : base(jailBreak)
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
        jailBreak.RegisterEventHandler<EventRoundStart>(OnRoundStart, HookMode.Post);
    }
}