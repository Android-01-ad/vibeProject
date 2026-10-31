namespace RoguelikeUnity
{
    // 단일 카드의 데이터 모델입니다.
    // 이름, 설명, 카드 등급(하위/상위), 강화 단계 등을 저장해 본체 카드와 UI에 공통으로 사용합니다.
    [System.Serializable]
    public sealed class CardDefinition
    {
        // 카드의 소속 구분입니다.
        // Lower는 드로우/보유/버리기 덱에 속하고,
        // Upper는 장비/도구 영역에만 속하도록 분리합니다.
        public enum CardTier
        {
            Lower,
            Upper
        }

        // 카드의 이름입니다. UI, 로그, 인벤토리 식별에 사용됩니다.
        public string Name;

        // 카드의 설명 문구입니다. 효과와 사용 의미를 설명하는 텍스트입니다.
        public string Description;

        // 카드의 구분입니다.
        // Lower는 덱 기반 카드, Upper는 장비/도구형 카드를 의미합니다.
        public CardTier Tier { get; private set; }

        // 동일 카드가 여러 번 획득되었을 때 상승하는 강화 단계입니다.
        public int UpgradeLevel { get; private set; }

        // 실제 카드의 효과 강도를 계산할 때 쓰는 값입니다.
        public int Power => 1 + UpgradeLevel;

        // 강화 단계가 반영된 표시 이름입니다. UI에 바로 사용할 수 있습니다.
        public string DisplayName => UpgradeLevel > 0 ? $"{Name}+{UpgradeLevel}" : Name;

        // 하위 카드 여부입니다. 하위 카드는 덱에 소속됩니다.
        public bool IsLowerCard => Tier == CardTier.Lower;

        // 상위 카드 여부입니다. 상위 카드는 장비/도구 영역에만 소속됩니다.
        public bool IsUpperCard => Tier == CardTier.Upper;

        // 카드의 이름과 설명, 분류를 함께 초기화하는 생성자입니다.
        public CardDefinition(string name, string description, CardTier tier = CardTier.Lower)
        {
            Name = name;
            Description = description;
            Tier = tier;
        }

        // 같은 카드가 다시 획득되면 강화 레벨을 1 증가시킵니다.
        public void Upgrade()
        {
            UpgradeLevel++;
        }
    }
}
