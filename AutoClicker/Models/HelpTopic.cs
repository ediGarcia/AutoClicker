namespace AutoClicker.Models;

public class HelpTopic(string commandText, string title, string description)
{
    #region Properties

    /// <summary>
    /// Gets the command text to be sent to the clipboard.
    /// </summary>
    public string CommandText { get; } = commandText;

    /// <summary>
    /// Gets the command's description.
    /// </summary>
    public string Description { get; } = description;

    /// <summary>
    /// Gets the topic's title.
    /// </summary>
    public string Title { get; } = title;

    #endregion
}