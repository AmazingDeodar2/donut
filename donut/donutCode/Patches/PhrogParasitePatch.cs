
using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace donut.donutCode.Patches;

/// <summary>
/// 将 PhrogParasite 的 Spine 外观替换为静态 PNG。
/// 不修改怪物的攻击、技能或战斗逻辑。
/// </summary>
[HarmonyPatch(
    typeof(NCreatureVisuals),
    nameof(NCreatureVisuals._Ready))]
public static class PhrogParasitePatch
{
    private const string TexturePath =
        "res://donut/Monsters/phrog_parasite/replacement.png";

    /// <summary>
    /// 判断当前视觉节点是否属于 PhrogParasite。
    /// </summary>
    internal static bool IsTarget(NCreatureVisuals visuals)
    {
        string scenePath = visuals.SceneFilePath;

        return !string.IsNullOrEmpty(scenePath) &&
               scenePath.Contains(
                   "phrog_parasite",
                   StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 怪物视觉节点初始化完成后执行。
    /// </summary>
    [HarmonyPostfix]
    public static void Postfix(NCreatureVisuals __instance)
    {
        try
        {
            if (!IsTarget(__instance))
                return;

            GD.Print("[donut] Found PhrogParasite visuals");

            // 防止重复创建 PNG
            if (__instance.HasNode("DonutReplacement"))
                return;

            // 加载 PNG
            Texture2D? texture =
                ResourceLoader.Load<Texture2D>(TexturePath);

            if (texture == null)
            {
                GD.PushError(
                    $"[donut] PNG load failed: {TexturePath}");
                return;
            }

            GD.Print(
                $"[donut] PNG loaded: " +
                $"{texture.GetWidth()}x{texture.GetHeight()}");

            // 获取原版 Spine 身体节点
            Node2D originalBody =
                __instance.GetNode<Node2D>("%Visuals");

            GD.Print(
                $"[donut] Original body: {originalBody.Name}, " +
                $"position={originalBody.Position}, " +
                $"scale={originalBody.Scale}");

            // 创建静态图片
            Sprite2D sprite = new Sprite2D
            {
                Name = "DonutReplacement",
                Texture = texture,
                Centered = true,
                Position = originalBody.Position + new Vector2(0, -160),
                Scale = new Vector2(0.40f, 0.40f),
                ZIndex = 2,
                Visible = true
            };

            // PNG 和原版 Spine 是兄弟节点
            __instance.AddChild(sprite);

            // 隐藏原版 Spine
            originalBody.Visible = false;

            GD.Print(
                $"[donut] Sprite added. " +
                $"Parent={sprite.GetParent().Name}, " +
                $"GlobalPosition={sprite.GlobalPosition}, " +
                $"Visible={sprite.IsVisibleInTree()}");

            GD.Print(
                "[donut] PhrogParasite skin applied!");
        }
        catch (Exception ex)
        {
            GD.PushError(
                $"[donut] PhrogParasite patch failed: {ex}");
        }
    }
}

/// <summary>
/// 防止 UpdatePhobiaMode 重新显示原版 Spine。
/// </summary>
[HarmonyPatch(
    typeof(NCreatureVisuals),
    nameof(NCreatureVisuals.UpdatePhobiaMode))]
public static class PhrogParasiteVisibilityPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCreatureVisuals __instance)
    {
        try
        {
            if (!PhrogParasitePatch.IsTarget(__instance))
                return;

            Node2D? originalBody =
                __instance.GetNodeOrNull<Node2D>("%Visuals");

            if (originalBody == null)
                return;

            // 再次隐藏原版 Spine
            originalBody.Visible = false;

            GD.Print(
                "[donut] PhrogParasite Spine hidden again");
        }
        catch (Exception ex)
        {
            GD.PushError(
                $"[donut] Visibility patch failed: {ex}");
        }
    }
}
