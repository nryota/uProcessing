using UnityEngine;
using System.Collections;

public class JsonData : uProcessing {
	string dataPath;
	int x = 10;
	int y = 10;

	protected override void setup () {
		size(512 * displayAspectW, 512);
		background(0, 128, 128);
		textAlign(LEFT, TOP);
		textSize(30);

	#if UNITY_EDITOR && !UNITY_WEBPLAYER
		dataPath = Application.dataPath + "/uProcessing/Resources/PSamples/";
		saveJson();
		saveText();
	#else
		dataPath = "PSamples/";
	#endif

		loadJson();
		loadText();

		noLoop();
	}
	
	protected override void draw () {
	}
	
	private void loadJson() {
		//JSONArray jsonArray = loadJSONArray(dataPath + "jsonArray.json");
		//dispJsonArray(JSONArray);
		loadJSONArray(dataPath + "jsonArray.json", dispJsonArray);

		//JSONObject jsonObj = loadJSONObject(dataPath + "jsonObj.json");
		//dispJsonObject(jsonObj);
		loadJSONObject(dataPath + "jsonObj.json", dispJsonObject);
	}

	private void dispJsonArray(JSONArray jsonArray) {
		text("-- Array: jsonArray.json", x, y); y += 40;
		for (int i = 0; i < jsonArray.size(); i++) {
			JSONObject animal = jsonArray.getJSONObject(i); 
			
			int id = animal.getInt("id");
			string species = animal.getString("species");
			string name = animal.getString("name");

			string info = id + ", " + species + ", " + name;
			text(info, x, y); y += 40;
			println(info);
		}
		y += 40;
	}

	private void dispJsonObject(JSONObject jsonObj) {
		text("-- Object: jsonObj.json", x, y); y += 40;

		int id = jsonObj.getInt("id");
		string species = jsonObj.getString("species");
		string name = jsonObj.getString("name");

		string info = id + ", " + species + ", " + name;
		text(info, x, y); y += 40;
		println(info);

		y += 40;
	}
	
	private void saveJson() {
		string[] species = { "Capra hircus", "Panthera pardus", "Equus zebra" };
		string[] names = { "Goat", "Leopard", "Zebra" };
		
		// Array
		JSONArray values;
		values = new JSONArray();
		
		for (int i = 0; i < species.Length; i++) {
			
			JSONObject animal = new JSONObject();
			
			animal.setInt("id", i);
			animal.setString("species", species[i]);
			animal.setString("name", names[i]);
			
			values.setJSONObject(i, animal);
		}
		
		saveJSONArray(values, dataPath + "jsonArray.json");
		
		// Object
		JSONObject json = new JSONObject();
		
		json.setInt("id", 0);
		json.setString("species", "Panthera leo");
		json.setString("name", "Lion");
		
		saveJSONObject(json, dataPath + "jsonObj.json");
	}

	private void loadText() {
		//string[] strings = loadStrings(dataPath + "strings.txt");
		//dispText(strings);
		loadStrings(dataPath + "strings.txt", dispText);
	}

	private void dispText(string[] strings) {
		text("-- Text: strings.txt", x, y); y += 40;
		for (int i = 0; i < strings.Length; i++) {
			text(strings[i], x, y); y += 40;
			println(strings[i]);
		}
	}

	private void saveText() {
		string[] strings = { "Abc 123", "日本語テキスト" };
		saveStrings(dataPath + "strings.txt", strings);
	}

	protected override void onKeyTyped() {
		if(key == ESC) { loadScene("ListView"); }
	}
}
