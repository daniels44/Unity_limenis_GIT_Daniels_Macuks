using System.Collections;
using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float amount);
}

public class Sword : MonoBehaviour
{
    [Header("Attack")]
    public float damage = 25f;
    public float reach = 2f;
    public float radius = 0.6f;
    public LayerMask hitMask = ~0;
    public float swingCooldown = 0.6f;
    public string swingTrigger = "Swing";

    [Header("References")]
    public Transform handSocket;
    public Animator animator;
    public AudioSource swingSound;

    bool canSwing = true;

    void Update()
    {
        // fallback test: left mouse click
        if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            TryAttack();
    }

    public void OnAttack(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (ctx.performed) TryAttack();
    }

    public void TriggerAttack()
    {
        TryAttack();
    }

    void TryAttack()
    {
        if (!canSwing) return;
        StartCoroutine(PerformAttack());
    }

    IEnumerator PerformAttack()
    {
        canSwing = false;

        if (animator != null) animator.SetTrigger(swingTrigger);
        if (swingSound != null) swingSound.Play();

        // small timing offset: wait a frame to let animation orient if needed
        yield return null;

        Vector3 origin = handSocket != null ? handSocket.position : transform.position;
        Vector3 forward = handSocket != null ? handSocket.forward : transform.forward;

        // OverlapSphere centered a bit forward to cover swing arc
        Collider[] hits = Physics.OverlapSphere(origin + forward * (reach * 0.5f), radius, hitMask);
        foreach (var col in hits)
        {
            // check distance
            Vector3 closest = col.ClosestPoint(origin);
            if ((closest - origin).magnitude > reach + 0.01f) continue;

            // IDamageable preferred
            var dmg = col.GetComponentInParent<IDamageable>();
            if (dmg != null)
            {
                dmg.TakeDamage(damage);
                continue;
            }

            // fallback: common TakeDamage method
            var mb = col.GetComponentInParent<MonoBehaviour>();
            if (mb != null)
            {
                var method = mb.GetType().GetMethod("TakeDamage");
                if (method != null)
                    method.Invoke(mb, new object[] { damage });
            }
        }

        yield return new WaitForSeconds(swingCooldown);
        canSwing = true;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 origin = handSocket != null ? handSocket.position : transform.position;
        Vector3 forward = handSocket != null ? handSocket.forward : transform.forward;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin + forward * (reach * 0.5f), radius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + forward * reach);
    }
}