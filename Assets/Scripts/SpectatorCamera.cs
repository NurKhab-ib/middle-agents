using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public sealed class SpectatorCamera : MonoBehaviour {
    // the camera moves right away, and shift gives it a speed boost
    [SerializeField] private float moveSpeed = 50f;
    [SerializeField] private float shiftSpeedMultiplier = 2.5f;

    private Camera view;

    private void Awake() {
        view = GetComponent<Camera>();
        // an orthographic camera keeps the top-down pixel map flat
        view.orthographic = true;
        view.orthographicSize = 22f;
        view.backgroundColor = new Color(0.13f, 0.20f, 0.22f);
    }

    private void Update() {
        var keyboard = Keyboard.current;
        
        if (keyboard == null) {
            return;
        }

        // opposite keys cancel each other out
        Vector2 input = Vector2.zero;
        if (keyboard.wKey.isPressed) {
            input.y++;
        }

        if (keyboard.sKey.isPressed) {
            input.y--;
        }

        if (keyboard.dKey.isPressed) {
            input.x++;
        }

        if (keyboard.aKey.isPressed) {
            input.x--;
        }

        // normalized input keeps diagonal movement at the same speed
        bool shiftHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        float currentSpeed = moveSpeed * (shiftHeld ? shiftSpeedMultiplier : 1f);
        transform.position += (Vector3) (input.normalized * (currentSpeed * Time.deltaTime));

        var mouse = Mouse.current;
        
        if (mouse != null) {
            float wheel = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(wheel) > 0.01f) {
                // one wheel step changes the zoom right away
                view.orthographicSize = Mathf.Clamp(view.orthographicSize
                                                    * Mathf.Exp(-Mathf.Sign(wheel) * 0.15f), 7f, 75f);
            }
        }
    }
}
