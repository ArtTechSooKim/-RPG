"""Ninja Adventure 에셋 팩(CC0)에서 이 게임이 쓰는 그림만 골라 프로젝트로 복사한다.

    python Tools/import_ninja_art.py ["팩 폴더 경로"]

- 몬스터: 시트에서 정면(첫 칸) 한 프레임만 잘라 Monsters/{speciesId}.png (파일 이름 = speciesId)
  → Unity 메뉴 WordRPG > Data > Link Monster Art 가 같은 이름의 몬스터 에셋에 연결한다
- 주인공: 4방향 × 걷기 4프레임 시트 그대로 (게임이 잘라 씀)
- 성유물: Relics/{relicId}.png (파일 이름 = relicId) — 게임이 RelicData.icon이 비어 있으면 이 그림을 쓴다
- 상처약 등 아이템 도트: Items/{itemId}.png (Figma로 그린 UI/Icons/Items/{itemId}.png가 없을 때 쓰임)
필요한 Pillow: pip install pillow
"""
import os
import shutil
import sys

from PIL import Image

DEFAULT_PACK = r"F:\Unity\김수연습\Ninja Adventure - Asset Pack\Ninja Adventure - Asset Pack"
PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(PROJECT, "Assets", "Resources", "Art", "NinjaAdventure")

# speciesId: (팩 안의 시트 경로, 한 칸 크기) — 적 몬스터만 (아군 몬스터는 성유물로 바뀜)
MONSTERS = {
    "ink_slime": ("Actor/Monsters/Slime/Slime.png", 16),                 # 잉크 슬라임
    "scribble_bat": ("Actor/Monsters/BlueBat/SpriteSheet.png", 16),      # 낙서 박쥐
    "forget_goblin": ("Actor/Monsters/KappaGreen/SpriteSheet.png", 16),  # 까먹깨비
    "boss_forget_king": ("Actor/Boss/GiantSpirit/Idle.png", 50),         # 까먹대왕
    # 숲 (두 번째 지역)
    "spell_mushroom": ("Actor/Monsters/Mushroom/mushroom.png", 16),      # 철자버섯
    "squiggle_snake": ("Actor/Monsters/Snake2/Snake2.png", 16),          # 꼬부랑뱀
    "question_owl": ("Actor/Monsters/Owl/Owl.png", 16),                  # 물음표부엉이
    "boss_muddle_raccoon": ("Actor/Boss/GiantRacoon/Idle.png", 60),      # 헷갈너구리
}

PLAYER = "Actor/Characters/Boy/SpriteSheet.png"

# relicId: (그림 경로, 시트에서 자를 한 칸 크기 — None이면 그대로)
RELICS = {
    "relic_quill": ("Items/Resource/feather.png", None),                  # 깃펜 (예전 펜촉이)
    "relic_book": ("Items/Object/Book.png", None),                        # 백과사전 (예전 책껍질)
    "relic_lantern": ("Actor/Monsters/LanternRed/SpriteSheet.png", 16),   # 등불 (예전 등불이)
    "relic_wand": ("Items/Weapons/MagicWand/Sprite.png", None),           # 마법 지팡이
    "relic_grail": ("Items/Treasure/GoldCup.png", None),                  # 기억의 성배 (서고 보스 보상)
    "relic_whip": ("Items/Weapons/Whip/Sprite.png", None),                # 덩굴 채찍 (숲 상자)
    "relic_hourglass": ("Items/Object/Hourglass.png", None),              # 시간의 모래시계 (숲 상자)
    "relic_leaf": ("Items/Food/TeaLeaf.png", None),                       # 세계수 잎 (숲 보스 보상)
}

# itemId: 그림 경로 (도트 아이템)
ITEMS = {
    "potion": "Items/Potion/Medipack.png",       # 상처약
    "potion_large": "Items/Potion/LifePot.png",  # 큰 상처약
    "keepsake_forest": "Items/Food/Nut.png",     # 도토리 책갈피 (숲 도감 완성 징표)
}

