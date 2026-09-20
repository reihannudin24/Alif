using Alif.Characters;
using TMPro;
using UnityEngine;

namespace Alif.Adventure
{
    public sealed class AdventurePoint : MonoBehaviour, IInteractable
    {
        public AdventureGame Game;
        public string Target;
        public int Destination = -1;
        public int Area;
        public bool Optional;
        /// <summary>Benda kota yang bisa diperiksa (QuestNpc.IsObject): Target tetap "npc:…" tapi bukan tokoh.</summary>
        public bool Object;
        public TMP_Text Label;
        public AdventureNpcReaction Reaction;

        /// <summary>True untuk NPC quest kota (Target "npc:…"), bukan titik pindah area atau
        /// benda seperti papan/loket — dipakai PlayerController untuk memunculkan balon chat.</summary>
        public bool IsNpc => Destination < 0 && !Optional && !Object && Target != null && Target.StartsWith("npc:", System.StringComparison.Ordinal);

        public void Interact()
        {
            if (Game == null || !Game.CanExplore) return;
            Reaction?.React();
            if (Destination >= 0) Game.Travel(Destination);
            else if (Optional) Game.Discover();
            else Game.Interact(Target);
        }
    }
}
