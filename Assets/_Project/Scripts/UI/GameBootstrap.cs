using DeliveryBot.Delivery;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace DeliveryBot.UI
{
    /// <summary>Small quality-of-life: R restarts the round (not while typing a nickname or viewing results), Esc quits a standalone build.</summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Application.targetFrameRate = 60;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.rKey.wasPressedThisFrame && !GameFlow.MenuOpen) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            if (kb.escapeKey.wasPressedThisFrame && !Application.isEditor) Application.Quit();
        }
    }
}
