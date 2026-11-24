# Street Adventure — Məhəllə Macərası

Bakı məhəlləsində keçən, uşaqlar üçün 2D piksel-art macəra oyunu. Unity 6 ilə hazırlanıb. Oyun süni intellektin (Claude) köməyi ilə yazılıb, onu uşaqlara süni intellektin nələr edə bildiyini göstərmək üçün düzəltmişik.

Oyunda məhəlləni gəzirsən, qonşularla danışırsan, onların tapşırıqlarını yerinə yetirirsən. Yol boyu pul yığır, bazardan alış-veriş edir, tarla əkir, itə top atır, qışda qartopu oynayırsan.

## Oyunda nələr var

- **İki xəritə.** Əsas məhəllə, bir də sağ kənardan keçilən "Məktəb məhəlləsi" (məktəb, futbol meydançası, kitabxana, çörəkxana, böyük tarla).
- **Qonşular və tapşırıqlar.** Sara, Elçin, Rəşad, Babalar, Nigar, Kamal, Leyla, Tural, Zəhra, Gülnarə.
- **İki hekayə xətti.** Birinci fəsil, sonra ikinci fəsil "Məhəllə şənliyi" və bayram finalı.
- **İçəri məkanlar.** Sara xalanın evi, bazar, poçt, çayxana, öz evin (yataqda yatmaq olur), kitabxana.
- **Bağçılıq.** Tarlanı bellə ilə qaz, sula, pomidor/kök/qarpız ək, günlərlə böyüt, sat.
- **Gecə-gündüz, hava və fəsillər.** Yağış, gölməçələr, ildırım, çətir; payız yarpaqları; qış, qar, qartopu və qar adamı.
- **Heyvanlar.** Pişik, it, göyərçinlər, pişik balaları.
- **20 nailiyyət**, xəritə, gündəlik, çanta, fasilə menyusu, yaddaş (save) sistemi.

## İdarəetmə

| Düymə | Nə edir |
|---|---|
| W A S D / oxlar | Hərəkət |
| E / Boşluq / Enter | Danış, götür, qapıdan keç |
| F | Seçilmiş əşyanı işlət (ye, iç, at, sula, bellə, çətir aç...) |
| 1 – 7 | Çantada xana seç |
| I | Çanta |
| J / Tab | Gündəlik (tapşırıqlar) |
| M | Xəritə |
| G | Qartopu at (qışda) |
| Esc | Fasilə menyusu / bağla |

## Layihəni açmaq

1. Unity Hub ilə **Unity 6000.6.10f1** (və ya daha yeni Unity 6) quraşdır.
2. Bu repo-nu klonla: `git clone https://github.com/FuadIbrahimov127/Street-Adventure.git`
3. Unity Hub-da **Add → Add project from disk** ilə qovluğu seç və aç (ilk açılışda Unity `Library` qovluğunu yaradır, bir az çəkə bilər).
4. `Assets/Scenes/MehelleMacerasi.unity` səhnəsini aç və ▶ Play bas.

Windows üçün `.exe` düzəltmək: **File → Build Profiles → Windows → Build**.

## Qovluqlar

```
Assets/MehelleMacerasi/
  Scripts/   oyun kodu (MMGame, MMPlayer, MMNpc, MMStory, MMGarden, MMWeather, MMSeasons ...)
  Editor/    səhnəni koddan quran alətlər (MMBuilder, MMLevelData, MMArtImporter)
  Art/       bütün sprite-lar (personajlar, binalar, içəri məkanlar, əşyalar, UI)
  Tiles/     tilemap üçün tile-lar
  Audio/     musiqi və səslər
  Fonts/     Pixelify Sans şrifti (OFL lisenziyası)
  _Old/      köhnə versiyanın faylları
Tools/
  art/       piksel-art-ı yaradan Python skriptləri
  audio/     musiqini və hava səslərini yaradan Python skriptləri
```

Səhnəni sıfırdan yenidən qurmaq üçün Unity menyusunda: **Tools → Mehelle Macerasi → Build Scene**.

## Qeydlər

- Kodda adlar ingiliscədir, şərhlər (comment) Azərbaycan dilindədir, oyunçunun gördüyü mətnlər Azərbaycan dilindədir.
- Yaddaş faylı `%USERPROFILE%\AppData\LocalLow\DefaultCompany\My project\mehelle_save.json` ünvanında saxlanır.
- Şrift: [Pixelify Sans](https://fonts.google.com/specimen/Pixelify+Sans), SIL Open Font License 1.1.
