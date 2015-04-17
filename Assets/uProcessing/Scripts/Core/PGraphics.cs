#define DEBUG_uProcessing

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

// uProcessing Main System Class
public class PGraphics : MonoBehaviour {

	#region Settings
	[SerializeField] private bool isAutoFit = true;
	[SerializeField] private bool isCenter = true;
	[SerializeField] private bool isSetDefaultLight = true;
	[SerializeField] private bool isEnableMaterialPB = true;
	[SerializeField] private float sceneScale = 0.01f;
	[SerializeField] private float depthStep = 0.05f;
	[SerializeField] private bool isEnableDepthStep = true;
	[SerializeField] private bool isEnableFixedUpdate = false;
	#endregion

	#region Primitives
	[System.SerializableAttribute]
	class BasicPrefabs {
		public GameObject line = null;
		public GameObject rect = null;
		public GameObject ellipse = null;
		public GameObject box = null;
		public GameObject sphere = null;
		public GameObject text = null;
	}
	[SerializeField] private BasicPrefabs basicPrefabs = new BasicPrefabs();
	#endregion

	#region Variables
	private Dictionary<uint, GameObject> recyclePrimitives = new Dictionary<uint, GameObject>();
	private List<GameObject> tempPrimitives = new List<GameObject>();
	private List<GameObject> keepPrimitives = new List<GameObject>();
	private Stack<PMatrix> matrixStack = new Stack<PMatrix>();
	private Stack<PStyle> styleStack = new Stack<PStyle>();
	private Stack<uint> keyStack = new Stack<uint>();
	private Stack<bool> recycleStack = new Stack<bool>();
	private Stack<bool> keepStack = new Stack<bool>();
	private Stack<PGameObject> drawParentObjStack = new Stack<PGameObject>();

	private ScreenMode screenMode;
	private const float inv255 = 1.0f / 255.0f;
	private uint workPrimitiveKey;
	private uint workPrimitiveGroupIndex;
	private const uint PrimitiveGroupMult = 0x10000;
	private const uint PrimitiveGroupSetup = 0x80000000;
	private int _fixedUpdateCount = 0;

	public struct PStyle {
		public Color backgroundColor;
		public Color strokeColor;
		public Color fillColor;
		public Color tintColor;
		public bool isFill;
		public bool isStroke;
		public bool isCollision;
		public float strokeWeight;
		public float textSize;
		public int textAlignX;
		public int textAlignY;
		public Font font;
		public int layer;
		public int layerMask;
		public int rectMode;
		public int ellipseMode;
		public int imageMode;
		public bool isRecycle;
		public bool isKeep;

		public void init(PGraphics graphics) {
			backgroundColor = Color.gray;
			strokeColor = Color.black;
			fillColor = Color.white;
			tintColor = Color.white;
			isFill = true;
			isStroke = true;
			strokeWeight = 1;
			textSize = 13;
			textAlignX = LEFT;
			textAlignY = BASELINE;
			font = null;
			if(graphics!=null) {
				layer = graphics.gameObject.layer;
				layerMask = 1 << layer;
			} else {
				layer = -1;
			}
			layerMask = 1 << layer;
			rectMode = CORNER;
			ellipseMode = CENTER;
			imageMode = CORNER;
			isRecycle = false;
			isKeep = false;
		}
	}
	private MaterialPropertyBlock materialPB;

	protected struct SystemObject {
		public GameObject gameObject;
		public Transform work;
		public Transform parent;
		public Transform temp;
		public Camera mainCamera;
		public Camera camera;
		public int cameraNum;
		public PGameObject drawParentObj;
		public PStyle style;

		public void init(PGraphics graphics) {
			gameObject = new GameObject("System");
			gameObject.transform.parent = graphics.transform;
			work = AddChild(new GameObject("Work")).transform;
			parent = graphics.transform;
			temp = graphics.AttachPGameObject(AddChild(new GameObject("Temp")), 0).transform;
			GameObject cameraObj = AddChild(new GameObject("mainCamera"));
			cameraObj.tag = "MainCamera";
			mainCamera = cameraObj.AddComponent<Camera>();
			mainCamera.cullingMask = graphics.system.style.layerMask;
			camera = mainCamera;
			cameraNum = 0;
			drawParentObj = null;
			style.init(graphics);
		}

		public GameObject AddChild(GameObject obj) {
			obj.transform.parent = gameObject.transform;
			return obj;
		}
		
	}
	protected SystemObject system;
	
	private bool isNoLoop = false;
	private bool isLoop = true;
	private bool isClearBG = false;
	private bool isUseBaseCoordinate = false;
	private bool isSetup = false;
	private float fitScale = 1.0f;
	private float invFitScale = 1.0f;
	private Vector3 axis;
	private Vector3 sceneScaleAxis;
	private Vector3 invSceneScaleAxis;
	private long startTicks;
	private int oneKey;
	private int oneKeyCode;
	private float workDepth;
	#endregion

	#region System
	protected virtual void Awake() {
		screenMode = P3D;
		workPrimitiveKey = 1;
		workPrimitiveGroupIndex = 0;
		materialPB = new MaterialPropertyBlock ();
		system.init(this);
		axis = new Vector3(1, -1, -1);
		sceneScaleAxis = new Vector3(sceneScale * axis.x, sceneScale * axis.y, sceneScale * axis.z);
		invSceneScaleAxis = new Vector3(1.0f / sceneScaleAxis.x, 1.0f / sceneScaleAxis.y, 1.0f / sceneScaleAxis.z);
		system.style.init(this);
		pmouseX = 0;
		pmouseY = 0;
		oneKey = NONE;
		oneKeyCode = NONE;
		workDepth = 0.0f;
		_fixedUpdateCount = 0;
		LoadPrimitives();
		workPrimitiveKey = PrimitiveGroupSetup;
		InitMatrix();
		startTicks = DateTime.Now.Ticks;
	}
	
	protected virtual void PreDraw() {
		if(isNoLoop) { isLoop = false; }
		workPrimitiveKey = 1;
		workPrimitiveGroupIndex = 0;
		pmouseX = mouseX;
		pmouseY = mouseY;
		workDepth = 0.0f;
		system.camera = system.mainCamera;
		system.cameraNum = 0;
		//system.style.init(this);
		InitMatrix();
		layer(gameObject.layer);
		layerMask(1 << gameObject.layer);
	}
	
	void InitMatrix() {
		system.work.localPosition = Vector3.zero;
		system.work.localRotation = Quaternion.identity;
		system.work.localScale = Vector3.one;
	}

	protected virtual void Start() {
		isSetup = true;
		background(128);
		PreSetup();
		setup();
		isSetup = false;
		PreDraw();
	}

	protected virtual void PreSetup() {}

	protected virtual void Update() {
		UpdateOneKey();
		if(isEnableFixedUpdate) {
			for(int i=0; i<_fixedUpdateCount || i<1; i++) {
				if(i>0) { PreDraw(); }
				UpdateOneLoop();
			}
		} else {
			UpdateOneLoop();
		}
		if(_fixedUpdateCount<=0) _fixedUpdateCount--;
		else _fixedUpdateCount = 0;
		UpdateOneInput();
	}

	protected virtual void UpdateOneLoop() {
		if(isLoop) {
			if(system.camera && !isClearBG && frameCount>2) {
				system.camera.clearFlags = CameraClearFlags.Depth;
			}
			isClearBG = false;
			clear();
			draw();
			DrawPGameObjects();
		}
	}

	protected virtual void DrawPGameObjects() {
		foreach(KeyValuePair<uint, GameObject> pair in recyclePrimitives) {
			if(!pair.Value) continue;
			PGameObject pobj = pair.Value.GetComponent<PGameObject>();
			if(pobj!=null) { pobj.draw(); pobj.objFrameCount++; }
		}
		foreach(GameObject obj in keepPrimitives) {
			if(!obj) continue;
			PGameObject pobj = obj.GetComponent<PGameObject>();
			if(pobj!=null) { pobj.draw(); pobj.objFrameCount++; }
		}
		foreach(GameObject obj in tempPrimitives) {
			if(!obj) continue;
			PGameObject pobj = obj.GetComponent<PGameObject>();
			if(pobj!=null) { pobj.draw(); pobj.objFrameCount++; }
		}
	}

	protected virtual void UpdateOneInput() {
		if(mouseButtonDown!=NONE) { onMousePressed(); }
		if(mouseReleased) { onMouseReleased(); }
		if(mouseX!=pmouseX || mouseY!=pmouseY) {
			onMouseMoved();
			if(mousePressed) { onMouseDragged(); }
		}
		if(keyPressed) {
			onKeyPressed();
			if(Input.anyKeyDown) { onKeyTyped(); }
		}
	}
	
	protected virtual void LateUpdate() {
		if(isLoop) {
			PreDraw();
		}
	}

	protected virtual void FixedUpdate() {
		_fixedUpdateCount++;
	}

	protected virtual void finish() {}
	protected virtual void PreDestory() {}

	protected virtual void OnDestroy() {
		PreDestory();
		finish();
		dispose();
	}

