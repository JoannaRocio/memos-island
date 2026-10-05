using System.Collections;
using System.Collections.Generic;
using MemosIsland.Memos;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemosIsland.Core
{
    /// <summary>
    /// Música y efectos (Fase 9B, GDD §19). La música cambia sola según la escena (con fundido cruzado) y las escenas de
    /// historia pueden pisarla un rato (SetOverride). Los clips están en Resources/Audio (los genera Tools/Audio/make_audio.py).
    /// El volumen sale de las Opciones (GameSettings).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        const float MusicLevel = 0.55f; // la música, un poco por debajo de los efectos
        const int SfxVoices = 8;

        static readonly Dictionary<string, string> SceneMusic = new()
        {
            ["Title"] = "cajita",
            ["Map_PuebloPuerto"] = "pueblo", ["Map_Almacen"] = "pueblo", ["Map_Herreria"] = "pueblo",
            ["Map_Clinica"] = "pueblo", ["Map_Taberna"] = "pueblo",
            ["Map_RefugioExterior"] = "refugio", ["Map_RefugioInterior"] = "refugio",
            ["Map_Estadio"] = "copa",
        };

        AudioSource _musicA, _musicB;
        bool _aPlaying;
        string _current, _sceneTrack, _override;
        Coroutine _fade;
        readonly List<AudioSource> _sfx = new();
        int _nextVoice;
        readonly Dictionary<string, AudioClip> _clips = new();

        void Awake()
        {
            Instance = this;
            _musicA = NewSource("Music A", true);
            _musicB = NewSource("Music B", true);
            for (int i = 0; i < SfxVoices; i++) _sfx.Add(NewSource($"Sfx {i}", false));
            GameSettings.Changed += ApplyVolume;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            GameSettings.Changed -= ApplyVolume;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (Instance == this) Instance = null;
        }

        void Start() => OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

        AudioSource NewSource(string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.loop = loop;
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            return s;
        }

        AudioClip Clip(string path)
        {
            if (!_clips.TryGetValue(path, out var clip))
            {
                clip = Resources.Load<AudioClip>($"Audio/{path}");
                _clips[path] = clip;
            }
            return clip;
        }

        // ------------------------------------------------------------------ Música

        /// <summary>Qué tema suena en cada escena (lógica pura, con tests).</summary>
        public static string TrackFor(string scene, bool cupRace)
        {
            if (scene == "Race") return cupRace ? "copa" : "carrera";
            return SceneMusic.TryGetValue(scene, out var track) ? track : "ruta";
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            bool cup = Race.RaceLauncher.Pending != null && Race.RaceLauncher.Pending.format == Race.RaceFormat.Cup;
            _sceneTrack = TrackFor(scene.name, cup);
            if (_override == null) PlayMusic(_sceneTrack);
        }

        /// <summary>Una escena de historia pisa la música (por ejemplo, la cajita cuando se rompe un collar).</summary>
        public void SetOverride(string track)
        {
            _override = track;
            PlayMusic(track, 1.2f);
        }

        public void ClearOverride()
        {
            _override = null;
            if (_sceneTrack != null) PlayMusic(_sceneTrack, 1.2f);
        }

        public void PlayMusic(string track, float fade = 0.8f)
        {
            if (track == _current) return;
            _current = track;
            var clip = Clip($"Music/{track}");
            var from = _aPlaying ? _musicA : _musicB;
            var to = _aPlaying ? _musicB : _musicA;
            _aPlaying = !_aPlaying;
            to.clip = clip;
            to.volume = 0f;
            if (clip != null) to.Play();
            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(Crossfade(from, to, fade));
        }

        IEnumerator Crossfade(AudioSource from, AudioSource to, float seconds)
        {
            float start = from.volume;
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                float k = t / seconds;
                from.volume = start * (1f - k);
                to.volume = MusicVolume * k;
                yield return null;
            }
            from.Stop();
            to.volume = MusicVolume;
            _fade = null;
        }

        static float MusicVolume => GameSettings.MusicVolume * MusicLevel;

        void ApplyVolume()
        {
            if (_fade != null) return;
            (_aPlaying ? _musicA : _musicB).volume = MusicVolume;
        }

        // ------------------------------------------------------------------ Efectos

        /// <summary>Toca un efecto de Resources/Audio/Sfx. Seguro de llamar aunque no haya AudioManager.</summary>
        public static void Sfx(string name, float volume = 1f, float pitch = 1f)
        {
            var am = Instance;
            if (am == null || GameSettings.SfxVolume <= 0f) return;
            var clip = am.Clip($"Sfx/{name}");
            if (clip == null) return;
            var source = am._sfx[am._nextVoice];
            am._nextVoice = (am._nextVoice + 1) % am._sfx.Count;
            source.pitch = pitch;
            source.PlayOneShot(clip, volume * GameSettings.SfxVolume);
        }

        /// <summary>El grito corto de un Memo (uno distinto por especie).</summary>
        public static void Cry(MemoSpecies species, float pitch = 1f)
        {
            if (species != null) Sfx($"cry_{Mathf.Clamp(species.number, 1, 20):00}", 0.8f, pitch);
        }
    }
}
