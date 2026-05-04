using Core;
using Core.Extensions;
using FMOD.Studio;
using GameMain.RunTime;
using LDtkUnity;
using LDtkUnity.Editor;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using CameraMode = GameMain.LDtk.CameraMode;

namespace GameMain.Editor
{
    public class LDtkRoomProcessor : LDtkPostprocessor
    {
        public override int GetPostprocessOrder() => 4;

        private class RoomContext
        {
            public LDtkComponentLevel Level;
            public LevelRoom Room;
            public CinemachineCamera VCam = null;

            public RoomContext(LDtkComponentLevel level, LevelRoom levelRoom)
            {
                Level = level;
                Room = levelRoom;
            }
        }

        protected override void OnPostprocessLevel(GameObject root, LdtkJson projectJson) =>
            Result
                .Success(root)
                .Tap(r =>
                    CLogger.LogInfo($"Post process LDtk level: {r.name}", LogTag.LdtkRoomProcessor)
                )
                .Map(r => r.GetComponent<LDtkComponentLevel>())
                .Tap(OnProcessRoomCamera);

        private void OnProcessRoomCamera(LDtkComponentLevel level) =>
            Result
                .Success(new RoomContext(level, level.gameObject.AddComponent<LevelRoom>()))
                .Tap(ctx =>
                {
                    ctx.Room.BorderBounds = ctx.Level.BorderBounds;
                    EditorUtility.SetDirty(ctx.Room);
                })
                .Tap(ApplyCameraModeFields)
                .Map(CreateVirtualCamera)
                .Tap(ApplyCameraCommonSettings)
                .Tap(ApplyCameraFollow)
                .Tap(ApplyFixedCameraSettings)
                .Tap(ApplyConfinerSettings)
                .Tap(ApplyLayerTagSettings)
                .Tap(AddLevelBoundsTrigger);

        private void ApplyCameraModeFields(RoomContext ctx)
        {
            var bounds = ctx.Room.BorderBounds;

            // Rooms larger than 40*23 should use follow mode.
            ctx.Room.CameraMode =
                bounds.size.x <= 40f && bounds.size.y <= 23f ? CameraMode.Fixed : CameraMode.Follow;
            EditorUtility.SetDirty(ctx.Room);
        }

        private RoomContext CreateVirtualCamera(RoomContext ctx)
        {
            var vCam = new GameObject(
                $"{ctx.Level.name}_VirtualCamera"
            ).AddComponent<CinemachineCamera>();
            vCam.transform.SetParent(ctx.Level.transform);
            ctx.VCam = vCam;
            ctx.Room.VirtualCamera = vCam;

            vCam.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            vCam.Lens.OrthographicSize = 11.25f; // This matches half of 22.5 (close to 23)
            vCam.Lens.NearClipPlane = 0.1f;
            vCam.Lens.FarClipPlane = 5000f;

            var composer = vCam.GetComponent<CinemachinePositionComposer>();

            //if (composer != null)
            //{
            //    // --- 修改限制框 (Composition) ---
            //    // 注意：Unity 6 中取消了 m_ 前缀，改为大写开头的属性

            //    // --- 修改延迟/阻尼 (Damping) ---
            //    composer.Damping.x = 1.5f;
            //    composer.Damping.y = 1.5f;
            //}

            // Default settings
            if (ctx.Level.GetComponentInParent<LDtkComponentWorld>().Identifier == "Chapter_Snake")
            {
                vCam.Lens.OrthographicSize = 8f;
            }

            EditorUtility.SetDirty(ctx.Room);
            return ctx;
        }

        private void ApplyCameraCommonSettings(RoomContext ctx)
        {
            //CinemachineFollow vCamFollow = ctx.VCam.gameObject.AddComponent<CinemachineFollow>();
            //vCamFollow.
        }

        private void ApplyCameraFollow(RoomContext ctx)
        {
            if (ctx.Room.CameraMode == CameraMode.Follow)
                ctx.VCam.gameObject.AddComponent<CinemachinePositionComposer>();
        }

        private void ApplyFixedCameraSettings(RoomContext ctx)
        {
            if (ctx.Room.CameraMode != CameraMode.Fixed)
                return;

            var bounds = ctx.Room.BorderBounds;
            ctx.VCam.transform.position = bounds.center + Vector3.back * 10f;
        }

        private void ApplyConfinerSettings(RoomContext ctx)
        {
            LDtkComponentLayer logicMapLayer = null;
            foreach (var layer in ctx.Level.LayerInstances)
            {
                if (layer != null && layer.Identifier == LDtkIdentifiers.LogicMap)
                {
                    logicMapLayer = layer;
                    break;
                }
            }

            if (logicMapLayer == null)
            {
                CLogger.LogWarn(
                    $"Level {ctx.Level.name} does not have a LogicMap layer.",
                    LogTag.LdtkRoomProcessor
                );
                return;
            }

            if (!logicMapLayer.gameObject.TryGetComponent<PolygonCollider2D>(out var collider))
            {
                collider = logicMapLayer.gameObject.AddComponent<PolygonCollider2D>();
            }

            collider.isTrigger = true;

            Bounds bounds = ctx.Level.BorderBounds;
            Vector3 localMin = logicMapLayer.transform.InverseTransformPoint(bounds.min);
            Vector3 localMax = logicMapLayer.transform.InverseTransformPoint(bounds.max);

            collider.points = new Vector2[]
            {
                new(localMin.x, localMin.y),
                new(localMax.x, localMin.y),
                new(localMax.x, localMax.y),
                new(localMin.x, localMax.y),
            };

            var confiner = ctx.VCam.gameObject.AddComponent<CinemachineConfiner2D>();
            confiner.BoundingShape2D = collider;

            var overSetting = new CinemachineConfiner2D.OversizeWindowSettings
            {
                Enabled = true,
                MaxWindowSize = 0,
                Padding = 0,
            };
            confiner.OversizeWindow = overSetting;

            EditorUtility.SetDirty(logicMapLayer);
            EditorUtility.SetDirty(ctx.VCam);
        }

        private void AddLevelBoundsTrigger(RoomContext ctx)
        {
            Bounds bounds = ctx.Level.BorderBounds;
            Vector2 localCenter = (Vector2)(bounds.center - ctx.Level.transform.position);

            var col = ctx.Level.gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.offset = localCenter;
            col.size = bounds.size;
            ctx.Level.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

            ctx.Level.gameObject.AddComponent<LevelBoundsTrigger>();
            EditorUtility.SetDirty(ctx.Level.gameObject);
        }

        private void ApplyLayerTagSettings(RoomContext ctx)
        {
            // Find the LogicMap layer
            LDtkComponentLayer logicMapLayer = null;
            foreach (var layer in ctx.Level.LayerInstances)
            {
                if (layer != null && layer.Identifier == LDtkIdentifiers.LogicMap)
                {
                    logicMapLayer = layer;
                    break;
                }
            }

            if (logicMapLayer == null)
            {
                CLogger.LogWarn(
                    $"Level {ctx.Level.name} does not have a LogicMap layer.",
                    LogTag.LdtkRoomProcessor
                );
                return;
            }

            var transform = logicMapLayer.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetLayerRecursively(LayerMask.NameToLayer("Wall"));
                transform.GetChild(i).gameObject.SetTagRecursively("Wall");
            }
        }
    }
}
