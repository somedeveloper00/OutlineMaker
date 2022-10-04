using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


public class OutlineRenderer : MonoBehaviour
{
	public Point[] points;
	[SerializeField] private LineRenderer _lineRenderer;

	private void OnValidate()
	{
		ValidateLineRenderer();
		Draw();
	}

	/// <summary>
	/// Draws the points through the <see cref="LineRenderer"/> attached to this game obejct
	/// </summary>
	public void Draw()
	{
		var positions = new List<Vector3>();
		for (var i = 0; i < points.Length; i++) positions.Add(points[i].transform.position);
		_lineRenderer.SetPositions(positions.ToArray());
	}

	/// <summary>
	/// Create a hidden line renderer, or if there already is one, using it
	/// </summary>
	private void ValidateLineRenderer()
	{
		if (_lineRenderer == null)
		{
			if (gameObject.TryGetComponent<LineRenderer>(out var lineRenderer))
			{
				_lineRenderer = lineRenderer;
			}
			else
			{
				// create new line renderer
				_lineRenderer = gameObject.AddComponent<LineRenderer>();
#if UNITY_EDITOR
				// hiding the line renderer inside inspector
				if (!Application.isPlaying)
				{
					// _lineRenderer.hideFlags |= HideFlags.HideInInspector;
					EditorUtility.SetDirty(gameObject);
				}
#endif
			}
		}
	}

	[Serializable]
	public struct Point
	{
		public Transform transform;
		public float radius;
	}
}