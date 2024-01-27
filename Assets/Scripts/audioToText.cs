using System.Collections;
using System.Collections.Generic;
using Meta.WitAi.Json;
using System.Xml;
using UnityEngine;
using UnityEngine.Networking;

public class audioToText : MonoBehaviour
{



    private string apiUrl = "https://api.elevenlabs.io/v1/text-to-speech/9vOrFHPHFEbvq5anYV2T";

    private string secret = "177e0d0f2f125cf5ced2792546e3790f";


    AudioSource audioSource;



    //public IEnumerator CallElevenAPI(string text)
    //{

    //    // Create a dictionary for voice settings
    //    var voiceSettings = new Dictionary<string, double>
    //    {
    //        { "similarity_boost", 0.5 },
    //        { "stability", 0.5 }
    //    };

    //    // Create an anonymous object to represent the data structure
    //    var data = new
    //    {
    //        model_id = "eleven_multilingual_v2",
    //        text = "Bonjour comment ça va",
    //        voice_settings = voiceSettings
    //    };

    //    // Serialize the anonymous object to JSON string
    //    string jsonString = JsonConvert.SerializeObject(data, Formatting.Indented);

    //    DownloadHandlerAudioClip downloadHandler = new DownloadHandlerAudioClip(string.Empty, AudioType.MPEG);
    //    downloadHandler.streamAudio = true;

    //    UnityWebRequest request = UnityWebRequest.Put(apiUrl, jsonString);
    //    request.method = "POST";
    //    request.SetRequestHeader("Content-Type", "application/json");
    //    request.SetRequestHeader("xi-api-key", secret);
    //    request.downloadHandler = downloadHandler;

    //    Debug.Log("eleven calling api");

    //    yield return request.SendWebRequest();

    //    if (request.result != UnityWebRequest.Result.Success)
    //    {
    //        Debug.Log(request.error);
    //        Debug.Log("problem");
    //    }
    //    else
    //    {
    //        Debug.Log("Form upload complete!");
    //        AudioClip audio = DownloadHandlerAudioClip.GetContent(request);
    //        audioSource.clip = audio;
    //        audioSource.Play();
    //    }
    //}



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
