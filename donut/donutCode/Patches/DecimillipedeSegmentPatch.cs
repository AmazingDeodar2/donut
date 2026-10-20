
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace donut.donutCode.Patches;

/// <summary>
/// DECIMILLIPEDE_SEGMENT 三节段 PNG 皮肤替换。
///
/// 功能：
/// 1. 三张 PNG 随机不重复分配
/// 2. 死亡和再生不更换图片
/// 3. 隐藏原版 Spine 模型
/// 4. 不修改原版战斗逻辑
/// </summary>
[HarmonyPatch(typeof(NCreatureVisuals), nameof(NCreatureVisuals._Ready))]
public static class DecimillipedeSegmentPatch
{
    // =====================================
    // 图片配置
    // =====================================

    private static readonly string[] TexturePaths =
    {
        "res://donut/Monsters/decimillipede/01.png",
        "res://donut/Monsters/decimillipede/02.png",
        "res://donut/Monsters/decimillipede/03.png"
    };

    // 默认缩放比例
    private const float DefaultScale = 0.35f;

    // 默认位置偏移
    private static readonly Vector2 DefaultOffset =
        new Vector2(0, -80);

    // 每张 PNG 可以单独设置缩放
    private static readonly float[] SkinScales =
    {
        0.20f, // 01.png
        0.20f, // 02.png
        0.20f  // 03.png
    };

    // 每张 PNG 可以单独设置位置
    private static readonly Vector2[] SkinOffsets =
    {
        new Vector2(0, -150), // 01.png
        new Vector2(400, -150), // 02.png
        new Vector2(-400, -150)  // 03.png
    };

    internal const string SpriteName =
        "DonutDecimillipedeReplacement";

    // =====================================
    // 战斗内随机分配状态
    // =====================================

    private sealed class AssignmentState
    {
        public int NextIndex;

        public int[] Order = { 0, 1, 2 };

        public Dictionary<ulong, int> Assignments = new();
    }

    private static readonly Dictionary<ulong, AssignmentState>
        BattleAssignments = new();

    private static readonly object AssignmentLock = new();

    // =====================================
    // 怪物识别
    // =====================================

    /// <summary>
    /// 识别 DecimillipedeSegment 及其派生节段。
    /// </summary>
    internal static bool IsTarget(NCreatureVisuals visuals)
    {
        string path = visuals.SceneFilePath;

        if (string.IsNullOrEmpty(path))
            return false;

        return path.Contains(
            "decimillipede",
            StringComparison.OrdinalIgnoreCase);
    }

    // =====================================
    // 随机洗牌
    // =====================================

    private static void Shuffle(int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);

            (array[i], array[j]) =
                (array[j], array[i]);
        }
    }

    /// <summary>
    /// 为一个节段分配图片。
    /// 同一节段重复调用时，返回相同的图片。
    /// </summary>
    private static int GetSkinIndex(NCreatureVisuals visuals)
    {
        ulong battleId =
            visuals.GetTree().CurrentScene?.GetInstanceId() ?? 0;

        ulong visualId = visuals.GetInstanceId();

        lock (AssignmentLock)
        {
            if (!BattleAssignments.TryGetValue(
                    battleId, out AssignmentState? state))
            {
                state = new AssignmentState();

                Shuffle(state.Order);

                BattleAssignments[battleId] = state;

                GD.Print(
                    "[donut] DECIMILLIPEDE new random skin order: " +
                    string.Join(", ", state.Order.Select(x => x + 1)));
            }

            if (state.Assignments.TryGetValue(
                    visualId, out int existingIndex))
            {
                return existingIndex;
            }

            // 三张图循环分配。
            // 正常三节段战斗中，前三次不会重复。
            int skinIndex =
                state.Order[state.NextIndex % state.Order.Length];

            state.NextIndex++;

            state.Assignments[visualId] = skinIndex;

            return skinIndex;
        }
    }

    // =====================================
    // 创建 PNG 替换模型
    // =====================================

    [HarmonyPostfix]
    public static void Postfix(NCreatureVisuals __instance)
    {
        try
        {
            if (!IsTarget(__instance))
                return;

            // 防止重复创建
            if (__instance.HasNode(SpriteName))
                return;

            Node2D? originalBody =
                __instance.GetNodeOrNull<Node2D>("%Visuals");

            if (originalBody == null)
            {
                GD.PushWarning(
                    "[donut] DECIMILLIPEDE original visuals not found");
                return;
            }

            int skinIndex = GetSkinIndex(__instance);

            string texturePath = TexturePaths[skinIndex];

            Texture2D? texture =
                ResourceLoader.Load<Texture2D>(texturePath);

            if (texture == null)
            {
                GD.PushError(
                    "[donut] DECIMILLIPEDE texture load failed: " +
                    texturePath);
                return;
            }

            float scale =
                skinIndex < SkinScales.Length
                    ? SkinScales[skinIndex]
                    : DefaultScale;

            Vector2 offset =
                skinIndex < SkinOffsets.Length
                    ? SkinOffsets[skinIndex]
                    : DefaultOffset;

            Sprite2D sprite = new Sprite2D
            {
                Name = SpriteName,

                Texture = texture,

                Centered = true,

                Position = originalBody.Position + offset,

                Scale = Vector2.One * scale,

                ZIndex = 2,

                Visible = true
            };

            // 添加自定义 PNG
            __instance.AddChild(sprite);

            // 隐藏原版 Spine
            originalBody.Visible = false;

            GD.Print(
                "[donut] DECIMILLIPEDE segment skin applied: " +
                texturePath);
        }
        catch (Exception ex)
        {
            GD.PushError(
                "[donut] DECIMILLIPEDE skin patch failed: " +
                ex);
        }
    }

    // =====================================
    // 维持 PNG 显示
    // =====================================

    internal static void KeepReplacementVisible(
        NCreatureVisuals visuals)
    {
        if (!IsTarget(visuals))
            return;

        if (!visuals.HasNode(SpriteName))
            return;

        Node2D? originalBody =
            visuals.GetNodeOrNull<Node2D>("%Visuals");

        if (originalBody != null)
            originalBody.Visible = false;

        Sprite2D? sprite =
            visuals.GetNodeOrNull<Sprite2D>(SpriteName);

        if (sprite != null)
            sprite.Visible = true;
    }
}

/// <summary>
/// 游戏切换视觉模式时，继续隐藏原版模型。
/// </summary>
[HarmonyPatch(
    typeof(NCreatureVisuals),
    nameof(NCreatureVisuals.UpdatePhobiaMode))]
public static class DecimillipedePhobiaPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCreatureVisuals __instance)
    {
        DecimillipedeSegmentPatch.KeepReplacementVisible(
            __instance);
    }
}
