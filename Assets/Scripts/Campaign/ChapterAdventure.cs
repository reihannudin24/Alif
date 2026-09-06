using UnityEngine;
using UnityEngine.SceneManagement;
namespace Alif.Campaign
{
    public sealed class ChapterAdventure : MonoBehaviour
    {
        public int Chapter = 3;
        void Start() => Inspect(0);
        public void Inspect(int index) => SceneManager.LoadScene(Alif.Adventure.AdventureGame.SceneFor(Mathf.Clamp(Chapter, 1, 5)));
    }
}
