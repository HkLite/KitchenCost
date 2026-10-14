using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// KitchenCost: calculate ingredient costs and manage recipes saved on this device.
// Both quantity fields use grams. Only added ingredients are included in a recipe.
public class KitchenCostCalculator : MonoBehaviour
{
    [SerializeField] private TMP_InputField recipeNameInput;
    [SerializeField] private TMP_InputField ingredientNameInput;
    [SerializeField] private TMP_InputField usedQuantityInput;
    [SerializeField] private TMP_InputField packSizeInput;
    [SerializeField] private TMP_InputField packPriceInput;
    [SerializeField] private TMP_InputField servingsInput;
    [SerializeField] private TMP_Text ingredientListText;
    [SerializeField] private TMP_Text resultText;

    [Header("Ingredient panel (optional)")]
    [SerializeField] private GameObject editorScreen;
    [SerializeField] private GameObject ingredientPanel;
    [SerializeField] private TMP_Text ingredientMessageText;

    [Header("Ingredient editing")]
    [SerializeField] private TMP_Text ingredientTitleText;
    [SerializeField] private TMP_Text ingredientSubmitText;

    private const string SavedRecipeKey = "KitchenCost.SavedRecipe.v1";
    private const string RecipeCollectionKey = "KitchenCost.SavedRecipes.v1";
    private readonly List<IngredientEntry> ingredients = new List<IngredientEntry>();
    private readonly List<GameObject> ingredientRows = new List<GameObject>();
    private int editingIngredientIndex = -1;
    private RecipeCollectionData recipeCollection;
    private string currentRecipeId;
    private string cleanRecipeSignature;
    private KitchenCostRecipeBrowser recipeBrowser;
    private KitchenCostSummaryScreen summaryScreen;

    private sealed class IngredientEntry
    {
        public string Name;
        public decimal UsedQuantity;
        public decimal PackSize;
        public decimal PackPrice;
        public decimal Cost;
    }

    [Serializable]
    private sealed class RecipeSaveData
    {
        public int version;
        public string recipeName;
        public int servings;
        public List<IngredientSaveData> ingredients;
    }

    [Serializable]
    private sealed class IngredientSaveData
    {
        public string name;
        // Store decimal values as invariant strings for an exact JSON round trip.
        public string usedQuantity;
        public string packSize;
        public string packPrice;
    }

    [Serializable]
    private sealed class StoredRecipeData
    {
        public string id;
        public RecipeSaveData recipe;
    }

    [Serializable]
    private sealed class RecipeCollectionData
    {
        public int version;
        public List<StoredRecipeData> recipes;
    }

    [Serializable]
    private sealed class RecipeDraftData
    {
        public string recipeName;
        public string servingsText;
        public List<IngredientSaveData> ingredients;
    }

    private void Start()
    {
        if (!HasConnectedFields())
            return;

        servingsInput.onValueChanged.AddListener(OnServingsChanged);
        SetIngredientEditMode(-1);
        SetIngredientPanelVisible(false);
        if (ingredientMessageText != null)
            ingredientMessageText.text = string.Empty;
        RefreshIngredientList();
        resultText.text = "Add ingredients to calculate your recipe cost.";
        MarkRecipeClean();
    }

    private void OnDestroy()
    {
        if (servingsInput != null)
            servingsInput.onValueChanged.RemoveListener(OnServingsChanged);
    }

    private void OnServingsChanged(string value)
    {
        if (ingredients.Count > 0 && resultText != null)
            ShowTotal(GetRecipeTotal());
    }

    public void OpenIngredientPanel()
    {
        if (!HasConnectedFields())
            return;

        if (!HasConnectedIngredientPanel())
            return;

        // Opening Add after an unfinished edit must never reuse that edit's target.
        if (editingIngredientIndex >= 0)
            ClearIngredientInputs();
        SetIngredientEditMode(-1);
        ingredientMessageText.text = string.Empty;
        SetIngredientPanelVisible(true);
    }

