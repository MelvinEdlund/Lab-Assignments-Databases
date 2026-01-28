using Labb3_Quiz_MongoDB.Command;
using Labb3_Quiz_MongoDB.Models;
using Labb3_Quiz_MongoDB.Services;
using Labb3_Quiz_MongoDB.Views.Dialogs;
using System.Windows;

namespace Labb3_Quiz_MongoDB.ViewModels;

// ViewModel för redigeringsvyn (lägg till/ta bort frågor, öppna pack-inställningar)
public class ConfigurationViewModel : ViewModelBase
{
    private readonly MainWindowViewModel? _mainWindowViewModel;
    private readonly IDialogService _dialogService;
    private Question? _selectedQuestion;

    public ConfigurationViewModel(MainWindowViewModel? mainWindowViewModel, IDialogService dialogService)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _dialogService = dialogService;
        AddQuestionCommand = new DelegateCommand(_ => AddQuestion());
        RemoveQuestionCommand = new DelegateCommand(_ => RemoveQuestion(), _ => SelectedQuestion != null);
        OpenPackOptionsCommand = new DelegateCommand(_ => OpenPackOptions());
    }

    public QuestionPackViewModel? ActivePack => _mainWindowViewModel?.ActivePack;

    public Question? SelectedQuestion
    {
        get => _selectedQuestion;
        set
        {
            _selectedQuestion = value;
            if (_selectedQuestion != null && _selectedQuestion.Options.Count >= 4)
            {
                _selectedQuestion.Options[0].IsCorrect = true;
                _selectedQuestion.Options[1].IsCorrect = false;
                _selectedQuestion.Options[2].IsCorrect = false;
                _selectedQuestion.Options[3].IsCorrect = false;
            }
            RaisePropertyChanged();
            RemoveQuestionCommand.RaiseCanExecuteChanged();
        }
    }

    public DelegateCommand AddQuestionCommand { get; }
    public DelegateCommand RemoveQuestionCommand { get; }
    public DelegateCommand OpenPackOptionsCommand { get; }

    private void AddQuestion()
    {
        if (ActivePack == null) return;
        var question = new Question
        {
            Text = "New Question",
            Options = new List<AnswerOption>
            {
                new AnswerOption { Text = "", IsCorrect = true },
                new AnswerOption { Text = "", IsCorrect = false },
                new AnswerOption { Text = "", IsCorrect = false },
                new AnswerOption { Text = "", IsCorrect = false }
            }
        };
        ActivePack.Questions.Add(question);
        SelectedQuestion = question;
    }

    private void RemoveQuestion()
    {
        if (ActivePack == null || SelectedQuestion == null) return;
        ActivePack.Questions.Remove(SelectedQuestion);
        SelectedQuestion = null;
    }

    private void OpenPackOptions()
    {
        if (ActivePack == null) return;
        var dialog = new PackOptionsDialog(ActivePack)
        {
            Owner = Application.Current.MainWindow
        };
        dialog.ShowDialog();
    }
}
