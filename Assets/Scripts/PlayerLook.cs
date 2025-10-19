using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    public Camera cam;
    private float xRotation = 0f;
    public float xSensitivity = 30f;
    public float ySenitivity = 30f;

    // smoothing
    public float smoothTime = 0.03f;
    private Vector2 smoothLook = Vector2.zero;
    private Vector2 lookVelocity = Vector2.zero;

    public void ProcessLook(Vector2 input)
    {
        // Detect if a mouse is present and using delta; mouse input is already per-frame delta -> don't multiply by Time.deltaTime.
        bool usingMouse = Mouse.current != null;

        Vector2 target = input;
        if (!usingMouse)
        {
            // for gamepad/joystick (normalized stick), scale by deltaTime so it's frame-rate independent
            target *= Time.deltaTime * 100f; // 100f to keep sensitivity similar to mouse; tweak if needed
        }

        // smooth the input to reduce jerks
        smoothLook = Vector2.SmoothDamp(smoothLook, target, ref lookVelocity, smoothTime);

        float mouseX = smoothLook.x * xSensitivity;
        float mouseY = smoothLook.y * ySenitivity;

        // up and down
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        cam.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);

        // left and right
        transform.Rotate(Vector3.up * mouseX);
    }   
}