	static KeyCode[] atozKeys = {
		KeyCode.A, KeyCode.B, KeyCode.C, KeyCode.D, KeyCode.E, KeyCode.F, KeyCode.G,
		KeyCode.H, KeyCode.I, KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.M, KeyCode.N,
		KeyCode.O, KeyCode.P, KeyCode.Q, KeyCode.R, KeyCode.S, KeyCode.T, KeyCode.U,
		KeyCode.V, KeyCode.W, KeyCode.X, KeyCode.Y, KeyCode.Z,
	};

	static KeyCode[] numKeys = {
		KeyCode.Alpha0, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
		KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9,
		KeyCode.Keypad0, KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4,
		KeyCode.Keypad5, KeyCode.Keypad6, KeyCode.Keypad7, KeyCode.Keypad8, KeyCode.Keypad9,
	};

	void UpdateOneKey() {
		if(isKeyDown(KeyCode.LeftShift) || isKeyDown(KeyCode.RightShift)) oneKeyCode = SHIFT;
		else if(isKeyDown(KeyCode.LeftControl) || isKeyDown(KeyCode.RightControl)) oneKeyCode = CTRL;
		else {
			if(isKeyDown(KeyCode.Space)) oneKey = ' ';
			else if(isKeyDown(KeyCode.UpArrow)) oneKeyCode = UP;
			else if(isKeyDown(KeyCode.DownArrow)) oneKeyCode = DOWN;
			else if(isKeyDown(KeyCode.LeftArrow)) oneKeyCode = LEFT;
			else if(isKeyDown(KeyCode.RightArrow)) oneKeyCode = RIGHT;
			else if(isKeyDown(KeyCode.Backspace)) oneKey = BACKSPACE;
			else if(isKeyDown(KeyCode.Tab)) oneKey = TAB;
			else if(isKeyDown(KeyCode.Return) || isKeyDown(KeyCode.KeypadEnter)) oneKey = RETURN;
			else if(isKeyDown(KeyCode.Escape)) oneKey = ESC;
			else if(isKeyDown(KeyCode.Delete)) oneKey = DELETE;
			else {
				for(int i=0; i<atozKeys.Length; i++) {
					if(!isKeyDown(atozKeys[i])) continue;

					if(isKey(KeyCode.LeftShift) || isKeyDown(KeyCode.RightShift)) { oneKey = 'A' + i; }
					else { oneKey = 'a' + i; }
					break;
				}
				for(int i=0; i<numKeys.Length; i++) {
					if(isKeyDown(numKeys[i])) { oneKey = '0' + (i % 10); break; }
				}
			}
			return;
		}
		oneKey = CODED;
	}

	void LoadPrimitives() {
		if(!basicPrefabs.line) { basicPrefabs.line = Resources.Load("PPrefabs/Line") as GameObject; }
		if(!basicPrefabs.rect) { basicPrefabs.rect = Resources.Load("PPrefabs/Rect") as GameObject; }
		if(!basicPrefabs.ellipse) { basicPrefabs.ellipse = Resources.Load("PPrefabs/Ellipse") as GameObject; }
		if(!basicPrefabs.box) { basicPrefabs.box = Resources.Load("PPrefabs/Box") as GameObject; }
		if(!basicPrefabs.sphere) { basicPrefabs.sphere = Resources.Load("PPrefabs/Sphere") as GameObject; }
		if(!basicPrefabs.text) { basicPrefabs.text = Resources.Load("PPrefabs/Text") as GameObject; }
	}

	private PGameObject GetPrimitive() {
		GameObject obj = null;
		if(system.style.isRecycle && !system.style.isKeep) {
			++workPrimitiveKey;
			recyclePrimitives.TryGetValue(workPrimitiveKey, out obj);
		}
		PGameObject pg = obj ? obj.GetComponent<PGameObject>() : null;
		return pg;
	}

	private PGameObject AddPrimitive(GameObject obj) {
		if(!obj) return null;
		uint key = (system.style.isRecycle && !system.style.isKeep) ? workPrimitiveKey : 0;
		PGameObject pg = AttachPGameObject(obj, key);
		if(key > 0) {
			recyclePrimitives.Add(key, obj);
		} else if(system.style.isKeep) {
			pg.primitiveKey = 0;
			keepPrimitives.Add(obj);
		} else {
			tempPrimitives.Add(obj);
			obj.name = obj.name + "/(Temp)";
		}

		return pg;
	}

	private PGameObject AttachPGameObject(GameObject obj, uint key) {
		PGameObject pg = obj.GetComponent<PGameObject>();
		if(!pg) { pg = obj.AddComponent<PGameObject>(); }
		pg.graphics = this;
		if(key!=0) pg.primitiveKey = key;
		return pg;
	}

	private Camera AddCamera() {
		if(system.cameraNum==0 || isSetup) {
			system.camera = system.mainCamera;
		} else {
			var obj = pobject("subCamera");
			system.AddChild(obj.gameObject);
			Camera camera = obj.GetComponent<Camera>();
			if(!camera) {
				camera = obj.gameObject.AddComponent<Camera>();
				camera.clearFlags = CameraClearFlags.Depth; 
			}
			//obj.tag = "MainCamera";
			if(system.camera.cullingMask == system.style.layerMask) {
				int newLayer = system.style.layer + 1; // temp
				layer(newLayer);
				layerMask(1 << newLayer);
			}
			system.camera = camera;
		}
		system.cameraNum++;
		system.camera.cullingMask = system.style.layerMask;
		return system.camera;
	}

	private void SetProperty(PGameObject obj, Vector3 pos, Vector3 scale) {
		if(obj) {
			var trans = obj.transform;
			var parent = (system.style.isRecycle || system.style.isKeep || system.parent.transform!=gameObject.transform) ? system.parent : system.temp;
			if(system.drawParentObj) { parent = system.drawParentObj.transform; }
			if(parent && (trans.parent==null || !trans.parent.Equals(parent.transform))) trans.parent = parent.transform;

			Vector3 newPos = system.work.localPosition + toSceneCoordinate(pos);

			if(obj.is2D && isEnableDepthStep) {
				workDepth -= sceneScale * depthStep;
				Vector3 depth = Vector3.Scale(system.camera.transform.forward, new Vector3(workDepth, workDepth, workDepth));
				newPos += depth;
			}
			obj.transform.localPosition = newPos;

			trans.localRotation = system.work.localRotation;
			trans.localScale = Vector3.Scale(system.work.localScale, toScene(scale));
			if(obj.renderer) {
				Color col;
				if(obj.isImage) col = system.style.tintColor;
				else if(obj.isLine) col = system.style.strokeColor;
				else col = system.style.fillColor;

				if(obj.isImage || system.style.isFill) SetColor(obj, col);
				else noFill(obj);

				if(system.style.isStroke) {
					obj._strokeColor = system.style.strokeColor;
					stroke(obj, system.style.strokeColor);
					strokeWeight(obj, system.style.strokeWeight);
				} else noStroke(obj);
			}

			obj.gameObject.layer = system.style.layer;
			if(obj.objFrameCount==0) { obj.setup(); }
		}
	}

	private void SetColor(PGameObject obj, Color col) {
		if(obj.isText) {
			TextMesh tm = obj.GetComponent<TextMesh>();
			if(tm) { tm.color = col; }
		} else if(obj.renderer) {
			obj._color = col;
			if(isEnableMaterialPB) {
				materialPB.Clear();
				materialPB.AddColor("_Color", col);
				obj.renderer.SetPropertyBlock(materialPB);
			} else if(obj.renderer.material) {
				obj.renderer.material.color = col;
			}
			obj.renderer.enabled = true;
		}
	}

	#region PGameObject Utility
	public void tint(PGameObject obj, Color col) {
		if(obj.isImage) { SetColor(obj, col); }
	}

	public void noTint(PGameObject obj) {
		if(obj.isImage) { tint(obj, Color.white); }
	}

	public void fill(PGameObject obj, Color col) {
		if(!obj.isImage) SetColor(obj, col);
	}

	public void noFill(PGameObject obj) {
		if(!obj.isImage && obj.renderer) {
			obj.renderer.enabled = false;
		}
	}

	public void stroke(PGameObject obj, Color col) {
		PWireframe pw = obj.wireframe;
		if(pw) {
			pw.isStroke = true;
			pw.strokeColor = col;
		}
	}

	public void strokeWeight(PGameObject obj, float weight) {
		PWireframe pw = obj.wireframe;
		if(pw) {
			pw.strokeWeight = weight * sceneScale;
		}
	}

	public void noStroke(PGameObject obj) {
		PWireframe pw = obj.wireframe;
		if(pw) {
			pw.isStroke = false;
		}
	}
	#endregion

	public Vector3 toAxis(Vector3 v) {
		return Vector3.Scale(v, axis);
	}
	
	public Vector3 toAxis(float x, float y, float z) {
		return new Vector3(x * axis.x, y * axis.y, z * axis.z);
	}

	public Vector3 toScene(Vector3 v) {
		return Vector3.Scale(v, sceneScaleAxis);
	}

	public Vector3 toScene(float x, float y, float z) {
		return new Vector3(x * sceneScaleAxis.x, y * sceneScaleAxis.y, z * sceneScaleAxis.z);
	}

	public Vector3 toSceneXYz(float x, float y, float z) {
		return new Vector3(x * sceneScaleAxis.x, y * sceneScaleAxis.y, z * sceneScale);
	}

