#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using Core;
using UnityEngine;

public class CollideAndSlideDebugger : MonoSingletonPersistent<CollideAndSlideDebugger>
{
    public bool ShowCapsuleCast = true;
    public bool ShowHitInfo = true;
    public Color CapsuleCastColor = Color.yellow;
    public Color HitPointColor = Color.red;
    public Color HitNormalColor = Color.green;

    private RaycastHit2D mCurrentHit;
    private Vector2 mCastOrigin;
    private Vector2 mCastDirection;
    private float mCastDistance;
    private Vector2 mCapsuleSize;
    private CapsuleDirection2D mCapsuleDirection;
    private bool mHasHit;

    public void UpdateCapsuleCastInfo(
        RaycastHit2D hit,
        Vector2 origin,
        Vector2 direction,
        float distance,
        Vector2 capsuleSize,
        CapsuleDirection2D capsuleDirection
    )
    {
        mCurrentHit = hit;
        mCastOrigin = origin;
        mCastDirection = direction;
        mCastDistance = distance;
        mCapsuleSize = capsuleSize;
        mCapsuleDirection = capsuleDirection;
        mHasHit = hit.collider != null;
    }

    private void OnDrawGizmos()
    {
        if (!ShowCapsuleCast)
            return;

        if (mHasHit)
        {
            // Draw capsule cast ray
            Gizmos.color = CapsuleCastColor;
            Gizmos.DrawLine(mCastOrigin, mCastOrigin + mCastDirection * mCastDistance);

            // Draw hit point
            Gizmos.color = HitPointColor;
            Gizmos.DrawSphere(mCurrentHit.point, 0.05f);

            // Draw hit normal
            Gizmos.color = HitNormalColor;
            Gizmos.DrawLine(mCurrentHit.point, mCurrentHit.point + mCurrentHit.normal * 0.5f);

            // Draw capsule at hit position
            DrawCapsule(
                mCastOrigin + mCastDirection * mCurrentHit.distance,
                mCapsuleSize,
                mCapsuleDirection,
                Color.red
            );
        }
        else
        {
            // Draw capsule cast ray when no hit
            Gizmos.color = CapsuleCastColor * 0.5f;
            Gizmos.DrawLine(mCastOrigin, mCastOrigin + mCastDirection * mCastDistance);
        }
    }

    private void DrawCapsule(
        Vector2 center,
        Vector2 size,
        CapsuleDirection2D direction,
        Color color
    )
    {
        Gizmos.color = color;

        float radius = direction == CapsuleDirection2D.Vertical ? size.x / 2f : size.y / 2f;
        float height = direction == CapsuleDirection2D.Vertical ? size.y : size.x;

        if (height < radius * 2)
        {
            // Draw as circle if height is too small
            DrawCircle(center, radius, 16);
            return;
        }

        float cylinderHeight = height - radius * 2;
        Vector2 offset =
            direction == CapsuleDirection2D.Vertical
                ? new Vector2(0, cylinderHeight / 2f)
                : new Vector2(cylinderHeight / 2f, 0);

        Vector2 top = center + offset;
        Vector2 bottom = center - offset;

        // Draw circles at both ends
        DrawCircle(top, radius, 16);
        DrawCircle(bottom, radius, 16);

        // Draw lines connecting the circles
        if (direction == CapsuleDirection2D.Vertical)
        {
            Gizmos.DrawLine(
                new Vector3(top.x - radius, top.y),
                new Vector3(bottom.x - radius, bottom.y)
            );
            Gizmos.DrawLine(
                new Vector3(top.x + radius, top.y),
                new Vector3(bottom.x + radius, bottom.y)
            );
        }
        else
        {
            Gizmos.DrawLine(
                new Vector3(top.x, top.y - radius),
                new Vector3(bottom.x, bottom.y - radius)
            );
            Gizmos.DrawLine(
                new Vector3(top.x, top.y + radius),
                new Vector3(bottom.x, bottom.y + radius)
            );
        }
    }

    private void DrawCircle(Vector2 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector2(Mathf.Cos(0), Mathf.Sin(0)) * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = Mathf.Deg2Rad * angleStep * i;
            Vector3 newPoint = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    private void OnGUI()
    {
        if (!ShowHitInfo || !mHasHit)
            return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.BeginVertical("box");

        GUILayout.Label(
            "<b>Collide and Slide Debug Info</b>",
            new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 }
        );
        GUILayout.Space(5);

        GUILayout.Label(
            $"<color=yellow>Hit Object:</color> {mCurrentHit.collider.gameObject.name}",
            new GUIStyle(GUI.skin.label) { richText = true }
        );
        GUILayout.Label(
            $"<color=yellow>Hit Point:</color> {mCurrentHit.point}",
            new GUIStyle(GUI.skin.label) { richText = true }
        );
        GUILayout.Label(
            $"<color=yellow>Hit Normal:</color> {mCurrentHit.normal}",
            new GUIStyle(GUI.skin.label) { richText = true }
        );
        GUILayout.Label(
            $"<color=yellow>Hit Distance:</color> {mCurrentHit.distance:F4}",
            new GUIStyle(GUI.skin.label) { richText = true }
        );
        GUILayout.Label(
            $"<color=yellow>Cast Origin:</color> {mCastOrigin}",
            new GUIStyle(GUI.skin.label) { richText = true }
        );
        GUILayout.Label(
            $"<color=yellow>Cast Direction:</color> {mCastDirection}",
            new GUIStyle(GUI.skin.label) { richText = true }
        );

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
#endif
