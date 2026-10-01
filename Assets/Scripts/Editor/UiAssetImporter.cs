using UnityEditor;
using UnityEngine;

namespace WordRPG.EditorTools
{
    // 그림 가져오기 설정
    //  - Assets/Resources/UI/Icons: Figma에서 내보낸 아이콘 → 부드러운 UI 스프라이트
    //  - Assets/Resources/Art: 도트 그림(Ninja Adventure 팩) → 16픽셀 = 1칸, 확대해도 또렷하게(Point)
    internal class UiAssetImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            bool icon = assetPath.StartsWith("Assets/Resources/UI/Icons/");
            bool pixelArt = assetPath.StartsWith("Assets/Resources/Art/");
            if (!icon && !pixelArt) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            if (icon)
            {
                importer.spritePixelsPerUnit = 100;
                return;
            }
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
        }

        // 소리: 음악은 스트리밍(메모리 절약), 효과음은 미리 풀어 두고 모노로 (바로 재생)
        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/Resources/Audio/")) return;
            var importer = (AudioImporter)assetImporter;
            bool music = assetPath.StartsWith("Assets/Resources/Audio/Music/");
            var settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
            settings.quality = 0.6f;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = !music;
        }
    }
}
