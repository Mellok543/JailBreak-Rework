using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using JailBreak.Games.LRGames;

namespace JailBreak.LRGames;

public class NoScope : LRGame
{
    public override string Name => "Битва без прицелов";
    private string _selectedWeapon;

    private Link _link = new();

    private BasePlugin.GameEventHandler<EventWeaponZoom> _eventWeaponZoom;

    private List<string> _weapons = new()
    {
        "weapon_ssg08",
        "weapon_awp",
        "weapon_scar20",
    };

    public override bool DisableAllDamage { get; set; } = false;

    public NoScope(JailBreak jailBreak, LRGameController lrGameController) : base(jailBreak, lrGameController)
    {
    }

    protected override void OnSelected()
    {
        var weaponMenu = new ChatMenu("Выберите оружие: ");

        foreach (var weapon in _weapons)
        {
            weaponMenu.AddMenuOption(weapon, (controller, option) =>
            {
                _selectedWeapon = weapon;
                ChooseOpponent();
            });
        }

        MenuManager.OpenChatMenu(Inmate, weaponMenu);
    }

    protected override void OnExecute()
    {
        _link.Inmate = Inmate;
        _link.Guardian = Guardian;
        _link.JailBreak = _jailBreak;

        _link.Start();

        _eventWeaponZoom = OnWeaponZoom;
        _jailBreak.RegisterEventHandler(_eventWeaponZoom, HookMode.Pre);


        ProcessParticipants(player =>
        {
            player.GiveNamedItem(_selectedWeapon);
        });
    }

    private HookResult OnWeaponZoom(EventWeaponZoom @event, GameEventInfo info)
    {
        var player = @event.Userid;

        if (player == Inmate || player == Guardian)
        {
            var currentWeapon = player.PlayerPawn.Value!.WeaponServices!.ActiveWeapon.Value!.DesignerName;
            player.PlayerPawn.Value.WeaponServices!.ActiveWeapon.Value!.Remove();
            player.GiveNamedItem(currentWeapon);
        }

        return HookResult.Continue;
    }

    protected override void OnEnd(CCSPlayerController? winner)
    {
        _link.Dispose();
        _jailBreak.DeregisterEventHandler("weapon_zoom", _eventWeaponZoom, false);
    }
}