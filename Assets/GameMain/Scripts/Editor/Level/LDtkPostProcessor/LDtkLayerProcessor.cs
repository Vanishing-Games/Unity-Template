using Core;
using Core.Extensions;
using GameMain.RunTime;
using LDtkUnity;
using LDtkUnity.Editor;
using UnityEngine;

namespace GameMain.Editor
{
    public class LDtkLayerProcessor : LDtkPostprocessor
    {
        public override int GetPostprocessOrder() => 0;

        protected override void OnPostprocessLevel(GameObject root, LdtkJson projectJson)
        {
            CLogger.LogInfo($"Post process LDtk level: {root.name}", LogTag.LdtkLogicMapProcessor);

            LDtkComponentLevel level = root.GetComponent<LDtkComponentLevel>();
            if (level == null)
                return;

            foreach (LDtkComponentLayer layer in level.LayerInstances)
            {
                if (layer == null)
                    continue;

                switch (layer.Identifier)
                {
                    case LDtkIdentifiers.LogicMap:
                        layer.gameObject.SetLayerRecursively(LayerMask.NameToLayer("LogicMap"));
                        layer.gameObject.SetTagRecursively("LogicMap");
                        layer.gameObject.SetSortingLayerRecursively("LogicMap");
                        break;
                    case LDtkIdentifiers.BGMap:
                        layer.gameObject.SetLayerRecursively(LayerMask.NameToLayer("LogicMap"));
                        layer.gameObject.SetTagRecursively("LogicMap");
                        layer.gameObject.SetSortingLayerRecursively("LogicMap");
                        break;

                    case LDtkIdentifiers.ManualTiles:
                        layer.gameObject.SetLayerRecursively(LayerMask.NameToLayer("ManualTile"));
                        layer.gameObject.SetTagRecursively("ManualTile");
                        layer.gameObject.SetSortingLayerRecursively("ManualTile");

                        break;

                    case LDtkIdentifiers.Entities:
                        layer.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Entity"));
                        layer.gameObject.SetTagRecursively("Entity");
                        layer.gameObject.SetSortingLayerRecursively("Entity");

                        break;
                    case LDtkIdentifiers.AutoTiles_BG_A:
                        layer.gameObject.SetLayerRecursively(LayerMask.NameToLayer("TileWall"));
                        layer.gameObject.SetTagRecursively("AutoTile");
                        layer.gameObject.SetSortingLayerRecursively("BackGroundTile");

                        break;
                    case LDtkIdentifiers.AutoTiles_BG_B:
                        layer.gameObject.SetLayerRecursively(LayerMask.NameToLayer("TileWall"));
                        layer.gameObject.SetTagRecursively("AutoTile");
                        layer.gameObject.SetSortingLayerRecursively("BackGroundTile");

                        break;
                    case LDtkIdentifiers.AutoTiles_Wall_A:
                        layer.gameObject.SetLayerRecursively(LayerMask.NameToLayer("AutoTile"));
                        layer.gameObject.SetTagRecursively("AutoTile");
                        layer.gameObject.SetSortingLayerRecursively("AutoTile");

                        break;
                    case LDtkIdentifiers.AutoTiles_Wall_B:
                        layer.gameObject.SetLayerRecursively(LayerMask.NameToLayer("AutoTile"));
                        layer.gameObject.SetTagRecursively("AutoTile");
                        layer.gameObject.SetSortingLayerRecursively("AutoTile");

                        break;
                    case LDtkIdentifiers.AutoLayer_Zone:
                        layer.gameObject.SetLayerRecursively(LayerMask.NameToLayer("AutoTile"));
                        layer.gameObject.SetTagRecursively("AutoTile");
                        layer.gameObject.SetSortingLayerRecursively("AutoTile");

                        break;
                    case LDtkIdentifiers.AutoLayer_MineWall:
                        layer.gameObject.SetLayerRecursively(LayerMask.NameToLayer("AutoTile"));
                        layer.gameObject.SetTagRecursively("AutoTile");
                        layer.gameObject.SetSortingLayerRecursively("AutoTile");

                        break;
                    case LDtkIdentifiers.AutoLayer_MineBG:
                        layer.gameObject.SetLayerRecursively(LayerMask.NameToLayer("TileWall"));
                        layer.gameObject.SetTagRecursively("AutoTile");
                        layer.gameObject.SetSortingLayerRecursively("BackGroundTile");

                        break;
                    case LDtkIdentifiers.AutoLayer_MinePole:
                        layer.gameObject.SetLayerRecursively(LayerMask.NameToLayer("AutoTile"));
                        layer.gameObject.SetTagRecursively("AutoTile");
                        layer.gameObject.SetSortingLayerRecursively("BackGroundTile");

                        break;
                    case LDtkIdentifiers.AutoLayer_MineDanger:
                        layer.gameObject.SetLayerRecursively(LayerMask.NameToLayer("AutoTile"));
                        layer.gameObject.SetTagRecursively("AutoTile");
                        layer.gameObject.SetSortingLayerRecursively("AutoTile");

                        break;
                }
            }
        }
    }
}
