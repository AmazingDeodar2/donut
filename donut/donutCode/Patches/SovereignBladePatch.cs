using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace donut.donutCode.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Portrait), MethodType.Getter)]
public static class SovereignBladePatch
{
    private const string CardId = "SOVEREIGN_BLADE";

    private const string TexturePath =
        "res://donut/Cards/sovereign_blade.png";

    private static Texture2D? _customPortrait;

    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, ref Texture2D __result)
    {
        if (__instance.Id.Entry != CardId)
            return;

        _customPortrait ??= GD.Load<Texture2D>(TexturePath);

        if (_customPortrait != null)
        {
            __result = _customPortrait;
        }
        else
        {
            GD.PushWarning(
                "[donut] Sovereign Blade portrait not found: "
                + TexturePath
            );
        }
    }
}