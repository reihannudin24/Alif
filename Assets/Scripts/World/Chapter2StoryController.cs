using UnityEngine;
using UnityEngine.SceneManagement;
namespace Alif.World
{
    public class Chapter2StoryController : MonoBehaviour
    {
        void Start() => RequestConversation();
        public void RequestConversation() => SceneManager.LoadScene(Alif.Adventure.AdventureGame.SceneFor(2));
    }
}
