using TMPro;  // Required for TextMeshPro elements
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
//import for networking
using UnityEngine.Networking;

// whisper
using OpenAI;
using UnityEngine.UI;
using TMPro;

public class LevelScript : MonoBehaviour
{

    public static string allUserSentences1 = "";
    //string fileloc = "./Assets/keys.txt";
    public class RootObject
    {
        public string id;
        public List<Classification> classifications;
        public Meta meta;
    }

    [System.Serializable]
    public class Classification
    {
        public string classification_type;
        public double confidence;
        public List<double> confidences;
        public string id;
        public string input;
        public Dictionary<string, LabelConfidence> labels;
        public string prediction;
        public List<string> predictions;
    }

    [System.Serializable]
    public class LabelConfidence
    {
        public double confidence;
    }

    [System.Serializable]
    public class Meta
    {
        public ApiVersion api_version;
        public BilledUnits billed_units;
    }

    [System.Serializable]
    public class ApiVersion
    {
        public string version;
    }

    [System.Serializable]
    public class BilledUnits
    {
        public int input_tokens;
        public int output_tokens;
    }

    public class ApiResponse
    {
        public string response_id;
        public string text;
        public string generation_id;
        public Classification classification;
        public TokenCount token_count;
        public Meta meta;
        // tool_inputs is null, so it's not included here
    }

    [System.Serializable]
    public class TokenCount
    {
        public int prompt_tokens;
        public int response_tokens;
        public int total_tokens;
        public int billed_tokens;
    }


    private string cohereURLClassify = "https://api.cohere.ai/v1/classify";
    private string cohereURL = "https://api.cohere.ai/v1/chat";

    private string cohereAPI = "";
    public TextMeshPro estimatedLevel;  // Reference to the TextMeshProUGUI component
    public TextMeshPro frenchTips;  


    public class FrenchSentence
    {
        public string text { get; set; }
        public string label { get; set; }

        public FrenchSentence(string content, string classificationLabel)
        {
            text = content;
            label = classificationLabel;
        }
    }
    private List<FrenchSentence> frenchClassificationSentences = new List<FrenchSentence>();

    void Start()
    {
        string[] lines = { };
        // Load the text asset from the Resources folder
        TextAsset textAsset = Resources.Load<TextAsset>("keys");

        // // Check if the text asset is not null before reading lines
        if (textAsset != null)
        {
            // Read lines from the text asset
            lines = textAsset.text.Split('\n');
        }
        else
        {
            // Handle the case where the text asset is not found
            Debug.LogError("TextAsset 'keys' not found in Resources folder.");
        }
        cohereAPI = lines[1];
        // Example: Change the text at the start
        estimatedLevel.fontSize = 16;
        frenchTips.fontSize = 12;
        StartCoroutine(evaluateUser());
        StartCoroutine(generateReport());
    }

    public void ChangeText(TextMeshPro textfield, string newText)
    {
        // Change the text of the TextMeshProUGUI component
        textfield.text = newText;
    }

