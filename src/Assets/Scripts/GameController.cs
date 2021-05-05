using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Linq;
using UnityEngine.SceneManagement;

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
        
        [System.Serializable]
        public class UI_Letter
        {
            public GameObject gameObject;
            public LetterControl spawned_letter;

            public UI_Letter(GameObject obj, char letter)
            {
                spawned_letter = obj.GetComponent<LetterControl>();
                gameObject = obj;

                ChangeLetter(letter);
            }

            public void ChangeLetter(char l)
            {
                spawned_letter.SetLetter(l, true);
            }
        }

        [System.Serializable]
        public class _GAME
        {
            [System.Serializable]
            public class WORD
            {
                [Header("PALABRA")]
                public string PALABRA = "default";
                public string LETRAS_A_USAR = "AEIOU";

                [Header("IMAGEN")]
                public string LINK_IMAGEN = "";
                public Texture2D TEXTURE = null;
                public string ERROR_IMAGEN = "";


                public WORD(string palabraInicial, string letras, string linkImagen)
                {
                    PALABRA = palabraInicial;
                    if (letras.Length >= 3)
                        LETRAS_A_USAR = letras;
                    LINK_IMAGEN = linkImagen;

                    if (LINK_IMAGEN.Length < 5)
                        TEXTURE = Resources.Load("Textures/noimg_placeholder") as Texture2D;
                }

                public IEnumerator LoadImage()
                {
                    if (LINK_IMAGEN.Length < 10)
                        yield break;

                    using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(LINK_IMAGEN))
                    {
                        yield return www.SendWebRequest();

                        if (www.result != UnityWebRequest.Result.Success)
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
            public int PalabraActual = -1;

            [HideInInspector]
            public bool Waiting = false;

            private bool isRunning = false;

            public void AdvanceWord(float loadingTime = 0.2f)
            {
                PalabraActual++;
                UIConf UI = GameController.instance.UI;

                if (PalabraActual >= PALABRAS.Count)
                {
                    Debug.Log("endgame reached, with no endgame declaration");
                    UI.ShowLevelEnd(new WORD("END", "", "https://upload.wikimedia.org/wikipedia/en/1/1a/Smiley-Face.png"));
                    isRunning = false;
                    return;
                }
                void onComplete()
                {
                    WORD word = PALABRAS[PalabraActual];

                    if (!word.PALABRA.Contains("_"))
                    {
                        UI.CreateWord(PALABRAS[PalabraActual]);
                    }
                    else
                    {
                        if (word.PALABRA.Contains("_MOSTRAR.CAMBIONIVEL"))
                        {
                            UI.ShowLevelChange(word);
                            Waiting = true;
                        }

                        if (word.PALABRA.Contains("_MOSTRAR.FINAL"))
                        {
                            UI.ShowLevelEnd(word);
                            isRunning = false;
                        }

                        if (word.PALABRA.Contains("_SET.BOTON"))
                        {
                            Configuration.Change_ButtonImage(word.TEXTURE);
                            AdvanceWord(0);
                        }
                    }
                }

                UI.DeleteEveryLetter(0);
                if (loadingTime > 0)
                {
                    UI.ShowLoadingScreen(true, time: loadingTime, onComplete: () =>
                     {
                         onComplete();
                         LeanTween.delayedCall(0.5f, () => UI.ShowLoadingScreen(false, time: loadingTime));
                     });
                }
                else
                {
                    onComplete();
                }
            }

            public bool CheckWord()
            {
                UIConf UI = GameController.instance.UI;
                WORD word = PALABRAS[PalabraActual];
                string current = "";

                foreach (UI_Letter l in UI.currentWordLetters)
                {
                    current += l.spawned_letter.LETTER_TXT.text[0];
                }

                return current.ToUpper() == word.PALABRA.ToUpper();
            }

            public void Start()
            {
                if(!isRunning)
                {
                    isRunning = true;
                    gameObject.StartCoroutine(CORO_Game());
                }
            }

            public IEnumerator CORO_Game()
            {
                UIConf UI = GameController.instance.UI;

                UI.ShowLoadingScreen(true, "Cargando...");
                yield return new WaitForSeconds(1.25f);
                UI.EnableMenu(1);
                AdvanceWord();
                yield return new WaitForSeconds(.25f);
                UI.ShowLoadingScreen(false);

                while (gameObject.enabled && isRunning && PalabraActual < PALABRAS.Count)
                {
                    WORD word = PALABRAS[PalabraActual];
                    if (word.PALABRA.Contains("_"))
                    {
                        yield return new WaitUntil(() => !Waiting);
                        AdvanceWord(0);
                        yield return new WaitForSeconds(.25f);
                    }

                    if (!CheckWord())
                        yield return new WaitForSeconds(0.5f);
                    else
                    {
                        UI.ShowFeedback(0);
                        yield return new WaitForSeconds(.75f);

                        AdvanceWord();
                        yield return new WaitForSeconds(1.0f);
                    }

                    yield return null;
                }
            }
        }

        [System.Serializable]
        public class UIConf
        {

            public Canvas canvas;
            public CanvasScaler canvas_scaler;

            [HideInInspector]
            public MonoBehaviour gameObject;

            [Header("SCREENS")]
            public RectTransform[] MENUS;
            public CanvasGroup LOADING_SCREEN;
            public TextMeshProUGUI LOADING_TEXT;

            public RawImage GAME_IMAGE;
            public Vector2 GAME_ImagePreferredSize;
            public void SetImage(Texture2D image)
            {
                float newWidth = 0;
                float newHeight = 0;

                if (image.width <= image.height)
                {
                    newWidth = (GAME_ImagePreferredSize.x * image.width) / image.height;
                    newHeight = GAME_ImagePreferredSize.y;
                }
                else
                {
                    newWidth = GAME_ImagePreferredSize.x;
                    newHeight = (GAME_ImagePreferredSize.y * image.height) / image.width;
                }

                GAME_IMAGE.rectTransform.sizeDelta = new Vector2(newWidth, newHeight);
                GAME_IMAGE.texture = image;
            }

            [Header("LETTERS")]
            public Transform LETTERS_Parent;
            public Transform DRAGGABLE_LETTERS_Parent;
            public Transform FreeLetters_Parent;
            public GameObject LETTER_Prefab;

            public List<UI_Letter> currentWordLetters = new List<UI_Letter>();
            private List<UI_Letter> currentDraggableLetters = new List<UI_Letter>();
            public LetterControl currentHoveredLetter = null;

            public delegate void DEL();
            public DEL OnWrongLetter;

            public UI_Letter AddLetter(int where, char letter)
            {
                Transform parent = null;
                UI_Letter r = null;

                if (where == 0)
                {
                    parent = LETTERS_Parent;
                    GameObject obj = Instantiate(LETTER_Prefab, parent) as GameObject;
                    r = new UI_Letter(obj, letter);
                    currentWordLetters.Add(r);
                }
                else
                {
                    parent = DRAGGABLE_LETTERS_Parent;
                    GameObject obj = Instantiate(LETTER_Prefab, parent) as GameObject;
                    r = new UI_Letter(obj, letter);
                    currentDraggableLetters.Add(new UI_Letter(obj, letter));
                }

                return r;
            }

            public void DeleteAllLettersAt(int where, float anim_speed = 0.2f)
            {
                List<GameObject> gameObjects = new List<GameObject>();
                if(where == 0)
                {
                    if (currentWordLetters.Count == 0)
                        return;
                    gameObjects.AddRange(currentWordLetters.Select(l => l.gameObject));
                    currentWordLetters.Clear();
                }
                else
                {
                    if (currentDraggableLetters.Count == 0)
                        return;
                    gameObjects.AddRange(currentDraggableLetters.Select(l => l.gameObject));
                    currentDraggableLetters.Clear();
                }

                if (anim_speed < 0)
                    anim_speed = 0;

                for (int i = 0; i < gameObjects.Count; i++)
                {
                    GameObject go = gameObjects[i];

                    if(anim_speed == 0)
                    {
                        Destroy(go);
                    }
                    else
                    {
                        LeanTween.scale(go, Vector3.zero, anim_speed)
                            .setEaseInOutBounce();
                        LeanTween.delayedCall(anim_speed + 0.05f, () => Destroy(go));
                    }
                }
            }
        
            public void DeleteEveryLetter(float anim_speed = 0.2f)
            {
                DeleteAllLettersAt(0, anim_speed); DeleteAllLettersAt(1, anim_speed);
            }

            public void CreateWord(_GAME.WORD word, float delay = 0.025f)
            {
                SetImage(word.TEXTURE);
                gameObject.StartCoroutine(CORO_CreateWord(0, word, delay));
            }

            private IEnumerator CORO_CreateWord(int where, _GAME.WORD word, float delay = 0.05f)
            {
                if (delay < 0)
                    delay = 0;

                string final_word = "";
                List<char> used_characters = new List<char>();

                for (int i = 0; i < word.PALABRA.Length; i++)
                {
                    if (word.LETRAS_A_USAR.Contains(word.PALABRA[i].ToString()))
                    {
                        used_characters.Add(word.PALABRA[i]);
                        final_word += ' ';
                    }
                    else
                    {
                        final_word += word.PALABRA[i];
                    }
                }

                if (Configuration.shuffleTopLetters)
                    used_characters.Shuffle();

                foreach (char c in used_characters)
                {
                    UI_Letter letter = AddLetter(1, c);
                    letter.spawned_letter.canChangeLetter = true;
                    letter.spawned_letter.destroyOnLetterEmpty = true;
                    letter.spawned_letter.interchangeable = false;
                    yield return new WaitForSeconds(delay);
                }

                for (int i = 0; i < final_word.Length; i++)
                {
                    char c = final_word[i];
                    UI_Letter letter = AddLetter(where, c);
                    letter.spawned_letter.ExpectedLetter = word.PALABRA[i];

                    if (c == ' ')
                    {
                        letter.spawned_letter.canChangeLetter = true;
                    }
                    yield return new WaitForSeconds(delay);
                }
            }

            public void ShowLoadingScreen(bool show, string text = "", float time = 0.2f, System.Action onComplete = null)
            {
                LOADING_SCREEN.blocksRaycasts = show;
                LOADING_TEXT.text = text;

                LeanTween.cancel(LOADING_SCREEN.gameObject);

                if (!show)
                    LeanTween.value(LOADING_SCREEN.alpha, 0, time).setOnUpdate((float f) => LOADING_SCREEN.alpha = f)
                        .setOnComplete(() =>
                        {
                            onComplete?.Invoke();
                        });
                else
                    LeanTween.value(LOADING_SCREEN.alpha, 1, time).setOnUpdate((float f) => LOADING_SCREEN.alpha = f)
                        .setOnComplete(() =>
                        {
                            onComplete?.Invoke();
                            LeanTween.cancel(Feedback_IMG.gameObject);
                            Feedback_IMG.transform.localScale = Vector3.zero;
                        });
            }

            public void EnableMenu(int index)
            {
                for(int i = 0; i < MENUS.Length; i++)
                    MENUS[i].gameObject.SetActive(i == index);
            }


            [Header("LEVEL CHANGE")]
            public CanvasGroup LevelChange_PARENT;
            public TextMeshProUGUI LevelChange_NAME;
            public RawImage LevelChange_IMAGE;

            public void ShowLevelChange(_GAME.WORD word)
            {
                if(word != null)
                {
                    LevelChange_NAME.text = word.LETRAS_A_USAR;
                    LevelChange_IMAGE.texture = word.TEXTURE;

                    LevelChange_PARENT.gameObject.SetActive(true);

                    LeanTween.moveLocalY(LevelChange_PARENT.gameObject, 0, 1.5f).setEaseInOutCubic().setOnComplete(() => LevelChange_PARENT.interactable = true);
                }
                else
                {
                    LevelChange_PARENT.interactable = false;
                    LeanTween.moveLocalY(LevelChange_PARENT.gameObject, -canvas_scaler.referenceResolution.y, 1.5f).setEaseInOutCubic()
                        .setOnComplete(() => LevelChange_PARENT.gameObject.SetActive(false));
                }
            }

            [Header("LEVEL END")]
            public CanvasGroup LevelEnd_PARENT;
            public RawImage LevelEnd_IMAGE;

            public void ShowLevelEnd(_GAME.WORD word)
            {
                if (word != null)
                {
                    LevelEnd_IMAGE.texture = word.TEXTURE;
                    LevelEnd_PARENT.gameObject.SetActive(true);

                    LeanTween.moveLocalY(LevelEnd_PARENT.gameObject, 0, 1.5f).setEaseInOutCubic()
                        .setOnComplete(() => LevelEnd_PARENT.interactable = true);
                }
                else
                {
                    LevelEnd_PARENT.interactable = false;
                    LeanTween.moveLocalY(LevelEnd_PARENT.gameObject, -canvas_scaler.referenceResolution.y, 1.5f).setEaseInOutCubic()
                        .setOnComplete(() => LevelEnd_PARENT.gameObject.SetActive(false));
                }
            }

            [Header("PLAYER FEEDBACK")]
            public RawImage Feedback_IMG;
            public float Feedback_Time, Feedback_sleepTime;
            public Texture2D Feedback_right, Feedback_wrong;

            public void ShowFeedback(int index)
            {
                if(index == 0)
                    Feedback_IMG.texture = Feedback_right;
                else
                    Feedback_IMG.texture = Feedback_wrong;

                RectTransform rt = Feedback_IMG.GetComponent<RectTransform>();
                LeanTween.scale(rt, Vector3.one, Feedback_Time)
                    .setEaseOutElastic()
                    .setOnComplete(() => LeanTween.delayedCall(Feedback_sleepTime, () =>
                    {
                        LeanTween.scale(rt, Vector3.zero, Feedback_Time / 2);
                    }));
            }
        }

        public UIConf UI;
        public _GAME GAME;

        [Header("CSV")]
        public string csv_url = "https://pastebin.com/raw/AjTa5ehC";
        public char csv_separator = ',';
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
            if (Input.GetKeyDown(KeyCode.P))
            {
                StartCoroutine(GAME.CORO_Game());
            }
        }

        public void UI_Button(string action)
        {
            action = action.ToLower();
            switch(action)
            {
                case "startgame":
                    GAME.Start();
                    break;
                case "continue":
                    GAME.Waiting = false;
                    UI.ShowLevelChange(null);
                    break;
                case "reload":
                    SceneManager.LoadScene(0);
                    break;
            }
        }

        #region COROUTINES
        private IEnumerator CORO_ReadCSVFromURL(string url)
        {
            UnityWebRequest.ClearCookieCache();
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
                csvreader = new CSVReader(url_data.Split('\n'), csv_separator);

                yield return new WaitForSeconds(1.0f);

                for(int i = 1; i < csvreader.max_rows; i++)
                {
                    string cell_0 = "";
                    string cell_1 = "";
                    string cell_2 = "";
                    string failedStr = null;

                    try
                    {
                        cell_0 = csvreader.ReturnCell(i, 0);
                        cell_1 = csvreader.ReturnCell(i, 1);
                        cell_2 = csvreader.ReturnCell(i, 2);
                    }
                    catch(System.Exception ex)
                    {
                        failedStr = ex.Message;
                    }

                    if(failedStr != null)
                    {
                        UI.LOADING_TEXT.text = $"Error cargando celda en la linea {i}\n{failedStr}";
                        yield return new WaitForSeconds(1.5f);
                        continue;
                    }

                    UI.LOADING_TEXT.text = $"Generando objeto de la linea {i}...";
                    _GAME.WORD word = new _GAME.WORD(
                        cell_0,
                        cell_1,
                        cell_2);

                    yield return word.LoadImage();

                    if(word.ERROR_IMAGEN.Length >= 3)
                    {
                        UI.LOADING_TEXT.text = $"Error generando imagen en la linea N{i}! '{word.PALABRA}'\n{word.ERROR_IMAGEN}";
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