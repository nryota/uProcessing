using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class JSONArray {
	internal List<object> list;

	public JSONArray() {
		list = new List<object>(); 
	}

	public JSONArray(List<object> list) {
		this.list = list;
	}

	#region Processing/Java Members
	public string getString(int index, string defaultValue=null) { return getValue(index, defaultValue); }
	public int getInt(int index, int defaultValue=0) { return getValue(index, defaultValue); }
	public float getFloat(int index, float defaultValue=0.0f) { return getValue(index, defaultValue); }
	public bool getBoolean(int index, bool defaultValue=false) { return getValue(index, defaultValue); }

	public JSONArray getJSONArray(int index) {
		List<object> list = getValue<List<object>>(index);
		if(list!=null) {
			return new JSONArray(list);
		}
		return null;
	}

	public JSONObject getJSONObject(int index) {
		Dictionary<string, object> dic = getValue<Dictionary<string, object>>(index);
		if(dic!=null) {
			return new JSONObject(dic);
		}
		return null;
	}

	public string[] getStringArray() { return getValueArray<string>(); }
	public int[] getIntArray() { return getValueArray<int>(); }

	public JSONArray append(string value) { return appendValue(value); }
	public JSONArray append(int value) { return appendValue(value); }
	public JSONArray append(float value) { return appendValue(value); }
	public JSONArray append(bool value) { return appendValue(value); }
	public JSONArray append(JSONArray value) { return appendValue(value.list); }
	public JSONArray append(JSONObject value) { return appendValue(value.data); }

	public JSONArray setString(int index, string value) { return setValue(index, value); }
	public JSONArray setInt(int index, int value) { return setValue(index, value); }
	public JSONArray setFloat(int index, float value) { return setValue(index, value); }
	public JSONArray setBoolean(int index, bool value) { return setValue(index, value); }
	public JSONArray setJSONArray(int index, JSONArray value) { return setValue(index, value.list); }
	public JSONArray setJSONObject(int index, JSONObject value) { return setValue(index, value.data); }
	public int size() { return list.Count; }
	public object remove(int index) {
		object obj = list[index];
		list.RemoveAt(index);
		return obj;
	}
	#endregion

	#region Processing Extra Members
	public void load(PGraphics graphics, string path, Action<JSONArray> onComplete = null) {
		graphics.loadStringText(path, strings => {
			onLoadComplete(strings);
			if(onComplete!=null) { onComplete(this); }
		});
	}

	protected void onLoadComplete(string json) {
		list = MiniJSON.Json.Deserialize(json) as List<object>;
	}
	
	public bool save(PGraphics graphics, string path) {
		if(list==null) return false;
		string text = MiniJSON.Json.Serialize(list);
		if(text==null) return false;
		graphics.saveStringText(path, text);
		return true;
	}
	
	public bool isComplete { get { return list!=null; } }

	public T getValue<T>(int index, T defaultValue = default(T)) {
		if(index < 0 || index >= list.Count) return defaultValue;
		return (T)list[index];
	}

	public T[] getValueArray<T>() {
		if(list.Count<=0) return null;
		T[] array = new T[list.Count];
		for(int i=0; i<list.Count; i++){
			array[i] = (T)list[i];
		}
		return array;
	}

	public JSONArray setValue<T>(int index, T value) {
		if(index < 0 || index > list.Count) return null;
		if(index==list.Count) {
			list.Add(value);
		} else {
			list[index] = value;
		}
		return this;
	}

	public JSONArray appendValue<T>(T value) {
		list.Add(value);
		return this;
	}
	#endregion
	
}

public class JSONObject {
	internal Dictionary<string, object> data;

	public JSONObject() {
		data = new Dictionary<string, object>(); 
	}
	public JSONObject(Dictionary<string, object> dic) {
		this.data = dic;
	}
	
	#region Processing/Java Members
	public string getString(string key, string defaultValue=null) { return getValue(key, defaultValue); }
	public int getInt(string key, int defaultValue=0) { return (int)getValue(key, (long)defaultValue); }
	public float getFloat(string key, float defaultValue=0.0f) { return getValue(key, defaultValue); }
	public bool getBoolean(string key, bool defaultValue=false) { return getValue(key, defaultValue); }

	public JSONArray getJSONArray(string key) {
		List<object> list = getValue<List<object>>(key);
		if(list!=null) {
			return new JSONArray(list);
		}
		return null;
	}

	public JSONObject getJSONObject(string key) {
		Dictionary<string, object> dic = getValue<Dictionary<string, object>>(key);
		if(dic!=null) {
			return new JSONObject(dic);
		}
		return null;
	}

	public JSONObject setString(string key, string value) { return setValue(key, value); }
	public JSONObject setInt(string key, int value) { return setValue(key, value); }
	public JSONObject setFloat(string key, float value) { return setValue(key, value); }
	public JSONObject setBoolean(string key, bool value) { return setValue(key, value); }
	public JSONObject setJSONArray(string key, JSONArray value) { return setValue(key, value.list); }
	public JSONObject setJSONObject(string key, JSONObject value) { return setValue(key, value.data); }
	#endregion
	
	#region Processing Extra Members
	public void load(PGraphics graphics, string path, Action<JSONObject> onComplete = null) {
		graphics.loadStringText(path, strings => {
			onLoadComplete(strings);
			onComplete(this);
		});
	}

	public bool save(PGraphics graphics, string path) {
		if(data==null) return false;
		string text = MiniJSON.Json.Serialize(data);
		if(text==null) return false;
		graphics.saveStringText(path, text);
		return true;
	}

	protected void onLoadComplete(string json) {
		data = MiniJSON.Json.Deserialize(json) as Dictionary<string, object>;
	}

	public bool isComplete { get { return data!=null; } }

	public T getValue<T>(string key, T defaultValue = default(T)) {
		T value = defaultValue;
		if(data.ContainsKey (key)) {
			value = (T)data[key];
		} else {
			//Debug.LogWarning("key not found. " + key);
		}
		return value;
	}
	
	public JSONObject setValue<T>(string key, T value) {
		data[key] = value;
		return this;
	}
	#endregion
}


