using System;
using System.Collections.Generic;
using Core.Extensions;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Core
{
    public enum ParallaxClampMode
    {
        None,
        Repeat,
        Mirror,
    }

    public class ParallaxBackground : MonoBehaviour
    {
        private void Start()
        {
            if (m_TargetCamera == null)
            {
                m_TargetCamera = Camera.main;
            }

            if (m_TargetCamera != null)
            {
                m_LastCameraPosition = m_TargetCamera.transform.position;
            }

            InitializeLayers();
        }

        private void LateUpdate()
        {
            if (m_TargetCamera == null)
            {
                return;
            }

            Vector3 cameraPosition = m_TargetCamera.transform.position;
            Vector3 cameraDelta = cameraPosition - m_LastCameraPosition;

            foreach (var layer in m_Layers)
            {
                if (layer == null || layer.layerObject == null)
                {
                    continue;
                }

                Vector3 currentPos = layer.layerObject.transform.position;
                float newX = currentPos.x + cameraDelta.x * (1 - layer.parallaxFactorX);
                float newY = currentPos.y;

                if (layer.clampModeY == ParallaxClampMode.None)
                {
                    // 计算摄像机在世界限制内的进度 t (0 到 1)
                    float t = Mathf.InverseLerp(layer.worldMinY, layer.worldMaxY, cameraPosition.y);

                    // 根据进度插值算出偏移量：最低点时偏移 maxVerticalOffset，最高点时偏移 -maxVerticalOffset
                    // (视觉效果：摄像机上升时，背景在视野中相对下沉)
                    float yOffset = Mathf.Lerp(
                        layer.maxVerticalOffset,
                        -layer.maxVerticalOffset,
                        t
                    );

                    newY = cameraPosition.y + layer.initialRelativeY + yOffset;
                }
                else
                {
                    newY -= cameraDelta.y * layer.parallaxFactorY;
                }

                layer.layerObject.transform.position = new Vector3(newX, newY, currentPos.z);

                HandleHorizontalWrapping(layer, cameraPosition);
            }

            m_LastCameraPosition = cameraPosition;
        }

        private void InitializeLayers()
        {
            m_PropertyBlock ??= new MaterialPropertyBlock();

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                    Destroy(transform.GetChild(i).gameObject);
                else
                    DestroyImmediate(transform.GetChild(i).gameObject);
            }

            if (m_Layers == null)
                return;

            for (int i = 0; i < m_Layers.Count; i++)
            {
                var layer = m_Layers[i];
                if (layer == null || layer.sprite == null)
                    continue;

                GameObject container = new($"Layer_{i}_{layer.sprite.name}");
                container.transform.SetParent(transform);

                if (m_TargetCamera != null)
                {
                    Vector3 camPos = m_TargetCamera.transform.position;
                    if (Application.isPlaying)
                    {
                        container.transform.position = new Vector3(
                            camPos.x,
                            camPos.y,
                            transform.position.z
                        );
                    }
                    else
                    {
                        container.transform.localPosition = Vector3.zero;
                    }

                    layer.initialRelativeY = container.transform.position.y - camPos.y;
                }
                else
                {
                    container.transform.localPosition = Vector3.zero;
                    layer.initialRelativeY = 0f;
                }

                layer.layerObject = container;

                layer.textureWidth = layer.sprite.bounds.size.x;
                if (layer.textureWidth <= 0)
                    continue;

                // 根据 X 轴的 clampMode 决定生成几个 Sprite
                int count = (layer.clampModeX == ParallaxClampMode.None) ? 1 : 3;
                layer.renderers = new SpriteRenderer[count];

                for (int j = 0; j < count; j++)
                {
                    GameObject child = new($"Sprite_{j}");
                    child.transform.SetParent(container.transform);

                    float scaledWidth = layer.textureWidth * layer.imageScale.x;
                    float xPos =
                        (layer.clampModeX == ParallaxClampMode.None) ? 0 : (j - 1) * scaledWidth;

                    child.transform.localPosition = new Vector3(xPos, 0, 0);
                    child.transform.localScale = new Vector3(
                        layer.imageScale.x,
                        layer.imageScale.y,
                        1
                    );

                    SpriteRenderer sr = child.AddComponent<SpriteRenderer>();
                    sr.sprite = layer.sprite;
                    sr.sortingOrder = m_Layers.Count - i;

                    layer.renderers[j] = sr;
                }
            }

            UpdateLayerProperties();

            gameObject.SetLayerRecursively(LayerMask.NameToLayer("BackGround"));
            gameObject.SetTagRecursively("BackGround");
            gameObject.SetSortingLayerRecursively("BackGround");
        }

        private void UpdateLayerProperties()
        {
            m_PropertyBlock ??= new MaterialPropertyBlock();

            if (m_Layers == null)
                return;

            foreach (var layer in m_Layers)
            {
                if (layer == null || layer.renderers == null)
                    continue;

                foreach (var sr in layer.renderers)
                {
                    if (sr == null)
                        continue;

                    if (layer.blurIntensity > 0 && m_BlurMaterial != null)
                    {
                        sr.sharedMaterial = m_BlurMaterial;
                        sr.GetPropertyBlock(m_PropertyBlock);
                        m_PropertyBlock.SetFloat(BlurIntensityProperty, layer.blurIntensity);
                        m_PropertyBlock.SetFloat(BlurModeProperty, layer.blurMode);
                        sr.SetPropertyBlock(m_PropertyBlock);
                    }
                    else
                    {
                        sr.sharedMaterial = null; // Use default sprite material
                    }
                }
            }
        }

        private void HandleHorizontalWrapping(ParallaxLayer layer, Vector3 cameraPosition)
        {
            if (layer.clampModeX == ParallaxClampMode.None || layer.renderers == null)
                return;

            float localCameraX = cameraPosition.x - layer.layerObject.transform.position.x;
            float width = layer.textureWidth;

            foreach (var sr in layer.renderers)
            {
                if (sr == null)
                    continue;

                Vector3 pos = sr.transform.localPosition;
                float offset = localCameraX - pos.x;

                if (Mathf.Abs(offset) > width * 1.5f)
                {
                    float shiftCount = Mathf.Sign(offset) * 3f;
                    pos.x += shiftCount * width;
                    sr.transform.localPosition = pos;

                    if (layer.clampModeX == ParallaxClampMode.Mirror)
                    {
                        int index = Mathf.RoundToInt(pos.x / width);
                        sr.flipX = Mathf.Abs(index) % 2 != 0;
                    }
                }
            }
        }

        private static readonly int BlurIntensityProperty = Shader.PropertyToID("_BlurIntensity");
        private static readonly int BlurModeProperty = Shader.PropertyToID("_BlurMode");
        private MaterialPropertyBlock m_PropertyBlock;

        public List<ParallaxLayer> Layers
        {
            get => m_Layers;
            set => m_Layers = value;
        }

        public Material BlurMaterial
        {
            get => m_BlurMaterial;
            set => m_BlurMaterial = value;
        }

        [SerializeField]
        private Camera m_TargetCamera;

        [SerializeField]
        private List<ParallaxLayer> m_Layers = new();

        [SerializeField]
        private Material m_BlurMaterial;

        private Vector3 m_LastCameraPosition;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_Layers == null)
                return;

            if (m_Layers.Count > 1)
            {
                m_Layers.RemoveAll(l => l == null);
                m_Layers.Sort((a, b) => b.parallaxFactorX.CompareTo(a.parallaxFactorX));
            }

            if (transform.childCount > 0)
            {
                UpdateLayerProperties();
            }
        }

        [ContextMenu("一键分配视差系数")]
        public void DistributeFactors()
        {
            if (m_Layers == null || m_Layers.Count == 0)
                return;

            Undo.RecordObject(this, "Distribute Parallax Factors");

            if (m_Layers.Count == 1)
            {
                m_Layers[0].parallaxFactorX = 0.5f;
                m_Layers[0].parallaxFactorY = 0.5f;
            }
            else
            {
                for (int i = 0; i < m_Layers.Count; i++)
                {
                    float factor = 1.0f - ((float)i / (m_Layers.Count - 1));
                    m_Layers[i].parallaxFactorX = factor;
                    m_Layers[i].parallaxFactorY = factor;
                    m_Layers[i].blurIntensity = Math.Clamp((1.0f - factor) * 10f, 0.001f, 10f);

                    m_Layers[i].clampModeX = ParallaxClampMode.Repeat;
                    m_Layers[i].clampModeY = ParallaxClampMode.None;

                    EditorUtility.SetDirty(m_Layers[i]); // 保证ScriptableObject的修改被保存
                }
            }

            if (transform.childCount > 0)
            {
                UpdateLayerProperties();
            }
        }

        [ContextMenu("一键计算并应用世界Y轴边界")]
        public void CalculateAndApplyWorldBoundsY()
        {
            if (m_Layers == null || m_Layers.Count == 0)
                return;

            Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);

            float minY = float.MaxValue;
            float maxY = float.MinValue;

            foreach (var renderer in allRenderers)
            {
                // 跳过视差背景本身，避免递归干扰
                if (
                    renderer.gameObject.CompareTag("BackGround")
                    || renderer.gameObject.layer == LayerMask.NameToLayer("BackGround")
                )
                {
                    continue;
                }

                Bounds bounds = renderer.bounds;
                if (bounds.min.y < minY)
                    minY = bounds.min.y;
                if (bounds.max.y > maxY)
                    maxY = bounds.max.y;
            }

            if (minY == float.MaxValue && maxY == float.MinValue)
            {
                Debug.LogWarning("未能找到有效的渲染器来计算世界边界。");
                return;
            }

            Undo.RecordObject(this, "Calculate World Bounds Y");

            foreach (var layer in m_Layers)
            {
                if (layer != null && layer.clampModeY == ParallaxClampMode.None)
                {
                    Undo.RecordObject(layer, "Apply Bounds To Layer");
                    layer.worldMinY = minY;
                    layer.worldMaxY = maxY;
                    EditorUtility.SetDirty(layer);
                }
            }

            Debug.Log(
                $"世界Y轴边界计算完成: MinY = {minY}, MaxY = {maxY}，已应用到所有 Y轴为 None 的图层。"
            );
        }

        [ContextMenu("生成图层对象")]
        private void GenerateLayerObjects() => Start();
#endif
    }
}
