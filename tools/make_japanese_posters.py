from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import cv2
import numpy as np

SOURCE_DIR = Path(r"C:\Users\ihao_ting.PMR.000\Desktop\接機版")
FONT = r"C:\Windows\Fonts\YuGothB.ttc"
BASE_WIDTH = 1055
BASE_HEIGHT = 1491

# Text areas from the original poster, expressed in the 1055x1491 layout.
MASKS = [
    (205, 170, 850, 305),
    (155, 390, 900, 610),
    (160, 620, 900, 705),
    (175, 720, 880, 795),
    (145, 900, 915, 1100),
    (155, 1110, 900, 1195),
    (250, 1200, 805, 1285),
]

TEXT = [
    ("ようこそお越しくださいました", 527, 235, 54, (123, 30, 30)),
    ("中島 理", 527, 505, 113, (0, 50, 100)),
    ("株式会社有沢製作所", 527, 662, 42, (42, 139, 214)),
    ("事業戦略推進本部 本部長", 527, 760, 31, (0, 40, 86)),
    ("齋藤 吉男", 527, 995, 104, (0, 50, 100)),
    ("新揚科技股份有限公司", 527, 1150, 41, (42, 139, 214)),
    ("研究開発副総経理", 527, 1245, 37, (0, 40, 86)),
]


def fit_font(text, max_size, max_width):
    size = max_size
    while size > 12:
        font = ImageFont.truetype(FONT, size)
        bbox = font.getbbox(text)
        if bbox[2] - bbox[0] <= max_width:
            return font
        size -= 1
    return ImageFont.truetype(FONT, 12)


def make_poster(source):
    image = Image.open(source).convert("RGB")
    source_dpi = Image.open(source).info.get("dpi")
    width, height = image.size
    sx = width / BASE_WIDTH
    sy = height / BASE_HEIGHT
    draw = ImageDraw.Draw(image)

    source_pixels = cv2.cvtColor(np.array(image), cv2.COLOR_RGB2BGR)
    text_mask = np.zeros((height, width), dtype=np.uint8)
    for x1, y1, x2, y2 in MASKS:
        left, top = round(x1 * sx), round(y1 * sy)
        right, bottom = round(x2 * sx), round(y2 * sy)
        text_mask[top:bottom, left:right] = 255

    repaired = cv2.inpaint(source_pixels, text_mask, max(10, round(12 * ((sx + sy) / 2))), cv2.INPAINT_NS)
    image = Image.fromarray(cv2.cvtColor(repaired, cv2.COLOR_BGR2RGB))
    draw = ImageDraw.Draw(image)

    for text, x, y, size, color in TEXT:
        scaled_size = round(size * ((sx + sy) / 2))
        font = fit_font(text, scaled_size, round(width * 0.78))
        draw.text((round(x * sx), round(y * sy)), text, font=font, fill=color, anchor="mm")

    output = source.with_name(f"{source.stem}_日文版{source.suffix}")
    if source_dpi:
        image.save(output, dpi=source_dpi)
    else:
        image.save(output)
    return output


for source in sorted(SOURCE_DIR.glob("*.png")):
    if "日文版" not in source.stem:
        print(make_poster(source))
