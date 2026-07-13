using System.Windows;
using CaisseToast.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CaisseToast.Wpf;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
