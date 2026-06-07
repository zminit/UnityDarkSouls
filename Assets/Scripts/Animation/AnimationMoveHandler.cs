using UnityEngine;

public class AnimationMoveHandler : MonoBehaviour
{
    [SerializeField]
    Animator animator;

    /// <summary>
    /// Root motion is now owned by the character state machine and PlayerManager.
    /// This component is kept as a scene-compatible stub so old scene references do not become missing scripts.
    /// </summary>
    private void OnAnimatorMove()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }
}
