using UnityEngine;
using System.Collections;
using uP5;

public class HelloWorld : uProcessing {

	protected override void setup () {
		size(512 * displayAspectW, 512);
		background(0, 128, 0);
		textSize(70);
		text("Hello World!", 50, height/2 - 20);
		textSize(24);
		text("と、UnityでProcessingっぽく書ける", 50, height/2 + 30);
		textSize(18);
		text("ESCキー、qキーでMenuへ", 50, height - 30);
		noLoop();
	}

	protected override void draw () {
	}

	protected override void onKeyTyped() {
        if(key == ESC || key == 'q') { loadScene("Menu"); }
    }
}
