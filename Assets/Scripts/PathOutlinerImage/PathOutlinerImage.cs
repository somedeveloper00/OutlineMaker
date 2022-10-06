using System;
using System.Linq;
using UnityEngine;

[ExecuteAlways]
public class PathOutlinerImage : MonoBehaviour
{
	public Material material;
	public Renderer renderer;

	public Color color;
	[Range(0.001f, 0.1f)]
	public float length;
	public Circle[] circles;
	
	[Serializable]
	public class Circle
	{
		public Vector2 center;
		public float radius;
	}

	private void OnValidate()
	{
		if (renderer == null) renderer = GetComponent<Renderer>();
		if (renderer.sharedMaterial != material)
			renderer.sharedMaterial = material;
	}

	private void OnRenderObject()
	{
		UpdateShader();
	}

	private void UpdateShader()
	{
		if(renderer == null || material == null) return;

		var points = circles.Select(c => (Vector4)c.center).ToArray();
		var radiuses = circles.Select(c => c.radius).ToArray();
		
		var prop = new MaterialPropertyBlock();
		prop.SetColor("_Color", color);
		prop.SetFloat("_Count", circles.Length);
		prop.SetFloat("_Length", length);
		prop.SetVectorArray("_Centers", points);
		prop.SetFloatArray("_Radiuses", radiuses);
		renderer.SetPropertyBlock(prop);
	}
}