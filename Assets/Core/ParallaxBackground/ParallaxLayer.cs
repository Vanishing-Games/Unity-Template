using System;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(fileName = "NewParallaxLayer", menuName = "Parallax/Layer")]
    public class ParallaxLayer : VgSerializedScriptableObject
    {
        [Tooltip("背景层渲染的图片")]
        public Sprite sprite;

        [Tooltip("水平视差系数 (0: 随相机移动/无穷远, 1: 在世界空间固定/近处)"), Range(0, 1)]
        public float parallaxFactorX;

        [Tooltip("垂直视差系数 (仅在 Y轴ClampMode 不为 None 时生效)"), Range(0, 1)]
        public float parallaxFactorY;

        [Header("边界处理")]
        [Tooltip("边界处理模式 (水平方向)")]
        public ParallaxClampMode clampModeX = ParallaxClampMode.Repeat;

        [Tooltip("边界处理模式 (垂直方向)")]
        public ParallaxClampMode clampModeY = ParallaxClampMode.None;

        [Header("Y轴 None模式 设置")]
        [Tooltip("图片最高上下偏移量 (根据世界最高/最低处插值)")]
        public float maxVerticalOffset = 2f;

        [Tooltip("世界最高处")]
        public float worldMaxY = 10f;

        [Tooltip("世界最低处")]
        public float worldMinY = -10f;

        [Header("视觉特效")]
        [Tooltip("模糊模式,0=Gaussian,1=Kawase"), Range(0, 1)]
        public float blurMode;

        [Tooltip("高斯模糊强度"), Range(0.001f, 10f)]
        public float blurIntensity = 0.001f;

        [NonSerialized, HideInInspector]
        public GameObject layerObject;

        [NonSerialized, HideInInspector]
        public SpriteRenderer[] renderers;

        [NonSerialized, HideInInspector]
        public float textureWidth;

        [NonSerialized, HideInInspector]
        public float initialRelativeY; // 用于记录与摄像机的初始相对Y坐标

        [Header("图片设置")]
        public Vector2 imageScale = Vector2.one;
    }
}
