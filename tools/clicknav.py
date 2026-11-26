# -*- coding: utf-8 -*-
"""模拟用户在设置窗口左侧导航上点一下，然后抓整窗图。

用法：
    python tools/clicknav.py <窗口标题> <第几项,从0起> <输出bmp>

不需要交互桌面：直接把 WM_LBUTTONDOWN / WM_LBUTTONUP 投给 ListBox 子窗口。
"""
import ctypes
import struct
import sys
from ctypes import wintypes

user32 = ctypes.WinDLL("user32", use_last_error=True)
gdi32 = ctypes.WinDLL("gdi32", use_last_error=True)

WNDENUMPROC = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.c_void_p, ctypes.c_void_p)
user32.EnumWindows.argtypes = [WNDENUMPROC, ctypes.c_void_p]
user32.EnumChildWindows.argtypes = [ctypes.c_void_p, WNDENUMPROC, ctypes.c_void_p]

WM_LBUTTONDOWN = 0x0201
WM_LBUTTONUP = 0x0202


class RECT(ctypes.Structure):
    _fields_ = [("left", ctypes.c_long), ("top", ctypes.c_long),
                ("right", ctypes.c_long), ("bottom", ctypes.c_long)]


class BIH(ctypes.Structure):
    _fields_ = [("biSize", ctypes.c_ulong), ("biWidth", ctypes.c_long), ("biHeight", ctypes.c_long),
                ("biPlanes", ctypes.c_ushort), ("biBitCount", ctypes.c_ushort),
                ("biCompression", ctypes.c_ulong), ("biSizeImage", ctypes.c_ulong),
                ("biXPelsPerMeter", ctypes.c_long), ("biYPelsPerMeter", ctypes.c_long),
                ("biClrUsed", ctypes.c_ulong), ("biClrImportant", ctypes.c_ulong)]


def text(h):
    n = user32.GetWindowTextLengthW(h)
    buf = ctypes.create_unicode_buffer(n + 1)
    user32.GetWindowTextW(h, buf, n + 1)
    return buf.value


def cls(h):
    buf = ctypes.create_unicode_buffer(256)
    user32.GetClassNameW(h, buf, 256)
    return buf.value


def find_top(title):
    hits = []

    def cb(h, l):
        if user32.IsWindowVisible(h) and title in text(h):
            hits.append(h)
        return True

    user32.EnumWindows(WNDENUMPROC(cb), None)
    return hits[0] if hits else None


def children(h):
    out = []

    def cb(c, l):
        out.append(c)
        return True

    user32.EnumChildWindows(h, WNDENUMPROC(cb), None)
    return out


def grab(hwnd, path):
    r = RECT()
    user32.GetWindowRect(hwnd, ctypes.byref(r))
    w, h = r.right - r.left, r.bottom - r.top
    hdc = user32.GetWindowDC(hwnd)
    mdc = gdi32.CreateCompatibleDC(hdc)
    bmp = gdi32.CreateCompatibleBitmap(hdc, w, h)
    gdi32.SelectObject(mdc, bmp)
    ok = user32.SendMessageW(hwnd, 0x0317, mdc, 0x00000002)   # WM_PRINT | PRF_CHILDREN
    bmi = BIH()
    bmi.biSize = 40
    bmi.biWidth = w
    bmi.biHeight = h
    bmi.biPlanes = 1
    bmi.biBitCount = 32
    need = w * h * 4
    buf = (ctypes.c_char * need)()
    gdi32.GetDIBits(mdc, bmp, 0, h, buf, ctypes.byref(bmi), 0)
    data = bytearray(buf)
    # 转成 BMP 的 BLUE 顺序（GetDIBits 已是 BGRA）
    with open(path, "wb") as f:
        f.write(b"BM")
        f.write(struct.pack("<IHHI", 14 + 40 + len(data), 0, 0, 14 + 40))
        f.write(struct.pack("<IiiHHIIiiII", 40, w, h, 1, 32, 0, len(data), 2835, 2835, 0, 0))
        f.write(data)
    gdi32.SelectObject(mdc, bmp)
    gdi32.DeleteObject(bmp)
    gdi32.DeleteDC(mdc)
    user32.ReleaseDC(hwnd, hdc)
    return w, h, ok


def main():
    title = sys.argv[1]
    idx = int(sys.argv[2])
    out = sys.argv[3]
    top = find_top(title)
    if not top:
        print("找不到窗口:", title)
        return 1
    kids = children(top)
    print("子窗口:", [(h, cls(h), text(h)[:12]) for h in kids][:12])
    lbs = [h for h in kids if "LISTBOX" in cls(h).upper()]
    print("ListBox:", lbs)
    if not lbs:
        print("没有 ListBox，导航控件可能不是 ListBox")
        return 2
    lb = lbs[0]
    r = RECT()
    user32.GetWindowRect(lb, ctypes.byref(r))
    print("ListBox 屏幕矩形:", "(%d,%d)-(%d,%d)" % (r.left, r.top, r.right, r.bottom))
    # ListBox 在导航区里从窗口客户区 y=46 开始，每项 42 高
    x, y = 95, 42 * idx + 21
    lp = (y << 16) | x
    user32.SendMessageW(lb, WM_LBUTTONDOWN, 1, lp)
    user32.SendMessageW(lb, WM_LBUTTONUP, 0, lp)
    print("已点击第 %d 项，客户坐标 (%d,%d)" % (idx, x, y))
    print("grab:", grab(top, out))
    return 0


if __name__ == "__main__":
    sys.exit(main())