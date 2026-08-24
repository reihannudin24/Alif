using UnityEngine;
using Alif.Characters;

namespace Alif.World
{
    /// <summary>
    /// Adapter untuk titik interaksi restoran. File ini sengaja bernama sama dengan class-nya
    /// agar Unity membuat MonoScript asset yang stabil; menaruh MonoBehaviour kedua di file
    /// RestaurantStoryTrigger membuat referensinya tertanam di scene dan mudah menjadi rusak.
    /// </summary>
    public class RestaurantStoryPoint : MonoBehaviour, IInteractable
    {
        public enum PointType
        {
            Cashier,
            Seat,
            Menu
        }

        [SerializeField] private RestaurantStoryTrigger _story;
        [SerializeField] private PointType _pointType;

        public void Interact()
        {
            if (_story == null)
            {
                _story = FindAnyObjectByType<RestaurantStoryTrigger>();
            }

            if (_story == null)
            {
                Debug.LogWarning("[Alif] RestaurantStoryTrigger tidak ditemukan.");
                return;
            }

            switch (_pointType)
            {
                case PointType.Cashier:
                    _story.RequestOrder();
                    break;
                case PointType.Menu:
                    _story.RequestMenu();
                    break;
                default:
                    _story.RequestSeat();
                    break;
            }
        }
    }
}
