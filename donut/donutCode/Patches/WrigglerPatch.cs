
using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace donut.donutCode.Patches;

/// <summary>
/// 为每只 Wriggler 分配不同的静态 PNG。
/// 每组四只使用四张不同图片。
/// </summary>
[HarmonyPatch(
    typeof(NCreatureVisuals),
    nameof(NCreatureVisuals._Ready))]
public static class WrigglerPatch
{
    private const float SkinScale = 0.10f;

    private static readonly Vector2 SkinOffset =
        new Vector2(0, -70);

    private static readonly string[] TexturePaths =
    {
        "res://donut/Monsters/wriggler/01.png",
        "res://donut/Monsters/wriggler/02.png",
        "res://donut/Monsters/wriggler/03.png",
        "res://donut/Monsters/wriggler/04.png"
    };

    // 按场景保存分配状态
    private sealed class AssignmentState
    {
        public int NextIndex;
        public int[] Order = { 0, 1, 2, 3 };
    }

    private static readonly Dictionary<ulong, AssignmentState>
        SceneAssignments = new();

    internal static bool IsTarget(NCreatureVisuals visuals)
    {
        string scenePath = visuals.SceneFilePath;

        return !string.IsNullOrEmpty(scenePath) &&
               scenePath.Contains(
                   "wriggler",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static string GetTexturePath(NCreatureVisuals visuals)
    {
        // 使用当前场景根节点的实例 ID 区分战斗
        ulong sceneId =
            visuals.GetTree().CurrentScene?.GetInstanceId() ?? 0;

        if (!SceneAssignments.TryGetValue(
                sceneId, out var state))
        {
            state = new AssignmentState();

            // Fisher-Yates 洗牌
            for (int i = state.Order.Length - 1; i > 0; i--)
            {
                int j = Random.Shared.Next(i + 1);

                (state.Order[i], state.Order[j]) =
                    (state.Order[j], state.Order[i]);
            }

            SceneAssignments[sceneId] = state;
        }

        int index = state.Order[
            state.NextIndex % state.Order.Length];

        state.NextIndex++;

        return TexturePaths[index];
    }

    [HarmonyPostfix]
    public static void Postfix(NCreatureVisuals __instance)
    {
        try
        {
            if (!IsTarget(__instance))
                return;

            if (__instance.HasNode("DonutWrigglerReplacement"))
                return;

            string texturePath = GetTexturePath(__instance);

            Texture2D? texture =
                ResourceLoader.Load<Texture2D>(texturePath);

            if (texture == null)
            {
                GD.PushError(
                    $"[donut] Wriggler PNG load failed: {texturePath}");
                return;
            }

            Node2D originalBody =
                __instance.GetNode<Node2D>("%Visuals");

            Sprite2D sprite = new Sprite2D
            {
                Name = "DonutWrigglerReplacement",
                Texture = texture,
                Centered = true,
                Position = originalBody.Position + SkinOffset,
                Scale = Vector2.One * SkinScale,
                ZIndex = 2,
                Visible = true
            };

            __instance.AddChild(sprite);

            // 隐藏原版 Spine
            originalBody.Visible = false;

            GD.Print(
                $"[donut] Wriggler skin applied: {texturePath}");
        }
        catch (Exception ex)
        {
            GD.PushError(
                $"[donut] Wriggler patch failed: {ex}");
        }
    }
}

/// <summary>
/// 防止原版 Wriggler 模型重新显示。
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

            if (!__instance.HasNode("DonutWrigglerReplacement"))
                return;

            Node2D? originalBody =
                __instance.GetNodeOrNull<Node2D>("%Visuals");

            if (originalBody != null)
                originalBody.Visible = false;
        }
        catch (Exception ex)
        {
            GD.PushError(
                $"[donut] Wriggler visibility patch failed: {ex}");
        }
    }
}
