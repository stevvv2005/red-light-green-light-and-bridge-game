using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DG.Tweening
{
    public enum Ease
    {
        Linear,
        OutQuad,
        OutCubic,
        OutBack,
        OutSine,
        InOutSine
    }

    public enum LoopType
    {
        Restart,
        Yoyo
    }

    public class Tween
    {
        public virtual Tween SetEase(Ease ease) => this;
        public virtual Tween SetLoops(int loops, LoopType loopType) => this;
        public virtual Tween OnComplete(Action callback)
        {
            callback?.Invoke();
            return this;
        }

        public virtual void Kill()
        {
        }
    }

    public sealed class Sequence : Tween
    {
        public Sequence Append(Tween tween) => this;
        public Sequence Join(Tween tween) => this;
        public Sequence Insert(float atPosition, Tween tween) => this;
        public Sequence AppendInterval(float interval) => this;
    }

    public static class DOTween
    {
        public static Sequence Sequence() => new Sequence();
    }

    public static class ShortcutExtensions
    {
        public static Tween DOAnchorPos(this RectTransform target, Vector2 endValue, float duration)
        {
            if (target != null)
                target.anchoredPosition = endValue;
            return new Tween();
        }

        public static Tween DOAnchorPosX(this RectTransform target, float endValue, float duration)
        {
            if (target != null)
            {
                Vector2 pos = target.anchoredPosition;
                pos.x = endValue;
                target.anchoredPosition = pos;
            }
            return new Tween();
        }

        public static Tween DOScale(this Transform target, float endValue, float duration)
        {
            if (target != null)
                target.localScale = Vector3.one * endValue;
            return new Tween();
        }

        public static Tween DOScale(this Transform target, Vector3 endValue, float duration)
        {
            if (target != null)
                target.localScale = endValue;
            return new Tween();
        }

        public static Tween DOScaleY(this Transform target, float endValue, float duration)
        {
            if (target != null)
            {
                Vector3 scale = target.localScale;
                scale.y = endValue;
                target.localScale = scale;
            }
            return new Tween();
        }

        public static Tween DOFade(this CanvasGroup target, float endValue, float duration)
        {
            if (target != null)
                target.alpha = endValue;
            return new Tween();
        }

        public static Tween DOFade(this Graphic target, float endValue, float duration)
        {
            if (target != null)
            {
                Color color = target.color;
                color.a = endValue;
                target.color = color;
            }
            return new Tween();
        }

        public static Tween DOColor(this Graphic target, Color endValue, float duration)
        {
            if (target != null)
                target.color = endValue;
            return new Tween();
        }

        public static Tween DOColor(this TMP_Text target, Color endValue, float duration)
        {
            if (target != null)
                target.color = endValue;
            return new Tween();
        }

        public static Tween DOFillAmount(this Image target, float endValue, float duration)
        {
            if (target != null)
                target.fillAmount = endValue;
            return new Tween();
        }

        public static Tween DOPunchScale(this Transform target, Vector3 punch, float duration, int vibrato, float elasticity)
        {
            return new Tween();
        }

        public static Tween DOShakeAnchorPos(this RectTransform target, float duration, float strength, int vibrato, float randomness, bool snapping, bool fadeOut)
        {
            return new Tween();
        }
    }
}
