using UnityEditor;

namespace WordRPG.EditorTools
{
    // Assets/Resources/UI/Icons 아래 PNG(Figma에서 내보낸 아이콘)를 UI 스프라이트로 가져온다
    internal class UiAssetImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/UI/Icons/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spritePixelsPerUnit = 100;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