    public void EditIngredient(int index)
    {
        if (!HasConnectedFields() || !HasConnectedIngredientPanel())
            return;

        if (index < 0 || index >= ingredients.Count)
            return;

        if (ingredientTitleText == null || ingredientSubmitText == null)
        {
            resultText.text = "Setup needed: connect Ingredient Title Text and Ingredient Submit Text on AppCanvas.";
            return;
        }

        // The form is a draft. The stored ingredient changes only on successful submit.
        IngredientEntry ingredient = ingredients[index];
        SetIngredientEditMode(index);
        ingredientNameInput.SetTextWithoutNotify(ingredient.Name);
        usedQuantityInput.SetTextWithoutNotify(ingredient.UsedQuantity.ToString(CultureInfo.InvariantCulture));
        packSizeInput.SetTextWithoutNotify(ingredient.PackSize.ToString(CultureInfo.InvariantCulture));
        packPriceInput.SetTextWithoutNotify(ingredient.PackPrice.ToString(CultureInfo.InvariantCulture));
        ingredientMessageText.text = string.Empty;
        SetIngredientPanelVisible(true);
    }

    public void RemoveIngredient(int index)
    {
        if (!HasConnectedFields() || index < 0 || index >= ingredients.Count)
            return;

        ingredients.RemoveAt(index);

        // Keep an open edit attached to its original item if an earlier item is removed.
        if (editingIngredientIndex == index)
        {
            ClearIngredientInputs();
            SetIngredientEditMode(-1);
            SetIngredientPanelVisible(false);
        }
        else if (editingIngredientIndex > index)
        {
            editingIngredientIndex--;
        }

        RefreshIngredientList();
        ShowCurrentRecipeTotal();
    }

    public void CancelIngredient()
    {
        if (!HasConnectedFields())
            return;

        // Cancel discards only the form draft, including unsaved edits.
        ClearIngredientInputs();
        SetIngredientEditMode(-1);
        SetIngredientPanelVisible(false);
        ShowCurrentRecipeTotal();
    }

    private void ShowCurrentRecipeTotal()
    {
        if (ingredients.Count == 0)
            resultText.text = "Add ingredients to calculate your recipe cost.";
        else
            ShowTotal(GetRecipeTotal());
    }

    private bool HasConnectedIngredientPanel()
    {
        if (editorScreen != null && ingredientPanel != null && ingredientMessageText != null)
            return true;

        resultText.text = "Setup needed: connect Editor Screen, Ingredient Panel and Ingredient Message Text on AppCanvas.";
        return false;
    }

    private void SetIngredientEditMode(int index)
    {
        editingIngredientIndex = index;
        bool isEditing = index >= 0;
        if (ingredientTitleText != null)
            ingredientTitleText.text = isEditing ? "Edit ingredient" : "Add ingredient";
        if (ingredientSubmitText != null)
            ingredientSubmitText.text = isEditing ? "Save changes" : "Add ingredient";
    }

    private void SetIngredientPanelVisible(bool visible)
    {
        if (recipeBrowser != null)
            recipeBrowser.SetVisible(false);
        if (summaryScreen != null)
            summaryScreen.SetVisible(false);

        // Leaving these fields empty keeps the original single-screen scene working.
        if (editorScreen == null || ingredientPanel == null)
            return;

        if (visible)
        {
            ingredientPanel.SetActive(true);
            editorScreen.SetActive(false);
        }
        else
        {
            editorScreen.SetActive(true);
            ingredientPanel.SetActive(false);
        }
    }

    private void ShowIngredientError(string message)
    {
        if (ingredientMessageText != null && ingredientPanel != null && ingredientPanel.activeInHierarchy)
            ingredientMessageText.text = message;
        else if (resultText != null)
            resultText.text = message;
    }

