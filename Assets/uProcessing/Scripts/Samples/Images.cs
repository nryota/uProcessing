using UnityEngine;
using System.Collections;

public class Images : PGraphics {
	PImage bg, apple;
	float appleX = 0.0f;
	float appleY = 0.0f;

	protected override void setup() {
		size(512, 512);

		bg = loadImage("PSamples/bg");
		apple = loadImage("PSamples/apple");

		recycle();
	}
	
	protected override void draw() {
		background(bg);

		imageMode(CENTER);

		appleX = appleX * 0.9f + mouseX * 0.1f;
		appleY += (mouseY - appleY) * 0.1f;
		image(apple, appleX, appleY);
	}

	protected override void onKeyTyped() {
		if(key == ESC) { loadScene("Menu"); }
	}
}
