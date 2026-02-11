using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using JailBreak.Menus;

namespace JailBreak.CommanderFunctions;

public abstract class CommanderFunction : IFeature, IMenuItem
{
    public abstract string Name { get; set; }
    public abstract bool PlayersChoice { get; protected set; }

    protected JailBreak _jailBreak;

    public virtual Func<CCSPlayerController, bool> PlayerChoiceExpression { get; protected set; } =
        player => player.PawnIsAlive && player.Team == CsTeam.Terrorist;

    public void InternalSelect(CCSPlayerController player)
    {
        if (!PlayersChoice)
        {
            OnSelect(player);
            return;
        }

        var playersChoiceMenu = new ChatMenu("Выберите игрока");
        playersChoiceMenu.AddPlayers(PlayerChoiceExpression, OnSelect);

        MenuManager.OpenChatMenu(player, playersChoiceMenu);
    }

    protected abstract void OnSelect(CCSPlayerController commander, CCSPlayerController inmate = null!);

    protected CommanderFunction(JailBreak jailBreak)
    {
        _jailBreak = jailBreak;
    }
}