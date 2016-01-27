using UnityEngine;
using System.Collections;

namespace uP5
{
    public class PWireframe : MonoBehaviour
    {
        public bool isStroke = true;
        public bool isDraw1st = false;
        public bool isDraw2nd = true;
        public bool isDraw3rd = false;
        public bool isFlat = false;
        public Color strokeColor = new Color(0.0f, 1.0f, 1.0f);
        public float strokeWeight = 2;
        public float capLevel = 1.0f;
        public float depth = -0.0001f;

        private Vector3[] strokes;
        public Material strokeMaterial;

        void Awake()
        {
            if (strokeMaterial == null)
            {
                strokeMaterial = new Material(Shader.Find("Custom/uProcessing/strokeShader"));
            }

            MeshFilter filter = gameObject.GetComponent<MeshFilter>();
            if (filter)
            {
                Mesh mesh = filter.mesh;
                Vector3[] vertices = mesh.vertices;
                int[] triangles = mesh.triangles;
                ArrayList linesList = new ArrayList();

                for (int i = 0; i + 2 < triangles.Length; i += 3)
                {
                    linesList.Add(vertices[triangles[i]]);
                    linesList.Add(vertices[triangles[i + 1]]);
                    linesList.Add(vertices[triangles[i + 2]]);
                }

                //linesList.CopyTo(lines);
                strokes = (Vector3[])linesList.ToArray(typeof(Vector3));
            }
        }

        void Start()
        {
            PGameObject obj = gameObject.GetComponent<PGameObject>();
            if (obj)
            {
                depth = obj.graphics.sceneDepthStep * 0.5f;
            }
        }

        void DrawQuad(Vector3 p1, Vector3 p2)
        {
            //float w = 1.0f / Screen.width * strokeWeight * 0.5f;
            float w = (0.01f + strokeWeight) * 0.25f;
            Vector3 lineDir = p2 - p1;
            Vector3 lineToCamera;
            if (isFlat)
            {
                lineToCamera = new Vector3(0, 0, 1);
                p1.z += depth;
                p2.z += depth;
            }
            else
            {
                lineToCamera = Camera.main.transform.position - (p2 + p1) / 2.0f;
            }
            Vector3 perpendicular = Vector3.Cross(lineToCamera, lineDir).normalized * w;
            Vector3 cap = lineDir.normalized * (w * capLevel);
            p1 -= cap;
            p2 += cap;
            GL.Vertex(p1 - perpendicular);
            GL.Vertex(p1 + perpendicular);
            GL.Vertex(p2 + perpendicular);
            GL.Vertex(p2 - perpendicular);
        }

        void OnRenderObject()
        {
            if ((Camera.current.cullingMask & (1 << gameObject.layer)) == 0) return;
            DrawWireframe();
        }

        void DrawWireframe()
        {
            if (!Camera.main || strokes == null || strokes.Length <= 0 || strokeWeight <= 0 || !isStroke) return;

            Transform tr = gameObject.transform;
            strokeMaterial.color = strokeColor;
            strokeMaterial.SetPass(0);

            if (strokeWeight == 1)
            {
                GL.Begin(GL.LINES);
                GL.Color(strokeColor);
                for (int i = 0; i + 2 < strokes.Length; i += 3)
                {
                    Vector3 vec1 = tr.TransformPoint(strokes[i]);
                    Vector3 vec2 = tr.TransformPoint(strokes[i + 1]);
                    Vector3 vec3 = tr.TransformPoint(strokes[i + 2]);
                    if (isDraw1st)
                    {
                        GL.Vertex(vec1);
                        GL.Vertex(vec2);
                    }
                    if (isDraw2nd)
                    {
                        GL.Vertex(vec2);
                        GL.Vertex(vec3);
                    }
                    if (isDraw3rd)
                    {
                        GL.Vertex(vec3);
                        GL.Vertex(vec1);
                    }
                }
            }
            else
            {
                GL.Begin(GL.QUADS);
                GL.Color(strokeColor);
                for (int i = 0; i + 2 < strokes.Length; i += 3)
                {
                    Vector3 vec1 = tr.TransformPoint(strokes[i]);
                    Vector3 vec2 = tr.TransformPoint(strokes[i + 1]);
                    Vector3 vec3 = tr.TransformPoint(strokes[i + 2]);
                    if (isDraw1st) DrawQuad(vec1, vec2);
                    if (isDraw2nd) DrawQuad(vec2, vec3);
                    if (isDraw3rd) DrawQuad(vec3, vec1);
                }
            }
            GL.End();
        }
    }
}