    public IEnumerator evaluateUser()
    {
        frenchClassificationSentences.Add(new FrenchSentence("Bonjour, comment ça va ?", "A1"));
        frenchClassificationSentences.Add(new FrenchSentence("Je m'appelle Marie.", "A1"));
        frenchClassificationSentences.Add(new FrenchSentence("J'aime les chiens.", "A1"));
        frenchClassificationSentences.Add(new FrenchSentence("Où est la bibliothèque ?", "A1"));
        frenchClassificationSentences.Add(new FrenchSentence("Je voudrais une pomme.", "A1"));
        frenchClassificationSentences.Add(new FrenchSentence("Je vais au marché tous les samedis.", "A2"));
        frenchClassificationSentences.Add(new FrenchSentence("Pouvez-vous parler plus lentement, s'il vous plaît ?", "A2"));
        frenchClassificationSentences.Add(new FrenchSentence("Elle aime lire des romans.", "A2"));
        frenchClassificationSentences.Add(new FrenchSentence("Nous avons deux chats et un chien.", "A2"));
        frenchClassificationSentences.Add(new FrenchSentence("J'étudie le français depuis un an.", "A2"));
        frenchClassificationSentences.Add(new FrenchSentence("Je commence à comprendre le français parlé.", "B1"));
        frenchClassificationSentences.Add(new FrenchSentence("Le film était intéressant, mais un peu long.", "B1"));
        frenchClassificationSentences.Add(new FrenchSentence("Pourriez-vous me conseiller un bon restaurant ?", "B1"));
        frenchClassificationSentences.Add(new FrenchSentence("Elle travaille en tant qu'ingénieure.", "B1"));
        frenchClassificationSentences.Add(new FrenchSentence("J'aimerais voyager en France l'année prochaine.", "B1"));
        frenchClassificationSentences.Add(new FrenchSentence("La politique économique actuelle suscite beaucoup de débats.", "B2"));
        frenchClassificationSentences.Add(new FrenchSentence("Je préfère les romans qui abordent des thèmes complexes.", "B2"));
        frenchClassificationSentences.Add(new FrenchSentence("Il a exprimé ses idées avec beaucoup d'éloquence.", "B2"));
        frenchClassificationSentences.Add(new FrenchSentence("Cette expérience a changé ma perspective sur la vie.", "B2"));
        frenchClassificationSentences.Add(new FrenchSentence("Nous avons discuté des différences culturelles et de leurs impacts.", "B2"));
        frenchClassificationSentences.Add(new FrenchSentence("L'étude de la littérature française m'a permis de mieux comprendre la langue.", "C1"));
        frenchClassificationSentences.Add(new FrenchSentence("Ses arguments étaient soutenus par des recherches approfondies.", "C1"));
        frenchClassificationSentences.Add(new FrenchSentence("La manière dont elle articule ses pensées est vraiment impressionnante.", "C1"));
        frenchClassificationSentences.Add(new FrenchSentence("Ce problème nécessite une analyse multidimensionnelle pour être résolu.", "C1"));
        frenchClassificationSentences.Add(new FrenchSentence("Le débat sur l'éthique en intelligence artificielle est complexe et nuancé.", "C1"));
        frenchClassificationSentences.Add(new FrenchSentence("La littérature contemporaine reflète souvent les nuances socio-politiques de son époque.", "C2"));
        frenchClassificationSentences.Add(new FrenchSentence("L'interprétation de ces données requiert une compréhension approfondie des théories statistiques.", "C2"));
        frenchClassificationSentences.Add(new FrenchSentence("Son analyse critique des œuvres littéraires démontre une profonde connaissance du sujet.", "C2"));
        frenchClassificationSentences.Add(new FrenchSentence("La capacité à discerner les subtilités dans les dialogues politiques est essentielle pour comprendre les enjeux actuels.", "C2"));
        frenchClassificationSentences.Add(new FrenchSentence("Il est impératif d'aborder cette question avec une perspective globale, en tenant compte des implications historiques et culturelles.", "C2"));

        Debug.Log(allUserSentences1);

        var data = new
        {
            inputs = new string[]
            {
                allUserSentences1
            },
            examples = frenchClassificationSentences
        };

        string jsonString = JsonConvert.SerializeObject(data, Formatting.Indented);
        Debug.Log(jsonString);

        UnityWebRequest request = UnityWebRequest.Put(cohereURLClassify, jsonString);
        request.method = "POST";
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + cohereAPI);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            UnityEngine.Debug.Log("issue encountered");
            UnityEngine.Debug.Log("Error: " + request.error);
        }
        else
        {
            UnityEngine.Debug.Log(request.downloadHandler.text);
            RootObject response = JsonUtility.FromJson<RootObject>(request.downloadHandler.text);
            Debug.Log(response.classifications[0].prediction);
            ChangeText(estimatedLevel, response.classifications[0].prediction);
            // generatedText = response.text;
        }

    }
    public IEnumerator generateReport()
    {

        var data = new
        {
            preamble_override = "You are a smart French grammar assistant who suggests general ideas for how to improve in French from a series of sentences. If you cannot find grammar errors, suggest new vocaulary words.",
            message = allUserSentences1
        };

        string jsonString = JsonConvert.SerializeObject(data, Formatting.Indented);

        UnityWebRequest request = UnityWebRequest.Put(cohereURL, jsonString);
        request.method = "POST";
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + cohereAPI);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            UnityEngine.Debug.Log("issue encountered");
            UnityEngine.Debug.Log("Error: " + request.error);
        }
        else
        {
            UnityEngine.Debug.Log(request.downloadHandler.text);
            ApiResponse response = JsonUtility.FromJson<ApiResponse>(request.downloadHandler.text);

            Debug.Log("Text: " + response.text);
            ChangeText(frenchTips, response.text);
        }

    }
}