using UnityEngine;
namespace Alif.Characters
{
    public sealed class InteractionPriority : MonoBehaviour
    {
        [SerializeField] int _priority;
        public int Priority => _priority;
    }
}
