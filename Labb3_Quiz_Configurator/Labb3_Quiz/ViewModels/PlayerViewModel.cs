using Labb3_Quiz_MongoDB.Command;
using Labb3_Quiz_MongoDB.Models;
using Labb3_Quiz_MongoDB.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace Labb3_Quiz_MongoDB.ViewModels;

// ViewModel för quiz-spel (hanterar timer, poäng, blandning, feedback)
public class PlayerViewModel : ViewModelBase
{
    private readonly MainWindowViewModel? _mainWindowViewModel;
    private readonly IDialogService _dialogService;
    private readonly DispatcherTimer _timer; 
    private readonly DispatcherTimer _feedbackTimer;
    private List<Question> _shuffledQuestions = new();
    private List<AnswerOption> _shuffledOptions = new();
    private int _currentQuestionIndex = -1;
    private int _remainingTime;
    private int _score;
    private bool _hasAnswered;
    private string _correctAnswerText = "";
    private string _userAnswerText = "";
    private bool _isRoundActive;
    private bool _showResults;
    private int _feedbackDelaySeconds = 0;

    public PlayerViewModel(MainWindowViewModel? mainWindowViewModel, IDialogService dialogService)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _dialogService = dialogService;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += Timer_Tick;
        _feedbackTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _feedbackTimer.Tick += FeedbackTimer_Tick;
        ShuffledOptions = new ObservableCollection<AnswerOption>();

        StartRoundCommand = new DelegateCommand(_ => StartRound(), _ => ActivePack != null && ActivePack.Questions.Count > 0);
        SubmitAnswerCommand = new DelegateCommand(SubmitAnswer, _ => _isRoundActive && !_hasAnswered && _currentQuestionIndex >= 0);
        NextQuestionCommand = new DelegateCommand(_ => NextQuestion(), _ => _hasAnswered || _remainingTime <= 0);

