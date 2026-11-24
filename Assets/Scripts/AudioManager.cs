using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource voiceSource;

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
    [SerializeField] private AudioClip musicPeaceLoopClip;
    [SerializeField] private AudioClip bossIntroClip;
    [SerializeField] private AudioClip bossRoarClip;
    [SerializeField] private AudioClip bossTauntClip;
    [SerializeField] private AudioClip bossStepClip;
    [SerializeField] private AudioClip bossChargeClip;

    private bool isMuted = false;
    private float masterVolume = 1f;
    private float startupSilenceUntil = 0f;
    private float musicDuckMultiplier = 1f;
    private float musicFadeMultiplier = 1f;
    private bool isPeaceMusicActive = false;
    private Coroutine musicDuckRoutine;
    private Coroutine musicSwitchRoutine;

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
        if (voiceSource == null) voiceSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.ignoreListenerPause = true;
        musicSource.priority = 10;

        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.ignoreListenerPause = true;
        sfxSource.priority = 32;

        // Dedicated top-priority voice source for maximum loudness & clarity on "FINAL BOSS, FIGHT!!"
        voiceSource.loop = false;
        voiceSource.playOnAwake = false;
        voiceSource.spatialBlend = 0f;
        voiceSource.ignoreListenerPause = true;
        voiceSource.priority = 0;
    }

    private void Start()
    {
        TryStartMusic();
    }

    private void Update()
    {
        AudioClip desiredMusic = (isPeaceMusicActive && musicPeaceLoopClip != null) ? musicPeaceLoopClip : musicLoopClip;
        if (desiredMusic != null && !isMuted && !musicSource.isPlaying && musicSwitchRoutine == null)
        {
            TryStartMusic();
        }
        if (musicSource != null && musicSource.isPlaying)
        {
            float baseMusicVol = isPeaceMusicActive ? 0.68f : 0.62f;
            musicSource.volume = baseMusicVol * masterVolume * musicDuckMultiplier * musicFadeMultiplier;
        }
    }

    private void TryStartMusic()
    {
        AudioClip desiredMusic = (isPeaceMusicActive && musicPeaceLoopClip != null) ? musicPeaceLoopClip : musicLoopClip;
        if (desiredMusic == null || isMuted || musicSource.isPlaying) return;

        if (desiredMusic.loadState == AudioDataLoadState.Unloaded)
        {
            desiredMusic.LoadAudioData();
        }

        if (desiredMusic.loadState == AudioDataLoadState.Loaded)
        {
            musicSource.clip = desiredMusic;
            float baseMusicVol = isPeaceMusicActive ? 0.68f : 0.62f;
            musicSource.volume = baseMusicVol * masterVolume * musicDuckMultiplier * musicFadeMultiplier;
            musicSource.Play();
        }
    }

    public void SwitchToPeaceMusic()
    {
        if (isPeaceMusicActive || musicPeaceLoopClip == null) return;
        isPeaceMusicActive = true;
        if (musicSwitchRoutine != null) StopCoroutine(musicSwitchRoutine);
        musicSwitchRoutine = StartCoroutine(CrossfadeToPeaceRoutine());
    }

    private IEnumerator CrossfadeToPeaceRoutine()
    {
        // Smoothly fade out apocalyptic destruction music
        float fadeOut = 0.75f;
        float elapsed = 0f;
        while (elapsed < fadeOut)
        {
            musicFadeMultiplier = Mathf.Lerp(1f, 0f, elapsed / fadeOut);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        musicFadeMultiplier = 0f;
        musicSource.Stop();

        if (musicPeaceLoopClip.loadState == AudioDataLoadState.Unloaded)
        {
            musicPeaceLoopClip.LoadAudioData();
        }
        musicSource.clip = musicPeaceLoopClip;
        musicSource.volume = 0f;
        if (!isMuted) musicSource.Play();

        // Warmly swell in the Peace & Freedom soundtrack
        float fadeIn = 1.25f;
        elapsed = 0f;
        while (elapsed < fadeIn)
        {
            musicFadeMultiplier = Mathf.Lerp(0f, 1f, elapsed / fadeIn);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        musicFadeMultiplier = 1f;
        musicSwitchRoutine = null;
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
        if (bossIntroClip == null || isMuted) return;
        // Deeply duck apocalyptic music so the ultra-loud, evil, zero-echo "FINAL BOSS, FIGHT!!" blasts through unmistakably!
        if (musicDuckRoutine != null) StopCoroutine(musicDuckRoutine);
        musicDuckRoutine = StartCoroutine(DuckMusicTemporarily(0.10f, 2.95f));
        voiceSource.pitch = 1.0f;
        voiceSource.volume = 1.0f;
        voiceSource.PlayOneShot(bossIntroClip, 1.0f * masterVolume);
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
        if (voiceSource != null) voiceSource.mute = isMuted;
    }

    public void SetVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        float baseMusicVol = isPeaceMusicActive ? 0.68f : 0.62f;
        musicSource.volume = baseMusicVol * masterVolume * musicDuckMultiplier * musicFadeMultiplier;
    }
}
