using System;
using Gameplay.Unit;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Gameplay.Core.Editor
{
    /// <summary>
    /// Forces Unity Popup for EnemyUnitId inside Odin inspectors (no EnumSelector white popup).
    /// </summary>
    [DrawerPriority(DrawerPriorityLevel.SuperPriority)]
    public sealed class EnemyUnitIdSafeDrawer : OdinValueDrawer<EnemyUnitId>
    {
        private static readonly EnemyUnitId[] Values =
            (EnemyUnitId[])Enum.GetValues(typeof(EnemyUnitId));

        private static readonly string[] Names = BuildNames(Values);

        protected override bool CanDrawValueProperty(InspectorProperty property)
        {
            if (property.GetAttribute<EnumToggleButtonsAttribute>() != null)
                return false;
            if (property.GetAttribute<EnumPagingAttribute>() != null)
                return false;

            return true;
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            DrawEnumPopup(label, ValueEntry.SmartValue, Values, Names, out EnemyUnitId next);
            if (!next.Equals(ValueEntry.SmartValue))
                ValueEntry.SmartValue = next;
        }

        private static void DrawEnumPopup(
            GUIContent label,
            EnemyUnitId current,
            EnemyUnitId[] values,
            string[] names,
            out EnemyUnitId next)
        {
            int selectedIndex = 0;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i].Equals(current))
                {
                    selectedIndex = i;
                    break;
                }
            }

            EditorGUI.BeginChangeCheck();
            int nextIndex = EditorGUILayout.Popup(label, selectedIndex, names);
            if (EditorGUI.EndChangeCheck() && nextIndex >= 0 && nextIndex < values.Length)
                next = values[nextIndex];
            else
                next = current;
        }

        private static string[] BuildNames(EnemyUnitId[] values)
        {
            var names = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
                names[i] = ObjectNames.NicifyVariableName(values[i].ToString());
            return names;
        }
    }

    /// <summary>
    /// Replaces Odin EnumSelector with Unity Popup for all enum types.
    /// Avoids white stretched popup (FitWindowRectToScreen) with Odin + newer Unity.
    /// Uses struct constraint only (no Enum constraint) for broader Unity/C# compatibility.
    /// </summary>
    [DrawerPriority(DrawerPriorityLevel.SuperPriority)]
    public sealed class OdinUnitySafeEnumDrawer<T> : OdinValueDrawer<T>
        where T : struct
    {
        private static readonly bool IsEnumType = typeof(T).IsEnum;
        private static readonly bool IsFlags =
            IsEnumType && Attribute.IsDefined(typeof(T), typeof(FlagsAttribute));

        private static readonly Array EnumValues = IsEnumType ? Enum.GetValues(typeof(T)) : null;
        private static readonly string[] EnumNames = BuildNames();

        protected override bool CanDrawValueProperty(InspectorProperty property)
        {
            if (!IsEnumType)
                return false;
            if (typeof(T) == typeof(EnemyUnitId))
                return false;
            if (property.GetAttribute<EnumToggleButtonsAttribute>() != null)
                return false;
            if (property.GetAttribute<EnumPagingAttribute>() != null)
                return false;

            return true;
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            if (IsFlags)
            {
                EditorGUI.BeginChangeCheck();
                Enum current = (Enum)(object)ValueEntry.SmartValue;
                Enum next = EditorGUILayout.EnumFlagsField(label, current);
                if (EditorGUI.EndChangeCheck())
                    ValueEntry.SmartValue = (T)(object)next;
                return;
            }

            T currentValue = ValueEntry.SmartValue;
            int selectedIndex = 0;
            for (int i = 0; i < EnumValues.Length; i++)
            {
                if (Equals(EnumValues.GetValue(i), currentValue))
                {
                    selectedIndex = i;
                    break;
                }
            }

            EditorGUI.BeginChangeCheck();
            int nextIndex = EditorGUILayout.Popup(label, selectedIndex, EnumNames);
            if (EditorGUI.EndChangeCheck() && nextIndex >= 0 && nextIndex < EnumValues.Length)
                ValueEntry.SmartValue = (T)EnumValues.GetValue(nextIndex);
        }

        private static string[] BuildNames()
        {
            if (!IsEnumType || EnumValues == null)
                return Array.Empty<string>();

            var names = new string[EnumValues.Length];
            for (int i = 0; i < EnumValues.Length; i++)
                names[i] = ObjectNames.NicifyVariableName(EnumValues.GetValue(i).ToString());
            return names;
        }
    }
}
