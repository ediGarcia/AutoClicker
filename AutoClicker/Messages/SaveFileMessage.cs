namespace AutoClicker.Messages;

/// <summary>
/// Signals a request to save a file and awaits a response indicating success or failure.
/// </summary>
public class SaveFileMessage
{
    private readonly TaskCompletionSource<bool?> _tcs = new();

    #region Public Methods

    public Task<bool?> GetResponseAsync() =>
        _tcs.Task;

    public void Reply(bool? result) =>
        _tcs.TrySetResult(result);

    #endregion
}