# 소리: 파일 이름 = 게임 코드의 이름(Music·Sfx 열거형을 소문자로)
MUSIC = {
    "title": "Audio/Musics/1 - Adventure Begin.ogg",
    "meadow": "Audio/Musics/5 - Peaceful.ogg",
    "library": "Audio/Musics/13 - Mystical.ogg",
    "battle": "Audio/Musics/17 - Fight.ogg",
    "boss": "Audio/Musics/28 - Tension.ogg",
    "forest": "Audio/Musics/11 - Clearing.ogg",
}
SFX = {
    "click": "Audio/Sounds/Menu/Move2.wav",  # 가벼운 찰칵 (0.04초)
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
    "newword": "Audio/Jingles/Secret3.wav",  # 새 단어 '발견!' 징글 (나오는 동안 배경 음악을 잠깐 멈춤)
    "coin": "Audio/Sounds/Bonus/Coin.wav",
    "fountain": "Audio/Sounds/Magic & Skill/Heal2.wav",
    "evolvelight": "Audio/Sounds/Magic & Skill/Spirit.wav",
    "evolve": "Audio/Jingles/Secret1.wav",
    "dexcomplete": "Audio/Jingles/Success3.wav",
    "door": "Audio/Sounds/Whoosh & Slash/Whoosh2.wav",
    "gateopen": "Audio/Jingles/Secret2.wav",  # 보스를 물리쳐 새 길이 열릴 때
    "combo": "Audio/Sounds/Bonus/PowerUp1.wav",  # 연속 정답 콤보 (단계마다 음높이를 올려 재생)
}
AUDIO_OUT = os.path.join(PROJECT, "Assets", "Resources", "Audio")

# 전투 효과: (시트, 프레임 수). 프레임을 정사각형으로 맞춰 가로로 다시 붙인다 → 게임은 높이 = 한 칸 크기로 자름
FX = {
    "slash": ("FX/SlashFx/Slash/SpriteSheet.png", 4),          # 아군 공격
    "claw": ("FX/SlashFx/Claw/SpriteSheet.png", 4),            # 적 공격
    "explosion": ("FX/Elemental/Explosion/SpriteSheet.png", 9),  # 크리티컬
    "heal": ("FX/Magic/Circle/SpriteSheetSpark.png", 6),       # 회복
    "shield": ("FX/Magic/Shield/SpriteSheetBlue.png", 6),      # 보호막 · 막음
    "smoke": ("FX/Smoke/Smoke/SpriteSheet.png", 6),            # 쓰러짐
}


