using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace GameMain.RunTime
{
    public class PlayerDeathSplasher : VgSplasher
    {
        public override async UniTask CoverAsync(CancellationToken ct = default)
        {
            UpdateCenter();
            float maxRadius = ComputeMaxRadius(m_CenterUV);
            m_CircleWipe.SetCenter(m_CenterUV);
            m_CircleWipe.SetRadius(maxRadius);
            ShowChildren();

            var tween = DOTween
                .To(
                    () => m_CircleWipe.GetRadius(),
                    r => m_CircleWipe.SetRadius(r),
                    0f,
                    m_CoverDuration
                )
                .SetEase(m_CoverEase);
            await TweenAsync(tween, ct);
        }

        public override async UniTask RevealAsync(CancellationToken ct = default)
        {
            UpdateCenter();
            float maxRadius = ComputeMaxRadius(m_CenterUV);
            m_CircleWipe.SetCenter(m_CenterUV);
            m_CircleWipe.SetRadius(0f);

            var tween = DOTween
                .To(
                    () => m_CircleWipe.GetRadius(),
                    r => m_CircleWipe.SetRadius(r),
                    maxRadius,
                    m_RevealDuration
                )
                .SetEase(m_RevealEase);
            await TweenAsync(tween, ct);

            HideChildren();
        }

        private void UpdateCenter()
        {
            var player = GameMain.TryGetPlayer();
            if (player == null)
            {
                CLogger.LogWarn(
                    "[PlayerDeathSplasher] No player found — defaulting to screen center.",
                    LogTag.Loading
                );
                m_CenterUV = new Vector2(0.5f, 0.5f);
                return;
            }

            var cam = Camera.main;
            if (cam == null)
            {
                m_CenterUV = new Vector2(0.5f, 0.5f);
                return;
            }

            Vector3 screen = cam.WorldToScreenPoint(player.transform.position);
            m_CenterUV = new Vector2(screen.x / Screen.width, screen.y / Screen.height);
        }

        private static float ComputeMaxRadius(Vector2 center)
        {
            float aspect = (float)Screen.width / Screen.height;
            float maxDist = 0f;

            Vector2[] corners = { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };

            foreach (var corner in corners)
            {
                Vector2 diff = corner - center;
                diff.x *= aspect;
                float dist = diff.magnitude;
                if (dist > maxDist)
                    maxDist = dist;
            }

            return maxDist * 1.05f;
        }

        [SerializeField]
        private VgCircleWipeEffect m_CircleWipe;

        [SerializeField]
        private float m_CoverDuration = 0.8f;

        [SerializeField]
        private Ease m_CoverEase = Ease.InQuart;

        [SerializeField]
        private float m_RevealDuration = 1.0f;

        [SerializeField]
        private Ease m_RevealEase = Ease.OutElastic;

        private Vector2 m_CenterUV = new(0.5f, 0.5f);
    }
}
