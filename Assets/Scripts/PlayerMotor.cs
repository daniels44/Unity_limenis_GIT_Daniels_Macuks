using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMotor : MonoBehaviour
{
    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool IsGrounded;
    public float speed = 5f;
    public float gravity = -9.81f;
    public float jumpHeight = 3f;

    // sprint settings (stamina-based)
    public float sprintMultiplier = 1.8f;
    public float sprintCooldown = 10f;           // seconds cooldown after exhaustion / stop
    public float sprintMaxDuration = 10f;        // legacy cap (kept for compatibility)
    public float staminaMax = 10f;               // total stamina (seconds of sprint at drainRate = 1)
    public float staminaDrainRate = 1f;          // stamina units drained per second while sprinting
    public float staminaRegenRate = 1.5f;        // stamina units restored per second after regen delay
    public float staminaRegenDelay = 2f;         // seconds to wait after stopping sprint before regen starts

    private bool sprintAvailable = true;
    private float sprintCooldownTimer = 0f;
    private bool isSprinting = false;
    private float sprintTimer = 0f; // tracks continuous sprint time

    // stamina runtime
    private float stamina;
    private float regenDelayTimer = 0f;

    // Public read-only accessors for UI / other systems
    public bool SprintAvailable => sprintAvailable;
    public float SprintCooldownTimer => sprintCooldownTimer;
    public float SprintTimer => sprintTimer;
    public bool IsSprinting => isSprinting;
    public float SprintCooldown => sprintCooldown;
    public float SprintMaxDurationPublic => sprintMaxDuration;

    public float Stamina => stamina;
    public float StaminaMax => staminaMax;
    public float StaminaFraction => (staminaMax > 0f) ? (stamina / staminaMax) : 0f;
    public float RegenDelayRemaining => regenDelayTimer;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        stamina = staminaMax;
        regenDelayTimer = 0f;
    }

    void Update()
    {
        if (controller != null)
            IsGrounded = controller.isGrounded;
    }

    // input: movement vector and whether sprint is requested (button held)
    public void ProcessMove(Vector2 input, bool sprintRequested)
    {
        float dt = Time.deltaTime;

        // previous sprint state
        bool wasSprinting = isSprinting;

        // determine if we can sprint: player requested, sprint available, and have stamina
        bool canSprintNow = sprintRequested && sprintAvailable && stamina > 0f;

        isSprinting = canSprintNow;

        if (isSprinting)
        {
            // drain stamina
            stamina -= staminaDrainRate * dt;
            if (stamina <= 0f)
            {
                stamina = 0f;
                // exhausted -> force stop sprint and start cooldown
                isSprinting = false;
                sprintAvailable = false;
                sprintCooldownTimer = sprintCooldown;
                sprintTimer = 0f;
                regenDelayTimer = staminaRegenDelay;
            }
            else
            {
                // accumulate legacy sprint timer (optional cap)
                sprintTimer += dt;
                if (sprintTimer >= sprintMaxDuration)
                {
                    // reached max continuous sprint -> force stop and cooldown
                    isSprinting = false;
                    sprintAvailable = false;
                    sprintCooldownTimer = sprintCooldown;
                    sprintTimer = 0f;
                    regenDelayTimer = staminaRegenDelay;
                }
            }

            // while sprinting, reset regen delay
            regenDelayTimer = staminaRegenDelay;
        }
        else
        {
            // if player released sprint (was sprinting but now not)
            if (wasSprinting && !isSprinting)
            {
                // only trigger full sprint cooldown when stamina exhausted OR max-duration forced stop
                if (stamina <= 0f || sprintTimer >= sprintMaxDuration)
                {
                    sprintAvailable = false;
                    sprintCooldownTimer = sprintCooldown;
                }

                // always start regeneration delay after stopping sprint (prevents instant regen)
                regenDelayTimer = staminaRegenDelay;
            }

            // reset continuous sprint timer when not sprinting and not in the middle of sprint release
            if (!wasSprinting)
                sprintTimer = 0f;
        }

        // handle cooldown ticking
        if (!sprintAvailable)
        {
            sprintCooldownTimer -= dt;
            if (sprintCooldownTimer <= 0f)
            {
                sprintAvailable = true;
                sprintCooldownTimer = 0f;
                // allow regen to start (regenDelay may still be counting)
            }
        }

        // handle stamina regen after delay (only when not sprinting)
        if (!isSprinting)
        {
            if (regenDelayTimer > 0f)
            {
                regenDelayTimer -= dt;
                if (regenDelayTimer < 0f) regenDelayTimer = 0f;
            }
            else if (stamina < staminaMax)
            {
                stamina += staminaRegenRate * dt;
                if (stamina > staminaMax) stamina = staminaMax;
            }
        }

        // movement
        float currentSpeed = speed * (isSprinting ? sprintMultiplier : 1f);

        Vector3 moveDiraction = Vector3.zero;
        moveDiraction.x = input.x;
        moveDiraction.z = input.y;
        if (controller != null)
            controller.Move(transform.TransformDirection(moveDiraction) * currentSpeed * dt);

        // gravity + jump vertical move already handled elsewhere
        playerVelocity.y += gravity * dt;
        if (IsGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }
        if (controller != null)
            controller.Move(playerVelocity * dt);
    }

    public void Jump()
    {
        if (IsGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -0.3f * gravity);
        }
    }
}