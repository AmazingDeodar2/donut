
using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace donut.donutCode.Patches;

/// <summary>
/// EXOSKELETON 随机 PNG 替换。
/// 四张图片随机排列，依次分配，避免前四次重复。
/// </summary>
[HarmonyPatch(typeof(NCreatureVisuals), nameof(NCreatureVisuals._Ready))]
public static class ExoskeletonPatch
{
    // ============================
    // 图片设置
    // ============================

    private const float SkinScale = 0.12f;

    private static readonly Vector2 SkinOffset =
        new Vector2(0, -50);

    private const string SpriteName =
        "DonutExoskeletonReplacement";

    private static readonly string[] TexturePaths =
    {
        "res://donut/Monsters/exoskeleton/01.png",
        "res://donut/Monsters/exoskeleton/02.png",
        "res://donut/Monsters/exoskeleton/03.png",
        "res://donut/Monsters/exoskeleton/04.png"
    };

    // ============================
    // 随机分配状态
    // ============================

    private sealed class AssignmentState
    {
        public int NextIndex = 0;

        public int[] Order = { 0, 1, 2, 3 };
    }

    private static readonly Dictionary<ulong, AssignmentState>
        SceneAssignments = new();

    internal static bool IsTarget(NCreatureVisuals visuals)
    {
        string scenePath = visuals.SceneFilePath;

        return !string.IsNullOrEmpty(scenePath) &&
               scenePath.Contains(
                   "exoskeleton",
                   StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 获取下一张随机分配的 PNG。
    /// </summary>
    private static string GetTexturePath(NCreatureVisuals visuals)
    {
        ulong sceneId =
            visuals.GetTree().CurrentScene?.GetInstanceId() ?? 0;

        if (!SceneAssignments.TryGetValue(sceneId, out var state))
        {
            state = new AssignmentState();

            Shuffle(state.Order);

            SceneAssignments[sceneId] = state;
        }

        int index =
            state.Order[state.NextIndex % state.Order.Length];

        state.NextIndex++;

        return TexturePaths[index];
    }

    /// <summary>
    /// Fisher-Yates 洗牌算法。
    /// </summary>
    private static void Shuffle(int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);

            (array[i], array[j]) =
                (array[j], array[i]);
        }
    }

    // ============================
    // 替换怪物模型
    // ============================

    [HarmonyPostfix]
    public static void Postfix(NCreatureVisuals __instance)
    {
        try
        {
            if (!IsTarget(__instance))
                return;

            if (__instance.HasNode(SpriteName))
                return;

            Node2D originalBody =
                __instance.GetNode<Node2D>("%Visuals");

            string texturePath = GetTexturePath(__instance);

            Texture2D? texture =
                ResourceLoader.Load<Texture2D>(texturePath);

            if (texture == null)
            {
                GD.PushError(
                    $"[donut] EXOSKELETON PNG load failed: {texturePath}");
                return;
            }

            Sprite2D sprite = new Sprite2D
            {
                Name = SpriteName,

                Texture = texture,

                Centered = true,

                Position = originalBody.Position + SkinOffset,

                Scale = Vector2.One * SkinScale,

                ZIndex = 10,

                Visible = true
            };

            __instance.AddChild(sprite);

            // 隐藏原版 Spine 模型
            originalBody.Visible = false;

            GD.Print(
                $"[donut] EXOSKELETON skin applied: {texturePath}");
        }
        catch (Exception ex)
        {
            GD.PushError(
                $"[donut] EXOSKELETON patch failed: {ex}");
        }
    }
}

/// <summary>
/// 防止 UpdatePhobiaMode 恢复原版模型显示。
/// </summary>
[HarmonyPatch(
    typeof(NCreatureVisuals),
    nameof(NCreatureVisuals.UpdatePhobiaMode))]
public static class ExoskeletonVisibilityPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCreatureVisuals __instance)
    {
        try
        {
            if (!ExoskeletonPatch.IsTarget(__instance))
                return;

            if (!__instance.HasNode("DonutExoskeletonReplacement"))
                return;

            Node2D? originalBody =
                __instance.GetNodeOrNull<Node2D>("%Visuals");

            if (originalBody != null)
                originalBody.Visible = false;
        }
        catch (Exception ex)
        {
            GD.PushError(
                $"[donut] EXOSKELETON visibility patch failed: {ex}");
        }
    }
}
