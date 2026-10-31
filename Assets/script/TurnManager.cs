namespace RoguelikeUnity
{
    // 턴 흐름을 전담하는 관리자입니다.
    // 플레이어 턴과 적 턴 상태를 별도로 관리해,
    // GameManager가 턴 전환 로직을 직접 다루지 않도록 분리합니다.
    public sealed class TurnManager
    {
        // 현재 턴이 플레이어 턴인지 여부를 저장합니다.
        private bool playerTurn = true;

        // 플레이어 턴 여부를 외부에서 확인할 수 있습니다.
        public bool IsPlayerTurn => playerTurn;

        // 적 턴 여부를 외부에서 확인할 수 있습니다.
        public bool IsEnemyTurn => !playerTurn;

        // 플레이어 턴을 시작합니다.
        public void StartPlayerTurn()
        {
            playerTurn = true;
        }

        // 적 턴을 시작합니다.
        public void StartEnemyTurn()
        {
            playerTurn = false;
        }

        // 턴을 초기 상태로 되돌립니다.
        public void Reset()
        {
            StartPlayerTurn();
        }
    }
}
