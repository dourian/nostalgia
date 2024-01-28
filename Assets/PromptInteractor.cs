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
using static UnityEngine.Rendering.DebugUI;
using Button = UnityEngine.UI.Button;
using UnityEngine.SceneManagement;

namespace Samples.Whisper
{
    [System.Serializable]
    public class ReRankRoot
    {
        public string id;
        public List<Result> results;
        public Meta meta;
    }

    [System.Serializable]
    public class Result
    {
        public Document document;
        public int index;
        public double relevance_score;
    }

    [System.Serializable]
    public class Document
    {
        public string text;
    }

    [System.Serializable]
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

    public class ChatMessage
    {
        public string role;
        public string message;
        public ChatMessage(string role, string message)
        {
            this.role = role;
            this.message = message;
        }
    }

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

    [RequireComponent(typeof(AudioSource))]

    public class PromptInteractor : MonoBehaviour
    {
        private string cohereURL = "https://api.cohere.ai/v1/chat";
        private string cohereURLClassify = "https://api.cohere.ai/v1/classify";
        private string cohereReRank = "https://api.cohere.ai/v1/rerank";
        private string cohereAPI = "";

        private string elevenlabsURL = "https://api.elevenlabs.io/v1/text-to-speech/oW76A9DQMhaXu4F8IKwz";

        private string elevenlabsAPI = "";

        private string openAIAPI = "";

        public string generatedText = "";

        public int rankIndex = -1;

        private List<ChatMessage> chatHistory = new List<ChatMessage>();
        private List<ChatMessage> userMessages = new List<ChatMessage>();

        private List<ChatMessage> grammarSuggestionHistory = new List<ChatMessage>();
        private List<FrenchSentence> frenchClassificationSentences = new List<FrenchSentence>();
        private List<string> rerankDocumentsCoffee = new List<string>
        {
            "Bien sûr, vous pouvez commander quelque chose, que voudriez-vous",
            "Voici quelques recommandations de latte glacé, cappuccino, expresso shot",
            "désolé, je ne peux pas t'aider avec ça"
        };

        private List<string> rerankDocumentsMuseum = new List<string>
        {
            "merci de m'avoir dit ton tableau préféré",
            "-",
        };

        private List<string> rerankDocumentsMeadow = new List<string>
        {
            "C'est une belle carrière",
            "-",
        };


        private int countDown = 3;

        public string allUserSentences = "";

        AudioSource audioSource;

        //string fileloc = "Assets/Resources/keys.txt";


        [SerializeField] private Button recordButton;
        [SerializeField] private GameObject progressBar;
        [SerializeField] private TMP_Text response_text;

        private readonly string fileName = "output.wav";
        private readonly int duration = 5;

        private AudioClip clip;
        private bool isRecording;
        private float time;
        private OpenAIApi openai;
        private List<OpenAI.ChatMessage> messages = new List<OpenAI.ChatMessage>(); // open ai
        private string prompt = "Act as a random stranger in a chat room and reply to the questions. Don't break character. Don't ever mention that you are an AI model.";

        public int curScene = 0;


        public void StartRecording()
        {
            Debug.Log("CLICKED BUTTON");
            isRecording = true;
            recordButton.enabled = false;

            var index = PlayerPrefs.GetInt("user-mic-device-index");

#if !UNITY_WEBGL
            //clip = Microphone.Start("Android camcorder input", false, duration, 44100);
            clip = Microphone.Start("", false, duration, 44100);
            Debug.Log("DONE RECORDING");
#endif
        }

