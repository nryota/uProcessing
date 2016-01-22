using UnityEngine;
using System.Collections;

public class PGameObject : MonoBehaviour {

	public System.Object customData; // UserCustomData

	internal PGraphics graphics;
	public PGraphics g { get { return graphics; } }

	internal GameObject prefabObj = null;
	internal Camera camera = null;

	internal Color _color;
	internal Color _strokeColor;

	internal uint primitiveKey = 0;
	internal uint objFrameCount = 0;

	public enum PrimitiveType {
		None,
		Line,
		Rect,
		Ellipse,
		Box,
		Sphere,
		Text,
		Shape2D,
		Shape3D,
	}
	public PrimitiveType primitiveType = PrimitiveType.None;

	void Awake() {
		if(gameObject.GetComponent<MeshRenderer>() && !wireframe && !isText && !isLine && !isImage) {
			gameObject.AddComponent<PWireframe>();
		}
	}

	protected virtual void Start() {
		//setup();
	}

	#region Processing Object Members
	public virtual void setup() {}
	public virtual void draw() {}

	public PGraphics beginDraw() {
		graphics.beginDraw(this);
		graphics.pushMatrix();
		graphics.pushStyle();
		return graphics;
	}
	public void endDraw() {
		graphics.popStyle();
		graphics.popMatrix();
		graphics.endDraw();
	}

	public PGraphics beginRecycleDraw(short id) {
		beginDraw();
		graphics.beginRecycle(id);
		return graphics;
	}
	public PGraphics beginNoRecycleDraw() {
		beginDraw();
		graphics.beginNoRecycle();
		return graphics;
	}
	public void endRecycleDraw() {
		graphics.endRecycle();
		endDraw();
	}
	public PGraphics beginKeepDraw() {
		beginDraw();
		graphics.beginKeep();
		return graphics;
	}
	public void endKeepDraw() {
		graphics.endKeep();
		endDraw();
	}

	public void translate(float x, float y, float z=0.0f) { gameObject.transform.Translate(graphics.toScene(x, y, z)); }
	public void scale(float s) { scale(s, s, s); }
	//public void scale(float x, float y, float z=1.0f) { gameObject.transform.localScale = new Vector3(x, y, z); }
	public void scale(float x, float y, float z=1.0f) { var s = gameObject.transform.localScale; gameObject.transform.localScale = new Vector3(s.x * x, s.y * y, s.z * z); }
	public void rotate(float angle) { rotateZ(angle); }
	public void rotate(float angle, float x, float y, float z) { transform.RotateAround(Vector3.zero, g.toScene(x, y, z), g.degrees(angle)); }
	public void rotateX(float angle) { transform.Rotate(g.degrees(angle) * g.axis3D.x, 0.0f, 0.0f); }
	public void rotateY(float angle) { transform.Rotate(0.0f, g.degrees(angle) * g.axis3D.y, 0.0f); }
	public void rotateZ(float angle) { transform.Rotate(0.0f, 0.0f, g.degrees(angle) * g.axis3D.z); }
	public void tint(int gray, int alpha = 255) { tint(PGraphics.color(gray, alpha)); }
	public void tint(int r, int g, int b, int a = 255) { tint(PGraphics.color(r, g, b, a)); }
	public void tint(Color col) { graphics.tint(this, col); }
	public void noTint(Color col) { graphics.noTint(this); }
	public void fill(int gray, int alpha = 255) { fill(PGraphics.color(gray, alpha)); }
	public void fill(int r, int g, int b, int a = 255) { fill(PGraphics.color(r, g, b, a)); }
	public void fill(Color col) { graphics.fill(this, col); }
	public void noFill(Color col) { graphics.noFill(this); }
	public void stroke(int gray, int alpha = 255) { stroke(PGraphics.color(gray, alpha)); }
	public void stroke(int r, int g, int b, int a = 255) { stroke(PGraphics.color(r, g, b, a)); }
	public void stroke(Color col) { graphics.stroke(this, col); }
	public void strokeWeight(float weight) { graphics.strokeWeight(this, weight); }
	public void noStroke(Color col) { graphics.noFill(this); }
	#endregion

