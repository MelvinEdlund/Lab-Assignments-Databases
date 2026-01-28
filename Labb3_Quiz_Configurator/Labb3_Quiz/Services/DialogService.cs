using System.Windows;
using Labb3_Quiz_MongoDB.Services.Trivia;
using Labb3_Quiz_MongoDB.ViewModels;
using Labb3_Quiz_MongoDB.Views.Dialogs;

namespace Labb3_Quiz_MongoDB.Services;

// Service för att visa dialoger
public class DialogService : IDialogService
{
    private readonly ITriviaService _triviaService;

    public DialogService()
    {
        _triviaService = new OpenTdbService();
    }

    public void ShowInfo(string message, string title = "Information")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void ShowError(string message, string title = "Error")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public bool ShowConfirm(string message, string title = "Confirm")
    {
        var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    }

    public void ShowImportQuestionsDialog(
        Func<QuestionPackViewModel?> getSelected,
        Func<Task> saveAll)
    {
        var vm = new ImportQuestionsViewModel(_triviaService, this);
        vm.GetSelectedPack = getSelected;
        vm.SaveAllAsync = saveAll;

        var dlg = new ImportQuestionsDialog 
        { 
            DataContext = vm, 
            Owner = Application.Current.MainWindow 
        };
        dlg.ShowDialog();
    }
}

