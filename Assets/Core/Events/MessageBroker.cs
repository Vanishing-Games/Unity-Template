using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using R3;
using UnityEngine;

namespace Core
{
    public enum BrokerCacheStrategy
    {
        None,
        LastValue,
        ReplayAll,
    }

    /// <summary>
    /// 事件类型信息，包含Subject、引用计数和订阅者列表
    /// </summary>
    internal class EventTypeInfo : IDisposable
    {
        public object Subject { get; set; }
        public int SubscriberCount { get; set; }
        public HashSet<object> Subscribers { get; } = new HashSet<object>();
        public Type EventType { get; set; }
        public DateTime CreatedTime { get; } = DateTime.UtcNow;
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;

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
            }
        }

        public void Dispose()
        {
            (Subject as IDisposable)?.Dispose();
            Subscribers.Clear();
            SubscriberCount = 0;
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

        public static MessageBroker Global { get; } = new MessageBroker();

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
                    var subscription = subject.Subscribe(observer);

                    lock (m_Locker)
                    {
                        eventInfo.AddSubscriber(observer);
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

        public void Publish<T>(T message)
            where T : IEvent
        {
            Logger.EditorLogVerbose($"Publish Event: {typeof(T).Name}", LogTag.Event);

            lock (m_Locker)
            {
                var eventType = typeof(T);
                if (m_EventInfos.TryGetValue(eventType, out var eventInfo))
                {
                    ((ISubject<T>)eventInfo.Subject).OnNext(message);
                    eventInfo.LastActivity = DateTime.UtcNow;
                }
            }
        }

        public void PublishError<T>(Exception error)
            where T : IEvent
        {
            Logger.EditorLogVerbose($"Publish Error Event: {typeof(T).Name}", LogTag.Event);

            lock (m_Locker)
            {
                var eventType = typeof(T);
                if (m_EventInfos.TryGetValue(eventType, out var eventInfo))
                {
                    ((ISubject<T>)eventInfo.Subject).OnErrorResume(error);
                    eventInfo.LastActivity = DateTime.UtcNow;
                    CleanupEventInfo(eventType);
                }
            }
        }

        public void PublishComplete<T>(T message)
            where T : IEvent
        {
            Logger.EditorLogVerbose($"Publish Complete Event: {typeof(T).Name}", LogTag.Event);

            lock (m_Locker)
            {
                var eventType = typeof(T);
                if (m_EventInfos.TryGetValue(eventType, out var eventInfo))
                {
                    ((ISubject<T>)eventInfo.Subject).OnNext(message);
                    ((ISubject<T>)eventInfo.Subject).OnCompleted();
                    eventInfo.LastActivity = DateTime.UtcNow;
                }
                CleanupEventInfo(eventType);
            }
        }

        public void Complete<T>()
            where T : IEvent
        {
            Logger.EditorLogVerbose($"Complete Event: {typeof(T).Name}", LogTag.Event);

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
            Logger.EditorLogVerbose($"Clear MessageBroker", LogTag.Event);

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
            Logger.EditorLogVerbose($"Get Subscriber Count: {typeof(T).Name}", LogTag.Event);

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
            Logger.EditorLogVerbose($"Get Subscribers: {typeof(T).Name}", LogTag.Event);

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
            Logger.EditorLogVerbose($"Has Active Subscribers: {typeof(T).Name}", LogTag.Event);

            lock (m_Locker)
            {
                return m_EventInfos.TryGetValue(typeof(T), out var eventInfo)
                    && eventInfo.SubscriberCount > 0;
            }
        }

#if UNITY_EDITOR
        public string GetDebugInfo()
        {
            Logger.EditorLogVerbose($"Get Debug Info", LogTag.Event);

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
