#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Gameplay.Core.Editor
{
    /// <summary>
    /// Quét Texture/Sprite không được asset nào reference (prefab/scene/SO/material…),
    /// rồi cho phép chọn và xóa.
    /// Lưu ý: không bắt được load động qua Resources.Load / Addressables string / code hardcode path.
    /// </summary>
    public sealed class UnusedImageCleanerWindow : EditorWindow
    {
        private const string DefaultScanFolder = "Assets/_MainProject";

        private string _scanFolder = DefaultScanFolder;
        private bool _includeResourcesFolder;
        private bool _includeEditorFolders;
        private Vector2 _scroll;
        private string _filter = string.Empty;

        private readonly List<UnusedImageEntry> _unused = new();
        private readonly HashSet<string> _selectedPaths = new();
        private bool _isScanning;
        private string _status = "Chưa scan.";

        [MenuItem("GameUp/Tools/Unused Image Cleaner")]
        public static void Open()
        {
            var window = GetWindow<UnusedImageCleanerWindow>("Unused Images");
            window.minSize = new Vector2(520f, 420f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Unused Image Cleaner", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Chỉ scan .png / .jpg / .jpeg.\n" +
                "Ảnh unused = không có asset nào dependency tới nó.\n" +
                "Không phát hiện load động (Resources.Load / Addressables key / path trong code).",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(_isScanning))
            {
                DrawScanSettings();
                EditorGUILayout.Space(6f);
                DrawActions();
                EditorGUILayout.Space(6f);
                DrawResultList();
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(_status, EditorStyles.miniLabel);
        }

        private void DrawScanSettings()
        {
            EditorGUILayout.BeginHorizontal();
            _scanFolder = EditorGUILayout.TextField("Scan Folder", _scanFolder);
            if (GUILayout.Button("…", GUILayout.Width(28f)))
            {
                string absolute = EditorUtility.OpenFolderPanel("Chọn folder scan", Application.dataPath, string.Empty);
                if (!string.IsNullOrEmpty(absolute))
                {
                    string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                    string full = Path.GetFullPath(absolute);
                    if (full.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        string relative = full.Substring(projectRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        _scanFolder = relative.Replace('\\', '/');
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            _includeResourcesFolder = EditorGUILayout.ToggleLeft(
                "Include Resources/ (có thể load động)",
                _includeResourcesFolder);
            _includeEditorFolders = EditorGUILayout.ToggleLeft(
                "Include Editor/ folders",
                _includeEditorFolders);
            _filter = EditorGUILayout.TextField("Filter path", _filter);
        }

        private void DrawActions()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Scan Unused Images", GUILayout.Height(28f)))
                    ScanUnusedImages();

                using (new EditorGUI.DisabledScope(_unused.Count == 0))
                {
                    if (GUILayout.Button("Select All", GUILayout.Height(28f)))
                        SelectAll(true);

                    if (GUILayout.Button("Deselect All", GUILayout.Height(28f)))
                        SelectAll(false);

                    if (GUILayout.Button($"Delete Selected ({_selectedPaths.Count})", GUILayout.Height(28f)))
                        DeleteSelected();
                }
            }
        }

        private void DrawResultList()
        {
            EditorGUILayout.LabelField($"Unused: {_unused.Count}", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            string filter = (_filter ?? string.Empty).Trim();
            for (int i = 0; i < _unused.Count; i++)
            {
                UnusedImageEntry entry = _unused[i];
                if (!string.IsNullOrEmpty(filter)
                    && entry.Path.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    bool selected = _selectedPaths.Contains(entry.Path);
                    bool next = EditorGUILayout.Toggle(selected, GUILayout.Width(18f));
                    if (next != selected)
                    {
                        if (next)
                            _selectedPaths.Add(entry.Path);
                        else
                            _selectedPaths.Remove(entry.Path);
                    }

                    Texture preview = entry.Preview;
                    if (preview != null)
                        GUILayout.Box(preview, GUILayout.Width(36f), GUILayout.Height(36f));
                    else
                        GUILayout.Box(GUIContent.none, GUILayout.Width(36f), GUILayout.Height(36f));

                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField(Path.GetFileName(entry.Path), EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(entry.Path, EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(FormatBytes(entry.ByteSize), EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();

                    if (GUILayout.Button("Ping", GUILayout.Width(48f), GUILayout.Height(36f)))
                    {
                        UnityEngine.Object obj = AssetDatabase.LoadMainAssetAtPath(entry.Path);
                        if (obj != null)
                            EditorGUIUtility.PingObject(obj);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void ScanUnusedImages()
        {
            _isScanning = true;
            _unused.Clear();
            _selectedPaths.Clear();

            try
            {
                string folder = NormalizeFolder(_scanFolder);
                if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
                {
                    _status = $"Folder không hợp lệ: {_scanFolder}";
                    EditorUtility.DisplayDialog("Unused Images", _status, "OK");
                    return;
                }

                string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                var candidatePaths = new List<string>(textureGuids.Length);

                for (int i = 0; i < textureGuids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(textureGuids[i]);
                    if (ShouldSkipPath(path) || !IsPngOrJpgPath(path))
                        continue;

                    candidatePaths.Add(path);
                }

                HashSet<string> usedTexturePaths = CollectUsedTexturePaths();

                long totalBytes = 0;
                for (int i = 0; i < candidatePaths.Count; i++)
                {
                    string path = candidatePaths[i];
                    float progress = candidatePaths.Count == 0 ? 1f : (float)i / candidatePaths.Count;
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "Unused Image Cleaner",
                            $"Checking {path}",
                            0.85f + progress * 0.15f))
                    {
                        _status = "Scan bị hủy.";
                        return;
                    }

                    if (usedTexturePaths.Contains(path))
                        continue;

                    long size = GetAssetByteSize(path);
                    totalBytes += size;
                    Texture2D preview = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    _unused.Add(new UnusedImageEntry(path, size, preview));
                }

                _unused.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));
                _status = $"Scan xong: {_unused.Count} ảnh unused (~{FormatBytes(totalBytes)}) trong {folder}.";
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                _isScanning = false;
                Repaint();
            }
        }

        private HashSet<string> CollectUsedTexturePaths()
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] allAssetGuids = AssetDatabase.FindAssets(string.Empty, new[] { "Assets" });

            for (int i = 0; i < allAssetGuids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(allAssetGuids[i]);
                if (string.IsNullOrEmpty(assetPath)
                    || AssetDatabase.IsValidFolder(assetPath)
                    || assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                    || IsPngOrJpgPath(assetPath))
                {
                    continue;
                }

                float progress = allAssetGuids.Length == 0 ? 1f : (float)i / allAssetGuids.Length;
                if (EditorUtility.DisplayCancelableProgressBar(
                        "Unused Image Cleaner",
                        $"Collect refs: {assetPath}",
                        progress * 0.85f))
                {
                    break;
                }

                string[] dependencies = AssetDatabase.GetDependencies(assetPath, true);
                for (int d = 0; d < dependencies.Length; d++)
                {
                    string dep = dependencies[d];
                    if (IsPngOrJpgPath(dep))
                        used.Add(dep);
                }
            }

            return used;
        }

        private static bool IsPngOrJpgPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            string ext = Path.GetExtension(path);
            return ext.Equals(".png", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
        }

        private bool ShouldSkipPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return true;

            string normalized = path.Replace('\\', '/');

            if (!_includeEditorFolders && normalized.IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (!_includeResourcesFolder && normalized.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            // Thường là asset hệ thống / third-party — tránh xóa nhầm nếu scan rộng.
            if (normalized.StartsWith("Assets/Plugins/", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("Assets/ThirdParty/", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private void SelectAll(bool selected)
        {
            _selectedPaths.Clear();
            if (!selected)
                return;

            string filter = (_filter ?? string.Empty).Trim();
            for (int i = 0; i < _unused.Count; i++)
            {
                string path = _unused[i].Path;
                if (!string.IsNullOrEmpty(filter)
                    && path.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                _selectedPaths.Add(path);
            }
        }

        private void DeleteSelected()
        {
            if (_selectedPaths.Count == 0)
                return;

            bool confirmed = EditorUtility.DisplayDialog(
                "Delete Unused Images",
                $"Xóa {_selectedPaths.Count} ảnh đã chọn?\nThao tác này không thể Undo qua Ctrl+Z (AssetDatabase.DeleteAsset).",
                "Delete",
                "Cancel");

            if (!confirmed)
                return;

            var paths = _selectedPaths.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
            int deleted = 0;
            var failed = new List<string>();

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < paths.Count; i++)
                {
                    string path = paths[i];
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "Deleting images",
                            path,
                            (float)i / paths.Count))
                    {
                        break;
                    }

                    if (AssetDatabase.DeleteAsset(path))
                    {
                        deleted++;
                        _unused.RemoveAll(e => e.Path == path);
                        _selectedPaths.Remove(path);
                    }
                    else
                    {
                        failed.Add(path);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
                EditorUtility.ClearProgressBar();
            }

            _status = failed.Count == 0
                ? $"Đã xóa {deleted} ảnh."
                : $"Đã xóa {deleted} ảnh. Lỗi {failed.Count}: {failed[0]}";
            Repaint();
        }

        private static string NormalizeFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
                return string.Empty;

            return folder.Replace('\\', '/').TrimEnd('/');
        }

        private static long GetAssetByteSize(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(Application.dataPath) ?? string.Empty,
                assetPath));

            if (!File.Exists(fullPath))
                return 0;

            try
            {
                return new FileInfo(fullPath).Length;
            }
            catch
            {
                return 0;
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024f:0.0} KB";
            return $"{bytes / (1024f * 1024f):0.00} MB";
        }

        private sealed class UnusedImageEntry
        {
            public UnusedImageEntry(string path, long byteSize, Texture2D preview)
            {
                Path = path;
                ByteSize = byteSize;
                Preview = preview;
            }

            public string Path { get; }
            public long ByteSize { get; }
            public Texture2D Preview { get; }
        }
    }
}
#endif
