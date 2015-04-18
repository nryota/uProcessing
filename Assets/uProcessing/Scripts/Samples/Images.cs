using UnityEngine;
using System.Collections;

public class Images : uProcessing {
	PImage bg, apple;
	float appleX = 0.0f;
	float appleY = 0.0f;

	protected override void setup() {
		size(512 * displayAspectW, 512);

		bg = loadImage("PSamples/bg", 512, 512);
		apple = loadImage("PSamples/apple", 64, 64);

		recycle();
	}
	
	protected override void draw() {
		background(0);

		imageMode(CENTER);
		image(bg, width/2, height/2);

		appleX = appleX * 0.9f + mouseX * 0.1f;
		appleY += (mouseY - appleY) * 0.1f;
		image(apple, appleX, appleY);
	}

	protected override void onKeyTyped() {
		if(key == ESC) { loadScene("Menu"); }
	}
}
