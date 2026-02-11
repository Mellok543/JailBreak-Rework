using CounterStrikeSharp.API.Modules.Entities.Constants;

namespace JailBreak.Games.GameDays;

public class ArmRaceLevel
{
    public required List<CsItem> Weapons { get; set; }
    public List<CsItem>? ExtraWeapons { get; set; }
    public int Kills { get; init; }

    public CsItem GetRandomItem => Weapons == null! ? CsItem.Knife : Weapons[Random.Shared.Next(0, Weapons.Count)];
}