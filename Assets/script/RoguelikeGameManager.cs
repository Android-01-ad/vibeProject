using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RoguelikeUnity
{
    // 게임 전체 흐름을 관리하는 메인 매니저입니다.
    // 스테이지 전환, 턴 제어, 보상 처리, 덱 관리자 연결, 개발자 모드 입력을 담당합니다.
    public sealed class RoguelikeGameManager : MonoBehaviour
    {
        public enum GameResult
        {
            None,
            Victory,
            Defeat
        }

        // 현재 스테이지의 분류입니다.
        // Battle: 일반 전투 구간
        // Reward: 카드 보상 선택 구간
        // Boss: 보스 전투 구간
        public enum StageType
        {
            Battle,
            Reward,
            Boss
        }

        // 씬에 배치된 참조 객체들을 연결하는 영역입니다.
        [Header("Scene references")]

        // 플레이어 캐릭터 참조입니다.
        [SerializeField] private PlayerCharacter player;

        // 현재 방의 적 목록입니다.
        [SerializeField] private List<EnemyCharacter> roomEnemies = new List<EnemyCharacter>();

        // 보스 캐릭터 참조입니다. 보스가 처치되면 승리 판정을 발생시킵니다.
        [SerializeField] private BossCharacter boss;

        // 일반 전투 스테이지에서 생성되는 적 프리팹 또는 기본 적 원본입니다.
        // 설정이 없으면 런타임에 스크립트로 하나를 자동 생성합니다.
        [SerializeField] private EnemyCharacter battleEnemyPrefab;

        // 덱 화면 UI를 제어하는 컨트롤러입니다.
        [SerializeField] private DeckScreenController deckScreen;

        // 하위 카드와 상위 카드 보관을 관리하는 덱 관리자입니다.
        private DeckManager deckManager;

        // 보상으로 제공될 수 있는 전체 카드 풀입니다.
        private readonly List<CardDefinition> cardPool = new List<CardDefinition>();

        // 현재 방을 클리어한 후 선택 가능한 보상 카드 목록입니다.
        private List<CardDefinition> currentRewards = new List<CardDefinition>();

        // 턴 흐름을 별도 관리자에서 처리합니다.
        private TurnManager turnManager;

        // 카드 보상이 아직 선택 대기 중인지 여부입니다.
        private bool rewardPending;

        // 현재 탐험 깊이(층 수)입니다.
        private int depth = 1;

        // 현재 스테이지 종류입니다.
        private StageType currentStage = StageType.Battle;

        // 현재 보유 카드 덱을 외부에서 읽을 수 있게 하는 프로퍼티입니다.
        public IReadOnlyList<CardDefinition> Deck => deckManager != null ? deckManager.Deck : Array.Empty<CardDefinition>();

        // 현재 드로우 덱을 외부에서 읽기 위한 프로퍼티입니다.
        public IReadOnlyList<CardDefinition> DrawDeck => deckManager != null ? deckManager.DrawDeck : Array.Empty<CardDefinition>();

        // 현재 버리기 덱을 외부에서 읽기 위한 프로퍼티입니다.
        public IReadOnlyList<CardDefinition> DiscardDeck => deckManager != null ? deckManager.DiscardDeck : Array.Empty<CardDefinition>();

        // 덱 관리자 인스턴스를 외부에서 직접 참조할 수 있게 한 프로퍼티입니다.
        public DeckManager DeckManagerInstance => deckManager;

        // 현재 보상 목록을 외부에서 읽을 수 있게 하는 프로퍼티입니다.
        public IReadOnlyList<CardDefinition> CurrentRewards => currentRewards;

        // 현재 깊이(층)를 외부에서 읽기 위한 프로퍼티입니다.
        public int Depth => depth;

        // 현재 스테이지 종류를 외부에서 읽기 위한 프로퍼티입니다.
        public StageType CurrentStage => currentStage;

        // 개발자 모드 활성 상태입니다.
        // 활성화되면 T + 9 입력으로 모드를 전환하고 + 키로 스테이지를 바로 넘길 수 있습니다.
        private bool developerMode;

        // 개발자 모드 상태를 외부에서 확인할 수 있습니다.
        public bool DeveloperModeEnabled => developerMode;

        // 현재 결과 상태입니다. None이면 게임이 계속 진행 중입니다.
        private GameResult currentResult = GameResult.None;

        // 현재 게임 결과를 외부에서 확인할 수 있습니다.
        public GameResult CurrentResult => currentResult;

        // 게임이 끝났는지 여부입니다.
        public bool IsGameEnded => currentResult != GameResult.None;

        // 현재 턴이 플레이어 턴인지 여부입니다.
        public bool IsPlayerTurn => turnManager != null && turnManager.IsPlayerTurn;

        // 카드 보상이 생성되었을 때 외부에 알리는 이벤트입니다.
        public event Action<IReadOnlyList<CardDefinition>> CardRewardOffered;

        // 게임결과가 바뀔 때 외부에서 구독할 수 있는 이벤트입니다.
        public event Action<GameResult> GameEnded;

        // 패배 판정이 발생했을 때 외부에서 구독할 수 있는 게임 오버 이벤트입니다.
        public event Action GameOvered;

        // 플레이어 직업을 선택합니다.
        // Warrior(검사) 또는 Mage(마법사) 중 하나를 선택합니다.
        public void SelectClass(PlayerCharacter.ClassType selectedClass)
        {
            if (player == null) return;

            player.SetClass(selectedClass);
            Debug.Log($"직업 선택: {player.CurrentClassName}");
        }

        // UI에서 0은 검사, 1은 마법사를 의미하는 인덱스로 선택합니다.
        public void SelectClass(int selectedIndex)
        {
            SelectClass(selectedIndex == 1 ? PlayerCharacter.ClassType.Mage : PlayerCharacter.ClassType.Warrior);
        }

        // 새로운 런을 시작할 때 플레이어 상태와 스테이지를 초기화합니다.
        public void InitializeRun(PlayerCharacter.ClassType selectedClass)
        {
            EnsureManagers();

            deckManager.Clear();
            currentRewards.Clear();
            rewardPending = false;
            depth = 1;
            currentStage = StageType.Battle;
            currentResult = GameResult.None;
            roomEnemies.Clear();
            turnManager.StartPlayerTurn();
            BindBoss();
            SpawnBattleEnemy();

            if (player != null)
            {
                player.SetClass(selectedClass);
                Debug.Log($"새 런 시작: {player.CurrentClassName}");
            }
        }

        // UI용 인덱스 기반 초기화 메서드입니다.
        public void InitializeRun(int selectedIndex)
        {
            InitializeRun(selectedIndex == 1 ? PlayerCharacter.ClassType.Mage : PlayerCharacter.ClassType.Warrior);
        }

        // 오브젝트가 처음 활성화될 때 덱 관리자와 턴 관리자, 보스 바인딩을 초기화합니다.
        private void Awake()
        {
            EnsureManagers();
            turnManager.StartPlayerTurn();

            if (player != null)
                player.SetClass(PlayerCharacter.ClassType.Warrior);

            BindBoss();
            StartCoroutine(LoadCardPool());
        }

        // 덱 관리자와 턴 관리자를 안전하게 초기화합니다.
        private void EnsureManagers()
        {
            if (deckManager == null)
                deckManager = new DeckManager();

            if (turnManager == null)
                turnManager = new TurnManager();
        }

        private void OnDestroy()
        {
            if (boss != null)
                boss.BossDefeated -= HandleBossDefeated;
        }

        private void BindBoss()
        {
            if (boss == null) return;
            boss.BossDefeated -= HandleBossDefeated;
            boss.BossDefeated += HandleBossDefeated;
        }

        private void HandleBossDefeated()
        {
            EndGame(GameResult.Victory);
        }

        private void EndGame(GameResult result)
        {
            if (currentResult != GameResult.None) return;

            currentResult = result;
            rewardPending = false;

            if (turnManager != null)
                turnManager.StartEnemyTurn();

            if (result == GameResult.Victory)
                Debug.Log("승리 판정: 보스를 처치했습니다.");
            else if (result == GameResult.Defeat)
            {
                Debug.Log("패배 판정: 플레이어가 전투에서 패배했습니다.");
                GameOvered?.Invoke();
            }

            GameEnded?.Invoke(result);
        }

        // 카드 풀을 로드합니다.
        // StreamingAssets/cards.csv를 읽어 보상 후보 목록을 구성합니다.
        private IEnumerator LoadCardPool()
        {
            cardPool.Clear();
            yield return CardCatalog.Load(cards => cardPool.AddRange(cards));

            if (cardPool.Count == 0)
                Debug.LogWarning("카드 후보가 없습니다. Assets/StreamingAssets/cards.csv를 확인하세요.");
        }

        // 일반 전투 스테이지에서 적 하나를 생성합니다.
        // 현재 구조는 한 번의 전투에 적 1마리만 등장하도록 제한합니다.
        private void SpawnBattleEnemy()
        {
            roomEnemies.Clear();

            if (currentStage != StageType.Battle)
                return;

            EnemyCharacter spawnedEnemy;
            if (battleEnemyPrefab != null)
            {
                spawnedEnemy = Instantiate(battleEnemyPrefab, transform);
            }
            else
            {
                var enemyObject = new GameObject("BattleEnemy");
                enemyObject.transform.SetParent(transform);
                spawnedEnemy = enemyObject.AddComponent<EnemyCharacter>();
            }

            spawnedEnemy.gameObject.SetActive(true);
            roomEnemies.Add(spawnedEnemy);
            Debug.Log($"전투 스테이지 생성: {spawnedEnemy.EnemyName}");
        }

        // 전투 클리어 여부를 확인하고, 결과에 따라 다음 스테이지 또는 게임 종료를 처리합니다.
        private void ResolveBattleOutcome()
        {
            if (IsGameEnded) return;

            bool allEnemiesDefeated = roomEnemies.Count == 0 || roomEnemies.All(enemy => enemy == null || !enemy.IsAlive);
            if (!allEnemiesDefeated) return;

            if (currentStage == StageType.Boss)
            {
                EndGame(GameResult.Victory);
                return;
            }

            OpenCardReward();
        }

        // 플레이어가 적을 공격할 때 호출됩니다.
        public void PlayerAttack(EnemyCharacter target)
        {
            if (IsGameEnded || !IsPlayerTurn || rewardPending || player == null || target == null) return;
            if (turnManager != null)
                turnManager.StartEnemyTurn();
            player.Attack(target);

            // 적이 모두 사망하면 보상 단계로 진행합니다.
            if (roomEnemies.All(enemy => enemy == null || !enemy.IsAlive))
            {
                ResolveBattleOutcome();
                return;
            }

            StartCoroutine(EnemyTurn());
        }

        // 플레이어가 턴을 넘길 때 호출됩니다.
        public void EndPlayerTurn()
        {
            if (IsGameEnded || !IsPlayerTurn || rewardPending || player == null || !player.IsAlive) return;
            if (turnManager != null)
                turnManager.StartEnemyTurn();
            StartCoroutine(EnemyTurn());
        }

        // 적의 턴을 진행합니다.
        // 약간의 딜레이 후 모든 살아 있는 적이 플레이어를 공격합니다.
        private IEnumerator EnemyTurn()
        {
            if (IsGameEnded) yield break;

            yield return new WaitForSeconds(0.25f);

            foreach (var enemy in roomEnemies)
            {
                if (enemy != null && enemy.IsAlive && player.IsAlive)
                    enemy.Attack(player);
            }

            // 플레이어가 죽으면 패배 판정을 내립니다.
            if (!player.IsAlive)
            {
                Debug.Log($"탐험 실패. 도달 층: {depth}");
                EndGame(GameResult.Defeat);
                yield break;
            }

            if (!IsGameEnded && turnManager != null)
                turnManager.StartPlayerTurn();
        }

        // 방을 클리어했을 때 랜덤 보상 카드를 선택 목록으로 제공합니다.
        private void OpenCardReward()
        {
            currentStage = StageType.Reward;

            // 카드 풀에서 무작위로 3장 이하를 고릅니다.
            currentRewards = cardPool.OrderBy(_ => Random.value).Take(Mathf.Min(3, cardPool.Count)).ToList();

            // 카드 풀이 비어 있으면 즉시 다음 층으로 이동합니다.
            if (currentRewards.Count == 0)
            {
                AdvanceDepth();
                return;
            }

            rewardPending = true;
            Debug.Log("카드 보상 후보: " + string.Join(", ", currentRewards.Select(card => card.Name)));
            CardRewardOffered?.Invoke(currentRewards);
        }

        // 보상 목록에서 카드를 선택했을 때 호출됩니다.
        // 선택한 카드는 보유 덱에 추가되고 다음 층으로 진행합니다.
        public void ChooseCardReward(int choiceIndex)
        {
            if (IsGameEnded || !rewardPending || choiceIndex < 0 || choiceIndex >= currentRewards.Count) return;

            var chosenCard = currentRewards[choiceIndex];
            if (deckManager != null)
                deckManager.AddCardToOwnedDeck(chosenCard);

            currentRewards.Clear();
            rewardPending = false;
            AdvanceDepth();
        }

        // 다음 층으로 이동합니다.
        // depth가 3의 배수마다 보스 스테이지가 생성되고, 나머지는 전투 스테이지로 유지됩니다.
        private void AdvanceDepth()
        {
            if (IsGameEnded) return;

            depth++;
            currentStage = depth % 3 == 0 ? StageType.Boss : StageType.Battle;
            if (turnManager != null)
                turnManager.StartPlayerTurn();

            if (currentStage == StageType.Battle)
                SpawnBattleEnemy();

            Debug.Log($"스테이지 이동: {depth}층 / {currentStage}");
        }

        // 개발자 모드에서 스테이지를 한 번 건너뛰어 시연 속도를 높입니다.
        public void SkipStageForDemo()
        {
            if (!developerMode || IsGameEnded) return;

            if (rewardPending)
            {
                currentRewards.Clear();
                rewardPending = false;
            }

            AdvanceDepth();
            Debug.Log("개발자 모드: 플러스 키로 스테이지를 건너뜁니다.");
        }

        // 개발자 모드 토글과 스테이지 스킵 입력을 처리합니다.
        // T + 9를 누르면 개발자 모드가 토글되고, 개발자 모드가 활성화된 상태에서 + 키로 스테이지를 넘길 수 있습니다.
        private void Update()
        {
            if (IsDeveloperModeTogglePressed())
            {
                developerMode = !developerMode;
                Debug.Log($"개발자 모드 {(developerMode ? "활성화" : "비활성화")}");
                return;
            }

            if (!developerMode) return;

            if (IsStageSkipPressed())
                SkipStageForDemo();
        }

        // 개발자 모드 전환 입력인지 확인합니다.
        private static bool IsDeveloperModeTogglePressed()
        {
            bool tHeld = Input.GetKey(KeyCode.T);
            bool key9Pressed = Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9);

            return (tHeld && key9Pressed) || (Input.GetKeyDown(KeyCode.T) && Input.GetKey(KeyCode.Alpha9));
        }

        // 스테이지 건너뛰기 입력인지 확인합니다.
        private static bool IsStageSkipPressed()
        {
            return Input.GetKeyDown(KeyCode.Plus)
                || Input.GetKeyDown(KeyCode.KeypadPlus)
                || Input.GetKeyDown(KeyCode.Equals);
        }

        // 덱 화면을 열어 현재 보유/드로우/버리기 덱 상태를 표시합니다.
        public void ShowDeck()
        {
            if (deckScreen != null && deckManager != null) deckScreen.Show(deckManager.Deck);
            else if (deckManager != null) Debug.Log($"현재 보유 덱: {deckManager.Deck.Count}장 / 드로우 덱: {deckManager.DrawDeck.Count}장 / 버리기 덱: {deckManager.DiscardDeck.Count}장");
        }

        // 덱 화면을 닫습니다.
        public void HideDeck()
        {
            if (deckScreen != null) deckScreen.Hide();
        }

        // 휴식 방에서 플레이어 체력을 회복시킵니다.
        // 기본값은 8이며, 다른 회복량을 넘겨서 상황에 맞게 조정할 수 있습니다.
        public void RestoreAtRestRoom(int amount = 8)
        {
            if (player != null) player.Heal(amount);
        }
    }
}
