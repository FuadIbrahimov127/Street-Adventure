# Bütün spritləri bir yerə yığır: ad -> (qovluq, [kadrlar]).
import tiles as TL
import buildings as BD
import nature as N
import props as R
import chars as C
import pal as P

ART = {}


def add(folder, name, frames):
    ART[name] = (folder, frames if isinstance(frames, list) else [frames])


# --- personajlar ---
for n in C.SPECS:
    for k, v in C.all_frames(n).items():
        add('Characters', k, v)
for i in range(2):
    add('Characters', f'cat_idle_{i}', R.cat(i, 'idle'))
    add('Characters', f'cat_walk_{i}', R.cat(i, 'walk'))
    add('Characters', f'dog_idle_{i}', R.dog(i, 'idle'))
    add('Characters', f'dog_walk_{i}', R.dog(i, 'walk'))
    add('Characters', f'butterfly_{i}', R.butterfly(i, P.WHITE))
for i in range(4):
    add('Characters', f'pigeon_{i}', R.pigeon(i))

# --- binalar ---
for name, fn in BD.ALL.items():
    b = fn()
    add('Buildings', name, b.img)
    add('Buildings', name + '_glow', b.glow)

# --- təbiət və əşyalar ---
add('Objects', 'tree_oak', N.tree_oak(1))
add('Objects', 'tree_oak2', N.tree_oak(7))
add('Objects', 'tree_pomegranate', N.tree_pomegranate())
add('Objects', 'tree_cypress', N.tree_cypress())
add('Objects', 'tree_blossom', N.tree_blossom())
add('Objects', 'bush', N.bush(5))
add('Objects', 'bush_flowers', N.bush(8, flowers=P.PINK))
add('Objects', 'bush_flowers2', N.bush(11, flowers=P.YELLOW))
add('Objects', 'flowerbed', N.flowerbed())
add('Objects', 'lamp', R.lamp(False))
add('Objects', 'lamp_on', R.lamp(True))
add('Objects', 'bench', R.bench())
add('Objects', 'bin', R.trash_bin())
for i in range(3):
    add('Objects', f'fountain_{i}', R.fountain(i))
add('Objects', 'car_red', R.car(P.RED))
add('Objects', 'car_blue', R.car(P.BLUE))
add('Objects', 'car_white', R.car(P.WHITE))
add('Objects', 'car_taxi', R.car(P.PURPLE, 'taxi'))
add('Objects', 'car_green', R.car(P.GREEN, 'van'))
add('Objects', 'car_yellow', R.car(P.YELLOW))
add('Objects', 'car_light', R.car_light())
for i in range(2):
    add('Objects', f'nerd_table_{i}', R.nerd_scene(i))
add('Objects', 'fruit_stand', R.fruit_stand())

# --- götürülən əşyalar və işarələr ---
for i in range(4):
    add('Items', f'coin_{i}', R.coin(i))
add('Items', 'letter', R.letter())
add('Items', 'tea', R.tea())
add('Items', 'shadow', R.shadow())
add('Items', 'prompt_e', R.prompt_e())
add('Items', 'marker_excl', R.marker('!'))
add('Items', 'marker_q', R.marker('?'))
add('Items', 'glow', R.glow(40))
add('Items', 'firefly', R.firefly())

# --- plitələr ---
for i in range(4):
    add('Tiles', f'grass_{i}', TL.grass(i, dense=(i % 2 == 0)))
for i in range(2):
    add('Tiles', f'grass_flowers_{i}', TL.grass(10 + i, flowers=3))
for i in range(3):
    add('Tiles', f'dirt_{i}', TL.dirt(i))
    add('Tiles', f'pavers_{i}', TL.pavers(i))
add('Tiles', 'road_top', TL.asphalt(1, 'top'))
add('Tiles', 'road_bottom', TL.asphalt(2, 'bottom'))
add('Tiles', 'cross_top', TL.crosswalk(3, 'top'))
add('Tiles', 'cross_bottom', TL.crosswalk(4, 'bottom'))
add('Tiles', 'hedge', TL.hedge(5))
add('Tiles', 'hedge_front', TL.hedge(6, front=True))
for i, s in enumerate('nesw'):
    add('Tiles', f'edge_grass_{s}', TL.grass_edge(s, 20 + i))
