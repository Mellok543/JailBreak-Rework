using System.Reflection;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using Microsoft.Extensions.DependencyInjection;

namespace JailBreak.Menus;

public class JailBreakMenusManager : IFeature
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<Type, ChatMenu> _menus = new();

    public JailBreakMenusManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void RegisterMenu(Type menuType, string title, Action<CCSPlayerController, IMenuItem, Type> action)
    {
        var menu = new ChatMenu(title);

        foreach (var type in Assembly.GetExecutingAssembly().GetTypes()
                     .Where(t => !t.IsAbstract &&
                                 !t.IsInterface && t.IsSubclassOf(menuType)))
        {
            var menuItem = (IMenuItem)_serviceProvider.GetRequiredService(type);


            menu.AddMenuOption(menuItem.Name,
                (controller, option) =>
                {
                    action.Invoke(controller, menuItem, type);
                    //((LRGame)_provider.GetRequiredService(type)).OnSelectedInternal(controller);
                });
        }

        _menus[menuType] = menu;
    }

    public ChatMenu? GetMenu(Type menuType)
    {
        if (_menus.TryGetValue(menuType, out var menu))
            return menu;

        return null;
    }
}