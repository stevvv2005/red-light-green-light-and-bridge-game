using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SquidGameUI
{
    /// <summary>
    /// Controls the cinematic main menu screen.
    /// </summary>
    public sealed class MainMenuUI : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private RectTransform panelLeft;
        [SerializeField] private RectTransform panelRight;

        [Header("Brand")]
        [SerializeField] private TMP_Text logoText;
        [SerializeField] private TMP_Text subtitleText;

        [Header("Hero")]
        [SerializeField] private Image heroImage;
        [SerializeField] private RectTransform heroOverlay;
        [SerializeField] private TMP_Text playerBadgeNumber;
        [SerializeField] private TMP_Text playerBadgeLabel;
        [SerializeField] private TMP_Text taglineText;

        [Header("Buttons")]
        [SerializeField] private RectTransform buttonColumn;
        [SerializeField] private SquidButton playButton;
        [SerializeField] private SquidButton multiplayerButton;
        [SerializeField] private SquidButton settingsButton;
        [SerializeField] private SquidButton leaderboardButton;
        [SerializeField] private SquidButton exitButton;
        [SerializeField] private SquidButton gearButton;
        [SerializeField] private SquidButton powerButton;

        [Header("Optional Panels")]
        [SerializeField] private RectTransform modeSelectionPanel;
        [SerializeField] private RectTransform settingsPanel;
        [SerializeField] private RectTransform multiplayerPanel;
        [SerializeField] private RectTransform leaderboardPanel;

        private Coroutine introRoutine;

        private void Awake()
        {
            if (rootGroup == null)
                rootGroup = GetComponentInChildren<CanvasGroup>(true);

            if (rootGroup == null)
                rootGroup = gameObject.AddComponent<CanvasGroup>();

            if (panelLeft == null)
                panelLeft = GameObject.Find("Panel_Left")?.GetComponent<RectTransform>();

            if (panelRight == null)
                panelRight = GameObject.Find("Panel_Right")?.GetComponent<RectTransform>();

            BindButtons();
            SetupHeroImage();
            RegisterPanels();
            HideOptionalPanels();
        }

        private void Start()
        {
            PlayIntro();
        }

        public void PlayIntro()
        {
            if (introRoutine != null)
                StopCoroutine(introRoutine);

            introRoutine = StartCoroutine(IntroRoutine());
        }

        public void OnPlayClick()
        {
            ShowOverlay(modeSelectionPanel);
        }

        public void OnMultiplayerClick()
        {
            ShowOverlay(multiplayerPanel);
        }

        public void OnSettingsClick()
        {
            ShowOverlay(settingsPanel);
        }

        public void OnLeaderboardClick()
        {
            ShowOverlay(leaderboardPanel);
        }

        public void OnExitClick()
        {
            SceneFlow.Quit();
        }

        private IEnumerator IntroRoutine()
        {
            rootGroup.alpha = 0f;
            panelLeft.localScale = Vector3.one * 0.98f;
            panelRight.localScale = Vector3.one * 0.98f;

            UIAnimationHelper.FadeScaleIn(rootGroup, panelLeft, 0.45f);
            UIAnimationHelper.FadeScaleIn(rootGroup, panelRight, 0.45f);

            rootGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(0.05f);

            if (logoText != null)
            {
                Color logoColor = logoText.color;
                logoColor.a = 0f;
                logoText.color = logoColor;
                UIAnimationHelper.FadeGraphic(logoText, 0f, 1f, 0.6f);
                UIAnimationHelper.PulseGlow(logoText, UIColorPalette.LoadOrCreateDefault().accentPink, 1.5f);
            }

            if (subtitleText != null)
            {
                Color subtitleColor = subtitleText.color;
                subtitleColor.a = 0f;
                subtitleText.color = subtitleColor;
                UIAnimationHelper.FadeGraphic(subtitleText, 0f, 1f, 0.6f);
            }

            if (buttonColumn != null)
                UIAnimationHelper.StaggerChildren(buttonColumn, 0.07f);
            else if (panelLeft != null)
                UIAnimationHelper.StaggerChildren(panelLeft, 0.07f);
        }

        private IEnumerator LoadSceneAfterSlide(System.Action action)
        {
            RectTransform menuRoot = panelLeft != null ? panelLeft.parent as RectTransform : null;
            if (menuRoot != null)
            {
                Vector2 target = menuRoot.anchoredPosition;
                Vector2 outPos = target + new Vector2(-Mathf.Max(1200f, menuRoot.rect.width * 1.15f), 0f);
                float elapsed = 0f;
                const float duration = 0.28f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    menuRoot.anchoredPosition = Vector2.Lerp(target, outPos, t);
                    yield return null;
                }
            }

            action?.Invoke();
        }

        private void BindButtons()
        {
            if (playButton != null)
            {
                playButton.SetIcon(">");
                playButton.SetLabel("PLAY");
                playButton.GetComponent<Button>().onClick.AddListener(OnPlayClick);
            }

            if (multiplayerButton != null)
            {
                multiplayerButton.SetIcon("MP");
                multiplayerButton.SetLabel("MULTIPLAYER");
                multiplayerButton.GetComponent<Button>().onClick.AddListener(OnMultiplayerClick);
            }

            if (settingsButton != null)
            {
                settingsButton.SetIcon("*");
                settingsButton.SetLabel("SETTINGS");
                settingsButton.GetComponent<Button>().onClick.AddListener(OnSettingsClick);
            }

            if (leaderboardButton != null)
            {
                leaderboardButton.SetIcon("#");
                leaderboardButton.SetLabel("LEADERBOARD");
                leaderboardButton.GetComponent<Button>().onClick.AddListener(OnLeaderboardClick);
            }

            if (exitButton != null)
            {
                exitButton.SetIcon("X");
                exitButton.SetLabel("EXIT");
                exitButton.GetComponent<Button>().onClick.AddListener(OnExitClick);
            }

            if (gearButton != null)
                gearButton.GetComponent<Button>().onClick.AddListener(OnSettingsClick);

            if (powerButton != null)
                powerButton.GetComponent<Button>().onClick.AddListener(OnExitClick);
        }

        private void SetupHeroImage()
        {
            if (heroImage == null)
                return;

            Texture2D texture = Resources.Load<Texture2D>("grassland-and-blue-sky");
            if (texture == null)
                return;

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            heroImage.sprite = sprite;
            heroImage.type = Image.Type.Simple;
            heroImage.preserveAspect = false;
            heroImage.color = Color.white;
        }

        private void RegisterPanels()
        {
            RectTransform menuRoot = panelLeft != null ? panelLeft.parent as RectTransform : null;
            if (menuRoot != null)
                UIManager.Instance.RegisterPanel(UIManager.PanelType.MainMenu, menuRoot);
            if (modeSelectionPanel != null)
                UIManager.Instance.RegisterPanel(UIManager.PanelType.ModeSelection, modeSelectionPanel);
            if (settingsPanel != null)
                UIManager.Instance.RegisterPanel(UIManager.PanelType.Settings, settingsPanel);
            if (multiplayerPanel != null)
                UIManager.Instance.RegisterPanel(UIManager.PanelType.Multiplayer, multiplayerPanel);
            if (leaderboardPanel != null)
                UIManager.Instance.RegisterPanel(UIManager.PanelType.Leaderboard, leaderboardPanel);
        }

        private void HideOptionalPanels()
        {
            if (modeSelectionPanel != null)
                modeSelectionPanel.gameObject.SetActive(false);
            if (settingsPanel != null)
                settingsPanel.gameObject.SetActive(false);
            if (multiplayerPanel != null)
                multiplayerPanel.gameObject.SetActive(false);
            if (leaderboardPanel != null)
                leaderboardPanel.gameObject.SetActive(false);
        }

        public void HideOverlays()
        {
            HideOptionalPanels();
        }

        private void ShowOverlay(RectTransform panel)
        {
            HideOptionalPanels();
            if (panel == null)
                return;

            panel.gameObject.SetActive(true);
            CanvasGroup group = panel.GetComponent<CanvasGroup>();
            if (group == null)
                group = panel.gameObject.AddComponent<CanvasGroup>();

            group.alpha = 0f;
            UIAnimationHelper.FadeScaleIn(group, panel, 0.22f);
        }
    }
}
