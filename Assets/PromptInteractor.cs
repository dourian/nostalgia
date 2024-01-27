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

namespace Samples.Whisper
{

    [System.Serializable]
    public class ApiResponse
    {
        public string response_id;
        public string text;
        public string generation_id;
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


    [RequireComponent(typeof(AudioSource))]

    public class PromptInteractor : MonoBehaviour
    {
        private string cohereURL = "https://api.cohere.ai/v1/chat";
        private string cohereAPI = "";

        private string elevenlabsURL = "https://api.elevenlabs.io/v1/text-to-speech/Pln23uvtFNFOqsWYkpst";

        private string elevenlabsAPI = "";

        public string generatedText = "";

        private List<ChatMessage> chatHistory = new List<ChatMessage>();
        private List<ChatMessage> userMessages = new List<ChatMessage>();

        private List<ChatMessage> grammarSuggestionHistory = new List<ChatMessage>();


        AudioSource audioSource;

        string fileloc = "./Assets/keys.txt";

        // string apiKey;

        // Start is called before the first frame update


        [SerializeField] private Button recordButton;
        [SerializeField] private Image progressBar;
        [SerializeField] private TMP_Text message;

        private readonly string fileName = "output.wav";
        private readonly int duration = 5;

        private AudioClip clip;
        private bool isRecording;
        private float time;
        private OpenAIApi openai = new OpenAIApi();

        private void StartRecording()
        {
            Debug.Log("CLICKED BUTTON");
            isRecording = true;
            recordButton.enabled = false;

            var index = PlayerPrefs.GetInt("user-mic-device-index");

#if !UNITY_WEBGL
            clip = Microphone.Start("", false, duration, 44100);
            Debug.Log("DONE RECORDING");
#endif
        }

        private async void EndRecording()
        {
            Debug.Log("END RECORDING");
            message.SetText("Transcripting...");

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
                Language = "en"
            };
            var res = await openai.CreateAudioTranscription(req);
            Debug.Log("got response");
            message.SetText(res.Text);
            chatHistory.Add(new ChatMessage("USER", res.Text));
            Debug.Log(res.Text);
            recordButton.enabled = true;
        }

        public IEnumerator callCohere()
        {

            var data = new
            {
                message = "What is my name?",
                chat_history = chatHistory
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
                StartCoroutine(getGrammarSuggestions());
                // StartCoroutine(CallElevenAPI(generatedText));
            }
        }

        // public IEnumerator evaluateUser() 
        // {

        // }

        public IEnumerator getGrammarSuggestions()
        {
            userMessages.Add(new ChatMessage("USER", "Je me appelle Sarina"));
            int loopCount = Math.Min(5, userMessages.Count);
            string userSentences = "";

            for (int i = 0; i < loopCount; i++)
            {
                ChatMessage message = userMessages[i];
                // Process message here
                userSentences += message.message + " ";
            }

            string promptMessage = "Please provide concise grammar suggestions for the following sentences: " + userSentences;

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
                Debug.Log("problem");
            }
            else
            {
                Debug.Log("Form upload complete!");

                // int retryCount = 0;
                // int maxRetries = 3;
                AudioClip audio = null;
                // bool shouldRetry = false;

                // while (retryCount < maxRetries)
                // {
                audio = DownloadHandlerAudioClip.GetContent(request1);
                if (audio != null)
                {
                    audioSource.clip = audio;
                    audioSource.Play();
                }
                else
                {
                    AudioClip audio_error = null;
                    Debug.LogError("Error while getting audio clip: ");
                    audio_error = Resources.Load<AudioClip>("pikapika");
                    audioSource.clip = audio_error;
                    audioSource.Play();
                }


                // if (!audio)
                // {
                //     // Wait for 1 second before retrying
                //     Debug.Log("its null");
                //     yield return new WaitForSeconds(1.0f);
                //     retryCount++;
                // }
                // else
                // {
                //     break; // Break the loop if no need to retry
                // }
            }

            // if (audio != null)
            // {
            //     // Use the audioClip here
            //     Debug.Log("Audio clip loaded successfully.");
            // }


            // try {
            // AudioClip audio = DownloadHandlerAudioClip.GetContent(request1);
            // float[] myFloatArray = new float[audio.samples * audio.channels];
            // audio.GetData(myFloatArray, 0);
            // foreach (float a in myFloatArray)
            // {
            //     Debug.Log(a);
            // }
            // audioSource.clip = audio;
            // audioSource.Play();
            // }
        }

        void Start()
        {
            string[] lines;
            lines = File.ReadAllLines(fileloc);
            elevenlabsAPI = lines[0];
            cohereAPI = lines[1];
            StartCoroutine(callCohere());
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
                //progressBar.fillAmount = time / duration;
                Debug.Log("timing");

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