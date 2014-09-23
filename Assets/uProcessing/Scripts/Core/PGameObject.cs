using UnityEngine;
using System.Collections;

public class PGameObject : MonoBehaviour {

	internal PGraphics graphics;
	public PGraphics g { get { return graphics; } }

	internal uint primitiveKey = 0;

	public enum PrimitiveType {
		None,
		Line,
		Rect,
		Ellipse,
		Box,
		Sphere,
		Text,
	}
	public PrimitiveType primitiveType = PrimitiveType.None;

	void Awake() {
		if(gameObject.GetComponent<MeshRenderer>() && !wireframe && !isText && !isLine && !isImage) {
			gameObject.AddComponent<PWireframe>();
		}
	}

	#region Processing Object Members
	public PGraphics beginDraw() {
		graphics.beginDraw(this);
		graphics.pushMatrix();
		graphics.pushStyle();
		return graphics;
	}
	public void endDraw() {
		graphics.endDraw();
	}

	public PGraphics beginRecycleDraw(short id) {
		graphics.beginDraw(this);
		graphics.beginRecycle(id);
		graphics.pushMatrix();
		graphics.pushStyle();
		return graphics;
	}
	public void endRecycleDraw() {
		graphics.popStyle();
		graphics.popMatrix();
		graphics.endKeep();
		graphics.endRecycle();
	}

	public PGraphics beginKeepDraw() {
		graphics.beginDraw(this);
		graphics.beginKeep();
		graphics.pushMatrix();
		graphics.pushStyle();
		return graphics;
	}
	public void endKeepDraw() {
		graphics.popStyle();
		graphics.popMatrix();
		graphics.endKeep();
		graphics.endDraw();
	}

	public void translate(float x, float y, float z=0.0f) { gameObject.transform.Translate(graphics.toScene(x, y, z)); }
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
		get { return transform.localPosition; }
		set { transform.localPosition = value; }
	}
	public Quaternion rot {
		get { return transform.localRotation; }
		set { transform.localRotation = value; }
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

	public PWireframe wireframe { get { return GetComponent<PWireframe>(); } }
	public bool isLine { get { return GetComponent<LineRenderer>()!=null; } }
	public bool isImage { get { return GetComponent<PImage>()!=null; } }
	public bool isText { get { return GetComponent<TextMesh>()!=null; } }

	public RaycastHit raycastScreen() { return graphics.raycastScreen(); }
	public bool isHitMouse { get {
			RaycastHit hit = graphics.raycastScreen();
			return (hit.collider!=null && hit.collider.gameObject==this.gameObject);
		}
	}

	public bool is2D { get { return primitiveType==PrimitiveType.Rect || primitiveType==PrimitiveType.Ellipse || primitiveType==PrimitiveType.Line || primitiveType==PrimitiveType.Text; } } 

	public void destroy() { Destroy(gameObject); }
	#endregion
}
