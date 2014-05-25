using UnityEngine;
using System.Collections;

public class HelloWorld : PGraphics {

	protected override void setup () {
		size(512, 512);
		background(0, 128, 0);
		textSize(70);
		text("Hello World!", 50, height/2);
		textSize(24);
		text("と、UnityでProcessingっぽく書ける", 50, height/2 + 50);
		noLoop();
	}

	protected override void draw () {
	}

	protected override void onKeyTyped() {
		if(key == ESC) { loadScene("Menu"); }
	}
}
