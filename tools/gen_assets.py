# -*- coding: utf-8 -*-
"""生成 FlatCat 所需的图标与呼噜声，全部程序化，无第三方依赖与版权问题。"""
import os, math, struct, wave, random

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "res")
os.makedirs(OUT, exist_ok=True)

# ---------------------------------------------------------------- 图标
FUR = (156, 141, 122)
FUR_D = (120, 106, 89)
PINK = (217, 166, 160)
EYE = (123, 185, 63)
INK = (38, 34, 30)
WHITE = (245, 241, 233)


def blend(dst, src, a):
    """标准 over 合成：src 可以是不带 alpha 的 RGB 三元组。"""
    da = dst[3] / 255.0
    oa = a + da * (1 - a)
    if oa <= 0.0001:
        return (0, 0, 0, 0)
    r = (src[0] * a + dst[0] * da * (1 - a)) / oa
    g = (src[1] * a + dst[1] * da * (1 - a)) / oa
    b = (src[2] * a + dst[2] * da * (1 - a)) / oa
    return (int(r), int(g), int(b), int(oa * 255))


def draw_icon(size, ss=4):
    W = size * ss
    px = [[(0, 0, 0, 0)] * W for _ in range(W)]  # RGBA

    def put(x, y, color, a=1.0):
        if 0 <= x < W and 0 <= y < W:
            px[y][x] = blend(px[y][x], color, a)

    def disc(cx, cy, r, color):
        x0, x1 = int(cx - r) - 1, int(cx + r) + 2
        y0, y1 = int(cy - r) - 1, int(cy + r) + 2
        for y in range(y0, y1):
            for x in range(x0, x1):
                d = math.hypot(x - cx, y - cy)
                if d <= r - 1:
                    put(x, y, color)
                elif d <= r + 1:
                    put(x, y, color, max(0.0, min(1.0, r + 1 - d)) / 2)

    def ellipse(cx, cy, rx, ry, color):
        x0, x1 = int(cx - rx) - 1, int(cx + rx) + 2
        y0, y1 = int(cy - ry) - 1, int(cy + ry) + 2
        for y in range(y0, y1):
            for x in range(x0, x1):
                d = math.hypot((x - cx) / rx, (y - cy) / ry)
                if d <= 1:
                    put(x, y, color)
                elif d <= 1.06:
                    put(x, y, color, (1.06 - d) / 0.06)

    def tri(p1, p2, p3, color):
        xs = [p1[0], p2[0], p3[0]]
        ys = [p1[1], p2[1], p3[1]]
        for y in range(int(min(ys)) - 1, int(max(ys)) + 2):
            for x in range(int(min(xs)) - 1, int(max(xs)) + 2):
                def sign(pa, pb):
                    return (x - pb[0]) * (pa[1] - pb[1]) - (pa[0] - pb[0]) * (y - pb[1])
                d1 = sign(p1, p2)
                d2 = sign(p2, p3)
                d3 = sign(p3, p1)
                neg = (d1 < 0) or (d2 < 0) or (d3 < 0)
                pos = (d1 > 0) or (d2 > 0) or (d3 > 0)
                if not (neg and pos):
                    put(x, y, color)

    s = W / 32.0
    def S(v):
        return v * s

    # 耳朵
    tri((S(7), S(9)), (S(11.5), S(2.5)), (S(15.5), S(8)), FUR)
    tri((S(25), S(9)), (S(20.5), S(2.5)), (S(16.5), S(8)), FUR)
    tri((S(8.6), S(8.2)), (S(11.4), S(4.6)), (S(14), S(8)), PINK)
    tri((S(23.4), S(8.2)), (S(20.6), S(4.6)), (S(18), S(8)), PINK)
    # 头
    ellipse(S(16), S(17.5), S(9.6), S(9.0), FUR)
    # 额头条纹
    tri((S(14.2), S(9.6)), (S(15), S(13.4)), (S(15.8), S(9.6)), FUR_D)
    tri((S(16.4), S(9.6)), (S(17), S(12.6)), (S(17.6), S(9.6)), FUR_D)
    # 眼睛
    ellipse(S(12.2), S(16.6), S(2.5), S(2.9), EYE)
    ellipse(S(19.8), S(16.6), S(2.5), S(2.9), EYE)
    ellipse(S(12.2), S(16.9), S(0.75), S(2.0), INK)
    ellipse(S(19.8), S(16.9), S(0.75), S(2.0), INK)
    # 鼻子与嘴
    tri((S(15.1), S(21)), (S(16.9), S(21)), (S(16), S(22.4)), PINK)
    # 胸
    ellipse(S(16), S(26.5), S(5.4), S(3.0), WHITE)

    # 降采样
    out = []
    for y in range(size):
        row = []
        for x in range(size):
            r = g = b = a = 0.0
            for dy in range(ss):
                for dx in range(ss):
                    p = px[y * ss + dy][x * ss + dx]
                    al = p[3] / 255.0
                    r += p[0] * al
                    g += p[1] * al
                    b += p[2] * al
                    a += al
            n = ss * ss
            a /= n
            if a > 0.0001:
                row.append((int(r / n / a), int(g / n / a), int(b / n / a), int(a * 255)))
            else:
                row.append((0, 0, 0, 0))
        out.append(row)
    return out