        private async void EndRecording()
        {
            Debug.Log("END RECORDING");
            //message.SetText("Transcripting...");

#if !UNITY_WEBGL
            Microphone.End(null);
#endif
            Debug.Log("trying to save file");
            byte[] data = SaveWav.Save(fileName, clip);
            Debug.Log("saved file");
            var req = new CreateAudioTranscriptionsRequest
            {
                FileData = new FileData() { Data = data, Name = "audio.wav" },
                // File = Application.persistentDataPath + "/" + fileName,
                Model = "whisper-1",
                Language = "fr"
            };
            var res = await openai.CreateAudioTranscription(req);
            Debug.Log("got response");
            SendReply(res.Text);
            chatHistory.Add(new ChatMessage("USER", res.Text));
            userMessages.Add(new ChatMessage("USER", res.Text));

            // check rerank
            switch (curScene)
            {
                case 1:
                    StartCoroutine(rerank(rerankDocumentsCoffee, res.Text));
                    break;
                case 2:
                    StartCoroutine(rerank(rerankDocumentsMuseum, res.Text));
                    break;
                case 3:
                    StartCoroutine(rerank(rerankDocumentsMeadow, res.Text));
                    break;
            }

                allUserSentences += res.Text + " ";
            LevelScript.allUserSentences1 += res.Text + " ";
            Debug.Log(res.Text);
            recordButton.enabled = true;
        }

        public string GeneratePreamble(int scene)
        {
            string prompt = "";
            switch (scene)
            {
                case 1:
                    prompt = "vous êtes une heureuse barista. Ne parle pas anglais. Gardez chaque réponse de 10 mots ou moins.";
                    break;
                case 2:
                    prompt = "tu es mon ami avec moi au musée, tu veux savoir quel est mon tableau préféré. Ne parle pas anglais. Gardez chaque réponse de 10 mots ou moins.";
                    break;
                case 3:
                    prompt = "tu es mon petit-enfant, parle de ce que tu veux être quand tu seras grand et demande-moi ce que je voulais être. Ne parle pas anglais. Gardez chaque réponse de 10 mots ou moins.";
                    break;
            }

            return prompt;
        }

        private async void SendReply(string text)
        {
            var newMessage = new OpenAI.ChatMessage()
            {
                Role = "user",
                Content = text
            };

            if (messages.Count == 0) newMessage.Content = GeneratePreamble(curScene) + "\n" + text;

            messages.Add(newMessage);

            //button.enabled = false;
            //inputField.text = "";
            //inputField.enabled = false;

            // Complete the instruction
            var completionResponse = await openai.CreateChatCompletion(new CreateChatCompletionRequest()
            {
                Model = "gpt-3.5-turbo-0613",
                Messages = messages
            });

            if (completionResponse.Choices != null && completionResponse.Choices.Count > 0)
            {
                var message = completionResponse.Choices[0].Message;
                message.Content = message.Content.Trim();
                messages.Add(message);
                StartCoroutine(CallElevenAPI(message.Content));
                response_text.SetText(message.Content);
                chatHistory.Add(new ChatMessage("CHATBOT", message.Content));
            }
            else
            {
                Debug.LogWarning("No text was generated from this prompt.");
            }
        }

        public IEnumerator callCohere(string input)
        {
            var data = new
            {
                message = input,
                chat_history = chatHistory,
                preamble_override = GeneratePreamble(curScene),
                presence_penalty = 1
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

                UnityEngine.Debug.Log("Text: " + response.text);
                generatedText = response.text;
                chatHistory.Add(new ChatMessage("CHATBOT", response.text));
                // StartCoroutine(evaluateUser());
                // StartCoroutine(generateReport());
                // StartCoroutine(getGrammarSuggestions());
                StartCoroutine(CallElevenAPI(generatedText));
                response_text.SetText(generatedText);
            }
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
            var data = new
            {
                inputs = new string[]
                {
                    allUserSentences
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
                // generatedText = response.text;
            }

        }

        public IEnumerator rerank(List<string> documents, string query)
        {
            Debug.Log("running rerank");
            // rerankDocuments.Add("Of course you can order something, what would you like");
            // rerankDocuments.Add("Here are some recommendations: iced latte, cappuccino, espresso shot");
            // rerankDocuments.Add("The washroom is to the right");
            // rerankDocuments.Add("Sorry, I can't help you with that");

            var data = new
            {
                return_documents = true,
                max_chunks_per_doc = 10,
                documents = documents,
                query = query
            };

            string jsonString = JsonConvert.SerializeObject(data, Formatting.Indented);
            UnityWebRequest request = UnityWebRequest.Put(cohereReRank, jsonString);
            request.method = "POST";
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + cohereAPI);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                UnityEngine.Debug.Log("issue encountered");
                UnityEngine.Debug.Log("Error: " + request.error);
                UnityEngine.Debug.Log("Error: " + request.downloadHandler.text);
            }
            else
            {
                UnityEngine.Debug.Log(request.downloadHandler.text);
                ReRankRoot response = JsonUtility.FromJson<ReRankRoot>(request.downloadHandler.text);

                Debug.Log(response.results[0].index);
                // generatedText = response.text;
                rankIndex = response.results[0].index;
            }

        }

