using UnityEngine;
using System.Collections;

public class Dialog : uProcessing {

	bool isDispDialog = false;

	protected override void setup() {
		size(200 * displayAspectW, 200, P2D);
	}

	protected override void draw() {
		background(128);

		uiColor(Color.black, Color.red);

		layer2D();
		noRecycle();
		if(isDispDialog) {
			switch(dialog("メニューに戻りますか？", "OK", "Cancel")) {
			case UIResponse.OK:
				loadScene("Menu");
				break;
			case UIResponse.CANCEL:
				isDispDialog = false;
				break;
			}
		} else {
			if(button("OpenDialog", width / 4, height / 4, width / 2, 30)) {
				isDispDialog = true;
			}
		}
		recycle();
	}

	protected override void onKeyTyped() {
		if(key == ESC) { loadScene("ListView"); }
	}
}
