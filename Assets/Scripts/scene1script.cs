using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class scene1script : MonoBehaviour
{

    [SerializeField] private TMP_Text message;
    private readonly int duration = 15;
    private float time = 0;

    // Start is called before the first frame update
    void Start()
    {
        
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
            message.SetText("we hope you enjoy");
        }
        else if (time >= 5)
        {
            message.SetText("this is a game about growing up");
        }
        else
        {
            message.SetText("title \n by Brayden, Dorian, Jayee, Sarina");
        }
    }
}
