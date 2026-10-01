"""Ninja Adventure 에셋 팩(CC0)에서 이 게임이 쓰는 그림만 골라 프로젝트로 복사한다.

    python Tools/import_ninja_art.py ["팩 폴더 경로"]

- 몬스터: 시트에서 정면(첫 칸) 한 프레임만 잘라 Monsters/{speciesId}.png (파일 이름 = speciesId)
  → Unity 메뉴 WordRPG > Data > Link Monster Art 가 같은 이름의 몬스터 에셋에 연결한다
- 주인공: 4방향 × 걷기 4프레임 시트 그대로 (게임이 잘라 씀)
- 팩에 어울리는 그림이 없는 몬스터(펜촉이·깃펜기사)는 넣지 않는다 → 임시 도형 그대로
필요한 Pillow: pip install pillow
"""
import os
import shutil
import sys

from PIL import Image

DEFAULT_PACK = r"F:\Unity\김수연습\Ninja Adventure - Asset Pack\Ninja Adventure - Asset Pack"
PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(PROJECT, "Assets", "Resources", "Art", "NinjaAdventure")

# speciesId: (팩 안의 시트 경로, 한 칸 크기)
MONSTERS = {
    "lumi": ("Actor/Monsters/LanternRed/SpriteSheet.png", 16),          # 등불이
    "sage_lantern": ("Actor/Monsters/LanternGreen/SpriteSheet.png", 16),  # 지혜등불
    "bookshell": ("Actor/Monsters/Mollusc/Mollusc.png", 16),             # 책껍질
    "encyclotortoise": ("Actor/Monsters/Mollusc2/Mollusc2.png", 16),     # 백과거북
    "ink_slime": ("Actor/Monsters/Slime/Slime.png", 16),                 # 잉크 슬라임
    "scribble_bat": ("Actor/Monsters/BlueBat/SpriteSheet.png", 16),      # 낙서 박쥐
    "forget_goblin": ("Actor/Monsters/KappaGreen/SpriteSheet.png", 16),  # 까먹깨비
    "boss_forget_king": ("Actor/Boss/GiantSpirit/Idle.png", 50),         # 까먹대왕
}

PLAYER = "Actor/Characters/Boy/SpriteSheet.png"

# 소리: 파일 이름 = 게임 코드의 이름(Music·Sfx 열거형을 소문자로)
MUSIC = {
    "title": "Audio/Musics/1 - Adventure Begin.ogg",
    "meadow": "Audio/Musics/5 - Peaceful.ogg",
    "library": "Audio/Musics/13 - Mystical.ogg",
    "battle": "Audio/Musics/17 - Fight.ogg",
    "boss": "Audio/Musics/28 - Tension.ogg",
}
SFX = {
    "click": "Audio/Sounds/Menu/Accept.wav",
    "correct": "Audio/Sounds/Bonus/Bonus.wav",
    "wrong": "Audio/Sounds/Alert/Alert.wav",
    "hit": "Audio/Sounds/Hit & Impact/Hit1.wav",
    "critical": "Audio/Sounds/Hit & Impact/Impact.wav",
    "heal": "Audio/Sounds/Magic & Skill/Heal.wav",
    "shield": "Audio/Sounds/Magic & Skill/Magic1.wav",
    "fail": "Audio/Sounds/Menu/Cancel.wav",
    "faint": "Audio/Sounds/Hit & Impact/Hit5.wav",
    "encounter": "Audio/Sounds/Whoosh & Slash/Whoosh.wav",
    "victory": "Audio/Jingles/Success1.wav",
    "defeat": "Audio/Jingles/GameOver.wav",
    "levelup": "Audio/Jingles/LevelUp1.wav",
    "newword": "Audio/Sounds/Bonus/PowerUp1.wav",
    "coin": "Audio/Sounds/Bonus/Coin.wav",
    "fountain": "Audio/Sounds/Magic & Skill/Heal2.wav",
    "evolvelight": "Audio/Sounds/Magic & Skill/Spirit.wav",
    "evolve": "Audio/Jingles/Secret1.wav",
    "dexcomplete": "Audio/Jingles/Success3.wav",
    "door": "Audio/Sounds/Whoosh & Slash/Whoosh2.wav",
}
AUDIO_OUT = os.path.join(PROJECT, "Assets", "Resources", "Audio")


def copy_audio(pack):
    for folder, table in (("Music", MUSIC), ("Sfx", SFX)):
        os.makedirs(os.path.join(AUDIO_OUT, folder), exist_ok=True)
        for name, rel in table.items():
            ext = os.path.splitext(rel)[1]
            shutil.copyfile(os.path.join(pack, rel), os.path.join(AUDIO_OUT, folder, name + ext))
    print("audio", len(MUSIC), "music +", len(SFX), "sfx ->", AUDIO_OUT)

TILESETS = "Backgrounds/Tilesets/"


def crop_front(src, size, dst):
    sheet = Image.open(src).convert("RGBA")
    sheet.crop((0, 0, size, size)).save(dst)


# ------------------------------------------------------------------ 필드 타일
# 맵 글자 한 칸 = 16×16 한 장. 바닥 위에 물건을 겹쳐 그린 결과를 Tiles/{테마}_{종류}[_done].png 로 저장
# (_done: 연 보물상자, 쓰러뜨린 보스 자리). 보스는 그림이 커서 3배(48px) 타일로 만든다

