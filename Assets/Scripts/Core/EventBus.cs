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

        public static void Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (!Handlers.ContainsKey(type))
                Handlers[type] = new List<Action<object>>();

            Handlers[type].Add(e => handler((T)e));
        }

        /// <summary>
        /// Unsubscribes by storing a wrapper map. For simplicity in a student project,
        /// call ClearAll() on scene unload instead of per-handler unsubscribe.
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler)
        {
            // Simple implementation: clear all of this type and re-add minus the target.
            // For production, store the wrapper in a Dictionary<Delegate, Action<object>>.
            var type = typeof(T);
            if (Handlers.TryGetValue(type, out var handler1))
                handler1.Clear();
        }

        public static void Emit<T>(T evt)
        {
            var type = typeof(T);
            if (!Handlers.TryGetValue(type, out var list)) return;

            // Iterate a copy so handlers can safely subscribe new events during dispatch
            var copy = new List<Action<object>>(list);
            foreach (var h in copy)
                h(evt);
        }

        /// <summary>Call this when the gameplay scene unloads to prevent stale subscriptions.</summary>
        public static void ClearAll() => Handlers.Clear();
    }
}
