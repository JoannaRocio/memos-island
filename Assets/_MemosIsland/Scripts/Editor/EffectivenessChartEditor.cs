using MemosIsland.Memos;
using UnityEditor;
using UnityEngine;

namespace MemosIsland.EditorTools
{
    /// <summary>Muestra la tabla de efectividad como grilla tipo × terreno; clic en una celda la cambia (▲ → · → ▼).</summary>
    [CustomEditor(typeof(EffectivenessChart))]
    public class EffectivenessChartEditor : Editor
    {
        static readonly Color StrongColor = new(0.45f, 0.85f, 0.45f);
        static readonly Color WeakColor = new(0.95f, 0.45f, 0.45f);

        public override void OnInspectorGUI()
        {
            var chart = (EffectivenessChart)target;
            chart.Resize();

            EditorGUILayout.HelpBox("Filas = tipo del Memo, columnas = terreno. ▲ ×1,30 · · ×1,00 · ▼ ×0,75. " +
                                    "Clic en una celda para cambiarla.", MessageType.None);

            const float rowHeader = 70f, cell = 26f;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(rowHeader);
                foreach (var terrain in chart.terrains)
                {
                    var name = terrain != null ? terrain.displayName : "?";
                    GUILayout.Label(new GUIContent(name.Length > 4 ? name.Substring(0, 4) : name, name),
                        EditorStyles.miniLabel, GUILayout.Width(cell));
                }
            }

            foreach (var type in chart.types)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(type != null ? type.displayName : "?", GUILayout.Width(rowHeader));
                    foreach (var terrain in chart.terrains)
                    {
                        var value = chart.Get(type, terrain);
                        var old = GUI.backgroundColor;
                        GUI.backgroundColor = value switch
                        {
                            Effectiveness.Strong => StrongColor,
                            Effectiveness.Weak => WeakColor,
                            _ => old
                        };
                        var label = value switch { Effectiveness.Strong => "▲", Effectiveness.Weak => "▼", _ => "·" };
                        if (GUILayout.Button(label, GUILayout.Width(cell)))
                        {
                            Undo.RecordObject(chart, "Cambiar efectividad");
                            var next = value switch
                            {
                                Effectiveness.Strong => Effectiveness.Normal,
                                Effectiveness.Normal => Effectiveness.Weak,
                                _ => Effectiveness.Strong
                            };
                            chart.Set(type, terrain, next);
                            EditorUtility.SetDirty(chart);
                        }
                        GUI.backgroundColor = old;
                    }
                }
            }

            EditorGUILayout.Space();
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("types"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("terrains"), true);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
