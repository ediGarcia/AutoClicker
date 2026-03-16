using AutoClicker.Models;
using Gma.System.MouseKeyHook;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace AutoClicker.Services;

public static class SystemService
{
    #region Imports

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MouseInput MI;
        [FieldOffset(0)]
        public KeyboardInput KI;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort WVk;
        public ushort WScan;
        public uint DwFlags;
        public uint Time;
        public IntPtr DwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint DwFlags;
        public uint Time;
        public IntPtr DwExtraInfo;
    }

    [DllImport("user32.dll")]
    private static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

    [DllImport("User32.Dll")]
    private static extern bool SetCursorPos(int x, int y);

    #endregion

    // ReSharper disable CommentTypo
    private const uint MouseInputTypeFlag = 0; // INPUT_MOUSE
    private const uint KeyboardInputTypeFlag = 1; // INPUT_KEYBOARD
    private const uint KeyDownEventFlag = 0x0000; // KEYEVENTF_KEYDOWN
    private const uint KeyUpEventFlag = 0x0002; // KEYEVENTF_KEYUP
    private const uint MouseLeftDownEventFlag = 0x0002; // MOUSEEVENTF_LEFTDOWN
    private const uint MouseLeftUpEventFlag = 0x0004; // MOUSEEVENTF_LEFTUP
    private const uint MouseRightDownEventFlag = 0x0008; // MOUSEEVENTF_RIGHTDOWN
    private const uint MouseRightUpEventFlag = 0x0010; // MOUSEEVENTF_RIGHTUP
    private const uint MouseMiddleDownEventFlag = 0x0020; // MOUSEEVENTF_MIDDLEDOWN
    private const uint MouseMiddleUpEventFlag = 0x0040; // MOUSEEVENTF_MIDDLEUP
    private const uint MouseWheelEventFlag = 0x0800; // MOUSEEVENTF_WHEEL
    private const int WheelDelta = 120; // WHEEL_DELTA
    // ReSharper restore CommentTypo

    private static IKeyboardMouseEvents? _globalHook;
    private static DateTime _lastMousePositionTime;

    #region Public Methods

    #region SetMousePosition
    /// <summary>
    /// Sets the mouse cursor position to the specified point.
    /// </summary>
    /// <param name="position"></param>
    public static void SetMousePosition(Point position) =>
    SetCursorPos((int)position.X, (int)position.Y);
    #endregion

    #region ShowOpenFileDialog
    /// <summary>
    /// Shows an open file dialog and returns the selected filename.
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    public static bool ShowOpenFileDialog(out string? filename)
    {
        OpenFileDialog dialog = new()
        {
            Filter = "Auto Clicker Files (*.clk)|*.clk|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            Title = "Open File"
        };

        if (dialog.ShowDialog() == true)
        {
            filename = dialog.FileName;
            return true;
        }

        filename = null;
        return false;
    }
    #endregion

    #region ShowSaveFileDialog
    /// <summary>
    /// Shows a save file dialog and returns the selected filename.
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    public static bool ShowSaveFileDialog(out string? filename)
    {
        SaveFileDialog dialog = new()
        {
            Filter = "Auto Clicker Files (*.clk)|*.clk|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            Title = "Save File"
        };

        if (dialog.ShowDialog() == true)
        {
            filename = dialog.FileName;
            return true;
        }

        filename = null;
        return false;
    }
    #endregion

    #region SimulateKeyDown
    /// <summary>
    /// Simulates a key down event for the specified key.
    /// </summary>
    /// <param name="key"></param>
    public static void SimulateKeyDown(Key key) =>
        SendKeyboardInput(key, true);
    #endregion

    #region SimulateKeyPress
    /// <summary>
    /// Simulates a key press (down and up) event for the specified key.
    /// </summary>
    /// <param name="key"></param>
    public static void SimulateKeyPress(Key key)
    {
        ushort virtualKey = (ushort)KeyInterop.VirtualKeyFromKey(key);
        Input[] inputs = new Input[2];

        inputs[0].Type = KeyboardInputTypeFlag;
        inputs[0].U.KI.WVk = virtualKey;
        inputs[0].U.KI.DwFlags = KeyDownEventFlag;

        inputs[1].Type = KeyboardInputTypeFlag;
        inputs[1].U.KI.WVk = virtualKey;
        inputs[1].U.KI.DwFlags = KeyUpEventFlag;

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input)));
    }
    #endregion

    #region SimulateKeyUp
    /// <summary>
    /// Simulates a key up event for the specified key.
    /// </summary>
    /// <param name="key"></param>
    public static void SimulateKeyUp(Key key) =>
        SendKeyboardInput(key, false);
    #endregion

    #region SimulateMouseClick
    /// <summary>
    /// Simulates a mouse click (down and up) event for the specified button.
    /// </summary>
    /// <param name="button"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static void SimulateMouseClick(MouseButton button)
    {
        (uint downFlag, uint upFlag) flags = button switch
        {
            MouseButton.Left => (MouseLeftDownEventFlag, MouseLeftUpEventFlag),
            MouseButton.Right => (MouseRightDownEventFlag, MouseRightUpEventFlag),
            MouseButton.Middle => (MouseMiddleDownEventFlag, MouseMiddleUpEventFlag),
            _ => throw new ArgumentOutOfRangeException(nameof(button), button, "Unsupported mouse button.")
        };

        SendMouseInputs(flags.downFlag, flags.upFlag);
    }
    #endregion

    #region SimulateMouseDoubleClick
    /// <summary>
    /// Simulates a mouse double click (down, up, down, up) event for the specified button.
    /// </summary>
    /// <param name="button"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static void SimulateMouseDoubleClick(MouseButton button)
    {
        (uint downFlag, uint upFlag) flags = button switch
        {
            MouseButton.Left => (MouseLeftDownEventFlag, MouseLeftUpEventFlag),
            MouseButton.Right => (MouseRightDownEventFlag, MouseRightUpEventFlag),
            MouseButton.Middle => (MouseMiddleDownEventFlag, MouseMiddleUpEventFlag),
            _ => throw new ArgumentOutOfRangeException(nameof(button), button, "Unsupported mouse button.")
        };

        SendMouseInputs(flags.downFlag, flags.upFlag, flags.downFlag, flags.upFlag);
    }
    #endregion

    #region SimulateMouseDown
    /// <summary>
    /// Simulates a mouse down event for the specified button.
    /// </summary>
    /// <param name="button"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static void SimulateMouseDown(MouseButton button)
    {
        uint flag = button switch
        {
            MouseButton.Left => MouseLeftDownEventFlag,
            MouseButton.Right => MouseRightDownEventFlag,
            MouseButton.Middle => MouseMiddleDownEventFlag,
            _ => throw new ArgumentOutOfRangeException(nameof(button), button, "Unsupported mouse button.")
        };

        SendMouseInputs(flag);
    }
    #endregion

    #region SimulateMouseUp
    /// <summary>
    /// Simulates a mouse up event for the specified button.
    /// </summary>
    /// <param name="button"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static void SimulateMouseUp(MouseButton button)
    {
        uint flag = button switch
        {
            MouseButton.Left => MouseLeftUpEventFlag,
            MouseButton.Right => MouseRightUpEventFlag,
            MouseButton.Middle => MouseMiddleUpEventFlag,
            _ => throw new ArgumentOutOfRangeException(nameof(button), button, "Unsupported mouse button.")
        };

        SendMouseInputs(flag);
    }
    #endregion

    #region SimulateMouseWheel
    /// <summary>
    /// Simulates a mouse wheel event with the specified number of notches.
    /// </summary>
    /// <param name="notches"></param>
    public static void SimulateMouseWheel(int notches) =>
        SendMouseInputWithData(MouseWheelEventFlag, (uint)(notches * WheelDelta));
    #endregion

    #region StartMonitoring
    /// <summary>
    /// Starts monitoring global keyboard and mouse events.
    /// </summary>
    /// <param name="callback"></param>
    /// <param name="captureMouseMovement"></param>
    /// <param name="mouseCapturePrecision"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public static void StartMonitoring(Action<CommandType, object, DateTime> callback, bool captureMouseMovement, TimeSpan mouseCapturePrecision)
    {
        if (_globalHook is not null)
            throw new InvalidOperationException("The monitoring is already started.");

        _globalHook = Hook.GlobalEvents();

        if (captureMouseMovement)
            _globalHook.MouseMoveExt += (_, e) =>
            {
                DateTime fetchTime = DateTime.Now;
                if (fetchTime - _lastMousePositionTime < mouseCapturePrecision)
                    return;

                _lastMousePositionTime = fetchTime;
                    callback?.Invoke(CommandType.MouseMove, new Point(e.Location.X, e.Location.Y), fetchTime);
            };

        _globalHook.KeyDown += (_, e) => callback?.Invoke(CommandType.KeyDown, KeyInterop.KeyFromVirtualKey(e.KeyValue), DateTime.Now);
        _globalHook.KeyUp += (_, e) => callback?.Invoke(CommandType.KeyUp, KeyInterop.KeyFromVirtualKey(e.KeyValue), DateTime.Now);
        _globalHook.MouseDownExt += (_, e) => callback?.Invoke(GetMouseButton(e.Button, true), new Point(e.Location.X, e.Location.Y), DateTime.Now);
        _globalHook.MouseUpExt += (_, e) => callback?.Invoke(GetMouseButton(e.Button, false), new Point(e.Location.X, e.Location.Y), DateTime.Now);
        _globalHook.MouseWheelExt += (_, e) => callback?.Invoke(CommandType.MouseWheel, e.Delta, DateTime.Now);

        #region Local functions

        // Gets the CommandType for the specified mouse button and state.
        CommandType GetMouseButton(MouseButtons button, bool isDown) =>
            button switch
            {
                MouseButtons.Left => isDown ? CommandType.LeftButtonDown : CommandType.LeftButtonUp,
                MouseButtons.Right => isDown ? CommandType.RightButtonDown : CommandType.RightButtonUp,
                MouseButtons.Middle => isDown ? CommandType.MiddleButtonDown : CommandType.MiddleButtonUp,
                _ => CommandType.Comment
            };

        #endregion
    }
    #endregion

    #region StartMonitoringKey
    /// <summary>
    /// Starts monitoring for a specific key press globally.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="callback"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public static void StartMonitoringKey(Key key, Action callback)
    {
        if (_globalHook is not null)
            throw new InvalidOperationException("The monitoring is already started.");

        _globalHook = Hook.GlobalEvents();

        _globalHook.KeyDown += (_, e) =>
        {
            if (KeyInterop.KeyFromVirtualKey(e.KeyValue) == key)
            {
                StopMonitoring();
                callback?.Invoke();
            }
        };
    }
    #endregion

    #region StopMonitoring
    /// <summary>
    /// Stops monitoring global keyboard and mouse events.
    /// </summary>
    public static void StopMonitoring()
    {
        _globalHook?.Dispose();
        _globalHook = null;
    }
    #endregion

    #endregion

    #region Private Methods

    private static void SendKeyboardInput(Key key, bool isDown)
    {
        Input[] inputs = new Input[1];

        inputs[0].Type = KeyboardInputTypeFlag;
        inputs[0].U.KI.WVk = (ushort)KeyInterop.VirtualKeyFromKey(key);
        inputs[0].U.KI.DwFlags = isDown ? KeyDownEventFlag : KeyUpEventFlag;

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input)));
    }

    private static void SendMouseInputs(params uint[] inputs)
    {
        Input[] inputStructs = new Input[inputs.Length];

        for (int i = 0; i < inputs.Length; i++)
        {
            inputStructs[i].Type = MouseInputTypeFlag;
            inputStructs[i].U.MI.DwFlags = inputs[i];
        }

        SendInput((uint)inputStructs.Length, inputStructs, Marshal.SizeOf(typeof(Input)));
    }

    private static void SendMouseInputWithData(uint flag, uint data)
    {
        Input[] inputs = new Input[1];

        inputs[0].Type = MouseInputTypeFlag;
        inputs[0].U.MI.DwFlags = flag;
        inputs[0].U.MI.MouseData = data;

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(Input)));
    }

    #endregion
}