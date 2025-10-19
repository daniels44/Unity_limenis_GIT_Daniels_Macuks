using UnityEngine;
using UnityEngine.UI;

public class SprintUI : MonoBehaviour
{
    [Header("References")]
    public PlayerController playerController;   // assign in Inspector (or auto-find)
    public Image staminaFill;                   // Image.type = Filled, Fill Method = Horizontal
    public Text statusText;                     // optional, shows "Ready", "Cooldown", etc.

    void Start()
    {
        // Auto-assign PlayerController if not set
        if (playerController == null)
        {
#if UNITY_2023_1_OR_NEWER
            playerController = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
#else
            playerController = FindObjectOfType<PlayerController>();
#endif
            if (playerController != null)
                Debug.Log("SprintUI: auto-assigned PlayerController from scene: " + playerController.gameObject.name);
        }

        // Auto-assign staminaFill from UI named "FrontHealthBar (2)" (optional)
        if (staminaFill == null)
        {
            staminaFill = GameObject.Find("FrontHealthBar (2)")?.GetComponent<Image>();
            if (staminaFill != null)
                Debug.Log("SprintUI: auto-assigned staminaFill from FrontHealthBar (2)");
        }
    }

    void Update()
    {
        if (playerController == null || staminaFill == null) return;

        // Show current stamina fraction (0..1)
        staminaFill.fillAmount = Mathf.Clamp01(playerController.StaminaFraction);

        // Define color for sprint mode
        Color sprintBlue = new Color(0.2f, 0.6f, 1f); // light blue

        // Change color and text depending on sprint state
        if (playerController.IsSprinting)
        {
            staminaFill.color = sprintBlue;
            if (statusText != null)
                statusText.text = $"{Mathf.CeilToInt(playerController.Stamina)}";
        }
        else if (!playerController.SprintAvailable)
        {
            staminaFill.color = Color.gray;
            if (statusText != null)
                statusText.text = $"{Mathf.CeilToInt(playerController.SprintCooldownTimer)}";
        }
        else
        {
            staminaFill.color = sprintBlue * 0.8f; // slightly dim when idle
            if (statusText != null)
                statusText.text = "Ready";
        }
    }
}
