using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Runtime UI for the 1080 x 1920 KitchenCost canvas. The calculator owns its data.
// This helper is constructed by KitchenCostCalculator; no scene component is needed.
public sealed class KitchenCostRecipeBrowser
{
    private static readonly Color32 Forest = new Color32(36, 72, 61, 255);
    private static readonly Color32 Cream = new Color32(250, 247, 242, 255);
    private static readonly Color32 Muted = new Color32(99, 114, 104, 255);
    private static readonly Color32 Sage = new Color32(229, 237, 223, 255);
    private static readonly Color32 Rose = new Color32(249, 231, 225, 255);
    private static readonly Color32 Red = new Color32(155, 65, 51, 255);

    private readonly Sprite roundedCard;
    private readonly Sprite roundedControl;
    private readonly Sprite roundedOutline;

    private readonly TMP_Text textStyle;
    private readonly GameObject screen;
    private readonly RectTransform content;
    private readonly ScrollRect scrollView;
    private readonly TMP_Text statusText;
    private readonly TMP_Text emptyText;
    private readonly List<GameObject> rows = new List<GameObject>();
    private readonly GameObject dialog;
    private readonly TMP_Text dialogTitle;
    private readonly TMP_Text dialogBody;
    private readonly Button dialogPrimaryButton;
    private readonly TMP_Text dialogPrimaryText;

    public bool IsVisible { get { return screen.activeSelf; } }

    public KitchenCostRecipeBrowser(RectTransform parent, TMP_Text fontSource,
        UnityAction onBack, UnityAction onNewRecipe)
    {
        textStyle = fontSource;
        roundedCard = Resources.Load<Sprite>("KitchenCostUI/RoundedCard");
        roundedControl = Resources.Load<Sprite>("KitchenCostUI/RoundedControl");
        roundedOutline = Resources.Load<Sprite>("KitchenCostUI/RoundedOutline");
        if (roundedCard == null || roundedControl == null || roundedOutline == null)
            Debug.LogWarning("KitchenCost: rounded UI sprites are missing. " +
                "Run Tools > KitchenCost > Create Rounded UI Sprites, then restart Play mode.");
        RectTransform screenRect = CreateRect(parent, "SavedRecipesScreen");
        Fill(screenRect);
        screen = screenRect.gameObject;

        TMP_Text title = CreateText(screenRect, "Title", "Saved recipes", 56f, Forest);
        title.fontStyle = FontStyles.Bold;
        TopStretch(title.rectTransform, 60f, 60f, 230f, 80f);
        TMP_Text subtitle = CreateText(screenRect, "Subtitle", "Open a recipe to view or edit it.", 36f, Muted);
        TopStretch(subtitle.rectTransform, 60f, 60f, 320f, 50f);
        statusText = CreateText(screenRect, "Message", string.Empty, 36f, Muted);
        statusText.enableWordWrapping = true;
        statusText.alignment = TextAlignmentOptions.TopLeft;
        TopStretch(statusText.rectTransform, 60f, 60f, 390f, 80f);

        RectTransform scrollRect = CreateRect(screenRect, "RecipesScrollView");
        Fill(scrollRect);
        scrollRect.offsetMin = new Vector2(60f, 480f);
        scrollRect.offsetMax = new Vector2(-60f, -490f);
        // Keep the list raycastable for scrolling, but match the page background
        // so the separate white recipe cards have visible rounded edges.
        AddImage(scrollRect, Cream);
        scrollView = scrollRect.gameObject.AddComponent<ScrollRect>();

        RectTransform viewport = CreateRect(scrollRect, "Viewport");
        Fill(viewport);
        AddImage(viewport, Cream);
        viewport.gameObject.AddComponent<RectMask2D>();

        content = CreateRect(viewport, "Content");
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 24, 24);
        layout.spacing = 24f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollView.viewport = viewport;
        scrollView.content = content;
        scrollView.horizontal = false;
        scrollView.vertical = true;
        scrollView.movementType = ScrollRect.MovementType.Clamped;
        scrollView.inertia = true;
        scrollView.scrollSensitivity = 30f;

        emptyText = CreateText(content, "EmptyMessage",
            "No saved recipes yet.\nCreate a recipe and choose Save recipe.", 36f, Muted);
        emptyText.enableWordWrapping = true;
        emptyText.alignment = TextAlignmentOptions.TopLeft;
        LayoutElement emptyLayout = emptyText.gameObject.AddComponent<LayoutElement>();
        emptyLayout.minHeight = 180f;
        emptyLayout.preferredHeight = 180f;

        Button back = CreateButton(screenRect, "BackToRecipeButton", "Back to recipe", Forest, Forest, onBack, true);
        BottomLeft(back.GetComponent<RectTransform>(), 60f, 80f, 960f, 150f);
        Button add = CreateButton(screenRect, "NewRecipeButton", "+ New recipe", Forest, Color.white, onNewRecipe);
        BottomLeft(add.GetComponent<RectTransform>(), 60f, 270f, 960f, 150f);