	public float toScene(float n) { return n * sceneScale; }
	public float toSceneX(float x) { return x * sceneScaleAxis.x; }
	public float toSceneY(float y) { return y * sceneScaleAxis.y; }
	public float toSceneZ(float z) { return z * sceneScaleAxis.z; }


	public Vector3 toSceneCoordinate(Vector3 v) {
		return toSceneCoordinate(v.x, v.y, v.z);
	}

	public Vector3 toSceneCoordinate(float x, float y, float z) {
		if(isUseBaseCoordinate) {
			return new Vector3(baseX(x) * sceneScaleAxis.x, baseY(y) * sceneScaleAxis.y, z * sceneScaleAxis.z);
		} else {
			return toScene(x, y, z);
		}
	}

	public Vector3 toP5(Vector3 v) {
		return Vector3.Scale(v, invSceneScaleAxis);
	}
	
	public Vector3 toP5(float x, float y, float z) {
		return new Vector3(x * invSceneScaleAxis.x, y * invSceneScaleAxis.y, z * invSceneScaleAxis.z);
	}
	
	public float toP5X(float x) { return x * invSceneScaleAxis.x; }
	public float toP5Y(float y) { return y * invSceneScaleAxis.y; }
	public float toP5Z(float z) { return z * invSceneScaleAxis.z; }

	public Vector3 toP5Coordinate(Vector3 v) {
		return toP5Coordinate(v.x, v.y, v.z);
	}
	
	public Vector3 toP5Coordinate(float x, float y, float z) {
		if(isUseBaseCoordinate) {
			return new Vector3(x * invSceneScaleAxis.x - baseX(0), y * invSceneScaleAxis.y - baseY(0), z * invSceneScaleAxis.z);
		} else {
			return toP5(x, y, z);
		}
	}
	
	private Rect GetAspectRect(float x, float y, float w, float h) {
		float aspect = w / h;
		x /= w;
		y /= h;
		if(w >= h) {
			w = 1.0f;
			h = 1.0f / aspect;
		} else {
			w = aspect;
			h = 1.0f;
		}
		if(!isAutoFit) {
			w /= displayScale;
			h /= displayScale;
			y = 1 - invFitScale;
		}
		y += 1.0f - h;
		x += displayOffsetX / displayWidth;
		y -= displayOffsetY / displayHeight;
		return new Rect(x, y, w, h);
	}
	#endregion

	#region Processing Events
	protected virtual void setup() {}
	protected virtual void draw() {}
	protected virtual void dispose() {}
	protected virtual void onMousePressed() {}
	protected virtual void onMouseReleased() {}
	protected virtual void onMouseMoved() {}
	protected virtual void onMouseDragged() {}
	protected virtual void onKeyPressed() {}
	protected virtual void onKeyTyped() {}
	#endregion

	#region Processing System Members
	public enum ScreenMode {
		P2D, P3D, U2D, U3D,
	}
	public const ScreenMode P2D = ScreenMode.P2D;
	public const ScreenMode P3D = ScreenMode.P3D;
	public const ScreenMode U2D = ScreenMode.U2D;
	public const ScreenMode U3D = ScreenMode.U3D;
	public const int CORNER = -6;
	public const int CENTER = -5;
	public const int TOP = -4;
	public const int BOTTOM = -3;
	public const int BASELINE = -2;
	public const int CODED = -1;
	public const int NONE = 0; // extra
	public const int UP = 1;
	public const int DOWN = 2;
	public const int LEFT = 3;
	public const int RIGHT = 4;
	public const int MIDDLE = 5;
	public const int SHIFT = 10;
	public const int CTRL = 11;
	public const int BACKSPACE = 20;
	public const int TAB = 21;
	public const int ENTER = 22;
	public const int RETURN = ENTER;
	public const int ESC = 23;
	public const int DELETE = 24;

	//public const int LINE = (int)PShape.ShapeKind.LINE;
	public const int RECT = (int)PShape.ShapeKind.RECT;
	public const int ELLIPSE = (int)PShape.ShapeKind.ELLIPSE;
	//public const int ARC = (int)PShape.ShapeKind.ARC;
	//public const int SPHERE = (int)PShape.ShapeKind.SPHERE;
	//public const int BOX = (int)PShape.ShapeKind.BOX;
	
	//public const int POINTS = (int)PShape.ShapeType.POINTS;
	//public const int LINES = (int)PShape.ShapeType.LINES;
	public const int TRIANGLES = (int)PShape.ShapeType.TRIANGLES;
	public const int TRIANGLE_FAN = (int)PShape.ShapeType.TRIANGLE_FAN;
	public const int TRIANGLE_STRIP = (int)PShape.ShapeType.TRIANGLE_STRIP;
	public const int QUADS = (int)PShape.ShapeType.QUADS;
	public const int QUAD_STRIP = (int)PShape.ShapeType.QUAD_STRIP;

	public const int CLOSE = (int)PShape.CloseType.CLOSE;

	public int displayWidth { get { return Screen.width; } }
	public int displayHeight { get { return Screen.height; } }
	public int frameCount { get { return Time.frameCount; } }
	public int day() { return DateTime.Now.Day; }
	public int hour() { return DateTime.Now.Hour; }
	public int millis() { return (int)((DateTime.Now.Ticks - startTicks) / 10000 ); }
	public int minute() { return DateTime.Now.Minute; }
	public int month() { return DateTime.Now.Month; }
	public int second() { return DateTime.Now.Second; }
	public int year() { return DateTime.Now.Year; }
	public float PI { get { return Mathf.PI; } }
	public float HALF_PI { get { return Mathf.PI * 0.5f; } }
	public float QUATER_PI { get { return Mathf.PI * 0.25f; } }
	public float TWO_PI { get { return Mathf.PI * 2.0f; } }
	public float abs(float f) { return Mathf.Abs(f); }
	public int ceil(float f) { return (int)Mathf.Ceil(f); }
	public float constrain(float amt, float low, float high) { return Mathf.Clamp(amt, low, high); }
	public float dist(float x1, float y1, float x2, float y2) {
		return mag(x1 - x2, y1 - y2);
	}
	public float dist(float x1, float y1, float z1, float x2, float y2, float z2) {
		return mag(x1 - x2, y1 - y2, z1 - z2);
	}
	public float exp(float f) { return Mathf.Exp(f); }
	public int floor(float f) { return (int)Mathf.Floor(f); }
	public float lerp(float start, float stop, float amt) { return Mathf.Lerp(start, stop, amt); }
	public float log(float f) { return Mathf.Log(f); }
	public float mag(float x, float y) { return Mathf.Sqrt(x * x + y * y); }
	public float mag(float x, float y, float z) { return Mathf.Sqrt(x * x + y * y + z * z); }
	public float map(float value, float start1, float stop1, float start2, float stop2) {
		return start2 + norm(value, start1, stop1) * (stop2 - start2);
	}
	public float min(float a, float b) { return Mathf.Min(a, b); }
	public float min(float a, float b, float c) { return Mathf.Min(Mathf.Min(a, b), c); }
	public float min(params float[] values) { return Mathf.Min(values); }
	public float max(float a, float b) { return Mathf.Max(a, b); }
	public float max(float a, float b, float c) { return Mathf.Max(Mathf.Max(a, b), c); }
	public float max(params float[] values) { return Mathf.Max(values); }
	public int min(int a, int b) { return Math.Min(a, b); }
	public int min(int a, int b, int c) { return Math.Min(Mathf.Min(a, b), c); }
	public int max(int a, int b) { return Math.Max(a, b); }
	public int max(int a, int b, int c) { return Math.Max(Math.Max(a, b), c); }
	public float norm(float value, float start, float stop) { return value / (stop - start); }
	public float pow(float n, float e) { return Mathf.Pow(n, e); }
	public int round(float f) { return (int)Mathf.Round(f); }
	public float sq(float f) { return f * f; }
	public float sqrt(float f) { return Mathf.Sqrt(f); }
	public float acos(float f) { return Mathf.Acos(f); }
	public float asin(float f) { return Mathf.Asin(f); }
	public float atan(float f) { return Mathf.Atan(f); }
	public float atan2(float y, float x) { return Mathf.Atan2(y, x); }
	public float cos(float f) { return Mathf.Cos(f); }
	public float sin(float f) { return Mathf.Sin(f); }
	public float tan(float f) { return Mathf.Tan(f); }
	public float degrees(float rad) { return Mathf.Rad2Deg * rad; }
	public float radians(float deg) { return Mathf.Deg2Rad * deg; }
	public float noise(float x) { return Mathf.PerlinNoise(x, 0); }
	public float noise(float x, float y) { return Mathf.PerlinNoise(x, y); }
	public float random(float low, float high) { return UnityEngine.Random.Range(low, high); }
	public float random(float high) { return UnityEngine.Random.Range(0.0f, high); }
	public float randomSeed(int seed) { return UnityEngine.Random.seed = seed; }
	public int random(int high) { return UnityEngine.Random.Range(0, high); }
	public int random(int low, int high) { return UnityEngine.Random.Range(low, high); }
	private string ns(char c, int count) { return new String(c, count); }
	public string nf(int num, int digits) { return num.ToString("d" + digits); }
	public string nf(float num, int left, int right) { return num.ToString(ns('0', left) + "." + ns('0', right)); }
	public string nfs(int num, int digits) { return num.ToString(" d" + digits + ";" + "-d" + digits); }
	public string nfs(float num, int left, int right) { string s = ns('0', left) + "." + ns('0', right); return num.ToString(" " + s + ";-" + s); }
	public string nfp(int num, int digits) { return num.ToString("+d" + digits + ";" + "-d" + digits); }
	public string nfp(float num, int left, int right) { string s = ns('0', left) + "." + ns('0', right); return num.ToString("+" + s + ";-" + s); }
	public string nfc(int num) { return num.ToString("#,0"); }
	public string nfc(int num, int right) { return num.ToString("0,." + right); }
	public string[] split(string value, char delim) { return value.Split(delim); }
	public string[] splitTokens(string value, string delim = "¥t¥n¥r¥f ") { return value.Split(delim.ToCharArray(), StringSplitOptions.None); }
	public string trim(string value) { return value.Trim(); }
	public string join(string[] stringArray, string separator) { return string.Join(separator, stringArray); }

