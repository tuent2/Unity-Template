#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Networking;

namespace Tuent.Core.Editor
{
    public static class TDotweenDependencyUtility
    {
        public const string DotweenInstalledDefineSymbol = "DOTween__DEPENDENCIES_INSTALLED";
        public const string DotweenFolderPath = "Assets/Plugins/Demigiant/DOTween";
        public const string DotweenModulesAsmdefPath = "Assets/Plugins/Demigiant/DOTween/Modules/DOTween.Modules.asmdef";

        private static readonly NamedBuildTarget[] SupportTargets =
        {
            NamedBuildTarget.Standalone,
            NamedBuildTarget.Android,
            NamedBuildTarget.iOS
        };

        public static bool CanUseCoreTools() => IsDotweenDependencyInstalled();

        public static bool IsDotweenDependencyInstalled() =>
            HasDotweenFolder() && HasDotweenModulesAsmdef() && HasDefineSymbolOnAllTargets();

        public static bool HasDotweenFolder() => AssetDatabase.IsValidFolder(DotweenFolderPath);

        public static bool HasDotweenModulesAsmdef() => File.Exists(DotweenModulesAsmdefPath);

        public static bool HasDefineSymbolOnAllTargets()
        {
            for (var index = 0; index < SupportTargets.Length; index++)
            {
                var target = SupportTargets[index];
                PlayerSettings.GetScriptingDefineSymbols(target, out var defines);
                if (defines == null || !defines.Contains(DotweenInstalledDefineSymbol))
                    return false;
            }

            return true;
        }

        public static bool EnableDefineSymbolOnAllTargets()
        {
            var changed = false;
            for (var index = 0; index < SupportTargets.Length; index++)
            {
                var target = SupportTargets[index];
                PlayerSettings.GetScriptingDefineSymbols(target, out var defines);
                var defineList = defines?.ToList() ?? new List<string>();
                if (defineList.Contains(DotweenInstalledDefineSymbol))
                    continue;

                defineList.Add(DotweenInstalledDefineSymbol);
                PlayerSettings.SetScriptingDefineSymbols(target, defineList.ToArray());
                changed = true;
            }

            if (changed)
                AssetDatabase.Refresh();

            return changed;
        }

        public static bool DisableDefineSymbolOnAllTargets()
        {
            var changed = false;
            for (var index = 0; index < SupportTargets.Length; index++)
            {
                var target = SupportTargets[index];
                PlayerSettings.GetScriptingDefineSymbols(target, out var defines);
                var defineList = defines?.ToList() ?? new List<string>();
                if (!defineList.Contains(DotweenInstalledDefineSymbol))
                    continue;

                defineList.Remove(DotweenInstalledDefineSymbol);
                PlayerSettings.SetScriptingDefineSymbols(target, defineList.ToArray());
                changed = true;
            }

            if (changed)
                AssetDatabase.Refresh();

            return changed;
        }

        public static bool OpenDotweenUtilityPanel() =>
            EditorApplication.ExecuteMenuItem("Tools/Demigiant/DOTween Utility Panel")
            || EditorApplication.ExecuteMenuItem("Demigiant/DOTween Utility Panel");

        public static bool CreateDotweenModulesAsmdefIfMissing()
        {
            if (HasDotweenModulesAsmdef())
                return true;

            if (!HasDotweenFolder())
                return false;

            var parentFolder = Path.GetDirectoryName(DotweenModulesAsmdefPath)?.Replace("\\", "/");
            if (string.IsNullOrWhiteSpace(parentFolder) || !AssetDatabase.IsValidFolder(parentFolder))
                return false;

            File.WriteAllText(DotweenModulesAsmdefPath, "{\n    \"name\": \"DOTween.Modules\"\n}\n");
            AssetDatabase.ImportAsset(DotweenModulesAsmdefPath, ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.Refresh();
            return true;
        }
    }

