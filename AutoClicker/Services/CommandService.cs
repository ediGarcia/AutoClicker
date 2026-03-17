using AutoClicker.Models;
using HelperExtensions;
using System.Windows;
using System.Windows.Input;

namespace AutoClicker.Services;

public static class CommandService
{
    private const int ClickDelay = 10;
    private const string MouseMoveCommand = nameof(CommandType.MouseMove);
    private const string MouseWheelCommand = nameof(CommandType.MouseWheel);
    private const string LeftButtonClickCommand = nameof(CommandType.LeftButtonClick);
    private const string LeftButtonDoubleClickCommand = nameof(CommandType.LeftButtonDoubleClick);
    private const string LeftButtonDownCommand = nameof(CommandType.LeftButtonDown);
    private const string LeftButtonUpCommand = nameof(CommandType.LeftButtonUp);
    private const string RightButtonClickCommand = nameof(CommandType.RightButtonClick);
    private const string RightButtonDoubleClickCommand = nameof(CommandType.RightButtonDoubleClick);
    private const string RightButtonDownCommand = nameof(CommandType.RightButtonDown);
    private const string RightButtonUpCommand = nameof(CommandType.RightButtonUp);
    private const string MiddleButtonClickCommand = nameof(CommandType.MiddleButtonClick);
    private const string MiddleButtonDoubleClickCommand = nameof(CommandType.MiddleButtonDoubleClick);
    private const string MiddleButtonDownCommand = nameof(CommandType.MiddleButtonDown);
    private const string MiddleButtonUpCommand = nameof(CommandType.MiddleButtonUp);
    private const string KeyPressCommand = nameof(CommandType.KeyPress);
    private const string KeyDownCommand = nameof(CommandType.KeyDown);
    private const string KeyUpCommand = nameof(CommandType.KeyUp);
    private const string StartRepeaterCommand = nameof(CommandType.StartRepeater);
    private const string EndRepeaterCommand = nameof(CommandType.EndRepeater);
    private const string WaitCommand = nameof(CommandType.Wait);

    #region Public Methods

