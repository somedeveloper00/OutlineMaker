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
	public Circle[] circles;
	[Min(10)] public int precision;
	[SerializeField] private LineRenderer _lineRenderer;
	public bool drawOnGizmo = true;
	

	private List<Vector3> tangentPoints1 = new List<Vector3>();
	private List<Vector3> tangentPoints2 = new List<Vector3>();

	private void OnValidate()
	{
		bool emptyFound = false;
		for (int i = 0; i < circles.Length; i++)
		{
			if (!circles[i].isValid())
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
		var points_arr = circles.Where(p => p.isValid()).Select(p => p.c).ToArray();
		var radius_arr = circles.Where(p => p.isValid()).Select(p => p.r).ToArray();
		
		// simple circle for all
		for (int i = 0; i < points_arr.Length; i++)
		{
			// debug circles 
			Handles.DrawWireDisc(points_arr[i], Vector3.back, radius_arr[i]);
		}
		
		Gizmos.color = Color.green;
		foreach (var p in tangentPoints1) Gizmos.DrawSphere(p, 1.2f);
		Gizmos.color = Color.white;
		foreach (var p in tangentPoints2) Gizmos.DrawSphere(p, 1.2f);
	}

	
	public void Draw()
	{
		// updating circles
		foreach (var circle in circles) circle.Update();
		
		// all points around all circles
		List<List<Vector2>> drawingPoints = new List<List<Vector2>>();
		
		
		tangentPoints1.Clear();
		tangentPoints2.Clear();

		// add all points 
		for (int i = 0; i < circles.Length; i++)
		{
			var r = new List<Vector2>();
			for (int j = 0; j < precision; j++)
			{
				r.Add(GetPositionAroundCircle(circles[i], (float)j / (precision - 1) * 360));
			}
			drawingPoints.Add(r);
		}
		
		Debug_DrawingPoints(Color.red);

		// remove un-needed points based on tangents with the next & previous circles
		for (int p_ind = 0; p_ind < circles.Length; p_ind++)
		{
			// remove based on the next circle
			if(p_ind != circles.Length - 1)
				RemoveDrawingPointsIfOutsideTangent(p_ind, p_ind + 1, true);
			// remove based on the previous circle
			if(p_ind != 0)
				RemoveDrawingPointsIfOutsideTangent(p_ind, p_ind - 1, false);
		}
		
		// draw
		// _lineRenderer.positionCount = drawingPoints.Sum(dp => dp.Count);
		// var linePoints = drawingPoints.SelectMany(dp => dp.Select(p => (Vector3)p)).ToArray();
		// _lineRenderer.SetPositions(linePoints);


		Debug_DrawingPoints(Color.blue);
		
		
		void RemoveDrawingPointsIfOutsideTangent(int mainIndex, int otherIndex, bool sort)
		{
			var circle = circles[mainIndex];
			var circle_other = circles[otherIndex];


			// remove everything from the smaller if two circles inside each other
			if (Vector2.Distance(circle.c, circle_other.c) <= Mathf.Abs(circle.r - circle_other.r))
			{
				var index = circle_other.r > circle.r ? mainIndex : otherIndex;
				drawingPoints[index].Clear();
			}


			GetTangentsOfTwoCircle(circle, circle_other, out var tangent1, out var tangent2);


			// remove points inside tangents
			var tang1_local = tangent1 - circle.c;
			var tang2_local = tangent2 - circle.c;
			var tang_angle = Vector2.SignedAngle(tang1_local, tang2_local);
			var dpoints = drawingPoints[mainIndex]; // alias
			for (int i = 0; i < dpoints.Count; i++)
			{
				var alpha = Vector2.SignedAngle(tang1_local, dpoints[i] - circle.c);
				if (tang_angle >= 0)
				{
					if (alpha < 0 || tang_angle < alpha) 
						dpoints.RemoveAt(i--);
				}
				else
				{
					if (alpha < 0 && alpha > tang_angle) 
						dpoints.RemoveAt(i--);
				}
			}

			
			if (sort)
			{
				// sort lines by angular distance from tangent 1
				drawingPoints[mainIndex].Sort((p1, p2) =>
				{
					float v1 = Vector2.SignedAngle(tang1_local, p1 - circle.c);
					if (v1 < 0) v1 = 360 + v1;
					float v2 = Vector2.SignedAngle(tang1_local, p2 - circle.c);
					if (v2 < 0) v2 = 360 + v2;
					return v1.CompareTo(v2);
				});
			}
			
			// adding debugging tangent spheres
			tangentPoints1.Add(tangent1);
			tangentPoints2.Add(tangent2);
			
		}

	

		void Debug_DrawingPoints(Color color)
		{
			for (var i = 0; i < drawingPoints.Count; i++)
			{
				var drawingPoint = drawingPoints[i];
				foreach (var p in drawingPoint)
				{
					Debug.DrawLine(p, p + (p - circles[i].c).normalized * 2, color, 0.01f);
				}
			}
		}
	}
	
	public void Draw_old()
	{
		foreach (var circle in circles) circle.Update();

		var positions = new List<Vector2>();

		tangentPoints1.Clear();
		
		for (int i = 0; i < circles.Length; i++)
		{
			var circle = circles[i];
			var circle_next  = i == circles.Length - 1 ? circles[i - 1] : circles[i + 1];
			var circle_prev = i == 0 ? circles[i + 1] : circles[i - 1];
			
			// continue if 2nd is inside 1st or wise versa
			if(Vector2.Distance(circle.c, circle_next.c) <= Mathf.Abs(circle.r - circle_next.r))
				continue;

			GetTangentsOfTwoCircle(circle, circle_next, out var tangent1_next, out var tangent2_next);
			GetTangentsOfTwoCircle(circle, circle_prev, out var tangent1_prev, out var tangent2_prev);
			
			// adding lines between tangents
			var points = OutlineDiscPoints(circle.c, tangent1_next, tangent2_next, true, precision);
			positions.AddRange(points);

			tangentPoints1.Add(tangent1_next);
			tangentPoints1.Add(tangent2_next);
			tangentPoints1.Add(tangent1_prev);
			tangentPoints1.Add(tangent2_prev);
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

	/// Gets the common tangents of the two specified circles
	private static bool GetTangentsOfTwoCircle(Circle circle1, Circle circle2, out Vector2 tangent1, out Vector2 tangent2)
	{
		var c1 = circle1.c;
		var c2 = circle2.c;
		var r1 = circle1.r;
		var r2 = circle2.r;
		
		if (Vector2.Distance(c2, c1) < Mathf.Abs(r2 - r1))
		{
			tangent1 = tangent2 = Vector2.zero;
			return false;
		}
		var d = Vector2.Distance(c1, c2);
		
		// find 1st tangent point to the next circle
		var l = r2 - r1;
		var alpha = Mathf.Asin(l / d) * Mathf.Rad2Deg; // angle of center-connecting line & the circle 1's tangent point
		alpha += 90;
		var connect_on_rad_p = c1 + (c2 - c1).normalized * r1; // connecting point of circle 1 & center-connecting line
		tangent1 = RotatePoint(connect_on_rad_p, c1, alpha);

		
		// find 2nd tangent point...
		tangent2 = RotatePoint(connect_on_rad_p, c1, -alpha);
		return true;
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
	private static Vector2 GetPositionAroundCircle(Circle circle, float degree)
	{
		var p = new Vector2(
			x: Mathf.Cos(degree * Mathf.Deg2Rad),
			y: Mathf.Sin(degree * Mathf.Deg2Rad));
		return circle.c + circle.r * p;
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
	public class Circle
	{
		[SerializeField] Transform transform;
		public float r;
		public Vector2 c { get; private set; }

		public bool isValid() => transform != null;

		/// <summary>
		/// Updates circle's stats depending on the assigned <see cref="transform"/>
		/// </summary>
		public void Update()
		{
			if (isValid()) c = transform.position;
		}
	}
}