using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Core
{
    public class VgDissolveSplasher : VgSplasher
    {
        public override async UniTask CoverAsync(CancellationToken ct = default)
        {
            EnsureMaterial();
            m_Material.SetFloat(DissolveAmountId, 0f);
            m_DissolveImage.gameObject.SetActive(true);
            ShowChildren();

            var tween = DOTween
                .To(
                    () => m_Material.GetFloat(DissolveAmountId),
                    v => m_Material.SetFloat(DissolveAmountId, v),
                    1f,
                    m_Duration
                )
                .SetEase(m_CoverEase);
            await TweenAsync(tween, ct);
        }

        public override async UniTask RevealAsync(CancellationToken ct = default)
        {
            EnsureMaterial();
            m_Material.SetFloat(DissolveAmountId, 1f);

            var tween = DOTween
                .To(
                    () => m_Material.GetFloat(DissolveAmountId),
                    v => m_Material.SetFloat(DissolveAmountId, v),
                    0f,
                    m_Duration
                )
                .SetEase(m_RevealEase);
            await TweenAsync(tween, ct);

            m_DissolveImage.gameObject.SetActive(false);
            HideChildren();
        }

        private void EnsureMaterial()
        {
            if (m_Material != null)
                return;

            m_Material = new Material(m_DissolveImage.material);
            m_DissolveImage.material = m_Material;
        }

        private void OnDestroy()
        {
            if (m_Material != null)
                Destroy(m_Material);
        }

        private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");

        [SerializeField]
        private Image m_DissolveImage;

        [SerializeField]
        private float m_Duration = 0.6f;

        [SerializeField]
        private Ease m_CoverEase = Ease.Linear;

        [SerializeField]
        private Ease m_RevealEase = Ease.Linear;

        private Material m_Material;
    }
}