        public IEnumerator generateReport()
        {

            var data = new
            {
                preamble_override = "You are a smart French grammar assistant who suggests general ideas for how to improve in French from a series of sentences. If you cannot find grammar errors, suggest new vocaulary words.",
                message = allUserSentences
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
                // generatedText = response.text;
            }

        }

        public IEnumerator getGrammarSuggestions()
        {

            string promptMessage = "Please provide concise grammar suggestions for the following sentences: " + allUserSentences;

            var data = new
            {
                preamble_override = "You are a smart French grammar assistant who corrects mistakes made in French.",
                message = promptMessage
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
                // generatedText = response.text;
            }

        }


        public IEnumerator CallElevenAPI(string text)
        {
            // Create a dictionary for voice settings
            var voiceSettings = new Dictionary<string, double>
        {
            { "similarity_boost", 0.5 },
            { "stability", 0.5 }
        };

            // Create an anonymous object to represent the data structure
            var data = new
            {
                model_id = "eleven_multilingual_v2",
                text = text,
                voice_settings = voiceSettings
            };

            // Serialize the anonymous object to JSON string
            string jsonString = JsonConvert.SerializeObject(data, Formatting.Indented);

            DownloadHandlerAudioClip downloadHandler = new DownloadHandlerAudioClip(string.Empty, AudioType.MPEG);
            // downloadHandler.streamAudio = true;

            UnityWebRequest request1 = UnityWebRequest.Put(elevenlabsURL, jsonString);
            request1.method = "POST";
            request1.SetRequestHeader("Content-Type", "application/json");
            request1.SetRequestHeader("xi-api-key", elevenlabsAPI);
            request1.downloadHandler = downloadHandler;

            Debug.Log("eleven calling api");

            yield return request1.SendWebRequest();

            // AudioClip audio1 = null;
            // audio1 = Resources.Load<AudioClip>("pikapika");
            // audioSource.clip = audio1;
            // audioSource.Play();

            if (request1.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(request1.error);
                //Debug.Log(request1.downloadHandler.text);
                Debug.Log("problem");
            }
            else
            {
                Debug.Log("Form upload complete!");

                int retryCount = 0;
                int maxRetries = 10;
                AudioClip audio = null;
                // bool shouldRetry = false;

                while (retryCount < maxRetries)
                {
                    audio = DownloadHandlerAudioClip.GetContent(request1);
                    //yield return new WaitForSeconds(1.0f);
                    //if (audio != null)
                    //{
                    //    audioSource.clip = audio;
                    //    audioSource.Play();
                    //}
                    //else
                    //{
                    //    AudioClip audio_error = null;
                    //    Debug.LogError("Error while getting audio clip: ");
                    //    audio_error = Resources.Load<AudioClip>("pikapika");
                    //    audioSource.clip = audio_error;
                    //    audioSource.Play();
                    //}

                    if (!audio)
                    {
                        // Wait for 1 second before retrying
                        Debug.Log("its null");
                        yield return new WaitForSeconds(1f);
                        retryCount++;
                    }
                    else
                    {
                        break; // Break the loop if no need to retry
                    }
                }

                if (audio != null)
                {
                    // Use the audioClip here
                    Debug.Log("Audio clip loaded successfully.");

                    audioSource.clip = audio;
                    audioSource.Play();
                    yield return new WaitForSeconds(audioSource.clip.length + 3);
                }


                //try
                //{
                //AudioClip audio = DownloadHandlerAudioClip.GetContent(request1);
                //float[] myFloatArray = new float[audio.samples * audio.channels];
                //audio.GetData(myFloatArray, 0);
                //foreach (float a in myFloatArray)
                //{
                //    Debug.Log(a);
                //}
                //audioSource.clip = audio;
                //audioSource.Play();
                //}
            }
            Debug.Log("RANK INDEX: " + rankIndex);
            countDown--;
            if (rankIndex == 0)
            {
                countDown = 2 > countDown ? countDown : 2;
            }

            if (countDown == 0)
            {
                if (curScene == 3)
                {
                    Debug.Log(allUserSentences);
                    LevelScript.allUserSentences1 = allUserSentences;
                    SceneManager.LoadScene("TipsScene");
                }
                else if (curScene == 1)
                {
                    SceneManager.LoadScene("darkTheme");
                }
                else SceneManager.LoadScene("MenuScene");
            }
        }

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

