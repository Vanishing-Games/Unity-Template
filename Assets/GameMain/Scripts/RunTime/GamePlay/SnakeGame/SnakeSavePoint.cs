using Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameMain.RunTime
{
    public class SnakeSavePoint : LDtkTriggerEntity
    {
        [LDtkField]
        public string PointName;

        private bool m_IsSaved;

        public override void OnPostImport()
        {
            if (string.IsNullOrEmpty(PointName))
            {
                string worldId = World != null ? World.Identifier : "World";
                string levelId = Level != null ? Level.Identifier : "Level";
                PointName = $"{worldId}_{levelId}_{LdtkIid}";
            }
        }

        protected override void OnTriggerEnter2D(Collider2D other)
        {
            if (m_IsSaved)
                return;

            if (other.GetComponentInParent<PlayerSnake>() != null)
            {
                m_IsSaved = true;
                OnSaveTriggered();
            }
        }

        private void OnSaveTriggered()
        {
            CLogger.LogInfo($"Snake reached save point: {PointName}", LogTag.Game);
            MessageBroker.Global.Publish(
                new GamePlaySnakeGameEvents.SnakeCheckPointEvent(this.transform.position)
            );
            MessageBroker.Global.Publish(new GamePlaySnakeGameEvents.SnakeSaveEvent());
            LevelManager.Instance.RecordLastSavePoint(PointName);
            VgSaveSystem.Instance.WriteSlotSaveAsync().Forget();
        }
    }
}