    public sealed class TDotweenDependencyInstallerWindow : EditorWindow
    {
        private const string MenuPath = "Tuent/Project/TuentCore Installer";
        private const string WindowTitle = "Tuent Core Installer";

        [MenuItem(MenuPath)]
        private static void OpenWindow()
        {
            var window = GetWindow<TDotweenDependencyInstallerWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(520f, 240f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tuent Core — DOTween Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Cài DOTween (Assets/Plugins/Demigiant), tạo DOTween.Modules.asmdef, bật define cho UI tween.",
                MessageType.Info);

            DrawStatus();
            EditorGUILayout.Space(8f);
            DrawInstallActions();
            EditorGUILayout.Space(8f);
            DrawFinalizeActions();
        }

        private static void DrawStatus()
        {
            DrawStatusLine("DOTween folder", TDotweenDependencyUtility.HasDotweenFolder());
            DrawStatusLine("DOTween.Modules asmdef", TDotweenDependencyUtility.HasDotweenModulesAsmdef());
            DrawStatusLine($"Define `{TDotweenDependencyUtility.DotweenInstalledDefineSymbol}`",
                TDotweenDependencyUtility.HasDefineSymbolOnAllTargets());

            var ready = TDotweenDependencyUtility.IsDotweenDependencyInstalled();
            EditorGUILayout.HelpBox(
                ready
                    ? "DOTween sẵn sàng. Menu Tuent/Project và Tuent/Audio đã mở."
                    : "Hoàn tất các bước bên dưới để dùng UI tween và installer Tuent.",
                ready ? MessageType.Info : MessageType.Warning);
        }

        private static void DrawStatusLine(string label, bool ok)
        {
            var oldColor = GUI.color;
            GUI.color = ok ? new Color(0.3f, 0.9f, 0.3f) : new Color(0.95f, 0.35f, 0.35f);
            EditorGUILayout.LabelField($"{(ok ? "[OK]" : "[MISSING]")} {label}");
            GUI.color = oldColor;
        }

        private static void DrawInstallActions()
        {
            EditorGUILayout.LabelField("Bước 1 — DOTween", EditorStyles.boldLabel);

            if (GUILayout.Button("Open DOTween Utility Panel", GUILayout.Height(26f)))
            {
                if (!TDotweenDependencyUtility.OpenDotweenUtilityPanel())
                {
                    EditorUtility.DisplayDialog(
                        "DOTween Utility Panel",
                        "Không mở được panel. Cài DOTween vào Assets/Plugins/Demigiant trước.",
                        "OK");
                }
            }

            using (new EditorGUI.DisabledScope(!TDotweenDependencyUtility.HasDotweenFolder()))
            {
                if (GUILayout.Button("Create DOTween.Modules asmdef", GUILayout.Height(26f)))
                {
                    if (!TDotweenDependencyUtility.CreateDotweenModulesAsmdefIfMissing())
                    {
                        EditorUtility.DisplayDialog(
                            "Create asmdef failed",
                            "Không tạo được DOTween.Modules.asmdef.",
                            "OK");
                    }
                }
            }
        }

        private static void DrawFinalizeActions()
        {
            EditorGUILayout.LabelField("Bước 2 — Finalize", EditorStyles.boldLabel);

            if (GUILayout.Button($"Enable `{TDotweenDependencyUtility.DotweenInstalledDefineSymbol}`", GUILayout.Height(30f)))
            {
                TDotweenDependencyUtility.EnableDefineSymbolOnAllTargets();
                if (TDotweenDependencyUtility.IsDotweenDependencyInstalled())
                    EditorApplication.ExecuteMenuItem("Tuent/Project/Folder Setup");
            }

            using (new EditorGUI.DisabledScope(!TDotweenDependencyUtility.HasDefineSymbolOnAllTargets()))
            {
                if (GUILayout.Button("Disable DOTween define", GUILayout.Height(24f)))
                    TDotweenDependencyUtility.DisableDefineSymbolOnAllTargets();
            }
        }
    }
}
#endif
