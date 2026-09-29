using UnityEngine;
using UnityEngine.InputSystem;

namespace MiddleAgents
{
    [RequireComponent(typeof(Camera))]
    public sealed class SpectatorCamera : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 30f;
        [SerializeField] private float acceleration = 150f;
        [SerializeField] private float fastMultiplier = 3.5f;
        [SerializeField] private float slowMultiplier = 0.3f;
        [SerializeField] private float zoomSpeed = 5f;

        private Camera view;
        private Vector2 velocity;
        private float targetZoom = 22f;

        private void Awake()
        {
            view = GetComponent<Camera>();
            view.orthographic = true;
            view.orthographicSize = targetZoom;
            view.backgroundColor = new Color(0.13f, 0.20f, 0.22f);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard == null) return;

            Vector2 input = Vector2.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y++;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y--;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x++;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x--;

            float multiplier = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed
                ? fastMultiplier : keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed
                ? slowMultiplier : 1f;
            Vector2 wanted = input.normalized * moveSpeed * multiplier * (view.orthographicSize / 22f);
            velocity = Vector2.MoveTowards(velocity, wanted, acceleration * Time.unscaledDeltaTime);
            transform.position += (Vector3)(velocity * Time.unscaledDeltaTime);

            if (mouse != null)
            {
                if (mouse.rightButton.isPressed)
                {
                    Vector2 drag = mouse.delta.ReadValue();
                    float unitsPerPixel = view.orthographicSize * 2f / Screen.height;
                    transform.position -= (Vector3)(drag * unitsPerPixel);
                }

                float wheel = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(wheel) > 0.01f)
                    targetZoom = Mathf.Clamp(targetZoom * Mathf.Exp(-Mathf.Sign(wheel) * 0.15f), 7f, 75f);
            }

            view.orthographicSize = Mathf.MoveTowards(view.orthographicSize, targetZoom,
                zoomSpeed * Mathf.Max(1f, view.orthographicSize * 0.5f) * Time.unscaledDeltaTime);
        }
    }
}