	#region Processing Extra Members
	public Vector3 pos {
		get { return graphics.toP5Coordinate(transform.position); }
		set { transform.position = graphics.toSceneCoordinate(value); }
	}
	public Vector3 localPos {
		get { return graphics.toP5Coordinate(transform.localPosition); }
		set { transform.localPosition = graphics.toSceneCoordinate(value); }
	}
	public Vector3 scenePos {
		get { return transform.position; }
		set { transform.position = value; }
	}
	public Vector3 localScenePos {
		get { return transform.localPosition; }
		set { transform.localPosition = value; }
	}
	public Quaternion rot {
		get { return transform.localRotation; }
		set { transform.localRotation = value; }
	}
	public Vector3 localScale {
		get { return transform.localScale; }
		set { transform.localScale = value; }
	}
	public Color color {
		get { return _color; }
		set {
			_color = value;
			if(isImage) { tint(_color); }
			else { fill(_color); }
		}
	}
	public Color strokeColor {
		get { return _strokeColor; }
		set { _strokeColor = value; stroke(_strokeColor); }
	}

	public PGameObject addChild(PGameObject obj) {
		if(obj) { obj.transform.parent = transform; }
		return obj;
	}
	
	public Animator animator { get { return GetComponent<Animator>(); } }
	public Animator anim { get { return GetComponent<Animator>(); } }
	public AnimatorStateInfo animState { get { return anim.GetCurrentAnimatorStateInfo(0); } }
	public AnimatorStateInfo animStateInfo(int index) { return anim.GetCurrentAnimatorStateInfo(index); }
	public int animHash(string name) { return Animator.StringToHash(name); }
	public bool isAnimState(string name) { return animHash(name)==animState.nameHash; }

	public Rigidbody addRigid() { return gameObject.AddComponent<Rigidbody>(); }
	public Rigidbody2D addRigid2D() { return gameObject.AddComponent<Rigidbody2D>(); }

	public string label { 
		get { var tm = gameObject.GetComponent<TextMesh>(); return tm ? tm.text : null; }
		set { var tm = gameObject.GetComponent<TextMesh>(); if(tm) { tm.text = value; } }
	}

	public PWireframe wireframe { get { return GetComponent<PWireframe>(); } }
	public bool isLine { get { return GetComponent<LineRenderer>()!=null; } }
	public bool isImage { get { return GetComponent<PImage>()!=null; } }
	public bool isText { get { return GetComponent<TextMesh>()!=null; } }
	public bool is2D { get { return primitiveType==PrimitiveType.Rect || primitiveType==PrimitiveType.Ellipse || primitiveType==PrimitiveType.Line || primitiveType==PrimitiveType.Text || primitiveType==PrimitiveType.Shape2D; } } 
	
	public RaycastHit raycastScreen(float distance=PGraphics.INFINITY, int layerMask=-1) {
		return raycastScreen(g.mouseX, g.mouseY, distance, layerMask);
	}
	
	public RaycastHit raycastScreen(float x, float y, float distance=PGraphics.INFINITY, int layerMask=-1) {
		Ray ray = camera.ScreenPointToRay(new Vector3(g.screenToPixelX(x), g.screenToPixelY(y), Input.mousePosition.z));
		RaycastHit hitInfo;
		Physics.Raycast(ray, out hitInfo, distance, layerMask);
		return hitInfo;
	}

	public bool isHitMouse { get {
			RaycastHit hit = raycastScreen();
			return (hit.collider!=null && hit.collider.gameObject==this.gameObject);
		}
	}

	public void destroy() {
		// ToDo: destroy children
		if(g!=null) {
			g.destroyObject(this);
		} else {
			Destroy(gameObject);
		}
	}
	#endregion
}
