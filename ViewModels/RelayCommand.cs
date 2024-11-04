namespace RenPy_VisualScripting.ViewModels;
using System.Windows.Input;

public class RelayCommand(Func<Task> execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => canExecute != null && canExecute();

    public async void Execute(object? parameter) => await execute();
}