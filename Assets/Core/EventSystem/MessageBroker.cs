using System;
using System.Collections.Generic;
using System.Linq;
using R3;
#if UNITY_EDITOR
using System.Diagnostics;
using System.Reflection;
#endif

namespace Core
{
    public enum BrokerCacheStrategy
    {
        None,
        LastValue,
        ReplayAll,
    }

#if UNITY_EDITOR
    public class SubscriberDebugInfo
    {
        private readonly WeakReference<object> m_DelegateTarget;

        public string DelegateTargetTypeName { get; }
        public bool IsUnityObject { get; }
        public string CallSiteTypeName { get; }
        public string CallSiteMethodName { get; }
        public string CallSiteFile { get; }
        public int CallSiteLineNumber { get; }
        public DateTime SubscribedAt { get; } = DateTime.UtcNow;

        public bool TryGetUnityObject(out UnityEngine.Object obj)
        {
            obj = null;
            if (!IsUnityObject)
                return false;
            if (!m_DelegateTarget.TryGetTarget(out var target))
                return false;
            obj = target as UnityEngine.Object;
            return obj != null;
        }

        public SubscriberDebugInfo(object delegateTarget, StackTrace callStack)
        {
            if (delegateTarget != null)
            {
                m_DelegateTarget = new WeakReference<object>(delegateTarget);
                DelegateTargetTypeName = delegateTarget.GetType().Name;
                IsUnityObject = delegateTarget is UnityEngine.Object;
            }

            for (var i = 0; i < callStack.FrameCount; i++)
            {
                var frame = callStack.GetFrame(i);
                var method = frame?.GetMethod();
                if (method == null)
                    continue;
                var declaringType = method.DeclaringType;
                if (declaringType == typeof(MessageBroker))
                    continue;
                if (declaringType?.Namespace?.StartsWith("R3") == true)
                    continue;
                CallSiteTypeName = declaringType?.Name ?? "?";
                CallSiteMethodName = method.Name;
                CallSiteFile = frame.GetFileName();
                CallSiteLineNumber = frame.GetFileLineNumber();
                break;
            }
        }
    }

    public readonly struct BrokerEventParameterInfo
    {
        public string Name { get; }
        public string TypeName { get; }

        public BrokerEventParameterInfo(string name, string typeName)
        {
            Name = name;
            TypeName = typeName;
        }
    }

    public enum BrokerPublishKind
    {
        Normal,
        Complete,
        ErrorStop,
        ErrorResume,
    }

    public readonly struct BrokerPublishRecord
    {
        public DateTime Timestamp { get; }
        public string Payload { get; }
        public BrokerPublishKind Kind { get; }

        /// Snapshot of SubscriberDebugInfo objects active at publish time.
        /// WeakReferences inside may expire if the subscriber is later destroyed.
        public IReadOnlyList<SubscriberDebugInfo> Receivers { get; }

        public BrokerPublishRecord(
            DateTime ts,
            string payload,
            BrokerPublishKind kind,
            IReadOnlyList<SubscriberDebugInfo> receivers
        )
        {
            Timestamp = ts;
            Payload = payload;
            Kind = kind;
            Receivers = receivers;
        }
    }

    public readonly struct BrokerEventSnapshot
    {
        public string EventTypeName { get; }
        public string FullTypeName { get; }
        public int SubscriberCount { get; }
        public IReadOnlyList<SubscriberDebugInfo> Subscribers { get; }
        public IReadOnlyList<BrokerEventParameterInfo> Parameters { get; }
        public DateTime CreatedTime { get; }
        public DateTime LastActivity { get; }
        public IReadOnlyList<BrokerPublishRecord> PublishHistory { get; }

        public BrokerEventSnapshot(
            string eventTypeName,
            string fullTypeName,
            int subscriberCount,
            IReadOnlyList<SubscriberDebugInfo> subscribers,
            IReadOnlyList<BrokerEventParameterInfo> parameters,
            DateTime createdTime,
            DateTime lastActivity,
            IReadOnlyList<BrokerPublishRecord> publishHistory
        )
        {
            EventTypeName = eventTypeName;
            FullTypeName = fullTypeName;
            SubscriberCount = subscriberCount;
            Subscribers = subscribers;
            Parameters = parameters;
            CreatedTime = createdTime;
            LastActivity = lastActivity;
            PublishHistory = publishHistory;
        }
    }
#endif

