using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace donut.donutCode.Patches;

/// <summary>
/// SUMMON_FORTH 卡牌插画替换。
/// 只修改 Portrait，不影响卡牌其他属性。
/// </summary>
public static class SummonForthPatch
{
    // 自定义卡牌插画路径
    public const string CustomPortraitPath =
        "res://donut/Cards/summon_forth/replacement.png";

    // 缓存图片，避免反复加载
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
                "[donut] SUMMON_FORTH portrait load failed: " +
                CustomPortraitPath);
        }

        return _cachedPortrait;
    }
}

/// <summary>
/// 拦截 CardModel.Portrait，
/// 仅替换 SUMMON_FORTH 的插画。
/// </summary>
[HarmonyPatch(
    typeof(CardModel),
    nameof(CardModel.Portrait),
    MethodType.Getter)]
public static class SummonForthPortraitPatch
{
    [HarmonyPostfix]
    public static void Postfix(
        CardModel __instance,
        ref Texture2D __result)
    {
        // 根据卡牌 ID 精确识别
        if (!string.Equals(
                __instance.Id.Entry,
                "SUMMON_FORTH",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            Texture2D? texture =
                SummonForthPatch.GetCustomPortrait();

            if (texture == null)
                return;

            __result = texture;
        }
        catch (Exception ex)
        {
            GD.PushError(
                "[donut] SUMMON_FORTH portrait patch failed: " +
                ex);
        }
    }
}