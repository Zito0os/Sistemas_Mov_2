using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum SoundType
{
    Dinero,
    BubblePop,
    MusicaAmbiente,
}

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioClip[] soundList;
    private static SoundManager instance;
    private AudioSource audioSource;
    private AudioSource musicSource;
    private float buttonScanTimer;
    private const float buttonScanInterval = 0.5f;

    private void Awake()
    {
        instance = this;
        audioSource = GetComponent<AudioSource>();

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;

        ScanAndBindButtons();
    }

    private void Start()
    {
    }

    private void Update()
    {
        buttonScanTimer += Time.unscaledDeltaTime;
        if (buttonScanTimer < buttonScanInterval)
            return;

        buttonScanTimer = 0f;
        ScanAndBindButtons();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ScanAndBindButtons();
    }

    public static void PlaySound(SoundType sound, float volume = 1)
    {
        if (instance == null || instance.audioSource == null)
            return;

        if (sound == SoundType.MusicaAmbiente)
        {
            PlayMusicLoop(volume);
            return;
        }

        instance.audioSource.PlayOneShot(instance.soundList[(int)sound], volume);
    }

    public static void PlayMusicLoop(float volume = 1f)
    {
        if (instance == null || instance.musicSource == null || instance.soundList == null)
            return;

        AudioClip clip = instance.soundList[(int)SoundType.MusicaAmbiente];

        if (instance.musicSource.clip == clip && instance.musicSource.isPlaying)
            return;

        instance.musicSource.Stop();
        instance.musicSource.clip = clip;
        instance.musicSource.volume = volume;
        instance.musicSource.loop = true;
        instance.musicSource.Play();
    }

    public static void StopMusic()
    {
        if (instance == null || instance.musicSource == null)
            return;

        instance.musicSource.Stop();
    }

    private static void ScanAndBindButtons()
    {
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Button button in buttons)
        {
            if (button == null || !button.gameObject.scene.IsValid())
                continue;

            if (button.GetComponent<UIButtonBubblePop>() == null)
                button.gameObject.AddComponent<UIButtonBubblePop>();
        }
    }
}
