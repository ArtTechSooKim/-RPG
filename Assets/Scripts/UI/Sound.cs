using System.Collections.Generic;
using UnityEngine;
using WordRPG.Game;

namespace WordRPG.UI
{
    public enum Music { None, Title, Meadow, Library, Battle, Boss }

    public enum Sfx
    {
        Click, Correct, Wrong, Hit, Critical, Heal, Shield, Fail, Faint, Encounter,
        Victory, Defeat, LevelUp, NewWord, Coin, Fountain, EvolveLight, Evolve, DexComplete, Door
    }

    // 배경 음악·효과음 (Ninja Adventure 팩 → Resources/Audio/Music·Sfx, 파일 이름 = 열거형 이름 소문자).
    // 음량은 설정(GameSettings)을 따른다. 처음 소리를 낼 때 씬이 바뀌어도 남는 'Sound' 오브젝트를 만든다
    public static class Sound
    {
        private const float MusicBase = 0.6f; // 음악은 효과음보다 조금 작게

        private static AudioSource music, effects;
        private static Music current = Music.None;
        private static GameSettings settingsOverride;
        private static readonly Dictionary<string, AudioClip> Clips = new Dictionary<string, AudioClip>();

        public static Music CurrentMusic => current;

        private static GameSettings Settings =>
            settingsOverride ?? (GameManager.Instance != null ? GameManager.Instance.Settings : null);

        private static float MusicVolume => (Settings?.MusicVolume ?? GameSettings.DefaultMusicVolume) * MusicBase;
        private static float SfxVolume => Settings?.SfxVolume ?? GameSettings.DefaultSfxVolume;

        // 설정 화면에서 음량을 바꾸면 바로 반영
        public static void ApplyVolumes(GameSettings settings)
        {
            if (settings != null) settingsOverride = settings;
            if (music != null) music.volume = MusicVolume;
        }

        // 같은 곡이면 이어서, 다른 곡이면 처음부터. None = 멈춤
        public static void PlayMusic(Music track)
        {
            if (!Application.isPlaying || track == current || !Ensure()) return;
            current = track;
            var clip = track == Music.None ? null : Load("Audio/Music/" + track.ToString().ToLowerInvariant());
            music.Stop();
            music.clip = clip;
            music.volume = MusicVolume;
            if (clip != null) music.Play();
        }

        public static void Play(Sfx effect)
        {
            if (!Application.isPlaying || !Ensure()) return;
            var clip = Load("Audio/Sfx/" + effect.ToString().ToLowerInvariant());
            if (clip != null) effects.PlayOneShot(clip, SfxVolume);
        }

        private static bool Ensure()
        {
            if (music != null && effects != null) return true;
            var go = new GameObject("Sound");
            Object.DontDestroyOnLoad(go);
            music = go.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            effects = go.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            current = Music.None;
            return true;
        }

        private static AudioClip Load(string path)
        {
            if (!Clips.TryGetValue(path, out var clip))
            {
                clip = Resources.Load<AudioClip>(path);
                Clips[path] = clip;
            }
            return clip;
        }
    }
}
