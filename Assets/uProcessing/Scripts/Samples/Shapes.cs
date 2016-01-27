using UnityEngine;
using System.Collections;
using uP5;

public class Shapes : uProcessing {

	PShape square;
	PShape circle;
	PShape star;

	protected override void setup () {
		size(512 * displayAspectW, 512, P2D);

		square = createShape(RECT, 0, 0, width / 2 - 25 * 2, height - 25 * 2);
		square.disableStyle();

		circle = createShape(ELLIPSE, 0, 0, 100, 100);
		circle.setFill(color(0, 255, 0));

		star = createShape();
		star.beginShape(TRIANGLE_FAN);
		star.vertex(0, 0);
		star.vertex(0, -50);
		star.vertex(14, -20);
		star.vertex(47, -15);
		star.vertex(23, 7);
		star.vertex(29, 40);
		star.vertex(0, 25);
		star.vertex(-29, 40);
		star.vertex(-23, 7);
		star.vertex(-47, -15);
		star.vertex(-14, -20);
		star.vertex(0, -50);
		star.endShape();

		recycle();
	}

	protected override void draw() {
		background(255);

		layout2D();
		fill(color(0, 0, 255));
		shape(square, 25, 25);

		shape(circle, mouseX, mouseY);
		
		fill(color(255, 0, 0));
		shape(square, 25 + width/2, 25);

		for(int i=0; i<4; i++) {
			star.fill(color(255, 255, 255 - i * 60));
			shape(star, 100 + width / 5 * i, 100 + 100 * i);
		}
	}

	protected override void onKeyTyped() {
        if(key == ESC || key == 'q') { loadScene("ListView"); }
    }
}
