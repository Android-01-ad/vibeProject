using UnityEngine;

namespace RoguelikeUnity
{
    // 플레이어의 직업/전직 단계에 따라 스프라이트를 전환하는 컨트롤러입니다.
    public sealed class PlayerClassSpriteController : MonoBehaviour
    {
        public enum SpriteRank
        {
            Novice,
            Veteran,
            Master
        }

        // 스프라이트를 적용할 렌더러입니다.
        [SerializeField] private SpriteRenderer spriteRenderer;

        // 검사 직업군 스프라이트입니다.
        [Header("Warrior Sprites")]
        [SerializeField] private Sprite warriorNoviceSprite;
        [SerializeField] private Sprite warriorVeteranSprite;
        [SerializeField] private Sprite warriorMasterSprite;

        // 마법사 직업군 스프라이트입니다.
        [Header("Mage Sprites")]
        [SerializeField] private Sprite mageNoviceSprite;
        [SerializeField] private Sprite mageVeteranSprite;
        [SerializeField] private Sprite mageMasterSprite;

        // 스프라이트 렌더러를 자동으로 찾습니다.
        private void Reset()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // 직업에 맞는 스프라이트를 적용합니다.
        public void Apply(PlayerCharacter.ClassType classType, SpriteRank spriteRank)
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            var sprite = GetSprite(classType, spriteRank);
            if (spriteRenderer != null)
                spriteRenderer.sprite = sprite;
        }

        private Sprite GetSprite(PlayerCharacter.ClassType classType, SpriteRank spriteRank)
        {
            switch (classType)
            {
                case PlayerCharacter.ClassType.Warrior:
                    switch (spriteRank)
                    {
                        case SpriteRank.Veteran:
                            return warriorVeteranSprite != null ? warriorVeteranSprite : warriorNoviceSprite;
                        case SpriteRank.Master:
                            return warriorMasterSprite != null ? warriorMasterSprite : warriorNoviceSprite;
                        default:
                            return warriorNoviceSprite;
                    }

                case PlayerCharacter.ClassType.Mage:
                    switch (spriteRank)
                    {
                        case SpriteRank.Veteran:
                            return mageVeteranSprite != null ? mageVeteranSprite : mageNoviceSprite;
                        case SpriteRank.Master:
                            return mageMasterSprite != null ? mageMasterSprite : mageNoviceSprite;
                        default:
                            return mageNoviceSprite;
                    }

                default:
                    return warriorNoviceSprite;
            }
        }
    }
}
