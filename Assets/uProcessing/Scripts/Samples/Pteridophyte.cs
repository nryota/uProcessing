// Processing code : http://blog.p5info.com/?p=417
using UnityEngine;
using System.Collections;

public class Pteridophyte : uProcessing {
	float x = 0.0f;
	float y = 0.0f;
	const int RATIO = 59;

	protected override void setup() {
		size(600, 600);
		background(30, 60, 120);
		recycle();
	}
	
	protected override void draw() {
		for(int i = 0; i < 100; i++) {
			float tx = 0;
			float ty = 0;

			if(random(10) < 3) stroke(128, 255, 128);
			else stroke(128, 128, 64);

			point((x * RATIO) + width * 0.5f - 50, height - y * RATIO);
			point(0, 0);
			
			float sw = random(100);
			if (sw > 15) {
				tx = 0.85f * x + 0.04f * y;
				ty = -0.04f * x + 0.85f * y + 1.6f;
			}
			else if (sw > 8) {
				tx = -0.15f * x + 0.28f * y;
				ty = 0.26f * x + 0.24f * y + 0.44f;
			}
			else if (sw > 1) {
				tx = 0.2f * x - 0.26f * y;
				ty = 0.23f * x + 0.22f * y + 1.6f;
			}
			else {    
				tx = 0;
				ty = y * 0.16f;
			}
			
			x = tx;
			y = ty;
		}
	}

	protected override void onKeyTyped() {
        if(key == ESC || key == 'q') { loadScene("ListView"); }
	}
}
