using System.Collections;
using UnityEngine;

namespace RoguelikeUnity
{
    // 보스 캐릭터의 턴제 행동을 관리하는 클래스입니다.
    // 이동, 기본 공격, 궁극기 공격, 피격 처리까지 보스 전투 로직을 담당합니다.
    /// <summary>Turn-based boss movement, basic attack, and ultimate attack behavior.</summary>
    public sealed class BossCharacter : MonoBehaviour
    {
        // 보스의 기본 스탯을 설정하는 영역입니다.
        [Header("Stats")]

        // 보스 이름입니다. UI에서 표시하거나 로그에 사용할 수 있습니다.
        [SerializeField] private string bossName = "던전의 수호자";

        // 보스의 최대 체력입니다.
        [SerializeField, Min(1)] private int maxHp = 120;

        // 기본 공격으로 입히는 피해량입니다.
        [SerializeField, Min(0)] private int basicAttackDamage = 10;

        // 궁극기 공격으로 입히는 피해량입니다.
        [SerializeField, Min(0)] private int ultimateDamage = 22;

        // 몇 턴마다 궁극기를 사용할지 결정합니다.
        [SerializeField, Min(1)] private int ultimateEveryNTurns = 3;

        // 보스의 격자 이동 관련 값입니다.
        [Header("Grid movement")]

        // 보스의 현재 격자 위치입니다.
        [SerializeField] private Vector2Int gridPosition;

        // 한 칸 이동하는 데 걸리는 시간입니다.
        [SerializeField, Min(0.01f)] private float moveDuration = 0.2f;

        // 공격 전에 이동할지 여부를 결정합니다.
        [SerializeField] private bool moveBeforeAttack = true;

        // 공격/효과음/애니메이션 연결을 위한 참조입니다.
        [Header("Effects")]

        // 보스 애니메이터입니다.
        [SerializeField] private Animator animator;

        // 기본 공격 시 재생할 파티클 효과입니다.
        [SerializeField] private ParticleSystem basicAttackEffect;

        // 궁극기 사용 시 재생할 파티클 효과입니다.
        [SerializeField] private ParticleSystem ultimateEffect;

        // 효과음을 재생할 AudioSource입니다.
        [SerializeField] private AudioSource audioSource;

        // 기본 공격 사운드입니다.
        [SerializeField] private AudioClip basicAttackSound;

        // 궁극기 사운드입니다.
        [SerializeField] private AudioClip ultimateSound;

        // 현재 체력입니다.
        private int currentHp;

        // 보스가 몇 번째 턴을 진행했는지 세는 값입니다.
        private int bossTurn;

        // 현재 행동이 진행 중인지 여부입니다.
        private bool actionRunning;

        // 보스가 쓰러졌을 때 외부에서 알림을 받을 수 있는 이벤트입니다.
        public event System.Action BossDefeated;

        // 보스 이름을 외부에서 읽기 위한 프로퍼티입니다.
        public string BossName => bossName;

        // 현재 체력을 외부에서 읽기 위한 프로퍼티입니다.
        public int Hp => currentHp;

        // 최대 체력을 외부에서 읽기 위한 프로퍼티입니다.
        public int MaxHp => maxHp;

        // 보스가 살아 있는지 여부입니다.
        public bool IsAlive => currentHp > 0;

        // 보스의 현재 격자 위치입니다.
        public Vector2Int GridPosition => gridPosition;

        // 오브젝트가 활성화될 때 체력을 초기화합니다.
        private void Awake() => currentHp = maxHp;

        // 게임 매니저가 보스 턴을 시작할 때 호출합니다.
        // 플레이어 위치를 기준으로 이동과 공격을 결정합니다.
        /// <summary>Called by the game manager once per boss turn.</summary>
        public void TakeTurn(PlayerCharacter player, Vector2Int playerGridPosition, System.Action onComplete = null)
        {
            if (!IsAlive || actionRunning || player == null || !player.IsAlive) return;
            StartCoroutine(DoTurn(player, playerGridPosition, onComplete));
        }

