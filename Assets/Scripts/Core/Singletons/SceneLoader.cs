using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Singletons
{
    public class SceneLoader : IndestructibleSingletonBehaviour<SceneLoader>
    {
        public void LoadScene(string sceneName)
        {
            StartCoroutine(LoadSceneAsync(sceneName));
        }

        public IEnumerator LoadSceneAsync(string sceneName)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
        }
    }
}
