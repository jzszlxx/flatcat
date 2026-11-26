# -*- coding: utf-8 -*-
"""按窗口标题找到窗口，用 PrintWindow 抓内容并存 BMP（非交互桌面下 BitBlt 会全黑）。"""
import ctypes, struct, sys

user32 = ctypes.WinDLL("user32", use_last_error=True)
gdi32 = ctypes.WinDLL("gdi32", use_last_error=True)
WNDENUMPROC = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.c_void_p, ctypes.c_void_p)
user32.EnumWindows.argtypes = [WNDENUMPROC, ctypes.c_void_p]


class RECT(ctypes.Structure):
    _fields_ = [("left", ctypes.c_long), ("top", ctypes.c_long),
                ("right", ctypes.c_long), ("bottom", ctypes.c_long)]


class BIH(ctypes.Structure):
    _fields_ = [("biSize", ctypes.c_ulong), ("biWidth", ctypes.c_long), ("biHeight", ctypes.c_long),
                ("biPlanes", ctypes.c_ushort), ("biBitCount", ctypes.c_ushort),
                ("biCompression", ctypes.c_ulong), ("biSizeImage", ctypes.c_ulong),
                ("biXPelsPerMeter", ctypes.c_long), ("biYPelsPerMeter", ctypes.c_long),
                ("biClrUsed", ctypes.c_ulong), ("biClrImportant", ctypes.c_ulong)]


def find(title):
    hits = []

    def cb(h, l):
        n = user32.GetWindowTextLengthW(h)
        if n:
            buf = ctypes.create_unicode_buffer(n + 1)
            user32.GetWindowTextW(h, buf, n + 1)
            if title in buf.value and user32.IsWindowVisible(h):
                hits.append((h, buf.value))
        return True

    user32.EnumWindows(WNDENUMPROC(cb), None)
    return hits


def grab(hwnd, path):
    r = RECT()
    user32.GetWindowRect(hwnd, ctypes.byref(r))
    w, h = r.right - r.left, r.bottom - r.top
    hdc = user32.GetDC(hwnd)
    mdc = gdi32.CreateCompatibleDC(hdc)
    bmp = gdi32.CreateCompatibleBitmap(hdc, w, h)
    old = gdi32.SelectObject(mdc, bmp)
    ok = user32.PrintWindow(hwnd, mdc, 0)

    bi = BIH()
    bi.biSize = ctypes.sizeof(BIH)
    bi.biWidth = w
    bi.biHeight = h
    bi.biPlanes = 1
    bi.biBitCount = 32
    bi.biCompression = 0

    stride = w * 4
    buf = ctypes.create_string_buffer(stride * h)
    gdi32.GetDIBits(mdc, bmp, 0, h, buf, ctypes.byref(bi), 0)

    data = buf.raw
    with open(path, "wb") as f:
        f.write(b"BM")
        f.write(struct.pack("<IHHI", 14 + 40 + len(data), 0, 0, 14 + 40))
        f.write(struct.pack("<IiiHHIIiiII", 40, w, h, 1, 32, 0, len(data), 2835, 2835, 0, 0))
        f.write(data)

    gdi32.SelectObject(mdc, old)
    gdi32.DeleteObject(bmp)
    gdi32.DeleteDC(mdc)
    user32.ReleaseDC(hwnd, hdc)
    return w, h, ok


if __name__ == "__main__":
    title, out = sys.argv[1], sys.argv[2]
    hits = find(title)
    print("windows:", [(h, t) for h, t in hits])
    if not hits:
        sys.exit(1)
    print("grab:", grab(hits[0][0], out))