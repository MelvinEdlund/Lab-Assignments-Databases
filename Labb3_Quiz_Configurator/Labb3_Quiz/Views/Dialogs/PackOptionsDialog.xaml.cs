using System.Windows;
using Labb3_Quiz_MongoDB.ViewModels;

namespace Labb3_Quiz_MongoDB.Views.Dialogs;

// (binder till QuestionPackViewModel)
public partial class PackOptionsDialog : Window
{
    public PackOptionsDialog(QuestionPackViewModel packViewModel)
    {
        InitializeComponent();
        DataContext = packViewModel;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

