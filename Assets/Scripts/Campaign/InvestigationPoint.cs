using Alif.Characters;
using UnityEngine;

namespace Alif.Campaign
{
    public sealed class InvestigationPoint : MonoBehaviour, IInteractable
    {
        public ChapterAdventure Story;
        public int Index;
        public void Interact() => Story.Inspect(Index);
    }
}
