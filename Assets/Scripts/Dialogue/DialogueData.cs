using System;
using System.Collections.Generic;
using UnityEngine;
using Alif.Characters;

namespace Alif.Dialogue
{
    /// <summary>
    /// Satu baris dialog: siapa yang bicara dan apa isi teksnya.
    /// </summary>
    [Serializable]
    public class DialogueLine
    {
        public string SpeakerName;

        [Tooltip("Opsional. Jika diisi, portrait pembicara pada baris ini akan menggantikan portrait default dialog.")]
        public CharacterData SpeakerData;

        [TextArea(2, 5)]
        public string Text;

        [Tooltip("Isi hanya jika baris ini punya pilihan (choices). Kosongkan jika dialog linear biasa.")]
        public List<DialogueChoice> Choices = new List<DialogueChoice>();

        [Tooltip("Lompatan sesudah baris linear ini. -1 berarti lanjut ke baris berikutnya seperti biasa. Berguna untuk menyatukan cabang dialog.")]
        public int NextLineIndex = -1;

        public bool HasChoices => Choices != null && Choices.Count > 0;
    }

    /// <summary>
    /// Satu pilihan jawaban pemain dalam dialog bercabang (branching).
    /// NextLineIndex menentukan baris mana yang dituju setelah pilihan ini dipilih,
    /// sehingga percabangan dialog bisa lompat ke index manapun dalam list Lines.
    /// </summary>
    [Serializable]
    public class DialogueChoice
    {
        public string ChoiceText;

        [Tooltip("Index baris (di list Lines) yang akan ditampilkan setelah pilihan ini dipilih. -1 berarti mengakhiri dialog.")]
        public int NextLineIndex = -1;

        [Tooltip("Opsional: poin affinity yang ditambahkan ke NPC saat pilihan ini dipilih, untuk dating sim.")]
        public int AffinityChange = 0;

        [Header("Transaction")]
        [Tooltip("Biaya tunai yang langsung dibayar saat pilihan dipilih. Pilihan ditolak jika uang tidak cukup.")]
        public int MoneyCost = 0;

        [Header("Dampak Neraca 100%")]
        [Tooltip("Menggeser neraca ke Logika Finansial. Dalam Shared Balance Mode, Kepatuhan Syariah bergerak berlawanan agar total tetap 100%.")]
        public float FinancialLogicChange = 0f;

        [Tooltip("Menggeser neraca ke Kepatuhan Syariah. Dalam Shared Balance Mode, Logika Finansial bergerak berlawanan agar total tetap 100%.")]
        public float ShariaComplianceChange = 0f;

        [Tooltip("Menarik neraca kembali ke titik tengah 50% Finansial / 50% Syariah. Dipakai untuk pilihan jalan tengah yang sehat.")]
        public float BalanceCorrection = 0f;

        [Tooltip("ID event opsional untuk logic quest, toko, atau cutscene. Contoh: warung.order.ayam_geprek.")]
        public string EventId;

        [TextArea(1, 3)]
        [Tooltip("Teks singkat yang ditampilkan UI setelah pilihan memengaruhi neraca.")]
        public string OutcomeText;
    }

    /// <summary>
    /// Struktur data dialog lengkap, dibuat sebagai ScriptableObject supaya tiap percakapan
    /// bisa dibuat sebagai asset terpisah dan diedit lewat Inspector.
    /// Klik kanan di Project window -> Create -> Alif -> Dialogue Data.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogueData", menuName = "Alif/Dialogue Data")]
    public class DialogueData : ScriptableObject
    {
        [Header("Dialogue Lines")]
        [Tooltip("Urutan baris dialog. Secara default dialog berjalan berurutan (index demi index), " +
                 "kecuali ada Choices yang mengarahkan ke index lain.")]
        public List<DialogueLine> Lines = new List<DialogueLine>();

        [Header("Event Setelah Dialog Selesai")]
        [Tooltip("Nama event opsional yang bisa dibaca sistem lain (misal quest system) setelah dialog ini selesai.")]
        public string OnCompleteEventId;
    }
}
