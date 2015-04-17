using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using PVector = UnityEngine.Vector3;

// uProcessing Main System Class
public class uProcessing : PGraphics {

	#region Settings
	[SerializeField] private bool isEnableSound = true;
	[SerializeField] private bool isClearSound = true;
	#endregion
	
	#region Variables
	private struct PUIStyle {
		public struct Property {
			public Color textColor;
			public Color bgColor;
			public Color frameColor;
			public int frameWeight;
			public void set(Color textColor, Color bgColor, Color frameColor, int frameWeight) {
				this.textColor = textColor;
				this.bgColor = bgColor;
				this.frameColor = frameColor;
				this.frameWeight = frameWeight;
			}
		}
		public Property normal;
		public Property active;
		public int textAlignX;
		public int textAlignY;
		public float textX, textY;
		public string groupName;
		
		public void init(PGraphics graphics) {
			normal.set(Color.white, Color.black, Color.white, 2);
			active.set(Color.black, Color.white, Color.white, 4);
			textAlignX = CENTER;
			textAlignY = CENTER;
			textX = textY = 0.0f;
			groupName = "none";
		}
	}

	private PUIStyle uiStyle;
	private Stack<PUIStyle> uiStyleStack = new Stack<PUIStyle>();
	private bool uiClickUp = true;

	private PGameObject pg2D;
	#endregion

	#region System
	protected override void PreSetup() {
		if(isEnableSound) { PSound.setup(); }
		uiStyle.init(this);
	}
	
	protected override void UpdateOneLoop() {
		if(isEnableSound) { PSound.update(); }
		PTweener.update();
		base.UpdateOneLoop();
	}
	
	protected override void PreDestory() {
		PTweener.clear();
		if(isClearSound) {
			PSound.destroy();
		}
	}
	
	private int autoTextSize(float w, float h, int textSize = 0) {
		if(textSize <= 0) {
			textSize = (int)max(1, h * 0.8f);
			if(name!=null && name.Length > 0) {
				float s = 0.6f;
				float tw = name.Length * textSize * s;
				if(tw > w) { textSize = (int)min(textSize, w / name.Length / s); }
			}
		}
		return textSize;
	}
	
	private void uiText(string name, float x, float y, float w, float h, int textSize) {
		textSize = autoTextSize(w, h, textSize);
		this.textSize(textSize);
		
		textAlign(uiStyle.textAlignX, uiStyle.textAlignY);

		switch(uiStyle.textAlignX) {
		case LEFT: x += textSize * 0.5f; break;
		case CENTER: x += w * 0.5f; break;
		case RIGHT: x += w - textSize * 0.5f; break;
		}

		switch(uiStyle.textAlignY) {
		case TOP: y += h * 0.1f; break;
		case CENTER: y += h * 0.5f; break;
		case BASELINE: y += h * 0.5f + textSize * 0.3f; break;
		case BOTTOM: y += h - h * 0.1f; break;
		}
		
		text(name, x + textSize * uiStyle.textX, y + textSize * uiStyle.textY);
	}
	#endregion

	#region Processing System Members

	#endregion

	#region Processing Extra Members
	// Tweener
	public static PTween tween<T>(System.Object obj, string name, T from, T to, float duration, PTweenEaseFunc easeFunc, PTweenOnComplete onComplete=null, params object[] props) {
		return PTweener.tween(obj, name, from, to, duration, easeFunc, onComplete, props);
	}
	public static PTween tween<T>(System.Object obj, string name, T from, T to, float duration = 1.0f) {
		return PTweener.tween(obj, name, from, to, duration, PEase.Linear, null);
	}
	public static PTween tween<T>(T from, T to, float duration, PTweenEaseFunc easeFunc, PTweenOnComplete onComplete=null, params object[] props) {
		return PTweener.tween(null, null, from, to, duration, easeFunc, onComplete, props);
	}
	public static PTween tween<T>(T from, T to, float duration = 1.0f) {
		return PTweener.tween(null, null, from, to, duration, PEase.Linear, null);
	}
	public static void clearTweens() { PTweener.clear(); }
	public static void removeTween(PTween t) { PTweener.remove(t); }
	public static void removeOneTween(PTween t) { PTweener.removeOne(t); }

	// Sound
	public static void reserveSE(string resourceName, string name = null) { PSound.reserveSE(resourceName, name); }
	public static void reserveBGM(string resourceName, string name = null) { PSound.reserveBGM(resourceName, name); }
	public static void playSE(string name) { PSound.playSE(name); }
	public static void playBGM(string name, bool isLoop = true) { PSound.playBGM(name); PSound.setLoopBGM(isLoop); }
	public static void pauseBGM() { PSound.pauseBGM(); }
	public static bool isPauseBGM() { return PSound.isPauseBGM(); }
	public static void resumeBGM() { PSound.playBGM(); }
	public static void stopBGM() { PSound.stopBGM(); }
	public static void clearSE() { PSound.clearSE(); }
	public static void clearBGM() { PSound.clearBGM(); }

	// UI
	public void layer2D(string layerName = "UI") {
		// 3D camera
		if(system.cameraNum <= 0 && is3D) { perspective (); }

		// 2D描画設定
		if(layerName!=null) { layerAndMask(layerName); }
		layout2D();
		ortho();
		noLights();
	}

	protected virtual void buttonClick(string groupName, string name, PGameObject obj) {}

	public void pushUIStyle() {
		PUIStyle ps = uiStyle;
		uiStyleStack.Push(ps);
	}
	
