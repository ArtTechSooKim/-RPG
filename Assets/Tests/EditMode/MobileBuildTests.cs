using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using WordRPG.EditorTools;

namespace WordRPG.Tests
{
    // 앱스토어 출시 준비: 아이콘 규격, Info.plist 수출 규정(암호화 안 씀) 표시
    public class MobileBuildTests
    {
        private const string Plist =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<plist version=\"1.0\">\n<dict>\n" +
            "\t<key>CFBundleName</key>\n\t<string>영단어RPG</string>\n" +
            "\t<key>UIDeviceFamily</key>\n\t<array>\n\t\t<integer>1</integer>\n\t</array>\n" +
            "\t<key>Sub</key>\n\t<dict>\n\t\t<key>A</key>\n\t\t<true/>\n\t</dict>\n" +
            "</dict>\n</plist>\n";

        [Test]
        public void PlistGetsNoEncryptionFlagOnceAtRootLevel()
        {
            string once = MobileBuild.WithNoEncryption(Plist);
            StringAssert.Contains("\t<key>ITSAppUsesNonExemptEncryption</key>\n\t<false/>\n</dict>\n</plist>", once,
                "맨 바깥 dict 끝에 들어감 (안쪽 dict가 아니라)");
            Assert.AreEqual(once, MobileBuild.WithNoEncryption(once), "두 번 빌드해도 한 번만");
        }

        // 앱스토어는 1024px 아이콘에 투명 채널이 있으면 받지 않는다
        [Test]
        public void AppIconIsSquare1024WithoutAlpha()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(MobileBuild.IconPath);
            Assert.IsNotNull(icon, MobileBuild.IconPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(MobileBuild.IconPath);
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            Assert.AreEqual(1024, width);
            Assert.AreEqual(1024, height);
            Assert.IsFalse(importer.DoesSourceTextureHaveAlpha(), "투명 채널 없음");
        }
    }
}
