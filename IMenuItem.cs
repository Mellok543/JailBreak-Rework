using CounterStrikeSharp.API.Core;

namespace JailBreak.Menus;

public interface IMenuItem
{
    public abstract string Name { get; set; }
    public void InternalSelect(CCSPlayerController player);
}