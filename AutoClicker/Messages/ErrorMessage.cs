namespace AutoClicker.Messages;

/// <summary>
/// Signals an error message to be displayed.
/// </summary>
/// <param name="Message"></param>
/// <param name="Title"></param>
public record ErrorMessage(string Message, string Title);