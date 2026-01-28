using Labb3_Quiz_MongoDB.ViewModels;

namespace Labb3_Quiz_MongoDB.Services;

// Interface för dialoger
public interface IDialogService
{
    void ShowInfo(string message, string title = "Information");
    void ShowError(string message, string title = "Error");
    bool ShowConfirm(string message, string title = "Confirm");
    void ShowImportQuestionsDialog(
        Func<QuestionPackViewModel?> getSelected,
        Func<Task> saveAll);
}

