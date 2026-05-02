using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RainRust.Rendering
{
    public class SonarObstacleFeature : ScriptableRendererFeature
    {
        [Serializable]
        public class Settings
        {
            public LayerMask obstacleLayerMask = -1;
            public Shader obstacleShader;
        }

        public Settings settings = new Settings();
        private SonarObstaclePass m_SonarPass;
        private Material m_ObstacleMaterial;

        public override void Create()
        {
            if (settings.obstacleShader == null)
            {
                settings.obstacleShader = Shader.Find("Hidden/RainRust/SonarObstacleUnlit");
            }

            if (settings.obstacleShader != null)
            {
                m_ObstacleMaterial = CoreUtils.CreateEngineMaterial(settings.obstacleShader);
            }

            m_SonarPass = new SonarObstaclePass(settings.obstacleLayerMask, m_ObstacleMaterial);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_ObstacleMaterial == null) return;
            renderer.EnqueuePass(m_SonarPass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(m_ObstacleMaterial);
        }
    }
}
