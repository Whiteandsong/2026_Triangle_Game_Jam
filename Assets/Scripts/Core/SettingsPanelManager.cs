using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelManager : MonoBehaviour
{
    [Header("Volume Sliders")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    private void OnEnable()
    {
        // 面板打开时加载当前音量设置
        LoadCurrentVolume();
    }

    private void Start()
    {
        // 添加滑块事件监听
        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }

        LoadCurrentVolume();
    }

    // BGM音量改变
    private void OnBGMVolumeChanged(float value)
    {
        AudioManager.Instance.SetBGMVolume(value);
    }

    // 音效音量改变
    private void OnSFXVolumeChanged(float value)
    {
        AudioManager.Instance.SetSFXVolume(value);
    }

    // 加载当前音量设置
    private void LoadCurrentVolume()
    {
        float bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.5f);

        if (bgmSlider != null)
        {
            bgmSlider.value = bgmVolume;
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = sfxVolume;
        }
    }
}
