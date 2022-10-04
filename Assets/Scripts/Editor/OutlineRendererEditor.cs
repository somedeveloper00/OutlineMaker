using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OutlineRenderer))]
public class OutlineRendererEditor : Editor
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		if (GUILayout.Button("Redraw"))
		{
			(target as OutlineRenderer)?.Draw();
		}

	}
}