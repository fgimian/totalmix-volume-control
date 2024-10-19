using System.Windows.Input;

namespace TotalMixVC;

/// <summary>
/// Implements the ICommand interface for system tray commands.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DelegateCommand"/> class.
/// </remarks>
/// <param name="commandAction">Action to perform for the command.</param>
/// <param name="canExecuteFunc">Whether the command can execute.</param>
public class DelegateCommand(Action commandAction, Func<bool>? canExecuteFunc = null) : ICommand
{
    /// <summary>
    /// Event which is triggered when the possible execute state changes.
    /// </summary>
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <summary>
    /// Gets or sets the action to perform for the command.
    /// </summary>
    public Action CommandAction { get; set; } = commandAction;

    /// <summary>
    /// Gets or sets whether the command can execute.
    /// </summary>
    public Func<bool>? CanExecuteFunc { get; set; } = canExecuteFunc;

    /// <summary>
    /// Execute the command.
    /// </summary>
    /// <param name="parameter">Parameter to be passed to the related function.</param>
    public void Execute(object? parameter)
    {
        CommandAction();
    }

    /// <summary>
    /// Whether the command can execute based on the related function defined.
    /// </summary>
    /// <param name="parameter">Parameter to be passed to the related function.</param>
    /// <returns>Whether or not the command can be executed.</returns>
    public bool CanExecute(object? parameter)
    {
        return CanExecuteFunc is null || CanExecuteFunc();
    }
}
