using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using USER;

public class LetterControl : MonoBehaviour
{

    public char ExpectedLetter = ' ';

    public bool destroyOnLetterEmpty = false;
    public bool canChangeLetter = false;
    public bool interchangeable = false;

    public TextMeshProUGUI LETTER_TXT;
    public RectTransform rectTransform;

    private CanvasGroup canvasGroup;
    private CanvasScaler scaler;
    private Vector2 mouseOffset = Vector2.zero;

    private void Awake()
    {
        scaler = GameController.instance.UI.canvas.GetComponent<CanvasScaler>();
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void SetLetter(char l, bool forceLetter = false)
    {
        if(forceLetter)
        {
            LETTER_TXT.text = l.ToString();
            return;
        }

        if (canChangeLetter)
        {
            LETTER_TXT.rectTransform.anchoredPosition = Vector2.zero;
            LETTER_TXT.text = l.ToString();

            if (destroyOnLetterEmpty)
                Destroy(gameObject);
        }
    }

    #region EventTrigger EVENTS
    public void ev_BeginDrag()
    {
        if (canChangeLetter)
        {
            LETTER_TXT.rectTransform.SetParent(GameController.instance.UI.FreeLetters_Parent, false);
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void ev_Drag()
    {
        if (canChangeLetter)
        {
            LETTER_TXT.rectTransform.position = Input.mousePosition;
        }
    }

    public void ev_EndDrag()
    {
        LETTER_TXT.rectTransform.SetParent(transform, false);
        LETTER_TXT.rectTransform.anchoredPosition = Vector2.zero;
        canvasGroup.blocksRaycasts = true;

        LetterControl hoveredLetter = GameController.instance.UI.currentHoveredLetter;
        if (hoveredLetter == null)
        {
            LETTER_TXT.rectTransform.anchoredPosition = Vector2.zero;
        }
        else
        {
            if (!hoveredLetter.canChangeLetter)
                return;

            if (hoveredLetter.LETTER_TXT.text[0] == ' ')
            {
                if (hoveredLetter.ExpectedLetter != LETTER_TXT.text[0])
                {
                    GameController.instance.UI.ShowFeedback(-1);
                    //Debug.Log("Letra inesperada!");
                    return;
                }

                hoveredLetter.SetLetter(LETTER_TXT.text[0]);
                SetLetter(' ');
            }
            else
            {
                if (!interchangeable)
                    return;

                char letter_a, letter_b;
                letter_a = LETTER_TXT.text[0];
                letter_b = hoveredLetter.LETTER_TXT.text[0];

                hoveredLetter.SetLetter(letter_a);
                SetLetter(letter_b);
            }
        }
    }

    public void ev_MouseHover(bool enter)
    {
        if(canChangeLetter)
        {
            if(enter)
                GameController.instance.UI.currentHoveredLetter = this;
            else
                GameController.instance.UI.currentHoveredLetter = null;
        }
    }
    #endregion
}
