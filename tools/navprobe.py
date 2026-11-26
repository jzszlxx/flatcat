# -*- coding: utf-8 -*-
"""量设置窗口的导航状态：点击前后 ListBox 选中项、TabControl 当前页、导航项文本。

比截图可靠：直接问控件自己（LB_GETCURSEL / TCM_GETCURSEL / LB_GETTEXT）。
用法：python tools/navprobe.py <窗口标题> <第几项> <输出bmp>
"""
import ctypes
import struct
import sys

user32 = ctypes.WinDLL("user32", use_last_error=True)
gdi32 = ctypes.WinDLL("gdi32", use_last_error=True)
WNDENUMPROC = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.c_void_p, ctypes.c_void_p)
user32.EnumWindows.argtypes = [WNDENUMPROC, ctypes.c_void_p]
user32.EnumChildWindows.argtypes = [ctypes.c_void_p, WNDENUMPROC, ctypes.c_void_p]

LB_GETCOUNT = 0x018B
LB_GETCURSEL = 0x0188
LB_GETTEXT = 0x0189
LB_GETTEXTLEN = 0x018A
TCM_GETCURSEL = 0x130B
TCM_GETITEMCOUNT = 0x1304
WM_LBUTTONDOWN = 0x0201
WM_LBUTTONUP = 0x0202
WM_PRINT = 0x0317
PRF_CLIENT = 0x04
PRF_ERASEBKGND = 0x08
PRF_CHILDREN = 0x10


class RECT(ctypes.Structure):
    _fields_ = [("left", ctypes.c_long), ("top", ctypes.c_long),
                ("right", ctypes.c_long), ("bottom", ctypes.c_long)]


class BIH(ctypes.Structure):
    _fields_ = [("biSize", ctypes.c_ulong), ("biWidth", ctypes.c_long), ("biHeight", ctypes.c_long),
                ("biPlanes", ctypes.c_ushort), ("biBitCount", ctypes.c_ushort),
                ("biCompression", ctypes.c_ulong), ("biSizeImage", ctypes.c_ulong),
                ("biXPelsPerMeter", ctypes.c_long), ("biYPelsPerMeter", ctypes.c_long),
                ("biClrUsed", ctypes.c_ulong), ("biClrImportant", ctypes.c_ulong)]


def wtext(h):
    n = user32.GetWindowTextLengthW(h)
    buf = ctypes.create_unicode_buffer(n + 1)
    user32.GetWindowTextW(h, buf, n + 1)
    return buf.value


def wclass(h):
    buf = ctypes.create_unicode_buffer(256)
    user32.GetClassNameW(h, buf, 256)
    return buf.value


def find_top(title):
    hit = []

    def cb(h, l):
        if user32.IsWindowVisible(h) and title in wtext(h):
            hit.append(h)
        return True

    user32.EnumWindows(WNDENUMPROC(cb), None)
    return hit[0] if hit else None


def kids(h):
    out = []

    def cb(c, l):
        out.append(c)
        return True

    user32.EnumChildWindows(h, WNDENUMPROC(cb), None)
    return out


def lb_text(lb, i):
    n = user32.SendMessageW(lb, LB_GETTEXTLEN, i, 0)
    buf = ctypes.create_unicode_buffer(max(2, n + 1))
    user32.SendMessageW(lb, LB_GETTEXT, i, ctypes.byref(buf))
    return buf.value


def grab(hwnd, path):
    r = RECT()
    user32.GetWindowRect(hwnd, ctypes.byref(r))
    w, h = r.right - r.left, r.bottom - r.top
    hdc = user32.GetWindowDC(hwnd)
    mdc = gdi32.CreateCompatibleDC(hdc)
    bmp = gdi32.CreateCompatibleBitmap(hdc, w, h)
    gdi32.SelectObject(mdc, bmp)
    user32.SendMessageW(hwnd, WM_PRINT, mdc, PRF_CLIENT | PRF_ERASEBKGND | PRF_CHILDREN)
    bmi = BIH()
    bmi.biSize = 40
    bmi.biWidth = w
    bmi.biHeight = h
    bmi.biPlanes = 1
    bmi.biBitCount = 32
    buf = (ctypes.c_char * (w * h * 4))()
    gdi32.GetDIBits(mdc, bmp, 0, h, buf, ctypes.byref(bmi), 0)
    data = bytearray(buf)
    with open(path, "wb") as f:
        f.write(b"BM")
        f.write(struct.pack("<IHHI", 14 + 40 + len(data), 0, 0, 14 + 40))
        f.write(struct.pack("<IiiHHIIiiII", 40, w, h, 1, 32, 0, len(data), 2835, 2835, 0, 0))
        f.write(data)
    gdi32.SelectObject(mdc, bmp)
    gdi32.DeleteObject(bmp)
    gdi32.DeleteDC(mdc)
    user32.ReleaseDC(hwnd, hdc)
    return w, h


def main():
    title, idx, out = sys.argv[1], int(sys.argv[2]), sys.argv[3]
    top = find_top(title)
    if not top:
        print("找不到窗口")
        return 1
    ks = kids(top)
    lb = next((h for h in ks if "LISTBOX" in wclass(h).upper()), None)
    tc = next((h for h in ks if "TABCONTROL" in wclass(h).upper()), None)
    if lb is None or tc is None:
        print("找不到导航或页容器", lb, tc)
        return 2

    cnt = user32.SendMessageW(lb, LB_GETCOUNT, 0, 0)
    items = [lb_text(lb, i) for i in range(cnt)]
    print("ListBox 项数=%d 内容=%s" % (cnt, items))
    print("TabControl 页数=%d 当前=%d" % (user32.SendMessageW(tc, TCM_GETITEMCOUNT, 0, 0),
                                          user32.SendMessageW(tc, TCM_GETCURSEL, 0, 0)))
    print("点击前 ListBox 选中=%d" % user32.SendMessageW(lb, LB_GETCURSEL, 0, 0))

    x, y = 95, 42 * idx + 21
    lp = (y << 16) | x
    user32.SendMessageW(lb, WM_LBUTTONDOWN, 1, lp)
    user32.SendMessageW(lb, WM_LBUTTONUP, 0, lp)
    user32.PostMessageW(top, 0x0000, 0, 0)

    print("点击第 %d 项后 ListBox 选中=%d" % (idx, user32.SendMessageW(lb, LB_GETCURSEL, 0, 0)))
    print("点击第 %d 项后 TabControl 当前=%d" % (idx, user32.SendMessageW(tc, TCM_GETCURSEL, 0, 0)))
    print("grab:", grab(top, out), out)
    return 0


if __name__ == "__main__":
    sys.exit(main())