    internal class EventTypeInfo : IDisposable
    {
        public object Subject { get; set; }
        public int SubscriberCount { get; set; }
        public HashSet<object> Subscribers { get; } = new HashSet<object>();
        public Type EventType { get; set; }
        public DateTime CreatedTime { get; } = DateTime.UtcNow;
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;

#if UNITY_EDITOR
        private const int MaxHistoryCount = 100;
        private readonly List<BrokerPublishRecord> m_PublishHistory = new();
        private readonly Dictionary<object, SubscriberDebugInfo> m_SubscriberDebugInfos = new();

        public IReadOnlyList<BrokerPublishRecord> PublishHistory => m_PublishHistory;

        public void SetSubscriberDebugInfo(object observer, SubscriberDebugInfo info)
        {
            if (info != null)
                m_SubscriberDebugInfos[observer] = info;
        }

        public IReadOnlyList<SubscriberDebugInfo> GetAllSubscriberDebugInfos() =>
            m_SubscriberDebugInfos.Values.ToList();

        public void RecordPublish(string payload, BrokerPublishKind kind)
        {
            var receivers =
                m_SubscriberDebugInfos.Count > 0
                    ? (IReadOnlyList<SubscriberDebugInfo>)m_SubscriberDebugInfos.Values.ToList()
                    : Array.Empty<SubscriberDebugInfo>();
            if (m_PublishHistory.Count >= MaxHistoryCount)
                m_PublishHistory.RemoveAt(0);
            m_PublishHistory.Add(new BrokerPublishRecord(DateTime.Now, payload, kind, receivers));
        }

        public void ClearPublishHistory() => m_PublishHistory.Clear();
#endif

        public void AddSubscriber(object subscriber)
        {
            Subscribers.Add(subscriber);
            SubscriberCount++;
            LastActivity = DateTime.UtcNow;
        }

        public void RemoveSubscriber(object subscriber)
        {
            if (Subscribers.Remove(subscriber))
            {
                SubscriberCount--;
                LastActivity = DateTime.UtcNow;
#if UNITY_EDITOR
                m_SubscriberDebugInfos.Remove(subscriber);
#endif
            }
        }

        public void Dispose()
        {
            (Subject as IDisposable)?.Dispose();
            Subscribers.Clear();
            SubscriberCount = 0;
#if UNITY_EDITOR
            m_SubscriberDebugInfos.Clear();
#endif
        }

        public override string ToString()
        {
            return $"EventType: {EventType?.Name}, Subscribers: {SubscriberCount}, Created: {CreatedTime:HH:mm:ss}, LastActivity: {LastActivity:HH:mm:ss}";
        }
    }

    /// <summary>
    /// Cache Strategy is None
    /// </summary>
    public class MessageBroker
    {
        private readonly object m_Locker = new();
        private readonly Dictionary<Type, EventTypeInfo> m_EventInfos = new();

#if UNITY_EDITOR
        // These must be declared before Global so their field initializers run first.
        private static readonly object s_RegistryLock = new();
        private static readonly List<WeakReference<MessageBroker>> s_Registry = new();

        [ThreadStatic]
        private static SubscriberDebugInfo s_PendingDebugInfo;

        private string m_DebugName;
#endif

        public static MessageBroker Global { get; } = new MessageBroker("Global");

#if UNITY_EDITOR
        public static IReadOnlyList<(string Name, MessageBroker Broker)> GetRegisteredBrokers()
        {
            lock (s_RegistryLock)
            {
                s_Registry.RemoveAll(wr => !wr.TryGetTarget(out _));
                var result = new List<(string, MessageBroker)>();
                foreach (var wr in s_Registry)
                    if (wr.TryGetTarget(out var b))
                        result.Add((b.m_DebugName, b));
                return result;
            }
        }

