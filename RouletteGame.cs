using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using JailBreak.Games.LRGames;

namespace JailBreak.LRGames;

public class RouletteGame : LRGame
{
    public override string Name => "Рулетка";

    private Link _link = new();

    public override bool DisableAllDamage { get; set; } = false;

    private BasePlugin.GameEventHandler<EventBulletImpact> _eventBulletImpact;


    public HookResult EventBulletImpact(EventBulletImpact @event, GameEventInfo info)
    {
        if (GameState == GameState.End)
            return HookResult.Continue;


        var player = @event.Userid;

        if (player == Inmate)
        {
            Guardian.SetAmmo(1, 0);
        }
        else if (player == Guardian)
        {
            Inmate.SetAmmo(1, 0);
        }

        return HookResult.Continue;
    }

    protected override void OnSelected()
    {
        ChooseOpponent();
    }

    protected override void OnExecute()
    {
        _eventBulletImpact = EventBulletImpact;
        _jailBreak.RegisterEventHandler(_eventBulletImpact);
        
        _link.Inmate = Inmate;
        _link.Guardian = Guardian;
        _link.JailBreak = _jailBreak;
        _link.Start();

        Inmate.GiveNamedItem(CsItem.DesertEagle);
        Guardian.GiveNamedItem(CsItem.DesertEagle);

        var magicNumber = Random.Shared.Next(0, 2);
        Inmate.SetAmmo(magicNumber == 0 ? 1 : 0, 0);
        Guardian.SetAmmo(magicNumber == 0 ? 0 : 1, 0);
    }

    protected override void OnEnd(CCSPlayerController? winner)
    {
        _jailBreak.DeregisterEventHandler("bullet_impact", _eventBulletImpact, true);
        _link.Dispose();
    }

    public RouletteGame(JailBreak jailBreak, LRGameController lrGameController) : base(
        jailBreak, lrGameController)
    {
    }
}