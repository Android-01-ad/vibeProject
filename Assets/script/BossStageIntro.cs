using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RoguelikeUnity
{
    // 보스 스테이지 입장 시 등장 연출을 재생하는 컴포넌트입니다.
    // 텍스트, 애니메이션, 카메라, 파티클, 사운드 등을 조합해 인트로를 구성할 수 있습니다.
    /// <summary>Configurable boss-stage intro. Add custom visuals in the Inspector or override PlayCustomIntro.</summary>
    public class BossStageIntro : MonoBehaviour
    {
        // 인트로 UI와 기본 연출 설정을 담는 영역입니다.
        [Header("Optional intro UI")]

        // 인트로 화면 전체를 감싸는 캔버스 그룹입니다.
        [SerializeField] private CanvasGroup introGroup;

        // 보스 이름을 표시할 텍스트 UI입니다.
        [SerializeField] private Text bossNameText;

        // 보스 애니메이터입니다. Intro 트리거를 발생시킵니다.
        [SerializeField] private Animator bossAnimator;

        // 애니메이터의 트리거 이름입니다.
        [SerializeField] private string introTrigger = "Intro";

        // 인트로가 표시되는 시간입니다.
        [SerializeField, Min(0f)] private float introDuration = 2.5f;

        // 추가 연출용 효과를 설정하는 영역입니다.
        [Header("Optional special effects")]

        // 등장 파티클 효과 배열입니다.
        [SerializeField] private ParticleSystem[] introParticles;

        // 인트로 사운드를 재생할 AudioSource입니다.
        [SerializeField] private AudioSource audioSource;

        // 인트로 재생 시 사용할 오디오 클립입니다.
        [SerializeField] private AudioClip introSound;

        // 인트로 전용 카메라입니다.
        [SerializeField] private Camera introCamera;

        // 인트로 시작/종료 시 호출될 이벤트 훅입니다.
        [Header("Hooks")]
        [SerializeField] private UnityEvent onIntroStarted;
        [SerializeField] private UnityEvent onIntroFinished;

        // 인트로가 현재 재생 중인지 여부입니다.
        public bool IsPlaying { get; private set; }

        // 보스 인트로를 재생합니다.
        // bossName: 표시할 보스 이름
        // onComplete: 인트로 종료 후 실행할 콜백
        public void Play(string bossName, System.Action onComplete = null)
        {
            if (IsPlaying) return;
            StartCoroutine(PlayRoutine(bossName, onComplete));
        }

        // 자식 클래스에서 커스텀 인트로 연출을 추가할 수 있는 확장 포인트입니다.
        // 예: 카메라 이동, 화면 흔들림, 타임라인 재생, 추가 UI 효과 등
        protected virtual IEnumerator PlayCustomIntro(string bossName)
        {
            // 파생 컴포넌트에서 카메라 이동, 타임라인, 화면 흔들림, 추가 연출 등을 넣고 싶을 때 오버라이드하세요.
            yield return null;
        }

        // 인트로 재생 전체 흐름입니다.
        private IEnumerator PlayRoutine(string bossName, System.Action onComplete)
        {
            IsPlaying = true;

            if (introGroup != null)
            {
                introGroup.gameObject.SetActive(true);
                introGroup.alpha = 1f;
                introGroup.interactable = false;
                introGroup.blocksRaycasts = true;
            }

            if (bossNameText != null) bossNameText.text = bossName;
            if (bossAnimator != null && !string.IsNullOrEmpty(introTrigger))
                bossAnimator.SetTrigger(introTrigger);
            if (introCamera != null) introCamera.enabled = true;
            if (introParticles != null)
                foreach (var particles in introParticles)
                    if (particles != null) particles.Play();
            if (audioSource != null && introSound != null) audioSource.PlayOneShot(introSound);

            onIntroStarted?.Invoke();
            yield return PlayCustomIntro(bossName);
            if (introDuration > 0f) yield return new WaitForSeconds(introDuration);

            if (introGroup != null)
            {
                introGroup.blocksRaycasts = false;
                introGroup.gameObject.SetActive(false);
            }

            onIntroFinished?.Invoke();
            IsPlaying = false;
            onComplete?.Invoke();
        }
    }
}
