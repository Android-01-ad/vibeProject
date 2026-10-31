using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoguelikeUnity
{
    // 플레이어 캐릭터의 상태와 기본 행동을 관리하는 컴포넌트입니다.
    // 체력, 공격력, 직업 전환, 카드 사용 시 필드 오브젝트 상호작용 등을 담당합니다.
    public sealed class PlayerCharacter : MonoBehaviour
    {
        public enum ClassType
        {
            Warrior,
            Mage
        }

        public enum CardUseType
        {
            Combat,
            Exploration
        }

        // 플레이어의 기본 최대 체력입니다. 최소 1 이상이어야 합니다.
        [SerializeField, Min(1)] private int maxHp = 30;

        // 플레이어의 기본 공격력입니다. 0 이상이어야 합니다.
        [SerializeField, Min(0)] private int attackPower = 6;

        // 현재 직업 종류입니다. 시작 시 검사 또는 마법사 중 하나를 선택합니다.
        private ClassType classType = ClassType.Warrior;

        // 현재 직업 이름입니다. UI 출력용입니다.
        private string currentClassName = "검사";

        // 플레이어의 직업별 스프라이트를 관리하는 컨트롤러입니다.
        private PlayerClassSpriteController classSpriteController;

        // 최대 체력을 외부에서 읽기 위한 프로퍼티입니다.
        public int MaxHp => maxHp;

        // 현재 남은 체력입니다. 게임 시작 시 maxHp로 초기화됩니다.
        public int Hp { get; private set; }

        // 현재 공격력을 외부에서 읽기 위한 프로퍼티입니다.
        public int AttackPower => attackPower;

        // 플레이어가 살아 있는지 여부를 확인합니다.
        public bool IsAlive => Hp > 0;

        // 현재 직업 종류를 외부에서 확인할 수 있습니다.
        public ClassType CurrentClassType => classType;

        // 현재 직업 이름입니다.
        public string CurrentClassName => currentClassName;

        // 컴포넌트가 활성화될 때 체력을 초기값으로 설정합니다.
        private void Awake()
        {
            Hp = maxHp;
            classSpriteController = GetComponent<PlayerClassSpriteController>();
            if (classSpriteController == null)
                classSpriteController = GetComponentInChildren<PlayerClassSpriteController>();

            ApplyClassStats();
        }

        // 직업을 지정합니다.
        // 검사는 Warrior, 마법사는 Mage로 분류되며 전환 시 체력과 공격력, 스프라이트가 즉시 갱신됩니다.
        public void SetClass(ClassType selectedClass)
        {
            classType = selectedClass;
            ApplyClassStats();
            Hp = maxHp;
        }

        // 현재 직업을 검사와 마법사 사이에서 토글합니다.
        // 단순 전직/강등 로직이 아니라 클래스 교체용 메서드입니다.
        public void ToggleClass()
        {
            SetClass(classType == ClassType.Warrior ? ClassType.Mage : ClassType.Warrior);
        }

        // 장비/도구 영역의 상위 카드 조합을 검사해 직업을 결정합니다.
        // 검사 계열 카드가 우세하면 Warrior, 마법 계열 카드가 우세하면 Mage로 전환됩니다.
        public void UpdateClassByUpperCards(IEnumerable<CardDefinition> equipmentCards, IEnumerable<CardDefinition> toolCards)
        {
            if (equipmentCards == null && toolCards == null)
                return;

            var equipment = equipmentCards ?? Enumerable.Empty<CardDefinition>();
            var tools = toolCards ?? Enumerable.Empty<CardDefinition>();

            var warriorCondition = equipment.Any(IsWarriorClassCard) || tools.Any(IsWarriorClassCard);
            var mageCondition = equipment.Any(IsMageClassCard) || tools.Any(IsMageClassCard);

            if (warriorCondition && !mageCondition)
            {
                SetClass(ClassType.Warrior);
                return;
            }

            if (mageCondition && !warriorCondition)
            {
                SetClass(ClassType.Mage);
            }
        }

        // UI에서 0=검사, 1=마법사처럼 인덱스로 직업을 선택할 때 사용합니다.
        public void SetClass(int selectedIndex)
        {
            SetClass(selectedIndex == 1 ? ClassType.Mage : ClassType.Warrior);
        }

        private static bool IsWarriorClassCard(CardDefinition card)
        {
            if (card == null) return false;
            var combinedText = $"{card.Name} {card.Description}".ToLower();
            var warriorKeywords = new[] { "검", "검사", "기사", "방패", "강철", "대검", "칼", "무기", "전사" };
            return warriorKeywords.Any(keyword => combinedText.Contains(keyword));
        }

        private static bool IsMageClassCard(CardDefinition card)
        {
            if (card == null) return false;
            var combinedText = $"{card.Name} {card.Description}".ToLower();
            var mageKeywords = new[] { "마법", "마법사", "스태프", "지팡이", "봉인", "소환", "비전", "마도", "룬", "신성" };
            return mageKeywords.Any(keyword => combinedText.Contains(keyword));
        }

        // 직업에 따라 최대 체력, 공격력, 직업명과 스프라이트를 다시 계산합니다.
        private void ApplyClassStats()
        {
            switch (classType)
            {
                case ClassType.Warrior:
                    maxHp = 30;
                    attackPower = 6;
                    currentClassName = "검사";
                    break;

                case ClassType.Mage:
                    maxHp = 22;
                    attackPower = 8;
                    currentClassName = "마법사";
                    break;
            }

            Hp = Mathf.Clamp(Hp, 0, maxHp);

            if (classSpriteController != null)
                classSpriteController.Apply(classType, PlayerClassSpriteController.SpriteRank.Novice);
        }

        // 특정 필드 오브젝트를 카드 사용으로 처리합니다.
        // 전투 카드: 장애물을 파괴하고, 탐험 카드: 보물 상자를 열어 애니메이션 후 제거합니다.
        public void TryUseCardOnField(GameObject target, CardUseType cardUseType)
        {
            if (target == null) return;

            if (target.TryGetComponent(out ObstacleObject obstacle))
            {
                obstacle.TryUse(this, cardUseType);
                return;
            }

            if (target.TryGetComponent(out TreasureChestObject treasureChest))
            {
                treasureChest.TryUse(this, cardUseType);
            }
        }

        // 적을 공격합니다.
        // 플레이어가 살아 있고, 대상이 존재하며, 대상이 살아 있을 때만 공격합니다.
        public void Attack(EnemyCharacter target)
        {
            if (!IsAlive || target == null || !target.IsAlive) return;
            target.TakeDamage(attackPower);
        }

        // 플레이어에게 데미지를 입힙니다.
        // 음수 값은 0으로 보정되어 체력이 올라가지 않도록 합니다.
        public void TakeDamage(int amount)
        {
            Hp = Mathf.Max(0, Hp - Mathf.Max(0, amount));
        }

        // 체력을 회복합니다.
        // amount가 음수면 0으로 보정되고, 최대 체력 이상으로는 증가하지 않습니다.
        public void Heal(int amount)
        {
            Hp = Mathf.Min(maxHp, Hp + Mathf.Max(0, amount));
        }

        // 공격력을 증가시킵니다.
        // amount가 음수면 감소하지 않고 최소 0을 유지합니다.
        public void IncreaseAttack(int amount)
        {
            attackPower = Mathf.Max(0, attackPower + amount);
        }
    }
}
