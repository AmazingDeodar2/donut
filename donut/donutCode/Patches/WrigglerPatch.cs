
using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace donut.donutCode.Patches;

/// <summary>
/// 将 Wriggler 的原版 Spine 模型替换为静态 PNG。
/// </summary>
[HarmonyPatch(
    typeof(NCreatureVisuals),
    nameof(NCreatureVisuals._Ready))]
public static class WrigglerPatch
{
    private const string TexturePath =
        "res://donut/Monsters/wriggler/replacement.png";

    // WRIGGLER 独立的缩放和位置参数
    private const float SkinScale = 0.13f;

    private static readonly Vector2 SkinOffset =
        new Vector2(0, -60);

    internal static bool IsTarget(NCreatureVisuals visuals)
    {
        string scenePath = visuals.SceneFilePath;

        return !string.IsNullOrEmpty(scenePath) &&
               scenePath.Contains(
                   "wriggler",
                   StringComparison.OrdinalIgnoreCase);
    }

    [HarmonyPostfix]
    public static void Postfix(NCreatureVisuals __instance)
    {
        try
        {
            if (!IsTarget(__instance))
                return;

            GD.Print("[donut] Found Wriggler visuals");

            // 避免重复添加图片
            if (__instance.HasNode("DonutWrigglerReplacement"))
                return;

            Texture2D? texture =
                ResourceLoader.Load<Texture2D>(TexturePath);

            if (texture == null)
            {
                GD.PushError(
                    $"[donut] Wriggler PNG load failed: {TexturePath}");
                return;
            }

            GD.Print(
                $"[donut] Wriggler PNG loaded: " +
                $"{texture.GetWidth()}x{texture.GetHeight()}");

            // 获取原版模型节点
            Node2D originalBody =
                __instance.GetNode<Node2D>("%Visuals");

            GD.Print(
                $"[donut] Wriggler original body: " +
                $"position={originalBody.Position}, " +
                $"scale={originalBody.Scale}");

            // 创建静态 PNG
            Sprite2D sprite = new Sprite2D
            {
                Name = "DonutWrigglerReplacement",
                Texture = texture,
                Centered = true,
                Position = originalBody.Position + SkinOffset,
                Scale = Vector2.One * SkinScale,
                ZIndex = 10,
                Visible = true
            };

            // 添加到怪物视觉节点
            __instance.AddChild(sprite);

            // 隐藏原版 Spine 模型
            originalBody.Visible = false;

            GD.Print(
                $"[donut] Wriggler skin applied! " +
                $"GlobalPosition={sprite.GlobalPosition}, " +
                $"Visible={sprite.IsVisibleInTree()}");
        }
        catch (Exception ex)
        {
            GD.PushError(
                $"[donut] Wriggler patch failed: {ex}");
        }
    }
}

/// <summary>
/// 防止游戏重新显示 Wriggler 原版模型。
/// </summary>
[HarmonyPatch(
    typeof(NCreatureVisuals),
    nameof(NCreatureVisuals.UpdatePhobiaMode))]
public static class WrigglerVisibilityPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCreatureVisuals __instance)
    {
        try
        {
            if (!WrigglerPatch.IsTarget(__instance))
                return;

            // 只有 PNG 已经创建时才隐藏原版模型
            if (!__instance.HasNode("DonutWrigglerReplacement"))
                return;

            Node2D? originalBody =
                __instance.GetNodeOrNull<Node2D>("%Visuals");

            if (originalBody == null)
                return;

            originalBody.Visible = false;
        }
        catch (Exception ex)
        {
            GD.PushError(
                $"[donut] Wriggler visibility patch failed: {ex}");
        }
    }
}
