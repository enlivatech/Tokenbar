using System.Runtime.InteropServices;
using Tokenbar.Interop;

namespace Tokenbar.Tray;

internal sealed class TrayIcon : IDisposable
{
    private const int CallbackMessage = Native.WM_USER + 100;
    private const int IconId = 1;

    private readonly Native.WndProc _wndProc;
    private readonly uint _taskbarCreatedMessage;
    private readonly nint _hwnd;
    private nint _hIcon;
    private string _tooltip = "Tokenbar";
    private bool _added;

    public event Action? LeftClicked;
    public event Action? RightClicked;
    public event Action? DisplaySettingsChanged;

    public TrayIcon()
    {
        _wndProc = WndProc;
        _taskbarCreatedMessage = Native.RegisterWindowMessageW("TaskbarCreated");
        var className = "TokenbarTrayWindow";
        var wc = new Native.WNDCLASSEXW
        {
            cbSize = Marshal.SizeOf<Native.WNDCLASSEXW>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
            hInstance = Native.GetModuleHandleW(null),
            lpszClassName = className,
        };
        Native.RegisterClassExW(ref wc);
        // A top-level (hidden) window, not message-only, so it receives WM_SETTINGCHANGE broadcasts.
        _hwnd = Native.CreateWindowExW(0, className, "Tokenbar", 0, 0, 0, 0, 0, 0, 0, wc.hInstance, 0);
    }

    public nint Handle => _hwnd;

    public void Update(nint hIcon, string tooltip)
    {
        var old = _hIcon;
        _hIcon = hIcon;
        _tooltip = tooltip.Length > 127 ? tooltip[..127] : tooltip;
        var data = CreateData();
        if (!_added)
        {
            _added = Native.Shell_NotifyIconW(Native.NIM_ADD, ref data);
            data.uVersion = Native.NOTIFYICON_VERSION_4;
            Native.Shell_NotifyIconW(Native.NIM_SETVERSION, ref data);
        }
        else
        {
            Native.Shell_NotifyIconW(Native.NIM_MODIFY, ref data);
        }
        if (old != 0) Native.DestroyIcon(old);
    }

    /// <summary>Screen rectangle of the icon in physical pixels, if the shell reports it.</summary>
    public Native.RECT? GetIconRect()
    {
        var id = new Native.NOTIFYICONIDENTIFIER
        {
            cbSize = Marshal.SizeOf<Native.NOTIFYICONIDENTIFIER>(),
            hWnd = _hwnd,
            uID = IconId,
        };
        return Native.Shell_NotifyIconGetRect(ref id, out var rect) == 0 ? rect : null;
    }

    private Native.NOTIFYICONDATAW CreateData() => new()
    {
        cbSize = Marshal.SizeOf<Native.NOTIFYICONDATAW>(),
        hWnd = _hwnd,
        uID = IconId,
        uFlags = Native.NIF_MESSAGE | Native.NIF_ICON | Native.NIF_TIP | Native.NIF_SHOWTIP,
        uCallbackMessage = CallbackMessage,
        hIcon = _hIcon,
        szTip = _tooltip,
        szInfo = string.Empty,
        szInfoTitle = string.Empty,
    };

    private nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        if (msg == CallbackMessage)
        {
            int evt = (int)(lParam & 0xFFFF);
            switch (evt)
            {
                // NOTIFYICON_VERSION_4 reports clicks as NIN_SELECT / WM_CONTEXTMENU; the raw
                // WM_*BUTTONUP messages arrive as well and must not trigger a second toggle.
                case Native.NIN_SELECT:
                case Native.NIN_KEYSELECT:
                    LeftClicked?.Invoke();
                    break;
                case Native.WM_CONTEXTMENU:
                    RightClicked?.Invoke();
                    break;
            }
            return 0;
        }
        if (msg == _taskbarCreatedMessage)
        {
            _added = false;
            Update(_hIcon, _tooltip);
            return 0;
        }
        if (msg is Native.WM_SETTINGCHANGE or Native.WM_DPICHANGED)
        {
            DisplaySettingsChanged?.Invoke();
        }
        return Native.DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_added)
        {
            var data = CreateData();
            Native.Shell_NotifyIconW(Native.NIM_DELETE, ref data);
            _added = false;
        }
        if (_hIcon != 0) Native.DestroyIcon(_hIcon);
        Native.DestroyWindow(_hwnd);
    }
}
