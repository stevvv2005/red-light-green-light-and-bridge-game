using UnityEngine;
using UnityEngine.UI;

namespace SquidGameUI
{
    /// <summary>
    /// Closes the currently visible main menu overlay.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class MenuBackButton : MonoBehaviour
    {
        [SerializeField] private MainMenuUI menu;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(Hide);
        }

        public void SetMenu(MainMenuUI target)
        {
            menu = target;
        }

        private void Hide()
        {
            if (menu != null)
                menu.HideOverlays();
        }
    }
}
