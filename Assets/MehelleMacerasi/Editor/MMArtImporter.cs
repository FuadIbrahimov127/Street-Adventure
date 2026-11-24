using UnityEditor;
using UnityEngine;

// Məhəllə Macərası: idxal ayarları avtomatik verilir.
// Art: pixel art (PPU 16, Point filtr). Tiles və UI -> mərkəz pivot, qalanlar -> alt-mərkəz (Y-sıralama üçün).
// Adında "_9s" olan UI şəkillərinə 9-slice kənarları (8px). Şriftlər və musiqi də burada ayarlanır.
public class MMArtImporter : AssetPostprocessor
{
    const string Root = "Assets/MehelleMacerasi/";

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Root + "Art/")) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.spritePixelsPerUnit = 16;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.isReadable = false;

        var s = new TextureImporterSettings();
        ti.ReadTextureSettings(s);
        s.spriteMeshType = SpriteMeshType.FullRect;
        s.spriteExtrude = 0;
        bool center = assetPath.Contains("/Tiles/") || assetPath.Contains("/UI/");
        s.spriteAlignment = center ? (int)SpriteAlignment.Center : (int)SpriteAlignment.BottomCenter;
        if (assetPath.Contains("_9s")) s.spriteBorder = new Vector4(8, 8, 8, 8);
        ti.SetTextureSettings(s);
    }

    void OnPreprocessAsset()
    {
        // piksel şrift: kəskin göstərmək üçün
        if (assetPath.StartsWith(Root + "Fonts/") && assetImporter is TrueTypeFontImporter fi)
        {
            fi.fontRenderingMode = FontRenderingMode.HintedRaster;
            fi.characterPadding = 1;
        }
    }

    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith(Root + "Audio/")) return;
        var ai = (AudioImporter)assetImporter;
        var st = ai.defaultSampleSettings;
        st.loadType = AudioClipLoadType.Streaming;
        st.compressionFormat = AudioCompressionFormat.Vorbis;
        st.quality = 0.6f;
        ai.defaultSampleSettings = st;
        ai.loadInBackground = true;
    }
}
