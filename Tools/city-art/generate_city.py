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
FONT = ROOT / "Assets/Resources/Fonts/PixelifySans-SemiBold.ttf"
T = 16            # piksel per petak
PPU = 32          # 1 petak = 0,5 unit
W, H = 64, 24     # ukuran map (petak)


def hx(s):
    return tuple(int(s[i:i + 2], 16) for i in (0, 2, 4))


C = {k: hx(v) for k, v in dict(
    out='3e1f10', road='5b5563', road2='534d5b', road3='625c6b', lane='e9e2d0', curb='9c8f86', curb2='bfb3a6',
    walk='dcc7a3', walk2='d2bb93', grout='c3aa80', grass='7aa25a', grass2='6b934d', grass3='86ad63', dirt='b9955f',
    wall='efe0c2', wall2='dccaa6', roof='b5562e', roof2='96431f', roof3='cf7040', wood='8a5a3a', wood2='6e4529',
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
        self.img = Image.new('RGBA', (w * T, h * T), C['grass'])
        self.d = ImageDraw.Draw(self.img)
        self.d.fontmode = '1'   # tanpa antialias: piksel tetap tajam

    def r(self, x, y, w, h, c):
        if w > 0 and h > 0:
            self.d.rectangle([x, y, x + w - 1, y + h - 1], fill=c)

    def box(self, x, y, w, h, c, o=C['out']):
        self.r(x, y, w, h, o)
        self.r(x + 1, y + 1, w - 2, h - 2, c)

    def shade(self, x, y, w, h):
        overlay = Image.new('RGBA', self.img.size, (0, 0, 0, 0))
        ImageDraw.Draw(overlay).rectangle([x, y, x + w - 1, y + h - 1], fill=C['shadow'])
        self.img.alpha_composite(overlay)

    def text(self, x, y, w, s, color, font=FONT_SIGN):
        tw = self.d.textlength(s, font=font)
        self.d.text((x + (w - tw) / 2, y), s, font=font, fill=color)


# ───────────────────────────── tanah ─────────────────────────────

def ground_grass(cv, rows, rng):
    for ty in rows:
        for tx in range(W):
            x, y = tx * T, ty * T
            cv.r(x, y, T, T, C['grass'] if (tx + ty) % 2 else C['grass2'])
            for _ in range(2):
                gx, gy = rng.randrange(T - 2), rng.randrange(T - 3)
                cv.r(x + gx, y + gy, 1, 3, C['grass3'])


def ground_sidewalk(cv, rows):
    for ty in rows:
        for tx in range(W):
            x, y = tx * T, ty * T
            cv.r(x, y, T, T, C['walk'] if (tx + ty) % 2 else C['walk2'])
            cv.r(x, y + T - 1, T, 1, C['grout'])
            cv.r(x + T - 1, y, 1, T, C['grout'])


def ground_road(cv, top, bottom, rng, crosswalk=None):
    cv.r(0, top * T, W * T, T, C['curb'])
    cv.r(0, top * T, W * T, 3, C['curb2'])
    for ty in range(top + 1, bottom):
        for tx in range(W):
            cv.r(tx * T, ty * T, T, T, rng.choice([C['road'], C['road'], C['road2'], C['road3']]))
    mid = (top + 1 + bottom) * T // 2 - 2
    for tx in range(0, W, 4):
        cv.r(tx * T + 4, mid, 2 * T, 3, C['lane'])
    if crosswalk is not None:
        for ty in range(top + 1, bottom):
            for k in range(4):
                cv.r(crosswalk * T + k * 8, ty * T + 3, 5, T - 6, C['lane'])
    cv.r(0, bottom * T, W * T, 3, C['curb2'])
    cv.r(0, bottom * T + 3, W * T, T - 3, C['curb'])


def ground_paving(cv, rows, a, b):
    for ty in rows:
        for tx in range(W):
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


PROPS = dict(tree=p_tree, palm=p_palm, plant=p_plant, lamp=p_lamp, bench=p_bench, table=p_table,
             umbrella=p_umbrella_table, halte=p_halte, bike=p_bike, motor=p_motor, bin=p_bin,
             fountain=p_fountain, gazebo=p_gazebo, pond=p_pond, board=p_board)
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
         spawn=(32, 15), npcs=[]),
    dict(id='J3', area='Jalan Kafe', file='J3_JalanKafe', template='street', seed=3, crosswalk=12,
         buildings=[dict(x=2, w=10, h=5, kind='kafe', name='KAFE SENJA', sign='5a3a26', wall='e8d2b0'),
                    dict(x=14, w=9, h=5, kind='kosmetik', name='GLOW', sign='d86a8f', wall='fbe9ee'),
                    dict(x=25, w=8, h=5, kind='gerai', name='GERAI HP', sign='3b6ea8', wall='eef1f4'),
                    dict(x=37, w=10, h=5, kind='kafe', name='KEDAI KOPI', sign='6e4529', wall='e9d9bb'),
                    dict(x=50, w=12, h=6, kind='minimarket', name='TOKO BUKU', sign='2f6f6a', wall='f2ead8')],
         back=[('tree', 33, 0), ('tree', 47, 1), ('tree', 62, 0)],
         props=[('umbrella', 3, 12), ('umbrella', 8, 12), ('plant', 12, 11), ('lamp', 24, 12), ('plant', 33, 11),
                ('table', 38, 13), ('table', 43, 13), ('bench', 55, 13), ('lamp', 48, 12), ('bin', 34, 13)],
         spawn=(30, 15), npcs=[('npc:kirana', 18, 14), ('npc:tara', 41, 15)]),
    dict(id='J4', area='Pusat Kota', file='J4_PusatKota', template='street', seed=4, crosswalk=30,
         buildings=[dict(x=3, w=12, h=6, kind='bank', name='BANK SYARIAH', sign='2f6f6a', wall='f2ead8'),
                    dict(x=16, w=3, h=4, kind='atm', name='ATM', sign='3b6ea8', wall='eef1f4'),
                    dict(x=42, w=14, h=6, kind='masjid', name='MASJID AL-AMANAH')],
         back=[('tree', 21, 0), ('tree', 26, 1), ('tree', 34, 0), ('tree', 58, 0)],
         props=[('fountain', 26, 11), ('bench', 22, 14), ('bench', 36, 14), ('flowers', 21, 12), ('flowers', 34, 12),
                ('lamp', 20, 12), ('lamp', 40, 12), ('palm', 57, 11), ('plant', 2, 11), ('board', 60, 12)],
         spawn=(30, 16), npcs=[('npc:bank', 9, 13)]),
    dict(id='K1', area='Kampus Cempaka', file='K1_Kampus', template='campus', seed=5,
         buildings=[dict(x=4, w=24, h=7, kind='fakultas', name='FAKULTAS EKONOMI & BISNIS', sign='2f6f6a', wall='f2ead8'),
                    dict(x=29, w=8, h=4, kind='gerbang', name='UNIV. CEMPAKA', sign='a4553a', wall='d9c4a0'),
                    dict(x=38, w=6, h=4, kind='fotokopi', name='FOTOKOPI', sign='3b6ea8'),
                    dict(x=46, w=14, h=7, kind='fakultas', name='GEDUNG REKTORAT', sign='5a3a26', wall='efe0c2')],
         back=[('tree', 1, 0), ('tree', 29, 0), ('tree', 43, 0), ('tree', 61, 0)],
         props=[('tree', 6, 14), ('tree', 22, 14), ('tree', 44, 14), ('tree', 58, 14), ('bench', 12, 15), ('bench', 50, 15),
                ('board', 36, 12), ('bike', 38, 12), ('bike', 40, 12), ('lamp', 30, 13), ('lamp', 34, 13), ('flowers', 26, 19)],
         spawn=(32, 17), npcs=[('npc:fadli', 16, 13)]),
    dict(id='K2', area='Taman Cempaka', file='K2_Taman', template='park', seed=6,
         buildings=[],
         back=[('tree', x, 0) for x in range(0, 64, 5)],
         props=[('gazebo', 10, 6), ('pond', 40, 7), ('tree', 4, 14), ('tree', 26, 5), ('tree', 30, 15), ('tree', 56, 14),
                ('tree', 60, 6), ('bench', 22, 11), ('bench', 36, 17), ('flowers', 44, 14), ('flowers', 16, 18),
                ('board', 30, 4), ('lamp', 20, 8), ('lamp', 50, 16), ('bin', 24, 11)],
         spawn=(32, 12), npcs=[]),
    dict(id='P1', area='Gang Permukiman', file='P1_Gang', template='gang', seed=7,
         buildings=[dict(x=2, w=8, h=4, kind='rumah', wall='e9d9bb'),
                    dict(x=11, w=9, h=5, kind='kos', name='KOS CEMPAKA', sign='96431f', wall='efe0c2'),
                    dict(x=22, w=8, h=4, kind='warung', name='WARUNG BIMA', sign='6e4529', wall='f2e2c4'),
                    dict(x=32, w=7, h=4, kind='rumah', wall='f4ecdd'),
                    dict(x=41, w=8, h=4, kind='rumah', wall='e8d2b0'),
                    dict(x=51, w=11, h=5, kind='kos', name='KONTRAKAN', sign='5a3a26', wall='e9d9bb')],
         back=[('tree', 21, 0), ('tree', 40, 1), ('tree', 50, 0)],
         props=[('clothesline', 32, 12), ('motor', 9, 13), ('motor', 47, 13), ('plant', 20, 11), ('plant', 31, 11),
                ('plant', 40, 11), ('bin', 49, 12), ('lamp', 30, 13), ('bike', 60, 14), ('plant', 10, 11)],
         spawn=(28, 16), npcs=[('npc:bima', 26, 13)]),
]

