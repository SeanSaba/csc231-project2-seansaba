using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Clips")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landClip;
    [SerializeField] private AudioClip dashClip;
    [SerializeField] private AudioClip collectClip;
    [SerializeField] private AudioClip hurtClip;
    [SerializeField] private AudioClip checkpointClip;
    [SerializeField] private AudioClip winClip;
    [SerializeField] private AudioClip musicLoopClip;

    private bool isMuted = false;
    private float masterVolume = 1f;

    public bool IsMuted => isMuted;
    public float MasterVolume => masterVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.ignoreListenerPause = true;
        musicSource.priority = 0;

        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.ignoreListenerPause = true;
    }

    private void Start()
    {
        TryStartMusic();
    }

    private void Update()
    {
        if (musicLoopClip != null && !isMuted && !musicSource.isPlaying)
        {
            TryStartMusic();
        }
    }

    private void TryStartMusic()
    {
        if (musicLoopClip == null || isMuted || musicSource.isPlaying) return;

        if (musicLoopClip.loadState == AudioDataLoadState.Unloaded)
        {
            musicLoopClip.LoadAudioData();
        }

        if (musicLoopClip.loadState == AudioDataLoadState.Loaded)
        {
            musicSource.clip = musicLoopClip;
            musicSource.volume = 0.65f * masterVolume;
            musicSource.Play();
        }
    }

    public void PlayJumpSound() => PlaySFX(jumpClip, 0.7f);
    public void PlayLandSound() => PlaySFX(landClip, 0.6f);
    public void PlayDashSound() => PlaySFX(dashClip, 0.8f);
    public void PlayCollectSound() => PlaySFX(collectClip, 0.85f);
    public void PlayHurtSound() => PlaySFX(hurtClip, 0.9f);
    public void PlayCheckpointSound() => PlaySFX(checkpointClip, 0.85f);
    public void PlayWinSound() => PlaySFX(winClip, 1f);

    private void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null || isMuted) return;
        sfxSource.PlayOneShot(clip, volume * masterVolume);
    }

    public void ToggleMute()
    {
        isMuted = !isMuted;
        musicSource.mute = isMuted;
        sfxSource.mute = isMuted;
    }

    public void SetVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        musicSource.volume = 0.5f * masterVolume;
    }
}
