using System.IO;
using UnityEditor;
using UnityEngine;

namespace Alif.EditorTools
{
    /// <summary>
    /// Sintesis audio original langsung dari kode (bebas lisensi). Engine-nya parametrik
    /// ala sfxr/bfxr (github.com/increpare/bfxr): envelope pitch, envelope amplitudo
    /// (attack-hold-decay + punch), duty/harmonik, vibrato, dan komponen noise — bukan
    /// lagi sine murni. BGM berlapis (melodi music box + bass + arpeggio akor + topi
    /// perkusi lembut) dengan loop sample-exact dan headroom (normalisasi 0.8, di bawah
    /// 0 dBFS). Semua 12 WAV (BGM + 7 SFX UI/dunia + 4 SFX campaign action) digenerate
    /// ulang dari sini — file lama ditimpa di tempat yang sama.
    /// </summary>
    public static class AlifAudioSetup
    {
        private const int SampleRate = 44100;
        private const string AudioFolder = "Assets/Audio";

        [MenuItem("Alif/6) Generate Game Audio")]
        public static void GenerateAudio()
        {
            EnsureFolder();
            GenerateMainMenuBgm();
            GenerateButtonClickSfx();
            GenerateFootstepSfx();
            GenerateDialogueBlipSfx();
            GenerateInteractSfx();
            GenerateCoinSfx();
            GenerateBumpSfx();
            GenerateCatMeowSfx();
            GenerateCampaignSwingSfx();
            GenerateCampaignHitSfx();
            GenerateCampaignDodgeSfx();
            GenerateCampaignVictorySfx();
            AssetDatabase.Refresh();
            Debug.Log("[Alif] Game audio (BGM berlapis + 11 SFX) selesai digenerate di 'Assets/Audio/'.");
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(AudioFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Audio");
            }
        }

        // ============================================================
        // ENGINE PARAMETRIK (pola sfxr)
        // ============================================================
        private enum Waveform { Sine, Square, Triangle, Saw }

        private struct SynthParams
        {
            public float Duration;
            public Waveform Wave;
            public float StartFreq;      // pitch awal (Hz)
            public float EndFreq;        // pitch akhir — slide logaritmik menuju sini
            public float Attack;         // detik naik ke penuh
            public float Hold;           // detik di penuh sebelum decay
            public float Decay;          // konstanta eksponensial decay
            public float Punch;          // gain ekstra sesaat di awal (dipakai impact)
            public float VibratoRate;    // Hz
            public float VibratoDepth;   // Hz
            public float NoiseMix;       // 0..1 — porsi noise vs tone
            public float NoiseDecay;     // decay eksponensial khusus noise
            public float Harmonic;       // 0..1 — mix harmonic ke-2 pada tone
            public float Amp;
        }

        private static uint _noiseSeed;

        // LCG deterministik — build harus reproducible, jadi tidak pakai Random.
        private static float NextNoise()
        {
            _noiseSeed = _noiseSeed * 1664525u + 1013904223u;
            return (_noiseSeed / 2147483648f) - 1f; // -1..1
        }

        private static float Oscillator(Waveform wave, float phase, float harmonic)
        {
            float fundamental = wave switch
            {
                Waveform.Square => (phase % 1f) < 0.5f ? 1f : -1f,
                Waveform.Triangle => 1f - 4f * Mathf.Abs(Mathf.RoundToInt(phase) - phase),
                Waveform.Saw => 2f * (phase - Mathf.Floor(phase + 0.5f)),
                _ => Mathf.Sin(2f * Mathf.PI * phase),
            };
            float second = wave == Waveform.Sine
                ? Mathf.Sin(4f * Mathf.PI * phase)
                : fundamental > 0f ? 1f - phase % 1f : (phase % 1f) - 1f; // versi lembut utk gelombang keras
            return Mathf.Lerp(fundamental, second, harmonic * 0.5f);
        }

