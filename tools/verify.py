# -*- coding: utf-8 -*-
"""实际运行 FlatCat.exe，取证窗口样式与截图（只用 ctypes，无第三方依赖）。"""
import ctypes, subprocess, time, os, sys, struct

user32 = ctypes.WinDLL("user32", use_last_error=True)
gdi32 = ctypes.WinDLL("gdi32", use_last_error=True)

WNDENUMPROC = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.c_void_p, ctypes.c_void_p)
user32.EnumWindows.argtypes = [WNDENUMPROC, ctypes.c_void_p]
user32.IsWindowVisible.argtypes = [ctypes.c_void_p]
user32.GetWindowThreadProcessId.argtypes = [ctypes.c_void_p, ctypes.POINTER(ctypes.c_ulong)]
user32.GetWindowLongW.argtypes = [ctypes.c_void_p, ctypes.c_int]
user32.GetWindowLongW.restype = ctypes.c_int


class RECT(ctypes.Structure):
    _fields_ = [("left", ctypes.c_long), ("top", ctypes.c_long),
                ("right", ctypes.c_long), ("bottom", ctypes.c_long)]


class BITMAPINFOHEADER(ctypes.Structure):
    _fields_ = [("biSize", ctypes.c_ulong), ("biWidth", ctypes.c_long), ("biHeight", ctypes.c_long),
                ("biPlanes", ctypes.c_ushort), ("biBitCount", ctypes.c_ushort),
                ("biCompression", ctypes.c_ulong), ("biSizeImage", ctypes.c_ulong),
                ("biXPelsPerMeter", ctypes.c_long), ("biYPelsPerMeter", ctypes.c_long),
                ("biClrUsed", ctypes.c_ulong), ("biClrImportant", ctypes.c_ulong)]


def find_window(pid):
    hits = []

    def cb(hwnd, lparam):
        p = ctypes.c_ulong()
        user32.GetWindowThreadProcessId(hwnd, ctypes.byref(p))
        if p.value == pid and user32.IsWindowVisible(hwnd):
            hits.append(hwnd)
        return True

    user32.EnumWindows(WNDENUMPROC(cb), None)
    return hits[0] if hits else None


def grab(hwnd, path):
    r = RECT()
    user32.GetWindowRect(hwnd, ctypes.byref(r))
    w, h = r.right - r.left, r.bottom - r.top
    hdc = user32.GetDC(None)
    mdc = gdi32.CreateCompatibleDC(hdc)
    bmp = gdi32.CreateCompatibleBitmap(hdc, w, h)
    gdi32.SelectObject(mdc, bmp)
    gdi32.BitBlt(mdc, 0, 0, w, h, hdc, r.left, r.top, 0x00CC0020)

    bi = BITMAPINFOHEADER()
    bi.biSize = ctypes.sizeof(BITMAPINFOHEADER)
    bi.biWidth = w
    bi.biHeight = h
    bi.biPlanes = 1
    bi.biBitCount = 32
    bi.biCompression = 0

    stride = w * 4
    buf = ctypes.create_string_buffer(stride * h)
    gdi32.GetDIBits(mdc, bmp, 0, h, buf, ctypes.byref(bi), 0)

    datasize = stride * h
    with open(path, "wb") as f:
        f.write(b"BM")
        f.write(struct.pack("<IHHI", 14 + 40 + datasize, 0, 0, 14 + 40))
        f.write(struct.pack("<IiiHHIIiiII", 40, w, h, 1, 32, 0, datasize, 2835, 2835, 0, 0))
        f.write(buf.raw)

    gdi32.DeleteObject(bmp)
    gdi32.DeleteDC(mdc)
    user32.ReleaseDC(None, hdc)
    return w, h


def main():
    exe, outdir = sys.argv[1], sys.argv[2]
    lines = []

    proc = subprocess.Popen([exe])
    time.sleep(3.0)
    alive = proc.poll() is None
    lines.append("pid = %d" % proc.pid)
    lines.append("alive_after_3s = %s" % alive)

    if alive:
        h = find_window(proc.pid)
        lines.append("window_handle = %s" % h)
        if h:
            ex = user32.GetWindowLongW(h, -20) & 0xFFFFFFFF
            lines.append("ex_style = 0x%08X" % ex)
            lines.append("  WS_EX_LAYERED    = %s" % bool(ex & 0x00080000))
            lines.append("  WS_EX_TOOLWINDOW = %s" % bool(ex & 0x00000080))
            lines.append("  WS_EX_NOACTIVATE = %s" % bool(ex & 0x08000000))
            lines.append("  WS_EX_TRANSPARENT= %s" % bool(ex & 0x00000020))
            r = RECT()
            user32.GetWindowRect(h, ctypes.byref(r))
            lines.append("rect = %d,%d %dx%d" % (r.left, r.top, r.right - r.left, r.bottom - r.top))
            try:
                w, hh = grab(h, os.path.join(outdir, "window.bmp"))
                lines.append("screenshot = ok (%dx%d)" % (w, hh))
            except Exception as e:
                lines.append("screenshot = FAIL %s" % e)
        else:
            lines.append("window_handle = None")

        # 单实例：第二个进程应立刻退出
        p2 = subprocess.Popen([exe])
        time.sleep(2.0)
        lines.append("second_instance_still_running = %s" % (p2.poll() is None))
        if p2.poll() is None:
            p2.kill()

        proc.kill()
        time.sleep(0.6)
        lines.append("killed = %s" % (proc.poll() is not None))

    text = "\n".join(lines)
    with open(os.path.join(outdir, "verify.txt"), "w", encoding="utf-8") as f:
        f.write(text)
    print(text)


main()