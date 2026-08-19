using UnityEngine;

namespace Alif.Characters
{
    /// <summary>
    /// Data reusable untuk satu karakter/NPC. Dibuat sebagai ScriptableObject supaya
    /// tiap NPC bisa dibuat sebagai asset terpisah dan diatur langsung lewat Inspector,
    /// tanpa perlu hardcode di script.
    /// Klik kanan di Project window -> Create -> Alif -> Character Data.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacterData", menuName = "Alif/Character Data")]
    public class CharacterData : ScriptableObject
    {
        [Header("Info Dasar")]
        public string CharacterName;
        [TextArea(3, 6)]
        public string Description;

        [Header("Visual")]
        public Sprite Portrait; // Ditampilkan di dialogue box
        public RuntimeAnimatorController AnimatorController; // Dipakai NPCController untuk animasi

        [Header("Relationship / Dating Sim")]
        [Tooltip("Level hubungan saat ini, misal 0 = Stranger, 1 = Acquaintance, 2 = Friend, dst.")]
        [SerializeField] private int _relationshipLevel = 0;

        [Tooltip("Poin affinity/kedekatan, dipakai untuk menentukan kapan naik ke relationship level berikutnya.")]
        [SerializeField] private int _affinityPoints = 0;

        [Tooltip("Jumlah affinity points yang dibutuhkan untuk naik satu relationship level.")]
        [SerializeField] private int _affinityPerLevel = 100;

        public int RelationshipLevel => _relationshipLevel;
        public int AffinityPoints => _affinityPoints;

        /// <summary>
        /// Tambah affinity points, misalnya setelah memberi hadiah atau memilih dialog yang disukai NPC.
        /// Otomatis naik level jika poin sudah cukup.
        /// </summary>
        public void AddAffinity(int amount)
        {
            _affinityPoints += amount;

            while (_affinityPoints >= _affinityPerLevel)
            {
                _affinityPoints -= _affinityPerLevel;
                _relationshipLevel++;
            }
        }
    }
}
