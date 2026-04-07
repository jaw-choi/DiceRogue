using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiceRogue
{
    public class BootSceneController : MonoBehaviour
    {
        [SerializeField] private RunConfig runConfig;
        [SerializeField] private float bootDelay = 0.1f;

        private IEnumerator Start()
        {
            UIInputSystemHelper.EnsureEventSystem();
            var manager = GameRunManager.EnsureInstance(runConfig);

            yield return new WaitForSeconds(bootDelay);
            SceneManager.LoadScene(manager.Config.MainMenuSceneName);
        }
    }
}
