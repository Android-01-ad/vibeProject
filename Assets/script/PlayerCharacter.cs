using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoguelikeUnity
{
    // 플레이어 캐릭터의 상태와 전투 행동을 관리하는 컴포넌트입니다.
    // 적과의 전투, 체력 회복, 공격력 증가, 직업 선택과 전직 로직이 여기에 포함됩니다.
    public sealed class PlayerCharacter : MonoBehaviour
    {
        public enum ClassType
        {
            Warrior,
            Mage
        }

        public enum ClassRank
        {
            Novice,
            Veteran,
            Master
        }

        // 플레이어의 기본 최대 체력입니다. 최소 1 이상이어야 합니다.
        [SerializeField, Min(1)] private int maxHp = 30;

        // 플레이어의 기본 공격력입니다. 0 이상이어야 합니다.
        [SerializeField, Min(0)] private int attackPower = 6;

        // 현재 직업 종류입니다. 시작 시 검사 또는 마법사 중 하나를 선택합니다.
        private ClassType classType = ClassType.Warrior;

        // 현재 직업 단계입니다. 초보, 기사/마도사, 대장군/대마법사까지 올라갈 수 있습니다.
        private ClassRank classRank = ClassRank.Novice;

        // 현재 직업 이름입니다. UI 출력용입니다.
        private string currentClassName = "검사";

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

        // 현재 전직 단계 여부를 확인합니다.
        public ClassRank CurrentClassRank => classRank;

        // 현재 직업 이름입니다.
        public string CurrentClassName => currentClassName;

        // 컴포넌트가 활성화될 때 체력을 초기값으로 설정합니다.
        private void Awake() => Hp = maxHp;

        // 게임 시작 시 직업을 지정합니다.
        public void SetClass(ClassType selectedClass)
        {
            classType = selectedClass;
            classRank = ClassRank.Novice;
            ApplyClassStats();
            Hp = maxHp;
        }

        // UI에서 0=검사, 1=마법사처럼 인덱스로 직업을 선택할 때 사용합니다.
        public void SetClass(int selectedIndex)
        {
            SetClass(selectedIndex == 1 ? ClassType.Mage : ClassType.Warrior);
        }

        // 덱의 조합을 검사해서 전직 조건을 만족하면 직업을 강화합니다.
        public void TryPromoteFromDeck(IReadOnlyList<CardDefinition> deck)
        {
            if (deck == null || deck.Count == 0) return;

            var promoted = false;

            switch (classType)
            {
                case ClassType.Warrior:
                    if (deck.Count >= 5 && deck.Any(card => ContainsAny(card.Name, "검", "칼", "검사", "강타", "방어")))
                    {
                        classRank = ClassRank.Veteran;
                        promoted = true;
                    }

                    if (deck.Count >= 8 && deck.Any(card => ContainsAny(card.Name, "방패", "대검", "철벽", "강화")))
                    {
                        classRank = ClassRank.Master;
                        promoted = true;
                    }
                    break;

                case ClassType.Mage:
                    if (deck.Count >= 5 && deck.Any(card => ContainsAny(card.Name, "마법", "파이어", "아이스", "번개", "비전", "소환")))
                    {
                        classRank = ClassRank.Veteran;
                        promoted = true;
                    }

                    if (deck.Count >= 8 && deck.Any(card => ContainsAny(card.Name, "소환", "궁극", "폭풍", "얼음", "화염", "수호")))
                    {
                        classRank = ClassRank.Master;
                        promoted = true;
                    }
                    break;
            }

            if (promoted)
            {
                ApplyClassStats();
            }
        }

        // 문자열에 포함된 키워드가 하나라도 있는지 검사합니다.
        private bool ContainsAny(string source, params string[] keywords)
        {
            if (string.IsNullOrEmpty(source)) return false;
            var normalized = source.ToLower();
            return keywords.Any(keyword => !string.IsNullOrEmpty(keyword) && normalized.Contains(keyword.ToLower()));
        }

        // 직업과 전직 단계에 따라 체력, 공격력을 다시 계산합니다.
        private void ApplyClassStats()
        {
            switch (classType)
            {
                case ClassType.Warrior:
                    switch (classRank)
                    {
                        case ClassRank.Novice:
                            maxHp = 30;
                            attackPower = 6;
                            currentClassName = "검사";
                            break;
                        case ClassRank.Veteran:
                            maxHp = 40;
                            attackPower = 9;
                            currentClassName = "기사";
                            break;
                        case ClassRank.Master:
                            maxHp = 55;
                            attackPower = 13;
                            currentClassName = "대장군";
                            break;
                    }
                    break;

                case ClassType.Mage:
                    switch (classRank)
                    {
                        case ClassRank.Novice:
                            maxHp = 22;
                            attackPower = 8;
                            currentClassName = "마법사";
                            break;
                        case ClassRank.Veteran:
                            maxHp = 30;
                            attackPower = 12;
                            currentClassName = "마도사";
                            break;
                        case ClassRank.Master:
                            maxHp = 40;
                            attackPower = 18;
                            currentClassName = "대마법사";
                            break;
                    }
                    break;
            }

            Hp = Mathf.Clamp(Hp, 0, maxHp);
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
