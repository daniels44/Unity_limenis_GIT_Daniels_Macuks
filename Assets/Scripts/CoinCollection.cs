using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CoinCollection : MonoBehaviour
{
    [Tooltip("How many crystals are required to win")]
    public int requiredCrystals = 8;

    private int crystalCount = 0;
    public TextMeshProUGUI crystalText;

    // Optional: if you set a scene name here, it will load that scene on win.
    // Leave empty to just pause the game and show a message.
    public string winSceneName = "";

    private void Start()
    {
        crystalCount = 0;
        UpdateUI();
    }

    private void OnTriggerEnter(Collider other)
    {
        // check for objects tagged "Crystal" (set this tag on your collectible prefabs)
        if (!other.CompareTag("Crystal")) return;

        crystalCount++;
        UpdateUI();
        Destroy(other.gameObject);

        if (crystalCount >= requiredCrystals)
            OnAllCrystalsCollected();
    }

    private void UpdateUI()
    {
        if (crystalText != null)
            crystalText.text = $"Crystals: {crystalCount}/{requiredCrystals}";
    }

    private void OnAllCrystalsCollected()
    {
        Debug.Log("All crystals collected!");

        if (!string.IsNullOrEmpty(winSceneName))
        {
            SceneManager.LoadScene(winSceneName);
        }
        else
        {
            Time.timeScale = 0f;
            if (crystalText != null)
                crystalText.text = "All crystals collected! You win!";
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}