using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace OpenShot;

/// <summary>
/// Registers global hotkeys (Win+Shift+S, etc.) and dispatches capture
/// actions via events. Must be initialized with a Window handle.
/// </summary>
public sealed class HotkeyService : IDisposable
{
    private readonly Window _window;
    private HwndSource? _source;
    private bool _registered;

    // Hotkey IDs
    public const int HotkeyCaptureId = 1;
    public const int HotkeyActiveWindowId = 2;

    // Modifier constants
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;

    // Default hotkeys
    public uint CaptureModifiers { get; set; } = MOD_WIN | MOD_SHIFT;
    public uint CaptureKey { get; set; } = 0x53; // S
    public uint ActiveWindowModifiers { get; set; } = MOD_WIN | MOD_SHIFT;
    public uint ActiveWindowKey { get; set; } = 0x57; // W

    public event Action? CaptureFullScreenRequested;
    public event Action? CaptureActiveWindowRequested;

    public HotkeyService(Window window)
    {
        _window = window;
    }

    public void Register()
    {
        if (_registered) return;
        _source = PresentationSource.FromVisual(_window) as HwndSource;
        if (_source == null)
        {
            _window.Loaded += OnWindowLoaded;
            return;
        }
        DoRegister();
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        _window.Loaded -= OnWindowLoaded;
        _source = PresentationSource.FromVisual(_window) as HwndSource;
        DoRegister();
    }

    private void DoRegister()
    {
        if (_source == null) return;
        _source.AddHook(WndProc);
        RegisterHotkey(HotkeyCaptureId, CaptureModifiers, CaptureKey);
        RegisterHotkey(HotkeyActiveWindowId, ActiveWindowModifiers, ActiveWindowKey);
        _registered = true;
    }

    private void RegisterHotkey(int id, uint modifiers, uint key)
    {
        if (_source == null) return;
        NativeMethods.RegisterHotKey(_source.Handle, id, modifiers, key);
    }

    private void UnregisterHotkey(int id)
    {
        if (_source == null) return;
        NativeMethods.UnregisterHotKey(_source.Handle, id);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_HOTKEY = 0x0312;
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            switch (id)
            {
                case HotkeyCaptureId:
                    CaptureFullScreenRequested?.Invoke();
                    handled = true;
                    break;
                case HotkeyActiveWindowId:
                    CaptureActiveWindowRequested?.Invoke();
                    handled = true;
                    break;
            }
        }
        return IntPtr.Zero;
    }

    public void Unregister()
    {
        if (!_registered || _source == null) return;
        _source.RemoveHook(WndProc);
        UnregisterHotkey(HotkeyCaptureId);
        UnregisterHotkey(HotkeyActiveWindowId);
        _registered = false;
    }

    public void Dispose()
    {
        Unregister();
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
