using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Menu;

namespace JailBreak;

public static class JailBreakUtilities
{
    public static bool IsLegal([NotNullWhen(true)] this CCSPlayerController? player)
    {
        return player != null && player is { IsValid: true, PlayerPawn.IsValid: true } &&
               player.PlayerPawn.Value?.IsValid == true;
    }

    public static void RemoveWeapons(this CCSPlayerController player, bool removeKnife = false)
    {
        // only care if player is valid
        if (!player.IsLegal() || !player.PawnIsAlive)
        {
            return;
        }

        player.RemoveWeapons();

        // dont remove knife its buggy
        if (!removeKnife)
        {
            player.GiveNamedItem(CsItem.Knife);
        }
    }

    public static void SetMoveType(this CCSPlayerController player, MoveType_t moveTypeT)
    {
        // only care if player is valid
        if (!player.IsLegal() || !player.PawnIsAlive)
        {
            return;
        }

        var playerPawn = player.PlayerPawn.Value!;
        playerPawn.ActualMoveType = moveTypeT;
        playerPawn.MoveType = moveTypeT;

        Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_MoveType");
    }

    public static void SetHealth(this CCSPlayerController player, int hp)
    {
        // only care if player is valid
        if (!player.IsLegal() || !player.PawnIsAlive)
        {
            return;
        }

        player.PlayerPawn.Value.Health = hp;

        Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
    }

    public static void SetArmor(this CCSPlayerController player, int armor)
    {
        // only care if player is valid
        if (!player.IsLegal() || !player.PawnIsAlive)
        {
            return;
        }

        player.PlayerPawn.Value.ArmorValue = armor;

        Utilities.SetStateChanged(player, "CCSPlayerPawnBase", "m_ArmorValue");
    }

    public static void OpenWeaponMenu(this CCSPlayerController player)
    {
        // only care if player is valid
        if (!player.IsLegal() || !player.PawnIsAlive)
        {
            return;
        }


        var weaponsInMenu = new HashSet<string>();

        var weaponMenu = new ChatMenu("Выбери оружие");
        var weapons = Enum.GetValues(typeof(CsItem));
        weaponMenu.PostSelectAction = PostSelectAction.Close;

        foreach (CsItem weapon in weapons)
        {
            if (weapon < (CsItem)200 || !weaponsInMenu.Add(weapon.ToString())) continue;

            weaponMenu.AddMenuOption(weapon.ToString(), (controller, option) =>
            {
                controller.GiveNamedItem(weapon);
                MenuManager.CloseActiveMenu(controller);
            });
        }

        MenuManager.OpenChatMenu(player, weaponMenu);
    }

    public static void SetColor(this CCSPlayerController player, Color color)
    {
        // only care if player is valid
        if (!player.IsLegal() || !player.PawnIsAlive)
        {
            return;
        }

        player.PlayerPawn.Value!.Render = color;
        Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseModelEntity", "m_clrRender");
    }


    public static void SetCollisionGroup(this CCSPlayerController player, CollisionGroup collisionGroup)
    {
        // only care if player is valid
        if (!player.IsLegal() || !player.PawnIsAlive)
        {
            return;
        }

        var playerPawn = player.PlayerPawn.Value!;
        playerPawn.Collision.CollisionGroup = (byte)collisionGroup;
        playerPawn.Collision.CollisionAttribute.CollisionGroup = (byte)collisionGroup;


        Utilities.SetStateChanged(playerPawn, "CCollisionProperty", "m_CollisionGroup");
        Utilities.SetStateChanged(playerPawn, "VPhysicsCollisionAttribute_t", "m_nCollisionGroup");
        Utilities.SetStateChanged(playerPawn, "CBaseModelEntity", "m_Collision");
    }


    public static bool IsLegal([NotNullWhen(true)] this CBasePlayerWeapon? weapon)
    {
        return weapon != null && weapon.IsValid;
    }

    public static void SetAmmo(this CCSPlayerController? player, int clip, int reserve)
    {
        if (!player.IsLegal())
        {
            return;
        }

        foreach (var weaponService in player.PlayerPawn.Value.WeaponServices.MyWeapons)
        {
            var weapon = weaponService.Value;

            if (!weapon.IsLegal()) continue;

            if (clip != -1)
            {
                weapon.Clip1 = clip;
                Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
            }

            if (reserve != -1)
            {
                weapon.ReserveAmmo[0] = reserve;
                Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_pReserveAmmo");
            }
        }
    }

    public static void SetAmmo(this CBasePlayerWeapon? weapon, int clip, int reserve)
    {
        if (!weapon.IsLegal())
        {
            return;
        }

        // overide reserve max so it doesn't get clipped when
        // setting "infinite ammo"
        // thanks 1Mack
        var weaponData = weapon.As<CCSWeaponBase>().VData;


        if (weaponData != null)
        {
            // TODO: this overide it for every gun the player has...
            // when not a map gun, this is not a big deal
            // for the reserve ammo it is for the clip though
            /*
                if(clip > weaponData.MaxClip1)
                {
                    weaponData.MaxClip1 = clip;
                }
            */
            if (reserve > weaponData.PrimaryReserveAmmoMax)
            {
                weaponData.PrimaryReserveAmmoMax = reserve;
            }
        }

        if (clip != -1)
        {
            weapon.Clip1 = clip;
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
        }

        if (reserve != -1)
        {
            weapon.ReserveAmmo[0] = reserve;
            Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_pReserveAmmo");
        }
    }

    private static readonly MemoryFunctionVoid<nint, string, nint, nint, nint, int> AcceptInputFunc =
        new(GameData.GetSignature("CEntityInstance_AcceptInput"));

    private static readonly Action<nint, string, nint, nint, nint, int> AcceptInput = AcceptInputFunc.Invoke;

    public static unsafe void FireInput(this CBaseEntity? ent, string input, string param = "",
        CBaseEntity? activator = null, CBaseEntity? caller = null)
    {
        if (ent == null || !ent.IsValid)
            throw new ArgumentNullException(nameof(ent));

        var strBytes = Encoding.ASCII.GetBytes(param + "\0");

        var secondParam = (variant_t*)Marshal.AllocHGlobal(0xB);
        var paramStrPtr = Marshal.AllocHGlobal(strBytes.Length);

        secondParam->fieldType = fieldtype_t.FIELD_STRING;
        secondParam->valuePtr = paramStrPtr;

        Marshal.Copy(strBytes, 0, paramStrPtr, strBytes.Length);

        AcceptInput(ent!.Handle, input, activator?.Handle ?? 0, caller?.Handle ?? 0, (nint)secondParam, 0);

        Marshal.FreeHGlobal(paramStrPtr);
        Marshal.FreeHGlobal((nint)secondParam);
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct variant_t
{
    public nint valuePtr;
    public fieldtype_t fieldType;
    public ushort m_flags;
}