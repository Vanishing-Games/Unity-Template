using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Core
{
    public class MessageBrokerDebuggerWindow : EditorWindow
    {
        [MenuItem("Tools/RainRust/Message Broker Debugger")]
        public static void Open()
        {
            GetWindow<MessageBrokerDebuggerWindow>("MB Debugger").Show();
        }

        private const float RefreshInterval = 0.5f;
        private const float LeftPanelWidth = 220f;

        private Vector2 m_LeftScroll;
        private Vector2 m_RightScroll;
        private string m_FilterText = "";
        private bool m_AutoRefresh = true;
        private double m_LastRefreshTime;
        private int m_SelectedBrokerIndex = -1;
        private string m_SelectedEventName;
        private bool[] m_BrokerFoldouts = System.Array.Empty<bool>();
        private readonly HashSet<long> m_ExpandedHistoryItems = new();

        private List<(string Name, IReadOnlyList<BrokerEventSnapshot> Snapshots)> m_CachedData =
            new();

        private static readonly Color s_NormalColor = new(0.8f, 0.8f, 0.8f);
        private static readonly Color s_CompleteColor = new(0.4f, 0.9f, 0.9f);
        private static readonly Color s_ErrorColor = new(1f, 0.4f, 0.4f);
        private static readonly Color s_ActiveBadgeColor = new(0.3f, 0.85f, 0.45f);
        private static readonly Color s_InactiveBadgeColor = new(0.55f, 0.55f, 0.55f);
        private static readonly Color s_SelectedRowColor = new(0.27f, 0.47f, 0.8f, 0.5f);

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            RefreshData();
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (
                m_AutoRefresh
                && EditorApplication.timeSinceStartup - m_LastRefreshTime > RefreshInterval
            )
            {
                RefreshData();
                Repaint();
            }
        }

        private void RefreshData()
        {
            var brokers = MessageBroker.GetRegisteredBrokers();
            if (m_BrokerFoldouts.Length != brokers.Count)
            {
                var old = m_BrokerFoldouts;
                m_BrokerFoldouts = new bool[brokers.Count];
                for (var i = 0; i < Mathf.Min(old.Length, m_BrokerFoldouts.Length); i++)
                    m_BrokerFoldouts[i] = old[i];
                for (var i = old.Length; i < m_BrokerFoldouts.Length; i++)
                    m_BrokerFoldouts[i] = true;
            }

            m_CachedData.Clear();
            foreach (var (name, broker) in brokers)
                m_CachedData.Add((name, broker.GetAllEventSnapshots()));

            m_LastRefreshTime = EditorApplication.timeSinceStartup;
        }

        private void OnGUI()
        {
            DrawToolbar();

            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawDivider();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();

            DrawStatusBar();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUI.BeginChangeCheck();
            m_AutoRefresh = EditorGUILayout.ToggleLeft(
                "Auto-Refresh",
                m_AutoRefresh,
                GUILayout.Width(90f)
            );

            if (GUILayout.Button("Refresh Now", EditorStyles.toolbarButton, GUILayout.Width(84f)))
            {
                RefreshData();
                Repaint();
            }

            if (
                GUILayout.Button(
                    "Clear All History",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(110f)
                )
            )
            {
                foreach (var (_, broker) in MessageBroker.GetRegisteredBrokers())
                    broker.ClearHistory();
                RefreshData();
            }

            GUILayout.FlexibleSpace();

            GUILayout.Label("Filter:", EditorStyles.toolbarButton, GUILayout.Width(42f));
            m_FilterText = EditorGUILayout.TextField(
                m_FilterText,
                EditorStyles.toolbarTextField,
                GUILayout.Width(140f)
            );
            if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(20f)))
                m_FilterText = "";

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(LeftPanelWidth));
            m_LeftScroll = EditorGUILayout.BeginScrollView(m_LeftScroll);

            for (var brokerIdx = 0; brokerIdx < m_CachedData.Count; brokerIdx++)
            {
                var (brokerName, snapshots) = m_CachedData[brokerIdx];

                var visibleSnapshots = GetFilteredSnapshots(snapshots);

                EditorGUILayout.BeginHorizontal();
                m_BrokerFoldouts[brokerIdx] = EditorGUILayout.Foldout(
                    m_BrokerFoldouts[brokerIdx],
                    $"{brokerName}  ({visibleSnapshots.Count})",
                    true,
                    EditorStyles.foldoutHeader
                );
                EditorGUILayout.EndHorizontal();

                if (!m_BrokerFoldouts[brokerIdx])
                    continue;

                EditorGUI.indentLevel++;
                if (visibleSnapshots.Count == 0)
                {
                    EditorGUILayout.LabelField(
                        "No active events",
                        EditorStyles.centeredGreyMiniLabel
                    );
                }
                else
                {
                    foreach (var snap in visibleSnapshots)
                        DrawEventRow(brokerIdx, snap);
                }
                EditorGUI.indentLevel--;

                EditorGUILayout.Space(4f);
            }

            if (m_CachedData.Count == 0)
                EditorGUILayout.LabelField(
                    "No brokers registered",
                    EditorStyles.centeredGreyMiniLabel
                );

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawEventRow(int brokerIdx, BrokerEventSnapshot snap)
        {
            var isSelected =
                m_SelectedBrokerIndex == brokerIdx && m_SelectedEventName == snap.EventTypeName;

            var rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(36f));

            if (isSelected)
            {
                EditorGUI.DrawRect(rowRect, s_SelectedRowColor);
            }

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(snap.EventTypeName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"Last: {snap.LastActivity:HH:mm:ss}",
                EditorStyles.miniLabel
            );
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            var badgeColor = snap.SubscriberCount > 0 ? s_ActiveBadgeColor : s_InactiveBadgeColor;
            var prevColor = GUI.color;
            GUI.color = badgeColor;
            GUILayout.Label(
                snap.SubscriberCount.ToString(),
                EditorStyles.boldLabel,
                GUILayout.Width(24f)
            );
            GUI.color = prevColor;

            EditorGUILayout.EndHorizontal();

            var clickRect = rowRect;
            if (
                Event.current.type == EventType.MouseDown
                && clickRect.Contains(Event.current.mousePosition)
            )
            {
                m_SelectedBrokerIndex = brokerIdx;
                m_SelectedEventName = snap.EventTypeName;
                m_RightScroll = Vector2.zero;
                Event.current.Use();
                Repaint();
            }
        }

        private void DrawDivider()
        {
            var rect = GUILayoutUtility.GetRect(
                1f,
                float.MaxValue,
                1f,
                float.MaxValue,
                GUILayout.Width(1f),
                GUILayout.ExpandHeight(true)
            );
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
        }

        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical();
            m_RightScroll = EditorGUILayout.BeginScrollView(m_RightScroll);

            if (
                m_SelectedBrokerIndex < 0
                || m_SelectedBrokerIndex >= m_CachedData.Count
                || string.IsNullOrEmpty(m_SelectedEventName)
            )
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(
                    "Select an event on the left",
                    EditorStyles.centeredGreyMiniLabel
                );
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }

            var (_, snapshots) = m_CachedData[m_SelectedBrokerIndex];
            BrokerEventSnapshot? selected = null;
            foreach (var s in snapshots)
            {
                if (s.EventTypeName == m_SelectedEventName)
                {
                    selected = s;
                    break;
                }
            }

            if (selected == null)
            {
                EditorGUILayout.LabelField(
                    $"{m_SelectedEventName}  (no longer active)",
                    EditorStyles.boldLabel
                );
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }

            var snap = selected.Value;

            EditorGUILayout.LabelField(snap.EventTypeName, EditorStyles.whiteLargeLabel);
            EditorGUILayout.LabelField(snap.FullTypeName, EditorStyles.miniLabel);
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                $"Created: {snap.CreatedTime:HH:mm:ss}    Last Activity: {snap.LastActivity:HH:mm:ss}",
                EditorStyles.miniLabel
            );

            EditorGUILayout.Space(8f);
            DrawParametersSection(snap);

            EditorGUILayout.Space(8f);
            DrawSubscribersSection(snap);

            EditorGUILayout.Space(8f);
            DrawHistorySection(snap);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawParametersSection(BrokerEventSnapshot snap)
        {
            EditorGUILayout.LabelField("── Event Parameters ──", EditorStyles.boldLabel);

            if (snap.Parameters.Count == 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("(no public fields or properties)", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
                return;
            }

            EditorGUI.indentLevel++;
            foreach (var param in snap.Parameters)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    param.Name,
                    EditorStyles.boldLabel,
                    GUILayout.Width(160f)
                );
                var prevColor = GUI.contentColor;
                GUI.contentColor = new Color(0.6f, 0.85f, 1f);
                EditorGUILayout.LabelField(param.TypeName, EditorStyles.miniLabel);
                GUI.contentColor = prevColor;
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        private void DrawSubscribersSection(BrokerEventSnapshot snap)
        {
            EditorGUILayout.LabelField(
                $"── Subscribers ({snap.SubscriberCount}) ──",
                EditorStyles.boldLabel
            );

            if (snap.Subscribers.Count == 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("(none)", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
                return;
            }

            EditorGUI.indentLevel++;
            foreach (var info in snap.Subscribers)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                var typeName = info.DelegateTargetTypeName ?? "(static / lambda)";
                EditorGUILayout.LabelField(typeName, EditorStyles.boldLabel);

                if (info.IsUnityObject)
                {
                    if (info.TryGetUnityObject(out var unityObj))
                    {
                        if (GUILayout.Button("Ping", GUILayout.Width(44f)))
                        {
                            EditorGUIUtility.PingObject(unityObj);
                            Selection.activeObject = unityObj;
                        }
                    }
                    else
                    {
                        var prev = GUI.color;
                        GUI.color = new Color(0.7f, 0.7f, 0.7f);
                        GUILayout.Label(
                            "(destroyed)",
                            EditorStyles.miniLabel,
                            GUILayout.Width(66f)
                        );
                        GUI.color = prev;
                    }
                }
                EditorGUILayout.EndHorizontal();

                if (info.CallSiteTypeName != null)
                {
                    var fileName =
                        info.CallSiteFile != null ? Path.GetFileName(info.CallSiteFile) : "?";
                    EditorGUILayout.LabelField(
                        $"{info.CallSiteTypeName}.{info.CallSiteMethodName}",
                        EditorStyles.miniLabel
                    );
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(
                        $"{fileName} : {info.CallSiteLineNumber}",
                        EditorStyles.miniLabel
                    );
                    if (
                        info.CallSiteFile != null
                        && GUILayout.Button("Open", EditorStyles.miniButton, GUILayout.Width(40f))
                    )
                    {
                        UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(
                            info.CallSiteFile,
                            info.CallSiteLineNumber
                        );
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.LabelField(
                    $"Subscribed: {info.SubscribedAt:HH:mm:ss}",
                    EditorStyles.miniLabel
                );

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2f);
            }
            EditorGUI.indentLevel--;
        }

        private void DrawHistorySection(BrokerEventSnapshot snap)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                $"── Publish History ({snap.PublishHistory.Count}) ──",
                EditorStyles.boldLabel
            );
            if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(44f)))
            {
                if (m_SelectedBrokerIndex >= 0 && m_SelectedBrokerIndex < m_CachedData.Count)
                {
                    var brokers = MessageBroker.GetRegisteredBrokers();
                    if (m_SelectedBrokerIndex < brokers.Count)
                    {
                        brokers[m_SelectedBrokerIndex]
                            .Broker.ClearEventHistory(m_SelectedEventName);
                        RefreshData();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            if (snap.PublishHistory.Count == 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("(no publishes yet)", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
                return;
            }

            EditorGUI.indentLevel++;
            for (var i = snap.PublishHistory.Count - 1; i >= 0; i--)
            {
                var record = snap.PublishHistory[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                var kindColor = record.Kind switch
                {
                    BrokerPublishKind.Complete => s_CompleteColor,
                    BrokerPublishKind.ErrorStop => s_ErrorColor,
                    BrokerPublishKind.ErrorResume => s_ErrorColor,
                    _ => s_NormalColor,
                };

                // Header row: timestamp + kind badge
                EditorGUILayout.BeginHorizontal();
                var prevColor = GUI.contentColor;
                GUI.contentColor = kindColor;
                EditorGUILayout.LabelField(
                    $"{record.Timestamp:yyyy-MM-dd HH:mm:ss.fff}",
                    EditorStyles.miniLabel,
                    GUILayout.Width(170f)
                );
                EditorGUILayout.LabelField(
                    $"[{record.Kind}]",
                    EditorStyles.miniLabel,
                    GUILayout.Width(90f)
                );
                GUI.contentColor = prevColor;
                EditorGUILayout.EndHorizontal();

                // Payload row
                if (!string.IsNullOrEmpty(record.Payload))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(
                        "Payload:",
                        EditorStyles.miniLabel,
                        GUILayout.Width(52f)
                    );
                    EditorGUILayout.LabelField(record.Payload, EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                }

                // Receivers foldout
                if (record.Receivers != null && record.Receivers.Count > 0)
                {
                    var key = record.Timestamp.Ticks;
                    var isExpanded = m_ExpandedHistoryItems.Contains(key);
                    var receiverLabel =
                        $"Receivers ({record.Receivers.Count}): "
                        + string.Join(
                            ", ",
                            record.Receivers.Select(r => r.DelegateTargetTypeName ?? "(lambda)")
                        );
                    var newExpanded = EditorGUILayout.Foldout(
                        isExpanded,
                        receiverLabel,
                        true,
                        EditorStyles.foldout
                    );
                    if (newExpanded != isExpanded)
                    {
                        if (newExpanded)
                            m_ExpandedHistoryItems.Add(key);
                        else
                            m_ExpandedHistoryItems.Remove(key);
                    }

                    if (newExpanded)
                    {
                        EditorGUI.indentLevel++;
                        foreach (var receiver in record.Receivers)
                        {
                            EditorGUILayout.BeginHorizontal();
                            var rName = receiver.DelegateTargetTypeName ?? "(lambda)";
                            EditorGUILayout.LabelField(rName, EditorStyles.miniLabel);

                            if (receiver.IsUnityObject)
                            {
                                if (receiver.TryGetUnityObject(out var unityObj))
                                {
                                    if (
                                        GUILayout.Button(
                                            "Ping",
                                            EditorStyles.miniButton,
                                            GUILayout.Width(36f)
                                        )
                                    )
                                    {
                                        EditorGUIUtility.PingObject(unityObj);
                                        Selection.activeObject = unityObj;
                                    }
                                }
                                else
                                {
                                    prevColor = GUI.contentColor;
                                    GUI.contentColor = new Color(0.6f, 0.6f, 0.6f);
                                    GUILayout.Label(
                                        "(destroyed)",
                                        EditorStyles.miniLabel,
                                        GUILayout.Width(66f)
                                    );
                                    GUI.contentColor = prevColor;
                                }
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        EditorGUI.indentLevel--;
                    }
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(1f);
            }
            EditorGUI.indentLevel--;
        }

        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            var totalEvents = 0;
            var totalSubs = 0;
            foreach (var (_, snapshots) in m_CachedData)
            {
                totalEvents += snapshots.Count;
                foreach (var s in snapshots)
                    totalSubs += s.SubscriberCount;
            }
            var timeSince = EditorApplication.timeSinceStartup - m_LastRefreshTime;
            EditorGUILayout.LabelField(
                $"Brokers: {m_CachedData.Count}   Events: {totalEvents}   Subscribers: {totalSubs}   Refreshed: {timeSince:F1}s ago",
                EditorStyles.miniLabel
            );
            EditorGUILayout.EndHorizontal();
        }

        private List<BrokerEventSnapshot> GetFilteredSnapshots(
            IReadOnlyList<BrokerEventSnapshot> snapshots
        )
        {
            var result = new List<BrokerEventSnapshot>();
            foreach (var s in snapshots)
            {
                if (
                    string.IsNullOrEmpty(m_FilterText)
                    || s.EventTypeName.IndexOf(
                        m_FilterText,
                        System.StringComparison.OrdinalIgnoreCase
                    ) >= 0
                )
                    result.Add(s);
            }
            return result;
        }
    }
}
