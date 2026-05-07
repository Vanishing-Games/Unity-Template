using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core;
using LDtkUnity;
using LDtkUnity.Editor;
using UnityEditor;
using UnityEngine;
using Logger = Core.CLogger;

namespace GameMain.Editor
{
    public class LDtkChapterBackgroundProcessor : LDtkPostprocessor
    {
        public override int GetPostprocessOrder() => 4;

        protected override void OnPostprocessLevel(GameObject root, LdtkJson projectJson)
        {
            CLogger.LogInfo(
                $"Post process LDtk level: {root.name}",
                LogTag.LDtkTransitionProcessor
            );
            LDtkComponentLevel level = root.GetComponent<LDtkComponentLevel>();

            for (int i = 0; i < level.transform.childCount; i++)
            {
                var child = level.transform.GetChild(i);
                if (child.name.EndsWith("BgColor"))
                {
                    var sr = child.GetComponent<SpriteRenderer>();
                    sr.color = new Color(0, 0, 0, 0);
                    child.gameObject.SetActive(false);
                }
            }
        }

        protected override void OnPostprocessProject(GameObject project)
        {
            if (project == null)
            {
                return;
            }

            foreach (Transform chapter in project.transform)
            {
                this.SetupBackgroundForChapter(chapter);
            }
        }

        private void SetupBackgroundForChapter(Transform chapter)
        {
            CLogger.LogVerbose(
                "Start creating background for:" + chapter,
                LogTag.LDtkChapterBackgroundProcessor
            );

            string chapterName = chapter.name;
            string[] guids = AssetDatabase.FindAssets(
                "t:ParallaxLayer",
                new[] { m_BackgroundsPath }
            );

            CLogger.LogVerbose(
                "Found BackgroundLayers with guid:" + guids,
                LogTag.LDtkChapterBackgroundProcessor
            );

            List<ParallaxLayer> layers = new();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(path);

                if (fileName.StartsWith($"LDtk_background_{chapterName}_"))
                {
                    ParallaxLayer layer = AssetDatabase.LoadAssetAtPath<ParallaxLayer>(path);
                    if (layer != null)
                    {
                        layers.Add(layer);
                    }
                }
            }

            if (layers.Count == 0)
            {
                return;
            }

            layers = layers.OrderByDescending(l => l.name).ToList();
            layers.Reverse();

            string bgName = $"LDtk_background_{chapterName}";
            Transform bgTransform = chapter.Find(bgName);
            GameObject bgObj;

            if (bgTransform == null)
            {
                bgObj = new GameObject(bgName);
                bgObj.transform.SetParent(chapter);
                bgObj.transform.localPosition = Vector3.zero;
            }
            else
            {
                bgObj = bgTransform.gameObject;
            }

            if (!bgObj.TryGetComponent<ParallaxBackground>(out var parallax))
            {
                parallax = bgObj.AddComponent<ParallaxBackground>();
            }

            parallax.Layers = layers;

            var blurMaterial = AssetDatabase.LoadAssetAtPath<Material>(m_BlurMaterialPath);
            if (blurMaterial != null)
            {
                parallax.BlurMaterial = blurMaterial;
            }

            Logger.LogInfo(
                $"Successfully setup background for chapter: {chapterName}",
                LogTag.LdtkProcessor
            );
        }

        private readonly string m_BackgroundsPath = "Assets/Resources/BackGrounds";
        private readonly string m_BlurMaterialPath =
            "Assets/Shaders/SpriteBlur/material_spriteBlur_default.mat";
    }
}
