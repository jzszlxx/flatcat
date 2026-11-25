param(
  [string]$Exe,
  [string]$OutDir
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

Add-Type @"
using System;
using System.Text;
using System.Runtime.InteropServices;

public class W
{
    [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr h, int i);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] public static extern bool GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] public static extern int GetWindowTextLength(IntPtr h);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);

    private delegate bool EnumProc(IntPtr h, IntPtr l);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumProc cb, IntPtr l);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    public static IntPtr FindByPid(uint pid, out string title)
    {
        IntPtr found = IntPtr.Zero;
        string t = "";
        EnumWindows(delegate(IntPtr h, IntPtr l)
        {
            uint wp;
            GetWindowThreadProcessId(h, out wp);
            if (wp == pid && IsWindowVisible(h))
            {
                int n = GetWindowTextLength(h);
                StringBuilder sb = new StringBuilder(n + 2);
                GetWindowText(h, sb, sb.Capacity);
                found = h;
                t = sb.ToString();
                return false;
            }
            return true;
        }, IntPtr.Zero);
        title = t;
        return found;
    }
}
"@

$lines = New-Object System.Collections.ArrayList
function Add-Line([string]$s) { [void]$lines.Add($s) }

$p = Start-Process -FilePath $Exe -PassThru
Start-Sleep -Seconds 3
$p.Refresh()
Add-Line ("pid = " + $p.Id)
Add-Line ("hasExited = " + $p.HasExited)

if (-not $p.HasExited) {
    $title = ""
    $h = [W]::FindByPid([uint32]$p.Id, [ref]$title)
    Add-Line ("windowHandle = " + $h)
    Add-Line ("windowTitle = " + $title)

    if ($h -ne [IntPtr]::Zero) {
        $ex = [W]::GetWindowLong($h, -20)
        Add-Line ("exStyle = 0x{0:X8}" -f $ex)
        Add-Line ("  layered    = " + [bool]($ex -band 0x00080000))
        Add-Line ("  toolwindow = " + [bool]($ex -band 0x00000080))
        Add-Line ("  noactivate = " + [bool]($ex -band 0x08000000))
        Add-Line ("  transparent= " + [bool]($ex -band 0x00000020))
        Add-Line ("visible = " + [W]::IsWindowVisible($h))

        $r = New-Object W+RECT
        [void][W]::GetWindowRect($h, [ref]$r)
        $w = $r.Right - $r.Left
        $hh = $r.Bottom - $r.Top
        Add-Line ("rect = {0},{1} {2}x{3}" -f $r.Left, $r.Top, $w, $hh)

        try {
            $bmp = New-Object System.Drawing.Bitmap $w, $hh
            $g = [System.Drawing.Graphics]::FromImage($bmp)
            $g.CopyFromScreen($r.Left, $r.Top, 0, 0, (New-Object System.Drawing.Size $w, $hh))
            $bmp.Save((Join-Path $OutDir "window.png"), [System.Drawing.Imaging.ImageFormat]::Png)
            $g.Dispose()
            $bmp.Dispose()
            Add-Line "screenshot = ok"
        } catch {
            Add-Line ("screenshot = FAIL " + $_.Exception.Message)
        }
    }
}

# 单实例：再起一个应当立刻退出
try {
    $p2 = Start-Process -FilePath $Exe -PassThru
    Start-Sleep -Seconds 2
    $p2.Refresh()
    Add-Line ("secondInstance.exited = " + $p2.HasExited)
    if (-not $p2.HasExited) { $p2.Kill() }
} catch {
    Add-Line ("secondInstance = FAIL " + $_.Exception.Message)
}

if (-not $p.HasExited) { $p.Kill() }
Start-Sleep -Milliseconds 500
$p.Refresh()
Add-Line ("killed = " + $p.HasExited)

$lines | Set-Content -Path (Join-Path $OutDir "verify.txt") -Encoding UTF8