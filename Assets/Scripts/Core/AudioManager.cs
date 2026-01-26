using UnityEngine;

public class AudioManager : Singleton<AudioManager>
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1.0f; // BGM切换时的淡入淡出时间
    
    private float globalBGMVolume = 1f;
    private float duckingMultiplier = 1f;
    private float previousDuckingMultiplier = 1f; // 追踪上一帧的ducking值

    private enum BGMState
    {
        Playing,  
        FadingOut,  
        FadingIn 
    }
    
    private BGMState currentBGMState = BGMState.Playing;
    private AudioClip nextClipToPlay = null; // 待播放的下一首音乐

    protected override void Awake()
    {
        base.Awake();
        LoadVolumeSettings();
    }

    private void Update()
    {
        HandleBGMFadeLogic();
    }

    // 在Update中每帧处理BGM的音量变化
    private void HandleBGMFadeLogic()
    {
        if (bgmSource == null) return;

        // 使用unscaledDeltaTime以支持暂停菜单中的音量调节
        float deltaTime = Time.unscaledDeltaTime;
        
        // 计算目标音量
        float targetBaseVolume = globalBGMVolume * duckingMultiplier;

        switch (currentBGMState)
        {
            case BGMState.Playing:
                // 只有当ducking倍率变化时才使用渐变（躲藏进出）
                // 用户通过slider调节globalBGMVolume时，直接应用，无渐变
                if (duckingMultiplier != previousDuckingMultiplier)
                {
                    // Ducking变化，使用渐变
                    bgmSource.volume = Mathf.MoveTowards(bgmSource.volume, targetBaseVolume, deltaTime / fadeDuration);
                    previousDuckingMultiplier = duckingMultiplier;
                }
                else
                {
                    // 只是globalBGMVolume变化，立即应用
                    bgmSource.volume = targetBaseVolume;
                }
                break;

            case BGMState.FadingOut:
                bgmSource.volume = Mathf.MoveTowards(bgmSource.volume, 0f, deltaTime / fadeDuration);
                
                if (Mathf.Approximately(bgmSource.volume, 0f))
                {
                    bgmSource.Stop();
                    bgmSource.clip = nextClipToPlay;
                    if (nextClipToPlay != null)
                    {
                        bgmSource.Play();
                        currentBGMState = BGMState.FadingIn;
                    }
                    else
                    {
                        currentBGMState = BGMState.Playing; 
                    }
                }
                break;

            case BGMState.FadingIn:
                bgmSource.volume = Mathf.MoveTowards(bgmSource.volume, targetBaseVolume, deltaTime / fadeDuration);

                if (Mathf.Approximately(bgmSource.volume, targetBaseVolume))
                {
                    currentBGMState = BGMState.Playing;
                }
                break;
        }
    }

    // 公共功能

    // 切换背景音乐
    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource == null) return;

        // 如果传入的是同一个片段且正在播放，则不做任何事
        if (bgmSource.clip == clip && bgmSource.isPlaying && currentBGMState != BGMState.FadingOut) return;

        nextClipToPlay = clip;

        // 如果当前正在播放其他音乐，先进入淡出状态
        if (bgmSource.isPlaying)
        {
            currentBGMState = BGMState.FadingOut;
        }
        else
        {
            // 如果当前没音乐，直接播放并淡入
            bgmSource.clip = clip;
            bgmSource.volume = 0f; // 从0开始
            bgmSource.Play();
            currentBGMState = BGMState.FadingIn;
        }
    }

    // 临时降低BGM音量（躲藏时）
    public void FadeBGMToLower(float multiplier = 0.1f)
    {
        duckingMultiplier = multiplier;
    }

    // 恢复BGM音量
    public void FadeBGMToOriginal()
    {
        duckingMultiplier = 1f;
    }

    // 播放全局音效
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }

    // 在指定位置播放3D音效
    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;
        float finalVolume = volume * (sfxSource != null ? sfxSource.volume : 1f);
        AudioSource.PlayClipAtPoint(clip, position, finalVolume);
    }

    // 设置BGM全局音量
    public void SetBGMVolume(float volume)
    {
        if (bgmSource == null) return;
        
        globalBGMVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("BGMVolume", globalBGMVolume);
        // Update会在下一帧立即应用，无需手动设置
    }

    // 设置音效全局音量
    public void SetSFXVolume(float volume)
    {
        if (sfxSource == null) return;
        
        volume = Mathf.Clamp01(volume);
        sfxSource.volume = volume;
        PlayerPrefs.SetFloat("SFXVolume", volume);
    }

    public float GetBGMVolume()
    {
        return globalBGMVolume;
    }

    private void LoadVolumeSettings()
    {
        // 加载 BGM 设置
        globalBGMVolume = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
        
        // 加载 SFX 设置
        if (sfxSource != null)
        {
            sfxSource.volume = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
        }

        // 初始状态下直接应用音量，防止刚开始游戏时BGM还没声音
        if (bgmSource != null)
        {
            bgmSource.volume = globalBGMVolume;
        }
    }
}