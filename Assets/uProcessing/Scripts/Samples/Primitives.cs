using UnityEngine;
using System.Collections;

public class Primitives : PGraphics {

	protected override void setup() {
		size(512, 512, P2D);
		
		stroke(0, 128, 64);
		strokeWeight(20);
		fill(0, 255, 128);
		rect(20, 20, 300, 400);
		
		noStroke();
		fill(255);
		ellipse(350, 350, 300, 300);

		noLoop();
	}

	protected override void draw() {
	}

	protected override void onKeyTyped() {
		if(key == ESC) { loadScene("Menu"); }
	}
}
