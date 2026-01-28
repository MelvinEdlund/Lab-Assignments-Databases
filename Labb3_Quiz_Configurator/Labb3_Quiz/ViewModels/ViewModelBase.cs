using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Labb3_Quiz_MongoDB.ViewModels;

// Basklass för ViewModels - hanterar property change notifications
public class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}