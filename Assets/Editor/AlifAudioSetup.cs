using System.IO;
using UnityEditor;
using UnityEngine;

namespace Alif.EditorTools
{
    /// <summary>
    /// Generate audio placeholder ORIGINAL — disintesis langsung dari kode (bukan reuse file
    /// audio dari project/sumber lain), jadi nggak ada masalah lisensi sama sekali.
    /// BGM: melodi pentatonik C major sederhana + bass lembut, loop ~8.6 detik, timbre mirip
    /// music box (sine + sedikit harmonic ke-2). SFX: "chirp" pendek turun nada buat klik tombol.
    /// </summary>
    public static class AlifAudioSetup
    {
        private const int SampleRate = 44100;
        private const string AudioFolder = "Assets/Audio";

        [MenuItem("Alif/6) Generate Placeholder Audio")]
        public static void GenerateAudio()
        {
            EnsureFolder();
            GenerateMainMenuBgm();
            GenerateButtonClickSfx();
            AssetDatabase.Refresh();
            Debug.Log("[Alif] Audio placeholder (BGM + SFX) selesai digenerate di 'Assets/Audio/'.");
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(AudioFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Audio");
            }
        }

        // ------------------------------------------------------------
        // BGM Main Menu — melodi pentatonik C major + bass, loop ~8.6 detik.
        // ------------------------------------------------------------
        private static void GenerateMainMenuBgm()
        {
            const float bpm = 112f;
            const float beatDuration = 60f / bpm;

            // (frekuensi Hz, durasi dalam beat). freq 0 = rest (diam).
            (float freq, float beats)[] melody =
            {
                (261.63f, 0.5f), (329.63f, 0.5f), (392.00f, 0.5f), (329.63f, 0.5f), (293.66f, 1f), (0f, 1f),
                (329.63f, 0.5f), (392.00f, 0.5f), (440.00f, 0.5f), (392.00f, 0.5f), (329.63f, 1f), (0f, 1f),
                (261.63f, 0.5f), (293.66f, 0.5f), (329.63f, 0.5f), (293.66f, 0.5f), (261.63f, 1f), (0f, 1f),
                (392.00f, 0.5f), (329.63f, 0.5f), (293.66f, 0.5f), (261.63f, 0.5f), (261.63f, 2f),
            };

            (float freq, float beats)[] bass =
            {
                (130.81f, 4f), // C3, bar 1
                (110.00f, 4f), // A2, bar 2
                (130.81f, 4f), // C3, bar 3
                (98.00f, 4f),  // G2, bar 4
            };

            const float totalBeats = 16f;
            int totalSamples = Mathf.CeilToInt(totalBeats * beatDuration * SampleRate);
            var buffer = new float[totalSamples];

            RenderNotes(buffer, melody, beatDuration, amplitude: 0.5f, harmonicMix: 0.25f);
            RenderNotes(buffer, bass, beatDuration, amplitude: 0.3f, harmonicMix: 0.1f);

            Normalize(buffer, 0.85f);
            SaveWav($"{AudioFolder}/BGM_MainMenu.wav", buffer);
        }

        // ------------------------------------------------------------
        // SFX klik tombol — "chirp" pendek turun nada.
        // ------------------------------------------------------------
        private static void GenerateButtonClickSfx()
        {
            const float duration = 0.09f;
            int totalSamples = Mathf.CeilToInt(duration * SampleRate);
            var buffer = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = i / (float)SampleRate;
                float progress = t / duration;
                float freq = Mathf.Lerp(1200f, 700f, progress);
                float envelope = Mathf.Exp(-progress * 8f);
                buffer[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.6f;
            }

            SaveWav($"{AudioFolder}/SFX_ButtonClick.wav", buffer);
        }

        // ------------------------------------------------------------
        // HELPERS
        // ------------------------------------------------------------
        private static void RenderNotes(float[] buffer, (float freq, float beats)[] notes, float beatDuration, float amplitude, float harmonicMix)
        {
            int sampleCursor = 0;
            foreach ((float freq, float beats) in notes)
            {
                int noteSamples = Mathf.RoundToInt(beats * beatDuration * SampleRate);
                if (freq > 0f)
                {
                    RenderNote(buffer, sampleCursor, noteSamples, freq, amplitude, harmonicMix);
                }

                sampleCursor += noteSamples;
                if (sampleCursor >= buffer.Length)
                {
                    break;
                }
            }
        }

        // Attack pendek + decay eksponensial lembut, biar kedengeran kayak marimba/music box
        // (bukan tone datar yang kaku) dan nggak ada "klik" di awal/akhir tiap not.
        private static void RenderNote(float[] buffer, int startSample, int noteSamples, float freq, float amplitude, float harmonicMix)
        {
            const float attack = 0.01f;
            int attackSamples = Mathf.RoundToInt(attack * SampleRate);

            for (int i = 0; i < noteSamples; i++)
            {
                int idx = startSample + i;
                if (idx >= buffer.Length)
                {
                    break;
                }

                float t = i / (float)SampleRate;
                float envelope = i < attackSamples
                    ? i / (float)attackSamples
                    : Mathf.Exp(-(t - attack) * 3.2f);

                float fundamental = Mathf.Sin(2f * Mathf.PI * freq * t);
                float harmonic = Mathf.Sin(2f * Mathf.PI * freq * 2f * t);
                float sample = Mathf.Lerp(fundamental, harmonic, harmonicMix) * envelope * amplitude;

                buffer[idx] += sample;
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
