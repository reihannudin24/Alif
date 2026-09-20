#!/usr/bin/env python3
"""Generator Kota Cempaka: 6 map pixel-art + peta HP + city.json untuk Unity.

Satu sumber kebenaran: tata letak (bangunan, properti, lantai, penghalang, spawn, NPC quest)
ditulis di MAPS di bawah; skrip menggambar PNG dan menulis city.json yang dirakit runtime
oleh CityWorld.cs. Jalankan ulang setelah mengubah tata letak:

    python3 Tools/city-art/generate_city.py

Butuh Pillow. Koordinat dalam petak (16 px, 0,5 unit), y dihitung dari atas gambar.
"""
import json
import random
import uuid
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets/Resources/Kota"
ART = Path(__file__).resolve().parent / "art"          # latar lukisan tangan (8:3), menang atas gambar kode
BACKUP = Path(__file__).resolve().parent / "generated"  # cadangan map buatan kode yang tertutup lukisan
FONT = ROOT / "Assets/Resources/Fonts/PixelifySans-SemiBold.ttf"
T = 16            # piksel per petak
PPU = 32          # 1 petak = 0,5 unit
W, H = 64, 24     # ukuran map (petak)
ART_PPU = 50      # px lukisan per unit dunia — sama untuk semua map, jadi kerapatan detailnya seragam


def hx(s):
    return tuple(int(s[i:i + 2], 16) for i in (0, 2, 4))


C = {k: hx(v) for k, v in dict(
    out='3e1f10', road='5b5563', road2='534d5b', road3='625c6b', lane='e9e2d0', curb='9c8f86', curb2='bfb3a6',
    walk='dcc7a3', walk2='d2bb93', grout='c3aa80', grass='7aa25a', grass2='6b934d', grass3='86ad63', dirt='b9955f',
    wall='efe0c2', wall2='dccaa6', roof='b5562e', roof2='96431f', roof3='cf7040', wood='8a5a3a', wood2='6e4529',
    wood3='a3714a', navy='22314f', navy2='33456e', sky='6fc6e8', sky2='b6e6f5', tosca='1aa59a', tosca2='6fd4c8',
    glass='6f8fa8', glass2='9ab6c8', green='3f8a5a', green2='2f6f45', white='f5f0e3', brown='5a3a26', warm='f6c46a',
    teal='2f6f6a', teal2='3f8a84', gold='e4b33c', grey='8d8a8f', grey2='6b6870', leaf='4f8a3f', leaf2='6fae52',
    leaf3='3c6e30', pot='b5562e', red='c0392b', pink='f09ab0', pink2='f7c9d6', blue='3b6ea8', blue2='7aa7d8',
    brick='a4553a', brick2='8a4530', water='5fa8c8', water2='8cc6de', stone='c9c1b3', stone2='a79f93',
    shadow='00000040', lilac='b7a2d6', cream='fff4dc', yellow='f2cf5b').items()}
C['shadow'] = (0, 0, 0, 60)

FONT_SIGN = ImageFont.truetype(str(FONT), 11)
FONT_SMALL = ImageFont.truetype(str(FONT), 9)


class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h          # ukuran map dalam petak — tiap map boleh beda
        self.img = Image.new('RGBA', (w * T, h * T), C['grass'])
        self.d = ImageDraw.Draw(self.img)
        self.d.fontmode = '1'   # tanpa antialias: piksel tetap tajam

    def r(self, x, y, w, h, c):
        if w > 0 and h > 0:
            self.d.rectangle([x, y, x + w - 1, y + h - 1], fill=c)

    def box(self, x, y, w, h, c, o=C['out']):
        self.r(x, y, w, h, o)
        self.r(x + 1, y + 1, w - 2, h - 2, c)

    def wash(self, x, y, w, h, c):
        """Lapisan warna tembus pandang di atas yang sudah digambar (RGBA)."""
        overlay = Image.new('RGBA', self.img.size, (0, 0, 0, 0))
        ImageDraw.Draw(overlay).rectangle([x, y, x + w - 1, y + h - 1], fill=c)
        self.img.alpha_composite(overlay)

    def shade(self, x, y, w, h):
        self.wash(x, y, w, h, C['shadow'])

    def text(self, x, y, w, s, color, font=FONT_SIGN):
        tw = self.d.textlength(s, font=font)
        self.d.text((x + (w - tw) / 2, y), s, font=font, fill=color)


# ───────────────────────────── tanah ─────────────────────────────

def ground_grass(cv, rows, rng):
    for ty in rows:
        for tx in range(cv.w):
            x, y = tx * T, ty * T
            cv.r(x, y, T, T, C['grass'] if (tx + ty) % 2 else C['grass2'])
            for _ in range(2):
                gx, gy = rng.randrange(T - 2), rng.randrange(T - 3)
                cv.r(x + gx, y + gy, 1, 3, C['grass3'])


def ground_sidewalk(cv, rows):
    for ty in rows:
        for tx in range(cv.w):
            x, y = tx * T, ty * T
            cv.r(x, y, T, T, C['walk'] if (tx + ty) % 2 else C['walk2'])
            cv.r(x, y + T - 1, T, 1, C['grout'])
            cv.r(x + T - 1, y, 1, T, C['grout'])


def ground_road(cv, top, bottom, rng, crosswalk=None):
    cv.r(0, top * T, cv.w * T, T, C['curb'])
    cv.r(0, top * T, cv.w * T, 3, C['curb2'])
    for ty in range(top + 1, bottom):
        for tx in range(cv.w):
            cv.r(tx * T, ty * T, T, T, rng.choice([C['road'], C['road'], C['road2'], C['road3']]))
    mid = (top + 1 + bottom) * T // 2 - 2
    for tx in range(0, cv.w, 4):
        cv.r(tx * T + 4, mid, 2 * T, 3, C['lane'])
    if crosswalk is not None:
        for ty in range(top + 1, bottom):
            for k in range(4):
                cv.r(crosswalk * T + k * 8, ty * T + 3, 5, T - 6, C['lane'])
    cv.r(0, bottom * T, cv.w * T, 3, C['curb2'])
    cv.r(0, bottom * T + 3, cv.w * T, T - 3, C['curb'])


def ground_paving(cv, rows, a, b):
    for ty in rows:
        for tx in range(cv.w):
            x, y = tx * T, ty * T
            cv.r(x, y, T, T, a if (tx + ty) % 2 else b)
            cv.r(x, y + T - 1, T, 1, C['stone2'])


# ─────────────────────────── bangunan ───────────────────────────

def roof(cv, x, y, w, h=16, c=None):
    c = c or (C['roof'], C['roof2'], C['roof3'])
    cv.box(x - 3, y, w + 6, h, c[0])
    for i in range(y + 3, y + h - 1, 4):
        cv.r(x - 2, i, w + 4, 1, c[1])
    cv.r(x - 2, y + 1, w + 4, 1, c[2])
    cv.r(x - 3, y + h - 2, w + 6, 2, C['out'])