	public void popUIStyle() {
		uiStyle = uiStyleStack.Pop();
	}
	public void uiGroup(string groupName) {
		uiStyle.groupName = groupName;
	}
	public void uiColor(Color normalBgColor, Color activelBgColor) { 
		uiStyleNormal(normalBgColor, Color.white, Color.white);
		uiStyleActive(activelBgColor, Color.white, Color.white);
	}
	public void uiStyleNormal(Color bgColor) { uiStyleNormal(bgColor, Color.white, Color.white); }
	public void uiStyleNormal(Color bgColor, Color textColor, Color frameColor, int frameWeight = 2) {
		uiStyle.normal.set(textColor, bgColor, frameColor, frameWeight);
	}
	public void uiStyleActive(Color bgColor) { uiStyleActive(bgColor, Color.white, Color.white); }
	public void uiStyleActive(Color bgColor, Color textColor, Color frameColor, int frameWeight = 5) {
		uiStyle.active.set(textColor, bgColor, frameColor, frameWeight);
	}
	public void uiTextAlign(int textAlignX, int textAlignY = BASELINE) {
		uiStyle.textAlignX = textAlignX;
		uiStyle.textAlignY = textAlignY;
	}
	public void uiTextOffset(float x, float y) {
		uiStyle.textX = x;
		uiStyle.textY = y;
	}

	public void uiClickModeUp() { uiClickUp = true; }
	public void uiClickModeDown() { uiClickUp = false; }

	public bool button(string name, float x, float y, float w, float h, int textSize = 0) {

		pushStyle();
		string objName = "button: " + name;
		var obj = pushMatrix(objName);

		PMatrix matrix = getMatrix();
		matrix.invert();
		Vector3 mv = matrix.mult(new Vector3(mouseX, mouseY));

		//bool isActive = (mouseX > x  && mouseX < x + w && mouseY > y && mouseY < y + h);
		bool isActive = (mv.x > x  && mv.x < x + w && mv.y > y && mv.y < y + h);
		PUIStyle.Property prop = isActive ? uiStyle.active : uiStyle.normal;

		if(prop.frameWeight > 0) {
			stroke(prop.frameColor);
			strokeWeight(prop.frameWeight);
		} else { noStroke(); }

		fill(prop.bgColor);
		float fh = prop.frameWeight * 0.2f;
		rect(x + fh, y + fh, w - fh * 2, h - fh * 2);

		fill(prop.textColor);
		uiText(name, x, y, w, h, textSize);

		popMatrix(objName);
		popStyle();
		bool isClick = uiClickUp ? mouseReleased : mousePressed;
		if(isActive && isClick) {
			buttonClick(uiStyle.groupName, name, obj);
		}
		return isActive && isClick;
	}

	public void label(string name, float x, float y, float w, float h, int textSize = 0) {
		pushStyle();
		string objName = "label: " + name;
		pushMatrix(objName);

		fill(uiStyle.normal.textColor);
		uiText(name, x, y, w, h, textSize);
		
		popMatrix(objName);
		popStyle();
	}

	public enum UIResponse {
		NONE = 0,
		CANCEL = -1,
		OK = 1,
	}

	public UIResponse dialog(string message, string okButtonName, int textSize = 0, float x = -1.0f, float y = -1.0f, float w = 0.0f, float h = 0.0f) {
		return dialog(message, okButtonName, null, textSize, x, y, w, h);
	}

	public UIResponse dialog(string message, string okButtonName, string cancelButtonName, int textSize = 0, float x = -1.0f, float y = -1.0f, float w = 0.0f, float h = 0.0f) {
		pushStyle();
		pushMatrix("dialog");

		bool isAutoResize = (h <= 0.0f);
		if(isAutoResize) {
			if(w <= 0.0f) { w = width * 0.85f; }
			h = w / 8;
		}
		textSize = autoTextSize(w, h, textSize);
		float messageH = textSize;
		float buttonH = textSize * 2;
		float margin = textSize;
		h = messageH + buttonH + margin * 3;
		if(isAutoResize) {
			if(y < 0.0f) { y = (height - h) * 0.5f; }
		}
		if(x < 0.0f) { x = (width - w) * 0.5f; }

		// BG
		if(uiStyle.normal.frameWeight > 0) {
			stroke(uiStyle.normal.frameColor);
			strokeWeight(uiStyle.normal.frameWeight);
		} else { noStroke(); }
		fill(uiStyle.normal.bgColor);
		rect(x, y, w, h);

		// Message
		y += margin;
		label(message, x, y, w, messageH);
		y += messageH;

		// Button
		y += margin;
		float buttonW = w / 2;
		UIResponse ret = UIResponse.NONE;
		if(cancelButtonName!=null) {
			x += margin;
			buttonW -= margin * 1.5f;
			if(button(okButtonName, x, y, buttonW, buttonH)) {
				ret = UIResponse.OK;
			}
			x += buttonW + margin;
			if(button(cancelButtonName, x, y, buttonW, buttonH)) {
				ret = UIResponse.CANCEL;
			}
		} else {
			x += buttonW / 2;
			if(button(okButtonName, x, y, buttonW, buttonH)) {
				ret = UIResponse.OK;
			}
		}

		popMatrix("dialog");
		popStyle();
		return ret;
	}

	public struct ListItem {
		public string name;
		public string info;
		public Object data;

		public ListItem(string name, string info="", Object data=null) {
			this.name = name;
			this.info = info;
			this.data = data;
		}
	}

	public int listView(List<ListItem> list, float x, float y, float w, float h, float itemH, int textSize = 0) {
		pushStyle();
		pushMatrix("list");

		int selectedItem = -1;
		int i = 0;
		foreach(ListItem item in list) {
			if(button(item.name, x, y, w, itemH, textSize)) {
				selectedItem = i;
			}
			y += itemH - uiStyle.normal.frameWeight;
			if(y >= h) break;
			i++;
		}

		popMatrix("list");
		popStyle();
		return selectedItem;
	}
	#endregion
}