        // A separate sibling overlay can protect both the editor and the browser.
        RectTransform overlay = CreateRect(parent, "RecipeConfirmation");
        Fill(overlay);
        AddImage(overlay, new Color(0f, 0f, 0f, 0.5f));
        dialog = overlay.gameObject;
        RectTransform panel = CreateRect(overlay, "Dialog");
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(960f, 720f);
        ApplySprite(AddImage(panel, Color.white), roundedCard);

        dialogTitle = CreateText(panel, "Title", string.Empty, 46f, Forest);
        dialogTitle.fontStyle = FontStyles.Bold;
        TopStretch(dialogTitle.rectTransform, 48f, 48f, 40f, 70f);
        dialogBody = CreateText(panel, "Message", string.Empty, 40f, Forest);
        dialogBody.enableWordWrapping = true;
        dialogBody.alignment = TextAlignmentOptions.TopLeft;
        TopStretch(dialogBody.rectTransform, 48f, 48f, 130f, 350f);

        Button cancel = CreateButton(panel, "CancelButton", "Cancel", Sage, Forest, CloseConfirmation);
        BottomLeft(cancel.GetComponent<RectTransform>(), 48f, 40f, 414f, 150f);
        dialogPrimaryButton = CreateButton(panel, "ConfirmButton", "Confirm", Rose, Red, null);
        BottomLeft(dialogPrimaryButton.GetComponent<RectTransform>(), 498f, 40f, 414f, 150f);
        dialogPrimaryText = dialogPrimaryButton.GetComponentInChildren<TMP_Text>();

        dialog.SetActive(false);
        screen.SetActive(false);
    }

    public void SetVisible(bool visible)
    {
        screen.SetActive(visible);
    }

    public void BeginList(int count)
    {
        foreach (GameObject row in rows)
        {
            if (row == null)
                continue;
            row.SetActive(false);
            Object.Destroy(row);
        }
        rows.Clear();
        emptyText.gameObject.SetActive(count == 0);
        SetMessage(count == 1 ? "1 saved recipe" : count + " saved recipes", false);
    }

    public void AddRecipe(string name, string details, string cost, UnityAction onOpen, UnityAction onDelete)
    {
        GameObject row = KitchenCostIngredientRow.Create(content, textStyle, name, details, cost,
            onOpen, onDelete, "Open", "Delete");
        row.name = "SavedRecipeRow";
        rows.Add(row);
    }

    public void EndList()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        scrollView.StopMovement();
        scrollView.verticalNormalizedPosition = 1f;
    }

    public void SetMessage(string message, bool isError)
    {
        statusText.color = isError ? Red : Muted;
        statusText.text = message;
    }

    public void ShowConfirmation(string title, string message, string confirmLabel, UnityAction onConfirm)
    {
        dialogTitle.text = title;
        dialogBody.text = message;
        dialogPrimaryText.text = confirmLabel;
        dialogPrimaryButton.onClick.RemoveAllListeners();
        dialogPrimaryButton.onClick.AddListener(() =>
        {
            CloseConfirmation();
            if (onConfirm != null)
                onConfirm();
        });
        dialog.transform.SetAsLastSibling();
        dialog.SetActive(true);
    }

    private void CloseConfirmation()
    {
        dialog.SetActive(false);
    }

    private static RectTransform CreateRect(RectTransform parent, string name)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.layer = parent.gameObject.layer;
        var rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static Image AddImage(RectTransform rect, Color color)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return image;
    }

    private static void ApplySprite(Image image, Sprite sprite)
    {
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.fillCenter = true;
        image.pixelsPerUnitMultiplier = 1f;
    }

    private TMP_Text CreateText(RectTransform parent, string name, string value, float size, Color color)
    {
        RectTransform rect = CreateRect(parent, name);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (textStyle.font != null)
            text.font = textStyle.font;
        text.fontSize = size;
        text.fontStyle = FontStyles.Normal;
        text.color = color;
        text.richText = false;
        text.enableAutoSizing = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;
        text.text = value;
        return text;
    }

    private Button CreateButton(RectTransform parent, string name, string label,
        Color backgroundColor, Color textColor, UnityAction action, bool outlined = false)
    {
        RectTransform rect = CreateRect(parent, name);
        Image image = AddImage(rect, backgroundColor);
        ApplySprite(image, outlined ? roundedOutline : roundedControl);
        // A missing outline sprite must not leave green text on a green fill.
        if (outlined && roundedOutline == null)
            image.color = Cream;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.94f, 0.97f, 0.94f, 1f);
        colors.pressedColor = new Color(0.8f, 0.88f, 0.82f, 1f);
        colors.selectedColor = Color.white;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        button.colors = colors;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        if (action != null)
            button.onClick.AddListener(action);
        TMP_Text text = CreateText(rect, "Text", label, 40f, textColor);
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        Fill(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(12f, 4f);
        text.rectTransform.offsetMax = new Vector2(-12f, -4f);
        return button;
    }

    private static void Fill(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void TopStretch(RectTransform rect, float left, float right, float top, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.offsetMin = new Vector2(left, -top - height);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void BottomLeft(RectTransform rect, float left, float bottom, float width, float height)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(left, bottom);
        rect.sizeDelta = new Vector2(width, height);
    }
}