            elevenlabsAPI = lines[0];
            cohereAPI = lines[1];
            openAIAPI = lines[2];
            openai = new OpenAIApi(openAIAPI);
            Debug.Log("runing");
            List<string> rerankDocuments = new List<string>();
            rerankDocuments.Add("Of course you can order something, what would you like");
            rerankDocuments.Add("Here are some recommendations: iced latte, cappuccino, espresso shot");
            rerankDocuments.Add("The washroom is to the right");
            rerankDocuments.Add("Sorry, I can't help you with that");

            Debug.Log("runing");
            StartCoroutine(rerank(rerankDocuments, "can you recommend something"));
            Debug.Log(rankIndex);
      
            // StartCoroutine(callCohere());
            audioSource = GetComponent<AudioSource>();
            // generatedText = "La France est un pays aux multiples facettes, riche d'une histoire profonde et d'une culture diversifiée. De la splendeur de Paris avec sa Tour Eiffel emblématique, ses musées d'art de renommée mondiale comme le Louvre, et ses charmantes rues pavées, à la beauté bucolique des régions telles que la Provence et la Vallée de la Loire, la France offre une expérience unique à chaque visiteur. La gastronomie française, réputée pour sa finesse et sa diversité, va des fromages savoureux et des vins délicats aux pâtisseries exquises et aux plats traditionnels comme le coq au vin. ";
            // StartCoroutine(CallElevenAPI());
            recordButton.onClick.AddListener(StartRecording);
            // StartCoroutine(callCohere());
            // { 
            //     StartCoroutine(CallElevenAPI(generatedText));
            // }
            // await callCohere();
            // await CallElevenAPI();
            // StartCoroutine(callCohere(() => StartCoroutine(CallElevenAPI())));

        }

        // Update is called once per frame
        void Update()
        {


            if (isRecording)
            {
                time += Time.deltaTime;
                float progress = time / duration;

                Vector3 scale = progressBar.transform.localScale;
                scale.x = Mathf.Clamp01(progress); // Ensure the scale is between 0 and 1
                progressBar.transform.localScale = scale;
                //Debug.Log("timing");

                if (time >= duration)
                {
                    time = 0;
                    isRecording = false;
                    EndRecording();
                    Debug.Log("booomba");
                }
            }

            if (OVRInput.Get(OVRInput.Button.One))
            {
                Debug.Log("A button pressed");
            }
        }
    }

}