using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class scene2script : MonoBehaviour
{

    [SerializeField] private TMP_Text message;
    private readonly int duration = 15;
    private float time = 0;
    public CanvasGroup fadePanel;
    public float fadeDuration = 2f;
    //public GameObject obj1;
    //public GameObject obj2;
    //public GameObject obj3;

    // Start is called before the first frame update
    void Start()
    {
        //obj1.SetActive(false);
        //obj2.SetActive(false);
        //obj3.SetActive(false);
    }

    IEnumerator FadeOut(string sceneToLoad)
    {
        float fadeTime = 0;
        while (fadeTime < fadeDuration)
        {
            fadeTime += Time.deltaTime;
            fadePanel.alpha = fadeTime / fadeDuration;
            yield return null;
        }
        SceneManager.LoadScene(sceneToLoad);
    }

    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;
        if (time >= 15)
        {
            message.SetText("");
            //obj1.SetActive(true);
            //obj2.SetActive(true);
            //obj3.SetActive(true);
            SceneManager.LoadScene("MenuScene");
        }
        else if (time >= 10)
        {
            message.SetText("But in the end\nit's just a memory.");
        }
        else if (time >= 5)
        {
            message.SetText("You might want to stay in the moment forever.");
        }
        else
        {
            message.SetText("You might not remember everything.");
        }
    }
}
