using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Configuration
{

    public static bool shuffleTopLetters = true;
    #region BUTTON IMAGE
    public static Sprite ButtonImage = null;
    public delegate void onImagenBotonChange(Sprite sprite);
    public static onImagenBotonChange OnButtonIMG_Change;
    public static void Change_ButtonImage(Texture2D img)
    {
        ButtonImage = Sprite.Create(img, new Rect(0, 0, img.width, img.height), new Vector2(0.5f, 0.5f));

        OnButtonIMG_Change?.Invoke(ButtonImage);
    }
    #endregion

}
