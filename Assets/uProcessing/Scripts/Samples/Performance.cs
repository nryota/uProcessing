using UnityEngine;
using System.Collections;

public class Performance : uProcessing {
	protected override void setup() {
		size(512 * displayAspectW, 512, P3D);
		recycle();
	}
	
	protected override void draw() {
		background(0);

		//perspective();
		strokeWeight(5);
		stroke(255, 255, 0);
		line(mouseX, 0, mouseX, height);
		stroke(0, 255, 0);
		line(0, mouseY, width, mouseY);

		noStroke();
		fill(255, 128);
		for(int y=0; y<10; y++) {
			for(int x=0; x<(int)(width / 50); x++) {
				pushMatrix();
				translate(x * 50.0f + 30.0f, y * 50.0f + 30.0f, 0);
				rotateZ( radians( millis()/10.0f ) );
				if((frameCount / 60) % 4 < 2) { fill(y*20, x*20, 255, 255 - x*10); }
				box(20 + x*2);
				popMatrix();
			}
		}

		layer2D();
		if(button("Back", 10, 10, 100, 25)) { loadScene("ListView"); }
	}
	
	protected override void onKeyTyped() {
		if(key == ESC) { loadScene("ListView"); }
	}
}

