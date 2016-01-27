using UnityEngine;
using System.Collections;
using uP5;

public class Menu : uProcessing {

	protected override void setup() {
		size(200 * displayAspectW, 200, P2D);
		frameRate(60);
		recycle();
	}
	
	protected override void draw() {
		background(128);
		noStroke();

		float s = 5;
		float x = s;
		float y = s;
		float w = width / 2 - s * 2;
		float h = height / 3 - s * 2;
		int ts = 15;

		if(button("Hello World", color(0, 200, 0), x, y, w, h, ts)) {
			loadScene("HelloWorld");
		}
		
		if(button("Primitives", color(0, 128, 255), x + s + w, y, w, h, ts)) {
			loadScene("Primitives");
		}
		
		y += s + h;
		if(button("Images", color(255, 0, 0), x, y, w, h, ts)) {
			loadScene("Images");
		}

		if(button("Earth", color(255, 128, 0), x + s + w, y, w, h, ts)) {
			loadScene("Earth");
		}

		y += s + h;
		if(button("Action", color(0, 0, 255), x, y, w, h, ts)) {
			loadScene("Action");
		}
		
		if(button("ListView", color(255, 0, 255), x + s + w, y, w, h, ts)) {
			loadScene("ListView");
		}

		textSize(6); textAlign(LEFT, TOP);
		text("uProcessing - dev.eyln.com / [ESC]:Return to Menu / " +
		     "mouse pos (" + mouseX + ", " + mouseY + ")", 10, height - 10);
	}

	public bool button(string name, Color col, float x, float y, float w, float h, int textSize) {
		uiColor(col, col);
		return button(name, x, y, w, h, textSize);
	}
}
