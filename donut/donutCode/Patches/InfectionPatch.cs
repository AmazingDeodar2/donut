
using System;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace donut.donutCode.Patches;

/// <summary>
/// Infection 卡牌自定义配置。
/// </summary>
public static class InfectionPatch
{
    // ==============================
    // 自定义卡牌名称
    // ==============================

    public const string CustomTitle = "宿醉";

    // ==============================
    // 自定义卡牌描述
    // ==============================

    public const string CustomDescription =
        "你喝得太多了而无法打出！\n回合结束时，受到3点醉酒伤害。没有上吐下泻就算胜利！";

    // ==============================
    // 自定义卡牌插画
    // ==============================

    public const string CustomPortraitPath =
        "res://donut/Cards/infection/replacement.png";

    private static Texture2D? _cachedPortrait;

    public static Texture2D? GetCustomPortrait()
    {
        if (_cachedPortrait != null &&
            GodotObject.IsInstanceValid(_cachedPortrait))
        {
            return _cachedPortrait;
        }

        _cachedPortrait =
            ResourceLoader.Load<Texture2D>(CustomPortraitPath);

        if (_cachedPortrait == null)
        {
            GD.PushError(
                $"[donut] Infection portrait load failed: {CustomPortraitPath}");
        }

        return _cachedPortrait;
    }
}

/// <summary>
/// 取消 Infection 的特殊卡面覆盖层。
/// </summary>
[HarmonyPatch(
    typeof(Infection),
    nameof(Infection.HasBuiltInOverlay),
    MethodType.Getter)]
public static class InfectionOverlayPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref bool __result)
    {
        __result = false;
    }
}

/// <summary>
/// 修改 Infection 卡牌名称。
/// </summary>
[HarmonyPatch(
    typeof(CardModel),
    nameof(CardModel.Title),
    MethodType.Getter)]
public static class InfectionTitlePatch
{
    [HarmonyPostfix]
    public static void Postfix(
        CardModel __instance,
        ref string __result)
    {
        if (__instance is not Infection)
            return;

        __result = InfectionPatch.CustomTitle;
    }
}

/// <summary>
/// 修改 Infection 卡牌描述。
/// 使用内部三参数 GetDescriptionForPile。
/// </summary>
[HarmonyPatch]
public static class InfectionDescriptionPatch
{
    [HarmonyTargetMethod]
    public static MethodBase TargetMethod()
    {
        MethodInfo[] methods =
            typeof(CardModel).GetMethods(
                BindingFlags.Instance |
                BindingFlags.NonPublic);

        foreach (MethodInfo method in methods)
        {
            if (method.Name != "GetDescriptionForPile")
                continue;

            if (method.GetParameters().Length != 3)
                continue;

            GD.Print(
                "[donut] Found internal CardModel.GetDescriptionForPile");

            return method;
        }

        throw new MissingMethodException(
            "Cannot find internal CardModel.GetDescriptionForPile");
    }

    [HarmonyPostfix]
    public static void Postfix(
        CardModel __instance,
        ref string __result)
    {
        if (__instance is not Infection)
            return;

        __result = InfectionPatch.CustomDescription;
    }
}

/// <summary>
/// 将 Infection 的卡牌插画替换为自定义 PNG。
/// </summary>
[HarmonyPatch(
    typeof(CardModel),
    nameof(CardModel.Portrait),
    MethodType.Getter)]
public static class InfectionPortraitPatch
{
    [HarmonyPostfix]
    public static void Postfix(
        CardModel __instance,
        ref Texture2D __result)
    {
        if (__instance is not Infection)
            return;

        try
        {
            Texture2D? texture =
                InfectionPatch.GetCustomPortrait();

            if (texture == null)
                return;

            __result = texture;
        }
        catch (Exception ex)
        {
            GD.PushError(
                $"[donut] Infection portrait patch failed: {ex}");
        }
    }
}