for c in ('nw', 'ne', 'sw', 'se'):
    add('Tiles', f'corner_grass_{c}', TL.grass_corner(c, 0))
add('Tiles', 'curb_n', TL.curb('n'))
add('Tiles', 'curb_s', TL.curb('s'))
add('Tiles', 'ao_shadow', TL.ao_shadow())

# --- UI ---
add('UI', 'heart_full', R.heart(True))
add('UI', 'heart_empty', R.heart(False))
add('UI', 'coin_icon', R.coin_icon())
add('UI', 'charm_icon', R.charm_icon())
add('UI', 'icecream_icon', R.icecream_icon())
add('UI', 'juice_icon', R.juice_icon())
add('UI', 'shoes_icon', R.shoes_icon())
add('UI', 'sun_icon', R.sun_icon())
add('UI', 'moon_icon', R.moon_icon())
add('UI', 'map_you', R.map_you())
add('UI', 'cat_icon', R.cat(0, 'idle'))
add('UI', 'letter_icon', R.letter())
add('UI', 'tea_icon', R.tea())
add('UI', 'panel_9s', R.nine_slice('panel'))
add('UI', 'dialog_9s', R.nine_slice('dialog'))
add('UI', 'slot_9s', R.nine_slice('slot'))
add('UI', 'slot_sel_9s', R.nine_slice('slot_sel'))


def flat():
    """ad -> şəkil (çox kadrlılar üçün ad_0, ad_1... artıq adda var)"""
    out = {}
    for name, (folder, frames) in ART.items():
        if len(frames) == 1:
            out[name] = (folder, frames[0])
        else:
            for i, f in enumerate(frames):
                out[f'{name}_{i}'] = (folder, f)
    return out

# --- çanta əşyaları (v3) ---
add('UI', 'ball_icon', R.ball_icon())
add('UI', 'bread_icon', R.bread_icon())
add('UI', 'can_icon', R.can_icon())
add('UI', 'camera_icon', R.camera_icon())
add('UI', 'flashlight_icon', R.flashlight_icon())
add('Items', 'ball', R.ball_world())
add('Items', 'crumbs', R.crumbs())
add('Items', 'water_drops', R.water_drops())
add('Objects', 'flowerbed_dry', R.flowerbed_dry())
V3_NAMES = ['ball_icon', 'bread_icon', 'can_icon', 'camera_icon', 'flashlight_icon', 'ball', 'crumbs', 'water_drops', 'flowerbed_dry', 'flowerbed']

# --- evlərin içi (v4) ---
import interiors as INT
for _n, _fn in INT.ALL.items():
    add('Interiors', _n, _fn())
V4_NAMES = list(INT.ALL.keys())

# --- yağış və nailiyyətlər (v6) ---
add('Items', 'raindrop', R.raindrop())
for _i in range(2):
    add('Items', f'rain_splash_{_i}', R.rain_splash(_i))
    add('Objects', f'puddle_{_i}', R.puddle(3 + _i * 5))
for _i in range(3):
    add('Items', f'ripple_{_i}', R.ripple(_i))
add('Objects', 'umbrella', R.umbrella_world())
add('UI', 'umbrella_icon', R.umbrella_icon())
add('UI', 'rain_icon', R.rain_icon())
add('UI', 'medal', R.medal(False))
add('UI', 'house_icon', R.house_icon())
add('UI', 'medal_locked', R.medal(True))
V6_NAMES = ['raindrop', 'rain_splash_0', 'rain_splash_1', 'puddle_0', 'puddle_1', 'ripple_0', 'ripple_1', 'ripple_2', 'umbrella', 'umbrella_icon', 'rain_icon', 'medal', 'medal_locked', 'house_icon']

# --- v7: yeni otaqlar, qonşular, bağça, fəsillər və qış ---
import v7 as V7
V7_NAMES = []


def add7(folder, name, im):
    add(folder, name, im)
    V7_NAMES.append(name)


for _n, _fn in V7.ROOMS.items():
    add7('Interiors', _n, _fn())
for _n, _spec in V7.CHAR_SPECS.items():
    C.SPECS[_n] = _spec
    for _k, _v in C.all_frames(_n).items():
        add7('Characters', _k, _v)
