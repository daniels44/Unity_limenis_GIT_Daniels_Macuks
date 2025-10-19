using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PlayerHealth : MonoBehaviour
{
    private float health;
    private float lerpTimer;
    public float maxHealth = 100f;
    public float chipSpeed = 2f;
    public Image frontHealthBar;
    public Image backHealthBar;

    void Start()
    {
        health = maxHealth;

        if (frontHealthBar != null)
        {
            frontHealthBar.type = Image.Type.Filled;
            frontHealthBar.fillMethod = Image.FillMethod.Horizontal;
            frontHealthBar.fillOrigin = (int)Image.OriginHorizontal.Left;
            frontHealthBar.fillAmount = 1f;
        }

        if (backHealthBar != null)
        {
            backHealthBar.type = Image.Type.Filled;
            backHealthBar.fillMethod = Image.FillMethod.Horizontal;
            // default back shows "missing" health from right
            backHealthBar.fillOrigin = (int)Image.OriginHorizontal.Right;
            backHealthBar.fillAmount = 0f; // no missing health at start
        }
    }

    void Update()
    {
        health = Mathf.Clamp(health, 0f, maxHealth);

        // test damage / heal using new Input System keys (C / B)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.cKey.wasPressedThisFrame)
                TakeDamage(Random.Range(5f, 10f));
            if (Keyboard.current.bKey.wasPressedThisFrame)
                RestoreHealth(Random.Range(5f, 10f));
        }

        UpdateHealthUI();
    }

    public void UpdateHealthUI()
    {
        if (frontHealthBar == null || backHealthBar == null)
        {
            Debug.LogWarning("PlayerHealth: Health bar images not assigned.");
            return;
        }

        float fillF = frontHealthBar.fillAmount;            // current front fill (0..1)
        float missingB = backHealthBar.fillAmount;          // current back fill (represents missing health when red)
        float hFraction = health / maxHealth;               // current health fraction (0..1)

        // DAMAGE: front (green) snaps to new health, back (red) shows missing health and fills from RIGHT
        if (fillF > hFraction)
        {
            // ensure origins
            frontHealthBar.fillOrigin = (int)Image.OriginHorizontal.Left;
            backHealthBar.fillOrigin = (int)Image.OriginHorizontal.Right;
            backHealthBar.color = Color.red;

            // set front to current health immediately
            frontHealthBar.fillAmount = hFraction;

            // target missing amount = 1 - current health (red fill amount)
            float targetMissing = 1f - hFraction;

            // make sure back starts from previous missing so red area is visible
            if (missingB < (1f - fillF))
                backHealthBar.fillAmount = (1f - fillF);

            lerpTimer += Time.deltaTime;
            float percentComplete = Mathf.Clamp01(lerpTimer / chipSpeed);
            percentComplete = percentComplete * percentComplete; // ease

            backHealthBar.fillAmount = Mathf.Lerp(backHealthBar.fillAmount, targetMissing, percentComplete);
        }
        // HEAL: back (green) snaps to health, front (green) lerps up — keep behavior consistent
        else if (fillF < hFraction)
        {
            // back represents the front "cap" during heal — use left origin and green color
            backHealthBar.fillOrigin = (int)Image.OriginHorizontal.Left;
            backHealthBar.color = Color.green;
            backHealthBar.fillAmount = hFraction;

            lerpTimer += Time.deltaTime;
            float percentComplete = Mathf.Clamp01(lerpTimer / chipSpeed);
            percentComplete = percentComplete * percentComplete;
            frontHealthBar.fillAmount = Mathf.Lerp(fillF, hFraction, percentComplete);
        }
        else
        {
            // no change
            lerpTimer = 0f;
            backHealthBar.color = Color.white;
        }
    }

    public void TakeDamage(float damage)
    {
        // capture previous front fill so we can set back (missing) start value
        float prevFront = (frontHealthBar != null) ? frontHealthBar.fillAmount : 1f;

        health -= damage;
        health = Mathf.Clamp(health, 0f, maxHealth);
        lerpTimer = 0f;

        // back stores "missing health" when showing red: set it to previous missing so red is visible
        if (backHealthBar != null)
        {
            // previous missing = 1 - prevFront
            float prevMissing = 1f - prevFront;
            backHealthBar.fillOrigin = (int)Image.OriginHorizontal.Right;
            backHealthBar.color = Color.red;
            backHealthBar.fillAmount = Mathf.Max(backHealthBar.fillAmount, prevMissing);
        }
    }

    public void RestoreHealth(float healAmount)
    {
        health += healAmount;
        health = Mathf.Clamp(health, 0f, maxHealth);
        lerpTimer = 0f;

        // when healing, make back reflect the heal cap from left
        if (backHealthBar != null)
        {
            backHealthBar.fillOrigin = (int)Image.OriginHorizontal.Left;
            backHealthBar.color = Color.green;
            backHealthBar.fillAmount = frontHealthBar != null ? frontHealthBar.fillAmount : (health / maxHealth);
        }
    }
}
