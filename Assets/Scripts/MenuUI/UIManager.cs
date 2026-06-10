using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SquidGameUI
{
    /// <summary>
    /// Generic panel manager for the menu flow.
    /// </summary>
    public sealed class UIManager : MonoBehaviour
    {
        public enum PanelType
        {
            MainMenu,
            ModeSelection,
            Multiplayer,
            Settings,
            Leaderboard,
            RoundSelection,
            Pause,
            GameOver,
            Victory
        }

        private static UIManager instance;
        private readonly Dictionary<PanelType, RectTransform> panels = new Dictionary<PanelType, RectTransform>();
        private readonly Stack<PanelType> panelStack = new Stack<PanelType>();

        public static UIManager Instance
        {
            get
            {
                if (instance != null)
                    return instance;

                GameObject go = new GameObject("[UIManager]");
                instance = go.AddComponent<UIManager>();
                DontDestroyOnLoad(go);
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void RegisterPanel(PanelType panelType, RectTransform panelRoot)
        {
            if (panelRoot == null)
                return;

            panels[panelType] = panelRoot;
        }

        public void ShowPanel(PanelType panelType, bool animated = true)
        {
            ShowPanelInternal(panelType, animated, true);
        }

        private void ShowPanelInternal(PanelType panelType, bool animated, bool addToStack)
        {
            if (!panels.TryGetValue(panelType, out RectTransform panel))
                return;

            foreach (KeyValuePair<PanelType, RectTransform> entry in panels)
            {
                if (entry.Key != panelType && entry.Value != null)
                    entry.Value.gameObject.SetActive(false);
            }

            panel.gameObject.SetActive(true);
            if (addToStack && (panelStack.Count == 0 || panelStack.Peek() != panelType))
                panelStack.Push(panelType);

            if (animated)
            {
                CanvasGroup group = panel.GetComponent<CanvasGroup>();
                if (group == null)
                    group = panel.gameObject.AddComponent<CanvasGroup>();

                group.alpha = 0f;
                UIAnimationHelper.FadeScaleIn(group, panel, 0.22f);
            }
            else
            {
                CanvasGroup group = panel.GetComponent<CanvasGroup>();
                if (group != null)
                    group.alpha = 1f;
            }
        }

        public void HidePanel(PanelType panelType, bool animated = true)
        {
            if (!panels.TryGetValue(panelType, out RectTransform panel))
                return;

            if (animated)
            {
                CanvasGroup group = panel.GetComponent<CanvasGroup>();
                if (group == null)
                    group = panel.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0f;
            }

            panel.gameObject.SetActive(false);
        }

        public void HideAllPanels()
        {
            foreach (RectTransform panel in panels.Values)
            {
                if (panel != null)
                    panel.gameObject.SetActive(false);
            }

            panelStack.Clear();
        }

        public bool TryGoBack()
        {
            if (panelStack.Count < 2)
                return false;

            panelStack.Pop();
            ShowPanelInternal(panelStack.Peek(), true, false);
            return true;
        }
    }
}