        // 실제 보스의 턴 로직입니다.
        // 1) 이동 여부 확인
        // 2) 플레이어가 살아 있는지 확인
        // 3) 거리 계산
        // 4) 근접 시 공격, 멀면 이동만 수행
        private IEnumerator DoTurn(PlayerCharacter player, Vector2Int playerGridPosition, System.Action onComplete)
        {
            actionRunning = true;
            bossTurn++;

            if (moveBeforeAttack)
            {
                yield return MoveToward(playerGridPosition);
            }

            if (!IsAlive || !player.IsAlive)
            {
                actionRunning = false;
                onComplete?.Invoke();
                yield break;
            }

            var distance = Mathf.Abs(gridPosition.x - playerGridPosition.x) +
                           Mathf.Abs(gridPosition.y - playerGridPosition.y);

            if (distance <= 1)
            {
                if (bossTurn % ultimateEveryNTurns == 0)
                    UseUltimate(player);
                else
                    UseBasicAttack(player);
            }
            else
            {
                TriggerAnimation("Move");
            }

            actionRunning = false;
            onComplete?.Invoke();
        }

        // 플레이어를 향해 한 칸 이동합니다.
        // x축 거리와 y축 거리 중 더 큰 축을 기준으로 이동합니다.
        private IEnumerator MoveToward(Vector2Int target)
        {
            var delta = target - gridPosition;
            if (delta == Vector2Int.zero) yield break;

            // 더 큰 축 방향으로 한 칸씩 이동합니다.
            Vector2Int step;
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                step = new Vector2Int((int)Mathf.Sign(delta.x), 0);
            else
                step = new Vector2Int(0, (int)Mathf.Sign(delta.y));

            // 맵 경로 찾기나 충돌 체크는 별도 로직으로 보강해야 합니다.
            var destination = gridPosition + step;
            var startWorld = transform.position;
            var endWorld = startWorld + new Vector3(step.x, step.y, 0f);
            TriggerAnimation("Move");

            var elapsed = 0f;
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startWorld, endWorld, Mathf.Clamp01(elapsed / moveDuration));
                yield return null;
            }

            transform.position = endWorld;
            gridPosition = destination;
        }

        // 기본 공격을 수행합니다.
        // 애니메이션과 효과음, 파티클을 재생한 뒤 플레이어에게 피해를 줍니다.
        private void UseBasicAttack(PlayerCharacter player)
        {
            TriggerAnimation("BasicAttack");
            PlayEffect(basicAttackEffect, basicAttackSound);
            player.TakeDamage(basicAttackDamage);
        }

        // 궁극기 공격을 수행합니다.
        // 더 큰 피해를 입히고 강한 시각/음향 효과를 재생합니다.
        private void UseUltimate(PlayerCharacter player)
        {
            TriggerAnimation("Ultimate");
            PlayEffect(ultimateEffect, ultimateSound);
            player.TakeDamage(ultimateDamage);
        }

        // 보스에게 데미지를 입힙니다.
        // 체력이 0이 되면 죽는 애니메이션을 실행하고 코루틴을 정지합니다.
        public void TakeDamage(int amount)
        {
            if (!IsAlive) return;
            currentHp = Mathf.Max(0, currentHp - Mathf.Max(0, amount));
            if (currentHp == 0)
            {
                TriggerAnimation("Death");
                StopAllCoroutines();
                actionRunning = false;
                BossDefeated?.Invoke();
            }
        }

        // 애니메이터 트리거를 실행합니다.
        private void TriggerAnimation(string trigger)
        {
            if (animator != null) animator.SetTrigger(trigger);
        }

        // 효과 파티클과 사운드를 재생합니다.
        private void PlayEffect(ParticleSystem effect, AudioClip sound)
        {
            if (effect != null) effect.Play();
            if (audioSource != null && sound != null) audioSource.PlayOneShot(sound);
        }
    }
}
