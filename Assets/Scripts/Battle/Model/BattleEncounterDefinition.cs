using System;
using UnityEngine;

namespace Alif.Battle
{
    [Serializable]
    public class BattleResponse
    {
        [TextArea(2, 4)] public string Text;
        public bool Correct;
        [TextArea(2, 5)] public string Explanation;
    }

    [Serializable]
    public class BattleStage
    {
        public string Skill;
        [TextArea(2, 5)] public string Offer;
        public BattleResponse[] Responses;
    }

    [CreateAssetMenu(menuName = "Alif/Battle Encounter")]
    public class BattleEncounterDefinition : ScriptableObject
    {
        public string EnemyName = "Monster Dana Kilat";
        public Sprite AlifSprite;
        public Sprite EnemySprite;
        public int PlayerMaxHP = 100;
        public int EnemyMaxHP = 100;
        public int Damage = 34;
        public string ReferenceTitle = "Fatwa MUI No. 1 Tahun 2004 tentang Bunga";
        public string ReferenceUrl = "https://mui.or.id/baca/fatwa/hukum-bunga-interestfaidah";
        public BattleStage[] Stages;

        public string ValidationError(bool requireVisuals = true)
        {
            if (requireVisuals && (AlifSprite == null || EnemySprite == null)) return "Sprite Alif dan monster wajib diisi.";
            if (string.IsNullOrWhiteSpace(EnemyName)) return "Nama monster wajib diisi.";
            if (PlayerMaxHP <= 0 || EnemyMaxHP <= 0 || Damage <= 0) return "HP dan damage harus positif.";
            if (string.IsNullOrWhiteSpace(ReferenceTitle) || !Uri.TryCreate(ReferenceUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https")
                return "Judul dan tautan HTTPS rujukan wajib diisi.";
            if (Stages == null || Stages.Length == 0) return "Encounter membutuhkan tahap.";
            if ((long)Damage * Stages.Length < EnemyMaxHP || (long)Damage * (Stages.Length - 1) >= EnemyMaxHP)
                return "HP monster harus habis tepat pada tahap terakhir.";
            foreach (var stage in Stages)
            {
                if (stage == null || string.IsNullOrWhiteSpace(stage.Skill) || string.IsNullOrWhiteSpace(stage.Offer)) return "Skill dan tawaran wajib diisi.";
                if (stage.Responses == null || stage.Responses.Length != 3) return "Setiap tahap membutuhkan tiga respons.";
                int correct = 0;
                foreach (var response in stage.Responses)
                {
                    if (response == null || string.IsNullOrWhiteSpace(response.Text) || string.IsNullOrWhiteSpace(response.Explanation)) return "Respons dan penjelasan wajib diisi.";
                    if (response.Correct) correct++;
                }
                if (correct != 1) return "Setiap tahap membutuhkan tepat satu jawaban benar.";
            }
            return null;
        }
    }
}