def window(cv, x, y, w, h, lit=False, curtain=None):
    cv.box(x, y, w, h, C['warm'] if lit else C['glass'])
    cv.r(x + 2, y + 2, max(2, w // 3), 2, C['cream'] if lit else C['glass2'])
    cv.r(x + w // 2, y + 1, 1, h - 2, C['out'])
    if curtain:
        cv.r(x + 1, y + 1, 3, h - 2, curtain)
        cv.r(x + w - 4, y + 1, 3, h - 2, curtain)
    cv.r(x - 1, y + h, w + 2, 2, C['wall2'])


def door(cv, x, y, w, h, c=None, glass=False):
    cv.box(x, y, w, h, c or C['wood'])
    if glass:
        cv.r(x + 3, y + 3, w - 6, h - 8, C['glass2'])
        cv.r(x + w // 2, y + 2, 1, h - 4, C['out'])
    else:
        cv.r(x + 3, y + 3, w - 6, h // 2 - 4, C['wood2'])
        cv.r(x + w - 5, y + h // 2, 2, 2, C['gold'])


def awning(cv, x, y, w, a, b):
    for i in range(0, w, 6):
        cv.r(x + i, y, 6, 8, a if (i // 6) % 2 == 0 else b)
    cv.d.rectangle([x, y, x + w - 1, y + 7], outline=C['out'])
    for i in range(0, w, 6):
        cv.r(x + i + 1, y + 8, 4, 2, a if (i // 6) % 2 == 0 else b)


def sign(cv, x, y, w, text, bg, fg=None):
    cv.box(x, y, w, 13, bg)
    cv.text(x, y + 1, w, text, fg or C['white'])


def shelves(cv, x, y, w, h, rng):
    cv.box(x, y, w, h, C['wood2'])
    for sy in range(y + 7, y + h - 2, 8):
        cv.r(x + 2, sy, w - 4, 1, C['wood'])
        for sx in range(x + 3, x + w - 5, 5):
            cv.r(sx, sy - 4, 3, 4, rng.choice([C['gold'], C['roof3'], C['green'], C['glass2'], C['white'], C['pink']]))


def building(cv, spec, base, rng):
    """Fasad bangunan berdiri di baris `base` (tepi bawah fasad)."""
    x, w, h = spec['x'] * T, spec['w'] * T, spec['h'] * T
    kind, name = spec['kind'], spec.get('name', '')
    top = base * T - h
    wall = hx(spec['wall']) if 'wall' in spec else C['wall']
    if kind == 'masjid':
        dome_w = w // 2
        cv.d.ellipse([x + w // 2 - dome_w // 2, top - 34, x + w // 2 + dome_w // 2, top + 18], fill=C['green'], outline=C['out'])
        cv.r(x + w // 2 - 1, top - 44, 2, 12, C['gold'])
        cv.d.ellipse([x + w // 2 - 4, top - 50, x + w // 2 + 3, top - 43], fill=C['gold'], outline=C['out'])
        cv.box(x, top, w, h, C['white'])
        for ax in range(x + 10, x + w - 20, 26):
            cv.d.pieslice([ax, top + 22, ax + 18, top + 40], 180, 360, fill=C['green2'])
            cv.r(ax, top + 31, 18, h - 38, C['green2'])
        sign(cv, x + 20, top + 6, w - 40, name, C['green'])
        cv.shade(x, base * T, w, 4)
        return
    roof(cv, x, top - 14, w)
    cv.box(x, top, w, h, wall)
    cv.r(x + 1, base * T - 5, w - 2, 4, C['wall2'])
    cv.shade(x, base * T, w, 4)
    if name:
        sign(cv, x + 8, top + 4, w - 16, name, hx(spec.get('sign', '5a3a26')))
    inner = top + 22
    if kind == 'kelontong':
        awning(cv, x + 2, inner, w - 4, C['green'], C['white'])
        shelves(cv, x + 6, inner + 12, w - 12, h - 40, rng)
        cv.box(x + w // 2 - 16, base * T - 16, 32, 12, C['wood'])
        for sx in range(x + w // 2 - 13, x + w // 2 + 12, 6):
            cv.r(sx, base * T - 21, 4, 5, rng.choice([C['gold'], C['roof3'], C['green']]))
    elif kind in ('minimarket', 'gerai'):
        stripe = C['red'] if kind == 'minimarket' else C['blue']
        cv.r(x + 1, inner - 2, w - 2, 5, stripe)
        window(cv, x + 8, inner + 8, w // 2 - 24, h - 42)
        door(cv, x + w // 2 - 12, base * T - 32, 24, 28, C['glass'], glass=True)
        window(cv, x + w // 2 + 16, inner + 8, w // 2 - 24, h - 42)
        if kind == 'gerai':
            for px in range(x + 14, x + w // 2 - 20, 9):
                cv.box(px, inner + 16, 6, 10, C['grey2'])
    elif kind == 'kafe':
        awning(cv, x + 2, inner, w - 4, C['roof'], C['cream'])
        window(cv, x + 8, inner + 12, 34, 24, lit=True)
        window(cv, x + w - 42, inner + 12, 34, 24, lit=True)
        door(cv, x + w // 2 - 9, base * T - 30, 18, 27)
        cv.box(x + w // 2 - 20, inner + 12, 11, 14, C['brown'])
        cv.r(x + w // 2 - 18, inner + 15, 7, 1, C['white'])
        cv.r(x + w // 2 - 18, inner + 18, 7, 1, C['white'])
    elif kind == 'kosmetik':
        awning(cv, x + 2, inner, w - 4, C['pink'], C['white'])
        window(cv, x + 8, inner + 12, w // 2 - 22, h - 44, lit=True, curtain=C['pink2'])
        door(cv, x + w // 2 - 10, base * T - 30, 20, 27, C['pink2'], glass=True)
        for px in range(x + w // 2 + 16, x + w - 10, 8):
            cv.box(px, inner + 18, 6, 12, rng.choice([C['pink'], C['lilac'], C['white']]))
    elif kind == 'bank':
        for cx in range(x + 8, x + w - 8, 22):
            cv.box(cx, inner, 7, h - 26, C['white'])
            cv.r(cx + 2, inner + 2, 1, h - 30, C['stone'])
        window(cv, x + 22, inner + 10, 26, 22)
        window(cv, x + w - 48, inner + 10, 26, 22)
        door(cv, x + w // 2 - 12, base * T - 32, 24, 29, C['glass'], glass=True)
        cv.d.ellipse([x + w // 2 - 7, inner - 2, x + w // 2 + 6, inner + 11], fill=C['gold'], outline=C['out'])
    elif kind == 'atm':
        cv.box(x + 6, inner + 2, w - 12, h - 28, C['grey'])
        cv.box(x + 10, inner + 6, w - 20, 10, C['blue2'])
        cv.r(x + 12, inner + 20, w - 24, 3, C['out'])
    elif kind == 'fakultas':
        for fl in range(2):
            fy = inner + 4 + fl * 30
            for wx in range(x + 10, x + w - 26, 30):
                window(cv, wx, fy, 20, 18)
        door(cv, x + w // 2 - 14, base * T - 32, 28, 29, C['glass'], glass=True)
    elif kind == 'gerbang':
        cv.box(x, top + 20, 14, h - 20, C['brick'])
        cv.box(x + w - 14, top + 20, 14, h - 20, C['brick'])
        for bx in range(x + 2, x + 12, 4):
            cv.r(bx, top + 24, 2, h - 26, C['brick2'])
    elif kind == 'fotokopi':
        window(cv, x + 8, inner + 10, w - 16, 20)
        cv.box(x + 12, inner + 32, 20, 14, C['grey'])
        cv.r(x + 14, inner + 34, 16, 3, C['white'])
    elif kind in ('rumah', 'kos', 'warung'):
        window(cv, x + 10, inner + 6, 24, 20, curtain=C['pink2'] if kind == 'rumah' else None)
        door(cv, x + w - 34, base * T - 30, 18, 27)
        if kind == 'warung':
            awning(cv, x + 2, inner, w - 4, C['yellow'], C['white'])
            cv.box(x + 8, base * T - 16, w - 50, 12, C['wood'])
            for sx in range(x + 10, x + w - 46, 5):
                cv.r(sx, base * T - 20, 3, 4, rng.choice([C['gold'], C['roof3'], C['pink']]))
        if kind == 'kos':
            window(cv, x + w // 2 - 6, inner + 6, 16, 16)


# ─────────────────────────── properti ───────────────────────────
# Setiap properti menggambar dirinya dan (bila berdiri di lantai) mengembalikan kotak penghalang
# dalam piksel (x, y, w, h) — ditulis ke city.json sebagai collider padat.

def p_tree(cv, x, y):
    cv.shade(x + 4, y + 36, 26, 5)
    cv.r(x + 13, y + 22, 7, 18, C['wood2'])
    cv.d.ellipse([x, y, x + 33, y + 28], fill=C['leaf'], outline=C['out'])
    cv.d.ellipse([x + 5, y + 3, x + 22, y + 15], fill=C['leaf2'])
    cv.d.ellipse([x + 18, y + 12, x + 30, y + 24], fill=C['leaf3'])
    return (x + 11, y + 34, 11, 6)


def p_palm(cv, x, y):
    cv.shade(x + 4, y + 38, 22, 4)
    cv.box(x + 5, y + 30, 20, 11, C['stone'])
    cv.r(x + 13, y + 10, 4, 22, C['wood'])
    for dx, dy in ((-12, -2), (12, -2), (-9, 6), (9, 6), (0, -6)):
        cv.d.ellipse([x + 15 + dx - 9, y + 6 + dy - 4, x + 15 + dx + 9, y + 6 + dy + 4], fill=C['leaf'], outline=C['out'])
    return (x + 5, y + 32, 20, 9)


def p_plant(cv, x, y):
    cv.shade(x + 2, y + 16, 14, 3)
    cv.box(x + 3, y + 9, 11, 8, C['pot'])
    cv.d.ellipse([x, y - 2, x + 16, y + 11], fill=C['leaf'], outline=C['out'])
    cv.r(x + 4, y + 1, 5, 3, C['leaf2'])
    return (x + 2, y + 10, 13, 7)


def p_lamp(cv, x, y):
    cv.shade(x - 2, y + 44, 10, 3)
    cv.box(x - 2, y + 40, 8, 5, C['grey2'])
    cv.r(x, y + 4, 3, 38, C['grey2'])
    cv.box(x - 4, y - 4, 11, 9, C['warm'])
    return (x - 2, y + 40, 8, 5)


def p_bench(cv, x, y):
    cv.shade(x, y + 14, 34, 3)
    cv.box(x, y, 34, 5, C['wood'])
    cv.box(x, y + 6, 34, 5, C['wood'])
    cv.r(x + 3, y + 11, 3, 5, C['grey2'])
    cv.r(x + 28, y + 11, 3, 5, C['grey2'])
    return (x, y + 6, 34, 9)


def p_table(cv, x, y):
    cv.shade(x - 7, y + 13, 28, 3)
    cv.d.ellipse([x, y, x + 14, y + 6], fill=C['white'], outline=C['out'])
    cv.r(x + 6, y + 7, 2, 7, C['brown'])
    cv.box(x - 7, y + 4, 6, 9, C['wood'])
    cv.box(x + 15, y + 4, 6, 9, C['wood'])
    return (x - 7, y + 6, 28, 8)


def p_umbrella_table(cv, x, y):
    box_ = p_table(cv, x, y + 10)
    cv.r(x + 6, y - 2, 2, 12, C['grey2'])
    cv.d.pieslice([x - 10, y - 10, x + 24, y + 8], 180, 360, fill=C['roof3'], outline=C['out'])
    return box_


def p_halte(cv, x, y):
    cv.shade(x, y + 30, 56, 4)
    cv.box(x, y, 56, 6, C['teal'])
    cv.r(x + 2, y + 6, 3, 26, C['grey2'])
    cv.r(x + 51, y + 6, 3, 26, C['grey2'])
    cv.box(x + 6, y + 8, 44, 12, C['glass2'])
    cv.box(x + 8, y + 22, 40, 5, C['wood'])
    sign(cv, x + 14, y - 12, 28, 'HALTE', C['teal'])
    return (x, y + 22, 56, 10)


def p_bike(cv, x, y):
    cv.d.ellipse([x, y + 6, x + 9, y + 15], outline=C['out'])
    cv.d.ellipse([x + 14, y + 6, x + 23, y + 15], outline=C['out'])
    cv.d.line([x + 4, y + 10, x + 11, y + 4, x + 18, y + 10], fill=C['red'], width=2)
    cv.r(x + 10, y + 2, 5, 2, C['out'])
    return (x, y + 8, 23, 7)


def p_motor(cv, x, y):
    cv.shade(x, y + 14, 30, 3)
    cv.d.ellipse([x, y + 8, x + 9, y + 16], fill=C['out'])
    cv.d.ellipse([x + 21, y + 8, x + 30, y + 16], fill=C['out'])
    cv.box(x + 5, y + 2, 20, 9, C['blue'])
    cv.box(x + 9, y, 10, 4, C['out'])
    return (x, y + 8, 30, 8)


def p_bin(cv, x, y):
    cv.shade(x, y + 13, 12, 2)
    cv.box(x, y, 12, 14, C['green'])
    cv.r(x + 2, y + 3, 8, 1, C['green2'])
    return (x, y + 8, 12, 6)


def p_fountain(cv, x, y):
    cv.shade(x - 2, y + 44, 68, 5)
    cv.d.ellipse([x, y + 12, x + 64, y + 46], fill=C['stone'], outline=C['out'])
    cv.d.ellipse([x + 6, y + 17, x + 58, y + 41], fill=C['water'])
    cv.d.ellipse([x + 12, y + 21, x + 30, y + 29], fill=C['water2'])
    cv.box(x + 27, y, 10, 26, C['stone'])
    cv.d.ellipse([x + 23, y - 6, x + 41, y + 4], fill=C['water2'], outline=C['out'])
    return (x + 2, y + 16, 60, 30)


def p_flowers(cv, x, y, rng):
    cv.box(x, y, 48, 14, C['dirt'])
    for fx in range(x + 3, x + 45, 5):
        cv.r(fx, y + 3 + rng.randrange(4), 3, 3, rng.choice([C['red'], C['pink'], C['yellow'], C['white']]))
        cv.r(fx + 1, y + 7, 1, 4, C['leaf'])
    return (x, y + 4, 48, 10)


def p_gazebo(cv, x, y):
    cv.shade(x, y + 58, 72, 5)
    cv.d.polygon([(x - 4, y + 14), (x + 36, y - 6), (x + 76, y + 14)], fill=C['roof'], outline=C['out'])
    cv.r(x - 4, y + 14, 80, 4, C['roof2'])
    for px in (x + 2, x + 34, x + 66):
        cv.r(px, y + 18, 4, 38, C['wood'])
    cv.box(x, y + 50, 72, 10, C['wood'])
    return (x, y + 44, 72, 16)


def p_pond(cv, x, y):
    cv.d.ellipse([x, y, x + 90, y + 44], fill=C['stone2'], outline=C['out'])
    cv.d.ellipse([x + 5, y + 4, x + 85, y + 40], fill=C['water'])
    cv.d.ellipse([x + 16, y + 10, x + 40, y + 20], fill=C['water2'])
    cv.d.ellipse([x + 56, y + 22, x + 66, y + 28], fill=C['leaf2'])
    return (x + 2, y + 2, 86, 40)


def p_board(cv, x, y):
    cv.shade(x, y + 30, 32, 3)
    cv.r(x + 3, y + 14, 3, 18, C['wood2'])
    cv.r(x + 26, y + 14, 3, 18, C['wood2'])
    cv.box(x, y, 32, 18, C['wood'])
    cv.box(x + 3, y + 3, 26, 12, C['cream'])
    for ly in range(y + 5, y + 13, 3):
        cv.r(x + 5, ly, 16, 1, C['grey'])
    return (x, y + 26, 32, 6)


def p_clothesline(cv, x, y, rng):
    cv.r(x, y, 2, 30, C['wood2'])
    cv.r(x + 60, y, 2, 30, C['wood2'])
    cv.r(x, y + 3, 62, 1, C['grey2'])
    for cx in range(x + 5, x + 55, 12):
        cv.box(cx, y + 4, 9, 11, rng.choice([C['blue2'], C['pink'], C['white'], C['yellow']]))
    return (x, y + 26, 62, 5)


# ─────────────────────────── interior ───────────────────────────
# Ruangan dalam gedung: dinding belakang di 3 baris atas, dinding depan berkaca di 2 baris
# bawah (tempat pintu keluar), lantai berubin di antaranya. Lantai yang bisa dilewati =
# baris 3 s.d. (tinggi - 3).

def room(cv, floor_a, floor_b, wall, front_door):
    """Gambar dinding belakang, lantai, dan dinding depan berkaca. front_door = petak pintunya."""
    cv.r(0, 0, cv.w * T, 3 * T, wall)                                   # dinding belakang
    for tx in range(cv.w):
        cv.r(tx * T, 0, 1, 3 * T, C['wall2'])                           # garis nat panel
    cv.r(0, 3 * T - 7, cv.w * T, 5, C['wood'])                          # lis kayu
    cv.r(0, 3 * T - 2, cv.w * T, 2, C['out'])
    for ty in range(3, cv.h - 2):                                       # lantai
        for tx in range(cv.w):
            cv.r(tx * T, ty * T, T, T, floor_a if (tx + ty) % 2 else floor_b)
            cv.r(tx * T, ty * T + T - 1, T, 1, C['grout'])
            cv.r(tx * T + T - 1, ty * T, 1, T, C['grout'])
    cv.shade(0, 3 * T, cv.w * T, 6)                                     # bayangan dinding
    y = (cv.h - 2) * T                                                  # dinding depan
    cv.r(0, y, cv.w * T, 2 * T, wall)
    cv.r(0, y, cv.w * T, 3, C['out'])
    for tx in range(0, cv.w - 1, 3):
        if abs(tx + 1 - front_door) <= 2:
            continue
        cv.box(tx * T + 3, y + 7, 2 * T - 6, T + 2, C['glass'])
        cv.r(tx * T + T, y + 9, 2, T - 2, C['wall2'])
    cv.box(front_door * T - T + 2, y + 5, 3 * T - 4, 2 * T - 8, C['glass2'])
    cv.r(front_door * T - 1, y + 7, 2, 2 * T - 12, C['wall2'])
    cv.r(front_door * T - T + 6, y + 2 * T - 6, 3 * T - 12, 4, C['wood2'])   # keset


def p_door(cv, x, y):
    """Daun pintu kayu di dinding belakang — pasangannya penanda pintu di city.json."""
    cv.box(x, y + 4, 28, 40, C['wood2'])
    cv.r(x + 3, y + 7, 22, 34, C['wood'])
    cv.r(x + 5, y + 10, 18, 12, C['wood2'])
    cv.r(x + 21, y + 26, 3, 4, C['gold'])
    cv.r(x - 2, y + 2, 32, 3, C['out'])
    return None                                     # menempel dinding, bukan penghalang lantai


def p_counter(cv, x, y):
    cv.box(x, y, 96, 24, C['wood'])
    cv.r(x + 3, y + 3, 90, 9, C['wood2'])
    cv.r(x + 3, y + 15, 90, 6, C['wall2'])
    cv.shade(x, y + 24, 96, 4)
    return (x, y + 12, 96, 14)


def p_sofa(cv, x, y):
    cv.box(x, y, 52, 13, C['teal'])
    cv.box(x, y + 9, 52, 14, C['teal2'])
    cv.r(x + 4, y + 12, 20, 7, C['white'])
    cv.r(x + 28, y + 12, 20, 7, C['white'])
    cv.shade(x, y + 23, 52, 3)
    return (x, y + 9, 52, 14)


def p_desk(cv, x, y):
    cv.box(x + 7, y, 16, 9, C['blue'])              # sandaran kursi
    cv.box(x, y + 7, 30, 11, C['wood'])             # meja
    cv.r(x + 2, y + 18, 3, 5, C['wood2'])
    cv.r(x + 25, y + 18, 3, 5, C['wood2'])
    cv.shade(x, y + 23, 30, 3)
    return (x, y + 7, 30, 16)


def p_whiteboard(cv, x, y):
    cv.box(x, y, 112, 36, C['grey2'])
    cv.r(x + 3, y + 3, 106, 30, C['white'])
    cv.r(x + 9, y + 10, 44, 2, C['blue'])
    cv.r(x + 9, y + 17, 66, 2, C['grey2'])
    cv.r(x + 9, y + 24, 32, 2, C['grey2'])
    return None


def p_teacherdesk(cv, x, y):
    cv.box(x, y, 48, 20, C['wood2'])
    cv.r(x + 3, y + 3, 42, 7, C['wood'])
    cv.box(x + 6, y - 7, 14, 8, C['white'])
    cv.shade(x, y + 20, 48, 3)
    return (x, y + 8, 48, 15)


def p_shelf(cv, x, y):
    books = [C['red'], C['blue'], C['green'], C['gold'], C['teal']]
    cv.box(x, y, 34, 42, C['wood2'])
    for j in range(4):
        cv.r(x + 3, y + 4 + j * 10, 28, 8, C['wood'])
        for i in range(7):
            cv.r(x + 4 + i * 4, y + 5 + j * 10, 3, 6, books[(i + j) % 5])
    return None


def p_officedesk(cv, x, y):
    cv.box(x + 32, y - 13, 18, 14, C['grey2'])      # monitor
    cv.r(x + 34, y - 11, 14, 10, C['glass'])
    cv.box(x, y, 54, 21, C['wood'])
    cv.r(x + 3, y + 3, 48, 8, C['wood2'])
    cv.box(x + 6, y - 6, 13, 7, C['white'])         # tumpukan kertas
    cv.shade(x, y + 21, 54, 3)
    return (x, y + 9, 54, 15)


def p_stall(cv, x, y):
    cv.box(x, y, 40, 46, C['wall2'])
    cv.r(x + 3, y + 3, 34, 40, C['glass2'])
    cv.box(x + 7, y + 9, 26, 34, C['white'])        # pintu bilik
    cv.r(x + 27, y + 25, 3, 4, C['grey2'])
    cv.shade(x, y + 46, 40, 3)
    return (x, y + 32, 40, 17)


def p_sink(cv, x, y):
    cv.box(x + 2, y - 27, 22, 20, C['glass'])       # cermin di dinding
    cv.r(x + 4, y - 25, 18, 16, C['glass2'])
    cv.r(x + 12, y - 6, 3, 7, C['grey2'])           # keran
    cv.box(x, y, 26, 13, C['white'])
    cv.r(x + 4, y + 3, 18, 6, C['glass2'])
    return (x, y + 4, 26, 11)


# ─────────────────────────── puskesmas ───────────────────────────
# Ruang klinik: dinding krem, ubin putih, dua bilik periksa bertirai, meja pendaftaran,
# lemari obat, dan deret kursi tunggu. Prop dinding mengembalikan None (menempel dinding,
# bukan penghalang lantai).

def p_poster(cv, x, y):
    """Dua poster penyuluhan kesehatan di dinding belakang."""
    for i in (0, 1):
        px = x + i * 36
        cv.box(px, y, 30, 40, C['wood2'])
        cv.r(px + 2, y + 2, 26, 36, C['white'])
        cv.r(px + 5, y + 5, 20, 2, C['out'])              # judul poster
        cv.r(px + 13, y + 10, 5, 5, C['pink2'])           # kepala
        cv.r(px + 11, y + 15, 9, 14, C['pink'])           # badan
        for gy in range(y + 17, y + 30, 5):
            cv.r(px + 6, gy, 4, 1, C['red'])              # keterangan kiri-kanan
            cv.r(px + 21, gy, 4, 1, C['red'])
        cv.r(px + 6, y + 32, 18, 2, C['grey'])
    return None


def p_medsign(cv, x, y):
    """Papan "KLINIK" dengan salib merah — dinding belakang, di atas meja pendaftaran."""
    cv.box(x, y, 112, 28, C['wood2'])
    cv.r(x + 3, y + 3, 106, 22, C['cream'])
    cv.box(x + 7, y + 6, 16, 16, C['red'])
    cv.r(x + 13, y + 8, 4, 12, C['white'])
    cv.r(x + 9, y + 12, 12, 4, C['white'])
    cv.text(x + 26, y + 7, 80, 'KLINIK', C['out'])
    return None


def p_curtain(cv, x, y):
    """Sekat bilik periksa: rel logam dengan tirai hijau berlipat (lebar 6 petak)."""
    cv.box(x, y, 96, 7, C['grey'])
    for rx in range(x + 6, x + 92, 12):
        cv.r(rx, y + 5, 3, 4, C['grey2'])                 # kait tirai
    cv.box(x + 2, y + 7, 92, 44, C['green2'])
    for fx in range(x + 5, x + 92, 8):
        cv.r(fx, y + 9, 4, 40, C['green'])                # lipatan kain
        cv.r(fx + 4, y + 9, 1, 40, C['teal2'])            # kilau tepi lipatan
    cv.r(x + 2, y + 45, 92, 2, C['teal'])                 # kelim bawah
    cv.r(x + 2, y + 49, 92, 2, C['out'])
    cv.shade(x, y + 51, 96, 3)
    return (x, y + 36, 96, 16)


def p_clinicbed(cv, x, y):
    """Ranjang periksa: rangka logam, kasur putih, bantal biru, alas kertas."""
    cv.box(x, y, 68, 32, C['grey'])
    cv.r(x + 3, y + 3, 62, 20, C['white'])
    cv.box(x + 5, y + 5, 18, 14, C['blue2'])              # bantal
    cv.r(x + 3, y + 24, 62, 5, C['glass2'])               # alas kertas
    for lx in (x + 4, x + 60):
        cv.r(lx, y + 32, 4, 9, C['grey2'])                # kaki
    cv.shade(x, y + 41, 68, 4)
    return (x, y + 18, 68, 25)


def p_trolley(cv, x, y):
    """Troli alat: botol antiseptik, kotak kasa, baki logam beroda."""
    cv.box(x + 3, y - 13, 11, 14, C['teal2'])
    cv.r(x + 5, y - 10, 7, 6, C['glass2'])
    cv.box(x + 18, y - 10, 13, 11, C['white'])
    cv.r(x + 22, y - 7, 6, 2, C['red'])
    cv.box(x, y, 34, 28, C['grey'])
    cv.r(x + 2, y + 2, 30, 4, C['glass2'])
    cv.r(x + 3, y + 13, 28, 6, C['gold'])                 # laci
    for lx in (x + 3, x + 28):
        cv.r(lx, y + 28, 3, 5, C['grey2'])                # roda
    cv.shade(x, y + 33, 34, 3)
    return (x, y + 14, 34, 19)


def p_medcabinet(cv, x, y):
    """Lemari obat berpintu kaca dengan tiga rak botol."""
    tint = (C['teal2'], C['red'], C['gold'], C['blue2'], C['green'])
    cv.box(x, y, 40, 56, C['grey2'])
    cv.r(x + 3, y + 3, 34, 50, C['glass2'])
    for j in range(3):
        for i in range(5):
            cv.r(x + 5 + i * 6, y + 7 + j * 16, 4, 9, tint[(i + j) % 5])
        cv.r(x + 4, y + 17 + j * 16, 32, 2, C['grey'])
    cv.r(x + 19, y + 3, 2, 50, C['grey2'])                # kusen pintu tengah
    cv.shade(x, y + 56, 40, 3)
    return (x, y + 40, 40, 19)


def p_clock(cv, x, y):
    """Jam dinding ruang tunggu."""
    cv.box(x, y, 24, 24, C['wood2'])
    cv.r(x + 3, y + 3, 18, 18, C['white'])
    cv.r(x + 11, y + 6, 2, 7, C['out'])                   # jarum panjang
    cv.r(x + 12, y + 11, 6, 2, C['out'])                  # jarum pendek
    cv.r(x + 11, y + 11, 2, 2, C['red'])
    return None


def p_chairrow(cv, x, y):
    """Deret tiga kursi tunggu berangka logam (lebar 4 petak)."""
    for i in range(3):
        cx = x + i * 22
        cv.box(cx + 2, y, 18, 12, C['roof2'])             # sandaran
        cv.box(cx, y + 10, 22, 10, C['red'])              # dudukan
        cv.r(cx + 3, y + 12, 16, 3, C['roof3'])
        cv.r(cx + 2, y + 20, 3, 6, C['grey2'])
        cv.r(cx + 17, y + 20, 3, 6, C['grey2'])
    cv.shade(x, y + 26, 66, 3)
    return (x, y + 10, 66, 17)


# ─────────────────────────── gereja ───────────────────────────
# Ruang ibadah: dinding belakang berkaca patri + salib di atas altar, panggung marmer,
# lorong berkarpet merah di tengah, dan deretan bangku jemaat di kiri-kanannya.

def holy_window(cv, x, y, w=22, h=34):
    """Jendela kaca patri melengkung di dinding belakang."""
    cv.d.pieslice([x, y, x + w - 1, y + w - 1], 180, 360, fill=C['out'])
    cv.r(x, y + w // 2, w, h - w // 2, C['out'])
    cv.d.pieslice([x + 2, y + 2, x + w - 3, y + w - 3], 180, 360, fill=C['glass2'])
    cv.r(x + 2, y + w // 2, w - 4, h - w // 2 - 2, C['glass2'])
    tint = (C['red'], C['gold'], C['teal2'], C['blue2'], C['pink'], C['green'])
    for i, c in enumerate(tint):
        cv.r(x + 3 + (i % 2) * 9, y + 10 + (i // 2) * 7, 8, 6, c)
    cv.r(x + w // 2 - 1, y + 5, 2, h - 8, C['wood2'])     # kisi tengah
    cv.r(x + 2, y + h - 3, w - 4, 2, C['wood2'])          # ambang bawah


def wall_cross(cv, x, y):
    """Salib batu di dinding belakang, tepat di atas altar. x = titik tengahnya."""
    cv.box(x - 5, y, 10, 42, C['white'])
    cv.box(x - 17, y + 12, 34, 10, C['white'])
    cv.r(x + 2, y + 2, 2, 38, C['stone2'])
    cv.r(x - 15, y + 18, 30, 2, C['stone2'])


def p_pew(cv, x, y):
    """Bangku jemaat selebar 10 petak. Penghalangnya setinggi jarak antarbaris (2 petak)
    supaya bangku-bangku menyatu — tidak ada celah sempit tempat pemain tersangkut."""
    cv.box(x, y + 1, 160, 10, C['wood2'])                 # sandaran
    cv.r(x + 3, y + 3, 154, 5, C['wood'])
    cv.box(x, y + 12, 160, 8, C['wood'])                  # dudukan
    cv.r(x + 3, y + 14, 154, 3, C['wood3'])
    cv.box(x, y + 1, 7, 25, C['wood2'])                   # papan ujung kiri
    cv.box(x + 153, y + 1, 7, 25, C['wood2'])             # papan ujung kanan
    cv.r(x + 77, y + 20, 6, 6, C['wood2'])                # kaki tengah
    cv.shade(x, y + 26, 160, 4)
    return (x, y, 160, 2 * T)


def p_altar(cv, x, y):
    """Meja altar berkain merah dengan mimbar kayu — pusat ruang ibadah (lebar 6 petak)."""
    cv.box(x + 30, y - 4, 36, 34, C['wood2'])             # badan mimbar
    cv.r(x + 33, y, 30, 26, C['wood'])
    cv.box(x + 34, y - 13, 28, 11, C['wood'])             # meja baca miring
    cv.r(x + 37, y - 11, 22, 6, C['cream'])               # kitab terbuka
    cv.r(x + 47, y - 11, 2, 6, C['wood2'])
    cv.box(x, y + 26, 96, 28, C['red'])                   # kain altar
    cv.r(x + 3, y + 29, 90, 5, C['roof2'])
    for gx in range(x + 4, x + 92, 8):
        cv.r(gx, y + 49, 5, 4, C['gold'])                 # rumbai emas
    cv.shade(x, y + 54, 96, 4)
    return (x, y + 26, 96, 30)


def church_room(cv, front):
    """Gambar seluruh ruang gereja. front = petak tengah lorong sekaligus tengah pintu keluar.
    Baris 0-2 dinding belakang, 3-6 panggung altar, 7 s.d. tinggi-3 ruang jemaat,
    dua baris terbawah dinding depan. Lantai yang bisa dilewati = baris 3 s.d. tinggi-3."""
    w, h = cv.w * T, cv.h * T
    cv.r(0, 0, w, 3 * T, C['wall'])                                   # dinding belakang
    for tx in range(cv.w):
        cv.r(tx * T, 0, 1, 3 * T, C['wall2'])                         # garis nat panel
    for i in (1, 2, 3):                                               # kaca patri kiri-kanan salib
        for sx in (round(front * T - i * 4 * T - 11), round(front * T + i * 4 * T - 11)):
            if 0 <= sx <= w - 22:
                holy_window(cv, sx, 5)
    wall_cross(cv, round(front * T), 6)
    cv.r(0, 3 * T - 7, w, 5, C['wood'])                               # lis kayu
    cv.r(0, 3 * T - 2, w, 2, C['out'])
    for ty in range(3, cv.h - 2):                                     # lantai
        a, b = (C['cream'], C['stone']) if ty < 7 else (C['wall2'], C['walk2'])
        for tx in range(cv.w):
            cv.r(tx * T, ty * T, T, T, a if (tx + ty) % 2 else b)
            cv.r(tx * T, ty * T + T - 1, T, 1, C['grout'])
            cv.r(tx * T + T - 1, ty * T, 1, T, C['grout'])
    cv.shade(0, 3 * T, w, 6)                                          # bayangan dinding
    cv.r(0, 7 * T - 4, w, 4, C['stone2'])                             # bibir panggung altar
    cv.r(0, 7 * T, w, 2, C['out'])
    cx = round((front - 2) * T)                                       # karpet merah lorong tengah
    cv.r(cx, 3 * T, 4 * T, (cv.h - 5) * T, C['red'])
    cv.r(cx, 3 * T, 2, (cv.h - 5) * T, C['brown'])
    cv.r(cx + 4 * T - 2, 3 * T, 2, (cv.h - 5) * T, C['brown'])
    for ry in range(3 * T + 6, (cv.h - 2) * T, 12):
        cv.r(cx + 3, ry, 4 * T - 6, 2, C['roof2'])                    # tenunan karpet
    y = (cv.h - 2) * T                                                # dinding depan
    cv.r(0, y, w, 2 * T, C['wall2'])
    cv.r(0, y, w, 3, C['out'])
    cv.r(0, y + 3, w, 5, C['wood'])
    dx = round((front - 1.5) * T)                                     # pintu ganda keluar
    cv.box(dx, y + 8, 3 * T, 2 * T - 12, C['wood2'])
    cv.r(dx + 3, y + 11, 3 * T - 6, 2 * T - 18, C['wood'])
    cv.r(dx + 3 * T // 2 - 1, y + 11, 2, 2 * T - 18, C['wood2'])
    for kx in (dx + 3 * T // 2 - 7, dx + 3 * T // 2 + 5):
        cv.r(kx, y + T + 2, 3, 5, C['gold'])                          # gagang pintu



# ─────────────────────── GG Learning Center ───────────────────────
# Ruang kelas bimbel: dinding putih 4 baris (poster mapel, banner SoCal, logo + layar
# sambutan, papan rencana, rak buku, mural maskot), lantai papan kayu terang, meja
# resepsionis di depan kelas, dan dua kolom meja belajar berlaptop.

def corgi(cv, x, y):
    """Maskot korgi GG (28 x 28) — dipakai di layar sambutan dan sebagai mural dinding."""
    cv.box(x + 4, y + 10, 20, 15, C['gold'])                                  # badan
    cv.r(x + 7, y + 15, 14, 9, C['cream'])                                    # dada
    cv.d.polygon([(x + 5, y + 4), (x + 8, y - 3), (x + 12, y + 4)], fill=C['gold'], outline=C['out'])
    cv.d.polygon([(x + 16, y + 4), (x + 20, y - 3), (x + 23, y + 4)], fill=C['gold'], outline=C['out'])
    cv.box(x + 5, y + 2, 18, 13, C['gold'])                                   # kepala
    cv.r(x + 9, y + 8, 10, 6, C['cream'])                                     # moncong
    cv.r(x + 9, y + 5, 2, 2, C['out'])
    cv.r(x + 17, y + 5, 2, 2, C['out'])
    cv.r(x + 13, y + 9, 3, 2, C['out'])
    cv.box(x + 3, y + 19, 22, 7, C['navy'])                                   # buku yang dipegang
    cv.r(x + 13, y + 20, 2, 5, C['cream'])


def gg_posters(cv, x, y):
    """Empat bingkai mapel (Math, English, Mandarin, AI & Coding) di dinding kiri."""
    icon = (C['blue2'], C['warm'], C['navy2'], C['gold'])
    for i in range(4):
        fx, fy = x + (i % 2) * 46, y + (i // 2) * 26
        cv.box(fx, fy, 42, 23, C['cream'], C['wood'])
        cv.box(fx + 4, fy + 5, 13, 13, icon[i])
        for ly in range(fy + 6, fy + 18, 5):
            cv.r(fx + 22, ly, 15, 2, C['grey'])


def gg_banner(cv, x, y, h):
    """Banner navy 'From Southern California' dengan siluet palem emas."""
    cv.box(x, y, 44, h, C['navy'])
    cv.text(x, y + 3, 44, 'SOCAL', C['gold'])
    for bx in (x + 9, x + 22, x + 33):
        cv.r(bx, y + h - 26, 2, 18, C['gold'])
        for dx in (-7, 7):
            cv.d.line([bx + 1, y + h - 25, bx + 1 + dx, y + h - 30], fill=C['gold'], width=2)
    cv.d.polygon([(x + 2, y + h - 8), (x + 16, y + h - 18), (x + 30, y + h - 8)], fill=C['gold'])
    cv.r(x + 2, y + h - 9, 40, 7, C['gold'])


def gg_screen(cv, x, y):
    """Layar sambutan di dinding depan kelas (160 x 44)."""
    cv.box(x, y, 160, 42, C['grey2'])
    cv.r(x + 3, y + 3, 154, 36, C['navy'])
    cv.text(x + 4, y + 2, 88, 'WELCOME TO', C['white'])
    cv.text(x + 4, y + 13, 88, 'GG LEARNING', C['gold'])
    cv.text(x + 4, y + 24, 88, 'CENTER', C['gold'])
    corgi(cv, x + 120, y + 7)


def gg_board(cv, x, y):
    """Papan putih 'TODAY'S PLAN' dengan empat centang mapel."""
    cv.box(x, y, 92, 48, C['white'], C['grey2'])
    cv.text(x, y + 2, 92, "TODAY'S PLAN", C['navy'])
    cv.r(x + 9, y + 16, 74, 1, C['navy'])
    for i in range(4):
        ly = y + 21 + i * 7
        cv.box(x + 11, ly, 5, 5, C['white'], C['navy2'])
        cv.r(x + 12, ly + 2, 3, 1, C['green'])
        cv.r(x + 20, ly + 2, 42 - i * 5, 2, C['grey'])
    cv.r(x + 7, y + 44, 78, 3, C['grey2'])                                    # rel spidol


def gg_shelf(cv, x, y, h):
    """Rak buku kayu terang menempel dinding kanan."""
    cv.box(x, y, 40, h, C['wood'])
    cv.r(x + 3, y + 3, 34, h - 6, C['dirt'])
    books = (C['navy'], C['red'], C['gold'], C['teal'], C['white'], C['green'])
    step = (h - 8) // 4
    for j in range(4):
        sy = y + 5 + j * step
        for i in range(6):
            cv.r(x + 4 + i * 6, sy + 2, 4, step - 6, books[(i + j) % 6])
        cv.r(x + 3, sy + step - 4, 34, 2, C['wood2'])


def p_ggdesk(cv, x, y):
    """Meja resepsionis GG (8 petak): badan putih berlogo emas + daun meja kayu terang."""
    cv.box(x, y, 128, 32, C['white'])
    cv.r(x + 3, y + 3, 122, 26, C['cream'])
    cv.text(x, y + 11, 128, 'GG', C['gold'], FONT_SIGN)
    cv.box(x - 5, y - 7, 138, 9, C['dirt'])                                   # daun meja
    cv.box(x + 86, y - 21, 28, 15, C['grey'])                                 # laptop
    cv.r(x + 88, y - 19, 24, 11, C['glass2'])
    cv.shade(x - 5, y + 32, 138, 4)
    return (x - 5, y - 2, 138, 34)


def p_studydesk(cv, x, y):
    """Meja belajar 10 petak: dua laptop GG + tetikus dan dua kursi navy di depannya.
    Penghalangnya menutup satu baris penuh (3 petak + laptop) supaya deretan meja menyatu."""
    cv.box(x, y + 6, 160, 16, C['dirt'])                                      # daun meja
    cv.r(x + 3, y + 9, 154, 6, C['walk'])
    for lx in (x + 18, x + 94):
        cv.box(lx, y - 9, 34, 16, C['grey2'])                                 # laptop
        cv.r(lx + 2, y - 7, 30, 12, C['navy'])
        cv.text(lx, y - 7, 34, 'GG', C['gold'])
        cv.box(lx + 38, y + 2, 8, 5, C['white'])                              # tetikus
    for kx in (x + 4, x + 151):
        cv.r(kx, y + 22, 5, 11, C['grey'])                                    # kaki meja
    for cx in (x + 30, x + 106):
        cv.box(cx, y + 25, 30, 9, C['navy'])                                  # sandaran kursi
        cv.r(cx + 3, y + 34, 24, 5, C['navy2'])                               # dudukan
        cv.r(cx + 5, y + 39, 3, 5, C['grey'])
        cv.r(cx + 22, y + 39, 3, 5, C['grey'])
    cv.shade(x, y + 44, 160, 4)
    return (x, y - 8, 160, 3 * T + 8)


def gg_room(cv, front):
    """Gambar seluruh ruang kelas. front = petak tengah lorong sekaligus tengah pintu keluar.
    Baris 0-3 dinding belakang, 4 s.d. tinggi-3 lantai, dua baris terbawah dinding depan."""
    w, h = cv.w * T, cv.h * T
    top = 4 * T
    cxp = round(front * T)
    cv.r(0, 0, w, top, C['white'])                                            # dinding putih
    cv.r(0, 0, w, 3, C['wall2'])                                              # lis plafon
    gg_posters(cv, 8, 3)
    gg_banner(cv, 104, 3, top - 12)
    cv.text(cxp - 84, 2, 168, 'GG GENIUSGROWTH.AI', C['gold'])
    gg_screen(cv, cxp - 80, 15)
    gg_board(cv, cxp + 90, 9)
    gg_shelf(cv, cxp + 186, 3, top - 12)
    corgi(cv, cxp + 232, 22)                                                  # mural maskot
    cv.r(0, top - 7, w, 5, C['navy'])                                         # lis lantai navy
    cv.r(0, top - 2, w, 2, C['out'])
    cv.r(0, top, w, (cv.h - 2) * T - top, C['walk'])                          # lantai papan kayu
    for i, py in enumerate(range(top, (cv.h - 2) * T, 10)):
        cv.r(0, py, w, 1, C['dirt'])                                          # nat antar papan
        for sx in range((i % 4) * 40, w, 160):                                # sambungan papan
            cv.r(sx, py, 1, 10, C['walk2'])
    cv.shade(0, top, w, 6)                                                    # bayangan dinding
    y = (cv.h - 2) * T                                                        # dinding depan berkaca
    cv.r(0, y, w, 2 * T, C['wall2'])
    cv.r(0, y, w, 3, C['out'])
    cv.r(0, y + 3, w, 5, C['navy'])
    for tx in range(0, cv.w - 1, 3):
        if abs(tx * T + T - cxp) <= 2 * T:
            continue
        cv.box(tx * T + 3, y + 10, 2 * T - 6, T + 1, C['glass'])
        cv.r(tx * T + T, y + 12, 2, T - 3, C['wall2'])
    dx = cxp - round(1.5 * T)                                                 # pintu kaca ganda
    cv.box(dx, y + 8, 3 * T, 2 * T - 12, C['grey2'])
    cv.r(dx + 3, y + 11, 3 * T - 6, 2 * T - 18, C['glass2'])
    cv.r(dx + 3 * T // 2 - 1, y + 11, 2, 2 * T - 18, C['wall2'])
    cv.text(dx, y + T - 4, 3 * T, 'GG', C['gold'])


# ─────────────────────────── minimarket ───────────────────────────
# Toko swalayan: dinding belakang berpita merah-biru dengan lemari pendingin, lantai
# ubin biru bermotif wajik, dua deret rak barang mengapit lorong, dan meja kasir di
# dekat pintu. Dipakai Minimarket 24; minimarket kedua tinggal menambah map baru.

def mart_tile(cv, x, y, tile=('sky2', 'sky', 'white')):
    """Satu ubin lantai: dasar berwarna dengan wajik di tengah dan nat lebih tua."""
    cv.r(x, y, T, T, C[tile[0]])
    cv.d.polygon([(x + 8, y + 2), (x + 14, y + 8), (x + 8, y + 14), (x + 2, y + 8)], fill=C[tile[2]])
    cv.r(x, y + T - 1, T, 1, C[tile[1]])
    cv.r(x + T - 1, y, 1, T, C[tile[1]])


def p_fridge(cv, x, y):
    """Lemari pendingin minuman berpintu kaca (4 x 3,5 petak)."""
    drink = (C['green'], C['red'], C['gold'], C['blue2'], C['teal2'], C['pink'])
    cv.box(x, y, 64, 56, C['grey2'])
    cv.r(x + 3, y + 3, 58, 46, C['glass2'])
    for j in range(3):
        sy = y + 6 + j * 14
        for i in range(8):
            cv.r(x + 6 + i * 7, sy, 5, 10, drink[(i + j) % 6])
        cv.r(x + 4, sy + 10, 56, 2, C['grey'])
    cv.r(x + 31, y + 3, 2, 46, C['white'])                # celah antar pintu
    cv.r(x + 26, y + 24, 3, 9, C['grey'])                 # gagang
    cv.r(x + 35, y + 24, 3, 9, C['grey'])
    cv.box(x, y + 49, 64, 7, C['blue'])                   # kaki
    cv.shade(x, y + 56, 64, 4)
    return (x, y + 42, 64, 16)


def p_gondola(cv, x, y):
    """Rak barang 10 petak: tiga tingkat produk warna-warni, dasar biru merek."""
    goods = (C['red'], C['gold'], C['green'], C['blue2'], C['pink'], C['warm'], C['teal2'])
    cv.box(x, y, 160, 46, C['white'])
    for j in range(3):
        sy = y + 4 + j * 13
        for i in range(19):
            cv.r(x + 5 + i * 8, sy, 6, 9, goods[(i + j * 2) % 7])
        cv.r(x + 3, sy + 9, 154, 3, C['grey'])
    cv.box(x, y + 43, 160, 7, C['blue'])
    cv.shade(x, y + 50, 160, 4)
    return (x, y, 160, 2 * T)


def p_cashier(cv, x, y):
    """Meja kasir 6 petak: badan biru, mesin kasir, dan rak permen."""
    cv.box(x, y, 96, 30, C['blue'])
    cv.r(x + 3, y + 3, 90, 11, C['blue2'])
    cv.box(x - 4, y - 7, 104, 9, C['white'])              # daun meja
    cv.box(x + 62, y - 23, 26, 17, C['grey'])             # mesin kasir
    cv.r(x + 65, y - 20, 20, 8, C['glass2'])
    cv.r(x + 65, y - 11, 20, 3, C['grey2'])
    cv.box(x + 10, y - 19, 22, 13, C['red'])              # rak permen
    cv.r(x + 13, y - 16, 16, 3, C['gold'])
    cv.shade(x - 4, y + 30, 104, 4)
    return (x - 4, y - 2, 104, 34)


def p_mat(cv, x, y):
    """Keset merah di depan pintu — hiasan, bukan penghalang."""
    cv.box(x, y, 64, 16, C['red'])
    cv.r(x + 4, y + 4, 56, 8, C['roof2'])
    return None


def mart_room(cv, front, name='MINIMARKET 24', band=('red', 'blue', 'cream'),
              tile=('sky2', 'sky', 'white')):
    """Gambar seluruh ruang minimarket. front = petak tengah lorong sekaligus tengah pintu.
    Baris 0-2 dinding belakang, 3 s.d. tinggi-3 lantai, dua baris terbawah dinding depan.
    name/band/tile datang dari MAPS, jadi template ini dipakai ulang tiap gerai dengan
    nama, pita merek, dan warna ubin sendiri."""
    w, h = cv.w * T, cv.h * T
    top, trim, glow = (C[c] for c in band)
    cv.r(0, 0, w, 3 * T, C['white'])                                   # dinding belakang
    cv.r(0, 0, w, 5, C['wall2'])
    cv.r(0, 7, w, 7, top)                                              # pita merek
    cv.r(0, 14, w, 4, trim)
    for lx in range(20, w, 110):                                       # lampu neon
        cv.r(lx, 22, 70, 4, glow)
    cv.text(round(front * T) - 90, 26, 180, name, trim)
    cv.r(0, 3 * T - 7, w, 5, trim)                                     # lis lantai
    cv.r(0, 3 * T - 2, w, 2, C['out'])
    for ty in range(3, cv.h - 2):                                      # lantai ubin
        for tx in range(cv.w):
            mart_tile(cv, tx * T, ty * T, tile)
    cv.shade(0, 3 * T, w, 6)
    y = (cv.h - 2) * T                                                 # dinding depan berkaca
    cv.r(0, y, w, 2 * T, C['wall2'])
    cv.r(0, y, w, 3, C['out'])
    cv.r(0, y + 3, w, 5, trim)
    cxp = round(front * T)
    for tx in range(0, cv.w - 1, 3):
        if abs(tx * T + T - cxp) <= 2 * T:
            continue
        cv.box(tx * T + 3, y + 10, 2 * T - 6, T + 1, C['glass'])
        cv.r(tx * T + T, y + 12, 2, T - 3, C['wall2'])
    dx = cxp - round(1.5 * T)                                          # pintu kaca geser
    cv.box(dx, y + 8, 3 * T, 2 * T - 12, C['grey2'])
    cv.r(dx + 3, y + 11, 3 * T - 6, 2 * T - 18, C['glass2'])
    cv.r(dx + 3 * T // 2 - 1, y + 11, 2, 2 * T - 18, C['wall2'])
    cv.r(dx + 3 * T // 2 - 8, y + T + 2, 5, 3, C['grey'])
    cv.r(dx + 3 * T // 2 + 3, y + T + 2, 5, 3, C['grey'])


# ─────────────────────────── FFC (gerai ayam goreng) ───────────────────────────
# Restoran cepat saji di Pusat Kota. Dinding belakang bata merah dengan jendela dapur,
# papan menu bercahaya, dan plakat ember FFC; lantai ubin catur krem-abu; meja pesan
# panjang membelah ruangan, area makan (meja + bilik) di bawahnya. Warnanya sengaja
# mengikuti fasad merah-emas di lukisan Jalan Pusat Kota.

def ffc_brickwall(cv, w):
    """Dinding belakang bata merah 3 petak: nat gelap berselang + lis krem di kakinya."""
    cv.r(0, 0, w, 3 * T, C['brick'])
    for row, ty in enumerate(range(4, 3 * T - 10, 7)):
        cv.r(0, ty + 6, w, 1, C['brown'])                 # nat mendatar
        for bx in range(0 if row % 2 else 14, w, 28):
            cv.r(bx, ty, 1, 7, C['brown'])                # nat tegak, digeser tiap baris
        cv.r(0, ty, w, 1, C['brick2'])
    cv.r(0, 0, w, 4, C['out'])
    cv.r(0, 3 * T - 11, w, 9, C['cream'])                 # lis krem di kaki dinding
    cv.r(0, 3 * T - 11, w, 2, C['gold'])
    cv.r(0, 3 * T - 2, w, 2, C['out'])


def ffc_tile(cv, x, y, accent):
    """Satu ubin lantai krem; satu dari empat ubin diberi wajik merah kecil."""
    cv.r(x, y, T, T, C['cream'])
    if accent:
        cv.d.polygon([(x + 8, y + 3), (x + 13, y + 8), (x + 8, y + 13), (x + 3, y + 8)], fill=C['roof3'])
        cv.d.polygon([(x + 8, y + 5), (x + 11, y + 8), (x + 8, y + 11), (x + 5, y + 8)], fill=C['red'])
    cv.r(x, y + T - 1, T, 1, C['stone'])
    cv.r(x + T - 1, y, 1, T, C['stone'])


def ffc_room(cv, front):
    """Gambar seluruh ruang FFC. front = petak tengah pintu kaca di dinding depan.
    Baris 0-2 dinding belakang, 3 s.d. tinggi-3 lantai, dua baris terbawah dinding depan."""
    w = cv.w * T
    ffc_brickwall(cv, w)
    for ty in range(3, cv.h - 2):
        for tx in range(cv.w):
            ffc_tile(cv, tx * T, ty * T, tx % 2 == 0 and ty % 2 == 1)
    cv.shade(0, 3 * T, w, 6)                              # bayangan dinding di lantai
    y = (cv.h - 2) * T                                    # dinding depan: merah + pita emas
    cv.r(0, y, w, 2 * T, C['roof2'])
    cv.r(0, y, w, 3, C['out'])
    cv.r(0, y + 3, w, 6, C['gold'])
    cxp = round(front * T)
    for tx in range(0, cv.w - 1, 3):                      # etalase kaca kiri-kanan pintu
        if abs(tx * T + T - cxp) <= 2 * T:
            continue
        cv.box(tx * T + 3, y + 12, 2 * T - 6, T - 2, C['glass'])
        cv.r(tx * T + T, y + 14, 2, T - 6, C['wall2'])
    dx = cxp - round(1.5 * T)                             # pintu kaca ganda
    cv.box(dx, y + 10, 3 * T, 2 * T - 14, C['grey2'])
    cv.r(dx + 3, y + 13, 3 * T - 6, 2 * T - 20, C['glass2'])
    cv.r(dx + 3 * T // 2 - 1, y + 13, 2, 2 * T - 20, C['wall2'])
    cv.r(dx + 3 * T // 2 - 8, y + T + 3, 5, 3, C['gold'])
    cv.r(dx + 3 * T // 2 + 3, y + T + 3, 5, 3, C['gold'])


def p_ffckitchen(cv, x, y):
    """Jendela dapur di dinding belakang (9 petak): lampu penghangat, nampan ayam, meja saji."""
    cv.box(x, y + 4, 144, 40, C['grey2'])
    cv.r(x + 4, y + 8, 136, 32, C['grey'])                # dinding stainless dapur
    cv.r(x + 4, y + 8, 136, 4, C['gold'])                 # bilah lampu penghangat
    cv.r(x + 4, y + 12, 136, 2, C['warm'])
    for i in range(4):
        cv.r(x + 12 + i * 34, y + 14, 3, 4, C['warm'])
    for i in range(6):                                    # nampan ayam goreng
        bx = x + 9 + i * 22
        cv.box(bx, y + 18, 18, 10, C['grey'])
        cv.r(bx + 2, y + 20, 14, 6, C['warm'])
        cv.r(bx + 4, y + 21, 4, 3, C['gold'])
        cv.r(bx + 10, y + 23, 4, 2, C['gold'])
    cv.r(x + 4, y + 31, 136, 9, C['grey'])                # meja saji stainless
    cv.r(x + 4, y + 31, 136, 2, C['white'])
    return None                                           # menempel dinding, bukan penghalang


def p_ffcmenu(cv, x, y):
    """Papan menu bercahaya (11 petak): tiga panel foto paket + deret harga."""
    cv.box(x, y + 5, 176, 36, C['brown'])
    for i in range(3):
        px = x + 5 + i * 57
        cv.r(px, y + 9, 53, 28, C['red'])
        cv.r(px + 2, y + 11, 49, 12, C['cream'])          # foto paket
        cv.r(px + 5, y + 15, 13, 7, C['warm'])            # ayam
        cv.r(px + 7, y + 14, 9, 3, C['gold'])
        cv.r(px + 21, y + 16, 10, 6, C['gold'])           # kentang
        cv.r(px + 34, y + 13, 11, 9, C['white'])          # gelas
        cv.r(px + 36, y + 15, 7, 2, C['red'])
        for j in range(3):                                # baris nama + harga
            cv.r(px + 4, y + 26 + j * 4, 29, 2, C['cream'])
            cv.r(px + 40, y + 26 + j * 4, 9, 2, C['gold'])
    cv.r(x + 2, y + 2, 172, 3, C['gold'])                 # lampu sorot
    return None


def p_ffclogo(cv, x, y):
    """Plakat ember ayam + tulisan FFC di dinding belakang (7 petak)."""
    cv.box(x, y + 6, 112, 34, C['cream'])
    cv.r(x + 4, y + 10, 104, 26, C['red'])
    bx, by = x + 11, y + 14
    cv.d.polygon([(bx, by), (bx + 30, by), (bx + 26, by + 19), (bx + 4, by + 19)], fill=C['white'])
    for i in range(3):                                    # garis merah ember
        cv.d.polygon([(bx + 5 + i * 9, by), (bx + 8 + i * 9, by),
                      (bx + 6 + i * 9, by + 19), (bx + 3 + i * 9, by + 19)], fill=C['red'])
    cv.r(bx + 1, by - 3, 29, 4, C['gold'])
    cv.r(bx + 6, by - 6, 18, 4, C['warm'])                # ayam menyembul dari ember
    cv.text(x + 46, y + 14, 60, 'FFC', C['gold'])
    cv.r(x + 50, y + 30, 52, 2, C['gold'])
    return None


def p_ffccounter(cv, x, y):
    """Meja pesan 10 petak: badan merah berlis emas, daun krem, dua mesin kasir, nampan antar."""
    cv.box(x, y, 160, 30, C['red'])
    cv.r(x + 3, y + 6, 154, 4, C['gold'])
    cv.r(x + 3, y + 14, 154, 12, C['roof2'])
    cv.box(x - 4, y - 8, 168, 10, C['cream'])             # daun meja
    cv.r(x - 1, y - 6, 162, 3, C['white'])
    for mx in (x + 16, x + 116):                          # mesin kasir
        cv.box(mx, y - 24, 26, 17, C['grey2'])
        cv.r(mx + 3, y - 21, 20, 8, C['glass2'])
        cv.r(mx + 3, y - 12, 20, 3, C['grey'])
    cv.box(x + 62, y - 18, 36, 11, C['roof2'])            # nampan pesanan
    cv.r(x + 66, y - 15, 13, 5, C['warm'])
    cv.r(x + 82, y - 15, 12, 5, C['gold'])
    cv.shade(x - 4, y + 30, 168, 4)
    return (x - 4, y - 2, 168, 34)


def p_ffcdrink(cv, x, y):
    """Dispenser minuman swalayan (6 petak): empat keran, gelas kertas, meja merah."""
    cv.box(x, y, 96, 34, C['grey2'])
    cv.r(x + 4, y + 4, 88, 26, C['navy'])
    for i, col in enumerate(('red', 'gold', 'brown', 'teal2')):
        tx = x + 9 + i * 21
        cv.r(tx, y + 8, 15, 12, C[col])
        cv.r(tx + 5, y + 21, 4, 6, C['grey'])             # keran
    cv.box(x, y + 34, 96, 12, C['red'])                   # meja
    cv.r(x + 3, y + 36, 90, 4, C['gold'])
    for i in range(3):                                    # tumpukan gelas kertas
        cv.box(x + 58 + i * 12, y + 26, 9, 10, C['white'])
        cv.r(x + 60 + i * 12, y + 28, 5, 2, C['red'])
    cv.shade(x, y + 46, 96, 4)
    return (x, y + 30, 96, 20)


def p_ffctray(cv, x, y):
    """Stasiun kembalikan nampan (3 petak): lubang sampah, papan TRAY, tumpukan nampan."""
    cv.box(x, y + 8, 48, 32, C['grey2'])
    cv.r(x + 5, y + 12, 38, 7, C['out'])                  # lubang buang
    cv.r(x + 5, y + 23, 38, 12, C['red'])
    cv.text(x + 5, y + 23, 38, 'TRAY', C['cream'], FONT_SMALL)
    for i in range(4):                                    # nampan bersih bertumpuk
        cv.box(x + 7, y + 1 + i * 2, 34, 4, C['roof2'])
    cv.shade(x, y + 40, 48, 4)
    return (x, y + 26, 48, 18)


def p_ffctable(cv, x, y):
    """Meja makan dua kursi (5 petak): daun krem, kursi merah kiri-kanan, nampan di atasnya."""
    for sx in (x, x + 62):                                # kursi
        cv.box(sx, y + 10, 18, 22, C['red'])
        cv.r(sx + 3, y + 13, 12, 8, C['roof2'])
    cv.box(x + 18, y + 6, 44, 26, C['cream'])             # daun meja
    cv.r(x + 21, y + 9, 38, 8, C['white'])
    cv.box(x + 30, y + 17, 21, 9, C['roof2'])             # nampan
    cv.r(x + 33, y + 19, 7, 5, C['warm'])
    cv.r(x + 42, y + 19, 6, 5, C['gold'])
    cv.r(x + 36, y + 32, 9, 7, C['wood2'])                # tiang + kaki meja
    cv.box(x + 30, y + 37, 21, 6, C['wood'])
    cv.shade(x, y + 43, 80, 4)
    return (x, y + 26, 80, 20)


def p_ffcbooth(cv, x, y):
    """Bilik makan (6 petak): dua sandaran merah mengapit meja kayu panjang."""
    cv.box(x, y, 96, 14, C['red'])                        # sandaran belakang
    cv.r(x + 3, y + 3, 90, 6, C['roof2'])
    cv.box(x + 6, y + 12, 84, 11, C['wood'])              # meja
    cv.r(x + 9, y + 14, 78, 4, C['wood3'])
    cv.box(x, y + 21, 96, 13, C['red'])                   # sandaran depan
    cv.r(x + 3, y + 24, 90, 5, C['roof2'])
    cv.shade(x, y + 34, 96, 4)
    return (x, y + 14, 96, 20)


def p_ffcstandee(cv, x, y):
    """Papan promo berdiri (3 petak): ember ayam + tulisan PAKET."""
    cv.box(x, y, 48, 40, C['cream'])
    cv.r(x + 3, y + 3, 42, 24, C['red'])
    cv.d.polygon([(x + 13, y + 7), (x + 35, y + 7), (x + 32, y + 24), (x + 16, y + 24)], fill=C['white'])
    for i in range(3):
        cv.d.polygon([(x + 17 + i * 7, y + 7), (x + 20 + i * 7, y + 7),
                      (x + 18 + i * 7, y + 24), (x + 15 + i * 7, y + 24)], fill=C['red'])
    cv.r(x + 18, y + 4, 13, 4, C['warm'])
    cv.text(x + 3, y + 27, 42, 'PAKET', C['brown'], FONT_SMALL)
    cv.box(x + 18, y + 40, 12, 6, C['grey'])              # penyangga
    cv.shade(x, y + 46, 48, 3)
    return (x, y + 34, 48, 14)


# ─────────────────────────── masjid ───────────────────────────
# Ruang salat: dinding kiblat berjendela lengkung dengan relung mihrab di tengah, panggung
# marmer imam, tiga saf karpet sajadah, lalu serambi dalam berubin tempat rak sandal dan
# kotak amal — persis urutan yang dilihat pemain begitu pintu ganda terbuka.

def arch_window(cv, x, y, w=26, h=42):
    """Jendela lengkung hijau di dinding kiblat."""
    cv.d.pieslice([x, y, x + w - 1, y + w - 1], 180, 360, fill=C['out'])
    cv.r(x, y + w // 2, w, h - w // 2, C['out'])
    cv.d.pieslice([x + 2, y + 2, x + w - 3, y + w - 3], 180, 360, fill=C['green'])
    cv.r(x + 2, y + w // 2, w - 4, h - w // 2 - 2, C['green'])
    cv.d.pieslice([x + 6, y + 6, x + w - 7, y + w - 7], 180, 360, fill=C['glass2'])
    cv.r(x + 6, y + w // 2, w - 12, h - w // 2 - 6, C['glass2'])
    cv.r(x + w // 2 - 1, y + 9, 2, h - 13, C['green2'])   # kisi tengah
    cv.r(x + 2, y + h - 4, w - 4, 3, C['green2'])         # ambang bawah


def sajadah(cv, x, y, w, h):
    """Satu petak sajadah: bingkai emas, bidang hijau, lengkung mihrab kecil di tengahnya."""
    cv.box(x, y, w, h, C['gold'])
    cv.r(x + 2, y + 2, w - 4, h - 4, C['green'])
    cx = x + w // 2
    cv.d.pieslice([cx - 8, y + 4, cx + 7, y + 19], 180, 360, fill=C['leaf2'])
    cv.r(cx - 8, y + 11, 16, h - 16, C['leaf2'])
    cv.d.pieslice([cx - 6, y + 6, cx + 5, y + 17], 180, 360, fill=C['green2'])
    cv.r(cx - 6, y + 11, 12, h - 18, C['green2'])
    cv.r(cx - 1, y + 9, 2, h - 15, C['leaf2'])            # sumbu lengkung


def p_mihrab(cv, x, y):
    """Relung mihrab berlapis emas di tengah dinding kiblat — tempat imam memimpin salat."""
    w, h = 64, 78
    cv.d.pieslice([x, y, x + w - 1, y + w - 1], 180, 360, fill=C['out'])
    cv.r(x, y + w // 2, w, h - w // 2, C['out'])
    cv.d.pieslice([x + 3, y + 3, x + w - 4, y + w - 4], 180, 360, fill=C['gold'])
    cv.r(x + 3, y + w // 2, w - 6, h - w // 2 - 3, C['gold'])
    cv.d.pieslice([x + 8, y + 8, x + w - 9, y + w - 9], 180, 360, fill=C['green'])
    cv.r(x + 8, y + w // 2, w - 16, h - w // 2 - 8, C['green'])
    cv.d.pieslice([x + 14, y + 14, x + w - 15, y + w - 15], 180, 360, fill=C['teal'])
    cv.r(x + 14, y + w // 2, w - 28, h - w // 2 - 14, C['teal'])
    cv.r(x + 24, y + 26, 16, 2, C['gold'])                # kaligrafi kecil di dalam relung
    cv.r(x + 26, y + 32, 12, 2, C['gold'])
    cv.shade(x + 8, y + h - 6, w - 16, 6)
    return (x + 6, y + h - 14, w - 12, 14)                # relungnya bukan tempat berdiri


def p_mimbar(cv, x, y):
    """Mimbar kayu bertangga, tempat khatib menyampaikan khotbah Jumat."""
    cv.d.pieslice([x + 11, y - 34, x + 48, y - 6], 180, 360, fill=C['out'])   # kanopi lengkung
    cv.d.pieslice([x + 13, y - 32, x + 46, y - 8], 180, 360, fill=C['wood'])
    cv.r(x + 28, y - 40, 3, 9, C['gold'])                 # puncak kanopi
    cv.box(x + 12, y - 12, 36, 14, C['wood'])             # sandaran atas
    cv.r(x + 15, y - 9, 30, 7, C['wood3'])
    cv.box(x + 10, y, 38, 40, C['wood2'])                 # badan mimbar
    cv.r(x + 13, y + 3, 32, 26, C['wood'])
    cv.r(x + 17, y + 8, 24, 4, C['gold'])
    for i in range(3):                                    # tangga naik dari kiri
        cv.box(x + 8 - i * 4, y + 14 + i * 9, 8 + i * 4, 11, C['wood'])
        cv.r(x + 10 - i * 4, y + 16 + i * 9, 4 + i * 4, 3, C['wood3'])
    cv.shade(x, y + 40, 48, 4)
    return (x, y + 20, 48, 24)


def p_quranrack(cv, x, y):
    """Rak kitab dua tingkat: mushaf Al-Qur'an bersusun rapi."""
    books = (C['green'], C['teal'], C['gold'], C['brown'], C['green2'])
    cv.box(x, y, 40, 34, C['wood2'])
    for j in range(2):
        cv.r(x + 3, y + 4 + j * 15, 34, 13, C['wood'])
        for i in range(8):
            cv.r(x + 4 + i * 4, y + 5 + j * 15, 3, 11, books[(i + j) % 5])
    cv.shade(x, y + 34, 40, 3)
    return (x, y + 20, 40, 17)


def p_donation(cv, x, y):
    """Kotak amal kayu berkaki dengan lubang koin di atasnya."""
    cv.box(x, y, 28, 26, C['wood2'])
    cv.r(x + 3, y + 3, 22, 20, C['wood'])
    cv.r(x + 9, y + 1, 10, 3, C['out'])
    cv.r(x + 6, y + 10, 16, 6, C['gold'])
    for lx in (x + 2, x + 22):
        cv.r(lx, y + 26, 4, 8, C['wood2'])
    cv.shade(x, y + 34, 28, 3)
    return (x, y + 18, 28, 18)


def p_shoerack(cv, x, y):
    """Rak sandal dua tingkat di serambi dalam."""
    sandals = (C['brown'], C['grey2'], C['teal2'], C['red'])
    cv.box(x, y, 56, 30, C['wood'])
    for j in range(2):
        cv.r(x + 3, y + 4 + j * 13, 50, 11, C['wood2'])
        for i in range(4):
            cv.r(x + 5 + i * 12, y + 6 + j * 13, 9, 7, sandals[(i + j) % 4])
    cv.shade(x, y + 30, 56, 3)
    return (x, y + 16, 56, 17)


def p_speaker(cv, x, y):
    """Pengeras suara di dinding kiblat."""
    cv.box(x, y, 18, 28, C['grey2'])
    cv.r(x + 3, y + 3, 12, 15, C['out'])
    for gy in range(y + 5, y + 17, 3):
        cv.r(x + 4, gy, 10, 1, C['grey'])
    cv.r(x + 5, y + 21, 8, 3, C['grey'])
    return None


def mosque_room(cv, front):
    """Gambar seluruh ruang salat. front = petak tengah mihrab sekaligus tengah pintu keluar.
    Baris 0-2 dinding kiblat, 3-4 panggung marmer imam, 5 s.d. tinggi-5 karpet bersaf,
    dua baris berikutnya serambi dalam berubin, dua baris terbawah dinding depan.
    Lantai yang bisa dilewati = baris 3 s.d. tinggi-3."""
    w, h = cv.w * T, cv.h * T
    cv.r(0, 0, w, 3 * T, C['wall'])                                   # dinding kiblat
    for tx in range(cv.w):
        cv.r(tx * T, 0, 1, 3 * T, C['wall2'])                         # garis nat panel
    cv.r(0, 0, w, 7, C['teal'])                                       # lajur hias atas
    for gx in range(0, w, 16):
        cv.r(gx + 6, 2, 4, 3, C['gold'])
    for i in (2, 3, 4, 5):                                            # jendela lengkung kiri-kanan mihrab
        for sx in (round(front * T - i * 3 * T - 13), round(front * T + i * 3 * T - 13)):
            if 0 <= sx <= w - 26:
                arch_window(cv, sx, 9)
    cv.r(0, 3 * T - 7, w, 5, C['wood'])                               # lis kayu
    cv.r(0, 3 * T - 2, w, 2, C['out'])
    for ty in range(3, 5):                                            # panggung marmer imam
        for tx in range(cv.w):
            cv.r(tx * T, ty * T, T, T, C['cream'] if (tx + ty) % 2 else C['stone'])
            cv.r(tx * T, ty * T + T - 1, T, 1, C['stone2'])
            cv.r(tx * T + T - 1, ty * T, 1, T, C['stone2'])
    cv.shade(0, 3 * T, w, 6)                                          # bayangan dinding
    cv.r(0, 5 * T - 4, w, 4, C['stone2'])                             # bibir panggung
    cv.r(0, 5 * T, w, 2, C['out'])
    porch = cv.h - 4                                                  # dua baris serambi dalam
    cv.r(0, 5 * T, w, (porch - 5) * T, C['green2'])                   # karpet
    for ry in range(5, porch, 2):                                     # tiap saf: garis emas + deret sajadah
        cv.r(0, ry * T, w, 2, C['gold'])
        for cx in range(4, w - 40, 48):                       # sajadah rapat sampai tepi kanan
            sajadah(cv, cx, ry * T + 5, 44, 2 * T - 9)
    cv.r(0, porch * T - 3, w, 3, C['gold'])                           # tepi karpet
    for ty in range(porch, cv.h - 2):                                 # serambi dalam berubin
        for tx in range(cv.w):
            cv.r(tx * T, ty * T, T, T, C['stone'] if (tx + ty) % 2 else C['walk2'])
            cv.r(tx * T, ty * T + T - 1, T, 1, C['grout'])
            cv.r(tx * T + T - 1, ty * T, 1, T, C['grout'])
    y = (cv.h - 2) * T                                                # dinding depan
    cv.r(0, y, w, 2 * T, C['wall2'])
    cv.r(0, y, w, 3, C['out'])
    cv.r(0, y + 3, w, 5, C['wood'])
    dx = round((front - 1.5) * T)                                     # pintu ganda keluar
    cv.box(dx, y + 8, 3 * T, 2 * T - 12, C['wood2'])
    cv.r(dx + 3, y + 11, 3 * T - 6, 2 * T - 18, C['wood'])
    cv.r(dx + 3 * T // 2 - 1, y + 11, 2, 2 * T - 18, C['wood2'])
    for kx in (dx + 3 * T // 2 - 7, dx + 3 * T // 2 + 5):
        cv.r(kx, y + T + 2, 3, 5, C['gold'])                          # gagang pintu


# ─────────────────────────── bank syariah ───────────────────────────
# Lobi bank: plafon bilah kayu + garis cahaya tosca, dinding bermotif mashrabiya dan
# papan logo, lantai marmer dengan medalion bunga di tengah, konter teller bernomor,
# meja resepsionis, sofa tunggu, mesin antrean, dan ATM.

def mashrabiya(cv, x, y, w, h):
    """Panel kisi geometris islami di dinding — motif belah ketupat berulang."""
    cv.box(x, y, w, h, C['wood'])
    cv.r(x + 2, y + 2, w - 4, h - 4, C['cream'])
    for gy in range(y + 4, y + h - 6, 10):
        for gx in range(x + 4, x + w - 6, 10):
            cv.d.polygon([(gx + 4, gy), (gx + 8, gy + 4), (gx + 4, gy + 8), (gx, gy + 4)],
                         fill=C['wood'], outline=C['wood2'])


def bank_medallion(cv, cx, cy):
    """Medalion bunga marmer di tengah lobi — hiasan lantai, bukan penghalang."""
    cv.d.ellipse([cx - 58, cy - 30, cx + 58, cy + 30], fill=C['stone'], outline=C['out'])
    cv.d.ellipse([cx - 52, cy - 26, cx + 52, cy + 26], fill=C['brown'])
    for dx, dy in ((0, -1), (.7, -.7), (1, 0), (.7, .7), (0, 1), (-.7, .7), (-1, 0), (-.7, -.7)):
        px_, py_ = cx + dx * 25, cy + dy * 12
        cv.d.ellipse([px_ - 15, py_ - 8, px_ + 15, py_ + 8], fill=C['cream'], outline=C['stone2'])
    cv.d.ellipse([cx - 13, cy - 8, cx + 13, cy + 8], fill=C['gold'], outline=C['out'])


def p_teller(cv, x, y):
    """Konter teller 12 petak: tiga loket bernomor dengan sekat kaca."""
    cv.box(x, y, 192, 32, C['wall2'])
    for i in range(3):
        bx = x + 8 + i * 62
        cv.box(bx, y + 3, 54, 27, C['glass2'])                # sekat kaca
        cv.text(bx, y + 3, 54, str(i + 1), C['tosca'])
        cv.r(bx + 6, y + 21, 42, 6, C['cream'])               # celah dokumen
    cv.box(x - 4, y + 32, 200, 20, C['cream'])                # meja konter
    cv.r(x - 1, y + 35, 194, 6, C['tosca2'])                  # lis pita tosca
    cv.shade(x - 4, y + 52, 200, 4)
    return (x - 4, y + 32, 200, 22)


def p_bankdesk(cv, x, y):
    """Meja resepsionis BSI: badan krem berlis cahaya dan papan logo berpendar."""
    cv.box(x + 12, y - 28, 104, 24, C['wall2'])               # papan logo
    cv.text(x + 12, y - 27, 104, 'BSI', C['tosca'])
    cv.r(x + 14, y - 8, 100, 3, C['tosca2'])
    cv.box(x, y, 128, 30, C['cream'])
    cv.r(x + 3, y + 3, 122, 9, C['white'])
    cv.r(x + 3, y + 25, 122, 4, C['gold'])                    # lis cahaya bawah
    cv.shade(x, y + 30, 128, 4)
    return (x, y, 128, 32)


def p_waitsofa(cv, x, y):
    """Sofa tunggu 8 petak dengan bantalan tosca dan meja bundar di ujungnya."""
    cv.box(x, y, 128, 13, C['stone'])                         # sandaran
    cv.box(x, y + 11, 128, 15, C['white'])                    # dudukan
    for i in range(3):
        cv.r(x + 7 + i * 40, y + 14, 34, 9, C['tosca2'])      # bantalan
    for lx in (x + 5, x + 118):
        cv.r(lx, y + 26, 5, 6, C['wood'])
    cv.d.ellipse([x + 98, y + 24, x + 126, y + 38], fill=C['brown'], outline=C['out'])
    cv.r(x + 110, y + 31, 3, 7, C['wood2'])
    cv.shade(x, y + 32, 128, 4)
    return (x, y + 11, 128, 24)


def p_queue(cv, x, y):
    """Mesin antrean: layar tosca dan slot tiket."""
    cv.box(x, y, 26, 46, C['grey'])
    cv.r(x + 4, y + 5, 18, 18, C['navy'])
    cv.r(x + 6, y + 7, 14, 14, C['tosca'])
    cv.r(x + 6, y + 27, 14, 4, C['white'])                    # slot tiket
    cv.box(x + 2, y + 38, 22, 8, C['grey2'])
    cv.shade(x, y + 46, 26, 3)
    return (x, y + 30, 26, 17)


def p_atm(cv, x, y):
    """Mesin ATM di sudut lobi."""
    cv.box(x, y, 40, 56, C['grey2'])
    cv.r(x + 4, y + 6, 32, 22, C['navy'])                     # layar
    cv.r(x + 6, y + 8, 28, 18, C['tosca'])
    cv.r(x + 9, y + 32, 22, 9, C['grey'])                     # tombol
    cv.r(x + 10, y + 45, 20, 4, C['gold'])                    # slot kartu
    cv.shade(x, y + 56, 40, 4)
    return (x, y + 42, 40, 16)


def bank_room(cv, front):
    """Gambar seluruh lobi bank. front = petak tengah lorong sekaligus tengah pintu keluar.
    Baris 0-2 dinding belakang, 3 s.d. tinggi-3 lantai marmer, dua baris terbawah dinding depan."""
    w, h = cv.w * T, cv.h * T
    cxp = round(front * T)
    cv.r(0, 0, w, 3 * T, C['wall2'])                                   # dinding taupe
    cv.r(0, 0, w, 7, C['wood'])                                        # plafon bilah kayu
    for sx in range(0, w, 6):
        cv.r(sx, 0, 2, 7, C['wood2'])
    cv.r(0, 9, w, 2, C['tosca2'])                                      # garis cahaya tosca
    for px_ in range(10, w - 110, 36):                                 # kisi mashrabiya
        if abs(px_ + 16 - cxp) < 110:
            continue
        mashrabiya(cv, px_, 13, 32, 3 * T - 22)
    cv.box(cxp - 104, 12, 208, 3 * T - 20, C['wall2'], C['wood'])      # papan logo dinding
    cv.text(cxp - 104, 15, 208, 'BSI  BANK SYARIAH', C['tosca'])
    cv.r(cxp - 76, 31, 152, 2, C['gold'])
    cv.r(cxp - 60, 35, 120, 2, C['tosca2'])
    cv.box(w - 96, 14, 76, 3 * T - 26, C['grey2'])                     # layar nomor antrean
    cv.r(w - 92, 18, 68, 3 * T - 36, C['navy'])
    cv.text(w - 92, 20, 68, 'A-07', C['tosca2'])
    cv.r(0, 3 * T - 7, w, 5, C['wood'])                                # lis lantai
    cv.r(0, 3 * T - 2, w, 2, C['out'])
    for ty in range(3, cv.h - 2):                                      # lantai marmer
        for tx in range(cv.w):
            cv.r(tx * T, ty * T, T, T, C['white'] if (tx + ty) % 2 else C['cream'])
            cv.r(tx * T, ty * T + T - 1, T, 1, C['stone'])
            cv.r(tx * T + T - 1, ty * T, 1, T, C['stone'])
    cv.shade(0, 3 * T, w, 6)
    bank_medallion(cv, cxp, (cv.h - 5) * T)
    y = (cv.h - 2) * T                                                 # dinding depan berkaca
    cv.r(0, y, w, 2 * T, C['wall2'])
    cv.r(0, y, w, 3, C['out'])
    cv.r(0, y + 3, w, 5, C['wood'])
    for tx in range(0, cv.w - 1, 3):
        if abs(tx * T + T - cxp) <= 2 * T:
            continue
        cv.box(tx * T + 3, y + 10, 2 * T - 6, T + 1, C['glass'])
        cv.r(tx * T + T, y + 12, 2, T - 3, C['wall2'])
    dx = cxp - round(1.5 * T)                                          # pintu kaca ganda
    cv.box(dx, y + 8, 3 * T, 2 * T - 12, C['wood'])
    cv.r(dx + 3, y + 11, 3 * T - 6, 2 * T - 18, C['glass2'])
    cv.r(dx + 3 * T // 2 - 1, y + 11, 2, 2 * T - 18, C['wall2'])
    for kx in (dx + 3 * T // 2 - 7, dx + 3 * T // 2 + 5):
        cv.r(kx, y + T, 3, 6, C['gold'])


# ─────────────────────────── toko kosmetik ───────────────────────────
# Toko Glow: dinding merah muda, rak pajang tengah toko, meja rias bercermin lampu, dan
# meja kasir. Prop dinding mengembalikan None (menempel dinding, bukan penghalang lantai).

def p_glowsign(cv, x, y):
    """Papan nama "GLOW" di dinding dalam toko."""
    cv.box(x, y, 96, 28, C['pink'])
    cv.r(x + 3, y + 3, 90, 22, C['pink2'])
    cv.text(x, y + 7, 96, 'GLOW', C['brown'])
    return None


def p_promo(cv, x, y):
    """Poster promo bergambar botol serum."""
    cv.box(x, y, 60, 42, C['wood2'])
    cv.r(x + 2, y + 2, 56, 38, C['pink2'])
    cv.box(x + 8, y + 10, 16, 24, C['pink'])              # botol serum
    cv.r(x + 12, y + 6, 8, 6, C['lilac'])
    cv.r(x + 30, y + 10, 24, 3, C['brown'])               # baris teks
    cv.r(x + 30, y + 17, 18, 3, C['brown'])
    cv.box(x + 30, y + 25, 24, 10, C['gold'])             # label diskon
    return None


def p_wallshelf(cv, x, y):
    """Rak dinding berisi produk perawatan kulit."""
    tint = (C['pink'], C['lilac'], C['cream'], C['teal2'], C['gold'])
    cv.box(x, y, 64, 42, C['white'])
    for j in range(3):
        cv.r(x + 3, y + 3 + j * 13, 58, 11, C['pink2'])
        for i in range(7):
            cv.r(x + 5 + i * 8, y + 5 + j * 13, 6, 8, tint[(i + j) % 5])
        cv.r(x + 3, y + 13 + j * 13, 58, 2, C['pink'])
    return None


def p_cosmeticshelf(cv, x, y):
    """Rak pajang tengah toko: tiga undak produk perawatan kulit (lebar 6 petak)."""
    tint = (C['pink'], C['lilac'], C['white'], C['teal2'], C['gold'], C['cream'])
    cv.box(x, y, 96, 44, C['white'])
    for j in range(3):
        cv.r(x + 3, y + 4 + j * 13, 90, 11, C['pink2'])
        for i in range(11):
            cv.r(x + 5 + i * 8, y + 6 + j * 13, 6, 8, tint[(i + j) % 6])
        cv.r(x + 3, y + 14 + j * 13, 90, 2, C['pink'])
    cv.shade(x, y + 44, 96, 4)
    return (x, y + 28, 96, 20)


def p_vanity(cv, x, y):
    """Meja rias: cermin oval berlampu dan tester di mejanya."""
    cv.box(x + 8, y - 34, 40, 34, C['pink'])              # bingkai cermin
    cv.d.ellipse([x + 12, y - 31, x + 43, y - 4], fill=C['glass2'], outline=C['out'])
    for lx in range(x + 10, x + 46, 8):
        cv.r(lx, y - 37, 4, 3, C['gold'])                 # lampu rias
    cv.box(x, y, 56, 20, C['wood'])
    cv.r(x + 3, y + 3, 50, 6, C['wood3'])
    for i, c in enumerate((C['pink'], C['lilac'], C['teal2'])):
        cv.r(x + 9 + i * 14, y - 7, 8, 8, c)              # tester
    cv.shade(x, y + 20, 56, 3)
    return (x, y + 8, 56, 15)


def p_glowcashier(cv, x, y):
    """Meja kasir Toko Glow: mesin kasir, kantong belanja, etalase merah muda (lebar 6 petak)."""
    cv.box(x + 8, y - 17, 26, 19, C['white'])             # mesin kasir
    cv.r(x + 11, y - 14, 20, 10, C['glass'])
    cv.r(x + 13, y - 3, 16, 3, C['grey'])
    for i, c in enumerate((C['pink'], C['lilac'])):
        cv.box(x + 52 + i * 18, y - 15, 15, 17, c)        # kantong belanja
        cv.r(x + 56 + i * 18, y - 17, 7, 3, C['brown'])
    cv.box(x, y, 96, 26, C['pink'])
    cv.r(x + 3, y + 3, 90, 10, C['pink2'])
    cv.r(x + 3, y + 16, 90, 6, C['white'])
    cv.shade(x, y + 26, 96, 4)
    return (x, y + 12, 96, 18)


def p_standee(cv, x, y):
    """Standee promo berdiri di lantai."""
    cv.box(x, y, 30, 40, C['pink'])
    cv.r(x + 3, y + 3, 24, 34, C['pink2'])
    cv.r(x + 7, y + 7, 16, 3, C['brown'])
    cv.box(x + 9, y + 14, 12, 18, C['lilac'])
    cv.r(x + 10, y + 40, 10, 6, C['grey2'])               # kaki
    cv.shade(x, y + 46, 30, 3)
    return (x + 4, y + 34, 22, 14)


def p_basket(cv, x, y):
    """Tumpukan keranjang belanja di dekat pintu."""
    for i in range(3):
        cv.box(x, y - i * 7, 34, 14, C['lilac'] if i % 2 else C['pink'])
        cv.r(x + 3, y + 3 - i * 7, 28, 6, C['pink2'])
    cv.shade(x, y + 14, 34, 3)
    return (x, y + 2, 34, 13)


# ─────────────────────────── Kafe Senja ───────────────────────────
# Kafe kayu hangat di Jalan Kafe. Dinding belakang plester krem berlambrisir kayu dengan
# jendela senja, papan menu kapur, rak biji kopi, dan papan nama berbola lampu; lantai papan
# kayu; meja barista di kiri, bangku empuk dan meja bundar di kanan dan bawah. Cahaya senja
# dari jendela depan dijatuhkan sebagai lapisan hangat di tiga baris lantai terdepan.

def cafe_floor(cv, top):
    """Lantai ubin kunci: dasar krem bernat, lalu tiap pertemuan empat ubin dihiasi motif
    bunga empat mahkota — tenang tapi tidak polos, dan cukup terang supaya perabot kayu
    gelapnya tetap terbaca."""
    w, bottom = cv.w * T, (cv.h - 2) * T
    for ty in range(top // T, cv.h - 2):
        for tx in range(cv.w):
            x, y = tx * T, ty * T
            cv.r(x, y, T, T, C['cream'])
            cv.r(x, y + T - 1, T, 1, C['walk2'])
            cv.r(x + T - 1, y, 1, T, C['walk2'])
    for cy in range(top + T, bottom, 2 * T):
        for cx in range(T, w, 2 * T):
            for ox, oy in ((-5, 0), (5, 0), (0, -5), (0, 5)):
                cv.d.ellipse([cx + ox - 4, cy + oy - 4, cx + ox + 4, cy + oy + 4], fill=C['walk2'])
            cv.d.ellipse([cx - 3, cy - 3, cx + 3, cy + 3], fill=C['roof3'])


def cafe_room(cv, front):
    """Gambar seluruh ruang Kafe Senja. front = petak tengah pintu kayu di dinding depan.
    Baris 0-2 dinding belakang, 3 s.d. tinggi-3 lantai, dua baris terbawah dinding depan."""
    w = cv.w * T
    cv.r(0, 0, w, 3 * T, C['wall'])                                   # plester krem
    cv.r(0, 0, w, 4, C['out'])
    cv.r(0, 4, w, 3, C['wall2'])                                      # lis plafon
    cv.r(0, 3 * T - 16, w, 3, C['wood2'])                             # rel kursi
    cv.r(0, 3 * T - 8, w, 6, C['wood'])                               # lambrisir bawah
    for sx in range(0, w, 14):
        cv.r(sx, 3 * T - 8, 1, 6, C['wood2'])
    cv.r(0, 3 * T - 2, w, 2, C['out'])
    cafe_floor(cv, 3 * T)
    cv.shade(0, 3 * T, w, 6)                                          # bayangan dinding
    for rows in (4, 3, 2):                                            # limpahan cahaya senja,
        cv.wash(0, (cv.h - 2 - rows) * T, w, rows * T, (246, 196, 106, 34))   # makin pekat di depan
    y = (cv.h - 2) * T                                                # dinding depan kayu
    cv.r(0, y, w, 2 * T, C['wood'])
    cv.r(0, y, w, 3, C['out'])
    cv.r(0, y + 3, w, 5, C['wood2'])
    cxp = round(front * T)
    for tx in range(0, cv.w - 1, 3):                                  # jendela berkaca hangat
        if abs(tx * T + T - cxp) <= 2 * T:
            continue
        cv.box(tx * T + 3, y + 11, 2 * T - 6, T, C['warm'])
        cv.r(tx * T + 5, y + 13, 2 * T - 10, 3, C['cream'])
        cv.r(tx * T + T, y + 13, 2, T - 4, C['wood2'])
    dx = cxp - round(1.5 * T)                                         # pintu kayu berkaca
    cv.box(dx, y + 8, 3 * T, 2 * T - 12, C['wood2'])
    cv.r(dx + 4, y + 12, 3 * T - 8, T - 2, C['warm'])
    cv.r(dx + 3 * T // 2 - 1, y + 12, 2, T - 2, C['wood2'])
    cv.r(dx + 6, y + 2 * T - 12, 3 * T - 12, 4, C['wood3'])
    cv.r(dx + 3 * T // 2 - 11, y + T + 5, 4, 3, C['gold'])            # gagang
    cv.r(dx + 3 * T // 2 + 7, y + T + 5, 4, 3, C['gold'])


def p_cafewindow(cv, x, y):
    """Jendela senja di dinding belakang (7 petak): langit jingga-ungu, siluet kota, ambang kayu."""
    cv.box(x, y + 2, 112, 38, C['wood2'])
    for i, col in enumerate(('lilac', 'pink', 'warm', 'gold')):
        cv.r(x + 4, y + 6 + i * 6, 104, 6, C[col])
    cv.d.ellipse([x + 46, y + 16, x + 66, y + 30], fill=C['cream'], outline=C['warm'])
    for sx in range(x + 6, x + 104, 15):                              # siluet atap kota
        cv.r(sx, y + 26, 11, 4, C['navy2'])
    cv.r(x + 4, y + 30, 104, 6, C['navy'])
    cv.r(x + 54, y + 6, 3, 30, C['wood2'])                            # bilah kusen
    cv.r(x + 4, y + 19, 104, 2, C['wood2'])
    cv.box(x - 2, y + 38, 116, 6, C['wood'])                          # ambang
    cv.shade(x - 2, y + 44, 116, 3)
    return None                                                       # menempel dinding


def p_cafeboard(cv, x, y):
    """Papan menu kapur (8 petak) yang digantung di atas meja barista."""
    cv.box(x, y + 4, 128, 38, C['wood2'])
    cv.r(x + 4, y + 8, 120, 30, C['navy'])
    cv.text(x + 4, y + 7, 120, 'KAFE SENJA', C['cream'], FONT_SMALL)
    for j in range(3):                                                # baris menu + harga
        cv.r(x + 10, y + 21 + j * 6, 52, 2, C['glass2'])
        cv.r(x + 74, y + 21 + j * 6, 17, 2, C['warm'])
    cv.d.ellipse([x + 100, y + 20, x + 116, y + 32], outline=C['cream'])
    cv.r(x + 104, y + 17, 8, 3, C['cream'])                           # coretan uap kopi
    return None


def p_cafeshelf(cv, x, y):
    """Rak dinding (7 petak): stoples biji kopi berselang-seling dengan cangkir tertelungkup."""
    jar = (C['brown'], C['wood2'], C['gold'], C['green2'], C['red'])
    cv.box(x, y + 6, 112, 34, C['wood'])
    cv.r(x + 3, y + 9, 106, 28, C['wall'])
    for j in range(2):
        sy = y + 11 + j * 14
        for i in range(7):
            bx = x + 7 + i * 15
            if (i + j) % 2:
                cv.box(bx, sy, 11, 10, jar[(i + j * 3) % 5])
                cv.r(bx + 2, sy + 2, 7, 3, C['cream'])
            else:
                cv.box(bx + 1, sy + 3, 9, 7, C['cream'])
                cv.r(bx + 10, sy + 5, 2, 3, C['cream'])
        cv.r(x + 3, sy + 10, 106, 3, C['wood2'])
    return None


def p_cafesign(cv, x, y):
    """Papan nama kayu + tiga bola lampu gantung (8 petak)."""
    for i in range(3):
        bx = x + 22 + i * 42
        cv.r(bx + 4, y, 2, 10, C['wood2'])                            # kabel
        cv.box(bx, y + 10, 11, 11, C['warm'])
        cv.r(bx + 3, y + 13, 5, 5, C['cream'])
    cv.box(x, y + 24, 128, 18, C['wood'])
    cv.r(x + 3, y + 27, 122, 12, C['wood3'])
    cv.text(x + 3, y + 27, 122, 'SEDUH PELAN', C['cream'], FONT_SMALL)
    return None


def p_cafebar(cv, x, y):
    """Meja barista 10 petak: badan kayu berlis kuningan, daun marmer, mesin espresso,
    penggiling, kubah kue, dan tumpukan cangkir."""
    cv.box(x, y, 160, 30, C['wood'])
    cv.r(x + 3, y + 5, 154, 4, C['gold'])
    for px in range(x + 6, x + 150, 26):
        cv.r(px, y + 13, 22, 13, C['wood2'])                          # panel kayu
    cv.box(x - 4, y - 8, 168, 10, C['stone'])                         # daun marmer
    cv.r(x - 1, y - 6, 162, 3, C['white'])
    cv.box(x + 10, y - 26, 34, 19, C['grey2'])                        # mesin espresso
    cv.r(x + 13, y - 23, 28, 8, C['red'])
    cv.r(x + 16, y - 14, 5, 6, C['grey'])
    cv.r(x + 31, y - 14, 5, 6, C['grey'])
    cv.box(x + 50, y - 22, 15, 15, C['brown'])                        # penggiling biji
    cv.r(x + 53, y - 26, 9, 5, C['wood2'])
    cv.box(x + 74, y - 20, 32, 13, C['glass2'])                       # kubah kue
    cv.r(x + 78, y - 14, 10, 6, C['warm'])
    cv.r(x + 92, y - 14, 10, 6, C['gold'])
    for i in range(4):                                                # tumpukan cangkir
        cv.box(x + 120 + (i % 2) * 14, y - 13 - (i // 2) * 6, 11, 7, C['cream'])
    cv.shade(x - 4, y + 30, 168, 4)
    return (x - 4, y - 2, 168, 34)


def p_cafetable(cv, x, y):
    """Meja bundar dua kursi (5 petak): kursi rotan kiri-kanan, cangkir dan kue di atasnya."""
    for sx in (x, x + 62):
        cv.box(sx, y + 8, 18, 24, C['wood3'])                         # kursi rotan
        cv.r(sx + 3, y + 11, 12, 9, C['wood'])
        cv.r(sx + 3, y + 22, 12, 6, C['warm'])                        # bantalan
    cv.d.ellipse([x + 16, y + 6, x + 64, y + 30], fill=C['wood'], outline=C['out'])
    cv.d.ellipse([x + 20, y + 9, x + 60, y + 24], fill=C['wood3'])
    cv.box(x + 28, y + 13, 10, 9, C['cream'])                         # cangkir
    cv.r(x + 30, y + 15, 6, 2, C['brown'])
    cv.box(x + 44, y + 14, 13, 8, C['warm'])                          # kue
    cv.r(x + 46, y + 16, 9, 2, C['pink'])
    cv.r(x + 36, y + 30, 8, 7, C['wood2'])                            # tiang meja
    cv.box(x + 28, y + 35, 24, 6, C['wood2'])
    cv.shade(x, y + 41, 80, 4)
    return (x, y + 24, 80, 20)


def p_cafesofa(cv, x, y):
    """Bangku empuk 7 petak: sandaran tinggi berbantal, dudukan, dan meja rendah di depannya."""
    cv.box(x, y, 112, 20, C['teal'])
    cv.r(x + 3, y + 3, 106, 9, C['teal2'])
    for i in range(3):                                                # bantal
        cv.box(x + 9 + i * 34, y + 6, 24, 12, C['warm'])
        cv.r(x + 12 + i * 34, y + 8, 18, 4, C['gold'])
    cv.box(x, y + 18, 112, 12, C['teal2'])                            # dudukan
    cv.box(x + 22, y + 30, 68, 11, C['wood'])                         # meja rendah
    cv.r(x + 25, y + 32, 62, 4, C['wood3'])
    cv.box(x + 48, y + 25, 11, 8, C['cream'])                         # cangkir di meja
    cv.r(x + 50, y + 27, 7, 2, C['brown'])
    cv.shade(x, y + 41, 112, 4)
    return (x, y + 22, 112, 22)


def p_cafestool(cv, x, y):
    """Meja tinggi dua bangku (4 petak) — tempat duduk singkat sambil menunggu pesanan."""
    for sx in (x, x + 48):
        cv.box(sx, y + 14, 16, 8, C['wood2'])                         # dudukan bangku
        cv.r(sx + 2, y + 16, 12, 4, C['warm'])
        cv.r(sx + 6, y + 22, 4, 13, C['wood2'])                       # tiang
        cv.box(sx + 2, y + 33, 12, 4, C['wood2'])
    cv.d.ellipse([x + 16, y + 6, x + 48, y + 24], fill=C['wood'], outline=C['out'])
    cv.d.ellipse([x + 20, y + 9, x + 44, y + 20], fill=C['wood3'])
    cv.box(x + 26, y + 10, 10, 9, C['cream'])                         # cangkir
    cv.r(x + 28, y + 12, 6, 2, C['brown'])
    cv.r(x + 29, y + 24, 6, 11, C['wood2'])
    cv.box(x + 22, y + 33, 20, 5, C['wood2'])
    cv.shade(x, y + 38, 64, 4)
    return (x, y + 22, 64, 18)


def p_cafeplant(cv, x, y):
    """Tanaman hias tinggi dalam pot rotan (2 petak) untuk mengisi sudut ruangan."""
    cv.r(x + 14, y + 8, 4, 22, C['leaf3'])
    for i, (lx, ly) in enumerate(((0, 0), (16, 2), (3, 10), (18, 13), (9, 19))):
        cv.d.ellipse([x + lx, y + ly, x + lx + 14, y + ly + 11],
                     fill=C['leaf2'] if i % 2 else C['leaf'], outline=C['out'])
    cv.box(x + 6, y + 28, 20, 14, C['wood3'])                         # pot rotan
    cv.r(x + 9, y + 31, 14, 2, C['wood'])
    cv.r(x + 9, y + 36, 14, 2, C['wood'])
    cv.shade(x + 6, y + 42, 20, 3)
    return (x + 6, y + 32, 20, 12)


# ─────────────────────────── Kamar kos Alif ───────────────────────────
# Kamar sewaan di rumah kos Jalan Pasar. Di sinilah hari cerita berganti: titik interaksi
# "npc:kasur" di sisi ranjang memicu tidur (AdventureGame.Cases.cs).

def p_kosbed(cv, x, y):
    """Ranjang tunggal 5 x 3 petak: kepala ranjang kayu, bantal, selimut hijau toska."""
    cv.box(x, y, 80, 46, C['wood2'])
    cv.r(x + 3, y + 3, 74, 40, C['white'])
    cv.box(x + 5, y + 6, 20, 34, C['cream'])              # bantal
    cv.r(x + 8, y + 9, 14, 28, C['white'])
    cv.box(x + 30, y + 3, 47, 40, C['teal2'])             # selimut
    cv.r(x + 33, y + 6, 41, 5, C['teal'])
    for sx in range(x + 38, x + 74, 9):
        cv.r(sx, y + 16, 3, 22, C['teal'])
    cv.r(x, y, 4, 46, C['wood'])                          # kepala ranjang
    cv.shade(x, y + 46, 80, 4)
    return (x, y + 8, 80, 40)


def p_wardrobe(cv, x, y):
    """Lemari pakaian dua pintu (3 petak) menempel dinding belakang."""
    cv.box(x, y, 48, 52, C['wood'])
    cv.r(x + 3, y + 3, 20, 44, C['wood3'])
    cv.r(x + 25, y + 3, 20, 44, C['wood3'])
    cv.r(x + 20, y + 22, 3, 8, C['gold'])
    cv.r(x + 25, y + 22, 3, 8, C['gold'])
    cv.shade(x, y + 52, 48, 4)
    return (x, y + 40, 48, 14)


def p_koswindow(cv, x, y):
    """Jendela kamar bertirai (4 petak) — langit pagi di luar."""
    cv.box(x, y + 6, 64, 32, C['wood2'])
    cv.r(x + 4, y + 10, 56, 24, C['sky2'])
    cv.r(x + 4, y + 26, 56, 8, C['leaf2'])
    cv.r(x + 31, y + 10, 2, 24, C['wood2'])
    cv.r(x + 4, y + 10, 9, 24, C['warm'])                 # tirai
    cv.r(x + 51, y + 10, 9, 24, C['warm'])
    return None


def p_kosrug(cv, x, y):
    """Karpet anyaman di tengah kamar — hiasan, bukan penghalang."""
    cv.box(x, y, 80, 30, C['roof3'])
    cv.r(x + 4, y + 4, 72, 22, C['warm'])
    cv.r(x + 8, y + 8, 64, 14, C['roof3'])
    return None


PROPS = dict(tree=p_tree, palm=p_palm, plant=p_plant, lamp=p_lamp, bench=p_bench, table=p_table,
             umbrella=p_umbrella_table, halte=p_halte, bike=p_bike, motor=p_motor, bin=p_bin,
             fountain=p_fountain, gazebo=p_gazebo, pond=p_pond, board=p_board,
             door=p_door, counter=p_counter, sofa=p_sofa, desk=p_desk, whiteboard=p_whiteboard,
             teacherdesk=p_teacherdesk, shelf=p_shelf, officedesk=p_officedesk, stall=p_stall, sink=p_sink,
             pew=p_pew, altar=p_altar, ggdesk=p_ggdesk, studydesk=p_studydesk,
             fridge=p_fridge, gondola=p_gondola, cashier=p_cashier, mat=p_mat,
             teller=p_teller, bankdesk=p_bankdesk, waitsofa=p_waitsofa, queue=p_queue, atm=p_atm,
             ffckitchen=p_ffckitchen, ffcmenu=p_ffcmenu, ffclogo=p_ffclogo, ffccounter=p_ffccounter,
             ffcdrink=p_ffcdrink, ffctray=p_ffctray, ffctable=p_ffctable, ffcbooth=p_ffcbooth,
             ffcstandee=p_ffcstandee,
             cafewindow=p_cafewindow, cafeboard=p_cafeboard, cafeshelf=p_cafeshelf, cafesign=p_cafesign,
             cafebar=p_cafebar, cafetable=p_cafetable, cafesofa=p_cafesofa, cafestool=p_cafestool,
             cafeplant=p_cafeplant,
             kosbed=p_kosbed, wardrobe=p_wardrobe, koswindow=p_koswindow, kosrug=p_kosrug,
             poster=p_poster, medsign=p_medsign, curtain=p_curtain, bed=p_clinicbed, trolley=p_trolley,
             medcabinet=p_medcabinet, clock=p_clock, chairs=p_chairrow,
             mihrab=p_mihrab, mimbar=p_mimbar, quranrack=p_quranrack, donation=p_donation,
             shoerack=p_shoerack, speaker=p_speaker,
             glowsign=p_glowsign, promo=p_promo, wallshelf=p_wallshelf,
             cosmeticshelf=p_cosmeticshelf, vanity=p_vanity, glowcashier=p_glowcashier,
             standee=p_standee, basket=p_basket)
PROPS_RNG = dict(flowers=p_flowers, clothesline=p_clothesline)


# ─────────────────────────── tata letak ───────────────────────────
# template "street": bangunan berdiri di baris BASE, trotoar s.d. ROAD_TOP, jalan raya,
# trotoar bawah. Lantai yang bisa dilewati = baris BASE s.d. dasar map.
BASE, ROAD_TOP, ROAD_BOTTOM = 11, 17, 22

MAPS = [
    dict(id='J2', area='Jalan Pasar', file='J2_JalanPasar', template='street', seed=2, crosswalk=40,
         buildings=[dict(x=2, w=10, h=5, kind='kelontong', name='TOKO KELONTONG', sign='3f8a5a'),
                    dict(x=14, w=9, h=5, kind='minimarket', name='MINIMARKET 24', sign='c0392b', wall='f4ecdd'),
                    dict(x=25, w=7, h=4, kind='rumah', wall='e9d9bb'),
                    dict(x=46, w=9, h=5, kind='warung', name='WARUNG SEMBAKO', sign='8a5a3a'),
                    dict(x=57, w=6, h=4, kind='rumah', wall='efe0c2')],
         back=[('tree', 1, 0), ('tree', 34, 0), ('tree', 42, 1), ('tree', 60, 0)],
         props=[('plant', 12, 11), ('halte', 34, 12), ('lamp', 23, 12), ('bin', 44, 12),
                ('motor', 56, 13), ('plant', 55, 11), ('lamp', 62, 12), ('bike', 3, 15)],
         spawn=(20, 14), npcs=[('npc:harun', 53, 15), ('npc:hendra', 16, 15),
                               ('npc:bima', 9, 15), ('npc:laras', 40, 16),
                               # Kasus Warga Bab 1 (CaseContent.cs): lapak di meja dagangan Toko Kelontong
                               ('npc:yanto', 3.4, 11.2), ('npc:lapak', 6, 11.4), ('npc:mira', 8.6, 11.6),
                               ('npc:wati', 12, 14.6)]),
    dict(id='J3', area='Jalan Kafe', file='J3_JalanKafe', template='street', seed=3, crosswalk=12,
         buildings=[dict(x=2, w=10, h=5, kind='kafe', name='KAFE SENJA', sign='5a3a26', wall='e8d2b0'),
                    dict(x=14, w=9, h=5, kind='kosmetik', name='GLOW', sign='d86a8f', wall='fbe9ee'),
                    dict(x=25, w=8, h=5, kind='gerai', name='GERAI HP', sign='3b6ea8', wall='eef1f4'),
                    dict(x=37, w=10, h=5, kind='kafe', name='KEDAI KOPI', sign='6e4529', wall='e9d9bb'),
                    dict(x=50, w=12, h=6, kind='minimarket', name='TOKO BUKU', sign='2f6f6a', wall='f2ead8')],
         back=[('tree', 33, 0), ('tree', 47, 1), ('tree', 62, 0)],
         props=[('umbrella', 3, 12), ('umbrella', 8, 12), ('plant', 12, 11), ('lamp', 24, 12), ('plant', 33, 11),
                ('table', 38, 13), ('table', 43, 13), ('bench', 55, 13), ('lamp', 48, 12), ('bin', 34, 13)],
         spawn=(30, 14), npcs=[('npc:kirana', 18, 14), ('npc:tara', 41, 14), ('npc:arga', 46, 14),
                               ('npc:gilang', 11, 14)]),    # Kasus Warga: menunggu di depan Kafe Senja
    dict(id='J4', area='Pusat Kota', file='J4_PusatKota', template='street', seed=4, crosswalk=30,
         buildings=[dict(x=3, w=12, h=6, kind='bank', name='BANK SYARIAH', sign='2f6f6a', wall='f2ead8'),
                    dict(x=16, w=3, h=4, kind='atm', name='ATM', sign='3b6ea8', wall='eef1f4'),
                    dict(x=42, w=14, h=6, kind='masjid', name='MASJID AL-AMANAH')],
         back=[('tree', 21, 0), ('tree', 26, 1), ('tree', 34, 0), ('tree', 58, 0)],
         props=[('fountain', 26, 11), ('bench', 22, 14), ('bench', 36, 14), ('flowers', 21, 12), ('flowers', 34, 12),
                ('lamp', 20, 12), ('lamp', 40, 12), ('palm', 57, 11), ('plant', 2, 11), ('board', 60, 12)],
         spawn=(30, 16), npcs=[('npc:bank', 9, 13), ('npc:ratna', 26, 16), ('npc:sinta', 14, 15),
                               ('npc:darto', 59, 15),
                               ('npc:joko', 36.5, 14.4)]),   # Kasus Warga: pembeli di depan Idmaret
    dict(id='K1', area='Kampus Cempaka', file='K1_Kampus', template='campus', seed=5,
         buildings=[dict(x=4, w=24, h=7, kind='fakultas', name='FAKULTAS EKONOMI & BISNIS', sign='2f6f6a', wall='f2ead8'),
                    dict(x=29, w=8, h=4, kind='gerbang', name='UNIV. CEMPAKA', sign='a4553a', wall='d9c4a0'),
                    dict(x=38, w=6, h=4, kind='fotokopi', name='FOTOKOPI', sign='3b6ea8'),
                    dict(x=46, w=14, h=7, kind='fakultas', name='GEDUNG REKTORAT', sign='5a3a26', wall='efe0c2')],
         back=[('tree', 1, 0), ('tree', 29, 0), ('tree', 43, 0), ('tree', 61, 0)],
         props=[('tree', 6, 14), ('tree', 22, 14), ('tree', 44, 14), ('tree', 58, 14), ('bench', 12, 15), ('bench', 50, 15),
                ('board', 36, 12), ('bike', 38, 12), ('bike', 40, 12), ('lamp', 30, 13), ('lamp', 34, 13), ('flowers', 26, 19)],
         spawn=(32, 17), npcs=[('npc:fadli', 16, 13), ('npc:dewi', 35, 15), ('npc:yusuf', 52, 13),
                               # Kasus Warga: mading = papan pengumuman kanan di lukisan
                               ('npc:salsa', 44, 11.8), ('npc:ilham', 50, 11.6), ('npc:mading', 56.2, 11.2)]),
    dict(id='K2', area='Taman Cempaka', file='K2_Taman', template='park', seed=6,
         buildings=[],
         back=[('tree', x, 0) for x in range(0, 64, 5)],
         props=[('gazebo', 10, 6), ('pond', 40, 7), ('tree', 4, 14), ('tree', 26, 5), ('tree', 30, 15), ('tree', 56, 14),
                ('tree', 60, 6), ('bench', 22, 11), ('bench', 36, 17), ('flowers', 44, 14), ('flowers', 16, 18),
                ('board', 30, 4), ('lamp', 20, 8), ('lamp', 50, 16), ('bin', 24, 11)],
         spawn=(32, 12), npcs=[('npc:nadia', 40, 14),
                               # Kasus Warga: papan informasi taman menyimpan plakat wakafnya
                               ('npc:salamah', 26.5, 10.6), ('npc:gunawan', 16.5, 10.4), ('npc:papantaman', 30.6, 5.9)]),
    # ── Interior Kampus Cempaka ──────────────────────────────────────────────────
    # Ruangan buatan kode (belum ada lukisan): skala 1, jadi propnya sepadan dengan tokoh.
    # Pintu keluar selalu di dinding depan (2 baris terbawah), pintu ke ruang lain di dinding
    # belakang. Lantai yang bisa dilewati = baris 3 s.d. tinggi-3.
    dict(id='K1L', area='Lobi Kampus', file='K1L_Lobi', template='indoor', seed=11,
         size=(40, 15), front=20, floor=('walk', 'walk2'),
         buildings=[], back=[],
         props=[('door', 6, 0), ('door', 18, 0), ('door', 30, 0),
                ('board', 24, 1), ('plant', 2, 4), ('plant', 37, 4),
                ('counter', 15, 7), ('sofa', 3, 9), ('sofa', 34, 9), ('bin', 12, 4)],
         doors=[dict(id='lobi.keluar', x=20, y=12.4, land=(20, 10.6), h=1.4, label='OUT'),
                dict(id='lobi.kelas', x=6.9, y=3.4, land=(6.9, 5.2), h=2.9, label='IN'),
                dict(id='lobi.dosen', x=18.9, y=3.4, land=(18.9, 5.2), h=2.9, label='IN'),
                dict(id='lobi.toilet', x=30.9, y=3.4, land=(30.9, 5.2), h=2.9, label='IN')],
         spawn=(20, 10), npcs=[]),
    dict(id='K1K', area='Kelas Ekonomi', file='K1K_Kelas', template='indoor', seed=12,
         size=(28, 13), front=14, floor=('wall2', 'walk2'),
         buildings=[], back=[],
         props=[('whiteboard', 8, 0), ('teacherdesk', 12, 4), ('plant', 24, 4), ('shelf', 2, 0),
                ('desk', 4, 6), ('desk', 9, 6), ('desk', 14, 6), ('desk', 19, 6),
                ('desk', 4, 8), ('desk', 9, 8), ('desk', 14, 8), ('desk', 19, 8)],
         doors=[dict(id='kelas.pintu', x=14, y=10.4, land=(14, 8.6), h=1.4, label='OUT')],
         spawn=(14, 9), npcs=[]),
    dict(id='K1D', area='Ruang Dosen', file='K1D_RuangDosen', template='indoor', seed=13,
         size=(24, 13), front=12, floor=('walk', 'wall2'),
         buildings=[], back=[],
         props=[('shelf', 2, 0), ('shelf', 19, 0), ('board', 9, 1), ('plant', 21, 4),
                ('officedesk', 2, 6), ('officedesk', 9, 6), ('officedesk', 16, 6)],
         doors=[dict(id='dosen.pintu', x=12, y=10.4, land=(12, 8.6), h=1.4, label='OUT')],
         spawn=(12, 9), npcs=[]),
    dict(id='K1T', area='Toilet Kampus', file='K1T_Toilet', template='indoor', seed=14,
         size=(20, 12), front=10, floor=('stone', 'stone2'), wall='wall2',
         buildings=[], back=[],
         props=[('stall', 10, 1), ('stall', 15, 1), ('sink', 2, 3), ('sink', 6, 3), ('bin', 17, 5)],
         doors=[dict(id='toilet.pintu', x=10, y=9.4, land=(10, 7.6), h=1.4, label='OUT')],
         spawn=(10, 8), npcs=[]),
    # ── Interior Idmaret (minimarket kedua) ─────────────────────────────────────
    # Template 'mart' yang sama dengan Minimarket 24, beda nama, pita merek, warna ubin,
    # dan tata letak: pendingin di kiri, kasir + ATM di kanan dekat pintu.
    dict(id='J4I', area='Idmaret', file='J4I_Idmaret', template='mart', seed=19,
         size=(32, 15), front=16, sign='IDMARET', band=('blue', 'red', 'yellow'),
         tile=('cream', 'stone', 'white'),
         buildings=[], back=[],
         props=[('fridge', 2, 3), ('fridge', 6, 3), ('fridge', 10, 3),
                ('cashier', 22, 5), ('atm', 29, 3), ('plant', 0, 11),
                ('gondola', 3, 7), ('gondola', 3, 10),
                ('gondola', 18, 7), ('gondola', 18, 10),
                ('mat', 14, 12)],
         doors=[dict(id='idmaret.keluar', x=16, y=12.4, land=(16, 10.6), h=1.4, label='OUT')],
         spawn=(16, 11), npcs=[('npc:rini', 25, 3)]),
    # ── Interior Bank Syariah ───────────────────────────────────────────────────
    # Pintunya di lukisan Pusat Kota (ART_LAYOUT J4). Konter teller di kanan, resepsionis
    # di kiri, sofa tunggu mengapit lorong tengah, ATM dan mesin antrean di sudut.
    dict(id='J4B', area='Bank Syariah', file='J4B_BankSyariah', template='bank', seed=18,
         size=(34, 15), front=17,
         buildings=[], back=[],
         props=[('atm', 0, 3), ('teller', 20, 3), ('plant', 32, 4),
                ('bankdesk', 6, 5),
                ('waitsofa', 4, 8), ('waitsofa', 21, 8),
                ('waitsofa', 4, 11), ('waitsofa', 21, 11),
                ('queue', 30, 10), ('plant', 0, 11)],
         doors=[dict(id='bank.keluar', x=17, y=12.4, land=(17, 10.6), h=1.4, label='OUT')],
         spawn=(17, 11), npcs=[]),
    # ── Interior Minimarket 24 ──────────────────────────────────────────────────
    # Pintunya di lukisan Jalan Pasar. Lemari pendingin menempel dinding belakang, dua
    # deret rak mengapit lorong tengah, kasir di dekat pintu.
    dict(id='J2M', area='Minimarket 24', file='J2M_Minimarket', template='mart', seed=17,
         size=(30, 15), front=15,
         buildings=[], back=[],
         props=[('fridge', 16, 3), ('fridge', 20, 3), ('fridge', 24, 3),
                ('cashier', 2, 5), ('plant', 28, 4),
                ('gondola', 3, 7), ('gondola', 3, 10),
                ('gondola', 17, 7), ('gondola', 17, 10),
                ('mat', 13, 12)],
         doors=[dict(id='mart.keluar', x=15, y=12.4, land=(15, 10.6), h=1.4, label='OUT')],
         spawn=(15, 11), npcs=[]),
    # ── Interior GG Learning Center ─────────────────────────────────────────────
    # Pintunya juga di lukisan Jalan Pasar. Dinding belakang 4 baris supaya logo, layar
    # sambutan, papan rencana, rak buku, dan mural maskot muat berjajar.
    dict(id='J2L', area='GG Learning Center', file='J2L_GGLearning', template='gg', seed=16,
         size=(34, 16), front=17,
         buildings=[], back=[],
         props=[('ggdesk', 13, 5), ('plant', 0, 5), ('plant', 33, 5),
                ('studydesk', 3, 8), ('studydesk', 3, 11),
                ('studydesk', 21, 8), ('studydesk', 21, 11)],
         doors=[dict(id='gg.keluar', x=17, y=13.4, land=(17, 11.6), h=1.4, label='OUT')],
         spawn=(17, 12), npcs=[]),
    # ── Interior Gereja Kasih Sejati ────────────────────────────────────────────
    # Pintunya ada di lukisan Jalan Pasar (ART_LAYOUT J2), jadi ruangannya buatan kode:
    # panggung altar di baris 3-6, bangku jemaat 3 baris di kiri-kanan lorong karpet.
    dict(id='J2G', area='Gereja Kasih Sejati', file='J2G_Gereja', template='church', seed=15,
         size=(30, 15), front=15,
         buildings=[], back=[],
         props=[('altar', 12, 3), ('plant', 10, 5), ('plant', 19, 5),
                ('pew', 3, 7), ('pew', 3, 9), ('pew', 3, 11),
                ('pew', 17, 7), ('pew', 17, 9), ('pew', 17, 11)],
         doors=[dict(id='gereja.keluar', x=15, y=12.4, land=(15, 10.6), h=1.4, label='OUT')],
         spawn=(15, 11), npcs=[]),
    # ── Interior Puskesmas Cempaka ──────────────────────────────────────────────
    # Pintunya ada di lukisan Jalan Kafe (ART_LAYOUT J3), jadi ruangannya buatan kode:
    # dua bilik periksa bertirai di kiri, meja pendaftaran + lemari obat di kanan,
    # kursi tunggu menghadap pintu keluar di tengah.
    dict(id='J3P', area='Puskesmas Cempaka', file='J3P_Puskesmas', template='indoor', seed=16,
         size=(34, 15), front=17, floor=('white', 'stone'), wall='cream',
         buildings=[], back=[],
         props=[('poster', 2, 0), ('medsign', 12, 0), ('poster', 27, 0),
                ('curtain', 1, 3), ('curtain', 7, 3),
                ('bed', 1, 5), ('bed', 7, 5), ('trolley', 12, 6),
                ('sink', 15, 4), ('plant', 18, 4), ('clock', 23, 0), ('counter', 20, 6),
                ('officedesk', 27, 5), ('medcabinet', 31, 4),
                ('chairs', 3, 10), ('chairs', 10, 10), ('bin', 28, 9), ('plant', 32, 9)],
         doors=[dict(id='puskesmas.keluar', x=17, y=12.4, land=(17, 10.6), h=1.4, label='OUT')],
         spawn=(17, 10), npcs=[('npc:ningsih', 23, 4)]),
    # ── Interior Masjid Al-Amanah ───────────────────────────────────────────────
    # Pintunya ada di lukisan Pusat Kota (ART_LAYOUT J4), jadi ruangannya buatan kode:
    # mihrab + mimbar di dinding kiblat, tiga saf karpet sajadah, dan serambi dalam
    # berubin tempat rak sandal serta kotak amal.
    dict(id='J4M', area='Masjid Al-Amanah', file='J4M_Masjid', template='mosque', seed=20,
         size=(36, 15), front=18,
         buildings=[], back=[],
         props=[('mihrab', 16, 0), ('speaker', 10, 1), ('clock', 13, 1), ('speaker', 25, 1),
                ('mimbar', 20, 3), ('quranrack', 1, 3), ('quranrack', 31, 3),
                ('donation', 14, 11), ('shoerack', 21, 11), ('plant', 1, 11), ('plant', 34, 11)],
         doors=[dict(id='masjid.keluar', x=18, y=12.4, land=(18, 10.6), h=1.4, label='OUT')],
         spawn=(18, 11), npcs=[('npc:mahfud', 8, 5)]),
    # ── Interior Toko Glow ──────────────────────────────────────────────────────
    # Pintunya ada di lukisan Jalan Kafe (ART_LAYOUT J3), jadi ruangannya buatan kode:
    # dua baris rak pajang di kiri, meja rias bercermin + meja kasir di kanan, dan lorong
    # lurus dari pintu ke belakang toko (petak 14-18) supaya pemain tidak terkurung rak.
    dict(id='J3G', area='Toko Glow', file='J3G_Glow', template='indoor', seed=21,
         size=(30, 15), front=15, floor=('cream', 'stone'), wall='pink2',
         buildings=[], back=[],
         props=[('wallshelf', 1, 0), ('promo', 6, 0), ('glowsign', 10, 0),
                ('wallshelf', 21, 0), ('wallshelf', 26, 0),
                ('cosmeticshelf', 1, 5), ('cosmeticshelf', 8, 5),
                ('vanity', 18, 4), ('glowcashier', 23, 7),
                ('cosmeticshelf', 1, 9), ('cosmeticshelf', 8, 9), ('cosmeticshelf', 17, 9),
                ('standee', 22, 4), ('basket', 25, 11), ('plant', 0, 11), ('plant', 28, 11)],
         doors=[dict(id='glow.keluar', x=15, y=12.4, land=(15, 10.6), h=1.4, label='OUT')],
         spawn=(15, 11), npcs=[]),
    # ── Interior FFC (gerai ayam goreng Pusat Kota) ─────────────────────────────
    # Pintunya ada di lukisan Pusat Kota (ART_LAYOUT J4), jadi ruangannya buatan kode:
    # jendela dapur + papan menu + plakat ember di dinding belakang, meja pesan panjang
    # membelah ruangan, sudut minuman swalayan di kanan, meja dan bilik makan di bawah.
    # Lorong petak 15-19 sengaja dikosongkan supaya jalur dari pintu ke kasir bebas.
    dict(id='J4F', area='FFC', file='J4F_FFC', template='ffc', seed=18,
         size=(34, 16), front=17,
         buildings=[], back=[],
         props=[('ffckitchen', 1, 0), ('ffcmenu', 11, 0), ('ffclogo', 24, 0), ('clock', 32, 0),
                ('ffccounter', 2, 5), ('ffccounter', 12, 5),
                ('ffcstandee', 23, 3), ('ffctray', 24, 6), ('ffcdrink', 28, 4), ('plant', 0, 4),
                ('ffctable', 2, 8), ('ffctable', 9, 8), ('ffctable', 20, 8), ('ffctable', 27, 8),
                ('ffcbooth', 1, 11), ('ffcbooth', 8, 11), ('ffcbooth', 21, 11), ('ffcbooth', 28, 11),
                ('mat', 15, 13)],
         doors=[dict(id='ffc.keluar', x=17, y=13.4, land=(17, 11.6), h=1.4, label='OUT')],
         spawn=(17, 12), npcs=[('npc:fira', 11, 3)]),
    # ── Interior Kafe Senja ─────────────────────────────────────────────────────
    # Pintunya ada di lukisan Jalan Kafe (ART_LAYOUT J3). Jendela senja + papan menu kapur +
    # rak biji + papan nama berbola lampu berjajar di dinding belakang; meja barista di kiri,
    # bangku empuk di kanan, meja bundar dan meja tinggi mengisi area duduk.
    # Lorong petak 14-20 dikosongkan supaya jalur pintu → barista bebas.
    dict(id='J3S', area='Kafe Senja', file='J3S_KafeSenja', template='cafe', seed=19,
         size=(34, 16), front=17,
         buildings=[], back=[],
         props=[('cafewindow', 1, 0), ('cafeboard', 8, 0), ('cafeshelf', 18, 0), ('cafesign', 26, 0),
                ('cafebar', 5, 5), ('cafesofa', 21, 4), ('cafeplant', 31, 4),
                ('cafetable', 1, 8), ('cafetable', 8, 8), ('cafetable', 21, 8), ('cafetable', 28, 8),
                ('cafestool', 2, 11), ('cafestool', 9, 11), ('cafestool', 21, 11), ('cafesofa', 26, 11),
                ('cafeplant', 0, 4), ('bin', 33, 12)],
         doors=[dict(id='senja.keluar', x=17, y=13.4, land=(17, 11.6), h=1.4, label='OUT')],
         spawn=(17, 12), npcs=[('npc:bayu', 10, 3)]),
    # ── Kamar Alif (rumah kos Jalan Pasar) ──────────────────────────────────────
    # Pintunya di rumah paling kanan lukisan Jalan Pasar. 'npc:kasur' = titik tidur di sisi
    # ranjang: dari sini hari cerita maju dan Kasus Warga hari berikutnya terbuka.
    dict(id='J2K', area='Kamar Alif', file='J2K_KamarAlif', template='indoor', seed=22,
         size=(20, 12), front=10, floor=('walk', 'walk2'), wall='cream',
         buildings=[], back=[],
         props=[('wardrobe', 1, 0), ('koswindow', 8, 0), ('shelf', 15, 0),
                ('kosbed', 14, 4), ('officedesk', 1, 5), ('kosrug', 7, 6), ('plant', 18, 8)],
         doors=[dict(id='kamar.keluar', x=10, y=9.4, land=(10, 7.6), h=1.4, label='OUT')],
         spawn=(10, 8), npcs=[('npc:kasur', 12.6, 6)]),
]

# Jalur dua arah antar pintu. Tiap pintu harus muncul tepat sekali di sini.
LINKS = [
    ('pasar.gereja', 'gereja.keluar'),
    ('pasar.gg', 'gg.keluar'),
    ('pasar.minimarket', 'mart.keluar'),
    ('pusat.bank', 'bank.keluar'),
    ('pusat.idmaret', 'idmaret.keluar'),
    ('kampus.fakultas', 'lobi.keluar'),
    ('lobi.kelas', 'kelas.pintu'),
    ('lobi.dosen', 'dosen.pintu'),
    ('lobi.toilet', 'toilet.pintu'),
    ('kafe.puskesmas', 'puskesmas.keluar'),
    ('kafe.glow', 'glow.keluar'),
    ('pusat.ffc', 'ffc.keluar'),
    ('kafe.senja', 'senja.keluar'),
    ('pasar.kos', 'kamar.keluar'),
    ('pusat.masjid', 'masjid.keluar'),
]

# ─────────────────────── lukisan tangan (Tools/city-art/art) ───────────────────────
# Kalau art/<file>.png ada, gambar itu dipakai sebagai latar dan geometri di bawah ini
# menggantikan lantai/penghalang hasil gambar kode. Satuan = petak (boleh pecahan),
# y dihitung dari atas; kotaknya dibaca dari lukisan lewat Tools/city-art (grid 64 x 24).
#
# 'doors' = pintu yang benar-benar bisa dimasuki. Tiap pintu: id unik, (x, y) titik lantai
# tepat di depan daun pintunya (tempat trigger-nya), 'land' titik mendarat saat pemain masuk
# DARI sisi seberang, 'h' tinggi yang harus dilewati penanda (ukur dari lantai sampai puncak
# kusen/serambi), dan 'label' teks penandanya. Satuan petak grid map itu sendiri.
# LINKS di bawah memasangkan dua pintu jadi satu jalur dua arah; CityWorld memasang SceneDoor
# + penanda panah dari data ini.
#
# 'scale' mengecilkan jejak map di dunia (default 1 = 64 x 24 petak / 32 x 12 unit). Gambarnya
# tetap utuh, tapi menutupi lebih sedikit unit sehingga pintu, pohon, bangku dan pot ikut
# mengecil dan lebih banyak isi map yang muat di layar. Harus menghasilkan petak bulat dan
# rasio 8:3, jadi kelipatan 1/8 (.5 → 32 x 12, .625 → 40 x 15, .75 → 48 x 18, .875 → 56 x 21).
#
# Tiap lukisan Gemini punya zoom sendiri, jadi 'scale'-nya diukur per map: bandingkan properti
# yang sama di lukisan dengan prop gambar-kode (pintu 1,5 x 1,75 petak, pot 1 x 1,2, bangku
# 2,1 x 1,06, pohon 2,06, tong sampah 0,75 x 0,94, lampu 3,06), lalu ambil 1/rasio. Sasarannya
# ~0,85-0,9x skala lama untuk semua map supaya ukurannya seragam di mata pemain.
#
#   map          properti terukur (petak)                rasio  scale  petak
#   J3 Kafe      pintu 1,9 x 2,4                          1,32   .625  40 x 15
#   J4 Pusat     pintu 1,8 x 2,9                          1,43   .625  40 x 15
#   K1 Kampus    bangku 2,3, tong 1,15 x 1,75             1,45   .625  40 x 15
#   K2 Taman     bangku 1,85, tong 0,8 x 1,05, lampu 3,45 0,98   .875  56 x 21
#
ART_LAYOUT = {
    'J2_JalanPasar': dict(
        scale=.625,                      # ~1,31x (pot 1,3, lampu 3,6, sepeda 2,0)
        doors=[dict(id='pasar.gereja', x=29.35, y=14.3, land=(29.35, 16.5), h=4.5, label='IN'),
               dict(id='pasar.gg', x=43.9, y=14.95, land=(43.9, 16.75), h=7.0, label='IN'),
               dict(id='pasar.minimarket', x=18.6, y=11.15, land=(18.6, 13.6), h=5.15, label='IN'),
               # Rumah kos paling kanan: titik mendarat digeser ke kiri agar tidak menabrak lampu jalan.
               dict(id='pasar.kos', x=61.5, y=11.6, land=(60.4, 13.8), h=3.0, label='IN')],
        walk=[(0, 11, 64, 13)],          # trotoar + jalan raya; deretan toko berhenti di baris 11
        blocks=[(2.6, 15.5, 2, .8),      # sepeda parkir
                (5.4, 10.8, 2.2, .5),    # meja dagangan Toko Kelontong
                (12.2, 11, 1.3, .6),     # pot depan Minimarket 24
                (22.5, 13.7, .8, .5),    # lampu jalan kiri
                (23.8, 13.6, 3.2, 1.6),  # papan "Gereja Kasih Sejati"
                (24.5, 11, 4.1, 3.4),    # sayap kiri gereja — menjorok ke trotoar
                (30.5, 11, 2.8, 3.4),    # sayap kanan gereja
                (24.5, 11, 8.8, 2.6),    # badan gereja di atas tangga serambi
                (32.8, 13.2, 2.8, 2.4),  # pot tanaman di kanan gereja
                (35.4, 14.8, 1.2, 1.2),  # pot kecil di sudut GG
                (36.2, 11, 6.5, 4.3),    # etalase kiri GG Learning Center
                (45.1, 11, 4.1, 4.3),    # etalase kanan GG Learning Center
                (36.2, 11, 13, 3.1),     # papan nama + kaca atas GG
                (50, 14.6, 1.6, 1.6),    # banner berdiri "Welcome to GG"
                (55, 10.9, 1.4, .6),     # pot depan rumah kanan
                (55.8, 13.1, 2.2, .6),   # motor parkir
                (61.7, 14, .9, .5)]),    # lampu jalan kanan
    'J3_JalanKafe': dict(
        scale=.625,                      # lukisannya ~1,32x skala prop lama
        # Titik mendarat dijauhkan 2,3 petak (0,72 unit) dari daun pintu — lebih dekat dari itu
        # pemain mendarat di dalam trigger seberang dan langsung ditanya balik.
        doors=[dict(id='kafe.puskesmas', x=33.3, y=13.7, land=(33.3, 16), h=3, label='IN'),
               # Fasad Glow berdiri lebih tinggi dari Puskesmas: ambang pintunya di petak 12,6,
               # jadi titik lantainya tepat di bawah itu dan penandanya berhenti di bawah tenda.
               dict(id='kafe.glow', x=19.8, y=13.2, land=(19.8, 15.5), h=2.9, label='IN'),
               # Pintu kayu Kafe Senja di petak 7,1-8,75; ambangnya di petak 9,9.
               dict(id='kafe.senja', x=7.95, y=13.7, land=(7.95, 16), h=3.8, label='IN')],
        walk=[(0, 13, 64, 11)],          # trotoar + jalan raya; fasad toko berhenti di baris 13
        blocks=[(5.55, 13, 1.35, .3),    # kaki papan menu Kafe Senja — jauh dari trigger pintunya
                (12.8, 13, 2.2, .5),     # pot bunga Glow
                (26.5, 13, 5.4, .7),     # tanaman Puskesmas kiri — berhenti di kusen pintu
                (34.95, 13, 1.3, .7),    # pot Puskesmas kanan; petak 31,9-34,9 dibiarkan kosong
                (46, 13, 1.3, .5),       # pot Gerai HP
                (55.3, 13, 1, .5)]),     # pot depan kedai kopi
    'J4_PusatKota': dict(
        scale=.625,                      # ~1,43x
        doors=[dict(id='pusat.bank', x=11.55, y=13.2, land=(11.55, 15.7), h=6.6, label='IN'),
               dict(id='pusat.idmaret', x=39.2, y=13.1, land=(39.2, 15.6), h=7.7, label='IN'),
               dict(id='pusat.ffc', x=33.9, y=13.7, land=(33.9, 16), h=4.5, label='IN'),
               # Penandanya sengaja berhenti di dalam lengkung: papan "MASJID AL-AMANAH"
               # menempel tepat di atas pintu (petak 6,5-7,9), jadi h lebih pendek dari
               # puncak lengkungnya (8,55) supaya label tidak menutupi papan nama.
               dict(id='pusat.masjid', x=52.9, y=14.1, land=(52.9, 16.5), h=3.4, label='IN')],
        walk=[(0, 13, 64, 11)],
        blocks=[(1.4, 13, 1.4, .4),      # pot depan Bank Syariah
                (20.6, 16.1, .9, .5),    # lampu jalan kiri
                (21.9, 13, 1.4, .4),     # perdu dalam pot
                (22, 16.4, 1.3, .5),     # pot kecil di trotoar
                (28.8, 13, 3.2, .4),     # kotak tanaman FFC
                (40.8, 16.1, .9, .5),    # lampu jalan kanan
                (45.3, 16.2, 3.4, .8),   # bangku depan Idmaret
                (57.5, 16.2, 3.5, .8)]), # bangku depan masjid
    'K1_Kampus': dict(
        scale=.625,                      # ~1,45x — properti terbesar dari keempatnya
        doors=[dict(id='kampus.fakultas', x=17.15, y=11.5, land=(17.15, 14), h=4.0, label='IN')],
        walk=[(0, 11, 64, 13)],          # plaza batu + petak rumput, mulai dasar gedung
        blocks=[(11, 11, 1.3, .3),       # pot depan Fakultas
                (13, 10.7, 2, .3),       # papan pengumuman
                (20, 11, 3.3, .3),       # deretan pot
                (30.8, 11, .8, .3),      # lampu
                (31.8, 11, 1, .3),       # tong sampah
                (55.2, 10.8, 2.8, .4),   # papan pengumuman kanan
                (61, 11, 1.8, .3),       # lampu + tong sampah kanan
                (2.3, 19.7, 2, 1.1),     # pot bunga sudut kiri bawah
                (7, 16, 1.6, .6),        # pohon petak 1
                (9.8, 19.6, 3.5, 1.3),   # bedeng bunga petak 1
                (13, 15.9, 2.3, .8),     # bangku petak 1
                (19.6, 20.1, 1.3, .8),   # tong sampah
                (25, 16.6, 1.6, .6),     # pohon petak 2
                (28.5, 19.6, 3.5, 1.3),  # bedeng bunga petak 2
                (37.3, 20.1, 1, .5),     # lampu taman
                (39.6, 16.6, 1.8, .9),   # prasasti
                (44.3, 19.4, 3.2, 1.3),  # bedeng bunga petak 3
                (45.3, 16.9, 1.6, .6),   # pohon petak 3
                (50.2, 20.1, 1.3, .8),   # tong sampah
                (50.8, 15.7, 2.8, .8),   # bangku petak 4
                (55, 16.5, 2.3, 1),      # patung dada
                (59, 19.6, 4, 1.3),      # bedeng bunga petak 4
                (60.8, 16.7, 1.6, .6)]), # pohon petak 4
    'K2_Taman': dict(
        scale=.875,                      # ~0,98x — lukisannya sudah hampir sepadan, tak perlu dikecilkan banyak
        walk=[(0, 3, 64, 21)],           # seluruh taman di bawah barisan pohon
        blocks=[(9.5, 7.5, 5.8, 2.8),    # gazebo
                (19.7, 9.6, .9, .6),     # lampu taman kiri
                (21.9, 10.6, 2.4, .9),   # bangku atas
                (24.6, 10.6, 1.1, .8),   # tong sampah
                (26.6, 7.4, 1.6, .6),    # pohon tengah atas
                (30, 5.1, 2.5, .6),      # papan informasi
                (39.9, 6.2, 6.6, 3.5),   # kolam
                (61.5, 7.4, 1.6, .6),    # pohon kanan atas
                (4.5, 16, 1.6, .6),      # pohon kiri bawah
                (15.4, 17.4, 4, 1.7),    # bedeng bunga kiri
                (30.6, 17, 1.6, .6),     # pohon di tepi jalur
                (35.9, 16.9, 2.8, 1),    # bangku bawah
                (43.9, 13.4, 3.6, 1.7),  # bedeng bunga kanan
                (49.7, 18.9, 1, .6),     # lampu taman kanan
                (57, 16.7, 1.6, .6)]),   # pohon kanan bawah
}

# Posisi pin di peta HP (piksel pada PhoneMap.png) — termasuk area lama per bab.
PIN_SIZE = (360, 216)
# Jalur jalan kaki antar area yang bersebelahan di peta HP: pemain berjalan sampai tepi map
# lalu muncul di tepi seberang tetangganya, jadi arah kiri-kanan di dunia sama dengan urutan
# pin di Peta HP. Pasangan ditulis (barat, timur) — yang barat ada di kiri peta.
# Area scene bab (Depan stasiun, Depan warung) ikut di sini; pintunya sudah digambar di scene,
# AdventureGame tinggal mengarahkannya ulang ke tetangga yang benar (lihat BuildRoadDoors).
# Jalur menanjak/menurun di peta (Jalan Kafe ke Taman, Pusat Kota ke Kampus, Depan warung ke
# Gang) belum jadi jalan kaki: map jalan tidak punya tepi atas/bawah yang bisa dilewati, jadi
# ke sana tetap lewat Peta HP.
ROADS = [
    ('Depan stasiun', 'Jalan Pasar'),
    ('Jalan Pasar', 'Jalan Kafe'),
    ('Jalan Kafe', 'Pusat Kota'),
    ('Pusat Kota', 'Depan warung'),
    ('Taman Cempaka', 'Kampus Cempaka'),
]

PINS = {  # area: (x, y, label di atas pin?)
    'Dalam stasiun': (40, 108, True), 'Depan stasiun': (40, 150, False), 'Jalan Pasar': (100, 150, True),
    'Jalan Kafe': (160, 150, False), 'Pusat Kota': (220, 150, True), 'Depan warung': (290, 150, False),
    'Dalam Warung Bu Siti': (330, 122, True), 'Kampus Cempaka': (220, 42, False), 'Taman Cempaka': (150, 70, True),
    'Halaman kos': (230, 196, True), 'Lantai 1 kos': (175, 196, True),
    'Kamar Dimas': (120, 196, True),
}


def draw_map(spec):
    rng = random.Random(spec['seed'])
    mw, mh = spec.get('size', (W, H))
    cv = Canvas(mw, mh)
    tpl = spec['template']
    walk, blocks = [], []
    if tpl == 'indoor':
        floor_a, floor_b = (C[c] for c in spec.get('floor', ('walk', 'walk2')))
        room(cv, floor_a, floor_b, C[spec.get('wall', 'wall')], spec['front'])
        walk.append((0, 3, mw, mh - 5))
        base = 3
    elif tpl == 'church':
        church_room(cv, spec['front'])
        walk.append((0, 3, mw, mh - 5))
        base = 3
    elif tpl == 'mosque':
        mosque_room(cv, spec['front'])
        walk.append((0, 3, mw, mh - 5))
        base = 3
    elif tpl == 'gg':
        gg_room(cv, spec['front'])
        walk.append((0, 4, mw, mh - 6))
        base = 4
    elif tpl == 'mart':
        mart_room(cv, spec['front'], spec.get('sign', 'MINIMARKET 24'),
                  spec.get('band', ('red', 'blue', 'cream')), spec.get('tile', ('sky2', 'sky', 'white')))
        walk.append((0, 3, mw, mh - 5))
        base = 3
    elif tpl == 'bank':
        bank_room(cv, spec['front'])
        walk.append((0, 3, mw, mh - 5))
        base = 3
    elif tpl == 'ffc':
        ffc_room(cv, spec['front'])
        walk.append((0, 3, mw, mh - 5))
        base = 3
    elif tpl == 'cafe':
        cafe_room(cv, spec['front'])
        walk.append((0, 3, mw, mh - 5))
        base = 3
    elif tpl == 'street':
        ground_grass(cv, range(0, BASE), rng)
        ground_sidewalk(cv, range(BASE, ROAD_TOP))
        ground_road(cv, ROAD_TOP, ROAD_BOTTOM, rng, spec.get('crosswalk'))
        ground_sidewalk(cv, range(ROAD_BOTTOM + 1, mh))
        walk.append((0, BASE, mw, mh - BASE))
        base = BASE
    elif tpl == 'campus':
        ground_grass(cv, range(0, mh), rng)
        ground_paving(cv, range(BASE, BASE + 3), C['stone'], C['walk'])
        ground_paving(cv, range(BASE + 3, mh), C['walk'], C['walk2'])
        for tx in range(0, mw):
            for ty in range(14, 21):
                if tx % 16 in range(2, 12) and ty in range(14, 20):
                    x, y = tx * T, ty * T
                    cv.r(x, y, T, T, C['grass'] if (tx + ty) % 2 else C['grass2'])
        walk.append((0, BASE, mw, mh - BASE))
        base = BASE
    elif tpl == 'park':
        ground_grass(cv, range(0, mh), rng)
        for tx in range(mw):  # jalan setapak melintang & membujur
            for ty in (12, 13):
                cv.r(tx * T, ty * T, T, T, C['dirt'] if (tx + ty) % 2 else C['walk2'])
        for ty in range(3, mh):
            for tx in (31, 32):
                cv.r(tx * T, ty * T, T, T, C['dirt'] if (tx + ty) % 2 else C['walk2'])
        walk.append((0, 3, mw, mh - 3))
        base = 3
    else:  # gang
        ground_grass(cv, range(0, BASE), rng)
        ground_paving(cv, range(BASE, mh), C['stone'], C['stone2'])
        for tx in range(mw):
            cv.r(tx * T, 18 * T, T, 2, C['stone2'])
        walk.append((0, BASE, mw, mh - BASE))
        base = BASE

    for kind, tx, ty in spec['back']:
        PROPS[kind](cv, tx * T, ty * T)
    for b in spec['buildings']:
        building(cv, b, base, rng)
    for kind, tx, ty in sorted(spec['props'], key=lambda p: p[2]):
        fn = PROPS.get(kind)
        box = fn(cv, tx * T, ty * T) if fn else PROPS_RNG[kind](cv, tx * T, ty * T, rng)
        if box and box[1] + box[3] > base * T:   # hanya properti di lantai yang jadi penghalang
            blocks.append(box)
    if tpl == 'park':
        cv.shade(0, 3 * T, mw * T, 3)

    OUT.mkdir(parents=True, exist_ok=True)
    art = install_art(spec['file'])
    width, height, k = mw, mh, 1.0
    if art:
        # Lukisan tangan menang: gambar kode disimpan sebagai cadangan, geometrinya diganti.
        BACKUP.mkdir(parents=True, exist_ok=True)
        cv.img.convert('RGB').save(BACKUP / f"{spec['file']}.png")
        layout = ART_LAYOUT[spec['file']]
        # ART_LAYOUT ditulis dalam petak; penghalang dari PROPS sudah dalam piksel.
        walk = layout['walk']
        blocks = [tuple(v * T for v in box) for box in layout['blocks']]
        width, height, k = art_grid(spec['file'])
    else:
        cv.img.convert('RGB').save(OUT / f"{spec['file']}.png")
    px = lambda v: round(v * T * k)      # petak → piksel map (sudah diskalakan)
    pp = lambda v: round(v * k)          # piksel gambar → piksel map
    return dict(
        id=spec['id'], area=spec['area'], file=spec['file'], width=width, height=height,
        doors=[dict(id=d['id'], label=d['label'], x=px(d['x']), y=px(d['y']), h=px(d['h']),
                    landX=px(d['land'][0]), landY=px(d['land'][1]), area='', tx=0, ty=0)
               for d in doors_of(spec)],
        walk=[dict(x=px(x), y=px(y), w=px(w), h=px(h)) for x, y, w, h in walk],
        blocks=[dict(x=pp(x), y=pp(y), w=pp(w), h=pp(h)) for x, y, w, h in blocks],
        spawnX=round((spec['spawn'][0] * T + T // 2) * k), spawnY=round((spec['spawn'][1] * T + T // 2) * k),
        npcs=[dict(id=n, x=round((x * T + T // 2) * k), y=round((y * T + T - 2) * k)) for n, x, y in spec['npcs']],
    )



def doors_of(spec):
    """Daftar pintu satu map — dari ART_LAYOUT kalau map-nya berlukisan, selain itu dari MAPS."""
    layout = ART_LAYOUT.get(spec['file'])
    if layout is not None and (ART / f"{spec['file']}.png").exists():
        return layout.get('doors', ())
    return spec.get('doors', ())


def link_doors(maps):
    """Isi tujuan tiap pintu dari pasangannya di LINKS: area seberang + titik mendaratnya."""
    door = {d['id']: (m, d) for m in maps for d in m['doors']}
    paired = set()
    for a, b in LINKS:
        for x, y in ((a, b), (b, a)):
            if x not in door:
                raise SystemExit(f"[kota] LINKS menyebut pintu yang tidak ada: {x}")
            here, other = door[x][1], door[y]
            here['area'], here['tx'], here['ty'] = other[0]['area'], other[1]['landX'], other[1]['landY']
            paired.add(x)
    for did, (m, d) in door.items():
        if did not in paired:
            raise SystemExit(f"[kota] pintu {did} di {m['area']} belum dipasangkan di LINKS")
    for m in maps:
        for d in m['doors']:
            d.pop('landX'), d.pop('landY'), d.pop('id')

def art_grid(file):
    """(petak lebar, petak tinggi, skala) untuk map berlukisan — rasio harus tetap W:H."""
    k = ART_LAYOUT[file].get('scale', 1)
    width, height = W * k, H * k
    if width != int(width) or height != int(height):
        raise SystemExit(f"[kota] scale {k} untuk {file} tidak menghasilkan petak bulat ({width} x {height})")
    return int(width), int(height), k


def art_size(file):
    """Ukuran piksel lukisan di Resources: jejak dunia map x ART_PPU."""
    width, height, _ = art_grid(file)
    return round(width * T / PPU * ART_PPU), round(height * T / PPU * ART_PPU)


def install_art(file):
    """Salin lukisan tangan art/<file>.png ke Resources pada skala map-nya. True kalau dipakai."""
    src = ART / f"{file}.png"
    if file not in ART_LAYOUT or not src.exists():
        return False
    img = Image.open(src).convert('RGB')
    if abs(img.width / img.height - W / H) > 0.01:
        raise SystemExit(f"[kota] {src.name} harus berasio {W}:{H} (8:3), bukan {img.width}x{img.height}")
    size = art_size(file)
    if img.size != size:
        img = img.resize(size, Image.LANCZOS)
    img.save(OUT / f"{file}.png")
    return True


def draw_phone_map():
    w, h = PIN_SIZE
    img = Image.new('RGB', (w, h), C['grass'])
    d = ImageDraw.Draw(img)
    rng = random.Random(9)
    for _ in range(140):
        x, y = rng.randrange(w), rng.randrange(h)
        d.rectangle([x, y, x + 1, y + 2], fill=C['grass3'])
    road = C['cream']

    def street(points, width=10):
        d.line(points, fill=C['out'], width=width + 4)
        d.line(points, fill=road, width=width)

    street([(20, 150), (340, 150)])                          # jalan utama
    street([(220, 150), (220, 40)])                          # jalan kampus
    street([(150, 150), (150, 70), (220, 70)], 8)            # ke taman
    street([(290, 150), (290, 200)], 8)                      # turunan ke jalan kos
    street([(110, 196), (300, 196)], 8)                      # jalan kos
    street([(40, 150), (40, 110)], 8)                        # stasiun
    for (x0, y0, x1, y1, c) in [(12, 92, 70, 132, 'roof'), (80, 120, 124, 140, 'wall2'), (140, 118, 184, 140, 'roof3'),
                                (196, 116, 248, 140, 'teal2'), (268, 118, 312, 140, 'roof'), (316, 118, 346, 140, 'wood'),
                                (190, 16, 252, 42, 'brick'), (120, 52, 184, 92, 'grass2'),
                                (120, 166, 214, 188, 'roof2')]:
        d.rectangle([x0, y0, x1, y1], fill=C[c], outline=C['out'])
    d.ellipse([140, 58, 164, 78], fill=C['water'], outline=C['out'])
    img.save(OUT / 'PhoneMap.png')


def write_meta(path, guid, ppu, text=False):
    if text:
        body = f"fileFormatVersion: 2\nguid: {guid}\nTextScriptImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    else:
        body = TEXTURE_META.format(guid=guid, ppu=ppu)
    Path(str(path) + '.meta').write_text(body)


def ensure_meta(path, ppu=PPU, text=False):
    meta = Path(str(path) + '.meta')
    guid = uuid.uuid5(uuid.NAMESPACE_URL, 'alif-kota/' + path.name).hex   # stabil antar-jalan
    if meta.exists():
        body = meta.read_text()
        if guid in body and (text or f'spritePixelsToUnits: {ppu}\n' in body):
            return
    write_meta(path, guid, ppu, text)


TEXTURE_META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: {ppu}
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationMethod: 0
  spriteTessellationDetail: -1
  spriteGeometrySubdivision: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def main():
    maps = [draw_map(spec) for spec in MAPS]
    link_doors(maps)
    draw_phone_map()
    city = dict(tile=T, ppu=PPU, mapWidth=PIN_SIZE[0], mapHeight=PIN_SIZE[1], maps=maps,
                pins=[dict(area=a, x=x, y=y, up=up) for a, (x, y, up) in PINS.items()],
                roads=[dict(west=w, east=e) for w, e in ROADS])
    (OUT / 'city.json').write_text(json.dumps(city, indent=1, ensure_ascii=False))
    for spec in MAPS:
        art = (ART / f"{spec['file']}.png").exists() and spec['file'] in ART_LAYOUT
        ensure_meta(OUT / f"{spec['file']}.png", ppu=ART_PPU if art else PPU)
    ensure_meta(OUT / 'PhoneMap.png', ppu=100)
    ensure_meta(OUT / 'city.json', text=True)
    folder_meta = Path(str(OUT) + '.meta')
    if not folder_meta.exists():
        folder_meta.write_text(f"fileFormatVersion: 2\nguid: {uuid.uuid5(uuid.NAMESPACE_URL, 'alif-kota/folder').hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
    painted = [m['file'] for m in MAPS if (ART / f"{m['file']}.png").exists() and m['file'] in ART_LAYOUT]
    print(f"{len(maps)} map + PhoneMap.png + city.json → {OUT.relative_to(ROOT)}")
    print(f"  lukisan tangan ({len(painted)}): {', '.join(painted) or '—'}")


if __name__ == '__main__':
    main()
