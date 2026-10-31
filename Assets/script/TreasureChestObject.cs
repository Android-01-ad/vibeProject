using UnityEngine;

namespace RoguelikeUnity
{
    // 탐험 카드로 열 수 있는 보물 상자입니다.
    public sealed class TreasureChestObject : MonoBehaviour
    {
        // 상자 열기 애니메이션 트리거 이름입니다.
        [SerializeField] private string openTrigger = "Open";

        // 상자 애니메이터입니다.
        [SerializeField] private Animator chestAnimator;

        // 오픈 시 재생할 이펙트입니다.
        [SerializeField] private ParticleSystem openEffect;

        // 탐험 카드 사용 시 보물 상자를 열고 사라지게 합니다.
        public bool TryUse(PlayerCharacter user, PlayerCharacter.CardUseType cardUseType)
        {
            if (user == null || cardUseType != PlayerCharacter.CardUseType.Exploration)
                return false;

            if (chestAnimator != null && !string.IsNullOrEmpty(openTrigger))
                chestAnimator.SetTrigger(openTrigger);

            if (openEffect != null)
                openEffect.Play();

            Destroy(gameObject, 0.8f);
            return true;
        }
    }
}
