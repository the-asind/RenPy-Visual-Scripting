using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RenPy_VisualScripting.Commands;

public class RelayCommandAsync : ICommand
{
    private readonly Func<Task> _executeAsync;
    private readonly Func<bool> _canExecute;

    public event EventHandler CanExecuteChanged 
    {
        add    { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public RelayCommandAsync(Func<Task> executeAsync, Func<bool> canExecute = null)
    {
        _executeAsync = executeAsync;
        _canExecute   = canExecute;
    }

    public bool CanExecute(object parameter) => _canExecute == null || _canExecute();

    public async void Execute(object parameter) => await _executeAsync();
}
