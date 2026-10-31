using UnityEngine;

namespace RoguelikeUnity
{
    // 전투 카드로 파괴할 수 있는 필드 장애물입니다.
    public sealed class ObstacleObject : MonoBehaviour
    {
        // 파괴 애니메이션 트리거 이름입니다.
        [SerializeField] private string destroyTrigger = "Break";

        // 파괴 시 재생할 애니메이터입니다.
        [SerializeField] private Animator obstacleAnimator;

        // 파괴 효과 파티클입니다.
        [SerializeField] private ParticleSystem destroyEffect;

        // 카드 사용으로 해당 장애물을 처리합니다.
        // 전투 카드일 때만 파괴되고, 그 외에는 무시됩니다.
        public bool TryUse(PlayerCharacter user, PlayerCharacter.CardUseType cardUseType)
        {
            if (user == null || cardUseType != PlayerCharacter.CardUseType.Combat)
                return false;

            if (obstacleAnimator != null && !string.IsNullOrEmpty(destroyTrigger))
                obstacleAnimator.SetTrigger(destroyTrigger);

            if (destroyEffect != null)
                destroyEffect.Play();

            Destroy(gameObject, 0.25f);
            return true;
        }
    }
}
