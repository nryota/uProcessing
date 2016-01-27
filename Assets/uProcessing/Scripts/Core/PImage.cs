using UnityEngine;
using System.Collections;
using System.IO;

namespace uP5
{
    public class PImage : MonoBehaviour
    {
        public System.Object customData; // UserCustomData

        public Texture2D texture;
        public int width = 0, height = 0;
        public string path;
        public Color32[] pixels;

        #region Processing Members
        public Color get(float x, float y)
        {
            if (!texture) return Color.black;
            int ix = (int)x;
            int iy = (int)y;
            if (pixels != null)
            {
                int index = iy * texWidth + ix;
                if (index >= 0 && index < pixels.Length)
                {
                    return pixels[index];
                }
                else return Color.black;
            }
            else return texture.GetPixel(ix, iy);
        }

        public void set(float x, float y, Color col, bool isApply = true)
        {
            if (!texture) return;
            int ix = (int)x;
            int iy = (int)y;
            if(pixels!=null)
            {
                int index = iy * texWidth + ix;
                if (index >= 0 && index < pixels.Length)
                {
                    pixels[index] = col;
                }
            }
            else
            {
                texture.SetPixel(ix, iy, col);
            }
            if (isApply) { texture.Apply(); }
        }

        public bool loadPixels()
        {
            if (!texture) return false;
            pixels = texture.GetPixels32();
            if (pixels == null)
            {
                PGraphics.debuglogWaring("loadPixels:GetPixels32 <failed>");
                return false;
            }
            else return true;
        }

        public void updatePixels()
        {
            if (!texture) return;
            if (pixels != null) { texture.SetPixels32(pixels); }
            texture.Apply();
        }

        public int texWidth { get { return texture.width; } }
        public int texHeight { get { return texture.height; } }
        #endregion

        #region Processing Extra Members
        public void create(int width, int height, TextureFormat texFormat = TextureFormat.ARGB32)
        {
            this.width = width;
            this.height = height;
            texture = new Texture2D(width, height, texFormat, false);
            if (texture == null)
            {
                PGraphics.debuglogWaring("create: new Texture2D <failed>");
                return;
            }
            clear(new Color(0, 0, 0, 0));
        }

        public void load(PGraphics g, string path)
        {
            this.path = path;
            if (path.StartsWith("http://") || path.StartsWith("file://"))
            {
                StartCoroutine("loadFromURL", path);
                PGraphics.debuglog("loadImage:loadFromURL " + path);
            }
            else
            {
                if (!loadFromResources(g, path))
                {
                    loadFromLocalFile(path);
                }
            }
        }

        public void set(PImage img)
        {
            set(img.texture);
            width = img.width;
            height = img.height;
        }

        public void set(Texture2D tex)
        {
            if (texture != tex)
            {
                texture = tex;
                if (gameObject && gameObject.GetComponent<Renderer>() && gameObject.GetComponent<Renderer>().material)
                {
                    //if(graphics) { gameObject.renderer.material.mainTextureScale = graphics.axis2D; }
                    gameObject.GetComponent<Renderer>().material.mainTexture = tex;
                }
            }
        }

        public void clear(Color col)
        {
            if (!texture) return;
            if (loadPixels())
            {
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = col;
                }
                updatePixels();
            }
        }
        #endregion

        bool loadFromResources(PGraphics g, string path)
        {
            string resPath = g.getResourceName(path);
            Texture2D tex = Resources.Load(resPath) as Texture2D;
            set(tex);
            if (tex != null)
            {
                PGraphics.debuglog("loadImage:loadFromResources " + resPath);
                if (width == 0) width = texWidth;
                if (height == 0) height = texHeight;
            }
            else { PGraphics.debuglogWaring("loadImage:loadFromResources <Failed> " + resPath); }
            return tex != null;
        }

        IEnumerator loadFromURL(string url)
        {
            WWW web = new WWW(url);
            yield return web;
            set(web.texture);
            if (width == 0) width = texWidth;
            if (height == 0) height = texHeight;
        }

        bool loadFromLocalFile(string path)
        {
            if (!File.Exists(path))
            {
                PGraphics.debuglogWaring("loadImage:loadFromLocalFile <NotFound> " + path); ;
                return false;
            }
            byte[] bytes = null;

            FileStream fs = new FileStream(path, FileMode.Open);
            using (BinaryReader bin = new BinaryReader(fs))
            {
                bytes = bin.ReadBytes((int)bin.BaseStream.Length);
            }

            Texture2D tex = new Texture2D(0, 0);
            tex.LoadImage(bytes);
            set(tex);

            if (tex != null)
            {
                PGraphics.debuglog("loadImage:loadFromLocalFile " + path);
                if (width == 0) width = texWidth;
                if (height == 0) height = texHeight;
            }
            else { PGraphics.debuglogWaring("loadImage:loadFromLocalFile <Failed> " + path); }
            return tex != null;
        }

        public PGameObject pgameObject { get { return GetComponent<PGameObject>(); } }
    }
}