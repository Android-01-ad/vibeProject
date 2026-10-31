using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RoguelikeUnity
{
    // 게임 전체 흐름을 관리하는 매니저입니다.
    // 플레이어 턴, 적 턴, 보상 선택, 덱 관리, 층 이동 같은 로직을 담당합니다.
    public sealed class RoguelikeGameManager : MonoBehaviour
    {
        public enum GameResult
        {
            None,
            Victory,
            Defeat
        }

        // 현재 스테이지 유형입니다.
        // 전투 스테이지: 적을 쓰러뜨리는 전투 구간
        // 보상 스테이지: 카드 보상을 선택하는 구간
        // 보스 스테이지: 보스를 상대하는 특수 구간
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

        // 보스 캐릭터 참조입니다. 보스가 죽으면 승리 판정을 처리합니다.
        [SerializeField] private BossCharacter boss;

        // 덱 화면 UI를 제어하는 컨트롤러입니다.
        [SerializeField] private DeckScreenController deckScreen;

        // 카드 덱 관리 객체입니다.
        private DeckManager deckManager;

        // 카드 보상 후보가 될 수 있는 전체 카드 풀입니다.
        private readonly List<CardDefinition> cardPool = new List<CardDefinition>();

        // 현재 방 클리어 후 제공되는 보상 카드 목록입니다.
        private List<CardDefinition> currentRewards = new List<CardDefinition>();

        // 현재 턴이 플레이어 턴인지 여부입니다.
        private bool playerTurn = true;

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

        // 개발자 모드가 활성화되었는지 여부입니다.
        private bool developerMode;

        // 개발자 모드 상태를 외부에서 확인할 수 있습니다.
        public bool DeveloperModeEnabled => developerMode;

        // 현재 결과 상태입니다. None이면 게임이 계속 진행 중입니다.
        private GameResult currentResult = GameResult.None;

        // 현재 게임 결과를 외부에서 확인할 수 있습니다.
        public GameResult CurrentResult => currentResult;

        // 게임이 끝났는지 여부입니다.
        public bool IsGameEnded => currentResult != GameResult.None;

        // 카드 보상이 생성되었을 때 외부에 알리는 이벤트입니다.
        public event Action<IReadOnlyList<CardDefinition>> CardRewardOffered;

        // 게임결과가 바뀔 때 외부에서 구독할 수 있는 이벤트입니다.
        public event Action<GameResult> GameEnded;

        // 게임 시작 시 플레이어의 직업을 선택합니다.
        // 검사(Warrior) 또는 마법사(Mage)를 선택할 수 있습니다.
        public void SelectClass(PlayerCharacter.ClassType selectedClass)
        {
            if (player == null) return;
            player.SetClass(selectedClass);
            Debug.Log($"직업 선택: {player.CurrentClassName}");
        }

        // UI에서 0=검사, 1=마법사처럼 인덱스로 선택할 때 사용합니다.
        public void SelectClass(int selectedIndex)
        {
            if (selectedIndex == 1)
                SelectClass(PlayerCharacter.ClassType.Mage);
            else
                SelectClass(PlayerCharacter.ClassType.Warrior);
        }

        // 전투 시작 전에 플레이어 직업을 초기화하고 상태를 리셋합니다.
        public void InitializeRun(PlayerCharacter.ClassType selectedClass)
        {
            if (deckManager == null)
                deckManager = new DeckManager();

            deckManager.Clear();
            currentRewards.Clear();
            rewardPending = false;
            depth = 1;
            currentStage = StageType.Battle;
            playerTurn = true;
            currentResult = GameResult.None;
            BindBoss();

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

        // 게임 오브젝트가 처음 활성화될 때 초기 상태를 설정합니다.
        private void Awake()
        {
            deckManager = new DeckManager();

            if (player != null)
                player.SetClass(PlayerCharacter.ClassType.Warrior);

            BindBoss();
            StartCoroutine(LoadCardPool());
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
            playerTurn = false;
            rewardPending = false;

            if (result == GameResult.Victory)
                Debug.Log("승리 판정: 보스를 처치했습니다.");
            else if (result == GameResult.Defeat)
                Debug.Log("패배 판정: 플레이어가 전투에서 패배했습니다.");

            GameEnded?.Invoke(result);
        }

        // 카드 풀을 로드합니다.
        // StreamingAssets/cards.csv를 읽어 전역 카드 목록을 구성합니다.
        private IEnumerator LoadCardPool()
        {
            yield return CardCatalog.Load(cards => cardPool.AddRange(cards));
            if (cardPool.Count == 0)
                Debug.LogWarning("카드 후보가 없습니다. Assets/StreamingAssets/cards.csv를 확인하세요.");
        }

        // 플레이어가 적을 공격할 때 호출됩니다.
        public void PlayerAttack(EnemyCharacter target)
        {
            if (IsGameEnded || !playerTurn || rewardPending || player == null || target == null) return;
            playerTurn = false;
            player.Attack(target);

            // 모든 적이 사망하면 보상을 열고 턴 종료 처리로 넘어갑니다.
            if (roomEnemies.All(enemy => enemy == null || !enemy.IsAlive))
            {
                OpenCardReward();
                return;
            }

            StartCoroutine(EnemyTurn());
        }

        // 플레이어가 턴을 넘길 때 호출됩니다.
        public void EndPlayerTurn()
        {
            if (IsGameEnded || !playerTurn || rewardPending || player == null || !player.IsAlive) return;
            playerTurn = false;
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

            if (!IsGameEnded)
                playerTurn = true;
        }

        // 방을 클리어했을 때 보상 카드를 랜덤으로 선택해 제공합니다.
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

        // 보상 카드 중 하나를 선택했을 때 호출됩니다.
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

        // 다음 층으로 이동하는 처리입니다.
        // 3층마다 보스 스테이지가 등장하고, 그 외에는 전투 스테이지를 유지합니다.
        private void AdvanceDepth()
        {
            if (IsGameEnded) return;

            depth++;
            currentStage = depth % 3 == 0 ? StageType.Boss : StageType.Battle;
            playerTurn = true;

            Debug.Log($"스테이지 이동: {depth}층 / {currentStage}");
            // Connect room generation here, then assign the new room's enemies to roomEnemies.
        }

        // 개발자 모드에서 스테이지를 한 번 건너뛰는 메서드입니다.
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

        // 개발자 모드 및 스테이지 건너뛰기 입력을 처리합니다.
        private void Update()
        {
            bool toggleDeveloperMode = (Input.GetKey(KeyCode.T) && (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9)))
                || (Input.GetKeyDown(KeyCode.T) && Input.GetKey(KeyCode.Alpha9));

            if (toggleDeveloperMode)
            {
                developerMode = !developerMode;
                Debug.Log($"개발자 모드 {(developerMode ? "활성화" : "비활성화")}");
                return;
            }

            if (!developerMode) return;

            bool plusPressed = Input.GetKeyDown(KeyCode.Plus)
                || Input.GetKeyDown(KeyCode.KeypadPlus)
                || Input.GetKeyDown(KeyCode.Equals);

            if (plusPressed)
                SkipStageForDemo();
        }

        // 덱 화면을 표시합니다.
        public void ShowDeck()
        {
            if (deckScreen != null && deckManager != null) deckScreen.Show(deckManager.Deck);
            else if (deckManager != null) Debug.Log($"현재 보유 덱: {deckManager.Deck.Count}장 / 드로우 덱: {deckManager.DrawDeck.Count}장 / 버리기 덱: {deckManager.DiscardDeck.Count}장");
        }

        // 덱 화면을 숨깁니다.
        public void HideDeck()
        {
            if (deckScreen != null) deckScreen.Hide();
        }

        // 휴식 방에서 체력을 회복시켜 줍니다.
        // 기본값은 8이며 필요시 다른 회복량으로 조정할 수 있습니다.
        public void RestoreAtRestRoom(int amount = 8)
        {
            if (player != null) player.Heal(amount);
        }
    }
}
