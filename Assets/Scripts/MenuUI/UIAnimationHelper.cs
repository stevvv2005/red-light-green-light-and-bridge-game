using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace SquidGameUI
{
    /// <summary>
    /// Lightweight animation helpers for the menu UI.
    /// </summary>
    public static class UIAnimationHelper
    {
        private sealed class Runner : MonoBehaviour
        {
            private static Runner instance;

            public static Runner Instance
            {
                get
                {
                    if (instance != null)
                        return instance;

                    GameObject go = new GameObject("[UIAnimationRunner]");
                    Object.DontDestroyOnLoad(go);
                    instance = go.AddComponent<Runner>();
                    return instance;
                }
            }
        }

        public static Sequence SlideInFromRight(RectTransform rt, float duration = 0.3f)
        {
            if (rt == null)
                return DOTween.Sequence();

            Vector2 target = rt.anchoredPosition;
            Vector2 from = target + new Vector2(Mathf.Max(160f, rt.rect.width * 0.35f), 0f);
            Runner.Instance.StartCoroutine(AnimateAnchoredPosition(rt, from, target, duration, Ease.OutCubic));
            return DOTween.Sequence();
        }

        public static Sequence SlideInFromLeft(RectTransform rt, float duration = 0.3f)
        {
            if (rt == null)
                return DOTween.Sequence();

            Vector2 target = rt.anchoredPosition;
            Vector2 from = target + new Vector2(-Mathf.Max(160f, rt.rect.width * 0.35f), 0f);
            Runner.Instance.StartCoroutine(AnimateAnchoredPosition(rt, from, target, duration, Ease.OutCubic));
            return DOTween.Sequence();
        }

        public static Sequence FadeScaleIn(CanvasGroup cg, RectTransform rt, float duration = 0.25f)
        {
            if (cg != null)
                cg.alpha = 0f;
            if (rt != null)
                rt.localScale = Vector3.one * 0.95f;

            Runner.Instance.StartCoroutine(AnimateFadeScale(cg, rt, 0f, 1f, 0.95f, 1f, duration, Ease.OutCubic));
            return DOTween.Sequence();
        }

        public static Sequence StaggerChildren(Transform parent, float delay = 0.08f)
        {
            if (parent == null)
                return DOTween.Sequence();

            Runner.Instance.StartCoroutine(StaggerRoutine(parent, delay));
            return DOTween.Sequence();
        }

        public static Sequence CountUpText(TMP_Text text, float from, float to, float duration)
        {
            if (text == null)
                return DOTween.Sequence();

            Runner.Instance.StartCoroutine(CountRoutine(text, from, to, duration));
            return DOTween.Sequence();
        }

        public static Sequence PulseGlow(Graphic graphic, Color glowColor, float duration = 1.5f)
        {
            if (graphic == null)
                return DOTween.Sequence();

            Runner.Instance.StartCoroutine(PulseRoutine(graphic, glowColor, duration));
            return DOTween.Sequence();
        }

        public static Sequence FadeGraphic(Graphic graphic, float fromAlpha, float toAlpha, float duration)
        {
            if (graphic == null)
                return DOTween.Sequence();

            Runner.Instance.StartCoroutine(FadeGraphicRoutine(graphic, fromAlpha, toAlpha, duration));
            return DOTween.Sequence();
        }

        public static Sequence PunchScale(RectTransform rt, float punch = 0.06f)
        {
            if (rt == null)
                return DOTween.Sequence();

            Runner.Instance.StartCoroutine(PunchScaleRoutine(rt, punch));
            return DOTween.Sequence();
        }

        public static Sequence Shake(RectTransform rt, float duration = 0.28f, float strength = 10f)
        {
            if (rt == null)
                return DOTween.Sequence();

            Runner.Instance.StartCoroutine(ShakeRoutine(rt, duration, strength));
            return DOTween.Sequence();
        }

        private static IEnumerator AnimateAnchoredPosition(RectTransform rt, Vector2 from, Vector2 to, float duration, Ease ease)
        {
            float elapsed = 0f;
            rt.anchoredPosition = from;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rt.anchoredPosition = Vector2.LerpUnclamped(from, to, Evaluate(ease, t));
                yield return null;
            }

            rt.anchoredPosition = to;
        }

        private static IEnumerator AnimateFadeScale(CanvasGroup cg, RectTransform rt, float fromAlpha, float toAlpha, float fromScale, float toScale, float duration, Ease ease)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Evaluate(ease, t);
                if (cg != null)
                    cg.alpha = Mathf.LerpUnclamped(fromAlpha, toAlpha, eased);
                if (rt != null)
                    rt.localScale = Vector3.one * Mathf.LerpUnclamped(fromScale, toScale, eased);
                yield return null;
            }

            if (cg != null)
                cg.alpha = toAlpha;
            if (rt != null)
                rt.localScale = Vector3.one * toScale;
        }

        private static IEnumerator StaggerRoutine(Transform parent, float delay)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                RectTransform rt = child as RectTransform;
                CanvasGroup cg = child.GetComponent<CanvasGroup>();
                if (cg != null)
                    cg.alpha = 0f;
                if (rt != null)
                    rt.localScale = Vector3.one * 0.96f;

                float elapsed = 0f;
                while (elapsed < 0.22f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / 0.22f);
                    float eased = Evaluate(Ease.OutCubic, t);
                    if (cg != null)
                        cg.alpha = eased;
                    if (rt != null)
                        rt.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, eased);
                    yield return null;
                }

                if (cg != null)
                    cg.alpha = 1f;
                if (rt != null)
                    rt.localScale = Vector3.one;

                if (delay > 0f)
                    yield return new WaitForSecondsRealtime(delay);
            }
        }

        private static IEnumerator CountRoutine(TMP_Text text, float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float value = Mathf.Lerp(from, to, Evaluate(Ease.OutCubic, t));
                text.text = Mathf.RoundToInt(value).ToString("N0");
                yield return null;
            }

            text.text = Mathf.RoundToInt(to).ToString("N0");
        }

        private static IEnumerator PulseRoutine(Graphic graphic, Color glowColor, float duration)
        {
            Color baseColor = graphic.color;
            float phase = 0f;
            while (graphic != null)
            {
                phase += Time.unscaledDeltaTime;
                float cycle = Mathf.Sin((phase / duration) * Mathf.PI * 2f) * 0.5f + 0.5f;
                graphic.color = Color.Lerp(baseColor, glowColor, cycle * 0.55f);
                yield return null;
            }
        }

        private static IEnumerator FadeGraphicRoutine(Graphic graphic, float fromAlpha, float toAlpha, float duration)
        {
            Color color = graphic.color;
            color.a = fromAlpha;
            graphic.color = color;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Evaluate(Ease.OutCubic, t);
                color.a = Mathf.Lerp(fromAlpha, toAlpha, eased);
                graphic.color = color;
                yield return null;
            }

            color.a = toAlpha;
            graphic.color = color;
        }

        private static IEnumerator PunchScaleRoutine(RectTransform rt, float punch)
        {
            Vector3 baseScale = rt.localScale;
            Vector3 target = baseScale * (1f + punch);
            float elapsed = 0f;
            const float duration = 0.1f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rt.localScale = Vector3.Lerp(baseScale, target, Evaluate(Ease.OutCubic, t));
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rt.localScale = Vector3.Lerp(target, baseScale, Evaluate(Ease.OutCubic, t));
                yield return null;
            }

            rt.localScale = baseScale;
        }

        private static IEnumerator ShakeRoutine(RectTransform rt, float duration, float strength)
        {
            Vector2 basePos = rt.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float attenuation = 1f - Mathf.Clamp01(elapsed / duration);
                float x = (Mathf.PerlinNoise(Time.unscaledTime * 60f, 0.1f) - 0.5f) * 2f * strength * attenuation;
                float y = (Mathf.PerlinNoise(0.1f, Time.unscaledTime * 60f) - 0.5f) * 2f * strength * 0.3f * attenuation;
                rt.anchoredPosition = basePos + new Vector2(x, y);
                yield return null;
            }

            rt.anchoredPosition = basePos;
        }

        private static float Evaluate(Ease ease, float t)
        {
            switch (ease)
            {
                case Ease.OutQuad:
                    return 1f - (1f - t) * (1f - t);
                case Ease.OutCubic:
                    return 1f - Mathf.Pow(1f - t, 3f);
                case Ease.OutBack:
                {
                    float c1 = 1.70158f;
                    float c3 = c1 + 1f;
                    return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                }
                case Ease.OutSine:
                    return Mathf.Sin((t * Mathf.PI) / 2f);
                case Ease.InOutSine:
                    return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
                default:
                    return t;
            }
        }
    }
}
