#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Tuent.Core.Editor
{
    /// <summary>
    /// Cursor đọc từ gốc project: <c>.cursor/rules/</c>, <c>.cursorrules</c>, <c>.cursorignore</c>.
    /// Mẫu trong <c>Documentation~/cursor-rules</c> và <c>Documentation~/cursor-project-root</c>
    /// (file mẫu ignore tên <c>cursorignore</c>, khi cài được ghi thành <c>.cursorignore</c>).
    /// </summary>
    public static class TCursorRulesInstaller
    {
        private const string MenuPath = "Tuent/Project/Install Cursor IDE rules (from Tuent Core)";
        private const string MarkerFile = "tuent-core-usage.mdc";
        private const string IdeCursorGitUrl = "https://github.com/boxqkrtm/com.unity.ide.cursor.git";
        private const string IdeCursorPackageDirName = "com.boxqkrtm.ide.cursor";
        private const string CursorRulesTemplatesDirName = "cursor-rules";
        private const string CursorProjectRootTemplatesDirName = "cursor-project-root";
        private const string CursorSkillsTemplatesDirName = "cursor-skills";
        private const string CursorHooksTemplatesDirName = "cursor-hooks";

        private static AddRequest _ideCursorAddRequest;

        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

        private static string DestCursorDir => Path.Combine(ProjectRoot, ".cursor");
        private static string DestRulesDir => Path.Combine(ProjectRoot, ".cursor", "rules");
        private static string DestSkillsDir => Path.Combine(ProjectRoot, ".cursor", "skills");
        private static string DestHooksDir => Path.Combine(ProjectRoot, ".cursor", "hooks");

        [InitializeOnLoadMethod]
        private static void OnEditorLoad()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            EditorApplication.delayCall += TryAutoInstallIfMissing;
        }

        private static void TryAutoInstallIfMissing()
        {
            if (File.Exists(Path.Combine(DestRulesDir, MarkerFile)))
            {
                return;
            }

            if (!TryGetCursorRulesTemplatesDir(out var src))
            {
                return;
            }

            CopyAllMdc(src, DestRulesDir, overwrite: false);
            TLogger.Log("CursorRules", "Đã copy Cursor rules (.mdc) vào .cursor/rules — mở lại Cursor nếu cần.");
        }

        [MenuItem(MenuPath)]
        private static void InstallFromMenu()
        {
            if (!TryGetTuentCorePackageRoot(out var packageRoot))
            {
                EditorUtility.DisplayDialog(
                    "Tuent Core",
                    "Không tìm thấy Assets/TuentCore.",
                    "OK");
                return;
            }

            var rulesSrc = Path.Combine(packageRoot, "Documentation~", CursorRulesTemplatesDirName);
            var rootTemplates = Path.Combine(packageRoot, "Documentation~", CursorProjectRootTemplatesDirName);
            var skillsSrc = Path.Combine(packageRoot, "Documentation~", CursorSkillsTemplatesDirName);
            var hooksSrc = Path.Combine(packageRoot, "Documentation~", CursorHooksTemplatesDirName);
            if (!Directory.Exists(rulesSrc))
            {
                EditorUtility.DisplayDialog(
                    "Tuent Core",
                    "Thiếu thư mục Documentation~/cursor-rules trong Tuent Core.",
                    "OK");
                return;
            }

            if (!Directory.Exists(rootTemplates))
            {
                EditorUtility.DisplayDialog(
                    "Tuent Core",
                    "Thiếu thư mục Documentation~/cursor-project-root (.cursorrules / cursorignore mẫu).",
                    "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Tuent Core",
                    "Thực hiện:\n" +
                    "• Thêm package IDE Cursor qua Git (nếu chưa có): com.boxqkrtm.ide.cursor\n" +
                    "• Ghi đè / cập nhật .cursor/rules/*.mdc, .cursor/skills/*, .cursor/hooks*, .cursorrules, .cursorignore tại gốc project\n\n" +
                    "Tiếp tục?",
                    "OK",
                    "Cancel"))
            {
                return;
            }

            CopyAllMdc(rulesSrc, DestRulesDir, overwrite: true);
            CopyProjectRootTemplate(Path.Combine(rootTemplates, ".cursorrules"), Path.Combine(ProjectRoot, ".cursorrules"));
            // File mẫu tên `cursorignore` (không dấu chấm) để tránh hạn chế FS/tooling khi đóng gói.
            CopyProjectRootTemplate(Path.Combine(rootTemplates, "cursorignore"), Path.Combine(ProjectRoot, ".cursorignore"));
            CopyAllSkills(skillsSrc, DestSkillsDir, overwrite: true);
            CopyHooksTemplates(hooksSrc, overwrite: true);

            RequestAddIdeCursorPackage();

            TLogger.Log(
                "CursorRules",
                "Đã cập nhật .cursor/rules, .cursor/skills, .cursor/hooks, .cursorrules, .cursorignore. Nếu package IDE Cursor chưa có, Unity đang thêm qua Git URL (xem Package Manager / Console).");
        }

        private static void CopyProjectRootTemplate(string sourceFile, string destFile)
        {
            if (!File.Exists(sourceFile))
            {
                TLogger.Warning("CursorRules", $"Thiếu file mẫu: {sourceFile}");
                return;
            }

            File.Copy(sourceFile, destFile, overwrite: true);
        }

        private static void RequestAddIdeCursorPackage()
        {
            if (IsIdeCursorInstalled())
            {
                TLogger.Log("CursorRules", "com.boxqkrtm.ide.cursor đã có trong Packages — bỏ qua Client.Add.");
                return;
            }

            if (_ideCursorAddRequest != null && !_ideCursorAddRequest.IsCompleted)
            {
                TLogger.Log("CursorRules", "Đang chờ thêm package IDE Cursor từ lần trước…");
                return;
            }

            _ideCursorAddRequest = Client.Add(IdeCursorGitUrl);
            EditorApplication.update += OnIdeCursorAddProgress;
        }

        private static void OnIdeCursorAddProgress()
        {
            if (_ideCursorAddRequest == null || !_ideCursorAddRequest.IsCompleted)
            {
                return;
            }

            EditorApplication.update -= OnIdeCursorAddProgress;
            var req = _ideCursorAddRequest;
            _ideCursorAddRequest = null;

            if (req.Status == StatusCode.Success)
            {
                TLogger.Log("CursorRules", "Đã thêm package com.boxqkrtm.ide.cursor (Cursor IDE for Unity).");
                return;
            }

            var err = req.Error != null ? req.Error.message : "unknown";
            TLogger.Warning(
                "CursorRules",
                $"Không thêm được IDE Cursor qua UPM: {err}. Thêm thủ công Git URL: {IdeCursorGitUrl} (xem https://github.com/boxqkrtm/com.unity.ide.cursor )");
        }

        private static bool IsIdeCursorInstalled()
        {
            return Directory.Exists(Path.Combine(ProjectRoot, "Packages", IdeCursorPackageDirName));
        }

        internal static bool TryGetCursorRulesTemplatesDir(out string path)
        {
            path = null;
            if (!TryGetTuentCorePackageRoot(out var root))
            {
                return false;
            }

            var p = Path.Combine(root, "Documentation~", CursorRulesTemplatesDirName);
            if (Directory.Exists(p))
            {
                path = p;
                return true;
            }

            return false;
        }

        internal static bool TryGetTuentCorePackageRoot(out string packageRoot)
        {
            packageRoot = null;
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                typeof(TCursorRulesInstaller).Assembly);
            if (info != null && !string.IsNullOrEmpty(info.resolvedPath))
            {
                packageRoot = info.resolvedPath;
                return true;
            }

            var fallback = Path.Combine(Application.dataPath, "TuentCore");
            if (Directory.Exists(fallback) && File.Exists(Path.Combine(fallback, "package.json")))
            {
                packageRoot = fallback;
                return true;
            }

            return false;
        }

        /// <summary>Giữ tên cũ cho mã gọi nội bộ (nếu có).</summary>
        internal static bool TryGetTemplatesDir(out string path) => TryGetCursorRulesTemplatesDir(out path);

        private static void CopyAllMdc(string sourceDir, string destDir, bool overwrite)
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir, "*.mdc"))
            {
                var name = Path.GetFileName(file);
                var dest = Path.Combine(destDir, name);
                if (!overwrite && File.Exists(dest))
                {
                    continue;
                }

                File.Copy(file, dest, overwrite);
            }
        }

        private static void CopyAllSkills(string sourceDir, string destDir, bool overwrite)
        {
            if (!Directory.Exists(sourceDir))
            {
                TLogger.Warning("CursorRules", $"Thiếu thư mục skill mẫu: {sourceDir}");
                return;
            }

            CopyDirectoryRecursive(sourceDir, destDir, overwrite);
        }

        private static void CopyHooksTemplates(string sourceDir, bool overwrite)
        {
            if (!Directory.Exists(sourceDir))
            {
                TLogger.Warning("CursorRules", $"Thiếu thư mục hook mẫu: {sourceDir}");
                return;
            }

            Directory.CreateDirectory(DestCursorDir);

            var hooksJson = Path.Combine(sourceDir, "hooks.json");
            if (File.Exists(hooksJson))
            {
                File.Copy(hooksJson, Path.Combine(DestCursorDir, "hooks.json"), overwrite);
            }

            var hooksDir = Path.Combine(sourceDir, "hooks");
            if (Directory.Exists(hooksDir))
            {
                CopyDirectoryRecursive(hooksDir, DestHooksDir, overwrite);
            }
        }

        private static void CopyDirectoryRecursive(string sourceDir, string destDir, bool overwrite)
        {
            Directory.CreateDirectory(destDir);
            var normalizedSource = sourceDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            foreach (var directory in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                var relative = directory.Substring(normalizedSource.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                Directory.CreateDirectory(Path.Combine(destDir, relative));
            }

            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                var relative = file.Substring(normalizedSource.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var destination = Path.Combine(destDir, relative);
                var destinationDir = Path.GetDirectoryName(destination);
                if (!string.IsNullOrEmpty(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }

                File.Copy(file, destination, overwrite);
            }
        }
    }
}
#endif
