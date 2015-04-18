using UnityEngine;
using System.Collections;
using System.IO;

public class PImage : MonoBehaviour {

	public System.Object customData; // UserCustomData

	internal PGraphics graphics;
	public Texture2D texture;
	public int width = 0, height = 0;
	public int abc = 0;

	#region Processing Members
	public Color get(int x, int y) {
		if(!texture) return Color.black;
		return texture.GetPixel(x, y);
	}

	public void set(int x, int y, Color c) {
		if(!texture) return;
		texture.SetPixel(x, y, c);
	}

	public int texWidth { get { return texture.width; } }
	public int texHeight { get { return texture.height; } }
	#endregion

	#region Processing Extra Members
	public void load(string path) {
		if(path.StartsWith("http://") || path.StartsWith("file://")) {
			StartCoroutine("loadFromURL", path);
			graphics.debuglog("loadImage:loadFromURL " + path);
		} else {
			if(!loadFromResources(path)) {
				loadFromLocalFile(path);
			}
		}
	}

	public void set(PImage img) {
		set(img.texture);
		width = img.width;
		height = img.height;
	}
	
	public void set(Texture2D tex) {
		if(texture!=tex) {
			texture = tex;
			if(gameObject && gameObject.renderer && gameObject.renderer.material) {
				//if(graphics) { gameObject.renderer.material.mainTextureScale = graphics.axis2D; }
				gameObject.renderer.material.mainTexture = tex;
			}
		}
	}
	#endregion

	bool loadFromResources(string path) {
		string resPath = graphics.getResourceName(path);
		Texture2D tex = Resources.Load(resPath) as Texture2D;
		set(tex);
		if(tex!=null) {
			graphics.debuglog("loadImage:loadFromResources " + resPath);
			if(width==0) width = texWidth;
			if(height==0) height = texHeight;
		}
		else { graphics.debuglogWaring("loadImage:loadFromResources <Failed> " + resPath); }
		return tex!=null;
	}

	IEnumerator loadFromURL(string url) {
		WWW web = new WWW(url);
		yield return web;
		set(web.texture);
		if(width==0) width = texWidth;
		if(height==0) height = texHeight;
	}

	bool loadFromLocalFile(string path) {
		if(!File.Exists(path)) {
			graphics.debuglogWaring("loadImage:loadFromLocalFile <NotFound> " + path);;
			return false;
		}
		byte[] bytes = null;

		FileStream fs = new FileStream(path, FileMode.Open);
		using(BinaryReader bin = new BinaryReader(fs)) {
			bytes = bin.ReadBytes((int)bin.BaseStream.Length);
		}

		Texture2D tex = new Texture2D(0, 0);
		tex.LoadImage(bytes);
		set(tex);

		if(tex!=null) {
			graphics.debuglog("loadImage:loadFromLocalFile " + path);
			if(width==0) width = texWidth;
			if(height==0) height = texHeight;
		}
		else { graphics.debuglogWaring("loadImage:loadFromLocalFile <Failed> " + path); }
		return tex!=null;
	}

	public PGameObject pgameObject { get { return GetComponent<PGameObject>(); } }
}
