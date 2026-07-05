using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Darkens the screen except for a rectangular "spotlight" cut out around a target UI element.
/// Built from four Image panels (top/bottom/left/right of the highlight) rather than a shader,
/// so it works with any Canvas Scaler setup with zero extra materials.
///
/// Setup:
/// 1. Create an empty GameObject as a child of your HUD Canvas, name it "TutorialOverlay".
/// 2. Add this script to it.
/// 3. It will auto-create the four panel Images on Awake (or you can assign them manually
///    in the inspector if you want to control their color/material yourself).
/// 4. Call Show(targetRectTransform) to spotlight something, Hide() to clear it.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class TutorialSpotlight : MonoBehaviour
{
    [Header("Appearance")]
    [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.75f);
    [SerializeField] private float padding = 8f; // extra breathing room around the target, in pixels

    [Header("Optional border highlight")]
    [SerializeField] private bool showBorder = true;
    [SerializeField] private Color borderColor = Color.white;
    [SerializeField] private float borderThickness = 3f;

    private RectTransform _root;
    private Image _top, _bottom, _left, _right;
    private Image _border;
    private Canvas _canvas;

    private void Awake()
    {
        _root = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();

        // Force pivot to (0,0). Stretch anchors already lock this object's edges to its
        // parent regardless of pivot, so this doesn't move the overlay itself — but it
        // makes _root.rect start at (0,0) instead of (-halfWidth,-halfHeight), which is
        // what SetPanel()'s anchoredPosition math below assumes. Without this, every
        // panel is offset by half the screen size toward the bottom-left.
        _root.pivot = Vector2.zero;

        _top = CreatePanel("Panel_Top");
        _bottom = CreatePanel("Panel_Bottom");
        _left = CreatePanel("Panel_Left");
        _right = CreatePanel("Panel_Right");

        if (showBorder)
        {
            _border = CreatePanel("Border", isBorder: true);
            _border.color = Color.clear;
            var outline = _border.gameObject.AddComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(borderThickness, borderThickness);
        }

        gameObject.SetActive(false);
    }

    private Image CreatePanel(string name, bool isBorder = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(_root, false);
        go.transform.SetAsFirstSibling(); // stay behind any hand-placed children (text panel, skip button)
        var img = go.GetComponent<Image>();
        img.color = isBorder ? Color.clear : overlayColor;
        img.raycastTarget = !isBorder; // block clicks on the dark areas, let the border pass through
        return img;
    }

    /// <summary>
    /// Spotlight the given RectTransform. Call every frame (e.g. from LateUpdate) if the
    /// target can move/resize while the spotlight is showing.
    /// </summary>
    public void Show(RectTransform target)
    {
        gameObject.SetActive(true);

        // Get the target's screen-space corners, then convert into this overlay's local space.
        Vector3[] worldCorners = new Vector3[4];
        target.GetWorldCorners(worldCorners); // 0=bottomLeft,1=topLeft,2=topRight,3=bottomRight

        Vector2 bl = WorldToLocal(worldCorners[0]);
        Vector2 tr = WorldToLocal(worldCorners[2]);

        bl -= new Vector2(padding, padding);
        tr += new Vector2(padding, padding);

        Rect overlayRect = _root.rect;

        // Top panel: full width, from top of screen down to tr.y
        SetPanel(_top,
            new Vector2(overlayRect.xMin, tr.y),
            new Vector2(overlayRect.xMax, overlayRect.yMax));

        // Bottom panel: full width, from yMin up to bl.y
        SetPanel(_bottom,
            new Vector2(overlayRect.xMin, overlayRect.yMin),
            new Vector2(overlayRect.xMax, bl.y));

        // Left panel: between top and bottom panels, left edge to bl.x
        SetPanel(_left,
            new Vector2(overlayRect.xMin, bl.y),
            new Vector2(bl.x, tr.y));

        // Right panel: between top and bottom panels, tr.x to right edge
        SetPanel(_right,
            new Vector2(tr.x, bl.y),
            new Vector2(overlayRect.xMax, tr.y));

        if (showBorder && _border != null)
        {
            SetPanel(_border, bl, tr);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private Vector2 WorldToLocal(Vector3 worldPoint)
    {
        Vector2 screenPoint = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? (Vector2)worldPoint
            : RectTransformUtility.WorldToScreenPoint(_canvas.worldCamera, worldPoint);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _root, screenPoint, _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera, out Vector2 localPoint);

        return localPoint;
    }

    private void SetPanel(Image panel, Vector2 min, Vector2 max)
    {
        RectTransform rt = panel.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = min;
        rt.sizeDelta = max - min;
    }
}
