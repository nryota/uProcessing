using UnityEngine;
using System.Collections;

public class Sounds : uProcessing {

	protected override void setup () {
		size(512 * displayAspectW, 512, P2D);

		reserveBGM("PSamples/bgm", "bgm");
		reserveSE("PSamples/se1", "se A");
	}

	protected override void draw() {
		background(0, 0, 160);

		int x = 20, y = 20;
		int w = (int)width - x * 2;
		int h = 40;
		int step = h * 2;

		if(button("playBGM", x, y, w, h)) {
			playBGM("bgm");
		}

		y += step;
		if(button("pauseBGM / resumeBGM", x, y, w, h)) {
			if(isPauseBGM()) { resumeBGM(); }
			else { pauseBGM(); }
		}

		y += step;
		if(button("stopBGM", x, y, w, h)) {
			stopBGM();
		}
		
		y += step;
		if(button("playSE A", x, y, w, h)) {
			playSE("se A");
		}

		y += step;
		if(button("playSE B", x, y, w, h)) {
			playSE("PSamples/se2");
		}
	}

	protected override void onKeyTyped() {
        if(key == ESC || key == 'q') { loadScene("ListView"); }
    }
}
