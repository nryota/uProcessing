#define DEBUG_uProcessing

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// uProcessing Main System Class
public class PGraphics : MonoBehaviour {

	#region Settings
	[SerializeField] private bool isAutoFit = true;
	[SerializeField] private bool isSetDefaultLight = true;
	[SerializeField] private float sceneScale = 0.01f;
	[SerializeField] private float depthStep = 0.05f;
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
	private Dictionary<uint, GameObject> primitives = new Dictionary<uint, GameObject>();
	private List<GameObject> tempPrimitives = new List<GameObject>();
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

	private struct PStyle {
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
		public int layer;
		public int layerMask;
		public int rectMode;
		public int ellipseMode;
		public int imageMode;

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
			layer = graphics.gameObject.layer;
			layerMask = 1 << layer;
			rectMode = CORNER;
			ellipseMode = CENTER;
			imageMode = CORNER;
		}
	}
	private PStyle style;

	private struct SystemObject {
		public GameObject gameObject;
		public Transform work;
		public Transform parent;
		public Transform temp;
		public Camera mainCamera;
		public Camera camera;
		public int cameraNum;
		public PGameObject drawParentObj;

		public void init(PGraphics graphics) {
			gameObject = new GameObject("System");
			gameObject.transform.parent = graphics.transform;
			work = AddChild(new GameObject("Work")).transform;
			parent = graphics.transform;
			temp = graphics.AttachPGameObject(AddChild(new GameObject("Temp")), 0).transform;
			GameObject cameraObj = AddChild(new GameObject("mainCamera"));
			cameraObj.tag = "MainCamera";
			mainCamera = cameraObj.AddComponent<Camera>();
			mainCamera.cullingMask = graphics.style.layerMask;
			camera = mainCamera;
			cameraNum = 0;
			drawParentObj = null;
		}

		public GameObject AddChild(GameObject obj) {
			obj.transform.parent = gameObject.transform;
			return obj;
		}
		
	}
	private SystemObject system;
	
	private bool isNoLoop = false;
	private bool isLoop = true;
	private bool isRecycle = false;
	private bool isKeep = false;
	private bool isClearBG = false;
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
	void Awake() {
		screenMode = P3D;
		workPrimitiveKey = 1;
		workPrimitiveGroupIndex = 0;
		system.init(this);
		axis = new Vector3(1, -1, -1);
		sceneScaleAxis = new Vector3(sceneScale * axis.x, sceneScale * axis.y, sceneScale * axis.z);
		invSceneScaleAxis = new Vector3(1.0f / sceneScaleAxis.x, 1.0f / sceneScaleAxis.y, 1.0f / sceneScaleAxis.z);
		style.init(this);
		pmouseX = 0;
		pmouseY = 0;
		oneKey = NONE;
		oneKeyCode = NONE;
		workDepth = 0.0f;
		LoadPrimitives();
		workPrimitiveKey = PrimitiveGroupSetup;
		InitMatrix();
		startTicks = DateTime.Now.Ticks;
	}
	
	void PreDraw() {
		if(isNoLoop) { isLoop = false; }
		workPrimitiveKey = 1;
		workPrimitiveGroupIndex = 0;
		pmouseX = mouseX;
		pmouseY = mouseY;
		workDepth = 0.0f;
		system.camera = system.mainCamera;
		system.cameraNum = 0;
		//style.init(this);
		InitMatrix();
		layer(gameObject.layer);
		layerMask(1 << gameObject.layer);
	}
	
	void InitMatrix() {
		system.work.localPosition = Vector3.zero;
		system.work.localRotation = Quaternion.identity;
		system.work.localScale = Vector3.one;
	}

	void Start() {
		isSetup = true;
		background(128);
		setup();
		isSetup = false;
		PreDraw();
	}

	void Update() {
		UpdateOneKey();

		if(isLoop) {
			if(system.camera && !isClearBG && frameCount>2) {
				system.camera.clearFlags = CameraClearFlags.Depth;
			}
			isClearBG = false;
			clear();
			draw();
		}

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
	
	void LateUpdate() {
		if(isLoop) {
			PreDraw();
		}
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
		if(isRecycle && !isKeep) {
			++workPrimitiveKey;
			primitives.TryGetValue(workPrimitiveKey, out obj);
		}
		PGameObject pg = obj ? obj.GetComponent<PGameObject>() : null;
		return pg;
	}

	private PGameObject AddPrimitive(GameObject obj) {
		if(!obj) return null;
		uint key = (isRecycle && !isKeep) ? workPrimitiveKey : 0;
		PGameObject pg = AttachPGameObject(obj, key);
		if(key > 0) {
			primitives.Add(key, obj);
		} else if(!isKeep) {
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
			if(system.camera.cullingMask == style.layerMask) {
				int newLayer = style.layer + 1; // temp
				layer(newLayer);
				layerMask(1 << newLayer);
			}
			system.camera = camera;
		}
		system.cameraNum++;
		system.camera.cullingMask = style.layerMask;
		return system.camera;
	}

	private void SetProperty(PGameObject obj, Vector3 pos, Vector3 scale) {
		if(obj) {
			var trans = obj.transform;
			var parent = (isRecycle || isKeep || system.parent.transform!=gameObject.transform) ? system.parent : system.temp;
			if(system.drawParentObj) { parent = system.drawParentObj.transform; }
			if(parent && (trans.parent==null || !trans.parent.Equals(parent.transform))) trans.parent = parent.transform;

			Vector3 newPos = system.work.localPosition + toScene(pos);
			workDepth -= sceneScale * depthStep;
			Vector3 depth = Vector3.Scale(system.camera.transform.forward, new Vector3(workDepth, workDepth, workDepth));
			newPos += depth;
			obj.transform.localPosition = newPos;

			trans.localRotation = system.work.localRotation;
			trans.localScale = Vector3.Scale(system.work.localScale, toScene(scale));

			if(obj.renderer && obj.renderer.material) {
				Color col;
				if(obj.isImage) col = style.tintColor;
				else if(obj.isLine) col = style.strokeColor;
				else col = style.fillColor;
				obj.renderer.material.color = col;
				if(obj.renderer) { obj.renderer.enabled = style.isFill; }
				PWireframe pw = obj.wireframe;
				if(pw) {
					pw.isStroke = style.isStroke;
					pw.strokeColor = style.strokeColor;
					pw.strokeWeight = style.strokeWeight * sceneScale;
				}
			}

			obj.gameObject.layer = style.layer;
		}
	}
	
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

	private Rect GetAspectRect(float x, float y, float w, float h) {
		float aspect = w / h;
		x /= w;
		y /= h;
		if(screenMode==U2D || screenMode==U3D) {
			w = 1.0f;
			h = 1.0f;
		} else if(w >= h) {
			w = 1.0f;
			h = 1.0f / aspect;
			y += 1.0f - h;
		} else {
			w = aspect;
			h = 1.0f;
		}
		return new Rect(x, y, w, h);
	}
	#endregion

	#region Processing Events
	protected virtual void setup() {}
	protected virtual void draw() {}
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

	public int displayWidth { get { return Screen.width; } }
	public int displayHeight { get { return Screen.height; } }
	public float displayAspectW { get { return (float)Screen.width / Screen.height; } }
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

	private float _width;
	private float _height;
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

	public int mouseX { get { return (int)pixelToSceneX(Input.mousePosition.x); } }
	public int mouseY { get { return (int)pixelToSceneY(Input.mousePosition.y); } }
	public int pmouseX { get; private set; } 
	public int pmouseY { get; private set; } 

	public bool keyPressed { get { return Input.anyKey; } }
	public int key { get { return oneKey; } }
	public int keyCode { get { return oneKeyCode; } }

	public Color color(int gray) {
		return new Color(gray * inv255, gray * inv255, gray * inv255);
	}
	
	public Color color(int r, int g, int b, int a = 255) {
		return new Color(r * inv255, g * inv255, b * inv255, a * inv255);
	}
	
	public Camera size(float w, float h, ScreenMode mode = P2D, float scale = 0.0f) {
		screenMode = mode;
		if(scale > 0.0f) { sceneScale = scale; }

		if(isAutoFit) { fitScale = (w >= h) ? (float)displayHeight / h : (float)displayWidth / w; }
		else { fitScale = 1.0f; }
		invFitScale = 1.0f / fitScale;

		_width = w * invFitScale;
		_height = h * invFitScale;

		if(mode==P2D || mode==P3D) {
			axis.Set(1, -1, -1);
		} else {
			axis.Set(1, 1, 1);
		}
		sceneScaleAxis.Set(sceneScale * axis.x, sceneScale * axis.y, sceneScale * axis.z);

		system.cameraNum = 0;
		bool is2D = (mode==P2D || mode==U2D);
		if(is2D) { ortho(); }
		else { perspective(); }

		if(isSetDefaultLight && !is2D) {
			beginKeep();
			lights();
			endKeep();
		} else noLights();

		system.camera.cullingMask = style.layerMask;

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
		style.backgroundColor = Color.black;
		isClearBG = true;
		if(system.camera) {
			system.camera.backgroundColor = style.backgroundColor;
			system.camera.clearFlags = CameraClearFlags.Depth;
			beginNoRecycle();
			pushStyle();
				noStroke();
				tint(255);
				float offsetX = (displayWidth - width * displayScale) * 0.5f * invFitScale;
				imageMode(CORNER);
				image(img, -offsetX, 0, width + offsetX * 2.0f, displayHeight * invFitScale);
			popStyle();
			endRecycle();
		}
	}

	public void background(int r, int g, int b, int a=255) {
		clear();
		style.backgroundColor = color(r, g, b, a);
		isClearBG = true;
		if(system.camera) {
			system.camera.backgroundColor = style.backgroundColor;
			if(a >= 255) {
				system.camera.clearFlags = CameraClearFlags.SolidColor;
			} else {
				system.camera.clearFlags = CameraClearFlags.Depth;
				if(a > 0) {
					beginNoRecycle();
					pushStyle();
						noStroke();
						fill(style.backgroundColor);
						rectMode(CORNER);
						rect(screenOffsetX, screenOffsetY, screenWidth, screenHeight);
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
			system.camera.backgroundColor = style.backgroundColor;
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
			system.camera.rect = GetAspectRect(x, y, abs(right-left), abs(bottom-top));
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
			float h = height;
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
		PStyle ps = style;
		styleStack.Push(ps);
	}

	public void popStyle() {
		style = styleStack.Pop();
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

	public PGameObject pushMatrix(string name) {
		PGameObject child = GetPrimitive();
		if(!child) {
			child = AddPrimitive(new GameObject(name));
			child.transform.parent = system.parent.transform;
		}
		if(child) {
			system.parent = child.transform;
			system.parent.transform.localPosition = system.work.localPosition;
			system.work.localPosition = new Vector3();
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
		return system.parent.gameObject;
	}

	public PMatrix getMatrix() { return new PMatrix(system.work.localToWorldMatrix); }

	public void beginDraw(PGameObject obj=null) {
		drawParentObjStack.Push(system.drawParentObj);
		system.drawParentObj = obj;
	}
	public void endDraw() {
		system.drawParentObj = drawParentObjStack.Pop();
	}

	public void translate(float x, float y, float z=0.0f) {
		system.work.Translate(toScene(x, y, z));
	}
	
	public void rotate(float angle) {
		rotateZ(degrees(angle));
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
		style.isFill =  true;
		style.fillColor = color(r, g, b, a);
	}
	
	public void noFill() {
		style.isFill =  false;
	}

	public void tint(int gray, int alpha = 255) {
		tint(gray, gray, gray, alpha);
	}
	
	public void tint(int r, int g, int b, int a = 255) {
		style.tintColor = color(r, g, b, a);
	}

	public void noTint() {
		style.tintColor = Color.white;
	}

	public void stroke(int gray, int alpha = 255) {
		stroke(gray, gray, gray, alpha);
	}

	public void stroke(int r, int g, int b, int a = 255) {
		style.isStroke = true;
		style.strokeColor = color(r, g, b, a);
	}

	public void noStroke() {
		style.isStroke = false;
	}

	public void strokeWeight(float weight) {
		style.strokeWeight = weight;
	}

	public PGameObject point(float x, float y, float z = 0.0f) {
		//float s = 1.0f * fitScale;
		float s = 1.0f;
		float sw = style.strokeWeight;
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
		l.SetWidth(style.strokeWeight * sceneScale, style.strokeWeight * sceneScale);
		l.SetVertexCount(2);
		l.SetPosition(0, toScene(x1, y1, z1));
		l.SetPosition(1, toScene(x2, y2, z2));
		float invScene = 1.0f / sceneScale;
		SetProperty(obj, Vector3.zero, new Vector3(axis.x * invScene, axis.y * invScene, axis.z * invScene));
		return obj;
	}

	public void rectMode(int mode) { // CORNER or CENTER
		assert(mode==CORNER || mode==CENTER);
		style.rectMode = mode;
	}

	public PGameObject rect(float x, float y, float w, float h) {
		PGameObject obj = GetPrimitive();
		if(!obj) { obj = AddPrimitive(Instantiate(basicPrefabs.rect) as GameObject); }
		Vector3 p;
		if(style.rectMode==CORNER) p = new Vector3(x + (w * 0.5f) * axis.x, y + (h * 0.5f) * -axis.y, 0.0f);
		else p = new Vector3(x, y, 0.0f);
		SetProperty(obj, p, new Vector3(w, h, sceneScaleAxis.z)); // rect prefab side
		return obj;
	}
	
	public void ellipseMode(int mode) { // CORNER or CENTER
		assert(mode==CORNER || mode==CENTER);
		style.ellipseMode = mode;
	}

	public PGameObject ellipse(float x, float y, float w, float h) {
		PGameObject obj = GetPrimitive();
		if(!obj) { obj = AddPrimitive(Instantiate(basicPrefabs.ellipse) as GameObject); }
		Vector3 p;
		if(style.ellipseMode==CORNER) p = new Vector3(x + (w * 0.5f) * axis.x, y + (h * 0.5f) * axis.y, 0.0f);
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

	public void textSize(float size) {
		style.textSize = size;
	}

	public void textAlign(int alignX, int alignY = BASELINE) {
		assert(alignX==LEFT || alignX==CENTER || alignX==RIGHT);
		assert(alignY==TOP || alignY==CENTER || alignY==BOTTOM || alignY==BASELINE);
		style.textAlignX = alignX; // LEFT, CENTER, RIGHT
		style.textAlignY = alignY; // TOP, CENTER, BTOTTOM, BASELINE
	}

	public PGameObject text(string str, float x, float y) {
		PGameObject obj = GetPrimitive();
		assert(!obj || obj.isText, "recycle error");
		if(!obj) { obj = AddPrimitive(Instantiate(basicPrefabs.text) as GameObject); }
		TextMesh tm = obj.GetComponent<TextMesh>();
		if(tm) {
			tm.color = style.fillColor;
			tm.characterSize = 10 * invFitScale;
			tm.fontSize = (int)(style.textSize * fitScale);
			tm.text = str;
			tm.anchor = TextAnchor.MiddleCenter;
			if(style.textAlignX==LEFT) {
				switch(style.textAlignY) {
					case TOP: tm.anchor = TextAnchor.UpperLeft; break;
					case CENTER: tm.anchor = TextAnchor.MiddleLeft; break;
					case BASELINE: tm.anchor = TextAnchor.MiddleLeft; break;
					case BOTTOM: tm.anchor = TextAnchor.LowerLeft; break;
				}
			} else if(style.textAlignX==RIGHT) {
				switch(style.textAlignY) {
					case TOP: tm.anchor = TextAnchor.UpperRight; break;
					case CENTER: tm.anchor = TextAnchor.MiddleRight; break;
					case BASELINE: tm.anchor = TextAnchor.MiddleRight; break;
					case BOTTOM: tm.anchor = TextAnchor.LowerRight; break;
				}
			} else {
				switch(style.textAlignY) {
					case TOP: tm.anchor = TextAnchor.UpperCenter; break;
					case CENTER: tm.anchor = TextAnchor.MiddleCenter; break;
					case BASELINE: tm.anchor = TextAnchor.MiddleCenter; break;
					case BOTTOM: tm.anchor = TextAnchor.LowerCenter; break;
				}
			}
			if(style.textAlignY==BASELINE) {
				y -= style.textSize * 0.3f;
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
		style.imageMode = mode;
	}

	public PGameObject image(PImage img, float x, float y) {
		return image(img, x, y, img.width, img.height);
	}

	public PGameObject image(PImage img, float x, float y, float w, float h) {
		int mode = style.rectMode;
		rectMode(style.imageMode);
		PGameObject obj = rect(x, y, w, h);
		style.rectMode = mode;
		PImage objImg = obj.gameObject.GetComponent<PImage>();
		if(!objImg) {
			obj.name = "Image(Clone)";
			objImg = obj.gameObject.AddComponent<PImage>();
			objImg.graphics = this;
			if(obj.wireframe) Destroy(obj.gameObject.GetComponent<PWireframe>());
		}
		objImg.set(img);
		return obj;
	}

	public void noLights() {
		RenderSettings.ambientLight = Color.white;
		Light[] lights = GetComponentsInChildren<Light>();
		foreach(Light light in lights) {
			if(light.gameObject.layer==style.layer) {
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
		light.cullingMask = style.layerMask;
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
	#endregion

	#region Processing Extra Members
	public float INFINITY { get { return Mathf.Infinity; } }
	public float deltaTime { get { return Time.deltaTime; } }
	public Vector2 axis2D { get { return new Vector2(axis.x, axis.y); } }
	public Vector3 axis3D { get { return axis; } }
	public float sceneDepthStep { get { return depthStep * sceneScaleAxis.z; } }

	public float screenOffsetX { get { return pixelToSceneX(0); } }
	public float screenOffsetY { get { return 0.0f; } }
	public float screenWidth { get { return displayWidth * invFitScale; } }
	public float screenHeight { get { return displayHeight * invFitScale; } }
	public float displayScale { get { return displayHeight / height; } }

	public Vector3 pixelToWorld(float x, float y) {
		return system.camera.ScreenToWorldPoint(new Vector3(x, displayHeight - y, system.camera.nearClipPlane));
	}

	public float pixelN(float n) { return n * invFitScale; }
	public float pixelX(float x) { return pixelToSceneX(x); }
	public float pixelY(float y) { return pixelToSceneY(y); }

	public float pixelToSceneX(float x) {
		if(axis.x <= 0.0f) { x = displayWidth - x; }
		x -= (displayWidth - width * displayScale) * 0.5f;
		if(screenMode==U2D || screenMode==U3D) { x -= displayWidth * 0.5f; }
		return x * invFitScale;
	}
	
	public float pixelToSceneY(float y) {
		if(axis.y <= 0.0f) { y = displayHeight - y; }
		if(screenMode==U2D || screenMode==U3D) { y = displayHeight * 0.5f - y; }
		return y * invFitScale;
	}
	
	public float sceneToPixelX(float x) {
		x *= fitScale;
		if(screenMode==U2D || screenMode==U3D) { x -= displayWidth * 0.5f; }
		x += (displayWidth - width * displayScale) * 0.5f;
		if(axis.x <= 0.0f) { x = displayWidth - x; }
		return x;
	}
	
	public float sceneToPixelY(float y) {
		y *= fitScale;
		if(screenMode==U2D || screenMode==U3D) { y = displayHeight * 0.5f - y; }
		if(axis.y <= 0.0f) { y = displayHeight - y; }
		return y;
	}

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

	public float inputAxis(string name) { return Input.GetAxis(name); }
	public float inputX { get { return Input.GetAxis("Horizontal"); } }
	public float inputY { get { return Input.GetAxis("Vertical"); } }

	public string B_FIRE1 { get { return "Fire1"; } }
	public string B_FIRE2 { get { return "Fire2"; } }
	public string B_FIRE3 { get { return "Fire3"; } }
	public string B_JUMP { get { return "Jump"; } }
	public bool isButton(string name) { return Input.GetButton(name); }
	public bool isButtonDown(string name) { return Input.GetButtonDown(name); }
	public bool isButtonUp(string name) { return Input.GetButtonDown(name); }

	public void layerAndMask(int layerIndex) { layer(layerIndex); layerMask(1 << layerIndex); }
	public void layer(int layerIndex) { style.layer = layerIndex; }
	public void layer(string layerName) { style.layer = LayerMask.NameToLayer(layerName); }
	public void layerMask(int layerBits) { style.layerMask = layerBits; }
	public void layerMaskEverything() { style.layerMask = -1; }
	public void addLayerMask(int layerIndex) { style.layerMask |= 1 << layerIndex; }
	public void addLayerMask(string layerName) { style.layerMask |= 1 << LayerMask.NameToLayer(layerName); }
	public void removeLayerMask(int layerIndex) { style.layerMask &= ~(1 << layerIndex); }
	public void removeLayerMask(string layerName) { style.layerMask &= ~(1 << LayerMask.NameToLayer(layerName)); }

	public void backgroundSkybox() {
		clear();
		if(system.camera) {
			system.camera.clearFlags = CameraClearFlags.Skybox;
		}
	}

	public void clearAll() {
		clear();
		primitives.Clear();
	}

	public void fill(Color c) { fill((int)(c.r*255), (int)(c.g*255), (int)(c.b*255), (int)(c.a*255)); }
	public void stroke(Color c) { stroke((int)(c.r*255), (int)(c.g*255), (int)(c.b*255), (int)(c.a*255)); }
	public void tint(Color c) { tint((int)(c.r*255), (int)(c.g*255), (int)(c.b*255), (int)(c.a*255)); }

	public void recycle() { isRecycle = true; }
	public void noRecycle() { isRecycle = false; }
	public void beginRecycle(short id) {
		if(id==0) {
			++workPrimitiveGroupIndex;
			if(workPrimitiveGroupIndex>=short.MaxValue) { Debug.LogError("over recycle groups", this); }
			id = (short)workPrimitiveGroupIndex; 
		}
		recycleStack.Push(isRecycle);
		keyStack.Push(workPrimitiveKey);
		workPrimitiveKey = (uint)id * PrimitiveGroupMult;
		recycle();
	}
	public void beginNoRecycle() {
		recycleStack.Push(isRecycle);
		keyStack.Push(workPrimitiveKey);
		noRecycle();
	}
	public void endRecycle() {
		workPrimitiveKey = keyStack.Pop();
		isRecycle = recycleStack.Pop();
	}
	
	public void keep() { isKeep = true; }
	public void noKeep() { isKeep = false; }
	public void beginKeep() {
		keepStack.Push(isKeep);
		keep();
	}
	public void endKeep() {
		isKeep = keepStack.Pop();
		noKeep();
	}

	public PGameObject prefab(string path, float x=0.0f, float y=0.0f, float z=0.0f, float sx=1.0f, float sy=1.0f, float sz=1.0f) {
		PGameObject obj = GetPrimitive();
		if(!obj) {
			GameObject prefab = Resources.Load(path) as GameObject;
			if(!prefab) return null;
			obj = AddPrimitive(Instantiate(prefab) as GameObject);
		}
		SetProperty(obj, new Vector3(x, y, z), new Vector3(sx, sy, sz));
		return obj;
	}

	public PGameObject pobject(string name, float x=0.0f, float y=0.0f, float z=0.0f, float sx=1.0f, float sy=1.0f, float sz=1.0f) {
		PGameObject obj = GetPrimitive();
		if(!obj) { obj = AddPrimitive(new GameObject(name)); }
		SetProperty(obj, new Vector3(x, y, z), Vector3.Scale(new Vector3(sx, sy, sz), invSceneScaleAxis));
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
		SetProperty(pg, new Vector3(x, y, z), Vector3.Scale(new Vector3(sx, sy, sz), invSceneScaleAxis));
		endKeep();
		return pg;
	}

	public void destroyObject(PGameObject obj) {
		if(!obj) return;
		if(obj.primitiveKey!=0 && primitives.ContainsKey(obj.primitiveKey)) {
			primitives.Remove(obj.primitiveKey);
		}
		Destroy(obj);
	}

	public RaycastHit raycast(Vector3 origin, Vector3 direction, float distance=Mathf.Infinity) {
		RaycastHit hitInfo = new RaycastHit();
		Physics.Raycast(origin, direction, out hitInfo, distance);
		return hitInfo;
	}

	public RaycastHit2D raycast(Vector2 origin, Vector2 direction, float distance=Mathf.Infinity) {
		return Physics2D.Raycast(origin, direction, distance);
	}

	public RaycastHit raycastScreen() {
		return raycastScreen(mouseX, mouseY);
	}

	public RaycastHit raycastScreen(float x, float y, float distance=Mathf.Infinity) {
		Ray ray = system.camera.ScreenPointToRay(new Vector3(sceneToPixelX(x), sceneToPixelY(y), Input.mousePosition.z));
		RaycastHit hitInfo;
		Physics.Raycast(ray, out hitInfo, distance);
		return hitInfo;
	}

	public void loadScene(string name) { Application.LoadLevel(name); }
	public void addScene(string name) { Application.LoadLevelAdditive(name); }
	#endregion
}