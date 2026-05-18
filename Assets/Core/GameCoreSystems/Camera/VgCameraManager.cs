using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Core
{
    [Serializable]
    public struct CrtSettings
    {
        public float ScanlineDensity;
        public float ScanlineStrength;
        public float BarrelDistortion;
        public float VignetteStrength;
        public Color ColorTint;
    }

    [Serializable]
    public struct ChapterCrtConfig
    {
        public string ChapterId;
        public CrtSettings Settings;
    }

    public class VgCameraManager : CoreModuleManagerBase<VgCameraManager>, ICoreModuleSystem
    {
        public string SystemName => "VgCameraManager";
        public Type[] Dependencies => Array.Empty<Type>();

        [Header("Aspect Ratio Settings")]
        [SerializeField]
        private float m_DefaultAspectRatio = 1.7777778f; // 16:9

        [SerializeField]
        private float m_SnakeAspectRatio = 1.0f; // 1:1

        [Header("Retro UI Settings")]
        [SerializeField]
        private GameObject m_BorderCanvasPrefab;

        [SerializeField]
        private Sprite m_TvBorderSprite;

        [Header("CRT Settings")]
        [SerializeField]
        private Material m_CrtMaterial;

        [SerializeField]
        private CrtSettings m_DefaultCrtSettings;

        [SerializeField]
        private List<ChapterCrtConfig> m_ChapterCrtConfigs;

        private bool m_IsSnakeChapter = false;
        private float m_TargetAspectRatio;
        private GameObject m_BorderCanvasInstance;
        private Image m_LeftBar,
            m_RightBar,
            m_TopBar,
            m_BottomBar;
        private int m_LastScreenWidth,
            m_LastScreenHeight;

        private CrtSettings m_InitialMaterialSettings;
        private bool m_IsBackupDone = false;

        public void RegisterHooks(IGameCoreHookRegistry registry)
        {
            registry.OnBootStart(async () =>
            {
                if (m_MainCamera == null)
                {
                    m_MainCamera = Camera.main;
                }

                if (m_MainCamera != null && m_CinemachineBrain == null)
                {
                    m_CinemachineBrain = m_MainCamera.GetComponent<CinemachineBrain>();
                    if (m_CinemachineBrain == null)
                    {
                        m_CinemachineBrain =
                            m_MainCamera.gameObject.AddComponent<CinemachineBrain>();
                    }
                }

                if (m_LoadingCamera != null)
                {
                    m_LoadingCamera.gameObject.SetActive(false);
                }

                m_TargetAspectRatio = m_DefaultAspectRatio;
                InitializeBorderUI();

                await UniTask.CompletedTask;
            });

            registry.OnUpdate(() =>
            {
                if (Screen.width != m_LastScreenWidth || Screen.height != m_LastScreenHeight)
                {
                    UpdateCameraViewport();
                }
            });

            registry.OnGameQuit(() =>
            {
                RestoreInitialCrtSettings();
                return UniTask.CompletedTask;
            });
        }

        /// <summary>
        /// Sets the camera to retro mode based on the chapter ID.
        /// Called by LevelManager or other systems that know about chapters.
        /// </summary>
        public void SetChapterRetroMode(string chapterId)
        {
            m_IsSnakeChapter = chapterId == "Chapter_Snake";
            m_TargetAspectRatio = m_IsSnakeChapter ? m_SnakeAspectRatio : m_DefaultAspectRatio;

            CLogger.LogInfo(
                $"Camera switching to {(m_IsSnakeChapter ? "Retro (1:1)" : "Standard (16:9)")} mode for chapter: {chapterId}",
                LogTag.VgCameraManager
            );
            UpdateCameraViewport();

            UpdateCrtSettings(chapterId);

            //GetComponent<PixelPerfectCamera>().enabled = m_IsSnakeChapter;
        }

        private void UpdateCrtSettings(string chapterId)
        {
            if (m_CrtMaterial == null)
            {
                return;
            }

            if (!m_IsBackupDone)
            {
                BackupInitialCrtSettings();
            }

            CrtSettings targetSettings = m_DefaultCrtSettings;
            bool found = false;

            if (m_ChapterCrtConfigs != null)
            {
                foreach (var config in m_ChapterCrtConfigs)
                {
                    if (chapterId.StartsWith(config.ChapterId))
                    {
                        targetSettings = config.Settings;
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                CLogger.LogError(
                    $"No CRT settings found for chapter: {chapterId}. Using default settings.",
                    LogTag.VgCameraManager
                );
            }

            ApplyCrtSettings(targetSettings);
        }

        private void ApplyCrtSettings(CrtSettings settings)
        {
            if (m_CrtMaterial == null)
            {
                return;
            }

            m_CrtMaterial.SetFloat("_ScanlineDensity", settings.ScanlineDensity);
            m_CrtMaterial.SetFloat("_ScanlineStrength", settings.ScanlineStrength);
            m_CrtMaterial.SetFloat("_BarrelDistortion", settings.BarrelDistortion);
            m_CrtMaterial.SetFloat("_VignetteStrength", settings.VignetteStrength);
            m_CrtMaterial.SetColor("_ColorTint", settings.ColorTint);
        }

        private void BackupInitialCrtSettings()
        {
            if (m_CrtMaterial == null)
            {
                return;
            }

            m_InitialMaterialSettings = new CrtSettings
            {
                ScanlineDensity = m_CrtMaterial.GetFloat("_ScanlineDensity"),
                ScanlineStrength = m_CrtMaterial.GetFloat("_ScanlineStrength"),
                BarrelDistortion = m_CrtMaterial.GetFloat("_BarrelDistortion"),
                VignetteStrength = m_CrtMaterial.GetFloat("_VignetteStrength"),
                ColorTint = m_CrtMaterial.GetColor("_ColorTint"),
            };
            m_IsBackupDone = true;
        }

        private void RestoreInitialCrtSettings()
        {
            if (m_IsBackupDone && m_CrtMaterial != null)
            {
                ApplyCrtSettings(m_InitialMaterialSettings);
            }
        }

        private void InitializeBorderUI()
        {
            if (m_BorderCanvasPrefab == null)
            {
                CLogger.LogWarn(
                    "Border Canvas Prefab is not assigned in VgCameraManager",
                    LogTag.VgCameraManager
                );
                return;
            }

            m_BorderCanvasInstance = Instantiate(m_BorderCanvasPrefab);
            m_BorderCanvasInstance.name = "[CameraBorderCanvas]";
            DontDestroyOnLoad(m_BorderCanvasInstance);

            // Assuming prefab structure: Left, Right, Top, Bottom Image components
            m_LeftBar = m_BorderCanvasInstance.transform.Find("Left")?.GetComponent<Image>();
            m_RightBar = m_BorderCanvasInstance.transform.Find("Right")?.GetComponent<Image>();
            m_TopBar = m_BorderCanvasInstance.transform.Find("Top")?.GetComponent<Image>();
            m_BottomBar = m_BorderCanvasInstance.transform.Find("Bottom")?.GetComponent<Image>();
        }

        private void UpdateCameraViewport()
        {
            if (m_MainCamera == null)
                return;

            m_LastScreenWidth = Screen.width;
            m_LastScreenHeight = Screen.height;

            float screenAspect = (float)m_LastScreenWidth / m_LastScreenHeight;
            Rect rect = new Rect(0, 0, 1, 1);

            if (screenAspect > m_TargetAspectRatio)
            {
                // Pillarbox
                float pillarWidth = m_TargetAspectRatio / screenAspect;
                rect.width = pillarWidth;
                rect.x = (1.0f - pillarWidth) / 2.0f;
            }
            else
            {
                // Letterbox
                float letterHeight = screenAspect / m_TargetAspectRatio;
                rect.height = letterHeight;
                rect.y = (1.0f - letterHeight) / 2.0f;
            }

            m_MainCamera.rect = rect;
            UpdateBorderUI(rect);
        }

        private void UpdateBorderUI(Rect viewportRect)
        {
            if (m_BorderCanvasInstance == null)
                return;

            bool showTvBorder = m_IsSnakeChapter && m_TvBorderSprite != null;

            if (m_LeftBar)
            {
                m_LeftBar.rectTransform.anchorMin = Vector2.zero;
                m_LeftBar.rectTransform.anchorMax = new Vector2(viewportRect.x, 1);
                m_LeftBar.rectTransform.sizeDelta = Vector2.zero;
                m_LeftBar.sprite = showTvBorder ? m_TvBorderSprite : null;
                m_LeftBar.color = showTvBorder ? Color.white : Color.black;
            }

            if (m_RightBar)
            {
                m_RightBar.rectTransform.anchorMin = new Vector2(
                    viewportRect.x + viewportRect.width,
                    0
                );
                m_RightBar.rectTransform.anchorMax = Vector2.one;
                m_RightBar.rectTransform.sizeDelta = Vector2.zero;
                m_RightBar.sprite = showTvBorder ? m_TvBorderSprite : null;
                m_RightBar.color = showTvBorder ? Color.white : Color.black;
            }

            if (m_TopBar)
            {
                m_TopBar.rectTransform.anchorMin = new Vector2(
                    0,
                    viewportRect.y + viewportRect.height
                );
                m_TopBar.rectTransform.anchorMax = Vector2.one;
                m_TopBar.rectTransform.sizeDelta = Vector2.zero;
                m_TopBar.color = Color.black;
            }

            if (m_BottomBar)
            {
                m_BottomBar.rectTransform.anchorMin = Vector2.zero;
                m_BottomBar.rectTransform.anchorMax = new Vector2(1, viewportRect.y);
                m_BottomBar.rectTransform.sizeDelta = Vector2.zero;
                m_BottomBar.color = Color.black;
            }
        }

        public void SetLoadingCameraActive(bool active)
        {
            if (m_LoadingCamera != null)
            {
                m_LoadingCamera.gameObject.SetActive(active);
            }
            else
            {
                CLogger.LogWarn("Loading Camera is not set in VgCameraManager", LogTag.Loading);
            }
        }

        [SerializeField]
        private Camera m_MainCamera;

        [SerializeField]
        private CinemachineBrain m_CinemachineBrain;

        [SerializeField]
        private Camera m_LoadingCamera;

        public Camera MainCamera => m_MainCamera;
        public CinemachineBrain CinemachineBrain => m_CinemachineBrain;
        public Camera LoadingCamera => m_LoadingCamera;
    }
}
