using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SquidGameUI
{
    /// <summary>
    /// Reusable stylized menu button for the Squid Game UI.
    /// </summary>
    [RequireComponent(typeof(Button), typeof(Image), typeof(RectTransform))]
    public sealed class SquidButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public enum Style
        {
            PinkFilled,
            GreenFilled,
            GoldFilled,
            DarkOutline,
            BackButton
        }

        [SerializeField] private Style buttonStyle = Style.DarkOutline;
        [SerializeField] private UIColorPalette palette;
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text icon;

        private Button button;
        private Image background;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Vector3 baseScale;
        private Color baseColor;

        private void Awake()
        {
            button = GetComponent<Button>();
            background = GetComponent<Image>();
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            baseScale = rectTransform.localScale;
            if (palette == null)
                palette = UIColorPalette.LoadOrCreateDefault();

            CacheTexts();
            ApplyStyle();
            button.transition = Selectable.Transition.None;
        }

        private void OnEnable()
        {
            ApplyStyle();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            rectTransform.localScale = baseScale * 1.04f;
            Brighten(0.1f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            rectTransform.localScale = baseScale;
            background.color = baseColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            UIAnimationHelper.PunchScale(rectTransform, 0.04f);
        }

        public void SetLabel(string text)
        {
            if (label != null)
                label.text = text;
        }

        public void SetIcon(string text)
        {
            if (icon != null)
                icon.text = text;
        }

        private void CacheTexts()
        {
            if (label == null)
            {
                Transform labelTransform = transform.Find("Label");
                if (labelTransform != null)
                    label = labelTransform.GetComponent<TMP_Text>();
            }

            if (icon == null)
            {
                Transform iconTransform = transform.Find("Icon");
                if (iconTransform != null)
                    icon = iconTransform.GetComponent<TMP_Text>();
            }
        }

        private void ApplyStyle()
        {
            if (palette == null || background == null)
                return;

            Color fill = palette.bgCard;
            Color border = palette.bgCardBorder;
            Color text = palette.textPrimary;
            baseColor = fill;

            switch (buttonStyle)
            {
                case Style.PinkFilled:
                    fill = palette.accentPink;
                    border = palette.accentPink;
                    break;
                case Style.GreenFilled:
                    fill = palette.accentGreen;
                    border = palette.accentGreen;
                    break;
                case Style.GoldFilled:
                    fill = palette.accentGold;
                    border = palette.accentGold;
                    text = Color.black;
                    break;
                case Style.DarkOutline:
                    fill = new Color(0f, 0f, 0f, 0.12f);
                    border = palette.bgCardBorder;
                    break;
                case Style.BackButton:
                    fill = new Color(0f, 0f, 0f, 0.2f);
                    border = palette.textSecondary;
                    break;
            }

            background.color = fill;
            baseColor = fill;

            Outline outline = GetComponent<Outline>();
            if (outline == null)
                outline = gameObject.AddComponent<Outline>();
            outline.effectColor = border;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            if (label != null)
                label.color = text;
            if (icon != null)
                icon.color = text;
        }

        private void Brighten(float delta)
        {
            Color c = baseColor;
            c.r = Mathf.Clamp01(c.r + delta);
            c.g = Mathf.Clamp01(c.g + delta);
            c.b = Mathf.Clamp01(c.b + delta);
            background.color = c;
        }
    }
}
