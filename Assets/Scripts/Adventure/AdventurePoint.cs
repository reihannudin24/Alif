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
        public TMP_Text Label;
        public void Interact()
        {
            if (Game == null || !Game.CanExplore) return;
            if (Destination >= 0) Game.Travel(Destination);
            else if (Optional) Game.Discover();
            else Game.Interact(Target);
        }
    }
}
