using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoguelikeUnity
{
    // 카드 덱 관리 전담 클래스입니다.
    // 보유 덱, 드로우 덱, 버리기 덱을 모두 분리해 관리하며 카드 소진 시 재셔플 로직까지 담당합니다.
    public sealed class DeckManager
    {
        // 보유하고 있는 카드 덱입니다.
        private readonly List<CardDefinition> deck = new List<CardDefinition>();

        // 현재 드로우 가능한 카드 덱입니다.
        private readonly List<CardDefinition> drawDeck = new List<CardDefinition>();

        // 사용하거나 버린 카드가 쌓이는 덱입니다.
        private readonly List<CardDefinition> discardDeck = new List<CardDefinition>();

        // 보유 카드 덱을 외부에서 읽을 수 있게 하는 프로퍼티입니다.
        public IReadOnlyList<CardDefinition> Deck => deck;

        // 드로우 덱을 외부에서 읽을 수 있게 하는 프로퍼티입니다.
        public IReadOnlyList<CardDefinition> DrawDeck => drawDeck;

        // 버리기 덱을 외부에서 읽을 수 있게 하는 프로퍼티입니다.
        public IReadOnlyList<CardDefinition> DiscardDeck => discardDeck;

        // 카드 덱을 모두 비웁니다.
        public void Clear()
        {
            deck.Clear();
            drawDeck.Clear();
            discardDeck.Clear();
        }

        // 보유 덱에 카드를 추가합니다. 동일 카드가 있으면 강화합니다.
        public void AddCardToOwnedDeck(CardDefinition card)
        {
            if (card == null) return;

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

        // 보유 덱 기준으로 드로우 덱을 다시 섞어 생성합니다.
        public void RefreshDrawDeck()
        {
            drawDeck.Clear();
            if (deck.Count == 0) return;

            drawDeck.AddRange(deck.OrderBy(_ => UnityEngine.Random.value));
            Debug.Log($"드로우 덱 재생성: {drawDeck.Count}장");
        }

        // 드로우 덱에서 카드 한 장을 뽑습니다.
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

        // 사용한 카드를 버리기 덱으로 옮깁니다.
        public void UseCard(CardDefinition card)
        {
            if (card == null) return;

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

        // 버리기 덱을 섞어서 드로우 덱으로 다시 채웁니다.
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