    public void AddIngredient()
    {
        if (!HasConnectedFields())
            return;

        if (editingIngredientIndex >= ingredients.Count)
        {
            ShowIngredientError("This ingredient is no longer available. Press Cancel and reopen the form.");
            return;
        }

        string ingredientName = CleanName(ingredientNameInput.text);

        if (string.IsNullOrWhiteSpace(ingredientName))
        {
            ShowIngredientError("Enter an ingredient name, such as Flour.");
            return;
        }

        if (!TryReadDecimal(usedQuantityInput.text, out decimal usedQuantity) || usedQuantity <= 0m)
        {
            ShowIngredientError("Enter an amount used greater than 0. Use a decimal point if needed.");
            return;
        }

        if (!TryReadDecimal(packSizeInput.text, out decimal packSize) || packSize <= 0m)
        {
            ShowIngredientError("Enter a pack size greater than 0. Use a decimal point if needed.");
            return;
        }

        if (!TryReadDecimal(packPriceInput.text, out decimal packPrice) || packPrice < 0m)
        {
            ShowIngredientError("Enter a pack price of 0 or more. Use a decimal point if needed.");
            return;
        }

        try
        {
            decimal ingredientCost = (usedQuantity / packSize) * packPrice;

            // Exclude the original item before adding its replacement. Validate the
            // entire new total before mutating anything, including on overflow.
            decimal newTotal = GetRecipeTotalExcluding(editingIngredientIndex) + ingredientCost;

            var ingredient = new IngredientEntry
            {
                Name = ingredientName,
                UsedQuantity = usedQuantity,
                PackSize = packSize,
                PackPrice = packPrice,
                Cost = ingredientCost
            };

            if (editingIngredientIndex >= 0)
                ingredients[editingIngredientIndex] = ingredient;
            else
                ingredients.Add(ingredient);

            RefreshIngredientList();
            ClearIngredientInputs();
            SetIngredientEditMode(-1);
            ShowTotal(newTotal);
            SetIngredientPanelVisible(false);
        }
        catch (OverflowException)
        {
            ShowIngredientError("These values are too large to calculate. Enter smaller amounts.");
        }
    }

    public void Calculate()
    {
        if (!HasConnectedFields())
            return;

        if (ingredients.Count == 0)
        {
            resultText.text = "Add at least one ingredient first.";
            return;
        }

        try
        {
            // Only ingredients already added to the list contribute to this total.
            decimal total = GetRecipeTotal();
            ShowTotal(total);
            if (int.TryParse(servingsInput.text, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out int servings) && servings > 0)
                OpenCostSummary(total, servings);
        }
        catch (OverflowException)
        {
            resultText.text = "The recipe total is too large to calculate.";
        }
    }

