using UnityEngine;
using System.Collections;

public class PGameObject : MonoBehaviour {

	internal PGraphics graphics;
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
