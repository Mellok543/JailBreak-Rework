using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace JailBreak.CommanderFunctions;

public class BhopCommand : CommanderFunction
{
    public override string Name { get; set; } = "Bhop";
    public override bool PlayersChoice { get; protected set; } = false;

    public bool IsBhop { get; private set; }

    private JailBreak _jailBreak;
    private Listeners.OnTick zalupa;


    public virtual HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        IsBhop = false;

        // if (_jailBreak.Listeners.ContainsKey(zalupa))
        //     _jailBreak.RemoveListener("OnTick", zalupa);


        return HookResult.Continue;
    }

    protected override void OnSelect(CCSPlayerController commander, CCSPlayerController? inmate = null)
    {
        IsBhop = !IsBhop;

        commander.PrintToChat($"Bhop: {IsBhop}");

        //
        // if (IsBhop == false)
        // {
        //     _jailBreak.RemoveListener("OnTick", zalupa);
        //     return;
        // }
        //
        // _jailBreak.RegisterListener(zalupa);
    }

    void OnTick()
    {
        if (!IsBhop)
            return;
        foreach (var player in Utilities.GetPlayers()
                     .Where(player => player is { IsValid: true, IsBot: false, PawnIsAlive: true }))
        {
            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn != null)
            {
                var flags = (PlayerFlags)playerPawn.Flags;
                var buttons = player.Buttons;

                if (buttons.HasFlag(PlayerButtons.Jump) && flags.HasFlag(PlayerFlags.FL_ONGROUND) &&
                    !playerPawn.MoveType.HasFlag(MoveType_t.MOVETYPE_LADDER))
                    playerPawn.AbsVelocity.Z = 300;
            }
        }
    }

    public BhopCommand(JailBreak jailBreak) : base(jailBreak)
    {
        zalupa = OnTick;
        jailBreak.RegisterEventHandler<EventRoundStart>(OnRoundStart, HookMode.Post);
        jailBreak.RegisterListener(zalupa);
    }
}