using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PShape : MonoBehaviour {
	void Awake() {
		//MeshFilter meshFilter = GetComponent<MeshFilter>();
		//meshFilter.sharedMesh = createEllipse(0, 0, 1, 1);
		//meshFilter.mesh = createEllipse(0, 0, 1, 1);
		//saveAssetDB(meshFilter.mesh);
	}

	public enum ShapeType {
		NONE,
		LINES,
	}

	public enum CloseType {
		NONE,
		CLOSE,
	}

	ShapeType shapeType = ShapeType.NONE;
	LineRenderer line;
	Vector3 firstV;
	int vCount = 0;

	#region Processing Members
	public void beginShape(ShapeType type) {
		if(!line) line = gameObject.GetComponent<LineRenderer>();
		if(!line) { line = gameObject.AddComponent<LineRenderer>(); }
		//lines.SetWidth(style.strokeWeight * sceneScale, style.strokeWeight * sceneScale);
		shapeType = type;
		firstV = Vector3.zero;
		line.SetVertexCount(vCount = 0);
	}

	public void endShape(CloseType type = CloseType.NONE) {
		if(shapeType==ShapeType.LINES && line) {
			if(type==CloseType.CLOSE) {
				vCount++;
				line.SetVertexCount(vCount);
				line.SetPosition(vCount, firstV);
			}
		}
	}

	public void vertex(float x, float y, float z=0.0f) {
		vertex(x, y, z, 0.0f, 0.0f);
	}

	public void vertex(float x, float y, float u, float v) {
		vertex(x, y, 0.0f, u, v);
	}

	public void vertex(float x, float y, float z, float u, float v) {
		if(shapeType==ShapeType.LINES) {
			vCount++;
			line.SetVertexCount(vCount);
			line.SetPosition(vCount, firstV = new Vector3(x, y, z));
		}
	}
	#endregion

	#region Processing Extra Members
	public void saveAssetDB(Mesh mesh) {
		#if UNITY_EDITOR
		var dbAssetName = "Assets/uProcessing/Models/" + mesh.name + ".asset";
		//AssetDatabase.DeleteAsset(dbAssetName);
		AssetDatabase.CreateAsset(mesh, dbAssetName);
		AssetDatabase.SaveAssets();
		#endif
	}

	public void apply(Mesh mesh) {
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		mesh.Optimize();
		MeshFilter meshFilter = GetComponent<MeshFilter>();
		meshFilter.mesh = mesh;
	}

	public Mesh createRect(float x, float y, float w, float h) {
		float hw = w * 0.5f;
		float hh = h * 0.5f;
		Mesh mesh = createQuad(x-hw, y-hh, x+hw, y-hh, x+hw, y+hh, x-hw, y+hh);
		mesh.name = "Rect";
		return mesh;
	}

	public Mesh createQuad(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4) {
		Mesh mesh = new Mesh();
		mesh.name = "Quad";

		mesh.vertices = new Vector3[]{
			new Vector3 (x4, y4, 0.0f),
			new Vector3 (x3, y3, 0.0f),
			new Vector3 (x2, y2, 0.0f),
			new Vector3 (x1, y1, 0.0f)
		};

		mesh.triangles = new int[]{
			0, 1, 2,
			2, 3, 0
		};

		mesh.uv = new Vector2[]{
			new Vector2 (0.0f, 1.0f),
			new Vector2 (1.0f, 1.0f),
			new Vector2 (1.0f, 0.0f),
			new Vector2 (0.0f, 0.0f)
		};
		
		apply(mesh);
		return mesh;
	}

	public Mesh createEllipse(float x, float y, float w, float h) {
		Mesh mesh = new Mesh(); //mesh.Clear();
		mesh.name = "Ellipse";

		const int div = 32;
		Vector3 [] vertices = new Vector3[div + 2];
		Vector2 [] UV = new Vector2[div + 2];
		int[] triangles = new int[(div + 1) * 3];
		float d = 0.0f;
		float dstep = (Mathf.PI * 2.0f) / (float)div;

		vertices[0].Set(0, 0, 0);
		UV[0].Set(0, 0);
		for (int i = 1; i <= div + 1; i++) {
			float rx = Mathf.Cos(d);
			float ry = Mathf.Sin(d);
			vertices[i].Set(x + rx * w, y + ry * h, 0);
			UV[i].Set(0.5f + rx * 0.5f, 0.5f + ry * 0.5f);
			d += dstep;
			if(i > 0 && i < div + 1) {
				int ti = (i - 1) * 3;
				triangles[ti] = 0;
				triangles[ti + 1] = i + 1;
				triangles[ti + 2] = i;
			}
		}
		mesh.vertices  = vertices;
		mesh.uv        = UV;
		mesh.triangles = triangles;

		apply(mesh);
		return mesh;
	}
	#endregion
}