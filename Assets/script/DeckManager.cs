using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoguelikeUnity
{
    // 카드 덱과 상위 카드 보관을 전담하는 관리 클래스입니다.
    // 하위 카드만 드로우/보유/버리기 덱에 소속되고, 상위 카드는 장비/도구 영역에만 보관되도록 분리합니다.
    public sealed class DeckManager
    {
        // 보유 중인 하위 카드 목록입니다.
        // 이 덱에는 드로우 덱 생성에 사용되는 카드만 들어갑니다.
        private readonly List<CardDefinition> deck = new List<CardDefinition>();

        // 현재 드로우 가능한 하위 카드 목록입니다.
        // 장비/도구 영역에 속하지 않는 카드만 이곳에 들어갑니다.
        private readonly List<CardDefinition> drawDeck = new List<CardDefinition>();

        // 사용되거나 버려진 하위 카드가 쌓이는 목록입니다.
        // 드로우 덱이 비면 여기서 재셔플해서 다시 채웁니다.
        private readonly List<CardDefinition> discardDeck = new List<CardDefinition>();

        // 상위 카드 중 장비로 분류된 카드의 보관 목록입니다.
        private readonly List<CardDefinition> equipmentCards = new List<CardDefinition>();

        // 상위 카드 중 도구로 분류된 카드의 보관 목록입니다.
        private readonly List<CardDefinition> toolCards = new List<CardDefinition>();

        // 하위 카드 보유 덱을 외부에서 열람할 수 있게 하는 프로퍼티입니다.
        public IReadOnlyList<CardDefinition> Deck => deck;

        // 현재 드로우 가능한 하위 카드 목록입니다.
        public IReadOnlyList<CardDefinition> DrawDeck => drawDeck;

        // 사용되거나 버려진 하위 카드 목록입니다.
        public IReadOnlyList<CardDefinition> DiscardDeck => discardDeck;

        // 장비형 상위 카드 목록입니다.
        public IReadOnlyList<CardDefinition> EquipmentCards => equipmentCards;

        // 도구형 상위 카드 목록입니다.
        public IReadOnlyList<CardDefinition> ToolCards => toolCards;

        // 모든 덱과 보관 영역을 초기화합니다.
        public void Clear()
        {
            deck.Clear();
            drawDeck.Clear();
            discardDeck.Clear();
            equipmentCards.Clear();
            toolCards.Clear();
        }

        // 하위 카드만 보유 덱에 추가합니다.
        // 같은 이름의 카드가 이미 있으면 강화 레벨을 올립니다.
        public void AddCardToOwnedDeck(CardDefinition card)
        {
            if (card == null) return;
            if (!card.IsLowerCard)
            {
                Debug.LogWarning($"{card.Name}은(는) 상위 카드라서 덱에 추가할 수 없습니다. 장비/도구 영역으로 보관해야 합니다.");
                return;
            }

            var existingCard = deck.FirstOrDefault(existing => string.Equals(existing.Name, card.Name, StringComparison.Ordinal));
            if (existingCard != null)
            {
                existingCard.Upgrade();
                Debug.Log($"{existingCard.DisplayName} 카드의 능력이 강화되었습니다. (강화 단계: {existingCard.UpgradeLevel})");
            }
            else
            {
                deck.Add(card);
                Debug.Log($"{card.Name} 카드를 보유 덱에 추가했습니다. 현재 {deck.Count}장");
            }

            RefreshDrawDeck();
        }

        // 상위 카드를 장비 영역에 배치합니다.
        public void AddUpperCardToEquipment(CardDefinition card)
        {
            if (card == null) return;
            if (!card.IsUpperCard)
            {
                Debug.LogWarning($"{card.Name}은(는) 하위 카드라서 장비 영역에 넣을 수 없습니다.");
                return;
            }

            var existingCard = equipmentCards.FirstOrDefault(existing => string.Equals(existing.Name, card.Name, StringComparison.Ordinal));
            if (existingCard != null)
            {
                existingCard.Upgrade();
                Debug.Log($"장비 {existingCard.DisplayName} 카드의 능력이 강화되었습니다. (강화 단계: {existingCard.UpgradeLevel})");
            }
            else
            {
                equipmentCards.Add(card);
                Debug.Log($"{card.Name} 카드를 장비 영역에 배치했습니다.");
            }
        }

        // 상위 카드를 도구 영역에 배치합니다.
        public void AddUpperCardToTool(CardDefinition card)
        {
            if (card == null) return;
            if (!card.IsUpperCard)
            {
                Debug.LogWarning($"{card.Name}은(는) 하위 카드라서 도구 영역에 넣을 수 없습니다.");
                return;
            }

            var existingCard = toolCards.FirstOrDefault(existing => string.Equals(existing.Name, card.Name, StringComparison.Ordinal));
            if (existingCard != null)
            {
                existingCard.Upgrade();
                Debug.Log($"도구 {existingCard.DisplayName} 카드의 능력이 강화되었습니다. (강화 단계: {existingCard.UpgradeLevel})");
            }
            else
            {
                toolCards.Add(card);
                Debug.Log($"{card.Name} 카드를 도구 영역에 배치했습니다.");
            }
        }

        // 보유 덱을 기준으로 드로우 덱을 다시 생성하고 섞습니다.
        public void RefreshDrawDeck()
        {
            drawDeck.Clear();
            if (deck.Count == 0) return;

            drawDeck.AddRange(deck.OrderBy(_ => UnityEngine.Random.value));
            Debug.Log($"드로우 덱 재생성: {drawDeck.Count}장");
        }

        // 드로우 덱에서 카드 한 장을 뽑습니다.
        // 드로우 덱이 비어 있으면 버리기 덱을 재셔플해 다시 채웁니다.
        public CardDefinition DrawCard()
        {
            if (drawDeck.Count == 0)
            {
                RefillDrawDeckFromDiscard();
                if (drawDeck.Count == 0)
                    return null;
            }

            var card = drawDeck[0];
            drawDeck.RemoveAt(0);
            return card;
        }

        // 하위 카드를 사용했을 때 버리기 덱으로 이동시킵니다.
        // 사용된 카드는 드로우 덱에서 제거되고, 없으면 보유 덱에서 제거된 것으로 간주합니다.
        public void UseCard(CardDefinition card)
        {
            if (card == null) return;
            if (!card.IsLowerCard)
            {
                Debug.LogWarning($"{card.Name}은(는) 상위 카드라서 덱에서 사용될 수 없습니다.");
                return;
            }

            if (drawDeck.Remove(card))
            {
                discardDeck.Add(card);
                return;
            }

            if (deck.Contains(card))
            {
                discardDeck.Add(card);
            }
        }

        // 버리기 덱을 섞어 드로우 덱으로 다시 채웁니다.
        // 이 로직은 드로우 덱 소진 시 호출됩니다.
        public void RefillDrawDeckFromDiscard()
        {
            if (discardDeck.Count == 0) return;

            var reshuffled = discardDeck.OrderBy(_ => UnityEngine.Random.value).ToList();
            drawDeck.AddRange(reshuffled);
            discardDeck.Clear();
            Debug.Log($"버리기 덱을 섞어 드로우 덱에 재추가했습니다. 현재 드로우: {drawDeck.Count}장");
        }
    }
}
