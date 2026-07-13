using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CaisseToast.Wpf.ViewModels;

public partial class PlaceholderViewModel : ObservableObject
{
    public string Title { get; }
    public string Subtitle { get; }

    public event Action? BackRequested;

    public PlaceholderViewModel(string title, string subtitle)
    {
        Title = title;
        Subtitle = subtitle;
    }

    [RelayCommand]
    private void GoBack() => BackRequested?.Invoke();
}
