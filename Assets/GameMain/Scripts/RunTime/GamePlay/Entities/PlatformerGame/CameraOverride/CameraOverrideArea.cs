using Core;
using FMOD.Studio;
using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;

namespace GameMain.RunTime
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class CameraOverrideArea : AutoLdtkEntity
    {
        private void Awake()
        {
            GetComponent<BoxCollider2D>().isTrigger = true;

            var go = new GameObject("OverrideCamera");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;

            m_OverrideCamera = go.AddComponent<CinemachineCamera>();
            go.AddComponent<CinemachinePositionComposer>();

            m_OverrideCamera.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            m_OverrideCamera.Lens.OrthographicSize = m_OrthographicSize;
            m_OverrideCamera.Lens.NearClipPlane = 0.1f;
            m_OverrideCamera.Lens.FarClipPlane = 5000f;

            m_OverrideCamera.Priority.Enabled = false;

            var composer = m_OverrideCamera.GetComponent<CinemachinePositionComposer>();

            if (composer != null)
            {
                // --- 修改限制框 (Composition) ---
                // 注意：Unity 6 中取消了 m_ 前缀，改为大写开头的属性
                Rect rect = composer.Composition.DeadZoneRect;

                rect.width = 0.2f;
                rect.height = 0.2f;
                rect.x = (1f - 0.2f) / 2f;
                rect.y = (1f - 0.2f) / 2f;

                composer.Composition.DeadZoneRect = rect;

                // --- 修改延迟/阻尼 (Damping) ---
                composer.Damping.x = 1.5f;
                composer.Damping.y = 1.5f;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
                return;

            m_OverrideCamera.Follow = other.transform;
            m_OverrideCamera.Priority.Enabled = true;
            m_OverrideCamera.Priority.Value = k_OverridePriority;

            CLogger.LogInfo(
                $"[CameraOverrideArea] {name}: camera override activated",
                LogTag.LevelManager
            );
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
                return;

            m_OverrideCamera.Priority.Enabled = false;

            CLogger.LogInfo(
                $"[CameraOverrideArea] {name}: camera override deactivated",
                LogTag.LevelManager
            );
        }

        private void OnDisable()
        {
            if (m_OverrideCamera != null)
                m_OverrideCamera.Priority.Enabled = false;
        }

        private const int k_OverridePriority = 114514;

        [LabelText("镜头大小")]
        [SerializeField]
        private float m_OrthographicSize = 11.25f;

        private CinemachineCamera m_OverrideCamera;
    }
}
