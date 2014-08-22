using UnityEngine;
using System.Collections;

public class Menu : PGraphics {

	protected override void setup() {
		size(200, 200, P2D);
		recycle();
	}
	
	protected override void draw() {
		background(128);

		noStroke();
		translate(screenOffsetX, screenOffsetY);

		float s = 5;
		float x = s;
		float y = s;
		float w = screenWidth / 2 - s * 2;
		float h = screenHeight / 3 - s * 2;
		int ts = 15;

		if(button("Hello World", color(0, 255, 0), ts, x, y, w, h)) {
			loadScene("HelloWorld");
		}
		
		if(button("Primitives", color(0, 128, 255), ts, x + s + w, y, w, h)) {
			loadScene("Primitives");
		}
		
		y += s + h;
		if(button("Images", color(255, 0, 0), ts, x, y, w, h)) {
			loadScene("Images");
		}

		/*
		if(button("Pteridophyte", color(255, 128, 0), ts, x + s + w, y, w, h)) {
			loadScene("Pteridophyte");
		}
		*/
		if(button("Performance", color(255, 128, 0), ts, x + s + w, y, w, h)) {
			loadScene("Performance");
		}

		y += s + h;
		if(button("Earth", color(0, 0, 255), ts, x, y, w, h)) {
			loadScene("Earth");
		}
		
		if(button("Action", color(255, 0, 255), ts, x + s + w, y, w, h)) {
			loadScene("Action");
		}

		textSize(6); textAlign(LEFT, TOP);
		text("uProcessing - dev.eyln.com / [ESC]:Return to Menu / " +
		     "mouse pos (" + mouseX + ", " + mouseY + ")", 10, height - 10);
	}

	public bool button(string name, Color col, int textSize, float x, float y, float w, float h) {
		float sx = x + screenOffsetX;
		float sy = y + screenOffsetY;
		bool isOnCursor = (mouseX > sx  && mouseX < sx + w && mouseY > sy && mouseY < sy + h);
		if(isOnCursor) { stroke(255); strokeWeight(5); }
		else { noStroke(); }

		fill(col);
		rect(x, y, w, h);

		fill(255);
		this.textSize(textSize);
		textAlign(CENTER, CENTER);
		text(name, x + w/2, y + h/2);

		return isOnCursor && mousePressed;
	}
}
