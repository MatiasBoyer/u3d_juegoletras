using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;

namespace USER
{
    public class GameController : MonoBehaviour
    {

        private static GameController Instance;
        public static GameController instance
        {
            get
            {
                if (Instance == null)
                    Instance = GameObject.FindObjectOfType<GameController>();

                return Instance;
            }
        }

        public class UI_Letter
        {
            public Transform letter_transform;
            public RectTransform letter_rectTransform;
            public TextMeshProUGUI letter_tmpgui;

            public UI_Letter(GameObject obj, char letter)
            {
                letter_transform = obj.transform;
                letter_rectTransform = obj.GetComponent<RectTransform>();
                letter_tmpgui = obj.transform.Find("LETTER").GetComponent<TextMeshProUGUI>();

                ChangeLetter(letter);
            }

            public void ChangeLetter(char l)
            {
                letter_tmpgui.text = l.ToString();
            }
        }

        [System.Serializable]
        public class _GAME
        {
            [System.Serializable]
            public class WORD
            {
                [Header("PALABRA")]
                public string PALABRA_FINAL = "default";
                public string PALABRA_INICIAL = "def ult";
                public string LETRAS_A_USAR = "AEIOU";

                [Header("IMAGEN")]
                public string LINK_IMAGEN = "";
                public Texture2D TEXTURE = null;
                public string ERROR_IMAGEN = "";


                public WORD(string palabraInicial, string palabraFinal, string letras, string linkImagen)
                {
                    PALABRA_INICIAL = palabraInicial;
                    PALABRA_FINAL = palabraFinal;
                    if(letras.Length >= 3)
                        LETRAS_A_USAR = letras;
                    LINK_IMAGEN = linkImagen;
                }

                public IEnumerator LoadImage()
                {
                    if (LINK_IMAGEN.Length < 10)
                        yield break;

                    using(UnityWebRequest www = UnityWebRequestTexture.GetTexture(LINK_IMAGEN))
                    {
                        yield return www.SendWebRequest();

                        if(www.result != UnityWebRequest.Result.Success)
                        {
                            ERROR_IMAGEN = www.error;
                            Debug.LogError($"Error loading texture!\n {www.error}");
                        }
                        else
                        {
                            TEXTURE = DownloadHandlerTexture.GetContent(www);
                        }
                    }
                }
            }

            [HideInInspector]
            public MonoBehaviour gameObject;

            public List<WORD> PALABRAS = new List<WORD>();
            
        }


        [System.Serializable]
        public class UIConf
        {

            [HideInInspector]
            public MonoBehaviour gameObject;

            [Header("SCREENS")]
            public RectTransform[] MENUS;
            public CanvasGroup LOADING_SCREEN;
            public TextMeshProUGUI LOADING_TEXT;

            [Header("LETTERS")]
            public Transform LETTERS_Parent;
            public GameObject LETTER_Prefab;

            private List<UI_Letter> currentLetters = new List<UI_Letter>();

            public void AddLetter(char letter)
            {
                GameObject obj = Instantiate(LETTER_Prefab, LETTERS_Parent) as GameObject;
                currentLetters.Add(new UI_Letter(obj, letter));
            }

            public void DeleteAllLetters(float anim_speed = 0.2f)
            {
                foreach (UI_Letter v in currentLetters)
                {
                    if (anim_speed > 0)
                    {
                        LeanTween.scale(v.letter_rectTransform, Vector3.one * 0, anim_speed)
                            .setOnComplete(() => Destroy(v.letter_transform.gameObject));
                    }
                    else
                    {
                        Destroy(v.letter_rectTransform.gameObject);
                    }
                }

                currentLetters.Clear();
            }

            public void CreateWord(string word, float delay)
            {
                gameObject.StartCoroutine(CORO_CreateWord(word, delay));
            }

            private IEnumerator CORO_CreateWord(string word, float delay = 0.05f)
            {
                if (delay < 0)
                    delay = 0;

                foreach (char c in word)
                {
                    AddLetter(c);
                    yield return new WaitForSeconds(delay);
                }
            }

            public void ShowLoadingScreen(bool show, float time = 0.2f)
            {
                LOADING_SCREEN.blocksRaycasts = show;

                LeanTween.cancel(LOADING_SCREEN.gameObject);

                if(!show)
                    LeanTween.value(LOADING_SCREEN.alpha, 0, time).setOnUpdate((float f) => LOADING_SCREEN.alpha = f);
                else
                    LeanTween.value(LOADING_SCREEN.alpha, 1, time).setOnUpdate((float f) => LOADING_SCREEN.alpha = f);
            }
        }

        public UIConf UI;
        public _GAME GAME;

        [Header("CSV")]
        public string csv_url = "https://pastebin.com/raw/AjTa5ehC";
        public CSVReader csvreader;

        private void Awake()
        {
            UI.gameObject = this;
            GAME.gameObject = this;

            UI.LOADING_SCREEN.alpha = 1;
        }

        private void Start()
        {
            StartCoroutine(CORO_ReadCSVFromURL(csv_url));
        }

        private void LateUpdate()
        {
            if (Input.GetKeyDown(KeyCode.L))
                UI.DeleteAllLetters();
        }

        #region COROUTINES
        private IEnumerator CORO_ReadCSVFromURL(string url)
        {
            UnityWebRequest www = UnityWebRequest.Get(url);

            UI.ShowLoadingScreen(true);
            UI.LOADING_TEXT.text = "Cargando base de datos...";
            yield return new WaitForSeconds(1.0f);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                UI.LOADING_TEXT.text = $"Error cargando la base de datos!\n{www.error}";
                Debug.Log(www.error);
            }
            else
            {

                UI.LOADING_TEXT.text = "Convirtiendo base de datos...";
                string url_data = ((DownloadHandler)www.downloadHandler).text;
                csvreader = new CSVReader(url_data.Split('\n'));

                yield return new WaitForSeconds(1.0f);

                for(int i = 1; i < csvreader.max_rows; i++)
                {
                    UI.LOADING_TEXT.text = $"Generando palabra N{i+1}...";
                    _GAME.WORD word = new _GAME.WORD(
                        csvreader.ReturnCell(i, 0),
                        csvreader.ReturnCell(i, 1),
                        csvreader.ReturnCell(i, 2),
                        csvreader.ReturnCell(i, 3)
                        );

                    yield return word.LoadImage();

                    if(word.ERROR_IMAGEN.Length >= 3)
                    {
                        UI.LOADING_TEXT.text = $"Error generando palabra en la linea N{i + 1} !";
                        yield return new WaitForSeconds(2.0f);
                    }

                    GAME.PALABRAS.Add(word);
                }

                UI.LOADING_TEXT.text = "Carga completa!";

                yield return new WaitForSeconds(1.5f);
                UI.ShowLoadingScreen(false);
            }

            //csvreader = new CSVReader()
        }
        #endregion

    }
}