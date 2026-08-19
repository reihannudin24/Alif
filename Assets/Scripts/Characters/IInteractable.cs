namespace Alif.Characters
{
    /// <summary>
    /// Kontrak umum untuk apa pun yang bisa di-interact pemain (tombol Interact/E) —
    /// NPC (NPCController) maupun benda statis di dunia (InteractableObject, misal bangku).
    /// PlayerController cukup cari komponen ini di collider terdekat, tanpa perlu tahu
    /// itu NPC atau bukan.
    /// </summary>
    public interface IInteractable
    {
        void Interact();
    }
}
