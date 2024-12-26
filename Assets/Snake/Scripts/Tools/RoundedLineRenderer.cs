﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tools
{
    [RequireComponent(typeof(LineRenderer))]
    public class RoundedLineRenderer : MonoBehaviour
    {
        [SerializeField] private List<Vector2> _points = new();
        [SerializeField] private float _cornerRadius = 0.5f;
        [SerializeField] private int _segmentsPerNinetyDegrees = 4;
 
        private LineRenderer _lineRenderer;
        private readonly List<Vector3> _lineRendererPoints = new();

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
        }
        
        private enum BuildLineMode
        {
            LineRenderer,
            Gizmos
        }
 
        private void RebuildLine()
        {
            BuildLine(BuildLineMode.LineRenderer);
        }
 
        private void BuildLine(BuildLineMode mode)
        {
            if (_points.Count == 0) return;
 
            var localToWorld = transform.localToWorldMatrix;
            var prevPoint = _points[0];
 
            if (mode == BuildLineMode.LineRenderer)
            {
                _lineRenderer ??= GetComponent<LineRenderer>();
 
                _lineRendererPoints.Clear();
                _lineRendererPoints.Add(prevPoint);
            }
 
            for (int i = 0; i < _points.Count - 1; i++)
            {
                var point = _points[i];
                var nextPoint = _points[i + 1];
                var toNext = nextPoint - point;
                var toPrev = prevPoint - point;
 
                var cornerAngleDegrees = Vector2.SignedAngle(toNext, toPrev);
                if (Math.Abs(cornerAngleDegrees) < 0.0001f) continue;
 
                var halfCornerAngleRadians = cornerAngleDegrees / 2 * Mathf.Deg2Rad;
                var cosAngle = Mathf.Cos(halfCornerAngleRadians);
                var sinAngle = Mathf.Sin(halfCornerAngleRadians);
                var toPivot = new Vector2(toNext.x * cosAngle - toNext.y * sinAngle,
                    toNext.x * sinAngle + toNext.y * cosAngle).normalized * _cornerRadius;
                var pivot = point + toPivot;
                var distanceToPivot = Mathf.Abs((point.y - prevPoint.y) * pivot.x - (point.x - prevPoint.x) * pivot.y + point.x * prevPoint.y - point.y * prevPoint.x) / toPrev.magnitude;
                pivot = point + toPivot * (_cornerRadius / distanceToPivot);
 
                if (mode == BuildLineMode.LineRenderer)
                {
                    Vector2 toCurvePoint;
                    if (cornerAngleDegrees < 0)
                    {
                        toCurvePoint = new Vector2(toPrev.y, -toPrev.x).normalized * _cornerRadius;
                    }
                    else
                    {
                        toCurvePoint = new Vector2(-toPrev.y, toPrev.x).normalized * _cornerRadius;
                    }
 
                    _lineRendererPoints.Add(pivot + toCurvePoint);
 
                    var turnAngleDegrees = Mathf.Sign(cornerAngleDegrees) * 180 - cornerAngleDegrees;
 
                    int numSegments = Mathf.CeilToInt(Mathf.Abs(turnAngleDegrees / 90) * _segmentsPerNinetyDegrees);
                    var rotation = Matrix4x4.Rotate(Quaternion.AngleAxis(turnAngleDegrees / numSegments, Vector3.forward));
 
                    for (int j = 0; j < numSegments; j++)
                    {
                        toCurvePoint = rotation.MultiplyPoint(toCurvePoint);
 
                        _lineRendererPoints.Add(pivot + toCurvePoint);
                    }
                }
                else
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(localToWorld.MultiplyPoint(prevPoint), localToWorld.MultiplyPoint(point));
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(localToWorld.MultiplyPoint(pivot), _cornerRadius);
                }
 
                prevPoint = point;
            }
 
            if (mode == BuildLineMode.LineRenderer)
            {
                _lineRendererPoints.Add(_points[_points.Count - 1]);
 
                _lineRenderer.positionCount = _lineRendererPoints.Count;
                _lineRenderer.SetPositions(_lineRendererPoints.ToArray());
            }
            else
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(localToWorld.MultiplyPoint(prevPoint), localToWorld.MultiplyPoint(_points[_points.Count - 1]));
            }
        }

        public void SetPositions(ICollection<Vector2> points)
        {
            _points = points.ToList();
            RebuildLine();
        }
 
#if UNITY_EDITOR
 
        private void OnDrawGizmosSelected()
        {
            BuildLine(BuildLineMode.Gizmos);
        }
 
        [CustomEditor(typeof(RoundedLineRenderer))]
        public class CableLineEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                var cableLine = (RoundedLineRenderer)target;
 
                bool changed = DrawDefaultInspector();
 
                EditorGUILayout.Space();
 
                if (GUILayout.Button("Add Point"))
                {
                    if (cableLine._points.Count == 0)
                    {
                        cableLine._points.Add(Vector2.zero);
                    }
                    else
                    {
                        cableLine._points.Add(cableLine._points[cableLine._points.Count - 1] + Vector2.right);
                    }
 
                    EditorUtility.SetDirty(cableLine);
                    changed = true;
                }
 
                if (GUILayout.Button("Force Rebuild"))
                {
                    cableLine.RebuildLine();
                }
 
                if (changed)
                {
                    cableLine.RebuildLine();
                }
            }
 
            private void OnSceneGUI()
            {
                var cableLine = (RoundedLineRenderer)target;
 
                var cableLineTransform = cableLine.transform;
                var snap = new Vector3(0.5f, 0.5f, 0.5f);
 
                for (int i = 0; i < cableLine._points.Count; i++)
                {
                    var prevPosition = cableLineTransform.localToWorldMatrix.MultiplyPoint(cableLine._points[i]);
                    var fmh_172_72_638706572273394147 = Quaternion.identity; var newPosition = Handles.FreeMoveHandle(prevPosition, .1f, snap, Handles.DotHandleCap);
 
                    if (newPosition != prevPosition)
                    {
                        newPosition.x = Mathf.Round(newPosition.x * 2) / 2;
                        newPosition.y = Mathf.Round(newPosition.y * 2) / 2;
                        cableLine._points[i] = cableLineTransform.worldToLocalMatrix.MultiplyPoint(newPosition);
                        cableLine.RebuildLine();
                        EditorUtility.SetDirty(cableLine);
                    }
                }
            }
        }
#endif
    }
}