        private static void RenderSynth(float[] buffer, in SynthParams p, float startTime = 0f, float ampScale = 1f)
        {
            int startIdx = Mathf.RoundToInt(startTime * SampleRate);
            int count = Mathf.Min(Mathf.RoundToInt(p.Duration * SampleRate), buffer.Length - startIdx);
            if (count <= 0) return;

            float phase = 0f;
            float attackSamples = Mathf.Max(1f, p.Attack * SampleRate);
            float holdSamples = p.Hold * SampleRate;

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float progress = i / (float)count;

                // Envelope pitch: interpolasi eksponensial supaya slide terasa natural.
                float freq = p.EndFreq > 0f
                    ? Mathf.Pow(p.StartFreq / p.EndFreq, 1f - progress) * p.EndFreq
                    : p.StartFreq;
                if (p.VibratoDepth > 0f) freq += Mathf.Sin(2f * Mathf.PI * p.VibratoRate * t) * p.VibratoDepth;

                phase += freq / SampleRate;
                float tone = Oscillator(p.Wave, phase, p.Harmonic);

                // Envelope amplitudo: attack -> hold -> decay (+punch di awal decay).
                float envelope;
                if (i < attackSamples) envelope = i / attackSamples;
                else if (i < attackSamples + holdSamples) envelope = 1f;
                else envelope = Mathf.Exp(-p.Decay * (t - p.Attack - p.Hold));
                if (p.Punch > 0f) envelope *= 1f + p.Punch * Mathf.Exp(-progress * 24f);

                float noise = NextNoise() * Mathf.Exp(-progress * p.NoiseDecay);
                float sample = Mathf.Lerp(tone, noise, p.NoiseMix) * envelope * p.Amp * ampScale;
                buffer[startIdx + i] += sample;
            }
        }

        // Nada bell dengan partial inharmonik (rasio ~2.76 & 5.4 — ciri logam/lonceng,
        // tidak mungkin dicapai dari harmonic ke-2/3 sine murni).
        private static void RenderBell(float[] buffer, float startTime, float dur, float baseFreq, float amp)
        {
            RenderSynth(buffer, new SynthParams
            {
                Duration = dur, Wave = Waveform.Sine, StartFreq = baseFreq, EndFreq = baseFreq,
                Attack = 0.002f, Hold = 0f, Decay = 9f, Amp = amp,
            }, startTime);
            RenderSynth(buffer, new SynthParams
            {
                Duration = dur * 0.6f, Wave = Waveform.Sine, StartFreq = baseFreq * 2.76f, EndFreq = baseFreq * 2.76f,
                Attack = 0.001f, Hold = 0f, Decay = 16f, Amp = amp * 0.4f,
            }, startTime);
            RenderSynth(buffer, new SynthParams
            {
                Duration = dur * 0.35f, Wave = Waveform.Sine, StartFreq = baseFreq * 5.4f, EndFreq = baseFreq * 5.4f,
                Attack = 0.001f, Hold = 0f, Decay = 24f, Amp = amp * 0.18f,
            }, startTime);
        }

