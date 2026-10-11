using UnityEngine;
using System;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance;

    public GameLanguage currentLanguage = GameLanguage.Turkish;
    public GameLanguage currentSubtitleLanguage = GameLanguage.Turkish;

    public static event Action OnLanguageChanged;
    public static event Action OnSubtitleLanguageChanged;

    private const string KEY_LANGUAGE = "Settings_Language";
    private const string KEY_SUBTITLE_LANGUAGE = "Settings_SubtitleLanguage";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetLanguage(GameLanguage newLanguage)
    {
        currentLanguage = newLanguage;
        PlayerPrefs.SetInt(KEY_LANGUAGE, (int)currentLanguage);
        PlayerPrefs.Save();
        OnLanguageChanged?.Invoke();
    }

    public void SetSubtitleLanguage(GameLanguage newLanguage)
    {
        currentSubtitleLanguage = newLanguage;
        PlayerPrefs.SetInt(KEY_SUBTITLE_LANGUAGE, (int)currentSubtitleLanguage);
        PlayerPrefs.Save();
        OnSubtitleLanguageChanged?.Invoke();
    }

    public void NextLanguage()
    {
        int nextLang = ((int)currentLanguage + 1) % Enum.GetValues(typeof(GameLanguage)).Length;
        SetLanguage((GameLanguage)nextLang);
    }

    public void PreviousLanguage()
    {
        int totalLangs = Enum.GetValues(typeof(GameLanguage)).Length;
        int prevLang = ((int)currentLanguage - 1 + totalLangs) % totalLangs;
        SetLanguage((GameLanguage)prevLang);
    }

    public void NextSubtitleLanguage()
    {
        int nextLang = ((int)currentSubtitleLanguage + 1) % Enum.GetValues(typeof(GameLanguage)).Length;
        SetSubtitleLanguage((GameLanguage)nextLang);
    }

    public void PreviousSubtitleLanguage()
    {
        int totalLangs = Enum.GetValues(typeof(GameLanguage)).Length;
        int prevLang = ((int)currentSubtitleLanguage - 1 + totalLangs) % totalLangs;
        SetSubtitleLanguage((GameLanguage)prevLang);
    }

    private void LoadSettings()
    {
        currentLanguage = (GameLanguage)PlayerPrefs.GetInt(KEY_LANGUAGE, (int)GameLanguage.Turkish);
        currentSubtitleLanguage = (GameLanguage)PlayerPrefs.GetInt(KEY_SUBTITLE_LANGUAGE, (int)GameLanguage.Turkish);
    }
}