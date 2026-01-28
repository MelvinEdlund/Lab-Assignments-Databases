using System.Windows;
using System.Windows.Controls;
using Labb3_Quiz_MongoDB.ViewModels;

namespace Labb3_Quiz_MongoDB.Views.Dialogs;

// (laddar kategorier från API vid öppning)
public partial class ImportQuestionsDialog : Window
{
    public ImportQuestionsDialog()
    {
        InitializeComponent();
        Loaded += async (_, __) =>
        {
            if (DataContext is ImportQuestionsViewModel vm && vm.LoadCategoriesCommand.CanExecute(null))
            {
                await Task.Run(async () =>
                {
                    await Application.Current.Dispatcher.InvokeAsync(() => 
                        vm.LoadCategoriesCommand.Execute(null));
                });
            }
        };
    }

    private void DifficultyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem item && DataContext is ImportQuestionsViewModel vm)
            vm.SelectedDifficulty = item.Tag?.ToString();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}

