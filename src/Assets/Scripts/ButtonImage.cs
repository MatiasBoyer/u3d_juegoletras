using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace USER
{
    public class ButtonImage : MonoBehaviour
    {

        public Image image;

        private void Awake()
        {
            image = GetComponent<Image>();
            image.sprite = Configuration.ButtonImage;
        }

        private void OnEnable()
        {
            Configuration.OnButtonIMG_Change += OnButtonIMG_Change;
        }
        private void OnDisable()
        {

            Configuration.OnButtonIMG_Change -= OnButtonIMG_Change;
        }

        private void OnButtonIMG_Change(Sprite sprite)
        {
            image.sprite = sprite;
        }

    }
}