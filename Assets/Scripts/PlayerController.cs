using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class PlayerController : MonoBehaviour
{
    PlayerInput playerInput;
    PlayerInput.MainActions input;

    CharacterController controller;
    Animator animator;
    AudioSource audioSource;

    [Header("Controller")]
    public float moveSpeed = 5f;
    public float gravity = -9.8f;
    public float jumpHeight = 1.2f;

    private Vector3 _playerVelocity;
    private bool isGrounded;

    [Header("Camera")]
    public Camera cam;
    public float sensitivity = 100f;
    private float xRotation = 0f;

    // ---------- //
    // SPRINT LOGIC
    // ---------- //
    [Header("Sprint / Stamina Settings")]
    public float sprintMultiplier = 1.8f;
    public float sprintCooldown = 10f;     // cooldown after exhaustion
    public float sprintMaxDuration = 10f;  // maximum continuous sprint
    public float staminaMax = 10f;         // total stamina
    public float staminaDrainRate = 1f;    // stamina drain per second while sprinting
    public float staminaRegenRate = 1.5f;  // regen rate per second
    public float staminaRegenDelay = 2f;   // delay before regen starts after stop

    private bool sprintAvailable = true;
    private float sprintCooldownTimer = 0f;
    private bool isSprinting = false;
    private float sprintTimer = 0f;
    private float stamina;
    private float regenDelayTimer = 0f;

    // Public getters (used by SprintUI)
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

    // ---------- //
    // LIFECYCLE  //
    // ---------- //
    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();

        playerInput = new PlayerInput();
        input = playerInput.Main;
        AssignInputs();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Start()
    {
        stamina = staminaMax;
        regenDelayTimer = 0f;
    }

    void Update()
    {
        isGrounded = controller.isGrounded;

        if (input.Attack.IsPressed())
            Attack();

        SetAnimations();
    }

    void FixedUpdate()
    {
        Vector2 move = input.Movement.ReadValue<Vector2>();
        bool sprintRequested = input.Sprint.IsPressed(); // Shift key
        ProcessMove(move, sprintRequested);
    }

    void LateUpdate()
    {
        LookInput(input.Look.ReadValue<Vector2>());
    }

    // ---------------- //
    // CAMERA & MOVEMENT
    // ---------------- //
    void LookInput(Vector2 look)
    {
        float mouseX = look.x;
        float mouseY = look.y;

        xRotation -= mouseY * Time.deltaTime * sensitivity;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        cam.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);

        transform.Rotate(Vector3.up * (mouseX * Time.deltaTime * sensitivity));
    }

    void Jump()
    {
        if (isGrounded)
            _playerVelocity.y = Mathf.Sqrt(jumpHeight * -3.0f * gravity);
    }

    void AssignInputs()
    {
        input.Jump.performed += ctx => Jump();
        input.Attack.started += ctx => Attack();
    }

    void OnEnable() => input.Enable();
    void OnDisable() => input.Disable();

    // ---------------- //
    // ANIMATION SYSTEM
    // ---------------- //
    public const string IDLE = "Idle";
    public const string WALK = "Walk";
    public const string ATTACK1 = "Attack 1";
    public const string ATTACK2 = "Attack 2";

    string currentAnimationState;

    public void ChangeAnimationState(string newState)
    {
        if (currentAnimationState == newState) return;
        currentAnimationState = newState;
        animator.CrossFadeInFixedTime(currentAnimationState, 0.2f);
    }

    void SetAnimations()
    {
        if (!attacking)
        {
            Vector3 horizontalVelocity = controller.velocity;
            horizontalVelocity.y = 0f;

            if (horizontalVelocity.magnitude < 0.1f)
                ChangeAnimationState(IDLE);
            else
                ChangeAnimationState(WALK);
        }
    }

    // ------------------- //
    // ATTACKING BEHAVIOUR
    // ------------------- //
    [Header("Attacking")]
    public float attackDistance = 3f;
    public float attackDelay = 0.4f;
    public float attackSpeed = 1f;
    public int attackDamage = 1;
    public LayerMask attackLayer;
    public GameObject hitEffect;
    public AudioClip swordSwing;
    public AudioClip hitSound;

    private bool attacking = false;
    private bool readyToAttack = true;
    private int attackCount;

    public void Attack()
    {
        if (!readyToAttack || attacking) return;

        readyToAttack = false;
        attacking = true;

        Invoke(nameof(ResetAttack), attackSpeed);
        Invoke(nameof(AttackRaycast), attackDelay);

        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(swordSwing);

        if (attackCount == 0)
        {
            ChangeAnimationState(ATTACK1);
            attackCount++;
        }
        else
        {
            ChangeAnimationState(ATTACK2);
            attackCount = 0;
        }
    }

    void ResetAttack()
    {
        attacking = false;
        readyToAttack = true;
    }

    void AttackRaycast()
    {
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, attackDistance, attackLayer))
        {
            HitTarget(hit.point);

            if (hit.transform.TryGetComponent<Actor>(out Actor target))
                target.TakeDamage(attackDamage);
        }
    }

    void HitTarget(Vector3 pos)
    {
        audioSource.pitch = 1f;
        audioSource.PlayOneShot(hitSound);
        GameObject GO = Instantiate(hitEffect, pos, Quaternion.identity);
        Destroy(GO, 20f);
    }

    // --------------------------- //
    // PROCESS MOVE & SPRINT LOGIC
    // --------------------------- //
    public void ProcessMove(Vector2 input, bool sprintRequested)
    {
        float dt = Time.deltaTime;
        bool wasSprinting = isSprinting;
        bool canSprintNow = sprintRequested && sprintAvailable && stamina > 0f;

        isSprinting = canSprintNow;

        if (isSprinting)
        {
            stamina -= staminaDrainRate * dt;
            if (stamina <= 0f)
            {
                stamina = 0f;
                isSprinting = false;
                sprintAvailable = false;
                sprintCooldownTimer = sprintCooldown;
                sprintTimer = 0f;
                regenDelayTimer = staminaRegenDelay;
            }
            else
            {
                sprintTimer += dt;
                if (sprintTimer >= sprintMaxDuration)
                {
                    isSprinting = false;
                    sprintAvailable = false;
                    sprintCooldownTimer = sprintCooldown;
                    sprintTimer = 0f;
                    regenDelayTimer = staminaRegenDelay;
                }
            }
            regenDelayTimer = staminaRegenDelay;
        }
        else
        {
            if (wasSprinting && !isSprinting)
            {
                if (stamina <= 0f || sprintTimer >= sprintMaxDuration)
                {
                    sprintAvailable = false;
                    sprintCooldownTimer = sprintCooldown;
                }
                regenDelayTimer = staminaRegenDelay;
            }

            if (!wasSprinting)
                sprintTimer = 0f;
        }

        if (!sprintAvailable)
        {
            sprintCooldownTimer -= dt;
            if (sprintCooldownTimer <= 0f)
            {
                sprintAvailable = true;
                sprintCooldownTimer = 0f;
            }
        }

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

        float currentSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);

        Vector3 moveDirection = Vector3.zero;
        moveDirection.x = input.x;
        moveDirection.z = input.y;

        Vector3 worldMovement = transform.TransformDirection(moveDirection);
        controller.Move(worldMovement * currentSpeed * dt);

        _playerVelocity.y += gravity * dt;
        if (isGrounded && _playerVelocity.y < 0)
            _playerVelocity.y = -2f;

        controller.Move(_playerVelocity * dt);
    }
}
