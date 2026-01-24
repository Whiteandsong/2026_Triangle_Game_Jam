using UnityEngine;

public class AudioManager : Singleton<AudioManager>
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    protected override void Awake()
    {
        base.Awake();

        // 从PlayerPrefs加载音量设置
        LoadVolumeSettings();
    }

    // 播放背景音乐
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource == null) return;

        bgmSource.clip = clip;
        bgmSource.Play();
    }

    // 播放全局音效（如UI按钮点击）
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }

    // 在指定位置播放3D音效（用于游戏世界中的交互）
    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position, volume);
    }

    // 设置BGM音量
    public void SetBGMVolume(float volume)
    {
        if (bgmSource == null) return;
        
        volume = Mathf.Clamp01(volume);
        bgmSource.volume = volume;
        PlayerPrefs.SetFloat("BGMVolume", volume);
    }

    // 设置音效音量
    public void SetSFXVolume(float volume)
    {
        if (sfxSource == null) return;
        
        volume = Mathf.Clamp01(volume);
        sfxSource.volume = volume;
        PlayerPrefs.SetFloat("SFXVolume", volume);
    }

    // 加载音量设置
    private void LoadVolumeSettings()
    {
        if (bgmSource != null)
        {
            bgmSource.volume = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
        }

        if (sfxSource != null)
        {
            sfxSource.volume = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
        }
    }
}
