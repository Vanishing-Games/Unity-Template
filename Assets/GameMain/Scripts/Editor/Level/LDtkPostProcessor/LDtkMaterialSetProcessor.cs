using Core;
using LDtkUnity;
using LDtkUnity.Editor;
using UnityEditor;
using UnityEngine;

namespace GameMain.Editor
{
    public class LDtkMaterialSetProcessor : LDtkPostprocessor
    {
        private const string DEFAULT_MATERIAL_PATH =
            "Assets/Rendering/RainRust/Materials/material_rainRust_default.mat";
        private const string WALL_MATERIAL_PATH =
            "Assets/GameMain/Rendering/Material/material_rainrust_wall.mat";

        public override int GetPostprocessOrder() => 10;

        protected override void OnPostprocessLevel(GameObject root, LdtkJson projectJson)
        {
            CLogger.LogInfo(
                $"[MaterialSetProcessor] Post process LDtk level: {root.name}",
                LogTag.LdtkLogicMapProcessor
            );

            Material targetMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                DEFAULT_MATERIAL_PATH
            );

            if (targetMaterial == null)
            {
                CLogger.LogError(
                    $"[MaterialSetProcessor] 未能在路径找到目标材质: {DEFAULT_MATERIAL_PATH}",
                    LogTag.LdtkLogicMapProcessor
                );
                return;
            }

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

            if (renderers.Length == 0)
            {
                return;
            }

            int count = 0;
            foreach (var renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = targetMaterial;
                }
                renderer.sharedMaterials = materials;
                count++;
            }

            // HARD CODE: 把AutoLayer_MineWall的材质换成墙壁材质
            Material wallMaterial = AssetDatabase.LoadAssetAtPath<Material>(WALL_MATERIAL_PATH);
            if (wallMaterial == null)
            {
                CLogger.LogError(
                    $"[MaterialSetProcessor] 未能在路径找到墙壁材质: {WALL_MATERIAL_PATH}",
                    LogTag.LdtkLogicMapProcessor
                );
                return;
            }
            var wallRenderers = root.GetComponentsInChildren<Renderer>(true);
            if (wallRenderers.Length == 0)
                return;

            foreach (var renderer in wallRenderers)
            {
                if (renderer.transform.parent.name != "AutoLayer_MineWall")
                    continue;

                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = wallMaterial;
                }
                renderer.sharedMaterials = materials;
            }

            CLogger.LogInfo(
                $"[MaterialSetProcessor] 已成功将 {root.name} 中 {count} 个 Renderer 的材质设置为 {targetMaterial.name}",
                LogTag.LDtkMaterialSetProcessor
            );
        }
    }
}
