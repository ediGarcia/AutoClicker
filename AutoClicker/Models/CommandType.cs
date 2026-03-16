namespace AutoClicker.Models;

public enum CommandType
{
    MouseMove,
    MouseWheel,
    LeftButtonClick,
    LeftButtonDoubleClick,
    LeftButtonDown,
    LeftButtonUp,
    RightButtonClick,
    RightButtonDoubleClick,
    RightButtonDown,
    RightButtonUp,
    MiddleButtonClick,
    MiddleButtonDoubleClick,
    MiddleButtonDown,
    MiddleButtonUp,
    KeyPress,
    KeyDown,
    KeyUp,
    StartRepeater,
    EndRepeater,
    Wait,
    Comment,
    Empty
}