class Pack:
    def __init__(self, root):
        self.root = root
        self.cache = {}

    def image(self, rel):
        if rel not in self.cache:
            self.cache[rel] = Image.open(os.path.join(self.root, rel)).convert("RGBA")
        return self.cache[rel]

    def tile(self, sheet, col, row):
        return self.image(TILESETS + sheet).crop((col * 16, row * 16, col * 16 + 16, row * 16 + 16))

    def frame(self, rel, index, width, height=None):
        img = self.image(rel)
        height = height or img.height
        return img.crop((index * width, 0, index * width + width, height))


def over(base, top, scale=1):
    """base(16×16) 위에 top을 가운데·아래 맞춰 겹친다. scale>1이면 바닥을 키워서(도트 유지) 큰 그림을 올린다"""
    out = base.resize((16 * scale, 16 * scale), Image.NEAREST) if scale > 1 else base.copy()
    x = (out.width - top.width) // 2
    y = max(0, out.height - top.height - (1 if top.height < out.height else 0))
    out.alpha_composite(top, (x, y) if top.height < out.height else (x, (out.height - top.height) // 2))
    return out


def ink(tile):
    """물 타일을 어두운 잉크색으로 다시 칠한다 (서고의 잉크 웅덩이)"""
    dark, light = (26, 20, 52), (110, 86, 150)
    out = tile.copy()
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            t = (r + g + b) / (3 * 255)
            px[x, y] = tuple(int(dark[i] + (light[i] - dark[i]) * t) for i in range(3)) + (a,)
    return out


def build_tiles(pack):
    boss = pack.frame("Actor/Boss/GiantSpirit/Idle.png", 0, 50).crop((1, 1, 49, 49))
    book = pack.image("Items/Object/Book.png")
    big_chest = [pack.frame("Items/Treasure/BigTreasureChest.png", i, 16) for i in range(2)]
    small_chest = [pack.frame("Items/Treasure/LittleTreasureChest.png", i, 16) for i in range(2)]
    gem = pack.tile("TilesetNature.png", 2, 15)
    stall = pack.tile("TilesetElement.png", 14, 0)

    themes = {}
    # 초원: 모래 길, 진한 풀숲(긴 풀잎), 덤불 벽, 물결 물, 굴 입구, 작은 연못(회복의 샘)
    sand = pack.tile("TilesetFloor.png", 1, 1)
    lime = pack.tile("TilesetField.png", 1, 4)
    themes["Meadow"] = {
        "Floor": sand,
        "Grass": over(pack.tile("TilesetField.png", 1, 7), pack.tile("TilesetNature.png", 7, 10)),
        "Wall": over(lime, pack.tile("TilesetNature.png", 1, 10)),
        "Water": pack.tile("TilesetWater.png", 11, 2),
        "Door": over(lime, pack.tile("TilesetNature.png", 7, 13)),
        "Fountain": pack.tile("TilesetWater.png", 3, 3),
        "Chest": over(sand, big_chest[0]),
        "Chest_done": over(sand, big_chest[1]),
        "Altar": over(sand, gem),
        "Shop": over(sand, stall),
        "Boss": over(sand, boss, scale=3),
        "Boss_done": over(sand, book),
    }
    # 서고: 어두운 돌바닥(책장과 잘 구분되게), 책장 벽, 잉크 웅덩이, 나무 문,
    #       받침대 위 파란 구슬(회복 지점), 작은 트렁크. 풀숲 = 바닥에 흩어진 하얀 종이 조각
    stone = pack.tile("Interior/TilesetInteriorFloor.png", 16, 13)
    themes["Library"] = {
        "Floor": stone,
        "Grass": over(stone, pack.tile("TilesetFloorDetail.png", 1, 3)),  # 흩어진 종이 조각
        "Wall": over(stone, pack.tile("TilesetElement.png", 3, 8)),
        "Water": ink(pack.tile("TilesetWater.png", 11, 2)),
        "Door": over(stone, pack.tile("TilesetElement.png", 8, 13)),
        "Fountain": over(stone, pack.tile("TilesetDungeon.png", 4, 2)),
        "Chest": over(stone, small_chest[0]),
        "Chest_done": over(stone, small_chest[1]),
        "Altar": over(stone, gem),
        "Shop": over(stone, stall),
        "Boss": over(stone, boss, scale=3),
        "Boss_done": over(stone, book),
    }

    folder = os.path.join(OUT, "Tiles")
    os.makedirs(folder, exist_ok=True)
    for theme, tiles in themes.items():
        for kind, img in tiles.items():
            img.save(os.path.join(folder, f"{theme}_{kind}.png"))
    print("tiles", sum(len(t) for t in themes.values()), "->", folder)


def main():
    pack = sys.argv[1] if len(sys.argv) > 1 else DEFAULT_PACK
    if not os.path.isdir(pack):
        sys.exit(f"팩 폴더가 없습니다: {pack}")

    os.makedirs(os.path.join(OUT, "Monsters"), exist_ok=True)
    os.makedirs(os.path.join(OUT, "Player"), exist_ok=True)

    for species_id, (rel, size) in MONSTERS.items():
        crop_front(os.path.join(pack, rel), size, os.path.join(OUT, "Monsters", species_id + ".png"))
        print("monster", species_id, "<-", rel)

    shutil.copyfile(os.path.join(pack, PLAYER), os.path.join(OUT, "Player", "Boy.png"))
    print("player <-", PLAYER)

    build_tiles(Pack(pack))
    copy_audio(pack)

    for name in ("LICENSE.txt", "README.md"):
        shutil.copyfile(os.path.join(pack, name), os.path.join(OUT, name))
    print("done ->", OUT)


if __name__ == "__main__":
    main()
