using UnityEngine;

namespace Alif.Core
{
    /// <summary>
    /// Pemutar BGM (loop) dan SFX (one-shot) sederhana. Satu instance per scene yang butuh
    /// (mis. Main Menu) — nggak DontDestroyOnLoad karena tiap scene bisa punya kebutuhan audio
    /// beda (menu vs gameplay).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _sfxSource;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (_musicSource == null || clip == null)
            {
                return;
            }

            _musicSource.clip = clip;
            _musicSource.loop = loop;
            _musicSource.Play();
        }

        public void PlaySfx(AudioClip clip)
        {
            if (_sfxSource == null || clip == null)
            {
                return;
            }

            _sfxSource.PlayOneShot(clip);
        }
    }
}