    #region CompileCommandText
    /// <summary>
    /// Compiles the given command text into a list of Command objects.
    /// </summary>
    /// <param name="commandText"></param>
    /// <returns></returns>
    /// <exception cref="FormatException"></exception>
    public static IReadOnlyList<Command> CompileCommandText(string commandText)
    {
        if (commandText.IsNullOrEmpty())
            return [];

        string[] lines = commandText.Split("\r\n", "\n");
        List<Command> commands = new(lines.Length);
        int repeaterCount = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            if (line.IsNullOrWhiteSpace())
            {
                commands.Add(new(CommandType.Empty));
                continue;
            }

            Command command;
            string[] parts = line.Trim().Split([' ', '\t'], 3, StringSplitOptions.RemoveEmptyEntries);

            switch (parts[0])
            {
                case MouseMoveCommand:
                    command = GenerateMouseCommand(CommandType.MouseMove, parts, i);
                    break;

                case MouseWheelCommand:
                    ValidatePartsLength(parts, 2, i);
                    command = new(CommandType.MouseWheel, ConvertToInt(parts[1], true, i));
                    break;

                case LeftButtonClickCommand:
                    command = GenerateMouseCommand(CommandType.LeftButtonClick, parts, i);
                    break;

                case LeftButtonDoubleClickCommand:
                    command = GenerateMouseCommand(CommandType.LeftButtonDoubleClick, parts, i);
                    break;

                case LeftButtonDownCommand:
                    command = GenerateMouseCommand(CommandType.LeftButtonDown, parts, i);
                    break;

                case LeftButtonUpCommand:
                    command = GenerateMouseCommand(CommandType.LeftButtonUp, parts, i);
                    break;

                case RightButtonClickCommand:
                    command = GenerateMouseCommand(CommandType.RightButtonClick, parts, i);
                    break;

                case RightButtonDoubleClickCommand:
                    command = GenerateMouseCommand(CommandType.RightButtonDoubleClick, parts, i);
                    break;

                case RightButtonDownCommand:
                    command = GenerateMouseCommand(CommandType.RightButtonDown, parts, i);
                    break;

                case RightButtonUpCommand:
                    command = GenerateMouseCommand(CommandType.RightButtonUp, parts, i);
                    break;

                case MiddleButtonClickCommand:
                    command = GenerateMouseCommand(CommandType.MiddleButtonClick, parts, i);
                    break;

                case MiddleButtonDoubleClickCommand:
                    command = GenerateMouseCommand(CommandType.MiddleButtonDoubleClick, parts, i);
                    break;

                case MiddleButtonDownCommand:
                    command = GenerateMouseCommand(CommandType.MiddleButtonDown, parts, i);
                    break;

                case MiddleButtonUpCommand:
                    command = GenerateMouseCommand(CommandType.MiddleButtonUp, parts, i);
                    break;

                case KeyPressCommand:
                {
                    ValidatePartsLength(parts, 2, i);
                    command = new(CommandType.KeyPress, ConvertToEnum<Key>(parts[1], i));
                    break;
                }

                case KeyDownCommand:
                {
                    ValidatePartsLength(parts, 2, i);
                    command = new(CommandType.KeyDown, ConvertToEnum<Key>(parts[1], i));
                    break;
                }

                case KeyUpCommand:
                {
                    ValidatePartsLength(parts, 2, i);
                    command = new(CommandType.KeyUp, ConvertToEnum<Key>(parts[1], i));
                    break;
                }

                case StartRepeaterCommand:
                {
                    ValidatePartsLength(parts, 2, i);
                    int count = ConvertToInt(parts[1], false, i);

                    if (count == 0)
                        ThrowFormatError(i, $"{nameof(StartRepeaterCommand)} value must be greater than zero");

                    command = new(CommandType.StartRepeater, count);
                    repeaterCount++;
                    break;
                }

                case EndRepeaterCommand:
                {
                    ValidatePartsLength(parts, 1, i);
                    if (repeaterCount == 0)
                        ThrowFormatError(i, $"'{nameof(CommandType.EndRepeater)}' without matching '{nameof(CommandType.StartRepeater)}'");

                    command = new(CommandType.EndRepeater);
                    repeaterCount--;
                    break;
                }

                case WaitCommand:
                {
                    ValidatePartsLength(parts, 2, i);
                    command = new(CommandType.Wait, ConvertToInt(parts[1], false, i));
                    break;
                }

                case "//":
                {
                    command = new(CommandType.Comment, line.Length > 2 ? line[2..].Trim() : String.Empty);
                    commands.Add(command);
                    break;
                }

                default:
                    ThrowFormatError(i, "Invalid command");
                    break;
            }

            commands.Add(command);
        }

        if (repeaterCount > 0)
            ThrowFormatError(lines.LastIndex(), $"'{nameof(CommandType.StartRepeater)}' without matching '{nameof(CommandType.EndRepeater)}'");

        return commands;

        #region Local Functions

        // Helper function to convert string to enum with error handling.
        T ConvertToEnum<T>(string part, int lineIndex) where T : struct, Enum
        {
            if (!Enum.TryParse(part, true, out T result))
                ThrowFormatError(lineIndex);

            return result;
        }

        // Helper function to convert string to int with error handling.
        int ConvertToInt(string part, bool allowNegative, int lineIndex)
        {
            if (!Int32.TryParse(part, out int number) || !allowNegative && number < 0)
                ThrowFormatError(lineIndex);

            return number;
        }

        // Helper function to generate mouse-related commands.
        Command GenerateMouseCommand(CommandType type, string[] parts, int lineIndex)
        {
            switch (parts.Length)
            {
                case 1:
                    return new(type);

                case 3:
                {
                    int x = ConvertToInt(parts[1], true, lineIndex);
                    int y = ConvertToInt(parts[2], true, lineIndex);

                    return new(type, new Point(x, y));
                }

                default:
                    ThrowFormatError(lineIndex);
                    break;
            }

            return null!;
        }

