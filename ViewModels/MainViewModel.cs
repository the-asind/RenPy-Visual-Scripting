using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Threading.Tasks;
using Microsoft.Win32;
using RenPy_VisualScripting.Models;
using System.Collections.ObjectModel;
using System.Linq;
using RenPy_VisualScripting.Commands;

namespace RenPy_VisualScripting.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    public ICommand OpenFileCommand { get; }

    private ChoiceNodeViewModel _choiceTreeViewModel;
    public ChoiceNodeViewModel ChoiceTreeViewModel
    {
        get => _choiceTreeViewModel;
        set
        {
            _choiceTreeViewModel = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ChoiceTreeViewModel)));
        }
    }

    public ObservableCollection<ChoiceNodeViewModel> RootNodes { get; set; } = new();

    public string FilePath { get; private set; }

    public event EventHandler<string> FileOpened;

    public MainViewModel()
    {
        OpenFileCommand = new RelayCommand(async () => await LoadChoiceTreeAsync(), () => true);
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
            var choiceTree = await parser.ParseAsync(FilePath);
            ChoiceTreeViewModel = new ChoiceNodeViewModel(choiceTree);

            // Extract LabelBlocks for tabs
            RootNodes.Clear();
            foreach (var node in ChoiceTreeViewModel.Children.Where(n => n.NodeType == ChoiceNodeType.LabelBlock))
            {
                RootNodes.Add(node);
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RootNodes)));
            FileOpened?.Invoke(this, FilePath);
        }
    }
}

