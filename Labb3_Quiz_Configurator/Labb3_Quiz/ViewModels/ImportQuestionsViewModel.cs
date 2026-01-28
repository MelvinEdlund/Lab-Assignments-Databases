using Labb3_Quiz_MongoDB.Command;
using Labb3_Quiz_MongoDB.Models;
using Labb3_Quiz_MongoDB.Services;
using Labb3_Quiz_MongoDB.Services.Trivia;
using System.Collections.ObjectModel;
using System.Net.Http;

namespace Labb3_Quiz_MongoDB.ViewModels;

// ViewModel för import-dialog (hämtar frågor från API → mappar till Question → sparar till pack)
public class ImportQuestionsViewModel : ViewModelBase
{
    private readonly ITriviaService _trivia;
    private readonly IDialogService _dialogs;
    private OpenTdbCategory? _selectedCategory;
    private string? _selectedDifficulty;
    private int _amount = 10;
    private bool _isBusy;
    private string _status = "";
    private CancellationTokenSource? _cts;

    public ObservableCollection<OpenTdbCategory> Categories { get; } = new();

    public OpenTdbCategory? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            _selectedCategory = value;
            RaisePropertyChanged();
        }
    }

    public string? SelectedDifficulty
    {
        get => _selectedDifficulty;
        set
        {
            _selectedDifficulty = value;
            RaisePropertyChanged();
        }
    }

    public int Amount
    {
        get => _amount;
        set
        {
            _amount = value;
            RaisePropertyChanged();
            ImportCommand.RaiseCanExecuteChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            RaisePropertyChanged();
            LoadCategoriesCommand.RaiseCanExecuteChanged();
            ImportCommand.RaiseCanExecuteChanged();
            CancelCommand.RaiseCanExecuteChanged();
        }
    }


    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            RaisePropertyChanged();
        }
    }

    public DelegateCommand LoadCategoriesCommand { get; }
    public DelegateCommand ImportCommand { get; }
    public DelegateCommand CancelCommand { get; }

    // Sätts av DialogService när dialogen öppnas
    public Func<QuestionPackViewModel?> GetSelectedPack { get; set; } = () => null;
    public Func<Task> SaveAllAsync { get; set; } = () => Task.CompletedTask;

    public ImportQuestionsViewModel(ITriviaService trivia, IDialogService dialogs)
    {
        _trivia = trivia;
        _dialogs = dialogs;
        LoadCategoriesCommand = new DelegateCommand(async _ => await LoadCategoriesAsync(), _ => !IsBusy);
        ImportCommand = new DelegateCommand(async _ => await ImportAsync(), _ => !IsBusy && Amount >= 1 && Amount <= 50);
        CancelCommand = new DelegateCommand(_ => _cts?.Cancel(), _ => IsBusy);
    }

    // Hämtar kategorier från API (HTTP GET → JSON → deserialisering)
    private async Task LoadCategoriesAsync()
    {
        try
        {
            IsBusy = true;
            Status = "Hämtar kategorier...";
            Categories.Clear();
            var list = await _trivia.GetCategoriesAsync();
            foreach (var c in list) Categories.Add(c);
            if (SelectedCategory == null && Categories.Count > 0) SelectedCategory = Categories[0];
            Status = $"Klar: {Categories.Count} kategorier";
        }
        catch (OperationCanceledException)
        {
            Status = "Hämtning avbruten.";
        }
        catch (HttpRequestException ex)
        {
            _dialogs.ShowError($"Kunde inte hämta kategorier.\nNätverksfel: {ex.Message}");
            Status = "Fel vid hämtning";
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"Kunde inte hämta kategorier.\n{ex.Message}");
            Status = "Fel vid hämtning";
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Importerar frågor från API (HTTP GET → JSON → mappar till Question → lägger till i pack → sparar till JSON)
    private async Task ImportAsync()
    {
        _cts = new CancellationTokenSource();
        try
        {
            IsBusy = true;
            Status = "Importerar frågor...";
            int? cat = SelectedCategory?.Id;
            string? diff = string.IsNullOrWhiteSpace(SelectedDifficulty) ? null : SelectedDifficulty;

            var data = await _trivia.GetQuestionsAsync(Amount, cat, diff, _cts.Token);

            switch (data.Response_Code)
            {
                case 0: break;
                case 1:
                    _dialogs.ShowInfo("Inga frågor matchade ditt val. Prova annan kombination.");
                    Status = "Inga frågor hittades";
                    return;
                case 2:
                    _dialogs.ShowError("Ogiltiga parametrar till API:et.");
                    Status = "Ogiltiga parametrar";
                    return;
                case 3:
                    _dialogs.ShowError("APItoken saknas/ogiltig (ej kritiskt i vår användning).");
                    Status = "API-token-fel";
                    return;
                case 4:
                    _dialogs.ShowError("API:et returnerade 'Token empty'. Försök igen.");
                    Status = "API-token-fel";
                    return;
                default:
                    _dialogs.ShowError("Okänt svar från API:et.");
                    Status = "Okänt API-svar";
                    return;
            }

            if (data.Results.Count == 0)
            {
                _dialogs.ShowInfo("API:et returnerade 0 frågor.");
                Status = "Inga frågor returnerade";
                return;
            }

            // Mappar OpenTdbQuestion (API) → Question (vår modell) och blandar alternativ
            var mappedQuestions = data.Results
                .Where(r => r.Type == "multiple" && r.Incorrect_Answers.Count >= 3)
                .Select(r =>
                {
                    var options = new List<AnswerOption>
                    {
                        new() { Text = r.Correct_Answer, IsCorrect = true }
                    };
                    options.AddRange(r.Incorrect_Answers.Take(3).Select(x => new AnswerOption { Text = x, IsCorrect = false }));

                    if (options.Count != 4) return null;

                    // Blanda alternativ slumpmässigt (Fisher-Yates)
                    var rng = new Random();
                    for (int i = options.Count - 1; i > 0; i--)
                    {
                        int j = rng.Next(i + 1);
                        (options[i], options[j]) = (options[j], options[i]);
                    }

                    return new Question
                    {
                        Text = r.Question,
                        Options = options
                    };
                })
                .Where(q => q != null)
                .ToList();

            if (mappedQuestions.Count == 0)
            {
                _dialogs.ShowInfo("Inga användbara frågor kunde mappas.");
                Status = "Inga användbara frågor";
                return;
            }

            var targetPack = GetSelectedPack();
            if (targetPack == null)
            {
                _dialogs.ShowError("Inget frågepack är valt. Vänligen välj ett pack först.");
                Status = "Inget pack valt";
                return;
            }

            foreach (var q in mappedQuestions)
                targetPack.Questions.Add(q!);

            await SaveAllAsync();
            Status = $"Importerade {mappedQuestions.Count} frågor till \"{targetPack.Name}\".";
            _dialogs.ShowInfo(Status);
        }
        catch (OperationCanceledException)
        {
            _dialogs.ShowInfo("Import avbröts.");
            Status = "Import avbruten";
        }
        catch (HttpRequestException ex)
        {
            _dialogs.ShowError($"Nätverksfel vid hämtning från OpenTDB.\n{ex.Message}");
            Status = "Nätverksfel";
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"Ett fel uppstod vid import.\n{ex.Message}");
            Status = "Fel vid import";
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
        }
    }
}

