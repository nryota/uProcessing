using UnityEngine;
using System.Collections;
using uP5;

public class Earth : uProcessing {
	bool isStroke = false;
	
	protected override void setup() {
		size(512 * displayAspectW, 512, P3D);
		recycle();
	}
	
	protected override void draw() {
		background(0);

		if(isStroke) {
			stroke(0, 255, 255);
			strokeWeight(2);
		} else { noStroke(); }

		pushMatrix();

			ambientLight(40, 40, 40);

			translate(width/2, height/2, 0);
			rotateY( radians( millis()/100.0f ) );

			directionalLight(200, 200, 200, 0, 0, 1.0f);

			fill(0, 100, 240);
			sphere(100);
			
			translate(0, 0, -180);
			fill(128);
			sphere(20);

		popMatrix();
    }

	protected override void onMousePressed() {
		isStroke = !isStroke;
	}

	protected override void onKeyTyped() {
		if(key == ESC || key == 'q') { loadScene("Menu"); }
	}
}
