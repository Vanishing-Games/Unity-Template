using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core
{
    public class VgLoadingBackground : MonoProgressable
    {
        private CancellationTokenSource _cancellationTokenSource;

        public override void Hide()
        {
            HideAsync().Forget();
        }

        public override void Show()
        {
            gameObject.SetActive(true);
        }

        private async UniTaskVoid HideAsync()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await WaitForAllTransitionsShown(_cancellationTokenSource.Token);
                gameObject.SetActive(false);
            }
            catch (OperationCanceledException) { }
            finally
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private async UniTask WaitForAllTransitionsShown(CancellationToken cancellationToken)
        {
            if (!HasActiveTransitions())
                return;

            var tcs = new UniTaskCompletionSource();
            var isCompleted = false;

            void OnTransitionsShown()
            {
                if (!isCompleted)
                {
                    isCompleted = true;
                    VgLoadingTransition.OnAllTransitionsShown -= OnTransitionsShown;
                    tcs.TrySetResult();
                }
            }

            VgLoadingTransition.OnAllTransitionsShown += OnTransitionsShown;

            try
            {
                await tcs.Task.AttachExternalCancellation(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (!isCompleted)
                {
                    VgLoadingTransition.OnAllTransitionsShown -= OnTransitionsShown;
                }
                throw;
            }
        }

        private bool HasActiveTransitions()
        {
            VgLoadingTransition[] transitions = FindObjectsOfType<VgLoadingTransition>();
            return transitions.Length > 0;
        }

        public override void UpdateProgress(float progress) { }
    }
}