	private float _width;
	private float _height;
	private float _screenWidth;
	private float _screenHeight;
	public float width { get { return _width * fitScale; } }
	public float height { get { return _height * fitScale; } }
	public float baseWidth { get { return _width; } }
	public float baseHeight { get { return _height; } }

	public bool mousePressed { get { return Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2); } }
	public int mouseButton {
		get { 
			if(Input.GetMouseButton(0)) return LEFT;
			else if(Input.GetMouseButton(1)) return RIGHT;
			else if(Input.GetMouseButton(2)) return MIDDLE;
			else return NONE;
		}
	}

	public int mouseX { get { return (int)pixelToScreenX(Input.mousePosition.x); } }
	public int mouseY { get { return (int)pixelToScreenY(Input.mousePosition.y); } }
	public int pmouseX { get; private set; } 
	public int pmouseY { get; private set; } 
	
	public bool keyPressed { get { return Input.anyKey; } }
	public int key { get { return oneKey; } }
	public int keyCode { get { return oneKeyCode; } }

	public static Color color(int gray, int alpha = 255) {
		return new Color(gray * inv255, gray * inv255, gray * inv255, alpha * 255);
	}

	public static Color color(int r, int g, int b, int a = 255) {
		return new Color(r * inv255, g * inv255, b * inv255, a * inv255);
	}
	
	public Camera size(float w, float h, ScreenMode mode = P2D, float scale = 0.0f) {
		screenMode = mode;
		if(scale > 0.0f) { sceneScale = scale; }

		float dh = (float)displayHeight / h;
		float dw = (float)displayWidth / w;

		//bool isScaling = isP5;
		bool isScaling = true; 

		if(isScaling) { _displayScale = min(dh, dw); }
		else { _displayScale = 1.0f; }

		if(_displayScale < 1.0f) isAutoFit = true;

		if(isAutoFit) { fitScale = max(dh, dw); }
		else { fitScale = 1.0f; }
		invFitScale = 1.0f / fitScale;

		_width = w * invFitScale;
		_height = h * invFitScale;
		_screenWidth = displayWidth / displayScale;
		_screenHeight = displayHeight / displayScale;

		_displayOffsetX = 0;
		_displayOffsetY = 0;
		if(isCenter && isScaling) {
			if(isAutoFit) {
				_displayOffsetX = (displayWidth - width * displayScale) * 0.5f;
			} else {
				_displayOffsetX = (displayWidth - width) * 0.5f;
				_displayOffsetY = (displayHeight - height) * 0.5f;
			}
		}

		if(isP5) { axis.Set(1, -1, -1); }
		else { axis.Set(1, 1, 1); }

		sceneScaleAxis.Set(sceneScale * axis.x, sceneScale * axis.y, sceneScale * axis.z);
		invSceneScaleAxis = new Vector3(1.0f / sceneScaleAxis.x, 1.0f / sceneScaleAxis.y, 1.0f / sceneScaleAxis.z);

		//println("size: " + 0 + ", " + 0 + ", " + width + ", " + height + " (x " + fitScale + ")");
		//println("screen: " + screenOffsetX + ", " + screenOffsetY + ", " + screenWidth + ", " + screenHeight);
		//println("display: " + displayOffsetX + ", " + displayOffsetY + ", " + displayWidth + ", " + displayHeight + " (x " + displayScale + ")");

		system.cameraNum = 0;
		if(is2D) { ortho(); }
		else { perspective(); }

		if(isSetDefaultLight && !is2D) {
			beginKeep();
			lights();
			endKeep();
		} else {
			noLights();
		}

		system.camera.cullingMask = system.style.layerMask;
		return system.camera;
	}

	public void clear() {
		foreach(GameObject obj in tempPrimitives) {
			Destroy(obj);
		}
		tempPrimitives.Clear();
	}

	public void background(int gray) {
		background(gray, gray, gray);
	}

	public void background(PImage img) {
		clear();
		system.style.backgroundColor = Color.black;
		isClearBG = true;
		if(system.camera) {
			system.camera.backgroundColor = system.style.backgroundColor;
			system.camera.clearFlags = CameraClearFlags.Depth;
			beginNoRecycle();
			pushStyle();
				noStroke();
				tint(255);
				imageMode(CORNER);
				image(img, 0, 0, width, height);
			popStyle();
			endRecycle();
		}
	}

	public void background(int r, int g, int b, int a=255) {
		clear();
		system.style.backgroundColor = color(r, g, b, a);
		isClearBG = true;
		if(system.camera) {
			system.camera.backgroundColor = system.style.backgroundColor;
			if(a >= 255) {
				system.camera.clearFlags = CameraClearFlags.SolidColor;
			} else {
				system.camera.clearFlags = CameraClearFlags.Depth;
				if(a > 0) {
					beginNoRecycle();
					pushStyle();
						noStroke();
						fill(system.style.backgroundColor);
						rectMode(CORNER);
						rect(0, 0, width, height);
					popStyle();
					endRecycle();
				}
			}
		}
	}
	
	public void loop() { isLoop = true; isNoLoop = false; }
	public void noLoop() { isNoLoop = true; }

	public void println(object message) { Debug.Log(message); }

	#pragma warning disable 108
	public Camera camera() {
		return camera(width/2.0f, height/2.0f, (height/2.0f) / tan(PI*30.0f / 180.0f),
		              width/2.0f, height/2.0f, 0.0f, 0.0f, 1.0f, 0.0f);
	}

	public Camera camera(float eyeX, float eyeY, float eyeZ, float centerX, float centerY, float centerZ, float upX, float upY, float upZ) {
		AddCamera();
		if(system.camera) {
			system.camera.transform.localPosition = toScene(eyeX, eyeY, eyeZ);
			system.camera.transform.LookAt(toScene(centerX, centerY, centerZ), toScene(upX, upY, upZ));
			system.camera.backgroundColor = system.style.backgroundColor;
		}
		return system.camera;
	}
	#pragma warning restore 108


	public Camera ortho() {
		return ortho(0.0f, width, 0.0f, height);
	}

	public Camera ortho(float left, float right, float bottom, float top, float near=0.3f, float far=1000.0f) {
		AddCamera();
		if(system.camera) {
			float x = (sceneScaleAxis.x * left) < (sceneScaleAxis.x * right) ? sceneScaleAxis.x * left : sceneScaleAxis.x * right;
			float y = 0;//(sceneScaleAxis.y * bottom) < (sceneScaleAxis.y * top) ? sceneScaleAxis.y * bottom : sceneScaleAxis.y * top;
			system.camera.rect = GetAspectRect(x, y, abs(right-left), abs(bottom-top) * displayAspectW);
			system.camera.nearClipPlane = near * sceneScale;
			system.camera.farClipPlane = far * sceneScale;
			system.camera.orthographic = true;
			system.camera.orthographicSize = abs(bottom-top) * sceneScale * 0.5f;
			if(screenMode==P2D || screenMode==P3D) {
				system.camera.transform.localPosition = toScene(width * 0.5f, height * 0.5f, -height * 0.8660254f * axis.z);
			} else {
				//system.camera.transform.localPosition = toScene(0, 0, -1.0f * sceneScale * axis.z);
				system.camera.transform.localPosition = toScene(0, 0, -height * 0.8660254f * axis.z);
			}
		}
		return system.camera;
	}

	public Camera perspective() {
		float fov = PI/3.0f;
		float cameraZ = (height/2.0f) / tan(fov/2.0f);
		if(screenMode==P2D || screenMode==P3D) {
			return perspective(PI/3.0f, width/height, cameraZ/10.0f, cameraZ*10.0f);
		} else {
			return perspective(PI/3.0f, width/height, 0.3f, 1000.0f);
		}
	}

	public Camera perspective(float fovy, float aspect, float near, float far) {
		AddCamera();
		if(system.camera) {
			system.camera.fieldOfView = degrees(fovy);
			system.camera.nearClipPlane = near * sceneScale;
			system.camera.farClipPlane = far * sceneScale;
			float w = height * aspect;
			float h = height * displayAspectW;
			system.camera.rect = GetAspectRect(0, 0, w, h);
			if(screenMode==P2D || screenMode==P3D) {
				system.camera.transform.localPosition = toScene(width * 0.5f, height * 0.5f, -height * 0.8660254f * axis.z);
			} else {
				//system.camera.transform.localPosition = toScene(0, 0, -1.0f * sceneScale * axis.z);
				system.camera.transform.localPosition = toScene(0, 0, -height * 0.8660254f * axis.z);
			}
		}
		return system.camera;
	}

	public void pushStyle() {
		PStyle ps = system.style;
		styleStack.Push(ps);
	}

	public void popStyle() {
		system.style = styleStack.Pop();
	}
	
	public void style(PStyle style) { // extra
		int layer = system.style.layer;
		int layerMask = system.style.layerMask;
		system.style = style;
		if(style.layer < 0) {
			system.style.layer = layer;
			system.style.layerMask = layerMask;
		}
	}

	public void pushMatrix() {
		matrixStack.Push(new PMatrix(system.work.localToWorldMatrix));
	}

	public void popMatrix() {
		if(matrixStack.Count > 0) {
			PMatrix m = matrixStack.Pop();
			system.work.set(m);
		}
	}

	/*
	public PGameObject pushMatrix(PGameObject pg) {
		if(pg) {
			system.parent = pg.transform;
			system.parent.transform.localPosition = system.work.localPosition;
			system.work.localPosition = new Vector3();
			pg.gameObject.layer = style.layer;
		}
		return pg;
	}

	public PGameObject pushMatrix(string name) {
		pushMatrix();
		PGameObject pg = GetPrimitive();
		if(!pg) {
			pg = AddPrimitive(new GameObject(name));
			pg.transform.parent = system.parent.transform;
		}
		return pushMatrix(pg);
	}
	*/
	public PGameObject pushMatrix(string name) {
		pushMatrix();
		PGameObject child = GetPrimitive();
		if(!child) {
			child = AddPrimitive(new GameObject(name));
			child.transform.parent = system.parent.transform;
		}
		if(child) {
			system.parent = child.transform;
			system.parent.transform.localPosition = system.work.localPosition;
			 system.parent.transform.localRotation = system.work.localRotation;
			 system.parent.transform.localScale = system.work.localScale;
			system.work.localPosition = new Vector3();
			 system.work.localRotation = Quaternion.identity;
			 system.work.localScale = Vector3.one;
			child.gameObject.layer = system.style.layer;
		}
		return child;
	}

	public GameObject popMatrix(string name) {
		if(system.parent.transform.parent) {
			system.parent = system.parent.transform.parent.transform;
			system.work.localPosition = Vector3.zero;
			system.work.localRotation = Quaternion.identity;
			system.work.localScale = Vector3.one;
		}
		popMatrix();
		return system.parent.gameObject;
	}

	public void printMatrix() {
		println(getMatrix ());
	}

	public PMatrix getMatrix() {
		var m = new PMatrix(system.work.localToWorldMatrix);
		if(system.parent) {
			m.set(system.parent.transform.localToWorldMatrix * m.m);
		}
		var tempM = new PMatrix(m);
		tempM.transpose();
		m.set3x3(tempM);
		m.m03 *= invSceneScaleAxis.x;
		m.m13 *= invSceneScaleAxis.y;
		m.m23 *= invSceneScaleAxis.z;
		return m;
	}

	public void beginDraw(PGameObject obj=null) {
		drawParentObjStack.Push(system.drawParentObj);
		system.drawParentObj = obj;
	}

	public void endDraw() {
		system.drawParentObj = drawParentObjStack.Pop();
	}

	public void translate(float x, float y, float z=0.0f) {
		system.work.Translate(toSceneCoordinate(x, y, z));
	}
	
	public void rotate(float angle) {
		rotateZ(angle);
	}

	public void rotateX(float angle, float x, float y, float z) {
		system.work.RotateAround(Vector3.zero, toScene(x, y, z), degrees(angle));
	}

	public void rotateX(float angle) {
		system.work.Rotate(degrees(angle) * axis.x, 0.0f, 0.0f);
	}

	public void rotateY(float angle) {
		system.work.Rotate(0.0f, degrees(angle) * axis.y, 0.0f);
	}

	public void rotateZ(float angle) {
		system.work.Rotate(0.0f, 0.0f, degrees(angle) * axis.z);
	}

	public void scale(float s) {
		scale(s, s, s);
	}

	public void scale(float x, float y, float z=1.0f) {
		Vector3 s = system.work.localScale;
		system.work.localScale = new Vector3(x * s.x, y * s.y, z * s.z);
	}
	
	public void fill(int gray, int alpha = 255) {
		fill(gray, gray, gray, alpha);
	}
	
	public void fill(int r, int g, int b, int a = 255) {
		system.style.isFill =  true;
		system.style.fillColor = color(r, g, b, a);
	}
	
	public void noFill() {
		system.style.isFill =  false;
	}

	public void tint(int gray, int alpha = 255) {
		tint(gray, gray, gray, alpha);
	}
	
	public void tint(int r, int g, int b, int a = 255) {
		system.style.tintColor = color(r, g, b, a);
	}

	public void noTint() {
		system.style.tintColor = Color.white;
	}

	public void stroke(int gray, int alpha = 255) {
		stroke(gray, gray, gray, alpha);
	}

	public void stroke(int r, int g, int b, int a = 255) {
		system.style.isStroke = true;
		system.style.strokeColor = color(r, g, b, a);
	}

	public void noStroke() {
		system.style.isStroke = false;
	}

	public void strokeWeight(float weight) {
		system.style.strokeWeight = weight;
	}

	public PGameObject point(float x, float y, float z = 0.0f) {
		//float s = 1.0f * fitScale;
		float s = 1.0f;
		float sw = system.style.strokeWeight;
		strokeWeight(s);
		PGameObject obj = line(x, y, z, x + s , y, z);
		obj.name = "Point(Clone)";
		strokeWeight(sw);
		return obj;
	}
	
	public PGameObject line(float x1, float y1, float x2, float y2) {
		return line(x1, y1, 0, x2, y2, 0);
	}

	public PGameObject line(float x1, float y1, float z1, float x2, float y2, float z2) {
		PGameObject obj = GetPrimitive();
		if(!obj) { obj = AddPrimitive(Instantiate(basicPrefabs.line) as GameObject); }
		var l = obj.gameObject.GetComponent<LineRenderer>();
		l.SetWidth(system.style.strokeWeight * sceneScale, system.style.strokeWeight * sceneScale);
		l.SetVertexCount(2);
		l.SetPosition(0, toSceneCoordinate(x1, y1, z1));
		l.SetPosition(1, toSceneCoordinate(x2, y2, z2));
		float invScene = 1.0f / sceneScale;
		SetProperty(obj, Vector3.zero, new Vector3(axis.x * invScene, axis.y * invScene, axis.z * invScene));
		return obj;
	}

	public void rectMode(int mode) { // CORNER or CENTER
		assert(mode==CORNER || mode==CENTER);
		system.style.rectMode = mode;
	}

	private Vector3 getRectPos(int rectMode, float x, float y, float w, float h) {
		Vector3 p;
		if(rectMode==CORNER) {
			if(isUseBaseCoordinate) {
				p = new Vector3(x + (w * 0.5f) * axis.x, y + (h * 0.5f) * axis.y, 0.0f);
			} else {
				p = new Vector3(x + (w * 0.5f) * axis.x, y + (h * 0.5f) * -axis.y, 0.0f);
			}
		}
		else p = new Vector3(x, y, 0.0f);
		return p;
	}

	public PGameObject rect(float x, float y, float w, float h) {
		PGameObject obj = GetPrimitive();
		if(!obj) { obj = AddPrimitive(Instantiate(basicPrefabs.rect) as GameObject); }
		Vector3 p = getRectPos(system.style.rectMode, x, y, w, h);
		SetProperty(obj, p, new Vector3(w, h, sceneScaleAxis.z)); // rect prefab side
		return obj;
	}
	
	public void ellipseMode(int mode) { // CORNER or CENTER
		assert(mode==CORNER || mode==CENTER);
		system.style.ellipseMode = mode;
	}

	public PGameObject ellipse(float x, float y, float w, float h) {
		PGameObject obj = GetPrimitive();
		if(!obj) { obj = AddPrimitive(Instantiate(basicPrefabs.ellipse) as GameObject); }
		Vector3 p;
		if(system.style.ellipseMode==CORNER) p = new Vector3(x + (w * 0.5f) * axis.x, y + (h * 0.5f) * axis.y, 0.0f);
		else p = new Vector3(x, y, 0.0f);
		SetProperty(obj, p, new Vector3(w, h, sceneScaleAxis.z)); // ellipse prefab side
		return obj;
	}
	
	public PGameObject box(float size) {
		return box(size, size, size);
	}

	public PGameObject box(float x, float y, float z) {
		PGameObject obj = GetPrimitive();
		if(!obj) { obj = AddPrimitive(Instantiate(basicPrefabs.box) as GameObject); }
		SetProperty(obj, Vector3.zero, new Vector3(x, y, z));
		return obj;
	}
	
	public PGameObject sphere(float r) {
		PGameObject obj = GetPrimitive();
		if(!obj) { obj = AddPrimitive(Instantiate(basicPrefabs.sphere) as GameObject); }
		r *= 2.0f;
		SetProperty(obj, Vector3.zero, new Vector3(r, r, r));
		return obj;
	}

	public PShape createShape(int kind, params float[] p) { return createShape((PShape.ShapeKind)kind, p); }
	public PShape createShape(PShape.ShapeKind kind, params float[] p) {
		PShape shape = new PShape();
		switch(kind) {
		case PShape.ShapeKind.RECT:
			shape.createRect(p[0], p[1], p[2], p[3]);
			break;
		case PShape.ShapeKind.ELLIPSE:
			shape.createEllipse(p[0], p[1], p[2], p[3]);
			break;
		}
		return shape;
	}

	public PShape createShape(int type) { return createShape((PShape.ShapeType)type); }
	public PShape createShape(PShape.ShapeType type = PShape.ShapeType.NONE) { return new PShape(type); }

	public PGameObject shape(PShape shape, float x, float y, float z = 0.0f) {
		PGameObject obj = GetPrimitive();
		if(!obj) { obj = AddPrimitive(new GameObject("shape")); }
		shape.apply(obj.gameObject);

		if(shape.isEnableStyle) {
			pushStyle();
			style(shape.style);
		}

		SetProperty(obj, new Vector3(x, y, z), toP5(sceneScaleAxis.x, sceneScaleAxis.y, sceneScaleAxis.z));

		if(shape.isEnableStyle) {
			popStyle();
		}

		return obj;
	}

	public void textSize(float size) {
		system.style.textSize = size;
	}

	public void textAlign(int alignX, int alignY = BASELINE) {
		assert(alignX==LEFT || alignX==CENTER || alignX==RIGHT);
		assert(alignY==TOP || alignY==CENTER || alignY==BOTTOM || alignY==BASELINE);
		system.style.textAlignX = alignX; // LEFT, CENTER, RIGHT
		system.style.textAlignY = alignY; // TOP, CENTER, BTOTTOM, BASELINE
	}

	public Font loadFont(string fontname) {
		//return new Font (fontname);
		string fontPath = getResourceName(fontname);
		Font font = Resources.Load(fontPath, typeof(Font)) as Font;
		if (font == null) {
			debuglogWaring("loadFont <NotFound> " + fontPath);
		} else {
			debuglog("loadFont " + fontPath);
		}
		return font;
	}

	public void textFont(Font font) {
		system.style.font = font;
	}

	public void textFont(string fontname) {
		textFont(loadFont(fontname));
	}

	public PGameObject text(string str, float x, float y) {
		PGameObject obj = GetPrimitive();
		assert(!obj || obj.isText, "recycle error");
		if(!obj) { obj = AddPrimitive(Instantiate(basicPrefabs.text) as GameObject); }
		TextMesh tm = obj.GetComponent<TextMesh>();
		if(tm) {
			tm.color = system.style.fillColor;
			tm.characterSize = 10 * invFitScale;
			tm.fontSize = (int)(system.style.textSize * fitScale);
			if(system.style.font!=null) {
				tm.font = system.style.font;
				obj.renderer.material = system.style.font.material;
			}
			tm.text = str;
			tm.anchor = TextAnchor.MiddleCenter;
			if(system.style.textAlignX==LEFT) {
				switch(system.style.textAlignY) {
					case TOP: tm.anchor = TextAnchor.UpperLeft; break;
					case CENTER: tm.anchor = TextAnchor.MiddleLeft; break;
					case BASELINE: tm.anchor = TextAnchor.MiddleLeft; break;
					case BOTTOM: tm.anchor = TextAnchor.LowerLeft; break;
				}
			} else if(system.style.textAlignX==RIGHT) {
				switch(system.style.textAlignY) {
					case TOP: tm.anchor = TextAnchor.UpperRight; break;
					case CENTER: tm.anchor = TextAnchor.MiddleRight; break;
					case BASELINE: tm.anchor = TextAnchor.MiddleRight; break;
					case BOTTOM: tm.anchor = TextAnchor.LowerRight; break;
				}
			} else {
				switch(system.style.textAlignY) {
					case TOP: tm.anchor = TextAnchor.UpperCenter; break;
					case CENTER: tm.anchor = TextAnchor.MiddleCenter; break;
					case BASELINE: tm.anchor = TextAnchor.MiddleCenter; break;
					case BOTTOM: tm.anchor = TextAnchor.LowerCenter; break;
				}
			}
			if(system.style.textAlignY==BASELINE) {
				y -= system.style.textSize * 0.3f;
			}
		}
		SetProperty(obj, new Vector3(x, y, 0), axis);
		return obj;
	}

	public PImage loadImage(string path) {
		PImage img = system.work.gameObject.AddComponent<PImage>();
		img.graphics = this;
		img.load(path);
		img.gameObject.SetActive(false);
		return img;
	}

	/*public void removeCache(PImage img) {
		PImage[] comps = system.work.gameObject.GetComponentsInChildren<PImage>();
		PImage obj  = ( from c in comps where c==img select c );
		if(obj) { Destroy(obj); }
	}*/

	public void imageMode(int mode) { // CORNER or CENTER
		assert(mode==CORNER || mode==CENTER);
		system.style.imageMode = mode;
	}

	public PGameObject image(PImage img, float x, float y) {
		return image(img, x, y, img.width, img.height);
	}

	public PGameObject image(PImage img, float x, float y, float w, float h) {
		PGameObject obj = GetPrimitive();
		if(!obj) { obj = AddPrimitive(Instantiate(basicPrefabs.rect) as GameObject); }
		Vector3 p = getRectPos(system.style.imageMode, x, y, w, h);
		PImage objImg = obj.gameObject.GetComponent<PImage>();
		if(!objImg) {
			obj.name = "Image(Clone)";
			objImg = obj.gameObject.AddComponent<PImage>();
			objImg.graphics = this;
			if(obj.wireframe) Destroy(obj.gameObject.GetComponent<PWireframe>());
		}
		objImg.set(img);
		SetProperty(obj, p, new Vector3(w, h, sceneScaleAxis.z)); // rect prefab side
		return obj;
	}

	public void noLights() {
		RenderSettings.ambientLight = Color.white;
		Light[] lights = GetComponentsInChildren<Light>();
		foreach(Light light in lights) {
			if(light.gameObject.layer==system.style.layer) {
				light.enabled = false;
			}
		}
	}

	public void lights() {
		noLights();
		ambientLight(128, 128, 128);
		directionalLight(128, 128, 128, 0, 0, -1);
	}

	public void ambientLight(int r, int g, int b) {
		RenderSettings.ambientLight = color(r, g, b);
	}

	public PGameObject directionalLight(int r, int g, int b, float nx, float ny, float nz) {
		var obj = pobject("DirectionalLight");
		Light light = obj.GetComponent<Light>();
		if(!light) { light = obj.gameObject.AddComponent<Light>(); }
		light.enabled = true;
		light.color = color(r, g, b);
		light.type = LightType.Directional;
		light.cullingMask = system.style.layerMask;
		obj.transform.localRotation = obj.transform.localRotation * Quaternion.LookRotation(toAxis(nx, ny, nz));
		return obj;
	}

	[System.Diagnostics.Conditional("DEBUG_uProcessing")]
	public void assert(bool condition, string message = null) {
		if(!condition) {
			if(message==null) { message = "assertion failed"; }
			Debug.LogError(message);
		}
	}

	public delegate void OnLoadCompleteStringArray(string[] text);

	// load resource-file ex) loadString("path/name");
	// load www-file ex) loadStrings("http://xxx.html", (texts) => { saveStrings(Application.dataPath + "out.txt", texts); });
	public string[] loadStrings(string filename, OnLoadCompleteStringArray onComplete=null) {
		if(filename.StartsWith("http://") || filename.StartsWith("file://")) {
			loadTextFromURL(filename, onComplete);
		} else {
			string t = tryLoadTextFromResources(filename);
			if(t==null) {
				t = loadTextFromLocalFile(filename);
			}
			if(t!=null) {
				string[] stringArray = t.Replace("\r\n","\n").Split('\n');
				if(onComplete!=null) { onComplete(stringArray); }
				return stringArray;
			}
		}
		return null;
	}
	
	public JSONArray loadJSONArray(string filename, Action<JSONArray> onComplete = null) {
		JSONArray jsonArray = new JSONArray();
		jsonArray.load(this, filename, onComplete);
		return jsonArray;
	}
	
	public JSONObject loadJSONObject(string filename, Action<JSONObject> onComplete = null) {
		JSONObject jsonObject = new JSONObject();
		jsonObject.load(this, filename, onComplete);
		return jsonObject;
	}

	public void saveStrings(string filename, string[] data) {
		saveTextToLocalFile(filename, data);
	}

	public bool saveJSONArray(JSONArray jsonArray, string filename) {
		if(jsonArray==null) return false;
		return jsonArray.save (this, filename);
	}
	
	public bool saveJSONObject(JSONObject jsonObject, string filename) {
		if(jsonObject==null) return false;
		return jsonObject.save (this, filename);
	}
	#endregion
	
	#region Processing Extra Members
	public void debuglog(object message) { Debug.Log(message); }
	public void debuglogWaring(object message) { Debug.LogWarning(message); }
	public void debuglogError(object message) { Debug.LogError(message); }

	public bool is2D { get { return (screenMode==P2D || screenMode==U2D); } }
	public bool is3D { get { return !is2D; } }
	public bool isP5 { get { return (screenMode==P2D || screenMode==P3D); } }
	public bool isProcessing { get { return isP5; } }
	public bool isUnity { get { return !isP5; } }

	public const float INFINITY = Mathf.Infinity;
	public float deltaTime { get { return Time.deltaTime; } }
	public int fixedUpdateCount { get { return _fixedUpdateCount > 0 ? _fixedUpdateCount : 1; } }
	public Vector2 axis2D { get { return new Vector2(axis.x, axis.y); } }
	public Vector3 axis3D { get { return axis; } }
	public float sceneDepthStep { get { return depthStep * sceneScaleAxis.z; } }

	private float _displayOffsetX;
	private float _displayOffsetY;
	private float _displayScale;
	public static float displayAspectW { get { return (float)Screen.width / Screen.height; } }
	public static float displayAspectH { get { return (float)Screen.height / Screen.width; } }
	public float displayOffsetX { get { return _displayOffsetX; } }
	public float displayOffsetY { get { return _displayOffsetY; } }
	public float displayScale { get { return _displayScale; } }

	public float screenOffsetX { get { return 0; } }
	public float screenOffsetY { get { return 0; } }
	public float screenWidth { get { return _screenWidth; } }
	public float screenHeight { get { return _screenHeight; } }

	public Vector3 pixelToWorld(float x, float y) {
		return system.camera.ScreenToWorldPoint(new Vector3(x, displayHeight - y, system.camera.nearClipPlane));
	}
	public Vector3 worldToPixel(float x, float y, float z=0.0f) {
		return system.camera.WorldToScreenPoint(new Vector3(x, y, z));
	}
	public Vector3 worldToScreen(float x, float y, float z=0.0f) {
		Vector3 v = system.camera.WorldToScreenPoint(new Vector3(x, y, z));
		v.x = pixelToScreenX(v.x);
		v.y = pixelToScreenY(v.y);
		//v.z = 0.0f;
		return v;
	}

	//public float baseN(float n) { return n; }
	public float baseX(float x) { return (isUnity) ? x - width * 0.5f : x; }
	public float baseY(float y) { return (isUnity) ? height * 0.5f - y: y; }

	public float pixelN(float n) { return (isAutoFit || isUnity) ? n / _displayScale : n; }
	public float pixelX(float x) { return pixelToScreenX(x); }
	public float pixelY(float y) { return pixelToScreenY(y); }

	public float pixelToScreenX(float x) {
		if(axis.x <= 0.0f) { x = displayWidth - x; }
		if(isUnity) {
			//x -= displayWidth * 0.5f;
			x -= displayOffsetX;
		} else {
			x -= displayOffsetX;
		}
		if(isAutoFit || isUnity) { x /= _displayScale; }
		return x;
	}
	
	public float pixelToScreenY(float y) {
		if(axis.y <= 0.0f) { y = displayHeight - y; }
		if(isUnity) {
			//y = displayHeight * 0.5f - y; 
			y = displayHeight - y; 
			y -= displayOffsetY;
		} else {
			y -= displayOffsetY;
		}
		if(isAutoFit || isUnity) { y /= _displayScale; }
		return y;
	}
	
	public float screenToPixelX(float x) {
		if(isAutoFit || isUnity) { x *= _displayScale; }
		if(isUnity) {
			x += displayWidth * 0.5f;
		} else {
			x += displayOffsetX;
		}
		if(axis.x <= 0.0f) { x = displayWidth - x; }
		return x;
	}
	
	public float screenToPixelY(float y) {
		if(isAutoFit || isUnity) { y *= _displayScale; }
		if(isUnity) {
			y = displayHeight * 0.5f - y;
		} else {
			y += displayOffsetY;
		}
		if(axis.y <= 0.0f) { y = displayHeight - y; }
		return y;
	}

	public void layout2D() {
		isEnableDepthStep = true;
		if(isUnity) { isUseBaseCoordinate = true; } // Use baseX(), baseY() automatic
	}
	public void layout3D() {
		isEnableDepthStep = false;
		if(isUnity) { isUseBaseCoordinate = false; }
	}
	public void setLayoutDepth(float depthLevel) {
		workDepth = -depthLevel * sceneScale * depthStep;
	}
	public float getLayoutDepth() {
		return -workDepth / sceneScale / depthStep;
	}

	internal PStyle getStyle() { return system.style; }

	public bool mouseReleased { get { return mouseButtonUp != NONE; } }
	public int mouseButtonUp {
		get { 
			if(Input.GetMouseButtonUp(0)) return LEFT;
			else if(Input.GetMouseButtonUp(1)) return RIGHT;
			else if(Input.GetMouseButtonUp(2)) return MIDDLE;
			else return NONE;
		}
	}
	public int mouseButtonDown {
		get { 
			if(Input.GetMouseButtonDown(0)) return LEFT;
			else if(Input.GetMouseButtonDown(1)) return RIGHT;
			else if(Input.GetMouseButtonDown(2)) return MIDDLE;
			else return NONE;
		}
	}

	public bool isKey(KeyCode keyCode) { return Input.GetKey(keyCode); }
	public bool isKeyDown(KeyCode keyCode) { return Input.GetKeyDown(keyCode); }
	public bool isKeyUp(KeyCode keyCode) { return Input.GetKeyUp(keyCode); }
	public bool isKey(string keyName) { return Input.GetKey(keyName); }
	public bool isKeyDown(string keyName) { return Input.GetKeyDown(keyName); }
	public bool isKeyUp(string keyName) { return Input.GetKeyUp(keyName); }

	public float inputAxis(string name) { return Input.GetAxis(name); }
	public float inputX { get { return Input.GetAxis("Horizontal"); } }
	public float inputY { get { return Input.GetAxis("Vertical"); } }

	#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBPLAYER
	public int touchCount { get { return mousePressed ? 1 : 0; } }
	public bool touchPressed { get { return mousePressed; } }
	public bool touchReleased { get { return mouseReleased; } }
	public Vector2 touch(int index) { return new Vector2(mouseX, mouseY); }
	#else
	public int touchCount { get { return Input.touchCount; } }
	public bool touchPressed { get { return mousePressed; } }
	public bool touchReleased { get { return mouseReleased; } }
	public Vector2 touch(int index) {
		Touch t = Input.GetTouch(index);
		return new Vector2(pixelToScreenX(t.position.x), pixelToScreenY(t.position.y));
	}
	#endif

	public string B_FIRE1 { get { return "Fire1"; } }
	public string B_FIRE2 { get { return "Fire2"; } }
	public string B_FIRE3 { get { return "Fire3"; } }
	public string B_JUMP { get { return "Jump"; } }
	public bool isButton(string name) { return Input.GetButton(name); }
	public bool isButtonDown(string name) { return Input.GetButtonDown(name); }
	public bool isButtonUp(string name) { return Input.GetButtonDown(name); }

	public void layerAndMask(int layerIndex) { layer(layerIndex); layerMask(1 << layerIndex); }
	public void layerAndMask(string layerName) { layer(layerName); layerMask(layerName); }
	public void layer(int layerIndex) { system.style.layer = layerIndex; }
	public void layer(string layerName) { system.style.layer = LayerMask.NameToLayer(layerName); }
	public void layerMask(int layerBits) { system.style.layerMask = layerBits; }
	public void layerMask(string layerName) { system.style.layerMask = 1 << LayerMask.NameToLayer(layerName); }
	public void layerMaskEverything() { system.style.layerMask = -1; }
	public void addLayerMask(int layerIndex) { system.style.layerMask |= 1 << layerIndex; }
	public void addLayerMask(string layerName) { system.style.layerMask |= 1 << LayerMask.NameToLayer(layerName); }
	public void removeLayerMask(int layerIndex) { system.style.layerMask &= ~(1 << layerIndex); }
	public void removeLayerMask(string layerName) { system.style.layerMask &= ~(1 << LayerMask.NameToLayer(layerName)); }

	public void backgroundSkybox() {
		clear();
		if(system.camera) {
			system.camera.clearFlags = CameraClearFlags.Skybox;
		}
	}

	public void clearAll() {
		clear();
		recyclePrimitives.Clear();
	}

	public void fill(Color c) { fill((int)(c.r*255), (int)(c.g*255), (int)(c.b*255), (int)(c.a*255)); }
	public void stroke(Color c) { stroke((int)(c.r*255), (int)(c.g*255), (int)(c.b*255), (int)(c.a*255)); }
	public void tint(Color c) { tint((int)(c.r*255), (int)(c.g*255), (int)(c.b*255), (int)(c.a*255)); }

	public void recycle() { system.style.isRecycle = true; }
	public void noRecycle() { system.style.isRecycle = false; }
	public void beginRecycle(short id) {
		if(id==0) {
			++workPrimitiveGroupIndex;
			if(workPrimitiveGroupIndex>=short.MaxValue) { Debug.LogError("over recycle groups", this); }
			id = (short)workPrimitiveGroupIndex; 
		}
		recycleStack.Push(system.style.isRecycle);
		keyStack.Push(workPrimitiveKey);
		workPrimitiveKey = (uint)id * PrimitiveGroupMult;
		recycle();
	}
	public void beginNoRecycle() {
		recycleStack.Push(system.style.isRecycle);
		keyStack.Push(workPrimitiveKey);
		noRecycle();
	}
	public void endRecycle() {
		workPrimitiveKey = keyStack.Pop();
		system.style.isRecycle = recycleStack.Pop();
	}
	
	public void keep() { system.style.isKeep = true; }
	public void noKeep() { system.style.isKeep = false; }
	public void beginKeep() {
		keepStack.Push(system.style.isKeep);
		keep();
	}
	public void endKeep() {
		system.style.isKeep = keepStack.Pop();
		noKeep();
	}

	public T prefab<T>(string path, float x=0.0f, float y=0.0f, float z=0.0f, float sx=1.0f, float sy=1.0f, float sz=1.0f) where T : MonoBehaviour {
		PGameObject pobj = prefab(path, x, y, z, sx, sy, sz);
		if(!pobj) return null;
		T comp = pobj.GetComponent<T>();
		if(!comp) { comp = pobj.gameObject.AddComponent<T>(); }
		return comp;
	}

	public PGameObject prefab(string path, float x=0.0f, float y=0.0f, float z=0.0f, float sx=1.0f, float sy=1.0f, float sz=1.0f) {
		PGameObject obj = GetPrimitive();
		if(!obj) {
			GameObject prefab = Resources.Load(path) as GameObject;
			if(!prefab) {
				debuglogWaring("prefab <NotFound> " + path);
				return null;
			}
			obj = AddPrimitive(Instantiate(prefab) as GameObject);
		}
		SetProperty(obj, new Vector3(x, y, z), toP5(sx, sy, sz));
		return obj;
	}

	public PGameObject pobject(string name, float x=0.0f, float y=0.0f, float z=0.0f, float sx=1.0f, float sy=1.0f, float sz=1.0f) {
		PGameObject obj = GetPrimitive();
		if(!obj) { obj = AddPrimitive(new GameObject(name)); }
		SetProperty(obj, new Vector3(x, y, z), toP5(sx, sy, sz));
		return obj;
	}

	public PGameObject createPrefab(string path, float x=0.0f, float y=0.0f, float z=0.0f, float sx=1.0f, float sy=1.0f, float sz=1.0f) {
		beginKeep();
		PGameObject obj = prefab(path, x, y, z, sx, sy, sz);
		endKeep();
		return obj;
	}

	public PGameObject createObject(string name="PGameObject", float x=0.0f, float y=0.0f, float z=0.0f, float sx=1.0f, float sy=1.0f, float sz=1.0f) {
		beginKeep();
		GameObject obj = new GameObject(name);
		PGameObject pg = AddPrimitive(obj);
		SetProperty(pg, new Vector3(x, y, z), toP5(sx, sy, sz));
		endKeep();
		return pg;
	}

	public void destroyObject(PGameObject obj) {
		if(!obj) return;
		//debuglog("destroyObject " + obj.gameObject.name);
		if(obj.primitiveKey==0) {
			if(!keepPrimitives.Contains(obj.gameObject)) {
				debuglogWaring("destroyObject: keepPrimitives <Not Found> " + obj.gameObject.name);
			}
			keepPrimitives.Remove(obj.gameObject);
		} else if(recyclePrimitives.ContainsKey(obj.primitiveKey)) {
			if(!recyclePrimitives.ContainsKey(obj.primitiveKey)) {
				debuglogWaring("destroyObject: recyclePrimitives <Not Found> " + obj.primitiveKey + " " + obj.gameObject.name);
			}
			recyclePrimitives.Remove(obj.primitiveKey);
		}
		Destroy(obj.gameObject);
	}

	public string getResourceName(string path) {
		string directory = Path.GetDirectoryName(path);
		if(directory.Length > 0) { directory += "/"; }
		return directory + Path.GetFileNameWithoutExtension(path);
	}

	private string tryLoadTextFromResources(string path) {
		string resourcePath = getResourceName(path);
		TextAsset textAsset = Resources.Load(resourcePath, typeof(TextAsset)) as TextAsset;
		if(textAsset!=null) { debuglog("loadTextFromResources " + resourcePath); }
		return (textAsset!=null) ? textAsset.text : null;
	}

	public string loadTextFromResources(string path) {
		string text = tryLoadTextFromResources(path);
		if(text==null) { debuglogWaring("loadTextFromResources <Failed> " + path); }
		return text;
	}
	
	public void loadTextFromURL(string url, OnLoadCompleteStringArray onComplete) {
		debuglog("loadTextFromURL loading... " + url);
		StartCoroutine(
			loadTextFromURL_corutine(url,
			    text => {
					debuglog("loadTextFromURL load complete " + url);
						string[] stringArray = text.Replace("\r\n","\n").Split('\n');
						if(onComplete!=null) { onComplete(stringArray); }
				}
			)
		);
	}
	
	public void loadTextFromURL(string url, Action<string> onComplete) {
		debuglog("loadTextFromURL loading... " + url);
#if false
		WWW web = new WWW(url);
		while(!web.isDone) {
			if(Time.realtimeSinceStartup > time_start + 10.0f) {
				debuglogError("loadTextFromURL: <TimeOut> " + url);
			}
		}
		debuglog("loadTextFromURL load complete " + url);
		if(onComplete!=null) { onComplete(web.text); }
#else
		StartCoroutine(
			loadTextFromURL_corutine(url,
			    text => {
					debuglog("loadTextFromURL load complete " + url);
					if(onComplete!=null) { onComplete(text); }
				}
			)
		);
#endif
	}
	
	public IEnumerator loadTextFromURL_corutine(string url, Action<string> onCompleate) {
		WWW web = new WWW(url);
		yield return web;
		onCompleate(web.text);
	}
	
	public string loadTextFromLocalFile(string path) {
		string result = null;
		#if UNITY_WEBPLAYER
		debuglogWaring("loadTextFromLocalFile: <NotWorking on WebPlayer>");
		#else
		if(!File.Exists(path)) {
			debuglogWaring("loadTextFromLocalFile <Not Found> " + path);
			return null;
		}
		debuglog("loadTextFromLocalFile " + path);
		FileInfo fi = new FileInfo(path);
		using (StreamReader sr = new StreamReader(fi.OpenRead())) {
			result = sr.ReadToEnd();
		}
		#endif
		return result;
	}
	
	public string loadStringText(string filename, Action<string> onComplete=null) {
		if(filename.StartsWith("http://") || filename.StartsWith("file://")) {
			loadTextFromURL(filename, onComplete);
		} else {
			string t = tryLoadTextFromResources(filename);
			if(t==null) {
				t = loadTextFromLocalFile(filename);
			}
			if(t!=null) {
				if(onComplete!=null) { onComplete(t); }
				return t;
			}
		}
		return null;
	}

	public void saveTextToLocalFile(string path, string[] data) {
		#if UNITY_WEBPLAYER
		debuglogWaring("saveTextToLocalFile: <NotWorking on WebPlayer>");
		#else
		debuglog("saveTextToLocalFile " + path);
		FileInfo fi = new FileInfo(path);
		using(StreamWriter sw = fi.CreateText()) {
			for(int i=0; i<data.Length; i++) {
				sw.WriteLine(data[i]);
			}
			sw.Flush();
		}
		#endif
	}

	public void saveStringText(string path, string data) {
		#if UNITY_WEBPLAYER
		debuglogWaring("saveStringText: <NotWorking on WebPlayer>");
		#else
		debuglog("saveStringText " + path);
		FileInfo fi = new FileInfo(path);
		using(StreamWriter sw = fi.CreateText()) {
			sw.Write(data);
			sw.Flush();
		}
		#endif
	}

	public RaycastHit raycast(Vector3 origin, Vector3 direction, float distance=INFINITY, int layerMask=-1) {
		RaycastHit hitInfo = new RaycastHit();
		Physics.Raycast(origin, direction, out hitInfo, distance, layerMask);
		return hitInfo;
	}

	public RaycastHit2D raycast(Vector2 origin, Vector2 direction, float distance=INFINITY, int layerMask=-1) {
		return Physics2D.Raycast(origin, direction, distance, layerMask);
	}

	public RaycastHit raycastScreen(float distance=INFINITY, int layerMask=-1) {
		return raycastScreen(mouseX, mouseY, distance, layerMask);
	}

	public RaycastHit raycastScreen(float x, float y, float distance=INFINITY, int layerMask=-1) {
		Ray ray = system.camera.ScreenPointToRay(new Vector3(screenToPixelX(x), screenToPixelY(y), Input.mousePosition.z));
		RaycastHit hitInfo;
		Physics.Raycast(ray, out hitInfo, distance, layerMask);
		return hitInfo;
	}

	public void loadScene(string name) { Application.LoadLevel(name); }
	public void addScene(string name) { Application.LoadLevelAdditive(name); }
	#endregion
}