using UnityEngine;

namespace SquidGameUI
{
    /// <summary>
    /// Shared color palette for the cinematic menu UI.
    /// </summary>
    [CreateAssetMenu(fileName = "UIColorPalette", menuName = "Squid Game/UI Color Palette")]
    public sealed class UIColorPalette : ScriptableObject
    {
        public static UIColorPalette Instance { get; private set; }

        [Header("Base")]
        public Color bgPrimary = new Color(0.051f, 0.051f, 0.059f, 1f);
        public Color bgCard = new Color(0.102f, 0.102f, 0.102f, 1f);
        public Color bgCardBorder = new Color(0.165f, 0.165f, 0.165f, 1f);

        [Header("Accents")]
        public Color accentPink = new Color(1f, 0.176f, 0.42f, 1f);
        public Color accentGreen = new Color(0f, 0.784f, 0.353f, 1f);
        public Color accentGold = new Color(0.961f, 0.651f, 0.137f, 1f);
        public Color dangerRed = new Color(1f, 0.176f, 0.176f, 1f);

        [Header("Text")]
        public Color textPrimary = Color.white;
        public Color textSecondary = new Color(0.533f, 0.533f, 0.533f, 1f);
        public Color textLabel = new Color(0.667f, 0.667f, 0.667f, 1f);

        public Color overlay = new Color(0.051f, 0.051f, 0.059f, 0.92f);

        private void OnEnable()
        {
            Instance = this;
        }

        public static UIColorPalette LoadOrCreateDefault()
        {
            if (Instance != null)
                return Instance;

            Instance = Resources.Load<UIColorPalette>("UI/UIColorPalette");
            return Instance;
        }
    }
}
