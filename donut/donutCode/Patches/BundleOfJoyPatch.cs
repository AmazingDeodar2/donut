using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace donut.donutCode.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Portrait), MethodType.Getter)]
public static class BundleOfJoyPatch
{
    private const string CardId = "BUNDLE_OF_JOY";

    private const string TexturePath =
        "res://donut/Cards/bundle_of_joy.png";

    private static Texture2D? _customPortrait;

    [HarmonyPostfix]
    public static void Postfix(
        CardModel __instance,
        ref Texture2D __result)
    {
        if (__instance.Id.Entry != CardId)
            return;

        _customPortrait ??= GD.Load<Texture2D>(TexturePath);

        if (_customPortrait != null)
            __result = _customPortrait;
    }
}