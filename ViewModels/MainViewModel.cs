using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Threading.Tasks;
using Microsoft.Win32;
using RenPy_VisualScripting.Models;

namespace RenPy_VisualScripting.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    public ICommand OpenFileCommand { get; }

    private ChoiceNode _choiceTree;
    public ChoiceNode ChoiceTree
    {
        get => _choiceTree;
        set
        {
            _choiceTree = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ChoiceTree)));
        }
    }

    public string FilePath { get; private set; }

    public event EventHandler<string> FileOpened;

    public MainViewModel()
    {
        OpenFileCommand = new RelayCommand(async () => await LoadChoiceTreeAsync());
    }

    private async Task LoadChoiceTreeAsync()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "RenPy Script Files (*.rpy)|*.rpy|All Files (*.*)|*.*"
        };
        if (openFileDialog.ShowDialog() == true)
        {
            FilePath = openFileDialog.FileName;
            var parser = new RenPyParser();
            ChoiceTree = await parser.ParseAsync(FilePath);
            FileOpened?.Invoke(this, FilePath);
        }
    }
}