        if (_mainWindowViewModel != null)
        {
            _mainWindowViewModel.PropertyChanged += MainViewModel_PropertyChanged;
            OnActivePackChanged();
        }
    }

    // Reagerar på ändringar i huvud-ViewModel (ActivePack, IsPlayMode)
    private void MainViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.ActivePack))
        {
            ResetQuizState();
            OnActivePackChanged();
        }
        else if (e.PropertyName == nameof(MainWindowViewModel.IsPlayMode))
        {
            if (!_mainWindowViewModel?.IsPlayMode ?? false)
                ResetQuizState();
            else if (ActivePack != null && ActivePack.Questions.Count > 0 && !_isRoundActive)
                StartRound();
        }
    }

    public QuestionPackViewModel? ActivePack => _mainWindowViewModel?.ActivePack;

    private void OnActivePackChanged()
    {
        RaisePropertyChanged(nameof(ActivePack));
        RaisePropertyChanged(nameof(PackQuestionCount));
        StartRoundCommand.RaiseCanExecuteChanged();
    }

    public ObservableCollection<AnswerOption> ShuffledOptions { get; }

    public int CurrentQuestionIndex
    {
        get => _currentQuestionIndex;
        set
        {
            _currentQuestionIndex = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(CurrentQuestionNumber));
            RaisePropertyChanged(nameof(TotalQuestions));
            RaisePropertyChanged(nameof(CurrentQuestion));
        }
    }

    public int CurrentQuestionNumber => _currentQuestionIndex >= 0 ? _currentQuestionIndex + 1 : 0;
    public int TotalQuestions => _shuffledQuestions.Count;
    public int PackQuestionCount => ActivePack?.Questions.Count ?? 0;
    public Question? CurrentQuestion => _currentQuestionIndex >= 0 && _currentQuestionIndex < _shuffledQuestions.Count 
        ? _shuffledQuestions[_currentQuestionIndex] 
        : null;

    public int RemainingTime
    {
        get => _remainingTime;
        set
        {
            _remainingTime = value;
            RaisePropertyChanged();
        }
    }

    public int Score
    {
        get => _score;
        set
        {
            _score = value;
            RaisePropertyChanged();
        }
    }

    public bool HasAnswered
    {
        get => _hasAnswered;
        set
        {
            _hasAnswered = value;
            RaisePropertyChanged();
            SubmitAnswerCommand.RaiseCanExecuteChanged();
            NextQuestionCommand.RaiseCanExecuteChanged();
        }
    }

    public string CorrectAnswerText
    {
        get => _correctAnswerText;
        set
        {
            _correctAnswerText = value;
            RaisePropertyChanged();
        }
    }

    public string UserAnswerText
    {
        get => _userAnswerText;
        set
        {
            _userAnswerText = value;
            RaisePropertyChanged();
        }
    }

    public bool ShowResults
    {
        get => _showResults;
        set
        {
            _showResults = value;
            RaisePropertyChanged();
        }
    }

    public bool IsRoundActive
    {
        get => _isRoundActive;
        set
        {
            _isRoundActive = value;
                RaisePropertyChanged();
        }
    }

    public DelegateCommand StartRoundCommand { get; }
    public DelegateCommand SubmitAnswerCommand { get; }
    public DelegateCommand NextQuestionCommand { get; }

    private void StartRound()
    {
        if (ActivePack == null || ActivePack.Questions.Count == 0) return;
        _shuffledQuestions = ActivePack.Questions.Select(q => new Question
        {
            Text = q.Text,
            Options = q.Options.Select(o => new AnswerOption { Text = o.Text, IsCorrect = o.IsCorrect }).ToList()
        }).ToList();
        RandomListAlgoritm(_shuffledQuestions);
        Score = 0;
        CurrentQuestionIndex = 0;
        IsRoundActive = true;
        ShowResults = false;
        StartRoundCommand.RaiseCanExecuteChanged();
        LoadQuestion();
    }

    private void LoadQuestion()
    {
        _feedbackTimer.Stop();
        _feedbackDelaySeconds = 0;
        if (_currentQuestionIndex < 0 || _currentQuestionIndex >= _shuffledQuestions.Count)
        {
            EndRound();
            return;
        }
        var question = _shuffledQuestions[_currentQuestionIndex];
        _shuffledOptions = question.Options.ToList();
        RandomListAlgoritm(_shuffledOptions);
        ShuffledOptions.Clear();
        foreach (var option in _shuffledOptions)
            ShuffledOptions.Add(option);
        HasAnswered = false;
        CorrectAnswerText = "";
        UserAnswerText = "";
        RemainingTime = ActivePack?.TimePerQuestionSeconds ?? 20;
        _timer.Start();
        RaisePropertyChanged(nameof(CurrentQuestion));
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (_remainingTime > 0)
            RemainingTime--;
        else
        {
            _timer.Stop();
            if (!_hasAnswered)
            {
                var correctOption = _shuffledOptions.FirstOrDefault(o => o.IsCorrect);
                if (correctOption != null)
                {
                    CorrectAnswerText = $"Time's up! Correct answer: {correctOption.Text}";
                    UserAnswerText = "Time's up!";
                }
                HasAnswered = true;
                StartFeedbackTimer();
            }
        }
    }

    // Feedback timer: väntar 2 sek innan nästa fråga
    private void FeedbackTimer_Tick(object? sender, EventArgs e)
    {
        _feedbackDelaySeconds--;
        if (_feedbackDelaySeconds <= 0)
        {
            _feedbackTimer.Stop();
            NextQuestion();
        }
    }

    private void StartFeedbackTimer()
    {
        _feedbackDelaySeconds = 2;
        _feedbackTimer.Start();
    }

    // Hanterar svar (kontrollerar rätt/fel, ökar poäng, visar feedback)
    private void SubmitAnswer(object? parameter)
    {
        if (parameter is not AnswerOption selectedOption || _hasAnswered) return;
        _timer.Stop();
        var correctOption = _shuffledOptions.FirstOrDefault(o => o.IsCorrect);
        if (correctOption != null)
        {
            if (selectedOption.IsCorrect)
            {
                Score++;
                UserAnswerText = "Correct!";
                CorrectAnswerText = $"Correct answer: {correctOption.Text}";
            }
            else
            {
                UserAnswerText = "Incorrect!";
                CorrectAnswerText = $"Correct answer: {correctOption.Text}";
            }
        }
        HasAnswered = true;
        StartFeedbackTimer();
    }

    private void NextQuestion()
    {
        _timer.Stop();
        CurrentQuestionIndex++;
        LoadQuestion();
    }

    private void EndRound()
    {
        _timer.Stop();
        _feedbackTimer.Stop();
        _feedbackDelaySeconds = 0;
        IsRoundActive = false;
        ShowResults = true;
        RaisePropertyChanged(nameof(TotalQuestions));
        RaisePropertyChanged(nameof(Score));
        StartRoundCommand.RaiseCanExecuteChanged();
    }

    private void ResetQuizState()
    {
        _timer.Stop();
        _feedbackTimer.Stop();
        _feedbackDelaySeconds = 0;
        _shuffledQuestions.Clear();
        _shuffledOptions.Clear();
        ShuffledOptions.Clear();
        CurrentQuestionIndex = -1;
        RemainingTime = 0;
        Score = 0;
        HasAnswered = false;
        CorrectAnswerText = "";
        UserAnswerText = "";
        IsRoundActive = false;
        ShowResults = false;
        StartRoundCommand.RaiseCanExecuteChanged();
    }

    private static void RandomListAlgoritm<T>(List<T> list)
    {
        var random = new Random();
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
