using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using JailBreak.Games.GameDays;
using JailBreak.Games.LRGames;
using JailBreak.Menus;

namespace JailBreak.CommanderFunctions;

public class GameDaysCommand : CommanderFunction
{
    private readonly JailBreakMenusManager _menusManager;
    private readonly LRGameController _lrGameController;
    private readonly GameDaysController _gameDaysController;

    public GameDaysCommand(JailBreakMenusManager menusManager, LRGameController lrGameController,
        GameDaysController gameDaysController, JailBreak jailBreak) :
        base(jailBreak)
    {
        _menusManager = menusManager;
        _lrGameController = lrGameController;
        _gameDaysController = gameDaysController;
    }

    public override string Name { get; set; } = "Игровые дни";
    public override bool PlayersChoice { get; protected set; } = false;

    protected override void OnSelect(CCSPlayerController commander, CCSPlayerController inmate = null!)
    {
        if (_lrGameController.PlayerAlreadyPlay(commander))
        {
            commander.PrintToChat("Идёт лр");
            return;
        }

        if (_gameDaysController.IsGame())
        {
            commander.PrintToChat("Уже идёт игровой день");
            return;
        }

        var menu = _menusManager.GetMenu(typeof(GameDay));
        if (menu != null)
            MenuManager.OpenChatMenu(commander, menu);
    }
}