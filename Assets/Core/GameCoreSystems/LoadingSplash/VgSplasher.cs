using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core
{
    [RequireComponent(typeof(Canvas))]
    public abstract class VgSplasher : MonoBehaviour
    {
        protected virtual void Awake()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
        }

        [Button("Preview Animation")]
        private void PreviewAnimation()
        {
            if (!Application.isPlaying)
            {
                CLogger.LogWarn(
                    "[VgSplasher] Preview is only available in Play Mode.",
                    LogTag.Loading
                );
                return;
            }
            RunPreviewAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid RunPreviewAsync(CancellationToken ct)
        {
            gameObject.SetActive(true);
            await CoverAsync(ct);
            await UniTask.Delay(500, cancellationToken: ct);
            await RevealAsync(ct);
            gameObject.SetActive(false);
        }

        public void Init()
        {
            if (m_Inited)
                return;

            m_Progressables.Clear();
            var found = GetComponentsInChildren<IProgressable>(true);
            foreach (var p in found)
            {
                if (p is VgSplasher)
                    continue;
                m_Progressables.Add(p);
            }

            StringBuilder sb = new();
            sb.AppendLine(
                $"[{GetType().Name}] Init on '{gameObject.name}' — {m_Progressables.Count} IProgressable(s):"
            );
            foreach (var p in m_Progressables)
                sb.AppendLine($"  • {p.GetType().Name} on '{((MonoBehaviour)p).gameObject.name}'");
            CLogger.LogInfo(sb.ToString(), LogTag.Loading);

            m_Inited = true;
        }

        public abstract UniTask CoverAsync(CancellationToken ct = default);
        public abstract UniTask RevealAsync(CancellationToken ct = default);

        public virtual void UpdateProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            foreach (var p in m_Progressables)
                p.UpdateProgress(progress);
        }

        protected void ShowChildren()
        {
            Init();
            foreach (var p in m_Progressables)
                p.Show();
        }

        protected void HideChildren()
        {
            foreach (var p in m_Progressables)
                p.Hide();
        }

        protected static async UniTask TweenAsync(Tween tween, CancellationToken ct)
        {
            var tcs = new UniTaskCompletionSource();
            tween.OnComplete(() => tcs.TrySetResult());
            try
            {
                await tcs.Task.AttachExternalCancellation(ct);
            }
            catch (OperationCanceledException)
            {
                if (tween.IsActive())
                    tween.Kill();
                throw;
            }
        }

        private bool m_Inited;
        private readonly List<IProgressable> m_Progressables = new();
    }
}
