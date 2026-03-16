namespace AutoClicker.Models;

public class Command(CommandType type, object? value = null)
{
    #region Properties

    /// <summary>
    /// Gets the type of the command.
    /// </summary>
    public CommandType Type { get; } = type;

    /// <summary>
    /// Gets the value associated with the command.
    /// </summary>
    public object? Value { get; } = value;

    #endregion
}