using UnityEngine;
using UnityEditor;
using System.Collections;

namespace uP5
{
    public class PEditor : PGraphics
    {

        /*
        [MenuItem ("GameObject/Create Other/uProcessing/Rect")]
        static void AddRect() {
            PShape shape = createPShapeObject("Rect");
            shape.saveAssetDB(shape.createRect(-0.5f, -0.5f, 0.5f, 0.5f));
        }

        [MenuItem ("GameObject/Create Other/uProcessing/Ellipse")]
        static void AddEllipse() {
            PShape shape = createPShapeObject("Ellipse");
            shape.saveAssetDB(shape.createEllipse(0, 0, 0.5f, 0.5f));
        }

        static PShape createPShapeObject(string name) {
            GameObject obj = new GameObject(name);
            obj.AddComponent<MeshFilter>();
            var meshRenderer = obj.AddComponent<MeshRenderer>();
            meshRenderer.material = new Material (Shader.Find ("Diffuse"));
            return obj.AddComponent<PShape>();
        }
        */
    }
}