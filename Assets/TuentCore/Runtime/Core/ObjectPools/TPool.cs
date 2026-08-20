using UnityEngine;

namespace Tuent.Core
{
    public static class TPool
    {
        public static T Spawn<T>(T prefab, Transform parent = null, bool worldPositionStays = false)
            where T : Component
        {
            return TPoolers.Instance.Spawn(prefab, parent, worldPositionStays);
        }

        /// <summary>This allows you to spawn a prefab via Component.</summary>
        public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null)
            where T : Component
        {
            // Clone this component's GameObject
            return TPoolers.Instance.Spawn(prefab, position, rotation, parent);
        }

        /// <summary>This allows you to spawn a prefab via GameObject.</summary>
        public static GameObject Spawn(GameObject prefab, Transform parent = null, bool worldPositionStays = false)
        {
            if (prefab) return TPoolers.Instance.Spawn(prefab, parent, worldPositionStays);

            TLogger.Error("TPool", "Attempting to spawn a null prefab.");

            return null;
        }

        /// <summary>This allows you to spawn a prefab via GameObject.</summary>
        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null,
            bool worldPositionStays = true)
        {
            if (prefab) return TPoolers.Instance.Spawn(prefab, position, rotation, parent);

            TLogger.Error("TPool", "Attempting to spawn a null prefab.");

            return null;
        }

        /// <summary>This allows you to despawn a clone via Component, with optional delay.</summary>
        public static void DeSpawn(Component clone, float delay = 0.0f)
        {
            if (clone) DeSpawn(clone.gameObject, delay);
        }

        /// <summary>This allows you to despawn a clone via GameObject, with optional delay.</summary>
        public static void DeSpawn(GameObject clone, float delay)
        {
            if (clone) TPoolers.Instance.DeSpawn(clone, delay);
        }

        /// <summary>This allows you to despawn a clone via GameObject, with optional delay.</summary>
        public static void DeSpawn(GameObject clone)
        {
            if (clone) TPoolers.Instance.DeSpawn(clone);
        }

        public static void DeSpawnAll(GameObject prefab)
        {
            if (prefab) TPoolers.Instance.DeSpawnAll(prefab);
        }

        public static void DeSpawnAll<T>(T prefab) where T : Component
        {
            if (prefab) TPoolers.Instance.DeSpawnAll(prefab);
        }
    }
}