add7('Objects', 'soil', V7.soil(False))
add7('Objects', 'soil_wet', V7.soil(True))
for _kind in ('tomato', 'carrot', 'melon'):
    for _s in range(4):
        add7('Objects', f'{_kind}_{_s}', V7.crop(_kind, _s))
    add7('UI', f'{_kind}_icon', V7.crop_icon(_kind))
    add7('UI', f'seed_{_kind}', V7.seed_packet(_kind))
add7('Objects', 'sell_box', V7.sell_box())
add7('Objects', 'water_tap', V7.water_tap())
add7('Objects', 'seed_stand', V7.seed_stand())
for _n, _c in V7.KITTENS.items():
    add7('Characters', _n, V7.kitten(_c, 0))
# fəsil variantları: _s1 yay, _s2 payız, _s3 qış (yaz = əsas sprite)
SEASON_TREES = ['tree_oak', 'tree_oak2', 'tree_pomegranate', 'tree_blossom', 'tree_cypress', 'bush', 'bush_flowers', 'bush_flowers2']
for _i, _n in enumerate(SEASON_TREES):
    _base = ART[_n][1][0]
    if _n == 'tree_blossom':
        add7('Objects', _n + '_s1', V7.season_variant(_base, 1, _i))
    if _n != 'tree_cypress':
        add7('Objects', _n + '_s2', V7.season_variant(_base, 2, _i))
    add7('Objects', _n + '_s3', V7.season_variant(_base, 3, _i))
SEASON_SNOWCAP = [n for n in BD.ALL] + ['bench', 'fruit_stand']
for _n in SEASON_SNOWCAP:
    add7('Buildings' if _n in BD.ALL else 'Objects', _n + '_s3', V7.roof_snow(_n) if _n in BD.ALL else V7.snowcap(ART[_n][1][0], 2))
for _i in range(4):
    add7('Tiles', f'snow_{_i}', V7.snow_tile(50 + _i))
    add7('Tiles', f'leaves_{_i}', V7.leaves_tile(60 + _i))
    add7('UI', f'season_{_i}', V7.season_icon(_i))
    add7('Objects', f'snowman_{_i}', V7.snowman(_i))
add7('Items', 'snowflake', V7.snowflake())
add7('Items', 'snowball', V7.snowball())
add7('Items', 'snow_splat', V7.snow_splat())
add7('Objects', 'snow_pile', V7.snow_pile())
add7('UI', 'invite_icon', V7.invite_icon())
add7('Objects', 'bunting', V7.bunting())
for _i in range(5):
    add7('Items', f'confetti_{_i}', V7.confetti(_i))

# --- v8: Məktəb məhəlləsi ---
import v8 as V8
V8_NAMES = []


def add8(folder, name, im):
    add(folder, name, im)
    V8_NAMES.append(name)


for _n, _fn in V8.BUILDINGS.items():
    _b = _fn()
    add8('Buildings', _n, _b.img)
    add8('Buildings', _n + '_glow', _b.glow)
    add8('Buildings', _n + '_s3', V7.roof_snow(_n))
for _i in range(3):
    add8('Tiles', f'yard_{_i}', V8.yard_tile(70 + _i))
    add8('Tiles', f'field_{_i}', V8.field_tile(80 + _i))
add8('Objects', 'pitch', V8.pitch())
add8('Objects', 'goal', V8.goal())
for _i in range(2):
    add8('Objects', f'swing_{_i}', V8.swing(_i))
    add8('Objects', f'tandir_{_i}', V8.tandir(_i))
    add8('Items', f'dust_{_i}', V8.dust(_i))
add8('Objects', 'slide', V8.slide())
add8('Objects', 'hoop', V8.hoop())
add8('Objects', 'fence_h', V8.fence_h())
add8('Objects', 'fence_v', V8.fence_v())
add8('Items', 'book_red', V8.book(P.RED))
add8('Items', 'book_blue', V8.book(P.BLUE))
add8('Items', 'book_green', V8.book(P.GREEN))
add8('UI', 'book_icon', V8.book_icon())
add8('UI', 'spade_icon', V8.spade_icon())
add8('Items', 'tile_cursor', V8.tile_cursor())
for _n, _fn in V8.ROOMS.items():
    add8('Interiors', _n, _fn())
for _n, _spec in V8.CHAR_SPECS.items():
    C.SPECS[_n] = _spec
    for _k, _v in C.all_frames(_n).items():
        add8('Characters', _k, _v)
