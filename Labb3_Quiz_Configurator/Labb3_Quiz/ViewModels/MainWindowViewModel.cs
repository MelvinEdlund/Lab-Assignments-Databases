using Labb3_Quiz_MongoDB.Command;
using Labb3_Quiz_MongoDB.Models;
using Labb3_Quiz_MongoDB.Services;
using Labb3_Quiz_MongoDB.Services.MongoDb;
using System.Collections.ObjectModel;
using System.Windows;

namespace Labb3_Quiz_MongoDB.ViewModels;

// Huvud-ViewModel (koordinerar packs, laddning/sparande, edit/play mode)
// Här ser du hela flödet: från att data laddas från MongoDB tills den
// binds till olika views (Menu, Configuration, Player).
public class MainWindowViewModel : ViewModelBase
{
    // Lagrings-abstraktionen (just nu MongoDB).
    private readonly IStorageService _storageService;
    // Service för MessageBox/dialoger.
    private readonly IDialogService _dialogService;

    // Vilket frågepack som är valt just nu (null om inga finns).
    private QuestionPackViewModel? _activePack;

    // Styr om vi är i "spelläget" (PlayerView) eller "konfigläget" (ConfigurationView).
    private bool _isPlayMode;

    // Styr helskärmsläge i huvudfönstret.
    private bool _isFullScreen;

    public MainWindowViewModel()
    {
        // Koppla upp mot MongoDB (via settings + MongoStorageService).
        _storageService = new MongoStorageService();

        // DialogService wraps alla MessageBox-anrop + ImportQuestionsDialog.
        _dialogService = new DialogService();

        // Samling av alla frågepaket som laddas från databasen.
        Packs = new ObservableCollection<QuestionPackViewModel>();
        // Samling av alla kategorier (delas mellan olika dialoger).
        Categories = new ObservableCollection<Category>();

        // Skapa underliggande ViewModels som används i UI:t.
        PlayerViewModel = new PlayerViewModel(this, _dialogService);
        ConfigurationViewModel = new ConfigurationViewModel(this, _dialogService);

        ShowConfigCommand = new DelegateCommand(_ => IsPlayMode = false);
        ShowPlayerCommand = new DelegateCommand(_ => IsPlayMode = true, _ => ActivePack != null && ActivePack.Questions.Count > 0);
        ToggleFullScreenCommand = new DelegateCommand(_ => IsFullScreen = !IsFullScreen);

        // CRUD-kommandon för packs.
        CreateNewPackCommand = new DelegateCommand(async _ => await CreateNewPackAsync());
        RemoveQuestionPackCommand = new DelegateCommand(async _ => await RemoveQuestionPackAsync(), _ => ActivePack != null);
        SaveAllCommand = new DelegateCommand(async _ => await SaveAllAsync());

        // Övriga huvudkommandon.
        SetActivePackCommand = new DelegateCommand(pack => ActivePack = pack as QuestionPackViewModel, _ => !_isPlayMode);
        ExitProgramCommand = new DelegateCommand(_ => Application.Current.Shutdown());
        ImportQuestionsCommand = new DelegateCommand(_ => ImportQuestions());
        ManageCategoriesCommand = new DelegateCommand(_ => ManageCategories());

        // Starta asynkron laddning av kategorier + packs från MongoDB.
        _ = LoadAllAsync();
    }

    public ObservableCollection<QuestionPackViewModel> Packs { get; }
    public ObservableCollection<Category> Categories { get; }

    public QuestionPackViewModel? ActivePack
    {
        get => _activePack;
        set
        {
            // Om vi byter paket medan vi är i PlayMode växlar vi automatiskt tillbaka
            // till Edit-läget så användaren ser vad hen spelar på.
            if (_isPlayMode && value != _activePack)
                IsPlayMode = false;
            _activePack = value;
            RaisePropertyChanged();

            // Olika kommandon får ny CanExecute-status när aktivt pack ändras.
            ShowPlayerCommand.RaiseCanExecuteChanged();
            RemoveQuestionPackCommand.RaiseCanExecuteChanged();
            ConfigurationViewModel?.RaisePropertyChanged(nameof(ConfigurationViewModel.ActivePack));
            PlayerViewModel?.RaisePropertyChanged(nameof(PlayerViewModel.ActivePack));
        }
    }

    public bool IsPlayMode
    {
        get => _isPlayMode;
        set
        {
            _isPlayMode = value;
            RaisePropertyChanged();
            // När IsPlayMode ändras behöver även CurrentView uppdateras.
            RaisePropertyChanged(nameof(CurrentView));
            SetActivePackCommand.RaiseCanExecuteChanged();
        }
    }

    // Binder till ContentControl i MainWindow.xaml – växlar mellan Editor/Player.
    public object? CurrentView => IsPlayMode ? PlayerViewModel : ConfigurationViewModel;

