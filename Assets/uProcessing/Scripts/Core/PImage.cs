using UnityEngine;
using System.Collections;
using System.IO;

public class PImage : MonoBehaviour {

	internal PGraphics graphics;
	public Texture2D texture;

	#region Processing Members
	public Color get(int x, int y) {
		if(!texture) return Color.black;
		return texture.GetPixel(x, y);
	}

	public void set(int x, int y, Color c) {
		if(!texture) return;
		texture.SetPixel(x, y, c);
	}

	public int width { get { return texture.width; } }
	public int height { get { return texture.height; } }
	#endregion

	#region Processing Extra Members
	public void load(string path) {
		if(path.StartsWith("http://")) {
			StartCoroutine("loadFromURL", path);
		} else {
			if(!loadFromResources(path)) {
				loadFromLocalFile(path);
			}
		}
	}

	public void set(PImage img) {
		set(img.texture);
	}
	
	public void set(Texture2D tex) {
		if(texture!=tex) {
			texture = tex;
			if(gameObject && gameObject.renderer && gameObject.renderer.material) {
				if(graphics) { gameObject.renderer.material.mainTextureScale = graphics.axis2D; }
				gameObject.renderer.material.mainTexture = tex;
			}
		}
	}
	#endregion

	bool loadFromResources(string path) {
		Texture2D tex = Resources.Load(path) as Texture2D;
		set(tex);
		return tex!=null;
	}

	IEnumerator loadFromURL(string url) {
		WWW web = new WWW(url);
		yield return web;
		set(web.texture);
	}

	bool loadFromLocalFile(string path) {
		if(!File.Exists(path)) return false;
		FileStream fs = new FileStream(path, FileMode.Open);
		BinaryReader bin = new BinaryReader(fs);
		byte[] bytes = bin.ReadBytes((int)bin.BaseStream.Length);
		bin.Close();

		Texture2D tex = new Texture2D(0, 0);
		tex.LoadImage(bytes);
		set(tex);
		return tex!=null;
	}
}
