using System;
using Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameMain.RunTime
{
    public interface IPlayer
    {
        void Initialize(LevelManager.LevelLoadInfo loadInfo);
    }

    public class PlayerManager
    {
        public string SystemName => "PlayerManageSystem";

        public void SpawnPlayer(LevelManager.LevelLoadInfo loadInfo)
        {
            DespawnPlayer();

            CreatePlayer(
                loadInfo.ChapterId == GameIdentifiers.Chapters.Snake
                    ? SNAKE_PLAYER_PREFAB_PATH
                    : PLATFORMER_PLAYER_PREFAB_PATH
            );

            if (m_Player != null)
                m_Player.GetComponent<IPlayer>()?.Initialize(loadInfo);
            else
                CLogger.LogError("Failed To Create Player", LogTag.PlayerManager);
        }

        public void DespawnPlayer()
        {
            if (m_Player != null)
                return;

            GameObject.Destroy(m_Player);
            m_Player = null;
        }

        private void CreatePlayer(string path)
        {
            var players = GameObject.FindGameObjectsWithTag("Player");

            foreach (var player in players)
                GameObject.Destroy(player);

            var prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                CLogger.LogError($"Failed to load player prefab at path: {path}", LogTag.Loading);
                return;
            }

            m_Player = GameObject.Instantiate(prefab);
        }

        private const string PLATFORMER_PLAYER_PREFAB_PATH = "Prefabs/Player/PlatformerPlayer";
        private const string SNAKE_PLAYER_PREFAB_PATH = "Prefabs/Player/SnakePlayer";
        private GameObject m_Player;
    }
}
