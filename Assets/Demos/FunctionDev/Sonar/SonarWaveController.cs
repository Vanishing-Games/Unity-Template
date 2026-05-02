using UnityEngine;

namespace Sonar
{
    [ExecuteInEditMode]
    public class SonarWaveController : MonoBehaviour
    {
        public Material sonarMaterial;
        public float maxRadius = 0.5f;
        public float expansionSpeed = 1f;
        public float loopDuration = 2f;

        private float m_CurrentRadius = 0f;
        private Camera m_MainCamera;
        private static readonly int s_RadiusId = Shader.PropertyToID("_Radius");
        private static readonly int s_SonarCenterScreenUVId = Shader.PropertyToID(
            "_SonarCenterScreenUV"
        );

        void Start()
        {
            m_MainCamera = Camera.main;
            if (sonarMaterial == null)
            {
                var renderer = GetComponent<Renderer>();
                if (renderer != null)
                    sonarMaterial = renderer.sharedMaterial;
            }
        }

        void Update()
        {
            if (sonarMaterial == null)
                return;

            // 1. 动画半径
            m_CurrentRadius += expansionSpeed * Time.deltaTime;
            if (m_CurrentRadius > maxRadius)
                m_CurrentRadius = 0f;

            sonarMaterial.SetFloat(s_RadiusId, m_CurrentRadius);

            // 2. 计算中心点在屏幕上的 UV 并传给 Shader
            if (m_MainCamera == null)
                m_MainCamera = Camera.main;
            if (m_MainCamera != null)
            {
                Vector3 screenPos = m_MainCamera.WorldToViewportPoint(transform.position);
                // Viewport Point 是 [0, 1] 范围的 UV，正好符合我们采样 _SonarObstacleTex 的需求
                sonarMaterial.SetVector(
                    s_SonarCenterScreenUVId,
                    new Vector4(screenPos.x, screenPos.y, 0, 0)
                );
            }
        }
    }
}
