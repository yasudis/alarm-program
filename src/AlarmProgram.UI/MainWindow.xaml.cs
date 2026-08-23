using AlarmProgram.UI.ViewModels;
using System.Windows;

namespace AlarmProgram.UI;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
