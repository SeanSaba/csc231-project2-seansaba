using System.Collections;
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
    [SerializeField] private AudioClip impactClip;
    [SerializeField] private AudioClip dashClip;
    [SerializeField] private AudioClip collectClip;
    [SerializeField] private AudioClip hurtClip;
    [SerializeField] private AudioClip checkpointClip;
    [SerializeField] private AudioClip winClip;
    [SerializeField] private AudioClip musicLoopClip;
    [SerializeField] private AudioClip bossIntroClip;
    [SerializeField] private AudioClip bossRoarClip;
    [SerializeField] private AudioClip bossTauntClip;
    [SerializeField] private AudioClip bossStepClip;
    [SerializeField] private AudioClip bossChargeClip;

    private bool isMuted = false;
    private float masterVolume = 1f;
    private float startupSilenceUntil = 0f;
    private float musicDuckMultiplier = 1f;
    private Coroutine musicDuckRoutine;

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
        // Suppress any stray initialization/spawn SFX right when the game opens!
        startupSilenceUntil = Time.unscaledTime + 0.45f;

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
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.volume = 0.62f * masterVolume * musicDuckMultiplier;
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
            musicSource.volume = 0.62f * masterVolume * musicDuckMultiplier;
            musicSource.Play();
        }
    }

    public void PlayJumpSound() => PlaySFX(jumpClip, 0.56f, Random.Range(0.96f, 1.04f));
    public void PlayLandSound() => PlaySFX(landClip, 0.54f, Random.Range(0.95f, 1.06f));
    public void PlayImpactSound() => PlaySFX(impactClip != null ? impactClip : landClip, 0.80f, Random.Range(0.92f, 1.08f));
    public void PlayDashSound() => PlaySFX(dashClip, 0.78f);
    public void PlayCollectSound() => PlaySFX(collectClip, 0.85f);
    public void PlayHurtSound() => PlaySFX(hurtClip, 0.90f);
    public void PlayCheckpointSound() => PlaySFX(checkpointClip, 0.85f);
    public void PlayWinSound() => PlaySFX(winClip, 1f);

    public void PlayBossIntroSound()
    {
        // Briefly duck background music so the deep scary "FINAL BOSS, FIGHT!!" voice rings out crystal clear!
        if (musicDuckRoutine != null) StopCoroutine(musicDuckRoutine);
        musicDuckRoutine = StartCoroutine(DuckMusicTemporarily(0.20f, 3.95f));
        PlaySFX(bossIntroClip, 1.0f, 1.0f);
    }

    public void PlayBossRoarSound() => PlaySFX(bossRoarClip, 0.96f, Random.Range(0.88f, 1.06f));
    public void PlayBossTauntSound() => PlaySFX(bossTauntClip, 0.82f, Random.Range(0.84f, 1.14f));
    public void PlayBossStepSound() => PlaySFX(bossStepClip != null ? bossStepClip : impactClip, 0.96f, Random.Range(0.92f, 1.04f));
    public void PlayBossChargeSound() => PlaySFX(bossChargeClip, 0.76f, Random.Range(0.96f, 1.06f));

    private IEnumerator DuckMusicTemporarily(float targetMult, float holdSeconds)
    {
        musicDuckMultiplier = targetMult;
        yield return new WaitForSecondsRealtime(holdSeconds);
        float elapsed = 0f;
        while (elapsed < 0.65f)
        {
            musicDuckMultiplier = Mathf.Lerp(targetMult, 1f, elapsed / 0.65f);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        musicDuckMultiplier = 1f;
        musicDuckRoutine = null;
    }

    private void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null || isMuted) return;
        if (Time.unscaledTime < startupSilenceUntil) return;
        if (UIManager.Instance != null && UIManager.Instance.IsTitleScreenVisible) return;
        sfxSource.pitch = pitch;
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
        musicSource.volume = 0.62f * masterVolume * musicDuckMultiplier;
    }
}
