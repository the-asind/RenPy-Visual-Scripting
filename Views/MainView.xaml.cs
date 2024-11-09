using System.Windows;
using RenPy_VisualScripting.ViewModels;

namespace RenPy_VisualScripting;

public partial class MainView : Window
{
    public MainView() 
    {
        InitializeComponent();
        var viewModel = new MainViewModel();
        DataContext = viewModel;
        viewModel.FileOpened += ViewModel_FileOpened;
    }

    private void ViewModel_FileOpened(object sender, string filePath)
    {
        // Handle file opened event if needed
    }
}