// FlatCat - 动画内核：缓动曲线、关键帧片段、播放器
// Copyright (c) 2026 认知觉醒能量站 (微信公众号同名)。MIT License，见 LICENSE。
using System;

namespace FlatCat
{
    internal enum Ease { Linear, Out, InOut, Back }

    internal struct Key
    {
        public float T;
        public float V;
        public Key(float t, float v) { T = t; V = v; }
    }

    /// <summary>一条参数曲线：若干关键帧 + 一段缓动。</summary>
    internal sealed class Track
    {
        public readonly string Param;
        public readonly Ease Ease;
        public readonly Key[] Keys;

        public Track(string param, Ease ease, params Key[] keys)
        {
            Param = param;
            Ease = ease;
            Keys = keys;
        }

        public float Eval(float tn)
        {
            int n = Keys.Length;
            if (n == 0) return float.NaN;
            if (tn <= Keys[0].T) return Keys[0].V;
            if (tn >= Keys[n - 1].T) return Keys[n - 1].V;

            for (int i = 1; i < n; i++)
            {
                if (tn <= Keys[i].T)
                {
                    float span = Keys[i].T - Keys[i - 1].T;
                    float f = span <= 0.0001f ? 1f : (tn - Keys[i - 1].T) / span;
                    f = Shape(f, Ease);
                    return Keys[i - 1].V + (Keys[i].V - Keys[i - 1].V) * f;
                }
            }
            return Keys[n - 1].V;
        }

        private static float Shape(float f, Ease e)
        {
            if (f < 0f) f = 0f;
            if (f > 1f) f = 1f;
            switch (e)
            {
                case Ease.Out:
                    return 1f - (1f - f) * (1f - f);
                case Ease.InOut:
                    return f < 0.5f ? 2f * f * f : 1f - 2f * (1f - f) * (1f - f);
                case Ease.Back:
                    {
                        // 轻微过冲再回落：先冲过头 8%，再收回来
                        float s = 1.14f;
                        float u = f - 1f;
                        return 1f + (s + 1f) * u * u * u + s * u * u;
                    }
                default:
                    return f;
            }
        }
    }

    /// <summary>一个动作片段：同时驱动多条曲线，可循环或一次性。</summary>
    internal sealed class Clip
    {
        public readonly string Name;
        public readonly float Duration;
        public readonly bool Loop;
        public readonly Track[] Tracks;

        public Clip(string name, float duration, bool loop, params Track[] tracks)
        {
            Name = name;
            Duration = duration <= 0.01f ? 0.01f : duration;
            Loop = loop;
            Tracks = tracks;
        }

        /// <summary>取样；该片段没有定义这个参数时返回 NaN。</summary>
        public float Sample(string param, float time)
        {
            float t = time / Duration;
            if (Loop) t = t - (float)Math.Floor(t);
            else if (t > 1f) t = 1f;

            for (int i = 0; i < Tracks.Length; i++)
            {
                if (Tracks[i].Param == param) return Tracks[i].Eval(t);
            }
            return float.NaN;
        }
    }

    /// <summary>叠两层片段（基础循环 + 一次性动作），动作优先。</summary>
    internal sealed class Animator
    {
        public Clip Base;
        public float BaseTime;
        public Clip Action;
        public float ActionTime;

        public void SetBase(Clip clip)
        {
            if (Base == clip) return;
            Base = clip;
            BaseTime = 0f;
        }

        public void Play(Clip clip)
        {
            Action = clip;
            ActionTime = 0f;
        }

        public bool ActionActive { get { return Action != null; } }

        public void Update(float dt)
        {
            BaseTime += dt;
            if (Action != null)
            {
                ActionTime += dt;
                if (!Action.Loop && ActionTime > Action.Duration + 0.12f)
                {
                    Action = null;
                    ActionTime = 0f;
                }
            }
        }

        /// <summary>取参数目标值：动作层优先，其次基础层，最后用 fallback。</summary>
        public float Get(string param, float fallback)
        {
            if (Action != null)
            {
                float v = Action.Sample(param, ActionTime);
                if (!float.IsNaN(v)) return v;
            }
            if (Base != null)
            {
                float v2 = Base.Sample(param, BaseTime);
                if (!float.IsNaN(v2)) return v2;
            }
            return fallback;
        }
    }

    /// <summary>内置片段库。参数名与 CatAnim 字段一一对应。</summary>
    internal static class AnimLib
    {
        public const string Idle = "idle";
        public const string Sleep = "sleep";
        public const string Pet = "pet";
        public const string Alert = "alert";
        public const string Focus = "focus";
        public const string Lick = "lick";
        public const string Tilt = "tilt";
        public const string Drag = "drag";
        public const string TypeLeft = "typeLeft";
        public const string TypeRight = "typeRight";
        public const string TypeBoth = "typeBoth";
        public const string Wake = "wake";
        public const string Happy = "happy";
        public const string Drop = "drop";
        public const string Yawn = "yawn";
        public const string Chime = "chime";

        private static readonly Clip[] _all = Build();