        public IReadOnlyList<BrokerEventSnapshot> GetAllEventSnapshots()
        {
            lock (m_Locker)
            {
                return m_EventInfos
                    .Select(kvp =>
                    {
                        var eventType = kvp.Key;
                        var fields = eventType.GetFields(
                            BindingFlags.Public | BindingFlags.Instance
                        );
                        var props = eventType.GetProperties(
                            BindingFlags.Public | BindingFlags.Instance
                        );
                        var parameters = fields
                            .Select(f => new BrokerEventParameterInfo(f.Name, f.FieldType.Name))
                            .Concat(
                                props.Select(p => new BrokerEventParameterInfo(
                                    p.Name,
                                    p.PropertyType.Name
                                ))
                            )
                            .ToList();

                        return new BrokerEventSnapshot(
                            eventType.Name,
                            eventType.FullName,
                            kvp.Value.SubscriberCount,
                            kvp.Value.GetAllSubscriberDebugInfos(),
                            parameters,
                            kvp.Value.CreatedTime,
                            kvp.Value.LastActivity,
                            new List<BrokerPublishRecord>(kvp.Value.PublishHistory)
                        );
                    })
                    .ToList();
            }
        }

        public void ClearHistory()
        {
            lock (m_Locker)
                foreach (var info in m_EventInfos.Values)
                    info.ClearPublishHistory();
        }

        public void ClearEventHistory(string eventTypeName)
        {
            lock (m_Locker)
            {
                var pair = m_EventInfos.FirstOrDefault(kvp => kvp.Key.Name == eventTypeName);
                pair.Value?.ClearPublishHistory();
            }
        }

        private static string GetPayloadString(object message)
        {
            var t = message.GetType();
            var fields = t.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            if (fields.Length == 0 && props.Length == 0)
                return "(no data)";
            var parts = fields
                .Select(f => $"{f.Name}={f.GetValue(message)}")
                .Concat(props.Select(p => $"{p.Name}={p.GetValue(message)}"));
            return string.Join(", ", parts);
        }
#endif

        public MessageBroker()
            : this(string.Empty) { }

        public MessageBroker(string debugName)
        {
#if UNITY_EDITOR
            m_DebugName = string.IsNullOrEmpty(debugName) ? $"Broker#{GetHashCode()}" : debugName;
            lock (s_RegistryLock)
                s_Registry.Add(new WeakReference<MessageBroker>(this));
#endif
        }

        public Observable<T> Receive<T>()
            where T : IEvent
        {
            lock (m_Locker)
            {
                var eventType = typeof(T);
                if (!m_EventInfos.TryGetValue(eventType, out var eventInfo))
                {
                    eventInfo = new EventTypeInfo
                    {
                        Subject = CreateSubject<T>(),
                        EventType = eventType,
                        SubscriberCount = 0,
                    };
                    m_EventInfos[eventType] = eventInfo;
                }

                var subject = (ISubject<T>)eventInfo.Subject;

                return Observable.Create<T>(observer =>
                {
#if UNITY_EDITOR
                    var capturedDebugInfo = s_PendingDebugInfo;
#endif
                    var subscription = subject.Subscribe(observer);

                    lock (m_Locker)
                    {
                        eventInfo.AddSubscriber(observer);
#if UNITY_EDITOR
                        eventInfo.SetSubscriberDebugInfo(observer, capturedDebugInfo);
#endif
                    }

                    return Disposable.Create(() =>
                    {
                        subscription.Dispose();
                        lock (m_Locker)
                        {
                            eventInfo.RemoveSubscriber(observer);
                            if (eventInfo.SubscriberCount <= 0)
                            {
                                CleanupEventInfo(eventType);
                            }
                        }
                    });
                });
            }
        }

        public IDisposable Subscribe<T>(Action<T> onNext)
            where T : IEvent
        {
#if UNITY_EDITOR
            s_PendingDebugInfo = new SubscriberDebugInfo(onNext?.Target, new StackTrace(1, true));
#endif
            try
            {
                return Receive<T>().Subscribe(onNext);
            }
            finally
            {
#if UNITY_EDITOR
                s_PendingDebugInfo = null;
#endif
            }
        }

        public IDisposable Subscribe<T>(Action<T> onNext, Action<R3.Result> onCompleted)
            where T : IEvent
        {
#if UNITY_EDITOR
            s_PendingDebugInfo = new SubscriberDebugInfo(onNext?.Target, new StackTrace(1, true));
#endif
            try
            {
                return Receive<T>().Subscribe(onNext, onCompleted);
            }
            finally
            {
#if UNITY_EDITOR
                s_PendingDebugInfo = null;
#endif
            }
        }