def ico_bytes(sizes):
    images = []
    for sz in sizes:
        px = draw_icon(sz)
        # BITMAPINFOHEADER，biHeight 为 2 倍（XOR + AND）
        hdr = struct.pack("<IiiHHIIiiII", 40, sz, sz * 2, 1, 32, 0, sz * sz * 4, 0, 0, 0, 0)
        xor = bytearray()
        for y in range(sz - 1, -1, -1):  # bottom-up
            for x in range(sz):
                r, g, b, a = px[y][x]
                xor += bytes((b, g, r, a))
        and_mask = bytearray()
        row_bytes = ((sz + 31) // 32) * 4
        for y in range(sz - 1, -1, -1):
            row = bytearray(row_bytes)
            for x in range(sz):
                if px[y][x][3] == 0:
                    row[x // 8] |= 0x80 >> (x % 8)
            and_mask += row
        images.append((sz, hdr + bytes(xor) + bytes(and_mask)))

    out = struct.pack("<HHH", 0, 1, len(images))
    offset = 6 + 16 * len(images)
    entries = b""
    blobs = b""
    for sz, blob in images:
        entries += struct.pack("<BBBBHHII", sz if sz < 256 else 0, sz if sz < 256 else 0, 0, 0, 1, 32, len(blob), offset)
        offset += len(blob)
        blobs += blob
    return out + entries + blobs


with open(os.path.join(OUT, "flatcat.ico"), "wb") as f:
    f.write(ico_bytes([16, 32, 48]))

# ---------------------------------------------------------------- 呼噜声
SR = 22050
DUR = 1.6
N = int(SR * DUR)
random.seed(7)
frames = []
for i in range(N):
    t = i / float(SR)
    pulse = math.sin(2 * math.pi * 25 * t)      # 1.6s 正好 40 个周期，循环无缝
    pulse = max(0.0, pulse) ** 0.7
    env = 0.62 + 0.38 * math.sin(2 * math.pi * 1.25 * t)  # 2 个周期，同样无缝
    carrier = 0.60 * math.sin(2 * math.pi * 55 * t) + 0.22 * math.sin(2 * math.pi * 110 * t)
    noise = (random.random() * 2 - 1) * 0.16
    s = (carrier + noise) * pulse * env * 0.45
    s = max(-1.0, min(1.0, s))
    frames.append(struct.pack("<h", int(s * 32767)))

with wave.open(os.path.join(OUT, "purr.wav"), "wb") as w:
    w.setnchannels(1)
    w.setsampwidth(2)
    w.setframerate(SR)
    w.writeframes(b"".join(frames))

# ---------------------------------------------------------------- 猫叫（提醒音）
MEOW_SR = 22050
MEOW_DUR = 0.62
meow_n = int(MEOW_SR * MEOW_DUR)
meow = []
random.seed(21)
phase = 0.0
for i in range(meow_n):
    t = i / float(MEOW_SR)
    k = t / MEOW_DUR
    # 音高先上扬再回落，模拟一声短促的「喵」
    f0 = 560 + 420 * (k / 0.22) if k < 0.22 else 980 - 430 * ((k - 0.22) / 0.78)
    vib = 1.0 + 0.018 * math.sin(2 * math.pi * 7.5 * t)
    phase += 2 * math.pi * f0 * vib / MEOW_SR
    s = 0.55 * math.sin(phase) + 0.22 * math.sin(2 * phase) + 0.08 * math.sin(3 * phase)
    env = (1 - math.exp(-t * 60)) * math.exp(-t * 3.4)
    if k < 0.08:
        env *= k / 0.08
    s = s * env * 0.7 + (random.random() * 2 - 1) * 0.03 * env
    s = max(-1.0, min(1.0, s))
    meow.append(struct.pack("<h", int(s * 32767)))

with wave.open(os.path.join(OUT, "catmeow.wav"), "wb") as w:
    w.setnchannels(1)
    w.setsampwidth(2)
    w.setframerate(MEOW_SR)
    w.writeframes(b"".join(meow))

print("icon:", os.path.getsize(os.path.join(OUT, "flatcat.ico")), "bytes")
print("meow:", os.path.getsize(os.path.join(OUT, "catmeow.wav")), "bytes")
print("purr:", os.path.getsize(os.path.join(OUT, "purr.wav")), "bytes")