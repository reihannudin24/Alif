using UnityEngine;
using Alif.Characters;

namespace Alif.World
{
    /// <summary>Titik interaksi pada Dimas untuk melanjutkan atau mengulang ringkasan Chapter 2.</summary>
    public class Chapter2StoryPoint : MonoBehaviour, IInteractable
    {
        [SerializeField] private Chapter2StoryController _story;

        public void Interact()
        {
            if (_story != null)
            {
                _story.RequestConversation();
            }
        }
    }
}
