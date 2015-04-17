using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PShape /*: MonoBehaviour*/ {
	public enum CloseType {
		NONE,
		CLOSE,
	}

	public enum ShapeKind {
		NONE,
		//LINE,
		RECT,
		ELLIPSE,
		//ARC,
		//SPHERE,
		//BOX,
		CUSTOM,
	}

	public enum ShapeType {
		NONE,
		//POINTS,
		//LINES,
		TRIANGLES,
		TRIANGLE_FAN,
		TRIANGLE_STRIP,
		QUADS,
		QUAD_STRIP,
		MESH,
	}

	ShapeType shapeType = ShapeType.NONE;
	ShapeKind shapeKind = ShapeKind.NONE;

	Mesh mesh;
	List<Vector3> vertices = new List<Vector3>();
	List<Vector2> uv = new List<Vector2>();
	List<int> triangles = new List<int>();

	public PGraphics.PStyle style;
	internal bool isEnableStyle = true;
	public bool isClockwise = false;

	public PShape(ShapeType type = ShapeType.NONE) {
		style.init(null);
		shapeType = type;
		shapeKind = ShapeKind.NONE;
	}

	#region Processing Members
	public void enableStyle() { isEnableStyle = true; }
	public void disableStyle() { isEnableStyle = false; }

	public void beginShape(int type, bool isClockwise = true) { beginShape((ShapeType)type, isClockwise); }
	public void beginShape(ShapeType type, bool isClockwise = true) {
		if(shapeKind!=ShapeKind.NONE) {
			Debug.LogWarning("beginShape kind warning:" + shapeKind);
		}
		shapeKind = ShapeKind.CUSTOM;
		shapeType = type;
		vertices.Clear();
		uv.Clear();
		triangles.Clear();
		this.isClockwise = isClockwise;
	}

	public void endShape(int closeType) { endShape((CloseType)closeType); }
	public void endShape(CloseType closeType = CloseType.NONE) {
		if(closeType==CloseType.CLOSE) {
			vertex(vertices[0].x, vertices[0].y, vertices[0].z, uv[0].x, uv[0].y);
		}
		//if(shapeType==ShapeType.LINES) { return; }

		mesh = new Mesh();
		if(shapeType==ShapeType.TRIANGLES) {
			mesh.name = "Triangles";
			for(int i=2; i<vertices.Count; i+=3) {
				triangles.Add(i - 2);
				triangles.Add(i - 1);
				triangles.Add(i);
			}
		} else if(shapeType==ShapeType.TRIANGLE_FAN) {
			mesh.name = "TriangleFan";
			for(int i=2; i<vertices.Count; i++) {
				triangles.Add(i);
				triangles.Add(i - 1);
				triangles.Add(0);
			}
		} else if(shapeType==ShapeType.TRIANGLE_STRIP) {
			mesh.name = "TriangleStrip";
			int d = 0;
			for(int i=2; i<vertices.Count; i++) {
				triangles.Add(i - d * 2);
				triangles.Add(i - 1);
				d = (i + 1) & 1;
				triangles.Add(i - d * 2);
			}
		} else if(shapeType==ShapeType.QUADS) {
			mesh.name = "Quads";
			for(int i=0; i<vertices.Count; i+=4) {
				triangles.Add(i + 2);
				triangles.Add(i + 1);
				triangles.Add(i + 0);
				triangles.Add(i + 3);
				triangles.Add(i + 1);
				triangles.Add(i + 2);
			}
		} else if(shapeType==ShapeType.QUAD_STRIP) {
			mesh = new Mesh();
			mesh.name = "QuadStrip";
			for(int i=3; i<vertices.Count; i+=2) {
				triangles.Add(i - 1);
				triangles.Add(i - 2);
				triangles.Add(i - 3);
				triangles.Add(i + 0);
				triangles.Add(i - 2);
				triangles.Add(i - 1);
			}
		}
		mesh.vertices = vertices.ToArray();
		mesh.uv = uv.ToArray();
		if(isClockwise) {
			List<int> revTriangles = new List<int>(triangles);
			revTriangles.Reverse();
			mesh.triangles = revTriangles.ToArray();
		} else {
			mesh.triangles = triangles.ToArray();
		}
		recalc(mesh);
	}

	/*
	void applyLine(GameObject gameObject) {
		if(shapeType==ShapeType.LINES) {
			PGameObject pg = gameObject.GetComponent<PGameObject>();
			LineRenderer line = gameObject.GetComponent<LineRenderer>();
			if(!line) { line = gameObject.AddComponent<LineRenderer>(); }
			PGraphics.PStyle s = isEnableStyle ? style : pg.g.getStyle();
			line.SetWidth(pg.g.toScene(s.strokeWeight), pg.g.toScene(s.strokeWeight));
			line.SetColors(s.strokeColor, s.strokeColor);
			line.SetVertexCount(vertices.Count);
			int i = 0;
			foreach(Vector3 pos in vertices) {
				line.SetPosition(i++, pg.g.toScene(pos));
			}
		}
	}
	*/

	public void vertex(float x, float y, float z=0.0f) {
		vertex(x, y, z, 0.0f, 0.0f);
	}

	public void vertex(float x, float y, float u, float v) {
		vertex(x, y, 0.0f, u, v);
	}

	public void vertex(float x, float y, float z, float u, float v) {
		vertices.Add(new Vector3(x, y, z));
		uv.Add(new Vector2(u, v));
	}

	public void setTint(int gray, int alpha = 255) { tint(gray, alpha); }
	public void setTint(int r, int g, int b, int a = 255) { tint(r, g, b, a); }
	public void setTint(Color col) { tint(col); }
	public void setTint(bool isTint) { if(!isTint) { noTint(); } }
	public void setFill(int gray, int alpha = 255) { fill(gray, alpha); }
	public void setFill(int r, int g, int b, int a = 255) { fill(r, g, b, a); }
	public void setFill(Color col) { fill(col); }
	public void setFill(bool isFill) { style.isFill = isFill; }
	/*
	public void setStroke(int gray, int alpha = 255) { stroke(gray, alpha); }
	public void setStroke(int r, int g, int b, int a = 255) { stroke(r, g, b, a); }
	public void setStroke(Color col) { stroke(col); }
	public void setStroke(bool isStroke) { style.isStroke = isStroke; }
	public void setStrokeWeight(float weight) { strokeWeight(weight); }
	*/
	#endregion

	#region Processing Extra Members
	public void tint(int gray, int alpha = 255) { tint(PGraphics.color(gray, alpha)); }
	public void tint(int r, int g, int b, int a = 255) { tint(PGraphics.color(r, g, b, a)); }
	public void tint(Color col) { style.tintColor = col; }
	public void noTint() { tint(Color.white); }
	public void fill(int gray, int alpha = 255) { fill(PGraphics.color(gray, alpha)); }
	public void fill(int r, int g, int b, int a = 255) { fill(PGraphics.color(r, g, b, a)); }
	public void fill(Color col) { style.fillColor = col; style.isFill = true; }
	public void noFill() { style.isFill = false; }
	/*
	public void stroke(int gray, int alpha = 255) { stroke(PGraphics.color(gray, alpha)); }
	public void stroke(int r, int g, int b, int a = 255) { stroke(PGraphics.color(r, g, b, a)); }
	public void stroke(Color col) { style.strokeColor = col; }
	public void strokeWeight(float weight) { style.strokeWeight = weight; style.isStroke = true; }
	public void noStroke() { style.isStroke = false; }
	*/

	public void saveAssetDB(Mesh mesh) {
		#if UNITY_EDITOR
		var dbAssetName = "Assets/uProcessing/Models/" + mesh.name + ".asset";
		//AssetDatabase.DeleteAsset(dbAssetName);
		AssetDatabase.CreateAsset(mesh, dbAssetName);
		AssetDatabase.SaveAssets();
		#endif
	}

	public void recalc(Mesh mesh) {
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		mesh.Optimize();
	}

	void applyMesh(GameObject gameObject) {
		MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
		if(!meshFilter) { meshFilter = gameObject.AddComponent<MeshFilter>(); }
		MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
		if(!meshRenderer) { meshRenderer = gameObject.AddComponent<MeshRenderer>(); }
		meshRenderer.material = new Material(Shader.Find ("Diffuse"));
		//obj.gameObject.AddComponent<PShape>();
		meshFilter.mesh = mesh;
	}

	public void apply(GameObject gameObject) {
		PGameObject pg = gameObject.GetComponent<PGameObject>();
		if(pg) {
			switch(shapeType) {
			/*
			case ShapeType.LINES:
				applyLine(gameObject);
				pg.primitiveType = PGameObject.PrimitiveType.Shape2D;
				break;
			*/
			default:
				applyMesh(gameObject);
				pg.primitiveType = PGameObject.PrimitiveType.Shape2D;
				break;
			}
		}
	}

	public Mesh createRect(float x, float y, float w, float h) {
		shapeKind = ShapeKind.RECT;
		shapeType = ShapeType.MESH;

		mesh = createQuad(x, y, x+w, y, x+w, y+h, x, y+h);
		mesh.name = "Rect";
		return mesh;
	}

	public Mesh createQuad(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4) {
		shapeKind = ShapeKind.RECT;
		shapeType = ShapeType.MESH;

		mesh = new Mesh();
		mesh.name = "Quad";

		mesh.vertices = new Vector3[]{
			new Vector3 (x1, y1, 0.0f),
			new Vector3 (x2, y2, 0.0f),
			new Vector3 (x3, y3, 0.0f),
			new Vector3 (x4, y4, 0.0f),
		};

		mesh.triangles = new int[]{
			0, 1, 2,
			2, 3, 0
		};

		mesh.uv = new Vector2[]{
			new Vector2 (0.0f, 0.0f),
			new Vector2 (1.0f, 0.0f),
			new Vector2 (1.0f, 1.0f),
			new Vector2 (0.0f, 1.0f),
		};
		
		recalc(mesh);
		return mesh;
	}

	public Mesh createEllipse(float x, float y, float w, float h) {
		shapeKind = ShapeKind.ELLIPSE;
		shapeType = ShapeType.MESH;

		mesh = new Mesh(); //mesh.Clear();
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
				triangles[ti] = i;
				triangles[ti + 1] = i + 1;
				triangles[ti + 2] = 0;
			}
		}
		mesh.vertices  = vertices;
		mesh.uv        = UV;
		mesh.triangles = triangles;

		recalc(mesh);
		return mesh;
	}
	#endregion
}