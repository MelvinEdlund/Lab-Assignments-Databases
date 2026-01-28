using System.Collections.ObjectModel;
using Labb3_Quiz_MongoDB.Command;
using Labb3_Quiz_MongoDB.Models;
using Labb3_Quiz_MongoDB.Services;

namespace Labb3_Quiz_MongoDB.ViewModels;

// ViewModel för dialogen där användaren kan skapa/ta bort kategorier.
// All logik för knapparna "Add" och "Delete" finns här.
public class ManageCategoriesViewModel : ViewModelBase
{
    // Lagringslager (MongoDB) för att utföra riktiga CRUD-operationer.
    private readonly IStorageService _storage;
    // Dialogservice för att visa felmeddelanden och confirm-rutor.
    private readonly IDialogService _dialogs;

    // Samlingen med kategorier som delas med huvudfönstret.
    // Vi jobbar direkt mot samma ObservableCollection så UI uppdateras live.
    private readonly ObservableCollection<Category> _categories;

    // Vilken kategori som är vald i listan just nu.
    private Category? _selectedCategory;

    // Texten som skrivs i "ny kategori"-textboxen.
    private string _newCategoryName = "";

    public ManageCategoriesViewModel(
        IStorageService storage,
        IDialogService dialogs,
        ObservableCollection<Category> categories)
    {
        _storage = storage;
        _dialogs = dialogs;
        _categories = categories;

        // Commands kopplas till knappar i XAML.
        AddCategoryCommand = new DelegateCommand(async _ => await AddCategoryAsync(), _ => CanAdd());
        DeleteCategoryCommand = new DelegateCommand(async _ => await DeleteCategoryAsync(), _ => SelectedCategory != null);
    }

    // ListBox i dialogen binder till denna property.
    public ObservableCollection<Category> Categories => _categories;

    // ListBox.SelectedItem binder hit.
    public Category? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            _selectedCategory = value;
            RaisePropertyChanged();
            // Delete-knappen ska bara vara aktiv när något är valt.
            DeleteCategoryCommand.RaiseCanExecuteChanged();
        }
    }

    // TextBox för nya kategorier binder hit.
    public string NewCategoryName
    {
        get => _newCategoryName;
        set
        {
            _newCategoryName = value;
            RaisePropertyChanged();
            // Add-knappen aktiveras/inaktiveras beroende på om textboxen är tom.
            AddCategoryCommand.RaiseCanExecuteChanged();
        }
    }

    public DelegateCommand AddCategoryCommand { get; }
    public DelegateCommand DeleteCategoryCommand { get; }

    // Enkel validering: får inte vara tom eller bara whitespace.
    private bool CanAdd() => !string.IsNullOrWhiteSpace(NewCategoryName);

    // Skapar en ny kategori i databasen och lägger in den sorterad i listan.
    private async Task AddCategoryAsync()
    {
        var name = NewCategoryName.Trim();
        if (string.IsNullOrWhiteSpace(name)) return;

        try
        {
            var cat = new Category { Name = name };
            await _storage.CreateCategoryAsync(cat);

            // Lägg in den nya kategorin på rätt plats alfabetiskt.
            var insertAt = 0;
            while (insertAt < _categories.Count &&
                   string.Compare(_categories[insertAt].Name, cat.Name, StringComparison.OrdinalIgnoreCase) < 0)
                insertAt++;

            _categories.Insert(insertAt, cat);

            // Rensa textboxen efter lyckad insert.
            NewCategoryName = "";
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"Could not add category: {ex.Message}");
        }
    }

    // Tar bort vald kategori från databasen och från ObservableCollection.
    private async Task DeleteCategoryAsync()
    {
        if (SelectedCategory == null) return;
        var toDelete = SelectedCategory;

        // Bekräfta med användaren, och informera att frågepaket tappar kopplingen.
        if (!_dialogs.ShowConfirm($"Delete category '{toDelete.Name}'?\nPacks using it will be cleared.", "Delete Category"))
            return;

        try
        {
            await _storage.DeleteCategoryAsync(toDelete.Id);
            _categories.Remove(toDelete);
            SelectedCategory = null;
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"Could not delete category: {ex.Message}");
        }
    }
}

