using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tuent.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Gameplay.Core
{
    /// <summary>
    /// Load Addressable prefab (cache) rồi spawn/despawn qua TPool.
    /// </summary>
    public static class AddressablePoolSpawner
    {
        private const string LogTag = nameof(AddressablePoolSpawner);

        private sealed class CacheEntry
        {
            public GameObject Prefab;
            public AsyncOperationHandle<GameObject> Handle;
            public bool IsLoading;
            public readonly List<Action<GameObject>> Waiters = new();
        }

        private static readonly Dictionary<string, CacheEntry> Cache = new(StringComparer.Ordinal);

        public static bool IsLoaded(string address) =>
            TryGetPrefab(GetKey(address), out _);

        public static bool IsLoaded(AssetReference reference) =>
            TryGetPrefab(GetKey(reference), out _);

        public static void Preload(string address, Action<bool> onDone = null) =>
            Load(GetKey(address), address, prefab => onDone?.Invoke(prefab));

        public static void Preload(AssetReference reference, Action<bool> onDone = null) =>
            Load(GetKey(reference), reference?.RuntimeKey, prefab => onDone?.Invoke(prefab));

        public static void Spawn(string address, Action<GameObject> onSpawned, Transform parent = null, bool worldPositionStays = false) =>
            Load(GetKey(address), address, prefab => onSpawned?.Invoke(prefab ? TPool.Spawn(prefab, parent, worldPositionStays) : null));

        public static void Spawn(AssetReference reference, Action<GameObject> onSpawned, Transform parent = null, bool worldPositionStays = false) =>
            Load(GetKey(reference), reference?.RuntimeKey, prefab => onSpawned?.Invoke(prefab ? TPool.Spawn(prefab, parent, worldPositionStays) : null));

        public static void Spawn(string address, Vector3 pos, Quaternion rot, Action<GameObject> onSpawned, Transform parent = null) =>
            Load(GetKey(address), address, prefab => onSpawned?.Invoke(prefab ? TPool.Spawn(prefab, pos, rot, parent) : null));

        public static void Spawn(AssetReference reference, Vector3 pos, Quaternion rot, Action<GameObject> onSpawned, Transform parent = null) =>
            Load(GetKey(reference), reference?.RuntimeKey, prefab => onSpawned?.Invoke(prefab ? TPool.Spawn(prefab, pos, rot, parent) : null));

        public static Task<GameObject> SpawnAsync(string address, Transform parent = null, bool worldPositionStays = false)
        {
            var tcs = new TaskCompletionSource<GameObject>();
            Spawn(address, go => tcs.TrySetResult(go), parent, worldPositionStays);
            return tcs.Task;
        }

        public static Task<GameObject> SpawnAsync(AssetReference reference, Transform parent = null, bool worldPositionStays = false)
        {
            var tcs = new TaskCompletionSource<GameObject>();
            Spawn(reference, go => tcs.TrySetResult(go), parent, worldPositionStays);
            return tcs.Task;
        }

        public static Task<GameObject> SpawnAsync(string address, Vector3 pos, Quaternion rot, Transform parent = null)
        {
            var tcs = new TaskCompletionSource<GameObject>();
            Spawn(address, pos, rot, go => tcs.TrySetResult(go), parent);
            return tcs.Task;
        }

        public static Task<GameObject> SpawnAsync(AssetReference reference, Vector3 pos, Quaternion rot, Transform parent = null)
        {
            var tcs = new TaskCompletionSource<GameObject>();
            Spawn(reference, pos, rot, go => tcs.TrySetResult(go), parent);
            return tcs.Task;
        }

        public static async Task<T> SpawnAsync<T>(string address, Transform parent = null, bool worldPositionStays = false)
            where T : Component
        {
            var go = await SpawnAsync(address, parent, worldPositionStays);
            return go ? go.GetComponent<T>() : null;
        }

        public static async Task<T> SpawnAsync<T>(AssetReference reference, Transform parent = null, bool worldPositionStays = false)
            where T : Component
        {
            var go = await SpawnAsync(reference, parent, worldPositionStays);
            return go ? go.GetComponent<T>() : null;
        }

        public static void Despawn(GameObject instance, float delay = 0f)
        {
            if (!instance) return;
            if (delay > 0f) TPool.DeSpawn(instance, delay);
            else TPool.DeSpawn(instance);
        }

        public static void Despawn(Component instance, float delay = 0f)
        {
            if (instance) Despawn(instance.gameObject, delay);
        }

        public static void DespawnAll(string address)
        {
            if (TryGetPrefab(GetKey(address), out var prefab)) TPool.DeSpawnAll(prefab);
        }

        public static void DespawnAll(AssetReference reference)
        {
            if (TryGetPrefab(GetKey(reference), out var prefab)) TPool.DeSpawnAll(prefab);
        }

        public static void Release(string address, bool despawnAll = true) =>
            ReleaseKey(GetKey(address), despawnAll);

        public static void Release(AssetReference reference, bool despawnAll = true) =>
            ReleaseKey(GetKey(reference), despawnAll);

        public static void ReleaseAll(bool despawnAll = true)
        {
            var keys = new List<string>(Cache.Keys);
            for (int i = 0; i < keys.Count; i++)
                ReleaseKey(keys[i], despawnAll);
        }

        private static void Load(string key, object loadKey, Action<GameObject> onReady)
        {
            if (string.IsNullOrEmpty(key) || loadKey == null)
            {
                TLogger.Error(LogTag, "Invalid addressable key.");
                onReady?.Invoke(null);
                return;
            }

            if (Cache.TryGetValue(key, out var entry))
            {
                if (entry.Prefab)
                {
                    onReady?.Invoke(entry.Prefab);
                    return;
                }

                if (entry.IsLoading)
                {
                    if (onReady != null) entry.Waiters.Add(onReady);
                    return;
                }
            }
            else
            {
                entry = new CacheEntry();
                Cache[key] = entry;
            }

            entry.IsLoading = true;
            if (onReady != null) entry.Waiters.Add(onReady);

            var handle = Addressables.LoadAssetAsync<GameObject>(loadKey);
            entry.Handle = handle;
            handle.Completed += h =>
            {
                if (!Cache.TryGetValue(key, out entry))
                {
                    if (h.IsValid()) Addressables.Release(h);
                    return;
                }

                entry.IsLoading = false;
                if (h.Status != AsyncOperationStatus.Succeeded || !h.Result)
                {
                    TLogger.Error(LogTag, $"Load failed: '{key}' ({h.Status})");
                    if (h.IsValid()) Addressables.Release(h);
                    Cache.Remove(key);
                    FlushWaiters(entry, null);
                    return;
                }

                entry.Prefab = h.Result;
                entry.Handle = h;
                FlushWaiters(entry, entry.Prefab);
            };
        }

        private static void FlushWaiters(CacheEntry entry, GameObject prefab)
        {
            if (entry.Waiters.Count == 0) return;
            var waiters = new List<Action<GameObject>>(entry.Waiters);
            entry.Waiters.Clear();
            for (int i = 0; i < waiters.Count; i++)
                waiters[i]?.Invoke(prefab);
        }

        private static bool TryGetPrefab(string key, out GameObject prefab)
        {
            prefab = null;
            if (string.IsNullOrEmpty(key) || !Cache.TryGetValue(key, out var entry) || !entry.Prefab)
                return false;
            prefab = entry.Prefab;
            return true;
        }

        private static void ReleaseKey(string key, bool despawnAll)
        {
            if (string.IsNullOrEmpty(key) || !Cache.TryGetValue(key, out var entry)) return;
            if (despawnAll && entry.Prefab && TPoolers.Instance != null) TPool.DeSpawnAll(entry.Prefab);
            if (entry.Handle.IsValid()) Addressables.Release(entry.Handle);
            Cache.Remove(key);
        }

        private static string GetKey(string address)
        {
            if (!string.IsNullOrWhiteSpace(address)) return address;
            TLogger.Error(LogTag, "Address is null or empty.");
            return null;
        }

        private static string GetKey(AssetReference reference)
        {
            if (reference == null || !reference.RuntimeKeyIsValid())
            {
                TLogger.Error(LogTag, "AssetReference is null or invalid.");
                return null;
            }

            return !string.IsNullOrEmpty(reference.AssetGUID)
                ? reference.AssetGUID
                : reference.RuntimeKey.ToString();
        }
    }
}
