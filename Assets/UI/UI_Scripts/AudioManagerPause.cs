using System;
using UnityEngine;

/// <summary>
/// One shared sound setting for MainUI and gameplay pause menus.
/// IsMuted is saved immediately, so the choice survives scene changes and app restarts.
/// </summary>
public class AudioManagerPause : MonoBehaviour
{
    private const string SoundKey = "MusicState";
    private const string LegacyMuteKey = "AudioMuted";

    private static AudioManagerPause instance;
    private static bool initialized;
    private static bool isMuted;

    public static event Action OnMuteStateChanged;

    public static bool IsMuted
    {
        get
        {
            Initialize();
            return isMuted;
        }
        set
        {
            Initialize();

            if (isMuted == value)
            {
                ApplyVolume();
                SaveSettings();
                return;
            }

            isMuted = value;
            ApplyVolume();
            SaveSettings();
            OnMuteStateChanged?.Invoke();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        Initialize();

        if (instance != null)
            return;

        GameObject managerObject = new GameObject(nameof(AudioManagerPause));
        instance = managerObject.AddComponent<AudioManagerPause>();
        DontDestroyOnLoad(managerObject);
    }

    public static void Initialize()
    {
        if (!initialized)
        {
            // MusicState is the project's existing MainUI preference.
            // Migrate AudioMuted only for installations that have no MusicState yet.
            if (PlayerPrefs.HasKey(SoundKey))
                isMuted = PlayerPrefs.GetInt(SoundKey, 1) == 0;
            else if (PlayerPrefs.HasKey(LegacyMuteKey))
                isMuted = PlayerPrefs.GetInt(LegacyMuteKey, 0) == 1;
            else
                isMuted = false;

            initialized = true;
        }

        ApplyVolume();
    }

    private static void ApplyVolume()
    {
        AudioListener.volume = isMuted ? 0f : 1f;
    }

    public static void SaveSettings()
    {
        Initialize();
        PlayerPrefs.SetInt(SoundKey, isMuted ? 0 : 1);
        // Keep the legacy key synchronized in case another old script reads it.
        PlayerPrefs.SetInt(LegacyMuteKey, isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        Initialize();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            SaveSettings();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            SaveSettings();
    }

    private void OnApplicationQuit()
    {
        SaveSettings();
    }
}