        // ============================================================
        // BGM MAIN MENU — music box + bass + arpeggio akor + topi perkusi.
        // Loop sample-exact: total panjang = 16 beat penuh, tanpa fade di ujung.
        // ============================================================
        private static void GenerateMainMenuBgm()
        {
            const float bpm = 112f;
            const float beatDuration = 60f / bpm;

            (float freq, float beats)[] melody =
            {
                (261.63f, 0.5f), (329.63f, 0.5f), (392.00f, 0.5f), (329.63f, 0.5f), (293.66f, 1f), (0f, 1f),
                (329.63f, 0.5f), (392.00f, 0.5f), (440.00f, 0.5f), (392.00f, 0.5f), (329.63f, 1f), (0f, 1f),
                (261.63f, 0.5f), (293.66f, 0.5f), (329.63f, 0.5f), (293.66f, 0.5f), (261.63f, 1f), (0f, 1f),
                (392.00f, 0.5f), (329.63f, 0.5f), (293.66f, 0.5f), (261.63f, 0.5f), (261.63f, 2f),
            };

            (float freq, float beats)[] bass =
            {
                (130.81f, 4f), // C3
                (110.00f, 4f), // A2
                (130.81f, 4f), // C3
                (98.00f,  4f), // G2
            };

            // Arpeggio lembut mengikuti akor tiap bar (C - Am - C - G), 1 nada per setengah beat.
            float[][] chords = { new[] { 261.63f, 329.63f, 392.00f }, new[] { 261.63f, 329.63f, 440.00f }, new[] { 261.63f, 329.63f, 392.00f }, new[] { 246.94f, 293.66f, 392.00f } };

            const float totalBeats = 16f;
            int totalSamples = Mathf.CeilToInt(totalBeats * beatDuration * SampleRate);
            var buffer = new float[totalSamples];

            // Layer 1: melodi music box (sine + harmonic tipis).
            RenderNotes(buffer, melody, beatDuration, amplitude: 0.42f, harmonicMix: 0.22f);
            // Layer 2: bass lembut.
            RenderNotes(buffer, bass, beatDuration, amplitude: 0.26f, harmonicMix: 0.06f);

            // Layer 3: arpeggio triangle pelan di belakang melodi.
            for (int bar = 0; bar < 4; bar++)
            {
                for (int step = 0; step < 8; step++)
                {
                    float note = chords[bar][step % 3];
                    float startBeat = bar * 4f + step * 0.5f;
                    RenderSynth(buffer, new SynthParams
                    {
                        Duration = 0.24f, Wave = Waveform.Triangle, StartFreq = note, EndFreq = note,
                        Attack = 0.01f, Hold = 0f, Decay = 7f, Amp = 0.1f,
                    }, startBeat * beatDuration);
                }
            }

            // Layer 4: topi perkusi lembut (noise pendek) di tiap beat, aksen di beat genap.
            _noiseSeed = 0x9e3779b9u;
            for (int beat = 0; beat < 16; beat++)
            {
                RenderSynth(buffer, new SynthParams
                {
                    Duration = 0.04f, Wave = Waveform.Sine, StartFreq = 6000f, EndFreq = 6000f,
                    Attack = 0.001f, Hold = 0f, Decay = 4f, NoiseMix = 1f, NoiseDecay = 10f,
                    Amp = beat % 2 == 0 ? 0.055f : 0.035f,
                }, beat * beatDuration);
            }

            Normalize(buffer, 0.8f); // headroom — jangan menempel 0 dBFS
            SaveWav($"{AudioFolder}/BGM_MainMenu.wav", buffer);
        }

        // ============================================================
        // SFX UI & DUNIA
        // ============================================================
        private static void GenerateButtonClickSfx()
        {
            var buffer = new float[Mathf.CeilToInt(0.09f * SampleRate)];
            RenderSynth(buffer, new SynthParams
            {
                Duration = 0.09f, Wave = Waveform.Square, StartFreq = 1150f, EndFreq = 620f,
                Attack = 0.001f, Hold = 0.01f, Decay = 34f, Punch = 0.35f, Harmonic = 0.15f, Amp = 0.55f,
            });
            Normalize(buffer, 0.8f);
            SaveWav($"{AudioFolder}/SFX_ButtonClick.wav", buffer);
        }

        private static void GenerateFootstepSfx()
        {
            var buffer = new float[Mathf.CeilToInt(0.075f * SampleRate)];
            _noiseSeed = 0x1234abcdu;
            // Body: thump rendah; tekstur: burst noise pendek (napas ubin/aspal).
            RenderSynth(buffer, new SynthParams
            {
                Duration = 0.075f, Wave = Waveform.Sine, StartFreq = 150f, EndFreq = 62f,
                Attack = 0.002f, Hold = 0f, Decay = 30f, Punch = 0.5f, Amp = 0.6f,
            });
            RenderSynth(buffer, new SynthParams
            {
                Duration = 0.045f, Wave = Waveform.Sine, StartFreq = 1000f, EndFreq = 1000f,
                Attack = 0.001f, Hold = 0f, Decay = 4f, NoiseMix = 1f, NoiseDecay = 18f, Amp = 0.3f,
            });
            SaveWav($"{AudioFolder}/SFX_Footstep.wav", buffer);
        }

        private static void GenerateDialogueBlipSfx()
        {
            var buffer = new float[Mathf.CeilToInt(0.045f * SampleRate)];
            RenderSynth(buffer, new SynthParams
            {
                Duration = 0.045f, Wave = Waveform.Triangle, StartFreq = 900f, EndFreq = 780f,
                Attack = 0.001f, Hold = 0.008f, Decay = 45f, Harmonic = 0.3f, Amp = 0.5f,
            });
            SaveWav($"{AudioFolder}/SFX_DialogueBlip.wav", buffer);
        }

