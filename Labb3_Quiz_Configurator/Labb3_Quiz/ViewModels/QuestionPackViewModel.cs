using Labb3_Quiz_MongoDB.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Labb3_Quiz_MongoDB.ViewModels;

// ViewModel-wrapper för QuestionPack (synkroniserar ObservableCollection med Model)
// Det är denna klass som UI:t binder till när du redigerar ett frågepack.
public class QuestionPackViewModel : ViewModelBase
{
    // Själva data-modellen som kommer från/skrivs till MongoDB.
    private readonly QuestionPack _model;

    // Lista med alla tillgängliga kategorier (kommer från MainWindowViewModel).
    private ObservableCollection<Category> _availableCategories = new();

    public QuestionPackViewModel(QuestionPack model)
    {
        _model = model;

        // Gör om listan av frågor till en ObservableCollection så att UI:t
        // kan reagera dynamiskt på förändringar (lägg till / ta bort frågor).
        Questions = new ObservableCollection<Question>(_model.Questions);
        Questions.CollectionChanged += Questions_CollectionChanged;

        // Underlag till Difficulty-combobox.
        Difficulties = new List<PackDifficulty> { PackDifficulty.Easy, PackDifficulty.Medium, PackDifficulty.Hard };
    }

    // Synkroniserar ObservableCollection med Model.Questions
    private void Questions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // När en fråga läggs till i ObservableCollection lägger vi till den i Model.Questions.
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            foreach (Question q in e.NewItems) _model.Questions.Add(q);

        // När en fråga tas bort i ObservableCollection tar vi bort den i Model.Questions.
        if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
            foreach (Question q in e.OldItems) _model.Questions.Remove(q);

        // Vid ersättning (t.ex. om en fråga byts ut) synkar vi motsvarande index i Model.Questions.
        if (e.Action == NotifyCollectionChangedAction.Replace && e.OldItems != null && e.NewItems != null)
            _model.Questions[e.OldStartingIndex] = (Question)e.NewItems[0]!;

        // Reset = rensa alla frågor.
        if (e.Action == NotifyCollectionChangedAction.Reset)
            _model.Questions.Clear();
    }

    public string Name
    {
        get => _model.Name;
        set
        {
            _model.Name = value;
            RaisePropertyChanged();
            // DisplayName bygger på Name, så vi raisar även den.
            RaisePropertyChanged(nameof(DisplayName));
        }
    }

    public string DisplayName => $"{Name} ({Difficulty})";

    public PackDifficulty Difficulty
    {
        get => _model.Difficulty;
        set
        {
            _model.Difficulty = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(DisplayName));
        }
    }

    public int TimePerQuestionSeconds
    {
        get => _model.TimePerQuestionSeconds;
        set
        {
            _model.TimePerQuestionSeconds = value;
            RaisePropertyChanged();
        }
    }

    public ObservableCollection<Question> Questions { get; set; }
    public List<PackDifficulty> Difficulties { get; }

    public ObservableCollection<Category> AvailableCategories
    {
        get => _availableCategories;
        set
        {
            _availableCategories = value ?? new ObservableCollection<Category>();
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(SelectedCategory));
        }
    }

    public Category? SelectedCategory
    {
        get
        {
            // Hitta den Category i listan som matchar modellens CategoryId.
            if (_model.CategoryId == null) return null;
            return AvailableCategories.FirstOrDefault(c => c.Id == _model.CategoryId);
        }
        set
        {
            if (value == null)
            {
                // Ingen kategori vald – rensa både Id och namn i modellen.
                _model.CategoryId = null;
                _model.CategoryName = null;
            }
            else
            {
                // Spara både Id och namn så att vi lätt kan visa text utan extra databas-lookups.
                _model.CategoryId = value.Id;
                _model.CategoryName = value.Name;
            }
            RaisePropertyChanged();
        }
    }

    // En enkel "read-only"-property om du vill visa kategorinamnet i UI:t.
    public string CategoryLabel => _model.CategoryName ?? "—";

    // Exponerar underliggande modell (används när vi sparar till databasen).
    public QuestionPack Model => _model;
}