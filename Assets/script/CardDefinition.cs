namespace RoguelikeUnity
{
    // 카드 한 장의 데이터를 담는 클래스입니다.
    // CSV에서 읽어온 카드 이름과 설명을 저장하는 용도로 사용됩니다.
    [System.Serializable]
    public sealed class CardDefinition
    {
        // 카드의 이름. 인벤토리, UI, 카드 세부 정보 표시 등에 사용됩니다.
        public string Name;

        // 카드의 설명 문구. 카드 효과나 사용 방식 등을 나타냅니다.
        public string Description;

        // 동일 카드가 여러 번 획득되었을 때의 강화 단계입니다.
        public int UpgradeLevel { get; private set; }

        // 현재 카드의 효과 강도를 계산할 때 쓰는 값입니다.
        public int Power => 1 + UpgradeLevel;

        // 강화 단계가 표시된 카드 이름입니다. UI에 바로 사용 가능합니다.
        public string DisplayName => UpgradeLevel > 0 ? $"{Name}+{UpgradeLevel}" : Name;

        // 카드 이름과 설명을 한 번에 초기화하는 생성자입니다.
        public CardDefinition(string name, string description)
        {
            Name = name;
            Description = description;
        }

        // 동일 카드 획득 시 능력을 한 단계 강화합니다.
        public void Upgrade()
        {
            UpgradeLevel++;
        }
    }
}