        private static void GenerateInteractSfx()
        {
            var buffer = new float[Mathf.CeilToInt(0.24f * SampleRate)];
            RenderBell(buffer, 0f, 0.13f, 659.25f, 0.42f);   // E5
            RenderBell(buffer, 0.07f, 0.16f, 987.77f, 0.5f); // B5
            Normalize(buffer, 0.8f);
            SaveWav($"{AudioFolder}/SFX_Interact.wav", buffer);
        }

        private static void GenerateCoinSfx()
        {
            var buffer = new float[Mathf.CeilToInt(0.28f * SampleRate)];
            RenderBell(buffer, 0f, 0.12f, 987.77f, 0.4f);     // B5
            RenderBell(buffer, 0.05f, 0.22f, 1318.51f, 0.52f);// E6
            RenderBell(buffer, 0.05f, 0.12f, 2637.02f, 0.16f);// shimmer
            Normalize(buffer, 0.85f);
            SaveWav($"{AudioFolder}/SFX_Coin.wav", buffer);
        }

        private static void GenerateBumpSfx()
        {
            var buffer = new float[Mathf.CeilToInt(0.11f * SampleRate)];
            _noiseSeed = 0x55aa1234u;
            RenderSynth(buffer, new SynthParams
            {
                Duration = 0.11f, Wave = Waveform.Square, StartFreq = 170f, EndFreq = 58f,
                Attack = 0.001f, Hold = 0.004f, Decay = 26f, Punch = 0.6f, Harmonic = 0.2f, Amp = 0.5f,
            });
            RenderSynth(buffer, new SynthParams
            {
                Duration = 0.06f, Wave = Waveform.Sine, StartFreq = 800f, EndFreq = 800f,
                Attack = 0.001f, Hold = 0f, Decay = 3f, NoiseMix = 1f, NoiseDecay = 14f, Amp = 0.28f,
            });
            SaveWav($"{AudioFolder}/SFX_Bump.wav", buffer);
        }

        private static void GenerateCatMeowSfx()
        {
            var buffer = new float[Mathf.CeilToInt(0.38f * SampleRate)];
            // Formant dua-fase (naik lalu turun) + vibrato halus — kucing, bukan sirene.
            RenderSynth(buffer, new SynthParams
            {
                Duration = 0.17f, Wave = Waveform.Saw, StartFreq = 520f, EndFreq = 780f,
                Attack = 0.045f, Hold = 0.02f, Decay = 7f, VibratoRate = 5.5f, VibratoDepth = 14f,
                Harmonic = 0.35f, Amp = 0.4f,
            });
            RenderSynth(buffer, new SynthParams
            {
                Duration = 0.21f, Wave = Waveform.Saw, StartFreq = 780f, EndFreq = 450f,
                Attack = 0.02f, Hold = 0.02f, Decay = 6f, VibratoRate = 5.5f, VibratoDepth = 10f,
                Harmonic = 0.3f, Amp = 0.38f,
            }, 0.17f);
            Normalize(buffer, 0.85f);
            SaveWav($"{AudioFolder}/SFX_CatMeow.wav", buffer);
        }

        // ============================================================
        // SFX CAMPAIGN ACTION (dipakai ActionEncounterDefinition chapter 3-5)
        // ============================================================
        private static void GenerateCampaignSwingSfx()
        {
            // Whoosh: noise band yang menyapu turun — noise-dominan, tanpa tonal jelas.
            var buffer = new float[Mathf.CeilToInt(0.26f * SampleRate)];
            _noiseSeed = 0x77e1c001u;
            RenderSynth(buffer, new SynthParams
            {
                Duration = 0.26f, Wave = Waveform.Triangle, StartFreq = 900f, EndFreq = 180f,
                Attack = 0.03f, Hold = 0.04f, Decay = 12f, NoiseMix = 0.85f, NoiseDecay = 8f, Amp = 0.6f,
            });
            SaveWav($"{AudioFolder}/SFX_CampaignSwing.wav", buffer);
        }

