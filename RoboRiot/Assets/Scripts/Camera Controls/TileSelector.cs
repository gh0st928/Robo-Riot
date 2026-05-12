using UnityEngine;
using UnityEngine.InputSystem;
using RoboRiot.Grid;

namespace RoboRiot.Controls
{
    /// <summary>
    /// Highlights hovered and selected tiles using the new Input System.
    /// Attach to the InputHandler GameObject.
    /// </summary>
    public class TileSelector : MonoBehaviour
    {
        [Header("Highlight Colours")]
        [SerializeField] private Color hoverColor    = new Color(1f, 1f, 1f, 0.4f);
        [SerializeField] private Color selectedColor = new Color(1f, 0.8f, 0f, 0.6f);

        private GridCell _hoveredCell;
        private GridCell _selectedCell;
        private Camera   _cam;

        // ---------------------------------------------------------------
        // Unity lifecycle
        // ---------------------------------------------------------------
        private void Awake()
        {
            _cam = Camera.main;
        }

        private void OnEnable()
        {
            if (InputHandler.Instance != null)
                InputHandler.Instance.OnWorldClicked.AddListener(OnWorldClicked);
        }

        private void OnDisable()
        {
            if (InputHandler.Instance != null)
                InputHandler.Instance.OnWorldClicked.RemoveListener(OnWorldClicked);
        }

        private void Update()
        {
            HandleHover();
        }

        // ---------------------------------------------------------------
        // Hover
        // ---------------------------------------------------------------
        private void HandleHover()
        {
            if (GridManager.Instance == null) return;
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 screenPos = mouse.position.ReadValue();
            Vector3 worldPos  = _cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
            worldPos.z = 0f;

            GridCell cell = GridManager.Instance.GetCellAtWorldPos(worldPos);
            if (cell == _hoveredCell) return;

            // Clear old hover
            if (_hoveredCell != null && _hoveredCell != _selectedCell)
                ClearHighlight(_hoveredCell);

            // Apply new hover
            _hoveredCell = cell;
            if (_hoveredCell != null && _hoveredCell != _selectedCell)
                SetHighlight(_hoveredCell, hoverColor);
        }

        // ---------------------------------------------------------------
        // Click selection
        // ---------------------------------------------------------------
        private void OnWorldClicked(Vector3 worldPos)
        {
            if (GridManager.Instance == null) return;

            GridCell cell = GridManager.Instance.GetCellAtWorldPos(worldPos);
            if (cell == null) return;

            if (_selectedCell != null) ClearHighlight(_selectedCell);

            _selectedCell = cell;
            SetHighlight(_selectedCell, selectedColor);

            Debug.Log($"[TileSelector] Selected: {cell}");
        }

        // ---------------------------------------------------------------
        // Highlight helpers
        // ---------------------------------------------------------------
        private void SetHighlight(GridCell cell, Color color)
        {
            var sr = GetDebugSprite(cell);
            if (sr != null) sr.color = color;
        }

        private void ClearHighlight(GridCell cell)
        {
            var sr = GetDebugSprite(cell);
            if (sr == null) return;

            var vis = FindObjectOfType<GridVisualizer>();
            if (vis != null) sr.color = vis.TileColor(cell.Type);
        }

        private SpriteRenderer GetDebugSprite(GridCell cell)
        {
            GameObject found = GameObject.Find($"Sprite ({cell.X},{cell.Y})");
            return found != null ? found.GetComponent<SpriteRenderer>() : null;
        }
    }
}