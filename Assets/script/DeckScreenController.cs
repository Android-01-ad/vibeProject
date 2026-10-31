using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoguelikeUnity
{
    // 덱(보유 카드 목록) 화면을 관리하는 UI 컨트롤러입니다.
    // 카드 목록을 보여주고, 화면이 들어오고 나가는 애니메이션을 처리합니다.
    public sealed class DeckScreenController : MonoBehaviour
    {
        // 화면 전체를 담당하는 CanvasGroup입니다.
        // 화면 표시 여부와 상호작용 가능 여부를 제어합니다.
        [SerializeField] private CanvasGroup screenGroup;

        // 카드 목록 영역만 따로 페이드 인/아웃할 때 쓰는 그룹입니다.
        [SerializeField] private CanvasGroup cardsGroup;

        // 카드 목록 영역의 실제 스케일을 조절해 팝업처럼 들어오게 하는 루트입니다.
        [SerializeField] private RectTransform cardsRoot;

        // 덱의 카드 이름과 설명을 출력할 텍스트 컴포넌트입니다.
        [SerializeField] private Text cardListText;

        // 화면 등장/퇴장 애니메이션 속도입니다.
        [SerializeField, Min(0.05f)] private float animationDuration = 0.35f;

        // 현재 실행 중인 애니메이션 코루틴을 저장해 중복 실행을 막습니다.
        private Coroutine animationRoutine;

        // 오브젝트가 활성화될 때 초기 상태를 정리합니다.
        private void Awake()
        {
            // screenGroup이 비어 있으면 자기 자신을 참조합니다.
            if (screenGroup == null) screenGroup = GetComponent<CanvasGroup>();

            // 화면은 기본적으로 비활성 상태로 시작해 깜빡임 없이 숨깁니다.
            if (screenGroup != null)
            {
                screenGroup.alpha = 1f;
                screenGroup.interactable = false;
                screenGroup.blocksRaycasts = false;
                gameObject.SetActive(false);
            }
        }

        // 덱 화면을 표시합니다.
        // cards: 현재 보유 중인 카드 목록입니다.
        public void Show(IReadOnlyList<CardDefinition> cards)
        {
            if (screenGroup == null) return;

            // 기존 애니메이션이 있으면 중지하고 새로 시작합니다.
            if (animationRoutine != null) StopCoroutine(animationRoutine);

            gameObject.SetActive(true);
            RenderCards(cards);
            animationRoutine = StartCoroutine(AnimateIn());
        }

        // 덱 화면을 닫습니다.
        public void Hide()
        {
            if (animationRoutine != null) StopCoroutine(animationRoutine);
            if (screenGroup == null) return;

            animationRoutine = StartCoroutine(AnimateOut());
        }

        // 카드 목록을 텍스트 형태로 화면에 렌더링합니다.
        private void RenderCards(IReadOnlyList<CardDefinition> cards)
        {
            if (cardListText == null) return;

            // 카드가 없으면 안내 문구를 표시합니다.
            if (cards == null || cards.Count == 0)
            {
                cardListText.text = "덱이 비어 있습니다.";
                return;
            }

            // 이름과 설명을 줄 단위로 구성해 표시합니다.
            var text = new System.Text.StringBuilder();
            for (var i = 0; i < cards.Count; i++)
                text.AppendLine($"{i + 1}. {cards[i].Name}\n   {cards[i].Description}\n");
            cardListText.text = text.ToString();
        }

        // 화면이 나타나는 애니메이션입니다.
        // 배경은 항상 보이게 하고, 카드 영역만 서서히 페이드 인하며 확대됩니다.
        private IEnumerator AnimateIn()
        {
            // 배경은 계속 불투명하게 유지하고,
            // 카드 영역만 투명도와 스케일을 애니메이트합니다.
            screenGroup.alpha = 1f;
            screenGroup.interactable = false;
            screenGroup.blocksRaycasts = false;
            if (cardsGroup != null) cardsGroup.alpha = 0f;
            if (cardsRoot != null) cardsRoot.localScale = Vector3.one * 0.92f;

            var elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / animationDuration);
                var eased = 1f - Mathf.Pow(1f - t, 3f);

                if (cardsGroup != null) cardsGroup.alpha = eased;
                if (cardsRoot != null) cardsRoot.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, eased);
                yield return null;
            }

            // 애니메이션 종료 후 최종 상태를 고정합니다.
            screenGroup.alpha = 1f;
            if (cardsGroup != null) cardsGroup.alpha = 1f;
            screenGroup.interactable = true;
            screenGroup.blocksRaycasts = true;
            animationRoutine = null;
        }

        // 화면이 사라지는 애니메이션입니다.
        // 카드 영역을 서서히 투명하게 만든 뒤 화면을 비활성화합니다.
        private IEnumerator AnimateOut()
        {
            screenGroup.interactable = false;
            screenGroup.blocksRaycasts = false;
            var startAlpha = cardsGroup != null ? cardsGroup.alpha : 1f;
            var elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (cardsGroup != null)
                    cardsGroup.alpha = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(elapsed / animationDuration));
                yield return null;
            }

            if (cardsGroup != null) cardsGroup.alpha = 0f;
            gameObject.SetActive(false);
            animationRoutine = null;
        }
    }
}