    private void OpenCostSummary(decimal total, int servings)
    {
        // The original single-screen scene can still calculate without extra panels.
        if (editorScreen == null || ingredientPanel == null)
            return;

        if (summaryScreen == null)
        {
            RectTransform parent = editorScreen.transform.parent as RectTransform;
            if (parent == null)
            {
                resultText.text = "Setup needed: Editor Screen must be inside the Background UI object.";
                return;
            }
            summaryScreen = new KitchenCostSummaryScreen(parent, ingredientListText,
                () => SetIngredientPanelVisible(false));
        }

        if (recipeBrowser != null)
            recipeBrowser.SetVisible(false);
        editorScreen.SetActive(false);
        ingredientPanel.SetActive(false);
        summaryScreen.SetVisible(true);

        string recipeName = CleanName(recipeNameInput.text);
        summaryScreen.BeginSummary(string.IsNullOrWhiteSpace(recipeName) ? "Untitled recipe" : recipeName,
            "PKR " + FormatMoney(total), "PKR " + FormatMoney(total / servings),
            servings, ingredients.Count, total == 0m);

        // Sort a copy for the breakdown. The editor's ingredient order stays intact.
        var ordered = new List<IngredientEntry>(ingredients);
        ordered.Sort((a, b) =>
        {
            int costOrder = b.Cost.CompareTo(a.Cost);
            return costOrder != 0 ? costOrder : StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name);
        });
        foreach (IngredientEntry ingredient in ordered)
        {
            // Divide first so very large valid costs do not overflow on cost * 100.
            decimal share = total > 0m ? ingredient.Cost / total : 0m;
            string percentage = Math.Round(share * 100m, 1, MidpointRounding.AwayFromZero)
                .ToString("0.#", CultureInfo.InvariantCulture) + "%";
            summaryScreen.AddIngredient(ingredient.Name,
                ingredient.UsedQuantity.ToString(CultureInfo.InvariantCulture) + " g used",
                "PKR " + FormatMoney(ingredient.Cost), percentage, (float)share);
        }
        summaryScreen.EndSummary();
    }

    public void RemoveLastIngredient()
    {
        if (!HasConnectedFields())
            return;

        if (ingredients.Count == 0)
        {
            resultText.text = "There are no ingredients to remove.";
            return;
        }

        RemoveIngredient(ingredients.Count - 1);
    }

    public void NewRecipe()
    {
        if (!HasConnectedFields())
            return;

        ConfirmDiscardChanges(StartNewRecipe);
    }

    private void StartNewRecipe()
    {
        // Starting a draft never deletes a recipe from the saved collection.
        ingredients.Clear();
        currentRecipeId = null;
        recipeNameInput.SetTextWithoutNotify(string.Empty);
        servingsInput.SetTextWithoutNotify("1");
        ClearIngredientInputs();
        SetIngredientEditMode(-1);
        SetIngredientPanelVisible(false);
        RefreshIngredientList();
        resultText.text = "New recipe. Enter a name and add ingredients.";
        MarkRecipeClean();
    }

    public void SaveRecipe()
    {
        if (!HasConnectedFields())
            return;

        string recipeName = CleanName(recipeNameInput.text);
        if (string.IsNullOrWhiteSpace(recipeName))
        {
            resultText.text = "Enter a recipe name before saving.";
            return;
        }

        if (!int.TryParse(servingsInput.text, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out int servings) || servings <= 0)
        {
            resultText.text = "Enter a whole number of servings greater than 0 before saving.";
            return;
        }

        if (editingIngredientIndex >= 0 || HasIngredientDraft())
        {
            ShowIngredientError("Finish the ingredient with Add ingredient or Save changes, or press Cancel, before saving the recipe.");
            return;
        }

        if (ingredients.Count == 0)
        {
            resultText.text = "Add at least one ingredient before saving.";
            return;
        }

        if (!EnsureRecipeCollectionLoaded())
            return;

        foreach (StoredRecipeData stored in recipeCollection.recipes)
        {
            if (stored.id != currentRecipeId && string.Equals(CleanName(stored.recipe.recipeName),
                    recipeName, StringComparison.OrdinalIgnoreCase))
            {
                resultText.text = "That recipe name is already saved. Choose another name, or open that recipe to update it.";
                return;
            }
        }

        var savedRecipe = new RecipeSaveData
        {
            version = 1,
            recipeName = recipeName,
            servings = servings,
            ingredients = CopyIngredientSaveData()
        };
        int previousIndex = FindSavedRecipeIndex(currentRecipeId);
        string savedId = previousIndex >= 0 ? currentRecipeId : Guid.NewGuid().ToString("N");
        var next = CopyRecipeCollection();
        if (previousIndex >= 0)
            next.recipes.RemoveAt(previousIndex);
        next.recipes.Insert(0, new StoredRecipeData { id = savedId, recipe = savedRecipe });

        // Commit the in-memory collection only after its complete replacement saves.
        if (!TryWriteRecipeCollection(next, out string saveError))
        {
            ShowRecipeError(saveError);
            return;
        }
        recipeCollection = next;
        currentRecipeId = savedId;
        recipeNameInput.SetTextWithoutNotify(recipeName);
        servingsInput.SetTextWithoutNotify(servings.ToString(CultureInfo.InvariantCulture));
        MarkRecipeClean();
        ShowTotal(GetRecipeTotal());
        resultText.text = (previousIndex >= 0 ? "Recipe updated.\n" : "Recipe saved.\n") + resultText.text;
    }

    public void LoadRecipe()
    {
        // Preserve the existing button callback; it now opens the recipe browser.
        if (!HasConnectedFields() || !EnsureRecipeBrowser() || !EnsureRecipeCollectionLoaded())
            return;

        if (summaryScreen != null)
            summaryScreen.SetVisible(false);
        ingredientPanel.SetActive(false);
        editorScreen.SetActive(false);
        recipeBrowser.SetVisible(true);
        RefreshSavedRecipes();
    }

    private bool EnsureRecipeBrowser()
    {
        if (recipeBrowser != null)
            return true;

        if (!HasConnectedIngredientPanel())
            return false;
        RectTransform parent = editorScreen.transform.parent as RectTransform;
        if (parent == null)
        {
            resultText.text = "Setup needed: Editor Screen must be inside the Background UI object.";
            return false;
        }

        recipeBrowser = new KitchenCostRecipeBrowser(parent, ingredientListText,
            () => SetIngredientPanelVisible(false), NewRecipe);
        return true;
    }

    private void RefreshSavedRecipes()
    {
        recipeBrowser.BeginList(recipeCollection.recipes.Count);
        foreach (StoredRecipeData stored in recipeCollection.recipes)
        {
            string recipeId = stored.id;
            if (!TryDecodeSavedRecipe(JsonUtility.ToJson(stored.recipe),
                    out RecipeSaveData data, out List<IngredientEntry> unused, out decimal total))
            {
                recipeBrowser.SetMessage("A saved recipe could not be displayed. Its data has been kept.", true);
                continue;
            }
            string details = data.servings.ToString(CultureInfo.InvariantCulture) +
                (data.servings == 1 ? " serving" : " servings") + "  |  " +
                data.ingredients.Count.ToString(CultureInfo.InvariantCulture) +
                (data.ingredients.Count == 1 ? " ingredient" : " ingredients") +
                "  |  PKR " + FormatMoney(total / data.servings) + " / serving";
            recipeBrowser.AddRecipe(CleanName(data.recipeName), details, "PKR " + FormatMoney(total),
                () => ConfirmDiscardChanges(() => OpenSavedRecipe(recipeId)),
                () => ConfirmDeleteSavedRecipe(recipeId));
        }
        recipeBrowser.EndList();
    }

    private void OpenSavedRecipe(string recipeId)
    {
        int index = FindSavedRecipeIndex(recipeId);
        if (index < 0)
        {
            ShowRecipeError("This saved recipe is no longer available.");
            return;
        }

        if (!TryDecodeSavedRecipe(JsonUtility.ToJson(recipeCollection.recipes[index].recipe),
                out RecipeSaveData savedRecipe, out List<IngredientEntry> loadedIngredients,
                out decimal total))
        {
            ShowRecipeError("This recipe could not be read. Your current recipe has been kept.");
            return;
        }

        // Only replace the draft after every saved ingredient passes validation.
        ingredients.Clear();
        ingredients.AddRange(loadedIngredients);
        currentRecipeId = recipeId;
        recipeNameInput.SetTextWithoutNotify(CleanName(savedRecipe.recipeName));
        servingsInput.SetTextWithoutNotify(savedRecipe.servings.ToString(CultureInfo.InvariantCulture));
        ClearIngredientInputs();
        SetIngredientEditMode(-1);
        SetIngredientPanelVisible(false);
        RefreshIngredientList();
        ShowTotal(total);
        resultText.text = "Recipe loaded.\n" + resultText.text;
        MarkRecipeClean();
    }

    private void ConfirmDeleteSavedRecipe(string recipeId)
    {
        int index = FindSavedRecipeIndex(recipeId);
        if (index < 0)
            return;
        string name = CleanName(recipeCollection.recipes[index].recipe.recipeName);
        if (name.Length > 70)
            name = name.Substring(0, 67) + "...";
        recipeBrowser.ShowConfirmation("Delete recipe?",
            "Delete \"" + name + "\" from your saved recipes? This cannot be undone.",
            "Delete recipe", () => DeleteSavedRecipe(recipeId));
    }

    private void DeleteSavedRecipe(string recipeId)
    {
        int index = FindSavedRecipeIndex(recipeId);
        if (index < 0)
            return;
        var next = CopyRecipeCollection();
        next.recipes.RemoveAt(index);
        if (!TryWriteRecipeCollection(next, out string error))
        {
            ShowRecipeError(error);
            return;
        }

        recipeCollection = next;
        bool wasCurrentRecipe = currentRecipeId == recipeId;
        if (wasCurrentRecipe)
        {
            // Keep the user's current working copy, but it is no longer saved.
            currentRecipeId = null;
            cleanRecipeSignature = null;
        }
        RefreshSavedRecipes();
        recipeBrowser.SetMessage(wasCurrentRecipe
            ? "Saved copy deleted. Your current recipe is now an unsaved draft."
            : "Recipe deleted.", false);
    }

    private void ConfirmDiscardChanges(Action continueAction)
    {
        if (!HasUnsavedRecipeChanges())
        {
            continueAction();
            return;
        }
        if (!EnsureRecipeBrowser())
            return;
        recipeBrowser.ShowConfirmation("Unsaved changes",
            "Choose Cancel to keep your current recipe and save it first, or discard these changes to continue.",
            "Discard changes", () => continueAction());
    }

    private bool HasUnsavedRecipeChanges()
    {
        return editingIngredientIndex >= 0 || HasIngredientDraft() ||
            !string.Equals(cleanRecipeSignature, GetRecipeSignature(), StringComparison.Ordinal);
    }

    private string GetRecipeSignature()
    {
        return JsonUtility.ToJson(new RecipeDraftData
        {
            recipeName = recipeNameInput.text,
            servingsText = servingsInput.text,
            ingredients = CopyIngredientSaveData()
        });
    }

    private void MarkRecipeClean()
    {
        cleanRecipeSignature = GetRecipeSignature();
    }

    private List<IngredientSaveData> CopyIngredientSaveData()
    {
        var entries = new List<IngredientSaveData>();
        foreach (IngredientEntry ingredient in ingredients)
        {
            entries.Add(new IngredientSaveData
            {
                name = ingredient.Name,
                usedQuantity = ingredient.UsedQuantity.ToString(CultureInfo.InvariantCulture),
                packSize = ingredient.PackSize.ToString(CultureInfo.InvariantCulture),
                packPrice = ingredient.PackPrice.ToString(CultureInfo.InvariantCulture)
            });
        }
        return entries;
    }

    private int FindSavedRecipeIndex(string recipeId)
    {
        if (recipeCollection == null || string.IsNullOrEmpty(recipeId))
            return -1;
        return recipeCollection.recipes.FindIndex(recipe => recipe.id == recipeId);
    }

    private RecipeCollectionData CopyRecipeCollection()
    {
        // Saved records are treated as immutable; replace whole records on updates.
        return new RecipeCollectionData
        {
            version = 1,
            recipes = new List<StoredRecipeData>(recipeCollection.recipes)
        };
    }

    private bool EnsureRecipeCollectionLoaded()
    {
        if (recipeCollection != null)
            return true;

        if (PlayerPrefs.HasKey(RecipeCollectionKey))
        {
            try
            {
                var decoded = JsonUtility.FromJson<RecipeCollectionData>(PlayerPrefs.GetString(RecipeCollectionKey));
                if (!IsValidRecipeCollection(decoded))
                {
                    ShowRecipeError("Saved recipes could not be read. Their data has been kept; no recipes were overwritten.");
                    return false;
                }
                recipeCollection = decoded;
                return true;
            }
            catch (ArgumentException)
            {
                ShowRecipeError("Saved recipes could not be read. Their data has been kept; no recipes were overwritten.");
                return false;
            }
        }

        var collection = new RecipeCollectionData { version = 1, recipes = new List<StoredRecipeData>() };
        if (PlayerPrefs.HasKey(SavedRecipeKey))
        {
            // Import the old one-recipe format once. Keep its original key intact.
            if (!TryDecodeSavedRecipe(PlayerPrefs.GetString(SavedRecipeKey),
                    out RecipeSaveData legacy, out List<IngredientEntry> unused, out decimal unusedTotal))
            {
                ShowRecipeError("The previous saved recipe could not be imported. Its data has been kept.");
                return false;
            }
            collection.recipes.Add(new StoredRecipeData { id = Guid.NewGuid().ToString("N"), recipe = legacy });
            if (!TryWriteRecipeCollection(collection, out string error))
            {
                ShowRecipeError(error);
                return false;
            }
        }
        recipeCollection = collection;
        return true;
    }

    private static bool IsValidRecipeCollection(RecipeCollectionData collection)
    {
        if (collection == null || collection.version != 1 || collection.recipes == null)
            return false;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (StoredRecipeData stored in collection.recipes)
        {
            if (stored == null || !Guid.TryParse(stored.id, out Guid unusedId) ||
                !ids.Add(stored.id) || stored.recipe == null ||
                !TryDecodeSavedRecipe(JsonUtility.ToJson(stored.recipe), out RecipeSaveData unusedRecipe,
                    out List<IngredientEntry> unusedIngredients, out decimal unusedTotal))
                return false;
        }
        // An empty collection is valid and must never reimport a deleted old recipe.
        return true;
    }

    private static bool TryWriteRecipeCollection(RecipeCollectionData collection, out string error)
    {
        bool hadPrevious = PlayerPrefs.HasKey(RecipeCollectionKey);
        string previous = PlayerPrefs.GetString(RecipeCollectionKey, string.Empty);
        try
        {
            PlayerPrefs.SetString(RecipeCollectionKey, JsonUtility.ToJson(collection));
            PlayerPrefs.Save();
            error = null;
            return true;
        }
        catch (PlayerPrefsException)
        {
            // Undo the pending preference change if Unity reports a write failure.
            try
            {
                if (hadPrevious)
                    PlayerPrefs.SetString(RecipeCollectionKey, previous);
                else
                    PlayerPrefs.DeleteKey(RecipeCollectionKey);
                PlayerPrefs.Save();
            }
            catch (PlayerPrefsException)
            {
                Debug.LogWarning("KitchenCost: storage is unavailable; save rollback could not be confirmed.");
            }
            error = "Could not save recipes on this device. Please try again.";
            return false;
        }
    }

    private void ShowRecipeError(string message)
    {
        if (recipeBrowser != null && recipeBrowser.IsVisible)
            recipeBrowser.SetMessage(message, true);
        else
            ShowIngredientError(message);
    }

    private static bool TryDecodeSavedRecipe(string json, out RecipeSaveData savedRecipe,
        out List<IngredientEntry> loadedIngredients, out decimal total)
    {
        savedRecipe = null;
        loadedIngredients = new List<IngredientEntry>();
        total = 0m;

        try
        {
            savedRecipe = JsonUtility.FromJson<RecipeSaveData>(json);
            if (savedRecipe == null || savedRecipe.version != 1 ||
                string.IsNullOrWhiteSpace(CleanName(savedRecipe.recipeName)) ||
                savedRecipe.servings <= 0 || savedRecipe.ingredients == null ||
                savedRecipe.ingredients.Count == 0)
            {
                return false;
            }

            foreach (IngredientSaveData ingredient in savedRecipe.ingredients)
            {
                if (ingredient == null || string.IsNullOrWhiteSpace(CleanName(ingredient.name)) ||
                    !TryReadDecimal(ingredient.usedQuantity, out decimal usedQuantity) || usedQuantity <= 0m ||
                    !TryReadDecimal(ingredient.packSize, out decimal packSize) || packSize <= 0m ||
                    !TryReadDecimal(ingredient.packPrice, out decimal packPrice) || packPrice < 0m)
                {
                    return false;
                }

                // Recalculate costs from quantities and prices instead of trusting a saved total.
                decimal cost = (usedQuantity / packSize) * packPrice;
                total += cost;
                loadedIngredients.Add(new IngredientEntry
                {
                    Name = CleanName(ingredient.name),
                    UsedQuantity = usedQuantity,
                    PackSize = packSize,
                    PackPrice = packPrice,
                    Cost = cost
                });
            }

            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    private bool HasIngredientDraft()
    {
        return !string.IsNullOrWhiteSpace(ingredientNameInput.text) ||
            !string.IsNullOrWhiteSpace(usedQuantityInput.text) ||
            !string.IsNullOrWhiteSpace(packSizeInput.text) ||
            !string.IsNullOrWhiteSpace(packPriceInput.text);
    }

    private decimal GetRecipeTotal()
    {
        return GetRecipeTotalExcluding(-1);
    }

    private decimal GetRecipeTotalExcluding(int excludedIndex)
    {
        decimal total = 0m;
        for (int i = 0; i < ingredients.Count; i++)
        {
            if (i != excludedIndex)
                total += ingredients[i].Cost;
        }
        return total;
    }

    private void ShowTotal(decimal total)
    {
        string totalLine = "Recipe total: PKR " + FormatMoney(total);

        if (!int.TryParse(servingsInput.text, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out int servings) || servings <= 0)
        {
            // The ingredient was still added; only the per-serving value needs servings.
            resultText.text = totalLine + "\nEnter a whole number of servings greater than 0.";
            return;
        }

        // Preserve precision until display; never sum individually rounded row values.
        resultText.text = totalLine + "\nCost per serving: PKR " + FormatMoney(total / servings);
    }

    private void RefreshIngredientList()
    {
        ClearIngredientRows();
        // Ingredient names are plain text, even when they contain angle brackets.
        ingredientListText.richText = false;

        if (ingredients.Count == 0)
        {
            ingredientListText.gameObject.SetActive(true);
            ingredientListText.text = "No ingredients added yet.";
            return;
        }

        RectTransform content = ingredientListText.transform.parent as RectTransform;
        bool canCreateRows = content != null && content.GetComponent<VerticalLayoutGroup>() != null &&
            editorScreen != null && ingredientPanel != null && ingredientMessageText != null;

        if (canCreateRows)
        {
            ingredientListText.gameObject.SetActive(false);
            for (int i = 0; i < ingredients.Count; i++)
            {
                // Capture this index separately for every button. All callbacks are
                // rebuilt after a removal so shifted rows still target the right item.
                int rowIndex = i;
                IngredientEntry ingredient = ingredients[i];
                string details = ingredient.UsedQuantity.ToString(CultureInfo.InvariantCulture) +
                    " g used  |  Pack: " + ingredient.PackSize.ToString(CultureInfo.InvariantCulture) +
                    " g / PKR " + FormatMoney(ingredient.PackPrice);
                ingredientRows.Add(KitchenCostIngredientRow.Create(
                    content, ingredientListText, ingredient.Name, details,
                    "PKR " + FormatMoney(ingredient.Cost),
                    () => EditIngredient(rowIndex), () => RemoveIngredient(rowIndex)));
            }
            return;
        }

        // Preserve the plain list in the original single-screen scene.
        ingredientListText.gameObject.SetActive(true);
        var lines = new StringBuilder();
        for (int i = 0; i < ingredients.Count; i++)
        {
            if (i > 0)
                lines.Append('\n');

            IngredientEntry ingredient = ingredients[i];
            lines.Append(i + 1).Append(". ").Append(ingredient.Name)
                .Append(" - ").Append(ingredient.UsedQuantity.ToString(CultureInfo.InvariantCulture))
                .Append(" g - PKR ").Append(FormatMoney(ingredient.Cost));
        }

        ingredientListText.text = lines.ToString();
    }

    private void ClearIngredientRows()
    {
        foreach (GameObject row in ingredientRows)
        {
            if (row == null)
                continue;
            // Destroy runs at the end of the frame; deactivate immediately so the
            // layout and pointer events cannot use an obsolete row in the meantime.
            row.SetActive(false);
            Destroy(row);
        }
        ingredientRows.Clear();
    }

    private void ClearIngredientInputs()
    {
        // Clearing prevents an accidental second click from adding the same item twice.
        ingredientNameInput.SetTextWithoutNotify(string.Empty);
        usedQuantityInput.SetTextWithoutNotify(string.Empty);
        packSizeInput.SetTextWithoutNotify(string.Empty);
        packPriceInput.SetTextWithoutNotify(string.Empty);
        if (ingredientMessageText != null)
            ingredientMessageText.text = string.Empty;
    }

    private bool HasConnectedFields()
    {
        if (resultText == null)
        {
            Debug.LogError("KitchenCost: assign Result Text in the Inspector.", this);
            return false;
        }

        if (recipeNameInput == null || ingredientNameInput == null || usedQuantityInput == null ||
            packSizeInput == null || packPriceInput == null || servingsInput == null ||
            ingredientListText == null)
        {
            ShowIngredientError("Setup needed: connect all eight calculator fields on AppCanvas.");
            return false;
        }

        return true;
    }

    private static string CleanName(string text)
    {
        return (text ?? string.Empty).Trim()
            .Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
    }

    private static bool TryReadDecimal(string text, out decimal value)
    {
        // TMP's Decimal Number field uses a dot. Reject ambiguous thousands separators.
        return decimal.TryParse(text == null ? null : text.Trim(),
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out value);
    }

    private static string FormatMoney(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero)
            .ToString("F2", CultureInfo.InvariantCulture);
    }
}
