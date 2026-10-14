using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// A read-only cost breakdown, built at runtime for the existing 1080 x 1920 canvas.
public sealed class KitchenCostSummaryScreen
{
    private static readonly Color32 Forest = new Color32(36, 72, 61, 255);
    private static readonly Color32 Cream = new Color32(250, 247, 242, 255);
    private static readonly Color32 Muted = new Color32(99, 114, 104, 255);
    private static readonly Color32 Sage = new Color32(229, 237, 223, 255);
    private static readonly Color32 Peach = new Color32(242, 223, 201, 255);
    private readonly Sprite roundedCard;
    private readonly Sprite roundedOutline;

    private readonly TMP_Text textStyle;
    private readonly GameObject screen;
    private readonly TMP_Text recipeNameText;
    private readonly TMP_Text totalText;
    private readonly TMP_Text perServingText;
    private readonly TMP_Text recipeDetailsText;
    private readonly TMP_Text explanationText;
    private readonly RectTransform content;
    private readonly ScrollRect scrollView;
    private readonly List<GameObject> rows = new List<GameObject>();

    public KitchenCostSummaryScreen(RectTransform parent, TMP_Text fontSource, UnityAction onBack)
    {
        textStyle = fontSource;
        roundedCard = Resources.Load<Sprite>("KitchenCostUI/RoundedCard");
        roundedOutline = Resources.Load<Sprite>("KitchenCostUI/RoundedOutline");
        if (roundedCard == null || roundedOutline == null)
            Debug.LogWarning("KitchenCost: rounded UI sprites are missing. " +
                "Run Tools > KitchenCost > Create Rounded UI Sprites, then restart Play mode.");
        RectTransform root = CreateRect(parent, "CostSummaryScreen");
        Fill(root);
        screen = root.gameObject;

        TMP_Text title = CreateText(root, "Title", "Cost summary", 56f, Forest);
        title.fontStyle = FontStyles.Bold;
        TopStretch(title.rectTransform, 60f, 60f, 230f, 80f);
        recipeNameText = CreateText(root, "RecipeName", string.Empty, 40f, Forest);
        TopStretch(recipeNameText.rectTransform, 60f, 60f, 330f, 60f);

        RectTransform metrics = CreateRect(root, "CostCard");
        TopStretch(metrics, 60f, 60f, 430f, 290f);
        ApplySprite(AddImage(metrics, Peach, false), roundedCard);
        TMP_Text totalLabel = CreateText(metrics, "TotalLabel", "Ingredient total", 36f, Forest);
        TopStretch(totalLabel.rectTransform, 32f, 480f, 28f, 54f);
        totalText = CreateText(metrics, "Total", string.Empty, 52f, Forest);
        totalText.fontStyle = FontStyles.Bold;
        TopStretch(totalText.rectTransform, 32f, 480f, 84f, 78f);

        TMP_Text perLabel = CreateText(metrics, "PerServingLabel", "Per serving", 36f, Forest);
        TopStretch(perLabel.rectTransform, 520f, 32f, 28f, 54f);
        perServingText = CreateText(metrics, "PerServing", string.Empty, 52f, Forest);
        perServingText.fontStyle = FontStyles.Bold;
        TopStretch(perServingText.rectTransform, 520f, 32f, 84f, 78f);
        recipeDetailsText = CreateText(metrics, "RecipeDetails", string.Empty, 36f, Forest);
        TopStretch(recipeDetailsText.rectTransform, 32f, 32f, 190f, 60f);

        TMP_Text heading = CreateText(root, "BreakdownHeading", "Ingredient breakdown", 42f, Forest);
        heading.fontStyle = FontStyles.Bold;
        TopStretch(heading.rectTransform, 60f, 60f, 750f, 60f);
        explanationText = CreateText(root, "Explanation", string.Empty, 36f, Muted);
        explanationText.enableWordWrapping = true;
        explanationText.alignment = TextAlignmentOptions.TopLeft;
        TopStretch(explanationText.rectTransform, 60f, 60f, 825f, 100f);

        RectTransform list = CreateRect(root, "BreakdownScrollView");
        Fill(list);
        list.offsetMin = new Vector2(60f, 290f);
        list.offsetMax = new Vector2(-60f, -950f);
        AddImage(list, Cream, true);
        scrollView = list.gameObject.AddComponent<ScrollRect>();
        RectTransform viewport = CreateRect(list, "Viewport");
        Fill(viewport);
        AddImage(viewport, Cream, true);
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

        RectTransform backRect = CreateRect(root, "BackToRecipeButton");
        backRect.anchorMin = new Vector2(0f, 0f);
        backRect.anchorMax = new Vector2(1f, 0f);
        backRect.pivot = new Vector2(0.5f, 0f);
        backRect.offsetMin = new Vector2(60f, 80f);
        backRect.offsetMax = new Vector2(-60f, 230f);
        Image backImage = AddImage(backRect, Forest, true);
        ApplySprite(backImage, roundedOutline);
        if (roundedOutline == null)
            backImage.color = Cream;
        Button back = backRect.gameObject.AddComponent<Button>();
        back.targetGraphic = backImage;
        back.navigation = new Navigation { mode = Navigation.Mode.None };
        back.onClick.AddListener(onBack);
        ColorBlock colors = back.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.94f, 0.97f, 0.94f, 1f);
        colors.pressedColor = new Color(0.8f, 0.88f, 0.82f, 1f);
        colors.selectedColor = Color.white;
        colors.colorMultiplier = 1f;
        back.colors = colors;
        TMP_Text backText = CreateText(backRect, "Text", "Back to recipe", 40f, Forest);
        backText.fontStyle = FontStyles.Bold;
        backText.alignment = TextAlignmentOptions.Center;
        Fill(backText.rectTransform);
        screen.SetActive(false);
    }

    public void SetVisible(bool visible)
    {
        screen.SetActive(visible);
    }

    public void BeginSummary(string recipeName, string total, string perServing,
        int servings, int ingredientCount, bool allCostsZero)
    {
        foreach (GameObject row in rows)
        {
            if (row == null)
                continue;
            row.SetActive(false);
            Object.Destroy(row);
        }
        rows.Clear();
        recipeNameText.text = recipeName;
        totalText.text = total;
        perServingText.text = perServing;
        recipeDetailsText.text = servings + (servings == 1 ? " serving" : " servings") +
            "  |  " + ingredientCount + (ingredientCount == 1 ? " ingredient" : " ingredients");
        explanationText.text = allCostsZero
            ? "All ingredient costs are zero."
            : "Largest costs first. Percentages show each ingredient's share of the total.";
    }

    public void AddIngredient(string name, string quantity, string cost, string percentage, float share)
    {
        RectTransform row = CreateRect(content, "CostBreakdownRow");
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = new Vector2(1f, 1f);
        row.pivot = new Vector2(0.5f, 1f);
        row.sizeDelta = new Vector2(0f, 220f);
        ApplySprite(AddImage(row, Color.white, true), roundedCard);
        LayoutElement element = row.gameObject.AddComponent<LayoutElement>();
        element.minHeight = 220f;
        element.preferredHeight = 220f;
        element.flexibleHeight = 0f;
        element.flexibleWidth = 1f;

        TMP_Text nameLabel = CreateText(row, "Name", name, 40f, Forest);
        nameLabel.fontStyle = FontStyles.Bold;
        TopStretch(nameLabel.rectTransform, 24f, 340f, 20f, 60f);
        TMP_Text costLabel = CreateText(row, "Cost", cost, 38f, Forest);
        costLabel.fontStyle = FontStyles.Bold;
        costLabel.alignment = TextAlignmentOptions.MidlineRight;
        TopRight(costLabel.rectTransform, 24f, 20f, 290f, 60f);
        TMP_Text quantityLabel = CreateText(row, "Quantity", quantity, 36f, Muted);
        TopStretch(quantityLabel.rectTransform, 24f, 220f, 100f, 54f);
        TMP_Text shareLabel = CreateText(row, "Percentage", percentage, 36f, Forest);
        shareLabel.alignment = TextAlignmentOptions.MidlineRight;
        TopRight(shareLabel.rectTransform, 24f, 100f, 180f, 54f);

        RectTransform track = CreateRect(row, "ShareTrack");
        TopStretch(track, 24f, 24f, 178f, 16f);
        AddImage(track, Sage, false);
        RectTransform fill = CreateRect(track, "ShareFill");
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(Mathf.Clamp01(share), 1f);
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        AddImage(fill, Forest, false);
        rows.Add(row.gameObject);
    }

    public void EndSummary()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        scrollView.StopMovement();
        scrollView.verticalNormalizedPosition = 1f;
    }

    private static RectTransform CreateRect(RectTransform parent, string name)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.layer = parent.gameObject.layer;
        var rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static Image AddImage(RectTransform rect, Color color, bool raycast)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
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

    private static void TopRight(RectTransform rect, float right, float top, float width, float height)
    {
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-right, -top);
        rect.sizeDelta = new Vector2(width, height);
    }
}
