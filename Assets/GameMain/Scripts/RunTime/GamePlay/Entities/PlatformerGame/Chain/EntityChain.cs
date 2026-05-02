using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameMain.RunTime
{
    public class EntityChain : MonoBehaviour
    {
        [System.Serializable]
        public struct VelocityForceStep
        {
            public float MinVelocity;
            public float Force;
        }

        public enum ChainState
        {
            Stopped,
            Swinging,
            Transitioning,
        }

        private void Start()
        {
            if (m_Joints.Count == m_ChainLength && m_ChainLength > 0)
            {
                foreach (var joint in m_Joints)
                {
                    if (joint != null)
                    {
                        var trigger = joint.GetComponent<ChainJointTrigger>();
                        if (trigger != null)
                        {
                            trigger.Initialize(this);
                        }
                    }
                }

                CacheJointRbs();

                m_CurrentState = ChainState.Stopped;
                SetJointsKinematic();
            }
            else
            {
                Setup();
            }
        }

        private void Update()
        {
            if (m_CooldownTimer > 0)
            {
                m_CooldownTimer -= Time.deltaTime;
            }

            if (m_CurrentState == ChainState.Swinging)
            {
                if (m_EnableAutoRestore && m_CooldownTimer <= 0)
                {
                    StartTransition();
                }
            }
            else if (m_CurrentState == ChainState.Transitioning)
            {
                m_TransitionTimer += Time.deltaTime;
                float t = Mathf.Clamp01(m_TransitionTimer / m_TransitionDuration);

                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                for (int i = 0; i < m_Joints.Count; i++)
                {
                    if (m_Joints[i] == null)
                    {
                        continue;
                    }

                    m_Joints[i].transform.localPosition = Vector3.Lerp(
                        m_TransitionStartPos[i],
                        m_InitialLocalPos[i],
                        smoothT
                    );
                    m_Joints[i].transform.localRotation = Quaternion.Lerp(
                        m_TransitionStartRot[i],
                        Quaternion.identity,
                        smoothT
                    );
                }

                if (t >= 1f)
                {
                    m_CurrentState = ChainState.Stopped;
                }
            }
        }

        private void StartTransition()
        {
            m_CurrentState = ChainState.Transitioning;
            m_TransitionTimer = 0f;

            m_TransitionStartPos.Clear();
            m_TransitionStartRot.Clear();

            foreach (var joint in m_Joints)
            {
                if (joint == null)
                {
                    continue;
                }

                m_TransitionStartPos.Add(joint.transform.localPosition);
                m_TransitionStartRot.Add(joint.transform.localRotation);
            }

            SetJointsKinematic();
        }

        private void SetJointsKinematic()
        {
            foreach (var rb in m_JointRbs)
            {
                if (rb == null)
                {
                    continue;
                }

                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }

        private void SetJointsDynamic()
        {
            foreach (var rb in m_JointRbs)
            {
                if (rb == null)
                {
                    continue;
                }

                rb.bodyType = RigidbodyType2D.Dynamic;
            }
        }

        public void Setup()
        {
            Clear();

            if (m_JointPrefab == null)
            {
                Debug.LogError($"[EntityChain] JointPrefab is not set on {gameObject.name}.", this);
                return;
            }

            float halfSpacing = m_JointSpacing * 0.5f;

            m_Anchor = new GameObject("ChainAnchor");
            m_Anchor.transform.SetParent(transform);
            m_Anchor.transform.localPosition = Vector3.zero;

            var anchorRb = m_Anchor.AddComponent<Rigidbody2D>();
            anchorRb.bodyType = RigidbodyType2D.Dynamic;

            var anchorHinge = m_Anchor.AddComponent<HingeJoint2D>();
            anchorHinge.anchor = new Vector2(0f, halfSpacing);
            anchorHinge.connectedBody = null;
            anchorHinge.autoConfigureConnectedAnchor = true;

            Rigidbody2D previousRb = anchorRb;
            bool isFirstJoint = true;

            for (int i = 0; i < m_ChainLength; i++)
            {
                GameObject joint = Instantiate(m_JointPrefab, transform);
                joint.name = $"ChainJoint_{i}";
                joint.transform.localPosition = new Vector3(0f, -i * m_JointSpacing, 0f);

                var collider = joint.GetComponent<Collider2D>();
                if (collider != null)
                {
                    collider.isTrigger = true;
                }

                var trigger = joint.AddComponent<ChainJointTrigger>();
                trigger.Initialize(this);

                var hinge = joint.GetComponent<HingeJoint2D>();
                if (hinge == null)
                {
                    hinge = joint.AddComponent<HingeJoint2D>();
                }

                hinge.anchor = new Vector2(0f, halfSpacing);
                hinge.connectedBody = previousRb;

                if (isFirstJoint)
                {
                    hinge.autoConfigureConnectedAnchor = false;
                    hinge.connectedAnchor = new Vector2(0f, halfSpacing);
                    isFirstJoint = false;
                }
                else
                {
                    hinge.autoConfigureConnectedAnchor = true;
                }

                previousRb = joint.GetComponent<Rigidbody2D>();
                m_Joints.Add(joint);
            }

            CacheJointRbs();
            m_CurrentState = ChainState.Stopped;

            if (Application.isPlaying)
            {
                SetJointsKinematic();
            }
        }

        public void Clear()
        {
            foreach (var joint in m_Joints)
            {
                if (joint == null)
                {
                    continue;
                }

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(joint);
                }
                else
#endif
                {
                    Destroy(joint);
                }
            }
            m_Joints.Clear();
            m_JointRbs.Clear();
            m_InitialLocalPos.Clear();

            if (m_Anchor != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(m_Anchor);
                }
                else
#endif
                {
                    Destroy(m_Anchor);
                }

                m_Anchor = null;
            }

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(child);
                }
                else
#endif
                {
                    Destroy(child);
                }
            }
        }

        private void CacheJointRbs()
        {
            m_JointRbs.Clear();
            m_InitialLocalPos.Clear();

            for (int i = 0; i < m_Joints.Count; i++)
            {
                var joint = m_Joints[i];
                if (joint != null)
                {
                    var rb = joint.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        m_JointRbs.Add(rb);
                    }

                    m_InitialLocalPos.Add(new Vector3(0f, -i * m_JointSpacing, 0f));
                }
            }
        }

        public void HandleJointTriggerEnter(Collider2D other, Rigidbody2D jointRb)
        {
            if (m_CooldownTimer > 0f)
            {
                return;
            }

            Rigidbody2D otherRb = other.attachedRigidbody;
            if (otherRb == null)
            {
                return;
            }

            Vector2 velocity = otherRb.linearVelocity;
            float absVelX = Mathf.Abs(velocity.x);
            if (absVelX < 0.1f)
            {
                return;
            }

            float mappedForce = m_ImpactForceMultiplier;
            if (m_ForceSteps != null && m_ForceSteps.Count > 0)
            {
                float maxMinVel = -1f;
                foreach (var step in m_ForceSteps)
                {
                    if (absVelX >= step.MinVelocity && step.MinVelocity > maxMinVel)
                    {
                        mappedForce = step.Force;
                        maxMinVel = step.MinVelocity;
                    }
                }
            }

            int jointIndex = m_JointRbs.IndexOf(jointRb);
            if (jointIndex == -1)
            {
                return;
            }

            int segmentCount = Mathf.Max(1, m_ForceSegments);
            int segmentIndex = Mathf.FloorToInt((float)jointIndex / m_Joints.Count * segmentCount);
            int targetIndex = Mathf.FloorToInt((segmentIndex + 0.5f) * ((float)m_Joints.Count / segmentCount));
            targetIndex = Mathf.Clamp(targetIndex, 0, m_JointRbs.Count - 1);

            SetJointsDynamic();

            Vector2 appliedForce = new Vector2(Mathf.Sign(velocity.x) * mappedForce, 0f);
            m_JointRbs[targetIndex].AddForce(appliedForce, ForceMode2D.Impulse);

            m_CurrentState = ChainState.Swinging;
            m_CooldownTimer = m_SwingCooldown;
        }

        [Header("Settings")]
        [SerializeField]
        private GameObject m_JointPrefab;

        [SerializeField]
        private int m_ChainLength = 5;

        [Tooltip("Distance between each joint center (match joint prefab height)")]
        [SerializeField]
        private float m_JointSpacing = 1f;

        [SerializeField]
        private bool m_EnableAutoRestore = true;

        [SerializeField]
        private int m_ForceSegments = 3;

        [Header("Physics Settings")]
        [SerializeField]
        private float m_ImpactForceMultiplier = 5f;

        [SerializeField]
        private List<VelocityForceStep> m_ForceSteps = new();

        [SerializeField]
        private float m_SwingCooldown = 0.5f;

        [Tooltip("Duration to smoothly return the chain to its initial vertical position")]
        [SerializeField]
        private float m_TransitionDuration = 1.0f;

        [Header("Runtime State")]
        [SerializeField]
        private ChainState m_CurrentState = ChainState.Stopped;

        [SerializeField]
        private List<GameObject> m_Joints = new();

        public IReadOnlyList<GameObject> Joints => m_Joints;

        private GameObject m_Anchor;
        private List<Rigidbody2D> m_JointRbs = new();

        private float m_CooldownTimer = 0f;
        private float m_TransitionTimer = 0f;

        private List<Vector3> m_InitialLocalPos = new();
        private List<Vector3> m_TransitionStartPos = new();
        private List<Quaternion> m_TransitionStartRot = new();
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(EntityChain))]
    public class EntityChainEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EntityChain chain = (EntityChain)target;

            EditorGUILayout.Space();

            if (GUILayout.Button("Build Chain"))
            {
                Undo.RegisterFullObjectHierarchyUndo(chain.gameObject, "Build Chain");
                chain.Setup();
                EditorUtility.SetDirty(chain.gameObject);
            }

            if (GUILayout.Button("Clear Chain"))
            {
                Undo.RegisterFullObjectHierarchyUndo(chain.gameObject, "Clear Chain");
                chain.Clear();
                EditorUtility.SetDirty(chain.gameObject);
            }
        }
    }
#endif
}