        public static Clip[] All { get { return _all; } }

        public static Clip ByName(string name)
        {
            for (int i = 0; i < _all.Length; i++)
                if (_all[i].Name == name) return _all[i];
            return null;
        }

        private static Clip[] Build()
        {
            Clip[] list = new Clip[16];

            // 基础：呼吸交给正弦，这里只让尾巴缓慢左右摆
            list[0] = new Clip(Idle, 3.4f, true,
                new Track("tailAngle", Ease.InOut, new Key(0f, -5f), new Key(0.5f, 9f), new Key(1f, -5f)),
                new Track("earAngle", Ease.InOut, new Key(0f, 0f), new Key(0.5f, 1.6f), new Key(1f, 0f)));

            // 睡着：闭眼、蜷一点、耳朵放松
            list[1] = new Clip(Sleep, 4.2f, true,
                new Track("eyeOpen", Ease.Out, new Key(0f, 0.04f)),
                new Track("sleep", Ease.Out, new Key(0f, 1f)),
                new Track("bodyDip", Ease.Out, new Key(0f, 1f)),
                new Track("headTilt", Ease.Out, new Key(0f, 8f)),
                new Track("earAngle", Ease.InOut, new Key(0f, 3f), new Key(0.5f, 5f), new Key(1f, 3f)),
                new Track("mouthOpen", Ease.Out, new Key(0f, 0f)),
                new Track("tailAngle", Ease.InOut, new Key(0f, -8f), new Key(0.5f, -2f), new Key(1f, -8f)));

            // 摸头：笑眼 + 腮红 + 头歪
            list[2] = new Clip(Pet, 1.6f, true,
                new Track("eyeHappy", Ease.Out, new Key(0f, 1f)),
                new Track("eyeOpen", Ease.Out, new Key(0f, 0.18f)),
                new Track("headTilt", Ease.InOut, new Key(0f, -6f), new Key(0.5f, -3f), new Key(1f, -6f)),
                new Track("earAngle", Ease.InOut, new Key(0f, 5f), new Key(0.5f, 8f), new Key(1f, 5f)),
                new Track("cheek", Ease.Out, new Key(0f, 0.85f)),
                new Track("mouthSmile", Ease.Out, new Key(0f, 0.7f)),
                new Track("headYaw", Ease.InOut, new Key(0f, 2f), new Key(0.5f, -2f), new Key(1f, 2f)));

            // 提醒：瞪眼 + 张嘴 + 耳朵竖
            list[3] = new Clip(Alert, 0.9f, true,
                new Track("eyeWide", Ease.Out, new Key(0f, 1f)),
                new Track("eyeOpen", Ease.Out, new Key(0f, 1f)),
                new Track("mouthOpen", Ease.Out, new Key(0f, 0.45f)),
                new Track("earAngle", Ease.InOut, new Key(0f, -8f), new Key(0.5f, -13f), new Key(1f, -8f)),
                new Track("headTilt", Ease.Out, new Key(0f, -3f)),
                new Track("tailAngle", Ease.InOut, new Key(0f, 14f), new Key(0.5f, -6f), new Key(1f, 14f)));

            // 专注：盯屏幕、瞳孔偏下、嘴闭紧
            list[4] = new Clip(Focus, 2.8f, true,
                new Track("eyeOpen", Ease.Out, new Key(0f, 0.92f)),
                new Track("pupilY", Ease.Out, new Key(0f, 0.7f)),
                new Track("headTilt", Ease.InOut, new Key(0f, 1f), new Key(0.5f, 3f), new Key(1f, 1f)),
                new Track("mouthSmile", Ease.Out, new Key(0f, 0f)));

            // 舔爪：抬左爪、低头、伸舌
            list[5] = new Clip(Lick, 1.3f, false,
                new Track("leftPaw", Ease.Out, new Key(0f, 0f), new Key(0.25f, 0.9f), new Key(0.75f, 0.9f), new Key(1f, 0f)),
                new Track("headTilt", Ease.InOut, new Key(0f, 0f), new Key(0.3f, -9f), new Key(0.8f, -9f), new Key(1f, 0f)),
                new Track("tongue", Ease.InOut, new Key(0f, 0f), new Key(0.3f, 0.9f), new Key(0.8f, 0.9f), new Key(1f, 0f)),
                new Track("eyeOpen", Ease.InOut, new Key(0f, 1f), new Key(0.3f, 0.45f), new Key(0.8f, 0.45f), new Key(1f, 1f)));

            // 歪头
            list[6] = new Clip(Tilt, 1.7f, false,
                new Track("headTilt", Ease.InOut, new Key(0f, 0f), new Key(0.3f, 11f), new Key(0.7f, 11f), new Key(1f, 0f)),
                new Track("headYaw", Ease.InOut, new Key(0f, 0f), new Key(0.3f, 5f), new Key(0.7f, 5f), new Key(1f, 0f)),
                new Track("earAngle", Ease.InOut, new Key(0f, 0f), new Key(0.3f, 4f), new Key(1f, 0f)));

            // 被拖走：瞪眼 + 身体后仰
            list[7] = new Clip(Drag, 0.6f, true,
                new Track("eyeWide", Ease.Out, new Key(0f, 0.6f)),
                new Track("headTilt", Ease.Out, new Key(0f, 7f)),
                new Track("earAngle", Ease.Out, new Key(0f, -6f)),
                new Track("tailAngle", Ease.Out, new Key(0f, -14f)));

            // 打字：抬单爪
            list[8] = new Clip(TypeLeft, 0.34f, false,
                new Track("leftPaw", Ease.Back, new Key(0f, 0f), new Key(0.35f, 1f), new Key(1f, 0f)),
                new Track("eyeOpen", Ease.Out, new Key(0f, 0.9f)),
                new Track("headTilt", Ease.Out, new Key(0f, 2f)));

            list[9] = new Clip(TypeRight, 0.34f, false,
                new Track("rightPaw", Ease.Back, new Key(0f, 0f), new Key(0.35f, 1f), new Key(1f, 0f)),
                new Track("eyeOpen", Ease.Out, new Key(0f, 0.9f)),
                new Track("headTilt", Ease.Out, new Key(0f, 2f)));

            list[10] = new Clip(TypeBoth, 0.4f, false,
                new Track("leftPaw", Ease.Back, new Key(0f, 0f), new Key(0.4f, 1f), new Key(1f, 0f)),
                new Track("rightPaw", Ease.Back, new Key(0f, 0f), new Key(0.4f, 1f), new Key(1f, 0f)),
                new Track("mouthOpen", Ease.InOut, new Key(0f, 0f), new Key(0.4f, 0.5f), new Key(1f, 0f)));

            // 惊醒：睁大眼 + 身体弹起
            list[11] = new Clip(Wake, 0.75f, false,
                new Track("eyeWide", Ease.Out, new Key(0f, 1f), new Key(0.6f, 0.25f), new Key(1f, 0f)),
                new Track("eyeOpen", Ease.Out, new Key(0f, 1f)),
                new Track("bodyDip", Ease.Back, new Key(0f, 0.8f), new Key(0.5f, 0f), new Key(1f, 0f)),
                new Track("earAngle", Ease.Out, new Key(0f, -10f), new Key(1f, 0f)));

            // 开心：笑眼 + 张嘴笑 + 腮红
            list[12] = new Clip(Happy, 1.1f, false,
                new Track("eyeHappy", Ease.Out, new Key(0f, 1f), new Key(0.75f, 1f), new Key(1f, 0f)),
                new Track("mouthSmile", Ease.Out, new Key(0f, 1f), new Key(0.75f, 1f), new Key(1f, 0f)),
                new Track("mouthOpen", Ease.InOut, new Key(0f, 0.3f), new Key(0.5f, 0.55f), new Key(1f, 0f)),
                new Track("cheek", Ease.Out, new Key(0f, 1f), new Key(0.8f, 1f), new Key(1f, 0f)),
                new Track("earAngle", Ease.InOut, new Key(0f, 6f), new Key(0.5f, 9f), new Key(1f, 0f)));

            // 落地：压扁回弹
            list[13] = new Clip(Drop, 0.5f, false,
                new Track("squash", Ease.Back, new Key(0f, 1f), new Key(0.45f, 0f), new Key(1f, 0f)),
                new Track("bodyDip", Ease.Out, new Key(0f, 0.6f), new Key(0.4f, 0f)),
                new Track("eyeHappy", Ease.InOut, new Key(0f, 0.6f), new Key(0.4f, 0.6f), new Key(1f, 0f)));

            // 打哈欠
            list[14] = new Clip(Yawn, 2.2f, false,
                new Track("mouthOpen", Ease.InOut, new Key(0f, 0f), new Key(0.3f, 0.95f), new Key(0.75f, 0.95f), new Key(1f, 0f)),
                new Track("eyeOpen", Ease.InOut, new Key(0f, 1f), new Key(0.3f, 0.1f), new Key(0.75f, 0.1f), new Key(1f, 1f)),
                new Track("headTilt", Ease.InOut, new Key(0f, 0f), new Key(0.4f, 9f), new Key(1f, 0f)),
                new Track("tongue", Ease.InOut, new Key(0f, 0f), new Key(0.4f, 0.5f), new Key(0.8f, 0f)),
                new Track("earAngle", Ease.Out, new Key(0f, 4f), new Key(1f, 0f)));

            // 整点/提醒音时抖一下
            list[15] = new Clip(Chime, 0.8f, false,
                new Track("earAngle", Ease.InOut, new Key(0f, -12f), new Key(0.4f, -4f), new Key(1f, 0f)),
                new Track("mouthOpen", Ease.InOut, new Key(0f, 0.5f), new Key(0.5f, 0.2f), new Key(1f, 0f)),
                new Track("eyeOpen", Ease.Out, new Key(0f, 1f)),
                new Track("headTilt", Ease.InOut, new Key(0f, -4f), new Key(1f, 0f)));

            return list;
        }
    }
}