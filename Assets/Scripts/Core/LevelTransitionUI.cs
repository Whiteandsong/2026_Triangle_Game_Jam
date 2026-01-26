using UnityEngine;

[RequireComponent(typeof(Animator))]
public class LevelTransitionUI : MonoBehaviour
{
    private Animator animator;
    
    // 动画状态机中的 Trigger 参数名
    private const string EXIT_TRIGGER = "Transition";
    private const string ENTER_TRIGGER = "Start"; // 关卡开始（黑幕消失）

    [Header("Settings")]
    public float transitionDuration = 3f;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    // 播放离开当前关卡的动画（例如：黑幕淡入/幕布降下）
    public void PlayExitAnimation()
    {
        animator.SetTrigger(EXIT_TRIGGER);
    }

    // 播放进入新关卡的动画（例如：黑幕淡出/幕布升起）
    public void PlayEnterAnimation()
    {
        animator.SetTrigger(ENTER_TRIGGER);
    }
}