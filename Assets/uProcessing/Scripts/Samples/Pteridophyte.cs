// Processing code : http://blog.p5info.com/?p=417
using UnityEngine;
using uP5;

public class Pteridophyte : uProcessing {
	float x = 0.0f;
	float y = 0.0f;
	const int RATIO = 59;
    PImage canvas;

	protected override void setup() {
		size(600, 600);
        canvas = createImage(width, height);
    }
	
	protected override void draw() {
        background(30, 60, 120);
        for (int i = 0; i < 100; i++) {
			float tx = 0;
			float ty = 0;

            Color col;
            if (random(10) < 3) col = color(128, 255, 128);
			else col = color(128, 128, 64);

            float px = (x * RATIO) + width * 0.5f - 50;
            float py = y * RATIO;
            canvas.set(px, py, col, false);

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
        canvas.updatePixels();
        image(canvas, 0, 0);
    }

    protected override void onKeyTyped() {
        if(key == ESC || key == 'q') { loadScene("ListView"); }
	}
}
