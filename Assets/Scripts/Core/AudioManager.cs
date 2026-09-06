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

        [Header("Common SFX Clips")]
        [SerializeField] private AudioClip _footstepSfx;
        [SerializeField] private AudioClip _dialogueBlipSfx;
        [SerializeField] private AudioClip _interactSfx;
        [SerializeField] private AudioClip _coinSfx;
        [SerializeField] private AudioClip _bumpSfx;
        [SerializeField] private AudioClip _catMeowSfx;

        public AudioClip FootstepSfx => _footstepSfx;
        public AudioClip DialogueBlipSfx => _dialogueBlipSfx;
        public AudioClip InteractSfx => _interactSfx;
        public AudioClip CoinSfx => _coinSfx;
        public AudioClip BumpSfx => _bumpSfx;
        public AudioClip CatMeowSfx => _catMeowSfx;

        private void Awake()
        {
            Instance = this;

            if (_musicSource == null)
            {
                _musicSource = gameObject.AddComponent<AudioSource>();
                _musicSource.playOnAwake = false;
            }

            if (_sfxSource == null)
            {
                _sfxSource = gameObject.AddComponent<AudioSource>();
                _sfxSource.playOnAwake = false;
            }
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
            PlaySfx(clip, 0f, 1f);
        }

        public void PlaySfx(AudioClip clip, float pitchVariance, float volume = 1f)
        {
            if (_sfxSource == null || clip == null)
            {
                return;
            }

            if (pitchVariance > 0.001f)
            {
                float originalPitch = _sfxSource.pitch;
                _sfxSource.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
                _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
                _sfxSource.pitch = originalPitch;
            }
            else
            {
                _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
            }
        }

        /// <summary>
        /// Putar SFX satu kali pada pitch eksplisit (bukan acak) — mis. nada pukulan yang
        /// meningkat mengikuti combo. Pitch dikembalikan setelahnya agar tidak bocor ke SFX lain.
        /// </summary>
        public void PlaySfxAtPitch(AudioClip clip, float pitch, float volume = 1f)
        {
            if (_sfxSource == null || clip == null)
            {
                return;
            }

            float originalPitch = _sfxSource.pitch;
            _sfxSource.pitch = Mathf.Clamp(pitch, 0.25f, 4f);
            _sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
            _sfxSource.pitch = originalPitch;
        }

        public void PlayFootstep(float pitchVariance = 0.08f, float volume = 0.55f)
        {
            PlaySfx(_footstepSfx, pitchVariance, volume);
        }

        public void PlayDialogueBlip(float pitchVariance = 0.12f, float volume = 0.45f)
        {
            PlaySfx(_dialogueBlipSfx, pitchVariance, volume);
        }

        public void PlayInteract()
        {
            PlaySfx(_interactSfx, 0.04f, 0.85f);
        }

        public void PlayCoin()
        {
            PlaySfx(_coinSfx, 0.03f, 0.9f);
        }

        public void PlayBump(float volume = 0.6f)
        {
            PlaySfx(_bumpSfx, 0.05f, volume);
        }

        public void PlayCatMeow()
        {
            PlaySfx(_catMeowSfx, 0.06f, 0.95f);
        }
    }
}
