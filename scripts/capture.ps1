# Captures the Tokenbar flyout (or the whole primary screen) to a PNG for visual checks.
param([string]$Out = (Join-Path (Split-Path -Parent $PSScriptRoot) 'docs\screenshots\flyout.png'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class CaptureNative {
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
    [DllImport("user32.dll")] public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr ctx);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindowW(string cls, string title);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] public static extern int GetSystemMetrics(int i);
}
'@
[void][CaptureNative]::SetThreadDpiAwarenessContext([IntPtr](-4))

$h = [CaptureNative]::FindWindowW('WinUIDesktopWin32WindowClass', 'Tokenbar')
if ($h -ne [IntPtr]::Zero -and [CaptureNative]::IsWindowVisible($h)) {
    $r = New-Object CaptureNative+RECT
    [void][CaptureNative]::GetWindowRect($h, [ref]$r)
    $pad = 24
    $x = $r.L - $pad; $y = $r.T - $pad; $w = ($r.R - $r.L) + 2 * $pad; $hgt = ($r.B - $r.T) + 2 * $pad
} else {
    $x = 0; $y = 0; $w = [CaptureNative]::GetSystemMetrics(0); $hgt = [CaptureNative]::GetSystemMetrics(1)
}

New-Item -ItemType Directory -Force (Split-Path -Parent $Out) | Out-Null
$bmp = New-Object System.Drawing.Bitmap $w, $hgt
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($x, $y, 0, 0, $bmp.Size)
$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()
Write-Output $Out
