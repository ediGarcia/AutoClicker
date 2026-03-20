using AutoClicker.Messages;
using AutoClicker.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;

namespace AutoClicker.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        CbxStopKey.ItemsSource = new[]
        {
            Key.D0,
            Key.D1,
            Key.D2,
            Key.D3,
            Key.D4,
            Key.D5,
            Key.D6,
            Key.D7,
            Key.D8,
            Key.D9,
            Key.A,
            Key.Add,
            Key.Apps,
            Key.B,
            Key.Back,
            Key.C,
            Key.CapsLock,
            Key.D,
            Key.Delete,
            Key.Divide,
            Key.Down,
            Key.E,
            Key.End,
            Key.Enter,
            Key.Escape,
            Key.F,
            Key.F1,
            Key.F10,
            Key.F11,
            Key.F12,
            Key.F13,
            Key.F14,
            Key.F15,
            Key.F16,
            Key.F17,
            Key.F18,
            Key.F19,
            Key.F2,
            Key.F20,
            Key.F21,
            Key.F22,
            Key.F23,
            Key.F24,
            Key.F3,
            Key.F4,
            Key.F5,
            Key.F6,
            Key.F7,
            Key.F8,
            Key.F9,
            Key.G,
            Key.H,
            Key.Home,
            Key.I,
            Key.Insert,
            Key.J,
            Key.K,
            Key.L,
            Key.LWin,
            Key.Left,
            Key.LeftAlt,
            Key.LeftCtrl,
            Key.LeftShift,
            Key.M,
            Key.Multiply,
            Key.N,
            Key.NumLock,
            Key.NumPad0,
            Key.NumPad1,
            Key.NumPad2,
            Key.NumPad3,
            Key.NumPad4,
            Key.NumPad5,
            Key.NumPad6,
            Key.NumPad7,
            Key.NumPad8,
            Key.NumPad9,
            Key.O,
            Key.P,
            Key.PageDown,
            Key.PageUp,
            Key.PrintScreen,
            Key.Q,
            Key.R,
            Key.RWin,
            Key.Right,
            Key.RightAlt,
            Key.RightCtrl,
            Key.RightShift,
            Key.S,
            Key.Scroll,
            Key.Space,
            Key.Subtract,
            Key.T,
            Key.Tab,
            Key.U,
            Key.Up,
            Key.V,
            Key.W,
            Key.X,
            Key.Y,
            Key.Z
        };

        WeakReferenceMessenger.Default.Register<ErrorMessage>(this, async (_, message) =>
            await new ContentDialog(CtpPopup)
            {
                CloseButtonText = "Ok",
                CloseButtonAppearance = ControlAppearance.Primary,
                Content = message.Message,
                Title = message.Title,
                UseLayoutRounding = true,
                VerticalContentAlignment = VerticalAlignment.Center
            }.ShowAsync()
        );

        WeakReferenceMessenger.Default.Register<SaveFileMessage>(this, async (_, message) =>
        {
            message.Reply(await ShowUnsavedChangesPopup() switch
            {
                ContentDialogResult.Primary => true,
                ContentDialogResult.Secondary => false,
                _ => null
            });
        });
    }

    #region Events

    #region MainWindow_OnClosing
    /// <summary>
    /// Checks for unsaved changes or active recording before closing the window.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    // ReSharper disable once AsyncVoidEventHandlerMethod
    private async void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        MainViewModel dataContext = (MainViewModel)DataContext;

        if (dataContext.IsRecording)
        {
            e.Cancel = true;

            ContentDialog dialog = new(CtpPopup)
            {
                CloseButtonAppearance = ControlAppearance.Primary,
                CloseButtonText = "No",
                Content = "Stop recording and leave?",
                PrimaryButtonAppearance = ControlAppearance.Secondary,
                PrimaryButtonText = "Yes",
                Title = "Recording in progress",
                UseLayoutRounding = true,
                VerticalContentAlignment = VerticalAlignment.Center
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                dataContext.StopMonitoringCommand.Execute(null);
            else
                return;
        }

        if (dataContext.HasChanges)
        {
            e.Cancel = true;

            switch (await ShowUnsavedChangesPopup())
            {
                case ContentDialogResult.Primary:
                {
                    dataContext.SaveFileCommand.Execute(null);

                    if (dataContext.HasChanges)
                        return;
                    break;
                }

                case ContentDialogResult.None:
                    return;
            }
        }

        // Forces closing without re-entering this event.
        if (e.Cancel)
        {
            Closing -= MainWindow_OnClosing;
            Close();
        }
    }
    #endregion

    #endregion

    #region Private Methods

    #region ShowUnsavedChangesPopup
    /// <summary>
    /// Shows a popup asking the user to save unsaved changes.
    /// </summary>
    /// <returns></returns>
    private async Task<ContentDialogResult> ShowUnsavedChangesPopup() =>
    await new ContentDialog(CtpPopup)
    {
        CloseButtonText = "Cancel",
        Content = "Save changes to the current file (unsaved changes will be lost)?",
        PrimaryButtonText = "Save",
        SecondaryButtonText = "Don't save",
        Title = "Unsaved changes",
        UseLayoutRounding = true,
        VerticalContentAlignment = VerticalAlignment.Center
    }.ShowAsync();
    #endregion

    #endregion
}