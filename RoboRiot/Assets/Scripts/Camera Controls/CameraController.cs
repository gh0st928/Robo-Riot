using UnityEngine;
using UnityEngine.InputSystem;

namespace RoboRiot.Controls
{
    /// <summary>
    /// 2D tactical camera using Unity's new Input System.
    /// - Middle mouse drag to pan
    /// - Scroll wheel to zoom
    /// - Optional edge scrolling
    /// Attach to Main Camera.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Pan Settings")]
        [SerializeField] private bool  edgeScrolling   = false;
        [SerializeField] private float edgeScrollSpeed = 5f;
        [SerializeField] private float edgeThreshold   = 20f;

        [Header("Zoom Settings")]
        [SerializeField] private float zoomSpeed = 2f;
        [SerializeField] private float minZoom   = 3f;
        [SerializeField] private float maxZoom   = 12f;

        [Header("Bounds")]
        [SerializeField] private bool  clampToBounds = true;
        [SerializeField] private float minX = 0f;
        [SerializeField] private float maxX = 10f;
        [SerializeField] private float minY = 0f;
        [SerializeField] private float maxY = 10f;

        private Camera  _cam;
        private Vector3 _dragOrigin;
        private bool    _isDragging;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
        }

        private void Update()
        {
            HandleDrag();
            HandleZoom();
            if (edgeScrolling)   HandleEdgeScroll();
            if (clampToBounds)   ClampPosition();
        }

        // ---------------------------------------------------------------
        // Middle mouse drag
        // ---------------------------------------------------------------
        private void HandleDrag()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.middleButton.wasPressedThisFrame)
            {
                Vector2 screenPos = mouse.position.ReadValue();
                _dragOrigin = _cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
                _isDragging = true;
            }

            if (mouse.middleButton.wasReleasedThisFrame)
                _isDragging = false;

            if (_isDragging)
            {
                Vector2 screenPos  = mouse.position.ReadValue();
                Vector3 currentPos = _cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
                Vector3 delta      = _dragOrigin - currentPos;
                transform.position += new Vector3(delta.x, delta.y, 0f);
            }
        }

        // ---------------------------------------------------------------
        // Scroll zoom
        // ---------------------------------------------------------------
        private void HandleZoom()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Approximately(scroll, 0f)) return;

            _cam.orthographicSize -= scroll * zoomSpeed * Time.deltaTime * 10f;
            _cam.orthographicSize  = Mathf.Clamp(_cam.orthographicSize, minZoom, maxZoom);
        }

        // ---------------------------------------------------------------
        // Edge scrolling
        // ---------------------------------------------------------------
        private void HandleEdgeScroll()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2  mp   = mouse.position.ReadValue();
            Vector3  move = Vector3.zero;

            if (mp.x < edgeThreshold)                    move.x = -1f;
            if (mp.x > Screen.width  - edgeThreshold)    move.x =  1f;
            if (mp.y < edgeThreshold)                    move.y = -1f;
            if (mp.y > Screen.height - edgeThreshold)    move.y =  1f;

            transform.position += move * edgeScrollSpeed * Time.deltaTime;
        }

        // ---------------------------------------------------------------
        // Clamp
        // ---------------------------------------------------------------
        private void ClampPosition()
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            transform.position = pos;
        }

        // ---------------------------------------------------------------
        // Auto-set bounds from grid size
        // ---------------------------------------------------------------
        public void SetBounds(float width, float height, float cellSize)
        {
            minX = 0f;
            minY = 0f;
            maxX = width  * cellSize;
            maxY = height * cellSize;
        }
    }
}