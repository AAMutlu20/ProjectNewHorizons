using System.Collections.Generic;
using Core;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives a sequence of tutorial steps over your HUD: highlights a target with
/// TutorialSpotlight, shows a title + description near it, and advances on
/// "Next" or bails out entirely on "Skip".
///
/// Setup:
/// 1. Put this on the same GameObject as TutorialSpotlight (or a sibling that
///    references it), under your HUD Canvas, ABOVE the HUD elements in
///    hierarchy order (so it renders on top).
/// 2. Build a small text panel (Image background + TMP title + TMP description)
///    as a child, assign its RectTransform + texts below.
/// 3. Add a "Skip Tutorial" button positioned top-center, and a "Next" button
///    inside/near the text panel. Both buttons' GameObjects must be LATER
///    siblings than the spotlight's dark panels so they render on top and
///    remain clickable.
/// 4. Fill in the Steps list in the inspector: drag each HUD element's
///    RectTransform in, write a title/description.
/// 5. On your Play button's onClick, call BeginIfNotSeen() (or Begin() to
///    always show it regardless of PlayerPrefs).
/// </summary>
public class TutorialSequenceController : MonoBehaviour
{
    private enum PanelSide { Auto, Above, Below, Left, Right, Center }

    [System.Serializable]
    private class TutorialStep
    {
        public RectTransform target;
        [TextArea] public string title;
        [TextArea(2, 4)] public string description;
        [Tooltip("Auto picks based on target's screen position. Override if it looks wrong for a specific element.")]
        public PanelSide panelSide = PanelSide.Auto;
    }

    [Header("References")]
    [SerializeField] private TutorialSpotlight spotlight;
    [SerializeField] private RectTransform textPanelRect;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private TMP_Text nextButtonLabel; // so we can change it to "Got it!" on the last step

    [Header("Steps (in display order)")]
    [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();

    [Header("Panel placement")]
    [SerializeField] private float panelGap = 24f; // gap between spotlight edge and text panel

    [Header("Gameplay pausing")]
    [Tooltip("Registers a freeze with GameFreezeController while the tutorial is showing. " +
             "UI clicks (Next/Skip) still work fine at timeScale 0 since UI input isn't time-scaled.")]
    [SerializeField] private bool pauseGameplayDuringTutorial = true;

    private const string PrefsKey = "TutorialCompleted";
    private const string EnabledPrefsKey = "TutorialEnabled";
    private const string FreezeReason = "Tutorial";
    private int _currentIndex = -1;

    private void Awake()
    {
        nextButton.onClick.AddListener(Next);
        skipButton.onClick.AddListener(EndTutorial);
        gameObject.SetActive(false);
    }

    /// <summary>Call this from your Play button if you only want the tutorial shown once ever.</summary>
    public void BeginIfNotSeen()
    {
        if (PlayerPrefs.GetInt(EnabledPrefsKey, 1) == 0) return; // player turned it off in Settings
        if (PlayerPrefs.GetInt(PrefsKey, 0) == 1) return;
        Begin();
    }

    /// <summary>Always shows the tutorial from the start, regardless of prior completion.</summary>
    public void Begin()
    {
        _currentIndex = -1;
        gameObject.SetActive(true);
        if (pauseGameplayDuringTutorial) GameFreezeController.RequestFreeze(FreezeReason);
        Next();
    }

    private void Next()
    {
        _currentIndex++;
        if (_currentIndex >= steps.Count)
        {
            EndTutorial();
            return;
        }
        ShowStep(steps[_currentIndex]);
    }

    private void ShowStep(TutorialStep step)
    {
        spotlight.Show(step.target);
        titleText.text = step.title;
        descriptionText.text = step.description;
        nextButtonLabel.text = (_currentIndex == steps.Count - 1) ? "Got it!" : "Next";
        PositionPanel(step);
    }

    private void PositionPanel(TutorialStep step)
    {
        Canvas canvas = spotlight.GetComponentInParent<Canvas>();
        RectTransform overlayRoot = (RectTransform)spotlight.transform;

        Vector3[] corners = new Vector3[4];
        step.target.GetWorldCorners(corners);

        // Convert target's world corners into overlay-local space (same trick as the spotlight itself).
        Vector2 bl = ScreenSpaceToLocal(corners[0], canvas, overlayRoot);
        Vector2 tr = ScreenSpaceToLocal(corners[2], canvas, overlayRoot);
        Rect overlayRect = overlayRoot.rect;

        PanelSide side = step.panelSide;
        if (side == PanelSide.Auto)
        {
            float targetCenterY = (bl.y + tr.y) * 0.5f;
            float screenCenterY = (overlayRect.yMin + overlayRect.yMax) * 0.5f;
            side = targetCenterY > screenCenterY ? PanelSide.Below : PanelSide.Above;
        }

        Vector2 pos;
        switch (side)
        {
            case PanelSide.Above:
                pos = new Vector2((bl.x + tr.x) * 0.5f, tr.y + panelGap + textPanelRect.rect.height * 0.5f);
                break;
            case PanelSide.Below:
                pos = new Vector2((bl.x + tr.x) * 0.5f, bl.y - panelGap - textPanelRect.rect.height * 0.5f);
                break;
            case PanelSide.Left:
                pos = new Vector2(bl.x - panelGap - textPanelRect.rect.width * 0.5f, (bl.y + tr.y) * 0.5f);
                break;
            case PanelSide.Right:
                pos = new Vector2(tr.x + panelGap + textPanelRect.rect.width * 0.5f, (bl.y + tr.y) * 0.5f);
                break;
            default:
                pos = Vector2.zero; // Center
                break;
        }

        // Clamp so the panel never goes off-screen.
        float halfW = textPanelRect.rect.width * 0.5f;
        float halfH = textPanelRect.rect.height * 0.5f;
        pos.x = Mathf.Clamp(pos.x, overlayRect.xMin + halfW, overlayRect.xMax - halfW);
        pos.y = Mathf.Clamp(pos.y, overlayRect.yMin + halfH, overlayRect.yMax - halfH);

        // Set via world position rather than anchoredPosition: anchoredPosition's meaning
        // depends on textPanelRect's own anchor/pivot settings, which would need to exactly
        // match overlayRoot's for a direct assignment to land in the right place. Going
        // through world space sidesteps that entirely — this works no matter what anchor
        // preset the text panel has.
        Vector3 worldPos = overlayRoot.TransformPoint(pos);
        Debug.Log($"[Tutorial] target={step.target.name} side={side} bl={bl} tr={tr} pos={pos} worldPos={worldPos} panelPosBefore={textPanelRect.position}");
        textPanelRect.position = worldPos;
        Debug.Log($"[Tutorial] panelPosAfter={textPanelRect.position}");
    }

    private Vector2 ScreenSpaceToLocal(Vector3 worldPoint, Canvas canvas, RectTransform root)
    {
        Vector2 screenPoint = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? (Vector2)worldPoint
            : RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, worldPoint);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            root, screenPoint,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 localPoint);

        return localPoint;
    }

    private void EndTutorial()
    {
        spotlight.Hide();
        gameObject.SetActive(false);
        if (pauseGameplayDuringTutorial) GameFreezeController.ReleaseFreeze(FreezeReason);
        PlayerPrefs.SetInt(PrefsKey, 1);
        PlayerPrefs.Save();
    }
}
