using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace WordRPG.EditorTools
{
    // 모바일(안드로이드) 출시 준비: 플레이어 설정 한 번에 맞추기 + 폰 테스트용 APK / 스토어용 AAB 빌드.
    // Unity Hub에서 Android Build Support(OpenJDK, SDK & NDK 포함)를 설치해야 빌드할 수 있다.
    // 배치모드: -executeMethod WordRPG.EditorTools.MobileBuild.BuildAndroidApk (또는 BuildAndroidAab)
    public static class MobileBuild
    {
        // 패키지 이름은 스토어에 한 번 올리면 바꿀 수 없고, 바꾸면 폰의 세이브 위치도 바뀐다 — 출시 전에 사용자 확인.
        // 회사 이름(companyName)은 건드리지 않는다: PC(에디터) 세이브·설정 위치가 회사·앱 이름으로 정해져서 바꾸면 지금 세이브가 안 보임
        public const string ProductName = "영단어RPG";
        public const string PackageName = "com.arttechsoo.wordrpg";
        public const string IconPath = "Assets/Branding/AppIcon.png";
        public const string OutputFolder = "Builds/Android";

        [MenuItem("WordRPG/Build/Apply Mobile Settings")]
        public static void ApplyMobileSettings()
        {
            PlayerSettings.productName = ProductName; // 폰 홈 화면에 보이는 앱 이름
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);

            // 세로 고정
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // 구글 플레이는 64비트(ARM64) 필수 → IL2CPP. 안드로이드 7.1(API 25) 이상, 대상 SDK는 설치된 최신
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            else Debug.LogWarning($"[WordRPG] 앱 아이콘이 없음: {IconPath}");

            AssetDatabase.SaveAssets();
            Debug.Log($"[WordRPG] 모바일 설정 적용: {ProductName} ({PackageName}), 세로 고정, IL2CPP ARM64, 최소 API 25");
        }

        [MenuItem("WordRPG/Build/Android APK (폰 테스트용)")]
        public static void BuildAndroidApk() => BuildAndroid(false);

        [MenuItem("WordRPG/Build/Android AAB (스토어 올리기용)")]
        public static void BuildAndroidAab() => BuildAndroid(true);

        private static void BuildAndroid(bool appBundle)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Fail("Android Build Support 모듈이 없어요. Unity Hub > 설치 > 6000.3.11f1 > 모듈 추가에서 " +
                     "Android Build Support(OpenJDK, Android SDK & NDK Tools 포함)를 설치한 뒤 다시 해 주세요.");
                return;
            }

            ApplyMobileSettings();
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0 || !scenes[0].EndsWith("Title.unity"))
            {
                Fail("빌드 씬 순서가 맞지 않아요. WordRPG > Scenes > Create All Scenes를 먼저 실행해 주세요 (첫 씬 = 타이틀).");
                return;
            }

            EditorUserBuildSettings.buildAppBundle = appBundle;
            Directory.CreateDirectory(OutputFolder);
            string file = $"{OutputFolder}/WordRPG-{PlayerSettings.bundleVersion}{(appBundle ? ".aab" : ".apk")}";
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = file,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                Fail($"빌드 실패 ({report.summary.result}, 오류 {report.summary.totalErrors}개) — Console 창을 확인해 주세요.");
                return;
            }
            Debug.Log($"[WordRPG] 빌드 완료: {file} ({report.summary.totalSize / (1024f * 1024f):0.0} MB)");
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(file);
        }

        // 메뉴에서는 알림 창, 배치모드에서는 예외 (종료 코드 1)
        private static void Fail(string message)
        {
            Debug.LogError("[WordRPG] " + message);
            if (Application.isBatchMode) throw new BuildFailedException(message);
            EditorUtility.DisplayDialog("빌드할 수 없어요", message, "확인");
        }
    }
}
