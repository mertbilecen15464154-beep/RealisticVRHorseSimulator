using UnityEngine;

public enum GameLanguage
{
    Turkish,
    English,
    German
}

[System.Serializable]
public class LocalizedText
{
    [TextArea(2, 5)] public string tr;
    [TextArea(2, 5)] public string en;
    [TextArea(2, 5)] public string de;

    public string GetText(GameLanguage lang)
    {
        return lang switch
        {
            GameLanguage.English => string.IsNullOrEmpty(en) ? tr : en,
            GameLanguage.German => string.IsNullOrEmpty(de) ? tr : de,
            _ => tr
        };
    }
}

[System.Serializable]
public class LocalizedAudio
{
    public AudioClip tr;
    public AudioClip en;
    public AudioClip de;

    public AudioClip GetAudio(GameLanguage lang)
    {
        return lang switch
        {
            GameLanguage.English => en != null ? en : tr,
            GameLanguage.German => de != null ? de : tr,
            _ => tr
        };
    }
}