    public bool IsFullScreen
    {
        get => _isFullScreen;
        set
        {
            _isFullScreen = value;
            RaisePropertyChanged();
        }
    }

    public PlayerViewModel PlayerViewModel { get; }
    public ConfigurationViewModel ConfigurationViewModel { get; }

    public DelegateCommand ShowConfigCommand { get; }
    public DelegateCommand ShowPlayerCommand { get; }
    public DelegateCommand ToggleFullScreenCommand { get; }
    public DelegateCommand CreateNewPackCommand { get; }
    public DelegateCommand RemoveQuestionPackCommand { get; }
    public DelegateCommand SaveAllCommand { get; }
    public DelegateCommand SetActivePackCommand { get; }
    public DelegateCommand ExitProgramCommand { get; }
    public DelegateCommand ImportQuestionsCommand { get; }
    public DelegateCommand ManageCategoriesCommand { get; }

    private async Task LoadAllAsync()
    {
        try
        {
            // Steg 1: Läs alla kategorier och packs från lagringslagret (MongoDB).
            var categories = await _storageService.GetAllCategoriesAsync();
            var packs = await _storageService.GetAllPacksAsync();

            // Steg 2: Uppdatera ObservableCollections på UI-tråden.
            Application.Current.Dispatcher.Invoke(() =>
            {
                Categories.Clear();
                foreach (var c in categories) Categories.Add(c);

                Packs.Clear();
                foreach (var pack in packs)
                {
                    // Wrap:a modellen i en QuestionPackViewModel så den kan bindas i XAML.
                    var vm = new QuestionPackViewModel(pack) { AvailableCategories = Categories };
                    Packs.Add(vm);
                }

                // Försök välja ett default-pack, annars bara första.
                ActivePack = Packs.FirstOrDefault(p => p.Name == "C#frågor") ?? Packs.FirstOrDefault();
            });
        }
        catch (Exception ex)
        {
            // Vid fel – visa meddelande och låt appen starta ändå.
            _dialogService.ShowError($"Failed to load from MongoDB: {ex.Message}");
            Application.Current.Dispatcher.Invoke(() => ActivePack = Packs.FirstOrDefault());
        }
    }

    private async Task CreateNewPackAsync()
    {
        try
        {
            var pack = new QuestionPack { Name = "New Pack" };
            // Skapa dokumentet i databasen.
            await _storageService.CreatePackAsync(pack);

            var packViewModel = new QuestionPackViewModel(pack) { AvailableCategories = Categories };
            Packs.Add(packViewModel);
            ActivePack = packViewModel;
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Failed to create pack: {ex.Message}");
        }
    }

    private async Task RemoveQuestionPackAsync()
    {
        if (ActivePack == null) return;

        // Bekräfta med användaren innan vi raderar.
        if (!_dialogService.ShowConfirm($"Delete pack '{ActivePack.Name}'?", "Delete Pack"))
            return;

        try
        {
            await _storageService.DeletePackAsync(ActivePack.Model.Id);
            Packs.Remove(ActivePack);
            ActivePack = Packs.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Failed to delete pack: {ex.Message}");
        }
    }

    private async Task SaveAllAsync(bool showMessage = true)
    {
        try
        {
            // Gå igenom alla QuestionPackViewModels och skicka ned modellen till lagringslagret.
            foreach (var pack in Packs.Select(p => p.Model))
                await _storageService.UpdatePackAsync(pack);

            if (showMessage)
                _dialogService.ShowInfo("Packs saved successfully.");
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Failed to save packs: {ex.Message}");
        }
    }

    private void ImportQuestions()
    {
        if (ActivePack == null)
        {
            _dialogService.ShowError("Vänligen välj ett frågepack först innan du importerar frågor.");
            return;
        }
        _dialogService.ShowImportQuestionsDialog(
            getSelected: () => ActivePack,
            saveAll: async () => await SaveAllAsync(showMessage: false)
        );
    }

    private void ManageCategories()
    {
        try
        {
            // Skapa ViewModel för kategorihantering och skicka in samma ObservableCollection
            // som huvudfönstret använder, så att allt hålls i synk.
            var vm = new ManageCategoriesViewModel(_storageService, _dialogService, Categories);
            var dlg = new Labb3_Quiz_MongoDB.Views.Dialogs.ManageCategoriesDialog
            {
                DataContext = vm,
                Owner = Application.Current.MainWindow
            };
            dlg.ShowDialog();

            // Efter dialogen stängts: se till att alla QuestionPackViewModels pekar på
            // samma Categories-lista och uppdatera SelectedCategory-bindingarna.
            foreach (var p in Packs)
            {
                if (!ReferenceEquals(p.AvailableCategories, Categories))
                    p.AvailableCategories = Categories;
                p.RaisePropertyChanged(nameof(QuestionPackViewModel.SelectedCategory));
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowError($"Could not open category manager: {ex.Message}");
        }
    }
}