        private static void GenerateCampaignHitSfx()
        {
            // Impact: punch square rendah + burst noise tajam.
            var buffer = new float[Mathf.CeilToInt(0.3f * SampleRate)];
            _noiseSeed = 0xdeadbeefu;
            RenderSynth(buffer, new SynthParams
            {
                Duration = 0.3f, Wave = Waveform.Square, StartFreq = 220f, EndFreq = 48f,
                Attack = 0.001f, Hold = 0.006f, Decay = 18f, Punch = 0.8f, Harmonic = 0.25f, Amp = 0.55f,
            });
            RenderSynth(buffer, new SynthParams
            {
                Duration = 0.09f, Wave = Waveform.Sine, StartFreq = 1500f, EndFreq = 1500f,
                Attack = 0.001f, Hold = 0f, Decay = 2.5f, NoiseMix = 1f, NoiseDecay = 16f, Amp = 0.4f,
            });
            SaveWav($"{AudioFolder}/SFX_CampaignHit.wav", buffer);
        }

        private static void GenerateCampaignDodgeSfx()
        {
            // Dodge: sapuan pendek naik-tipis, ringan dan "angin".
            var buffer = new float[Mathf.CeilToInt(0.22f * SampleRate)];
            _noiseSeed = 0x8badf00du;
            RenderSynth(buffer, new SynthParams
            {
                Duration = 0.22f, Wave = Waveform.Triangle, StartFreq = 300f, EndFreq = 1400f,
                Attack = 0.02f, Hold = 0.02f, Decay = 15f, NoiseMix = 0.7f, NoiseDecay = 9f, Amp = 0.45f,
            });
            SaveWav($"{AudioFolder}/SFX_CampaignDodge.wav", buffer);
        }

        private static void GenerateCampaignVictorySfx()
        {
            // Jingle kemenangan: arpeggio C-E-G-C naik + bell akhir.
            var buffer = new float[Mathf.CeilToInt(1.1f * SampleRate)];
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f }; // C5 E5 G5 C6
            for (int i = 0; i < notes.Length; i++)
            {
                RenderBell(buffer, i * 0.11f, 0.35f, notes[i], 0.42f);
            }
            RenderBell(buffer, 0.48f, 0.6f, 1318.51f, 0.4f); // E6 — resolusi
            Normalize(buffer, 0.85f);
            SaveWav($"{AudioFolder}/SFX_CampaignVictory.wav", buffer);
        }

        // ============================================================
        // HELPERS (BGM)
        // ============================================================
        private static void RenderNotes(float[] buffer, (float freq, float beats)[] notes, float beatDuration, float amplitude, float harmonicMix)
        {
            int sampleCursor = 0;
            foreach ((float freq, float beats) in notes)
            {
                int noteSamples = Mathf.RoundToInt(beats * beatDuration * SampleRate);
                if (freq > 0f)
                {
                    RenderSynth(buffer, new SynthParams
                    {
                        Duration = noteSamples / (float)SampleRate, Wave = Waveform.Sine,
                        StartFreq = freq, EndFreq = freq,
                        Attack = 0.01f, Hold = 0f, Decay = 3.2f, Harmonic = harmonicMix, Amp = amplitude,
                    }, sampleCursor / (float)SampleRate);
                }

                sampleCursor += noteSamples;
                if (sampleCursor >= buffer.Length)
                {
                    break;
                }
            }
        }

        private static void Normalize(float[] buffer, float targetPeak)
        {
            float peak = 0f;
            foreach (float sample in buffer)
            {
                peak = Mathf.Max(peak, Mathf.Abs(sample));
            }

            if (peak <= 0.0001f)
            {
                return;
            }

            float scale = targetPeak / peak;
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] *= scale;
            }
        }

        private static void SaveWav(string relativePath, float[] samples)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string fullPath = Path.Combine(projectRoot, relativePath);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                int byteRate = SampleRate * 2; // mono, 16-bit
                int dataSize = samples.Length * 2;

                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + dataSize);
                writer.Write(new[] { 'W', 'A', 'V', 'E' });
                writer.Write(new[] { 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1); // PCM
                writer.Write((short)1); // mono
                writer.Write(SampleRate);
                writer.Write(byteRate);
                writer.Write((short)2); // block align
                writer.Write((short)16); // bits per sample
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(dataSize);

                foreach (float sample in samples)
                {
                    short pcm = (short)Mathf.Clamp(sample * short.MaxValue, short.MinValue, short.MaxValue);
                    writer.Write(pcm);
                }
            }

            AssetDatabase.ImportAsset(relativePath);
        }
    }
}
