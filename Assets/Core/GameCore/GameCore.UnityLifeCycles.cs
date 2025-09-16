using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    public partial class GameCore : MonoSingletonPersistent<GameCore>
    {
        protected override async void Awake()
        {
            Logger.ReleaseLogInfo("[GameCore] Awake...", LogTag.GameCoreAwake);
            base.Awake();

            if (!GameRunCheck())
            {
                Logger.ReleaseLogInfo(
                    "[GameCore] Game Check Failed, Quit Game...",
                    LogTag.GameRunCheck
                );
                await QuitGame();
                return;
            }

            await InitiatingGame();
        }

        private void FixedUpdate() { }

        private void Update() { }

        private void OnDestroy() { }
    }
}
