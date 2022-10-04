using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


public class OutlineRenderer : MonoBehaviour
{
	[SerializeField] private Point[] _points;
	[Min(10)] public int precision;
	[SerializeField] private LineRenderer _lineRenderer;
	public bool drawOnGizmo = true;
	

	private List<Vector3> tangentPoints = new List<Vector3>();

	private void OnValidate()
	{
		bool emptyFound = false;
		for (int i = 0; i < _points.Length; i++)
		{
			if (_points[i].transform == null)
			{
				Debug.LogWarning($"Point at {i} is empty");
				emptyFound = true;
			}
		}

		if (emptyFound)
			return;
		ValidateLineRenderer();
		Draw();
	}

	private void OnDrawGizmos()
	{
		if(drawOnGizmo)
			Draw();
		
		// get nun-empty points 
		var points_arr = _points.Where(p => p.transform != null).Select(p => p.transform.position).ToArray();
		var radius_arr = _points.Where(p => p.transform != null).Select(p => p.radius).ToArray();
		
		// simple circle for all
		for (int i = 0; i < points_arr.Length; i++)
		{
			// debug circles 
			Handles.DrawWireDisc(points_arr[i], Vector3.back, radius_arr[i]);
		}
		
		foreach (var p in tangentPoints)
		{
			Gizmos.DrawSphere(p, 0.1f);
		}
	}

	/// <summary>
	/// Draws the points through the <see cref="LineRenderer"/> attached to this game obejct
	/// </summary>
	public void Draw()
	{
		// get nun-empty points 
		var points_arr = _points.Where(p => p.transform != null).Select(p => (Vector2)p.transform.position).ToArray();
		var radius_arr = _points.Where(p => p.transform != null).Select(p => p.radius).ToArray();

		var positions = new List<Vector2>();

		tangentPoints.Clear();
		
		for (int i = 0; i < points_arr.Length; i++)
		{
			var c = points_arr[i];
			var c_next = i == points_arr.Length - 1 ? points_arr[0] : points_arr[i + 1];
			var c_prev = i == 0 ? points_arr[points_arr.Length - 1] : points_arr[i - 1];
			var r = radius_arr[i];
			var r_next = i == radius_arr.Length - 1 ? radius_arr[0] : radius_arr[i + 1];
			var r_prev = i == 0 ? radius_arr[radius_arr.Length - 1] : radius_arr[i - 1];
			
			// continue if 2nd is inside 1st or wise versa
			if(Vector2.Distance(c, c_next) <= Mathf.Abs(r - r_next))
				continue;

			GetTangentsOfTwoCircle(c, c_next, r, r_next, out var tangent1_next, out var tangent2_next);
			GetTangentsOfTwoCircle(c, c_next, r, r_next, out var tangent1_prev, out var tangent2_prev);
			
			// var tang1 = 

			// adding lines between tangents
			var points = OutlineDiscPoints(c, tangent1_next, tangent2_next, true, precision);
			positions.AddRange(points);

			tangentPoints.Add(tangent1_next);
			tangentPoints.Add(tangent2_next);
		}
		if(positions.Count > 0)
			positions.Add(positions[0]);
		
		_lineRenderer.positionCount = positions.Count;
		_lineRenderer.SetPositions(positions.Select(p => (Vector3)p).ToArray());
		
	}

	private static Vector2[] OutlineDiscPoints(Vector2 center, Vector2 p1, Vector2 p2, bool clockwise, int precision)
	{
		Vector2[] positions = new Vector2[precision + 1];
		
		var tang1_local = (p1 - center).normalized;
		var tang2_local = (p2 - center).normalized;

		// calculating increment of angle
		var angle = Vector2.SignedAngle(tang1_local, tang2_local);
		if (clockwise && angle < 0)
			angle = 360 + angle;
		// else if (!clockwise && angle > 0)
		// 	angle = 360 - angle;
		var angle_inc = angle / precision;

		// rotating by inc & adding to results
		Vector2 p = p1;
		for (int j = 0; j < precision; j++)
		{
			positions[j] = p;
			p = RotatePoint(p, center, angle_inc);
		}

		positions[precision] = p;
		return positions;
	}
	private static void GetTangentsOfTwoCircle(Vector2 c1, Vector2 c2, float r1, float r2, out Vector2 tangent1, out Vector2 tangent2)
	{
		var d = Vector2.Distance(c1, c2);
		
		// find 1st tangent point to the next circle
		var l = r2 - r1;
		var alpha = Mathf.Asin(l / d) * Mathf.Rad2Deg; // angle of center-connecting line & the circle 1's tangent point
		alpha += 90;
		var connect_on_rad_p = c1 + (c2 - c1).normalized * r1; // connecting point of circle 1 & center-connecting line
		tangent1 = RotatePoint(connect_on_rad_p, c1, alpha);

		// find 2nd tangent point...
		tangent2 = RotatePoint(connect_on_rad_p, c1, -alpha);
	}
	private static Vector2 RotatePoint(Vector2 point, Vector2 center, float degree)
	{
		point -= center;
		var c = Mathf.Cos(degree * Mathf.Deg2Rad);
		var s = Mathf.Sin(degree * Mathf.Deg2Rad);
		return center + new Vector2(
			x: point.x * c - point.y * s,
			y: point.x * s + point.y * c);
	}
	private static Vector2 GetPositionAroundCircle(Vector2 center, float radius, float degree)
	{
		var p = new Vector2(
			x: Mathf.Cos(degree * Mathf.Deg2Rad),
			y: Mathf.Sin(degree * Mathf.Deg2Rad));
		return center + radius * p;
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