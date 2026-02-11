using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;

namespace JailBreak;

public static class MenuExtensions
{
    public static void AddPlayers(this IMenu menu, Func<CCSPlayerController, bool> filter,
        Action<CCSPlayerController, CCSPlayerController> handler)
    {
        var players = Utilities.GetPlayers().Where(filter);

        foreach (var player in players)
        {
            menu.AddMenuOption(player.PlayerName,
                ((playerController, menuOption) => handler(playerController, player)));
        }
    }
}