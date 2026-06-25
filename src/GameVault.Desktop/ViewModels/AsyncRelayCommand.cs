using System.Windows.Input;

namespace GameVault.ViewModels;

public class AsyncRelayCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Predicate<object?>? _canExecute;
    private bool _isRunning;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute is null ? null : _ => canExecute())
    {
    }

    public AsyncRelayCommand(
        Func<object?, Task> execute,
        Predicate<object?>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) =>
        !_isRunning && (_canExecute?.Invoke(parameter) ?? true);

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;

        _isRunning = true;
        CommandManager.InvalidateRequerySuggested();
        try
        {
            await _execute(parameter);
        }
        finally
        {
            _isRunning = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
