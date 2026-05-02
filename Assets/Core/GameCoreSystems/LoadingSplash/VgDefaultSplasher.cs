using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Core
{
    [RequireComponent(typeof(CanvasGroup))]
    public class VgDefaultSplasher : VgSplasher
    {
        public override async UniTask CoverAsync(CancellationToken ct = default)
        {
            m_CanvasGroup.alpha = 0f;
            ShowChildren();
            var tween = DOTween
                .To(() => m_CanvasGroup.alpha, x => m_CanvasGroup.alpha = x, 1f, m_FadeDuration)
                .SetEase(m_Ease);
            await TweenAsync(tween, ct);
        }

        public override async UniTask RevealAsync(CancellationToken ct = default)
        {
            m_CanvasGroup.alpha = 1f;
            var tween = DOTween
                .To(() => m_CanvasGroup.alpha, x => m_CanvasGroup.alpha = x, 0f, m_FadeDuration)
                .SetEase(m_Ease);
            await TweenAsync(tween, ct);
            HideChildren();
        }

        protected override void Awake()
        {
            base.Awake();
            m_CanvasGroup = GetComponent<CanvasGroup>();
        }

        [SerializeField]
        private float m_FadeDuration = 0.5f;

        [SerializeField]
        private Ease m_Ease = Ease.OutQuart;

        private CanvasGroup m_CanvasGroup;
    }
}