        public IDisposable Subscribe<T>(Action<T> onNext, Action onCompleted)
            where T : IEvent
        {
#if UNITY_EDITOR
            s_PendingDebugInfo = new SubscriberDebugInfo(onNext?.Target, new StackTrace(1, true));
#endif
            try
            {
                return Receive<T>().Subscribe(onNext, _ => onCompleted());
            }
            finally
            {
#if UNITY_EDITOR
                s_PendingDebugInfo = null;
#endif
            }
        }

        public IDisposable Subscribe<T>(
            Action<T> onNext,
            Action<Exception> onError,
            Action<R3.Result> onCompleted
        )
            where T : IEvent
        {
#if UNITY_EDITOR
            s_PendingDebugInfo = new SubscriberDebugInfo(onNext?.Target, new StackTrace(1, true));
#endif
            try
            {
                return Receive<T>().Subscribe(onNext, onError, onCompleted);
            }
            finally
            {
#if UNITY_EDITOR
                s_PendingDebugInfo = null;
#endif
            }
        }

        public IDisposable Subscribe<T>(
            Action<T> onNext,
            Action<Exception> onError,
            Action onCompleted
        )
            where T : IEvent
        {
#if UNITY_EDITOR
            s_PendingDebugInfo = new SubscriberDebugInfo(onNext?.Target, new StackTrace(1, true));
#endif
            try
            {
                return Receive<T>().Subscribe(onNext, onError, _ => onCompleted());
            }
            finally
            {
#if UNITY_EDITOR
                s_PendingDebugInfo = null;
#endif
            }
        }

        public void Publish<T>(T message)
            where T : IEvent
        {
            CLogger.LogVerbose($"Publish Event: {typeof(T).Name}", LogTag.Event);

            lock (m_Locker)
            {
                var eventType = typeof(T);
                if (m_EventInfos.TryGetValue(eventType, out var eventInfo))
                {
                    ((ISubject<T>)eventInfo.Subject).OnNext(message);
                    eventInfo.LastActivity = DateTime.UtcNow;
#if UNITY_EDITOR
                    eventInfo.RecordPublish(GetPayloadString(message), BrokerPublishKind.Normal);
#endif
                }
            }
        }

        public void PublishErrorStop<T>(object errorSource, Exception error)
            where T : IEvent
        {
            CLogger.LogVerbose($"Publish Error Event: {typeof(T).Name}", LogTag.Event);

            lock (m_Locker)
            {
                var eventType = typeof(T);
                if (m_EventInfos.TryGetValue(eventType, out var eventInfo))
                {
                    var subject = (ISubject<T>)eventInfo.Subject;
                    subject.OnCompleted();

                    CLogger.LogError(
                        $"Event {typeof(T).Name} terminated with error: {error.Message}",
                        LogTag.Event
                    );
                    eventInfo.LastActivity = DateTime.UtcNow;
#if UNITY_EDITOR
                    eventInfo.RecordPublish(error.Message, BrokerPublishKind.ErrorStop);
#endif
                    CleanupEventInfo(eventType);
                }
            }
        }

        public void PublishErrorResume<T>(object errorSource, Exception error)
            where T : IEvent
        {
            CLogger.LogVerbose($"Publish Error Resume Event: {typeof(T).Name}", LogTag.Event);

            lock (m_Locker)
            {
                var eventType = typeof(T);
                if (m_EventInfos.TryGetValue(eventType, out var eventInfo))
                {
                    ((ISubject<T>)eventInfo.Subject).OnErrorResume(error);
                    eventInfo.LastActivity = DateTime.UtcNow;
#if UNITY_EDITOR
                    eventInfo.RecordPublish(error.Message, BrokerPublishKind.ErrorResume);
#endif
                }
            }
        }

        public void PublishComplete<T>(T message)
            where T : IEvent
        {
            CLogger.LogVerbose($"Publish Complete Event: {typeof(T).Name}", LogTag.Event);

            lock (m_Locker)
            {
                var eventType = typeof(T);
                if (m_EventInfos.TryGetValue(eventType, out var eventInfo))
                {
                    ((ISubject<T>)eventInfo.Subject).OnNext(message);
                    ((ISubject<T>)eventInfo.Subject).OnCompleted();
                    eventInfo.LastActivity = DateTime.UtcNow;
#if UNITY_EDITOR
                    eventInfo.RecordPublish(GetPayloadString(message), BrokerPublishKind.Complete);
#endif
                }
                CleanupEventInfo(eventType);
            }
        }

