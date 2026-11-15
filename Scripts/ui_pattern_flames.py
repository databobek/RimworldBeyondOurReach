# Seamless, looping flame-like mask frames: white with alpha, tileable in x and y, and frame N wraps to frame 0.
# Flames rise: each noise layer scrolls up a whole number of tile heights per loop, so the loop is seamless in time.
import sys, numpy as np
from PIL import Image, ImageFilter
out, size, frames = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
rng = np.random.default_rng(7)

def lattice(gx, gy):
    return rng.random((gy, gx))

def sample(lat, x, y):
    # periodic value noise with smoothstep interpolation; x, y in lattice units
    gy, gx = lat.shape
    x0 = np.floor(x).astype(int); y0 = np.floor(y).astype(int)
    fx = x - x0; fy = y - y0
    sx = fx * fx * (3 - 2 * fx); sy = fy * fy * (3 - 2 * fy)
    a = lat[y0 % gy, x0 % gx]; b = lat[y0 % gy, (x0 + 1) % gx]
    c = lat[(y0 + 1) % gy, x0 % gx]; d = lat[(y0 + 1) % gy, (x0 + 1) % gx]
    return (a * (1 - sx) + b * sx) * (1 - sy) + (c * (1 - sx) + d * sx) * sy

# tall tongues: finer across than up; each layer is (lattice, scroll in tile heights per loop, weight)
layers = [(lattice(10, 3), 1, 0.5), (lattice(20, 6), 2, 0.3), (lattice(40, 12), 3, 0.2)]
wobble = lattice(4, 2)
ys, xs = np.mgrid[0:size, 0:size] / size  # 0..1
for k in range(frames):
    t = k / frames
    # sideways sway, periodic in y and time
    dx = (sample(wobble, xs * 4, (ys + t) * 2) - 0.5) * 0.08
    n = np.zeros_like(xs)
    for lat, speed, w in layers:
        gy, gx = lat.shape
        n += w * sample(lat, (xs + dx) % 1 * gx, ((ys + speed * t) % 1) * gy)
    a = np.clip((n - 0.44) / 0.26, 0, 1) ** 1.4
    img = Image.fromarray((a * 255).astype(np.uint8), 'L')
    # blur with wrap so edges stay seamless: tile 3x3, blur, crop the middle
    big = Image.new('L', (size * 3, size * 3))
    for i in range(3):
        for j in range(3):
            big.paste(img, (i * size, j * size))
    img = big.filter(ImageFilter.GaussianBlur(size / 96)).crop((size, size, size * 2, size * 2))
    Image.merge('RGBA', (Image.new('L', img.size, 255),) * 3 + (img,)).save(f'{out}/Flame_{k:02d}.png', optimize=True)
