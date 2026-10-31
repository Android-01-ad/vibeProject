using UnityEngine;

namespace RoguelikeUnity
{
    // 적 캐릭터의 상태와 행동을 관리하는 컴포넌트입니다.
    // 플레이어와의 전투에서 체력, 공격력, 공격 처리 등을 담당합니다.
    public sealed class EnemyCharacter : MonoBehaviour
    {
        // 적의 이름입니다. UI나 로그에 표시할 때 사용됩니다.
        [SerializeField] private string enemyName = "동굴 박쥐";

        // 적의 최대 체력입니다. 최소 1 이상이어야 합니다.
        [SerializeField, Min(1)] private int maxHp = 10;

        // 적의 기본 공격력입니다. 0 이상이어야 합니다.
        [SerializeField, Min(0)] private int attackPower = 3;

        // 현재 적의 이름을 외부에서 읽기 위한 프로퍼티입니다.
        public string EnemyName => enemyName;

        // 최대 체력 값을 외부에서 읽기 위한 프로퍼티입니다.
        public int MaxHp => maxHp;

        // 현재 남은 체력입니다. 게임 시작 시 maxHp로 초기화됩니다.
        public int Hp { get; private set; }

        // 현재 적의 공격력을 외부에서 읽기 위한 프로퍼티입니다.
        public int AttackPower => attackPower;

        // 적이 살아 있는지 여부를 확인합니다.
        public bool IsAlive => Hp > 0;

        // 컴포넌트가 활성화될 때 체력을 초기값으로 설정합니다.
        private void Awake() => Hp = maxHp;

        // 적에게 데미지를 입힙니다.
        // amount가 음수이거나 0보다 작으면 0으로 보정합니다.
        public void TakeDamage(int amount)
        {
            // 이미 죽은 적은 더 이상 체력을 깎지 않습니다.
            if (!IsAlive) return;

            Hp = Mathf.Max(0, Hp - Mathf.Max(0, amount));

            // 체력이 0이 되면 적 오브젝트를 비활성화합니다.
            if (!IsAlive) gameObject.SetActive(false);
        }

        // 플레이어를 대상으로 공격합니다.
        // 적이 살아 있고, 대상이 존재하며, 대상이 살아 있을 때만 공격합니다.
        public void Attack(PlayerCharacter target)
        {
            if (!IsAlive || target == null || !target.IsAlive) return;
            target.TakeDamage(attackPower);
        }
    }
}
