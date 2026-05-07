using Core;
using UnityEngine;

namespace GameMain.RunTime
{
    public class ChangeBackgroundArea : LDtkTriggerEntity
    {
        protected override void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
                return;

            var parallax = FindFirstObjectByType<ParallaxBackground>();
            if (parallax == null)
            {
                CLogger.LogWarn(
                    $"[ChangeBackgroundArea] {name}: no ParallaxBackground found in scene",
                    LogTag.LevelManager
                );
                return;
            }

            parallax.ChangeBackground(BackgroundName);

            CLogger.LogInfo(
                $"[ChangeBackgroundArea] {name}: changed background to '{BackgroundName}'",
                LogTag.LevelManager
            );
        }

        [LDtkField]
        public string BackgroundName;
    }
}
