using UnityEngine;

public class SoundManager : MonoBehaviour
{
    // Singleton instance
    public static SoundManager Instance { get; private set; }

    [Header("Background Music")]
    [SerializeField] private AudioClip backgroundMusic;

    [Header("UI Sounds")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip menuOpenSound;

    [Header("Game Sounds")]
    [SerializeField] private AudioClip playerRotateSound;

    // Audio sources
    private AudioSource musicSource;
    private AudioSource sfxSource;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Set up audio sources
            SetupAudioSources();

            // Start playing background music
            PlayBackgroundMusic();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetupAudioSources()
    {
        // Create separate audio sources for music and sound effects
        AudioSource[] sources = GetComponents<AudioSource>();

        if (sources.Length == 0)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
        else if (sources.Length == 1)
        {
            musicSource = sources[0];
            sfxSource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            musicSource = sources[0];
            sfxSource = sources[1];
        }

        // Configure music source
        musicSource.loop = true;
        musicSource.volume = 0.5f;

        // Configure sfx source
        sfxSource.loop = false;
        sfxSource.volume = 0.2f;
    }

    private void PlayBackgroundMusic()
    {
        if (backgroundMusic != null && musicSource != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
    }

    // Public methods to play different sound effects

    public void PlayButtonClick()
    {
        if (buttonClickSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(buttonClickSound);
        }
    }

    public void PlayMenuOpen()
    {
        if (menuOpenSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(menuOpenSound);
        }
    }

    public void PlayPlayerRotate()
    {
        if (playerRotateSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(playerRotateSound);
        }
    }

    // Method to play any sound clip
    public void PlaySound(AudioClip clip, float volume = 1.0f)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
}