def build_fx(pack):
    folder = os.path.join(OUT, "Fx")
    os.makedirs(folder, exist_ok=True)
    for name, (rel, count) in FX.items():
        sheet = pack.image(rel)
        fw, fh = sheet.width // count, sheet.height
        size = max(fw, fh)
        strip = Image.new("RGBA", (size * count, size), (0, 0, 0, 0))
        for i in range(count):
            frame = sheet.crop((i * fw, 0, i * fw + fw, fh))
            strip.alpha_composite(frame, (i * size + (size - fw) // 2, (size - fh) // 2))
        strip.save(os.path.join(folder, name + ".png"))
    print("fx", len(FX), "->", folder)


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
# (_done: 연 보물상자, 쓰러뜨린 보스 자리). 보스는 그림이 커서 3·4배(48·64px) 타일로 만든다
# 출입구: Door_locked = 보스를 물리쳐야 열리는 막힌 길, Door_{도착 테마} = 그 지역으로 가는 출입구만 다른 그림

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


def tint(tile, mul):
    """색을 채널별로 곱해 어둡게·물들인다 (숲의 짙은 나무·늪)"""
    out = tile.copy()
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            px[x, y] = (min(255, int(r * mul[0])), min(255, int(g * mul[1])), min(255, int(b * mul[2])), a)
    return out


def shade_top(tile, rows, mul):
    """윗부분 몇 줄을 어둡게(mul<1: 나무 그늘이 드리운 숲길 입구) 또는 밝게(mul>1: 햇빛 드는 출구)"""
    out = tile.copy()
    px = out.load()
    for y in range(rows):
        k = mul + (1 - mul) * y / rows
        for x in range(out.width):
            r, g, b, a = px[x, y]
            px[x, y] = (min(255, int(r * k)), min(255, int(g * k)), min(255, int(b * k)), a)
    return out


def copy_relics_and_items(pack):
    for folder in ("Relics", "Items"):
        os.makedirs(os.path.join(OUT, folder), exist_ok=True)
    for relic_id, (rel, size) in RELICS.items():
        dst = os.path.join(OUT, "Relics", relic_id + ".png")
        if size:
            crop_front(os.path.join(pack, rel), size, dst)
        else:
            shutil.copyfile(os.path.join(pack, rel), dst)
    for item_id, rel in ITEMS.items():
        shutil.copyfile(os.path.join(pack, rel), os.path.join(OUT, "Items", item_id + ".png"))
    print("relics", len(RELICS), "+ items", len(ITEMS), "->", OUT)


def build_tiles(pack):
    boss = pack.frame("Actor/Boss/GiantSpirit/Idle.png", 0, 50).crop((1, 1, 49, 49))
    raccoon = pack.frame("Actor/Boss/GiantRacoon/Idle.png", 0, 60)
    raccoon = raccoon.crop(raccoon.getbbox())
    brambles = pack.tile("TilesetNature.png", 4, 9)  # 엉킨 마른 덤불 = 막힌 길
    book = pack.image("Items/Object/Book.png")
    big_chest = [pack.frame("Items/Treasure/BigTreasureChest.png", i, 16) for i in range(2)]
    small_chest = [pack.frame("Items/Treasure/LittleTreasureChest.png", i, 16) for i in range(2)]
    gem = pack.tile("TilesetNature.png", 2, 15)
    stall = pack.tile("TilesetElement.png", 14, 0)

    themes = {}
    # 초원: 모래 길, 짧은 잔디, 진한 풀숲(긴 풀잎), 덤불 벽, 물결 물, 굴 입구, 작은 연못(회복의 샘)
    sand = pack.tile("TilesetFloor.png", 1, 1)
    lime = pack.tile("TilesetField.png", 1, 4)
    themes["Meadow"] = {
        "Floor": sand,
        "Lawn": lime,  # 짧은 잔디 (조우 없음)
        "Grass": over(pack.tile("TilesetField.png", 1, 7), pack.tile("TilesetNature.png", 7, 10)),
        "Wall": over(lime, pack.tile("TilesetNature.png", 1, 10)),
        "Water": pack.tile("TilesetWater.png", 11, 2),
        "Door": over(lime, pack.tile("TilesetNature.png", 7, 13)),
        "Door_locked": over(lime, brambles),
        "Door_Forest": shade_top(pack.tile("TilesetFloor.png", 12, 8), 9, 0.45),  # 그늘진 숲길 입구
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
        "Lawn": pack.tile("Interior/TilesetInteriorFloor.png", 12, 7),  # 열람실 초록 카펫 (조우 없음)
        "Grass": over(stone, pack.tile("TilesetFloorDetail.png", 1, 3)),  # 흩어진 종이 조각
        "Wall": over(stone, pack.tile("TilesetElement.png", 3, 8)),
        "Water": ink(pack.tile("TilesetWater.png", 11, 2)),
        "Door": over(stone, pack.tile("TilesetElement.png", 8, 13)),
        "Door_locked": over(stone, pack.tile("TilesetElement.png", 1, 11)),  # 쇠창살
        "Fountain": over(stone, pack.tile("TilesetDungeon.png", 4, 2)),
        "Chest": over(stone, small_chest[0]),
        "Chest_done": over(stone, small_chest[1]),
        "Altar": over(stone, gem),
        "Shop": over(stone, stall),
        "Boss": over(stone, boss, scale=3),
        "Boss_done": over(stone, book),
    }

    # 숲: 흙길, 진한 풀 위 고사리(조우), 짙은 덤불 벽, 초록빛 늪, 햇빛 드는 출구(초원으로)
    dirt = pack.tile("TilesetFloor.png", 12, 8)
    moss = pack.tile("TilesetField.png", 1, 7)
    deep = tint(moss, (0.62, 0.72, 0.62))
    themes["Forest"] = {
        "Floor": dirt,
        "Lawn": moss,  # 이끼 낀 땅 (조우 없음)
        "Grass": over(moss, pack.tile("TilesetNature.png", 4, 11)),
        "Wall": over(deep, tint(pack.tile("TilesetNature.png", 10, 9), (0.55, 0.75, 0.55))),
        "Water": tint(pack.tile("TilesetWater.png", 11, 2), (0.55, 0.85, 0.7)),
        "Door": shade_top(sand, 16, 1.25),  # 밝은 모래길 = 초원으로 나가는 길
        "Door_locked": over(moss, brambles),
        "Fountain": pack.tile("TilesetWater.png", 3, 3),
        "Chest": over(dirt, big_chest[0]),
        "Chest_done": over(dirt, big_chest[1]),
        "Altar": over(dirt, gem),
        "Shop": over(dirt, stall),
        "Boss": over(dirt, raccoon, scale=4),
        "Boss_done": over(dirt, book),
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

    copy_relics_and_items(pack)

    art = Pack(pack)
    build_tiles(art)
    build_fx(art)
    copy_audio(pack)

    for name in ("LICENSE.txt", "README.md"):
        shutil.copyfile(os.path.join(pack, name), os.path.join(OUT, name))
    print("done ->", OUT)


if __name__ == "__main__":
    main()
