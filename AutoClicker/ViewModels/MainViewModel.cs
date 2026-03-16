using AutoClicker.Messages;
using AutoClicker.Models;
using AutoClicker.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using HelperMethods;
using System.Windows.Input;
using HelperExtensions;

namespace AutoClicker.ViewModels;

public partial class MainViewModel : ObservableObject
{
    #region Inner Classes

    #region RepeaterData
    /// <summary>
    /// The data structure to hold repeater information during command execution.
    /// </summary>
    /// <param name="commandIndex"></param>
    /// <param name="counter"></param>
    private class RepeaterData(int commandIndex, int counter)
    {
        #region Properties

        /// <summary>
        /// Gets the index of the command where the repeater starts.
        /// </summary>
        public int CommandIndex { get; } = commandIndex;

        /// <summary>
        /// Gets or sets the remaining count of repetitions for the repeater.
        /// </summary>
        public int Counter { get; set; } = counter;

        #endregion
    }
    #endregion

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets a value indicating whether mouse movement should be captured during recording.
    /// </summary>
    [ObservableProperty]
    private bool _captureMouseMovement;

    /// <summary>
    /// Gets or sets the command text.
    /// </summary>
    public string CommandText
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
                HasChanges = !value.IsNullOrEmpty() || _filename is not null;
        }
    }

    /// <summary>
    /// Gets a value indicating whether there are unsaved changes.
    /// </summary>
    public bool HasChanges
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                SaveFileCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// Gets a value indicating whether the application is currently recording.
    /// </summary>
    public bool IsRecording
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                StartRecordingCommand.NotifyCanExecuteChanged();
                StartRunningCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the application is currently running.
    /// </summary>
    public bool IsRunning
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
            {
                StartRecordingCommand.NotifyCanExecuteChanged();
                StartRunningCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the mouse capture precision in milliseconds.
    /// </summary>
    [ObservableProperty] 
    private int _mouseCapturePrecision = 50;

    /// <summary>
    /// Gets or sets the key used to stop recording or running.
    /// </summary>
    [ObservableProperty]
    private Key _stopKey = Key.Escape;

    #endregion

    private string? _filename;
    private DateTime _lastRecordedUpdate;

    #region Events

    #region CanSaveFile
    /// <summary>
    /// Determines whether the <see cref="SaveFileCommand"/> command can be executed.
    /// </summary>
    /// <returns></returns>
    private bool CanSaveFile() =>
        HasChanges;
    #endregion

    #region CanStartRecording
    /// <summary>
    /// Determines whether the <see cref="StartRecordingCommand"/> command can be executed.
    /// </summary>
    /// <returns></returns>
    private bool CanStartRecording() =>
            !IsRecording && !IsRunning;
    #endregion

    #region CanStartRunning
    /// <summary>
    /// Determines whether the <see cref="StartRunningCommand"/> command can be executed.
    /// </summary>
    /// <returns></returns>
    private bool CanStartRunning() =>
    !IsRecording && !IsRunning;
    #endregion

    #region CreateNewFile
    /// <summary>
    /// Clears the current command text to create a new file.
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task CreateNewFile()
    {
        if (!await CheckUnsavedChanges())
            return;

        CommandText = String.Empty;
        _filename = null;
        HasChanges = false;
    }
    #endregion

    #region OpenFile
    /// <summary>
    /// Opens an existing file and loads its content into the command text.
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task OpenFile()
    {
        if (!await CheckUnsavedChanges() || !SystemService.ShowOpenFileDialog(out string? filename))
            return;

        try
        {
            CommandText = FileHelper.ReadAllText(filename!);
            _filename = filename;
            HasChanges = false;
        }
        catch (Exception ex)
        {
            WeakReferenceMessenger.Default.Send(new ErrorMessage(ex.Message, "File Error"));
        }
    }
    #endregion

    #region SaveFile
    /// <summary>
    /// Saves the current command text to a file.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSaveFile))]
    private void SaveFile() =>
    SaveFile(false);
    #endregion

    #region SaveFileAs
    /// <summary>
    /// Saves the current command text to a new file.
    /// </summary>
    [RelayCommand]
    private void SaveFileAs() =>
        SaveFile(true);
    #endregion

    #region StartRecording
    /// <summary>
    /// Starts recording user input to generate command text.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStartRecording))]
    private void StartRecording()
    {
        IsRecording = true;
        _lastRecordedUpdate = DateTime.Now;

        if (!CommandText.IsNullOrEmpty() && !CommandText.EndsWith('\n'))
            CommandText += Environment.NewLine;

        int repeaterLevel = 0;
        SystemService.StartMonitoring(
            (eventType, data, timeStamp) =>
        {
            if (eventType is CommandType.Comment or CommandType.Empty)
                return;

            string indentation = String.Empty.FillLeft(repeaterLevel, "    ");
            CommandText += indentation + CommandService.DecompileCommand(new(CommandType.Wait, (int)Math.Round((timeStamp - _lastRecordedUpdate).TotalMilliseconds)));

            if (eventType is CommandType.KeyPress or CommandType.KeyDown && (Key)data == StopKey)
            {
                StopMonitoring();
                return;
            }

            CommandText += indentation + CommandService.DecompileCommand(new(eventType, data));

            _lastRecordedUpdate = timeStamp;

            switch (eventType)
            {
                case CommandType.StartRepeater:
                    repeaterLevel++;
                    break;

                case CommandType.EndRepeater:
                    repeaterLevel--;
                    break;
            }
        },
            CaptureMouseMovement,
            TimeSpan.FromMilliseconds(MouseCapturePrecision)
            );
    }
    #endregion

    #region StartRunning
    /// <summary>
    /// Starts executing the commands in the command text.
    /// </summary>
    /// <returns></returns>
    [RelayCommand(CanExecute = nameof(CanStartRunning))]
    private async Task StartRunning()
    {
        IsRunning = true;

        try
        {
            IReadOnlyList<Command> commands = CommandService.CompileCommandText(CommandText);
            CancellationTokenSource tokenSource = new();

            SystemService.StartMonitoringKey(StopKey, () => tokenSource.Cancel());

            await Task.Run(async () =>
            {
                Stack<RepeaterData> repeaters = [];
                RepeaterData? currentRepeater = null;

                for (int i = 0; i < commands.Count; i++)
                {
                    if (tokenSource.Token.IsCancellationRequested)
                        break;

                    Command command = commands[i];

                    switch (command.Type)
                    {
                        case CommandType.StartRepeater:
                        {
                            if (currentRepeater != null)
                                repeaters.Push(currentRepeater);

                            currentRepeater = new(i, (int)command.Value);
                            break;
                        }

                        case CommandType.EndRepeater:
                        {
                            currentRepeater.Counter--;

                            if (currentRepeater.Counter > 0)
                                i = currentRepeater.CommandIndex;
                            else
                            {
                                if (repeaters.Count > 0)
                                {
                                    currentRepeater = repeaters.Pop();
                                    i = currentRepeater.CommandIndex;
                                }
                                else
                                    currentRepeater = null;
                            }
                            break;
                        }

                        case CommandType.Comment:
                        case CommandType.Empty:
                            continue;

                        default:
                            await CommandService.RunCommand(command, tokenSource.Token);
                            break;
                    }
                }
            }, tokenSource.Token);
        }
        catch(TaskCanceledException) { }
        catch (Exception ex)
        {
            WeakReferenceMessenger.Default.Send(new ErrorMessage(ex.Message, "Script Error"));
        }

        SystemService.StopMonitoring();
        IsRunning = false;
    }
    #endregion

    #region StopMonitoring
    /// <summary>
    /// Stops monitoring user input for recording.
    /// </summary>
    [RelayCommand]
    private void StopMonitoring()
    {
        SystemService.StopMonitoring();
        IsRecording = false;
    }
    #endregion

    #endregion

    #region Private Methods

    #region CheckUnsavedChanges
    /// <summary>
    /// Checks for unsaved changes and prompts the user to save if necessary.
    /// </summary>
    /// <returns></returns>
    private async Task<bool> CheckUnsavedChanges()
    {
        if (!HasChanges)
            return true;

        SaveFileMessage message = new();
        WeakReferenceMessenger.Default.Send(message);

        return await message.GetResponseAsync() switch
        {
            true => SaveFile(false),
            null => false,
            _ => true
        };
    }
    #endregion

    #region SaveFile
    /// <summary>
    /// Saves the command text to a file.
    /// </summary>
    /// <param name="forceNewFile"></param>
    /// <returns></returns>
    private bool SaveFile(bool forceNewFile)
    {
        string? filename = _filename;

        if (forceNewFile || _filename is null)
        {
            if (!SystemService.ShowSaveFileDialog(out filename))
                return false;
        }

        try
        {

            FileHelper.WriteAllText(filename!, CommandText);
            _filename = filename;
            HasChanges = false;

            return true;
        }
        catch (Exception ex)
        {
            WeakReferenceMessenger.Default.Send(new ErrorMessage(ex.Message, "File Error"));
            return false;
        }
    }
    #endregion

    #endregion
}