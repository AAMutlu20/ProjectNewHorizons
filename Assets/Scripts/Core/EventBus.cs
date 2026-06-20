using System;
using System.Collections.Generic;

namespace Core
{
    /// <summary>
    /// Global static event bus. All cross-system communication goes through here.
    /// No system should hold a direct MonoBehaviour reference to another system.
    ///
    /// Usage:
    ///   EventBus.Subscribe<WaveStartedEvent/>(OnWaveStarted);
    ///   EventBus.Emit(new WaveStartedEvent { wave = 1 });
    ///   EventBus.Unsubscribe<WaveStartedEvent/>(OnWaveStarted);  // always on OnDestroy
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Action<object>>> Handlers = new();

        // Maps each (event type, original handler) pair to the wrapper Emit
        // actually invokes, so Unsubscribe can remove exactly that one entry
        // instead of clearing every listener for the event type. Keyed by type
        // as well as delegate so the same handler instance can safely subscribe
        // to more than one event type without the keys colliding.
        private static readonly Dictionary<(Type, Delegate), Action<object>> WrapperByHandler = new();

        public static void Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (!Handlers.TryGetValue(type, out var list))
            {
                list = new List<Action<object>>();
                Handlers[type] = list;
            }

            Action<object> wrapper = boxedEvent => handler((T)boxedEvent);
            WrapperByHandler[(type, handler)] = wrapper;
            list.Add(wrapper);
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (!Handlers.TryGetValue(type, out var list)) return;

            var key = (type, (Delegate)handler);
            if (!WrapperByHandler.TryGetValue(key, out var wrapper)) return;

            list.Remove(wrapper);
            WrapperByHandler.Remove(key);
        }

        public static void Emit<T>(T evt)
        {
            var type = typeof(T);
            if (!Handlers.TryGetValue(type, out var list)) return;

            // Iterate a copy so handlers can safely subscribe new events during dispatch
            var copy = new List<Action<object>>(list);
            foreach (var wrapper in copy)
                wrapper(evt);
        }

        /// <summary>Call this when the gameplay scene unloads to prevent stale subscriptions.</summary>
        public static void ClearAll()
        {
            Handlers.Clear();
            WrapperByHandler.Clear();
        }
    }
}