        public void Complete<T>()
            where T : IEvent
        {
            CLogger.LogVerbose($"Complete Event: {typeof(T).Name}", LogTag.Event);

            lock (m_Locker)
            {
                var eventType = typeof(T);
                if (m_EventInfos.TryGetValue(eventType, out var eventInfo))
                {
                    ((ISubject<T>)eventInfo.Subject).OnCompleted();
                    eventInfo.LastActivity = DateTime.UtcNow;
                    CleanupEventInfo(eventType);
                }
            }
        }

        private void CleanupEventInfo(Type eventType)
        {
            if (m_EventInfos.TryGetValue(eventType, out var eventInfo))
            {
                eventInfo.Dispose();
                m_EventInfos.Remove(eventType);
            }
        }

        private ISubject<T> CreateSubject<T>()
            where T : IEvent
        {
            return new Subject<T>();
        }

        public void Clear()
        {
            CLogger.LogVerbose($"Clear MessageBroker", LogTag.Event);

            lock (m_Locker)
            {
                foreach (var eventInfo in m_EventInfos.Values)
                {
                    eventInfo.Dispose();
                }
                m_EventInfos.Clear();
            }
        }

        public int GetSubscriberCount<T>()
            where T : IEvent
        {
            CLogger.LogVerbose($"Get Subscriber Count: {typeof(T).Name}", LogTag.Event);

            lock (m_Locker)
            {
                return m_EventInfos.TryGetValue(typeof(T), out var eventInfo)
                    ? eventInfo.SubscriberCount
                    : 0;
            }
        }

        public IReadOnlyCollection<object> GetSubscribers<T>()
            where T : IEvent
        {
            CLogger.LogVerbose($"Get Subscribers: {typeof(T).Name}", LogTag.Event);

            lock (m_Locker)
            {
                if (m_EventInfos.TryGetValue(typeof(T), out var eventInfo))
                {
                    return eventInfo.Subscribers.ToArray();
                }
                return new object[0];
            }
        }

        public bool HasActiveSubscribers<T>()
            where T : IEvent
        {
            CLogger.LogVerbose($"Has Active Subscribers: {typeof(T).Name}", LogTag.Event);

            lock (m_Locker)
            {
                return m_EventInfos.TryGetValue(typeof(T), out var eventInfo)
                    && eventInfo.SubscriberCount > 0;
            }
        }

#if UNITY_EDITOR
        public string GetDebugInfo()
        {
            CLogger.LogVerbose($"Get Debug Info", LogTag.Event);

            lock (m_Locker)
            {
                if (m_EventInfos.Count == 0)
                    return "[MessageBroker] No active event types";

                var info = new System.Text.StringBuilder();
                info.AppendLine(
                    $"[MessageBroker] Debug Info ({m_EventInfos.Count} active event types):"
                );
                info.AppendLine("=====================================");

                foreach (var kvp in m_EventInfos)
                {
                    var eventType = kvp.Key;
                    var eventInfo = kvp.Value;

                    info.AppendLine($"Event Type: {eventType.Name}");
                    info.AppendLine($"  Subscribers: {eventInfo.SubscriberCount}");
                    info.AppendLine($"  Created: {eventInfo.CreatedTime:yyyy-MM-dd HH:mm:ss}");
                    info.AppendLine(
                        $"  Last Activity: {eventInfo.LastActivity:yyyy-MM-dd HH:mm:ss}"
                    );

                    if (eventInfo.Subscribers.Count > 0)
                    {
                        info.AppendLine("  Subscriber Objects:");
                        foreach (var subscriber in eventInfo.Subscribers)
                        {
                            info.AppendLine(
                                $"    - {subscriber.GetType().Name} (Hash: {subscriber.GetHashCode()})"
                            );
                        }
                    }
                    info.AppendLine();
                }

                return info.ToString();
            }
        }
#endif
    }
}
