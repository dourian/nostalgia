using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class scene1script : MonoBehaviour
{

    [SerializeField] private TMP_Text message;
    private readonly int duration = 15;
    private float time = 0;
    public CanvasGroup fadePanel;
    public float fadeDuration = 2f; 

    // Start is called before the first frame update
    void Start()
    {

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
            SceneManager.LoadScene("MenuScene");
        }
        else if (time >= 10)
        {
            message.SetText("Welcome to Paris, France,\nwhere you were born.");
        }
        else if (time >= 5)
        {
            message.SetText("Hello,\nWelcome to your life.");
        }
        else
        {
            message.SetText("Vitre\nBy: Brayden, Dorian, Jayee, Sarina");
        }
    }
}