# Posisi pin di peta HP (piksel pada PhoneMap.png) — termasuk area lama per bab.
PIN_SIZE = (360, 216)
PINS = {  # area: (x, y, label di atas pin?)
    'Dalam stasiun': (40, 108, True), 'Depan stasiun': (40, 150, False), 'Jalan Pasar': (100, 150, True),
    'Jalan Kafe': (160, 150, False), 'Pusat Kota': (220, 150, True), 'Depan warung': (290, 150, False),
    'Dalam Warung Bu Siti': (330, 122, True), 'Kampus Cempaka': (220, 42, False), 'Taman Cempaka': (150, 70, True),
    'Gang Permukiman': (290, 196, True), 'Halaman kos': (230, 196, True), 'Lantai 1 kos': (175, 196, True),
    'Kamar Dimas': (120, 196, True),
}


def draw_map(spec):
    rng = random.Random(spec['seed'])
    cv = Canvas(W, H)
    tpl = spec['template']
    walk, blocks = [], []
    if tpl == 'street':
        ground_grass(cv, range(0, BASE), rng)
        ground_sidewalk(cv, range(BASE, ROAD_TOP))
        ground_road(cv, ROAD_TOP, ROAD_BOTTOM, rng, spec.get('crosswalk'))
        ground_sidewalk(cv, range(ROAD_BOTTOM + 1, H))
        walk.append((0, BASE, W, H - BASE))
        base = BASE
    elif tpl == 'campus':
        ground_grass(cv, range(0, H), rng)
        ground_paving(cv, range(BASE, BASE + 3), C['stone'], C['walk'])
        ground_paving(cv, range(BASE + 3, H), C['walk'], C['walk2'])
        for tx in range(0, W):
            for ty in range(14, 21):
                if tx % 16 in range(2, 12) and ty in range(14, 20):
                    x, y = tx * T, ty * T
                    cv.r(x, y, T, T, C['grass'] if (tx + ty) % 2 else C['grass2'])
        walk.append((0, BASE, W, H - BASE))
        base = BASE
    elif tpl == 'park':
        ground_grass(cv, range(0, H), rng)
        for tx in range(W):  # jalan setapak melintang & membujur
            for ty in (12, 13):
                cv.r(tx * T, ty * T, T, T, C['dirt'] if (tx + ty) % 2 else C['walk2'])
        for ty in range(3, H):
            for tx in (31, 32):
                cv.r(tx * T, ty * T, T, T, C['dirt'] if (tx + ty) % 2 else C['walk2'])
        walk.append((0, 3, W, H - 3))
        base = 3
    else:  # gang
        ground_grass(cv, range(0, BASE), rng)
        ground_paving(cv, range(BASE, H), C['stone'], C['stone2'])
        for tx in range(W):
            cv.r(tx * T, 18 * T, T, 2, C['stone2'])
        walk.append((0, BASE, W, H - BASE))
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
        cv.shade(0, 3 * T, W * T, 3)

    OUT.mkdir(parents=True, exist_ok=True)
    cv.img.convert('RGB').save(OUT / f"{spec['file']}.png")
    return dict(
        id=spec['id'], area=spec['area'], file=spec['file'], width=W, height=H,
        walk=[dict(x=x * T, y=y * T, w=w * T, h=h * T) for x, y, w, h in walk],
        blocks=[dict(x=x, y=y, w=w, h=h) for x, y, w, h in blocks],
        spawnX=spec['spawn'][0] * T + T // 2, spawnY=spec['spawn'][1] * T + T // 2,
        npcs=[dict(id=n, x=x * T + T // 2, y=y * T + T - 2) for n, x, y in spec['npcs']],
    )


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
    street([(290, 150), (290, 200)], 8)                      # gang permukiman
    street([(110, 196), (300, 196)], 8)                      # jalan kos
    street([(40, 150), (40, 110)], 8)                        # stasiun
    for (x0, y0, x1, y1, c) in [(12, 92, 70, 132, 'roof'), (80, 120, 124, 140, 'wall2'), (140, 118, 184, 140, 'roof3'),
                                (196, 116, 248, 140, 'teal2'), (268, 118, 312, 140, 'roof'), (316, 118, 346, 140, 'wood'),
                                (190, 16, 252, 42, 'brick'), (120, 52, 184, 92, 'grass2'), (268, 166, 312, 188, 'wall2'),
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
    if meta.exists() and guid in meta.read_text():
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
    draw_phone_map()
    city = dict(tile=T, ppu=PPU, mapWidth=PIN_SIZE[0], mapHeight=PIN_SIZE[1], maps=maps,
                pins=[dict(area=a, x=x, y=y, up=up) for a, (x, y, up) in PINS.items()])
    (OUT / 'city.json').write_text(json.dumps(city, indent=1, ensure_ascii=False))
    for spec in MAPS:
        ensure_meta(OUT / f"{spec['file']}.png")
    ensure_meta(OUT / 'PhoneMap.png', ppu=100)
    ensure_meta(OUT / 'city.json', text=True)
    folder_meta = Path(str(OUT) + '.meta')
    if not folder_meta.exists():
        folder_meta.write_text(f"fileFormatVersion: 2\nguid: {uuid.uuid5(uuid.NAMESPACE_URL, 'alif-kota/folder').hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
    print(f"{len(maps)} map + PhoneMap.png + city.json → {OUT.relative_to(ROOT)}")


if __name__ == '__main__':
    main()
