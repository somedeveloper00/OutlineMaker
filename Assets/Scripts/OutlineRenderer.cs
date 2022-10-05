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
	public bool drawDebugs = true;
	

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
		if(drawOnGizmo) Draw();
		if(!drawDebugs) return;
		
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


	private class CircleDrawing
	{
		/// <summary>
		/// center
		/// </summary>
		public readonly Vector2 c;
		
		/// <summary>
		/// radius
		/// </summary>
		public readonly float r;

		public readonly Circle circle;

		public CircleDrawing(Circle circle)
		{
			this.circle = circle;
			c = this.circle.c;
			r = this.circle.r;
		}
		
		/// <summary>
		/// drawing points on 1st visit (in clockwise order)
		/// </summary>
		public List<Vector2> points_1 = new List<Vector2>();
		
		/// <summary>
		/// drawing points on 2nd visit (in clockwise order)
		/// </summary>
		public List<Vector2> points_2 = new List<Vector2>();
	}
	
	public void Draw()
	{
		// initializing drawing points objects
		var drawingCircles = new CircleDrawing[circles.Length];
		for (var i = 0; i < this.circles.Length; i++) drawingCircles[i] = new CircleDrawing(circles[i]);


		tangentPoints1.Clear();
		tangentPoints2.Clear();

		// add all points 
		for (int i = 0; i < drawingCircles.Length; i++)
		{
			var r = new List<Vector2>();
			for (int j = 0; j < precision; j++)
			{
				r.Add(GetPositionAroundCircle(drawingCircles[i], (float)j / (precision - 1) * 360));
			}

			drawingCircles[i].points_1 = r;
		}
		
		foreach(var dp in drawingCircles)
			Debug_DrawingPointsAroundCircle(dp.points_1, dp.c, Color.red);

		// remove un-needed points based on tangents with the next & previous circles
		for (int p_ind = 0; p_ind < drawingCircles.Length; p_ind++)
		{
			// remove based on the next circle
			if(p_ind != drawingCircles.Length - 1)
				RemoveDrawingPointsIfOutsideTangent(drawingCircles[p_ind], drawingCircles[p_ind + 1]);
		}

		foreach (var dp in drawingCircles)
		{
			Debug_DrawingPointsAroundCircle(dp.points_1, dp.c, Color.blue);
			Debug_DrawingPointsAroundCircle(dp.points_2, dp.c, Color.yellow);
		}

		DrawLine();


		void DrawLine()
		{
			var drawingPoints = new List<Vector2>();
			for (int i = 0; i < drawingCircles.Length; i++) drawingPoints.AddRange(drawingCircles[i].points_1);
			for (int i = drawingCircles.Length - 1; i >= 0; i--) drawingPoints.AddRange(drawingCircles[i].points_2);
			_lineRenderer.positionCount = drawingPoints.Count;
			_lineRenderer.SetPositions(drawingPoints.Select(p => (Vector3)p).ToArray());
		}
		
		// removes the outer points from both circles (given indices)
		void RemoveDrawingPointsIfOutsideTangent(CircleDrawing circle1, CircleDrawing circle2)
		{
			// remove everything from the smaller if two circles inside each other
			if (Vector2.Distance(circle1.c, circle2.c) <= Mathf.Abs(circle1.r - circle2.r))
			{
				var removing_circle = circle2.r > circle1.r ? circle1 : circle2;
				removing_circle.points_1.Clear();
				removing_circle.points_2.Clear();
			}


			GetTangentsOfTwoCircle(circle1.circle, circle2.circle, 
				out var curr_tan1, out var curr_tan2, 
				out var next_tan1, out var next_tan2);
			
			// draw debug tangents
			if (drawDebugs)
			{
				Debug.DrawLine(curr_tan1, next_tan2, Color.cyan, 0.01f);
				Debug.DrawLine(curr_tan2, next_tan1, Color.cyan, 0.01f);
			}


			RemoveInsidePoints(curr_tan1, curr_tan2, circle1, true);
			RemoveInsidePoints(next_tan1, next_tan2, circle2, true);

			// adding debugging tangent spheres
			tangentPoints1.Add(curr_tan1);
			tangentPoints2.Add(curr_tan2);
			tangentPoints1.Add(next_tan1);
			tangentPoints2.Add(next_tan2);

			// remove points inside tangents
			void RemoveInsidePoints(Vector2 tan1, Vector2 tan2, CircleDrawing circle, bool sort)
			{ 
				var tang1_local = tan1 - circle.c;
				var tang2_local = tan2 - circle.c;
				var tang_angle = Vector2.SignedAngle(tang1_local, tang2_local);
				var dpoints = circle.points_1; // alias
				bool isSecondDeletion = circle.points_1.Count != precision && circle.points_2.Count == 0;
				int lastDeletionIndex = -1; // the last index that got deleted
				bool firstIndexDeleted = false;

				
				for (int i = 0; i < dpoints.Count; i++)
				{
					var alpha = Vector2.SignedAngle(tang1_local, dpoints[i] - circle.c);
					if (tang_angle >= 0)
					{
						if (alpha < 0 || tang_angle < alpha)
						{
							if (i == 0 && isSecondDeletion) firstIndexDeleted = true;
							lastDeletionIndex = i;
							dpoints.RemoveAt(i--);
						}
					}
					else
					{
						if (alpha < 0 && alpha > tang_angle)
						{
							if (i == 0 && isSecondDeletion) firstIndexDeleted = true;
							lastDeletionIndex = i;
							dpoints.RemoveAt(i--);
						}
					}
				}

				// fit the rest of the remaining points to the 2nd list of points
				if (isSecondDeletion && lastDeletionIndex != -1)
				{
					var start_index = lastDeletionIndex;
					var count = circle.points_1.Count - lastDeletionIndex;
					
					// checking for ignoring all 1st points
					if (firstIndexDeleted)
					{
						circle.points_2.AddRange(circle1.points_1);
						circle.points_1.Clear();
					}
					else
					{
						circle.points_2 =
							circle.points_1.GetRange(start_index, count);
						circle.points_1.RemoveRange(start_index, count);
					}
					
				}


				if (sort)
				{
					// sort lines by angular distance from tangent 1
					circle.points_1.Sort((p1, p2) =>
					{
						float v1 = Vector2.SignedAngle(tang1_local, p1 - circle.c);
						if (v1 < 0) v1 = 360 + v1;
						float v2 = Vector2.SignedAngle(tang1_local, p2 - circle.c);
						if (v2 < 0) v2 = 360 + v2;
						return v1.CompareTo(v2);
					});
				}
			}
		}
		void Debug_DrawingPointsAroundCircle(List<Vector2> points, Vector2 center, Color color)
		{
			if(!drawDebugs) return;
			foreach (var p in points) 
				Debug.DrawLine(p, end: p + (p - center).normalized * 2, color, 0.01f);
		}
	}
	
	public void Draw_old()
	{
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

			GetTangentsOfTwoCircle(circle, circle_next, 
				out var curr_tan1, out var curr_tan2,
				out var next_tan1, out var next_tan2);
			
			// adding lines between tangents
			var points = OutlineDiscPoints(circle.c, curr_tan1, curr_tan2, true, precision);
			positions.AddRange(points);

			tangentPoints1.Add(curr_tan1);
			tangentPoints1.Add(curr_tan2);
			tangentPoints1.Add(next_tan1);
			tangentPoints1.Add(next_tan2);
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
	private static bool GetTangentsOfTwoCircle(Circle circle1, Circle circle2, 
		out Vector2 tan1_1, out Vector2 tan1_2, out Vector2 tan2_1, out Vector2 tan2_2)
	{
		var c1 = circle1.c;
		var c2 = circle2.c;
		var r1 = circle1.r;
		var r2 = circle2.r;
		
		if (Vector2.Distance(c2, c1) < Mathf.Abs(r2 - r1))
		{
			tan1_1 = tan1_2 = tan2_1 = tan2_2 = Vector2.zero;
			return false;
		}
		
		var d = Vector2.Distance(c1, c2);
		
		// find 1st tangent point to the next circle
		var l = r2 - r1;
		var alpha = Mathf.Asin(l / d) * Mathf.Rad2Deg; // angle of center-connecting line & the circle 1's tangent point
		alpha += 90;
		var connect_on_rad_p = c1 + (c2 - c1).normalized * r1; // connecting point of circle 1 & center-connecting line
		tan1_1 = RotatePoint(connect_on_rad_p, c1, alpha);

		
		// find 2nd tangent point
		tan1_2 = RotatePoint(connect_on_rad_p, c1, -alpha);
		
		// find 2nd circle's tangents
		tan2_1 = c2 + (tan1_2 - c1).normalized * r2;
		tan2_2 = c2 + (tan1_1 - c1).normalized * r2;
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
	private static Vector2 GetPositionAroundCircle(CircleDrawing circle, float degree)
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
		public Transform transform;
		public Vector2 c => transform.position;
		public float r;
		public bool isValid() => transform != null;
	}
}