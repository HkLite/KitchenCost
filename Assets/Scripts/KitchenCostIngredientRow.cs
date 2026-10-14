using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Builds an ingredient or saved-recipe card inside a scroll view at runtime.
// This helper needs no prefab and is not attached to a scene GameObject.
public static class KitchenCostIngredientRow
{
    private const float CardHeight = 350f;
    private const string CardSpritePath = "KitchenCostUI/RoundedCard";
    private const string ControlSpritePath = "KitchenCostUI/RoundedControl";

    private static readonly Color32 Forest = new Color32(36, 72, 61, 255);
    private static readonly Color32 Muted = new Color32(99, 114, 104, 255);
    private static readonly Color32 Sage = new Color32(229, 237, 223, 255);
    private static readonly Color32 Rose = new Color32(249, 231, 225, 255);
    private static readonly Color32 Red = new Color32(155, 65, 51, 255);

    private static Sprite roundedCard;
    private static Sprite roundedControl;
    private static bool missingSpritesWarningShown;

    public static GameObject Create(RectTransform parent, TMP_Text textStyle,
        string ingredientName, string details, string cost,
        UnityAction onEdit, UnityAction onRemove,
        string editLabel = "Edit", string removeLabel = "Remove")
    {
        LoadRoundedSprites();

        var row = new GameObject("IngredientRow", typeof(RectTransform),
            typeof(Image), typeof(LayoutElement));
        row.layer = parent.gameObject.layer;
        var rect = row.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, CardHeight);

        LayoutElement layout = row.GetComponent<LayoutElement>();
        layout.minHeight = CardHeight;
        layout.preferredHeight = CardHeight;
        layout.flexibleHeight = 0f;
        layout.flexibleWidth = 1f;

        Image background = row.GetComponent<Image>();
        StyleImage(background, roundedCard, Color.white);
        // Keep the card raycastable so dragging its blank area scrolls the list.
        background.raycastTarget = true;

        TMP_Text nameText = CreateText(rect, textStyle, "Name", ingredientName, 40f, Forest);
        nameText.fontStyle = FontStyles.Bold;
        SetTopStretch(nameText.rectTransform, 28f, 340f, 20f, 60f);

        TMP_Text costText = CreateText(rect, textStyle, "Cost", cost, 38f, Forest);
        costText.fontStyle = FontStyles.Bold;
        costText.alignment = TextAlignmentOptions.MidlineRight;
        SetTopRight(costText.rectTransform, 28f, 20f, 290f, 60f);

        TMP_Text detailsText = CreateText(rect, textStyle, "Details", details, 36f, Muted);
        SetTopStretch(detailsText.rectTransform, 28f, 28f, 88f, 90f);
        detailsText.enableWordWrapping = true;
        detailsText.alignment = TextAlignmentOptions.TopLeft;

        CreateButton(rect, textStyle, editLabel, 332f, 280f, Sage, Forest, onEdit);
        CreateButton(rect, textStyle, removeLabel, 28f, 280f, Rose, Red, onRemove);
        return row;
    }

    private static void LoadRoundedSprites()
    {
        // The sprite builder places these in Assets/Resources/KitchenCostUI.
        // Resources paths omit both the Resources folder and the file extension.
        if (roundedCard == null)
            roundedCard = Resources.Load<Sprite>(CardSpritePath);
        if (roundedControl == null)
            roundedControl = Resources.Load<Sprite>(ControlSpritePath);

        if ((roundedCard == null || roundedControl == null) && !missingSpritesWarningShown)
        {
            missingSpritesWarningShown = true;
            Debug.LogWarning("KitchenCost: rounded UI sprites were not found. " +
                "Run Tools > KitchenCost > Create Rounded UI Sprites, then restart Play mode.");
        }
    }

    private static void StyleImage(Image image, Sprite sprite, Color color)
    {
        image.sprite = sprite;
        // Keep the card functional if a sprite has not been generated yet.
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.fillCenter = true;
        image.pixelsPerUnitMultiplier = 1f;
        image.color = color;
    }

    private static TMP_Text CreateText(RectTransform parent, TMP_Text textStyle,
        string objectName, string value, float fontSize, Color color)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.layer = parent.gameObject.layer;
        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.rectTransform.SetParent(parent, false);
        if (textStyle != null && textStyle.font != null)
            text.font = textStyle.font;
        text.fontSize = fontSize;
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

    private static void CreateButton(RectTransform parent, TMP_Text textStyle,
        string label, float right, float width, Color backgroundColor, Color textColor,
        UnityAction onClick)
    {
        var buttonObject = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.layer = parent.gameObject.layer;
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        // Match the larger controls used by the recipe and ingredient screens.
        SetTopRight(rect, right, 184f, width, 150f);

        Image background = buttonObject.GetComponent<Image>();
        StyleImage(background, roundedControl, backgroundColor);
        background.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
        colors.selectedColor = Color.white;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.1f;
        button.colors = colors;
        // Mobile rows use direct taps. Avoid navigation selecting a row that gets
        // destroyed and recreated after an ingredient changes.
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(onClick);

        TMP_Text buttonText = CreateText(rect, textStyle, "Text", label, 40f, textColor);
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.rectTransform.anchorMin = Vector2.zero;
        buttonText.rectTransform.anchorMax = Vector2.one;
        buttonText.rectTransform.offsetMin = new Vector2(10f, 4f);
        buttonText.rectTransform.offsetMax = new Vector2(-10f, -4f);
    }

    private static void SetTopStretch(RectTransform rect, float left, float right, float top, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.offsetMin = new Vector2(left, -top - height);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void SetTopRight(RectTransform rect, float right, float top, float width, float height)
    {
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-right, -top);
        rect.sizeDelta = new Vector2(width, height);
    }
}
