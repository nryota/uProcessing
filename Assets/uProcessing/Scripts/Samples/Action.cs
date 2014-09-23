using UnityEngine;
using System.Collections;
//using PVector = UnityEngine.Vector3;

public class Action : PGraphics {

	[SerializeField] UnityChan unityChan = new UnityChan();
	[SerializeField] int LAYER_2D = 7;
	PImage unityChanLogo;
	Vector3 cameraPos;

	protected override void setup() {
		layerMaskEverything();
		removeLayerMask(LAYER_2D);

		size(500 * displayAspectW, 500, U3D, 1.0f);

		unityChanLogo = loadImage("unitychanLogo");
		unityChan.init(this);
		recycle();
	}

	protected override void draw() {
		layerMaskEverything();
		removeLayerMask(LAYER_2D);
		layout3D();

		backgroundSkybox();
		lights();

		unityChan.update();

		cameraPos = Vector3.Lerp(cameraPos, unityChan.obj.pos, 0.1f);
		cameraPos.y = constrain(cameraPos.y, 0.0f, 10.0f); 
		camera(cameraPos.x + 2.5f, cameraPos.y + 0.5f, cameraPos.z - 3.5f,
		       cameraPos.x + 2.3f, cameraPos.y + 1.5f, cameraPos.z + 0.0f,
		       Vector3.up.x, Vector3.up.y, Vector3.up.z);

		drawBlocks();
		draw2D();
	}

	void drawBlocks() {
		int pi = floor(unityChan.obj.pos.x / 3);
		for(int i=0; i<7; i++) {
			pushMatrix();
			noStroke();
			int bx = i - 3 + pi;
			float level = constrain(bx / 100.0f, 0.0f, 1.0f);
			randomSeed(bx + 777);
			float t = (frameCount + random (100)) * random(0.02f, 0.03f + level * 0.1f);
			float y = (sin(t) + 1.0f) * random(0.5f, 1.5f) + 0.2f;
			if(random(100) < 50 - level * 40) { y -= 10.0f; }
			translate(bx * 3, y, 0); 
			fill((int)(level * 255), 0, 0);
			/*var obj = */box(random(0.2f, 2.5f), 0.2f, 2.0f);
			/*if(!obj.rigidbody) {
				obj.addRigid();
				obj.rigidbody.isKinematic = true;
			}*/
			popMatrix();
		}
	}

	void draw2D() {
		layerAndMask(LAYER_2D);
		layout2D();
		ortho();
		noLights();

		fill(255);
		textSize(14); textAlign(LEFT, BOTTOM);
		text("[LEFT]:Back  [RIGHT]:Run  [UP or SPACE]:Jump  [DOWN or R-CTRL]:Slide", 10, height - 10);
		image(unityChanLogo,
		      width - unityChanLogo.width - 10, height - unityChanLogo.height - 10,
		      unityChanLogo.width, unityChanLogo.height);
	}
	
	protected override void onKeyTyped() {
		if(key == ESC) { loadScene("Menu"); }
	}
}

[System.SerializableAttribute]
class UnityChan {
	public float forwardSpeed = 3.0f;
	public float backwardSpeed = 1.0f;
	//public float rotateSpeed = 5.0f;
	public float jumpPower = 5.0f; 
	public float slidePower = 20.0f; 

	public PGameObject obj;
	PGraphics g;
	Vector3 velocity;
	
	public void init(PGraphics graphics) {
		g = graphics;
		g.beginKeep();
		obj = g.prefab("unitychanP5");
		//obj = g.box (1);
		//obj.addRigid();

		obj.transform.Rotate(0, 90, 0);
		obj.rigidbody.constraints |= RigidbodyConstraints.FreezePositionZ;
		g.endKeep();
	}

	public void update() {
		float v = g.inputX;
		obj.anim.SetFloat("Speed", v);
		Vector3 accel = new Vector3(0, 0, v);
		accel = obj.transform.TransformDirection(accel);
		if(v > 0.1f) {
			accel *= forwardSpeed;
		} else if (v < -0.1f) {
			accel *= backwardSpeed;
		}

		bool isJump = obj.isAnimState("Base Layer.Jump");
		bool isSlide = obj.isAnimState("Base Layer.Slide");

		if(!isJump && !isSlide) {
			obj.anim.SetBool("Jump", false);
			obj.anim.SetBool("Slide", false);
			if(g.isButtonDown("Jump") || g.isKeyDown(KeyCode.UpArrow)) {
				if(!obj.animator.IsInTransition(0) && v >= 0.0f) {
					obj.rigidbody.AddForce(Vector3.up * jumpPower, ForceMode.VelocityChange);
					obj.anim.SetBool("Jump", true);
				}
			} else if(g.isButtonDown("Fire1") || g.isKeyDown(KeyCode.DownArrow)) {
				if(!obj.animator.IsInTransition(0)) {
					accel *= slidePower;
					obj.anim.SetBool("Slide", true);
				}
			}
		}

		velocity = Vector3.Lerp(velocity, accel, 0.1f);
		obj.pos += velocity * Time.deltaTime;
		if(obj.pos.y < 0.0f) obj.pos = new Vector3(obj.pos.x, 0.0f, obj.pos.z);

		//float h = g.inputY;
		//obj.anim.SetFloat("Direction", h);
		//obj.transform.Rotate(0, h * rotateSpeed, 0);	
	}
}