        // Helper function to validate parts length.
        void ValidatePartsLength(string[] parts, int expectedLength, int lineIndex)
        {
            if (parts.Length != expectedLength)
                ThrowFormatError(lineIndex);
        }

        // Helper function to throw format error with line index.
        void ThrowFormatError(int lineIndex, string? message = "Invalid command format") =>
            throw new FormatException($"Invalid file format (line {lineIndex + 1}): {message}.");

        #endregion
    }
    #endregion

    #region DecompileCommands
    /// <summary>
    /// Decompiles the given Command object into command text.
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static string DecompileCommand(Command command)
    {
        switch (command.Type)
        {
            case CommandType.MouseMove:
            {
                Point position = (Point)command.Value;
                return $"{MouseMoveCommand} {position.X} {position.Y}{Environment.NewLine}";
            }

            case CommandType.MouseWheel:
                return $"{MouseWheelCommand} {(int)command.Value}{Environment.NewLine}";

            case CommandType.LeftButtonClick:
            {
                Point position = (Point)command.Value;
                return $"{LeftButtonClickCommand} {position.X} {position.Y}{Environment.NewLine}";
            }

            case CommandType.LeftButtonDoubleClick:
            {
                Point position = (Point)command.Value;
                return $"{LeftButtonDoubleClickCommand} {position.X} {position.Y}{Environment.NewLine}";
            }

            case CommandType.LeftButtonDown:
            {
                Point position = (Point)command.Value;
                return $"{LeftButtonDownCommand} {position.X} {position.Y}{Environment.NewLine}";
            }

            case CommandType.LeftButtonUp:
            {
                Point position = (Point)command.Value;
                return $"{LeftButtonUpCommand} {position.X} {position.Y}{Environment.NewLine}";
            }

            case CommandType.RightButtonClick:
            {
                Point position = (Point)command.Value;
                return $"{RightButtonClickCommand} {position.X} {position.Y}{Environment.NewLine}";
            }

            case CommandType.RightButtonDoubleClick:
            {
                Point position = (Point)command.Value;
                return $"{RightButtonDoubleClickCommand} {position.X} {position.Y}{Environment.NewLine}";
            }

            case CommandType.RightButtonDown:
            {
                Point position = (Point)command.Value;
                return $"{RightButtonDownCommand} {position.X} {position.Y}{Environment.NewLine}";
            }

            case CommandType.RightButtonUp:
            {
                Point position = (Point)command.Value;
                return $"{RightButtonUpCommand} {position.X} {position.Y}{Environment.NewLine}";
            }

            case CommandType.MiddleButtonClick:
            {
                Point position = (Point)command.Value;
                return $"{MiddleButtonClickCommand} {position.X} {position.Y}{Environment.NewLine}";
            }

            case CommandType.MiddleButtonDoubleClick:
            {
                Point position = (Point)command.Value;
                return $"{MiddleButtonDoubleClickCommand} {position.X} {position.Y}{Environment.NewLine}";
            }

            case CommandType.MiddleButtonDown:
            {
                Point position = (Point)command.Value;
                return $"{MiddleButtonDownCommand} {position.X} {position.Y}{Environment.NewLine}";
            }

            case CommandType.MiddleButtonUp:
            {
                Point position = (Point)command.Value;
                return $"{MiddleButtonUpCommand} {position.X} {position.Y}{Environment.NewLine}";
            }

            case CommandType.KeyPress:
                return $"{KeyPressCommand} {(Key)command.Value}{Environment.NewLine}";

            case CommandType.KeyDown:
                return $"{KeyDownCommand} {(Key)command.Value}{Environment.NewLine}";

            case CommandType.KeyUp:
                return $"{KeyUpCommand} {(Key)command.Value}{Environment.NewLine}";

            case CommandType.StartRepeater:
                return $"{StartRepeaterCommand} {(int)command.Value}{Environment.NewLine}";

            case CommandType.EndRepeater:
                return EndRepeaterCommand + Environment.NewLine;
            
            case CommandType.Wait:
                return $"{WaitCommand} {(int)command.Value}{Environment.NewLine}";

            case CommandType.Comment:
                return $"// {command.Value}{Environment.NewLine}";

            case CommandType.Empty:
                return Environment.NewLine;

            default:
                throw new ArgumentOutOfRangeException($"Unknown command type: {command.Type}.");
        }
    }
    #endregion

    #region RunCommand
    /// <summary>
    /// Runs the given <see cref="Command"/> instance.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static async Task RunCommand(Command command, CancellationToken token)
    {
        switch (command.Type)
        {
            case CommandType.MouseMove:
                SystemService.SetMousePosition((Point)command.Value);
                break;

            case CommandType.MouseWheel:
                SystemService.SimulateMouseWheel((int)command.Value);
                break;

            case CommandType.LeftButtonClick:
                await MoveMouseAndWait(command.Value as Point?);
                SystemService.SimulateMouseClick(MouseButton.Left);
                break;

            case CommandType.LeftButtonDoubleClick:
                await MoveMouseAndWait(command.Value as Point?);
                SystemService.SimulateMouseDoubleClick(MouseButton.Left);
                break;

            case CommandType.LeftButtonDown:
                await MoveMouseAndWait(command.Value as Point?);
                SystemService.SimulateMouseDown(MouseButton.Left);
                break;

            case CommandType.LeftButtonUp:
                await MoveMouseAndWait(command.Value as Point?);
                SystemService.SimulateMouseUp(MouseButton.Left);
                break;

            case CommandType.RightButtonClick:
                await MoveMouseAndWait(command.Value as Point?);
                SystemService.SimulateMouseClick(MouseButton.Right);
                break;

            case CommandType.RightButtonDoubleClick:
                await MoveMouseAndWait(command.Value as Point?);
                SystemService.SimulateMouseDoubleClick(MouseButton.Right);
                break;

            case CommandType.RightButtonDown:
                await MoveMouseAndWait(command.Value as Point?);
                SystemService.SimulateMouseDown(MouseButton.Right);
                break;

            case CommandType.RightButtonUp:
                await MoveMouseAndWait(command.Value as Point?);
                SystemService.SimulateMouseUp(MouseButton.Right);
                break;

            case CommandType.MiddleButtonClick:
                await MoveMouseAndWait(command.Value as Point?);
                SystemService.SimulateMouseClick(MouseButton.Middle);
                break;

            case CommandType.MiddleButtonDoubleClick:
                await MoveMouseAndWait(command.Value as Point?);
                SystemService.SimulateMouseDoubleClick(MouseButton.Middle);
                break;

            case CommandType.MiddleButtonDown:
                await MoveMouseAndWait(command.Value as Point?);
                SystemService.SimulateMouseDown(MouseButton.Middle);
                break;

            case CommandType.MiddleButtonUp:
                await MoveMouseAndWait(command.Value as Point?);
                SystemService.SimulateMouseUp(MouseButton.Middle);
                break;

            case CommandType.KeyPress:
                SystemService.SimulateKeyPress((Key)command.Value);
                break;

            case CommandType.KeyDown:
                SystemService.SimulateKeyDown((Key)command.Value);
                break;

            case CommandType.KeyUp:
                SystemService.SimulateKeyUp((Key)command.Value);
                break;

            case CommandType.Wait:
                await Task.Delay((int)command.Value, token);
                break;

            default:
                throw new ArgumentOutOfRangeException($"Unsupported command type: {command.Type}.");
        }

        #region Local Functions

        // Moves the cursor of the mouse to the specified position and waits 10 ms.
        async Task MoveMouseAndWait(Point? position)
        {
            if (position == null)
                return;

            SystemService.SetMousePosition((Point)command.Value);
                await Task.Delay(ClickDelay, CancellationToken.None);
        }

        #endregion
    }
    #endregion

    #endregion
}