using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OrientationManager : MonoBehaviour
{
    public Image loadingProgressUI;
    public float loadTime = 1;

    // Start is called before the first frame update
    void Start()
    {
        if (Camera.main.aspect > 1)
        {
            Debug.Log("LandscapeMode");
            //SceneManager.LoadSceneAsync(1);
            StartCoroutine(LoadScene(1));
        }
        else if (Camera.main.aspect < 1)
        {
            Debug.Log("PortraitMode");
            //SceneManager.LoadSceneAsync(2);
            StartCoroutine(LoadScene(2));
        }
        else if (Camera.main.aspect == 1)
        {
            Debug.Log("Square");
        }
    }

    IEnumerator LoadScene(int sceneIndex)
    {
        yield return null;

        //Begin to load the Scene you specify
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneIndex);

        //Don't let the Scene activate until you allow it to
        asyncOperation.allowSceneActivation = false;

        Debug.Log("Pro :" + asyncOperation.progress);

        float timer = 0;

        while (!asyncOperation.isDone)
        {
            //loadingProgressUI.fillAmount = asyncOperation.progress;
            // Check if the load has finished
            if (asyncOperation.progress >= 0.9f)
            {
                while (timer < loadTime)
                {
                    timer += Time.deltaTime;
                    yield return null;
                }
                asyncOperation.allowSceneActivation = true;
            }
            yield return null;
        }
    }
}

