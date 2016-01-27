using UnityEngine;
using System.Collections;
using uP5;

public class Tweens : uProcessing {

	Color col = Color.gray;
    Color bgCol = Color.black;
    Vector3 pos;
	PTween fadeTween, posTween;
	EaseType easeType = EaseType.OutQuart;

	protected override void setup () {
		size(512 * displayAspectW, 512, P2D);

		fadeTween = tween(255, 0, 1.0f, PEase.InCubic);

        tween(this, "col", Color.gray, Color.green, 0.5f, PEase.OutQuad)
			.to(Color.yellow, 0.5f, PEase.OutQuad)
			.wait(1.0f)
			.to(Color.red, 1.0f, PEase.Linear)
			.reverse()
			.loop();

        wait(3.0f).call(changeBGColor).loop();

        pos = new Vector3(width / 2, height /2, 0.0f);

		recycle();
		noStroke();
	}

	protected override void draw() {
		background(bgCol);

		fill(col);
		ellipse(pos.x, pos.y, 40, 40);

		fill(0, 255, 0);
		textSize(20);
		textAlign(LEFT, TOP);

		int fade = (int)fadeTween.Value;
		text("fade : " + fade, 20, 30);
		text("col  : " + col, 20, 60);

		text("ease : " + easeType, 20, 150);
		text("pos  : Vector3" + pos, 20, 180);

		if(button("Prev Ease", 20, 110, 150, 30)) {
			easeType = (EaseType)((int)easeType - 1);
			if((int)easeType < 0) { easeType = (EaseType)(EaseType.Max - 1); }
		} else if(button("Next Ease", 180, 110, 150, 30)) {
			easeType = (EaseType)( ((int)easeType + 1) % (int)EaseType.Max );
		} else if(mouseReleased) {
			removeTween(posTween);
			posTween = tween(this, "pos", pos, new Vector3(mouseX, mouseY, 0.0f), 0.25f, easeFuncs[(int)easeType]);
		}

		if(fade > 0) {
			beginNoRecycle();
			fill(0, 0, 60, fade);
			rect(0, 0, width, height);
			endRecycle();
		}
	}
	
	protected override void onKeyTyped() {
        if(key == ESC || key == 'q') { loadScene("ListView"); }
    }

    void changeBGColor(PTween tween) {
        bgCol = color(random(100), random(100), random(100));
    }

    #region EaseFunc
    PTweenEaseFunc[] easeFuncs = {
		PEase.Step, PEase.Linear,
		PEase.InQuad, PEase.OutQuad, PEase.InOutQuad,
		PEase.InCubic, PEase.OutCubic, PEase.InOutCubic,
		PEase.InQuart, PEase.OutQuart, PEase.InOutQuart,
		PEase.InQuint, PEase.OutQuint, PEase.InOutQuint,
		PEase.InSine, PEase.OutSine, PEase.InOutSine,
		PEase.InBounce, PEase.OutBounce, PEase.InOutBounce,
	};

	enum EaseType {
		Step, Linear,
		InQuad, OutQuad, InOutQuad,
		InCubic, OutCubic, InOutCubic,
		InQuart, OutQuart, InOutQuart,
		InQuint, OutQuint, InOutQuint,
		InSine, OutSine, InOutSine,
		InBounce, OutBounce, InOutBounce,
		Max,
	}
	#endregion
}
