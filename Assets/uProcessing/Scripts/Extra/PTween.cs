// PTween.cs
// (c) 2014 NISHIDA Ryota, http://dev.eyln.com
//
// Implementations based on TinyTween.cs
// https://gist.github.com/nickgravelyn/4953988 Copyright (c) 2013 Nick Gravelyn

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
 
// イベント通知関係
public delegate void PTweenOnComplete(PTween tween);
public delegate void PTweenOnUpdate(PTween tween);

// 補間アニメーション状態
public enum PTweenState {
	Running,
	Paused,
	Stopped,
	Completed,
	Finished,
}

// 補間インターフェイス
public interface PTween {
	PTweenState State { get; }
	PTween PrevTween { get; set; }
	PTween NextTween { get; set; }
	PTween FirstTween { get; }
	PTween LastTween { get; }
	bool isLoop { get; set; }
	bool isAutoRemove { get; set; }
	bool isRemove { get; set; }
	object Value { get; }
	Type ValueType { get; }
	float Duration { get; }
	bool isRunning { get; }
	bool isPaused { get; }
	bool isStopped { get; }
	bool isCompleted { get; }

	void pause();
    void resume();
	void restart();
	void stop();
	void stopCompleate();
	//void reversePlay();
	bool update(float elapsedTime = 1.0f / 60.0f);
	void setTarget(object obj, string name);
	void addOnComplete(PTweenOnComplete onComplete);
	void addOnUpdate(PTweenOnUpdate onUpdate);

	PTween tween<S>(object obj, string name, S from, S to, float duration, PTweenEaseFunc easeFunc, PTweenOnComplete onComplete=null, params object[] props) where S : struct;
	//PTween from<S>(S start, float delay=0.0f) where S : struct;
	//PTween from<S>(object obj, string name, S start, float delay=0.0f) where S : struct;
	PTween to<S>(S end, float duration, PTweenEaseFunc easeFunc, PTweenOnComplete onComplete=null) where S : struct;
	PTween wait(float duration, PTweenOnComplete onComplete=null);
	PTween loop();
	PTween getReverseTween();
	PTween reverse();
	PTween call(PTweenOnComplete onComplete);
	PTween callPTweenOnComplete(PTweenOnComplete onComplete);
	PTween callPTweenOnUpdate(PTweenOnUpdate onUpdate);
}

// 指定構造体の補間クラス
public class PTween<T> : PTween where T : struct {
	private PTween prevTween;
	private PTween nextTween;
	//private Tween<T> parallelTween;
	private readonly LerpFunc<T> lerpFunc;

    private TweenTimer timer;
    private T start;
    private T end;

	public bool isLoop { get; set; }
	public bool isAutoRemove { get; set; }
	bool isRemoveFlag = false;
	public bool isRemove { get { return isRemoveFlag; } set { isRemoveFlag = value; } }

	private TweenTarget target;
	public PTweenOnComplete onComplete;
	public PTweenOnUpdate onUpdate;

	private class TweenTimer
	{
		public PTweenState state;
		public float currentTime;
		public float duration;
		public float speed;
		public PTweenEaseFunc easeFunc;
		
		public TweenTimer() {}
		public TweenTimer(TweenTimer timer) {
			state = timer.state;
			currentTime = timer.currentTime;
			duration = timer.duration;
			speed = timer.speed;
			easeFunc = timer.easeFunc;
		}
	}

	private class TweenTarget
	{
		private T _value;
		public object obj;
		public string valueName;

		public T value {
			get {
				PTweenHelper.getValue(obj, valueName, out _value, _value);
				return _value;
			}
			set {
				PTweenHelper.setValue(obj, valueName, value);
				_value = value;
			}
		}

		public TweenTarget() {}
		public TweenTarget(TweenTarget target) {
			_value = target._value;
			obj = target.obj;
			valueName = target.valueName;
		}

		public TweenTarget(object obj, string valueName) {
			this.obj = obj;
			this.valueName = valueName;
		}

		public void setObj(object obj, string valueName) {
			this.obj = obj;
			this.valueName = valueName;
		}
	}
	
	public PTweenState State { get { return timer.state; } }
	public float CurrentTime { get { return timer.currentTime; } }
	public float Duration { get { return timer.duration; } }
	public T StartValue { get { return start; } }
    public T EndValue { get { return end; } }
	public T CurrentValue { get { return target.value; } }
	public object Value { get { return CurrentValue; } }
	public Type ValueType { get { return typeof(T); } }
	public PTween NextTween { get { return nextTween; }
		set { nextTween = value; if(value!=null && value.PrevTween!=this) value.PrevTween = this; } }
	public PTween PrevTween { get { return prevTween; }
		set { prevTween = value; if(value!=null && value.NextTween!=this) value.NextTween = this; } }
	public PTween FirstTween { get { return prevTween!=null ? prevTween.FirstTween : this; } }
	public PTween LastTween { get { return nextTween!=null ? nextTween.LastTween : this; } }

	public bool isRunning { get { return timer.state==PTweenState.Running; } }
	public bool isPaused { get { return timer.state==PTweenState.Paused; } }
	public bool isStopped { get { return timer.state==PTweenState.Stopped; } }
	public bool isCompleted { get { return timer.state==PTweenState.Completed || timer.state==PTweenState.Finished; } }

	public PTween(LerpFunc<T> lerpFunc) {
        this.lerpFunc = lerpFunc;
		timer = new TweenTimer();
		timer.state = PTweenState.Stopped;
		NextTween = null;
    }

	public PTween(PTween<T> tween) {
		lerpFunc = tween.lerpFunc;
		prevTween = null;
		nextTween = null;
		timer = new TweenTimer(tween.timer);
		start = tween.start;
		end = tween.end;
		isLoop = tween.isLoop;
		isAutoRemove = tween.isAutoRemove;
		target = new TweenTarget(tween.target);
		onComplete = tween.onComplete;
		onUpdate = tween.onUpdate;
	}

	public PTween play(T start, T end, float duration, PTweenEaseFunc easeFunc, PTweenOnComplete onComplete=null, float delay=0.0f) {
		if(duration <= 0 || easeFunc == null) {
			Debug.LogError("PTween play: Invalid Param");
			return null;
        }

		if(target == null) {
			target = new TweenTarget();
		}

		this.onComplete = onComplete;
		this.onUpdate = null;

		timer.currentTime = -delay;
		timer.duration = duration;
		timer.easeFunc = easeFunc;
		timer.speed = 1.0f;
		timer.state = PTweenState.Running;

        this.start = start;
        this.end = end;
		this.isLoop = false;
		this.isAutoRemove = true;

        updateValue();
		return this;
    }

	public void setTarget(PTween<T> tween) {
		if(tween.target!=null) {
			target = tween.target;
			updateValue();
		} else if (target == null) {
			target = new TweenTarget();
		}
	}

	public void setTarget(object obj, string name) {
		if (target == null) {
			target = new TweenTarget();
		}
		target.setObj(obj, name);
	}

	public void addOnComplete(PTweenOnComplete onComplete) {
		if (onComplete != null) {
			this.onComplete += onComplete;
		}
	}

	public void addOnUpdate(PTweenOnUpdate onUpdate) {
		if (onUpdate != null) {
			this.onUpdate += onUpdate;
		}
	}

	public PTween tween<S>(object obj, string name, S from, S to, float duration, PTweenEaseFunc easeFunc, PTweenOnComplete onComplete=null, params object[] props) where S : struct {
		PTween newTween = PTweener.tween(obj, name, from, to, duration, easeFunc, onComplete, props);
		NextTween = newTween;
		timer.state = PTweenState.Running;
		return newTween;
	}

	public PTween to<S>(S end, float duration, PTweenEaseFunc easeFunc, PTweenOnComplete onComplete=null) where S : struct {
		PTween<S> startTween = null;
		for(PTween t=this; t!=null; t=t.PrevTween) {
			startTween = t as PTween<S>;
			if(startTween!=null) break;
		}

		if(startTween==null) {
			Debug.LogWarning("PTween.to<S>: start <Incompatible Type>");
			return null;
		}

		object endValue = Convert.ChangeType(end, typeof(S));
		if(endValue==null) {
			Debug.LogWarning("PTween.to<S>: end <Incompatible Type>");
			return null;
		}

		return tween(startTween.target.obj, startTween.target.valueName, startTween.EndValue, (S)endValue, duration, easeFunc, onComplete);
	}

	public PTween getReverseTween() {
		PTween<T> newTween = new PTween<T>(this);
		newTween.onComplete = null;
		float delay = newTween.timer.currentTime < 0.0f ? newTween.timer.currentTime : 0.0f;
		newTween.play(newTween.end, newTween.start, newTween.Duration, newTween.timer.easeFunc, null, 0);
		if(delay > 0.0f) { newTween.wait(delay); }
		return newTween;
	}

	public PTween reverse() {
		PTween nt = this;
		for(PTween t = this; t!=null; t = t.PrevTween) {
			PTween rt = t.getReverseTween();
			nt.NextTween = rt;
			nt = rt.LastTween;
		}
		return nt;
	}

#region Test
	/*
	public void reversePlay() {
		timer.speed *= -1.0f;
		//for(PTween t = NextTween; t!=null; t = t.NextTween) { t.ReversePlay(); }
		//timer.state = PTweenState.Running;
	}
	*/
	
	private PTween _from<S>(object obj, string name, S start, float delay=0.0f) where S : struct {
		PTween<S> s = this;
		if (typeof(T) != typeof(S) || target != null) {
			s = PTweener.createTween<S>() as PTween<S>;
			NextTween = s;
			//timer.state = PTweenState.Running;
		}
		if(obj != null && name != null) {
			s.setTarget(obj, name);
		} else {
			s.setTarget(this);
		}
		s.timer.currentTime = -delay;
		s.start = start;
		return s;
	}

	/*
	private PTween _from<S>(object obj, string name) where S : struct {
		if(obj==null || name==null) { return null; }
		object objValue = PTweenHelper.getValue(obj, name);
		S value = objValue!=null ? (S)objValue : null;
		return _from(obj, name, value); 
	}
	*/

	private PTween _from<S>(S start, float delay=0.0f) where S : struct {
		return _from(null, null, start, delay);
	}

	private PTween _to<S>(S end, float duration, PTweenEaseFunc easeFunc, PTweenOnComplete onComplete=null) where S : struct {
		PTween<S> s = this;
		if(target != null) {
			s = s._from(s.end) as PTween<S>;
		}
		if(s!=null) {
			return s.play(s.start, end, duration, easeFunc, onComplete);
		}
		return s;
	}
#endregion

	public PTween wait(float duration, PTweenOnComplete onComplete=null) {
		timer.state = PTweenState.Running;
		var t = tween(null, "wait", 0, 0, duration, PEase.Linear, onComplete);
		NextTween = t;
		return t;
	}

	public PTween loop() {
		if(!isLoop) {
			isLoop = true;
			if(prevTween!=null) {
				prevTween.loop ();
			}
			if(nextTween!=null) {
				nextTween.loop ();
			}
		}
		return this;
	}

	public PTween call(PTweenOnComplete onComplete) {
		return callPTweenOnComplete(onComplete);
	}

	public PTween callPTweenOnComplete(PTweenOnComplete onComplete) {
		if(onComplete!=null) {
			addOnComplete(onComplete);
		}
		return this;
	}

	public PTween callPTweenOnUpdate(PTweenOnUpdate onUpdate) {
		if(onUpdate!=null) {
			addOnUpdate(onUpdate);
		}
		return this;
	}

	public void pause() {
		timer.state = PTweenState.Paused;
    }

    public void resume() {
		if (timer.currentTime >= timer.duration && nextTween != null) {
			timer.state = PTweenState.Finished;
		} else {
			timer.state = PTweenState.Running;
		}
    }

	public void restart() {
		timer.currentTime = 0.0f;
		timer.state = PTweenState.Running;
		if(prevTween==null) {
			updateValue();
		}
		if(nextTween!=null) {
			nextTween.restart();
		}
	}
	
	public void stop() {
		timer.state = PTweenState.Stopped;
	}

	public void stopCompleate() {
		timer.state = PTweenState.Stopped;
        timer.currentTime = timer.duration;
        updateValue();
		if(onComplete!=null) {
			onComplete(this);
		}
    }

	public bool update(float elapsedTime) {
		if(isRemove || timer.state == PTweenState.Stopped || timer.state == PTweenState.Paused) {
			return true;
		}

		bool isComplete = false;
		if(timer.currentTime < timer.duration) {
			timer.currentTime += elapsedTime * Mathf.Abs(timer.speed);
			if(timer.currentTime >= timer.duration) {
				if(nextTween == null) {
					timer.state = PTweenState.Finished;
					isComplete = true;
				} else if(timer.state!=PTweenState.Completed) {
					timer.state = PTweenState.Completed;
					elapsedTime = timer.currentTime - timer.duration;
					isComplete = true;
				}
				timer.currentTime = timer.duration;
			}
		}

		bool isPlaying = true;

		if(isComplete) {
			updateValue ();
			if(onComplete!=null) {
				onComplete(this);
			}
			if(nextTween == null) {
				if(isLoop && prevTween==null) {
					restart();
					return true;
				} else {
					timer.state = PTweenState.Finished;
					return false;
				}
			}
		}

		if (timer.state==PTweenState.Completed && nextTween != null) {
			if( !nextTween.update (elapsedTime) ) {
				if(isLoop && prevTween==null) {
					restart();
				} else {
					timer.state = PTweenState.Finished;
					isPlaying = false;
				}
			}
		} else if(!isComplete) {
			updateValue();
		}

		return isPlaying;
	}

    private void updateValue() {
		if (timer.currentTime < 0.0f || timer.easeFunc==null) {
			return;
		}
		float t = timer.currentTime / timer.duration;
		if(timer.speed < 0.0f) { t = 1.0f - t; }
		target.value = lerpFunc(start, end, timer.easeFunc(t));
		if(onUpdate!=null) {
			onUpdate(this);
		}
	}
}

//
public class PTweenHelper {
	private const BindingFlags fieldFlags = BindingFlags.GetField | BindingFlags.SetField
		| BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
	private const BindingFlags propertyFlags = BindingFlags.GetProperty | BindingFlags.SetProperty
		| BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

	// 指定名フィールドの値を取得
	public static bool getValue<T>(object obj, string name, out T objValue, T defaultValue) {
		if (obj == null) {
			objValue = defaultValue;
			return false;
		}

		Type t = obj.GetType();
		System.Reflection.FieldInfo fi = t.GetField(name, fieldFlags);
		PropertyInfo pi = t.GetProperty(name, propertyFlags);
		if (fi != null) {
			objValue = (T)fi.GetValue(obj);
			return true;
		} else if (pi != null) {
			MethodInfo getMethod = pi.GetGetMethod(true);
			objValue = (T)getMethod.Invoke(obj, null);
			return true;
		}

		objValue = defaultValue;

		Debug.LogWarning("PTween getValue : Not found target : " + obj + " / " + name);
		return false;
	}

	// 指定名フィールドに値を設定
	public static bool setValue<T>(object obj, string name, T value) {
		if (obj == null) {
			return false;
		}

		Type t = obj.GetType();
		System.Reflection.FieldInfo fi = t.GetField(name, fieldFlags);
		PropertyInfo pi = t.GetProperty(name, propertyFlags);

		if (fi != null) {
			fi.SetValue(obj, value);
			return true;
		}
		
		if (pi != null) {
			MethodInfo setMethod = pi.GetSetMethod();
			if(setMethod==null) {
				setMethod = pi.GetSetMethod(true);
			}
			setMethod.Invoke(obj, new object[1] {value});
			return true;
		}

		Debug.LogWarning("PTween setValue : Not found target : " + obj + " / " + name);
		return false;
	}
}

public delegate float PTweenEaseFunc(float progress);
public delegate T LerpFunc<T>(T start, T end, float progress);

public class IntTween : PTween<int>
{
	private static int LerpInt(int start, int end, float progress) { return (int)(start + (end - start) * progress); }
	private static readonly LerpFunc<int> LerpFunc = LerpInt;
	public IntTween() : base(LerpFunc) { }
}

public class FloatTween : PTween<float>
{
    private static float LerpFloat(float start, float end, float progress) { return start + (end - start) * progress; }
    private static readonly LerpFunc<float> LerpFunc = LerpFloat;
    public FloatTween() : base(LerpFunc) { }
}

public class Vector2Tween : PTween<Vector2>
{
    private static readonly LerpFunc<Vector2> LerpFunc = Vector2.Lerp;
    public Vector2Tween() : base(LerpFunc) { }
}

public class Vector3Tween : PTween<Vector3>
{
    private static readonly LerpFunc<Vector3> LerpFunc = Vector3.Lerp;
    public Vector3Tween() : base(LerpFunc) { }
}

public class Vector4Tween : PTween<Vector4>
{
    private static readonly LerpFunc<Vector4> LerpFunc = Vector4.Lerp;
    public Vector4Tween() : base(LerpFunc) { }
}

public class ColorTween : PTween<Color>
{
    private static readonly LerpFunc<Color> LerpFunc = Color.Lerp;
    public ColorTween() : base(LerpFunc) { }
}

public class QuaternionTween : PTween<Quaternion>
{
    private static readonly LerpFunc<Quaternion> LerpFunc = Quaternion.Lerp;
    public QuaternionTween() : base(LerpFunc) { }
}

public static class PEase
{
	public static readonly PTweenEaseFunc Step = StepImpl;
    public static readonly PTweenEaseFunc Linear = LinearImpl;
    public static readonly PTweenEaseFunc InQuad = EaseInQuadImpl;
    public static readonly PTweenEaseFunc OutQuad = EaseOutQuadImpl;
    public static readonly PTweenEaseFunc InOutQuad = EaseInOutQuadImpl;
    public static readonly PTweenEaseFunc InCubic = EaseInCubicImpl;
    public static readonly PTweenEaseFunc OutCubic = EaseOutCubicImpl;
    public static readonly PTweenEaseFunc InOutCubic = EaseInOutCubicImpl;
	public static readonly PTweenEaseFunc InQuart = EaseInQuartImpl;
    public static readonly PTweenEaseFunc OutQuart = EaseOutQuartImpl;
    public static readonly PTweenEaseFunc InOutQuart = EaseInOutQuartImpl;
    public static readonly PTweenEaseFunc InQuint = EaseInQuintImpl;
    public static readonly PTweenEaseFunc OutQuint = EaseOutQuintImpl;
    public static readonly PTweenEaseFunc InOutQuint = EaseInOutQuintImpl;
    public static readonly PTweenEaseFunc InSine = EaseInSineImpl;
    public static readonly PTweenEaseFunc OutSine = EaseOutSineImpl;
    public static readonly PTweenEaseFunc InOutSine = EaseInOutSineImpl;
	public static readonly PTweenEaseFunc InBounce = EaseInBounceImpl;
	public static readonly PTweenEaseFunc OutBounce = EaseOutBounceImpl;
	public static readonly PTweenEaseFunc InOutBounce = EaseInOutBounceImpl;

    private static float StepImpl(float progress) { return progress < 0.5f ? 0.0f : 1.0f; }
    private static float LinearImpl(float progress) { return progress; }
    private static float EaseInQuadImpl(float progress) { return EaseInPower(progress, 2); }
    private static float EaseOutQuadImpl(float progress) { return EaseOutPower(progress, 2); }
    private static float EaseInOutQuadImpl(float progress) { return EaseInOutPower(progress, 2); }
    private static float EaseInCubicImpl(float progress) { return EaseInPower(progress, 3); }
    private static float EaseOutCubicImpl(float progress) { return EaseOutPower(progress, 3); }
    private static float EaseInOutCubicImpl(float progress) { return EaseInOutPower(progress, 3); }
    private static float EaseInQuartImpl(float progress) { return EaseInPower(progress, 4); }
    private static float EaseOutQuartImpl(float progress) { return EaseOutPower(progress, 4); }
    private static float EaseInOutQuartImpl(float progress) { return EaseInOutPower(progress, 4); }
    private static float EaseInQuintImpl(float progress) { return EaseInPower(progress, 5); }
    private static float EaseOutQuintImpl(float progress) { return EaseOutPower(progress, 5); }
    private static float EaseInOutQuintImpl(float progress) { return EaseInOutPower(progress, 5); }

	private const float Pi = Mathf.PI;
	private const float HalfPi = Pi * 0.5f;
	
	private static float EaseInPower(float progress, int power) {
        return Mathf.Pow(progress, power);
    }

    private static float EaseOutPower(float progress, int power) {
        int sign = power % 2 == 0 ? -1 : 1;
        return (float)(sign * (Math.Pow(progress - 1, power) + sign));
    }

    private static float EaseInOutPower(float progress, int power) {
		progress *= 2.0f;
		if (progress < 1.0f) {
            return (float)Math.Pow(progress, power) * 0.5f;
        } else {
            int sign = power % 2 == 0 ? -1 : 1;
			return (float)(sign / 2.0f * (Math.Pow(progress - 2.0f, power) + sign * 2));
        }
    }

    private static float EaseInSineImpl(float progress) {
		return (float)Math.Sin(progress * HalfPi - HalfPi) + 1.0f;
    }

    private static float EaseOutSineImpl(float progress) {
        return (float)Math.Sin(progress * HalfPi);
    }

    private static float EaseInOutSineImpl(float progress) {
		return (float)(Math.Sin(progress * Pi - HalfPi) + 1.0f) * 0.5f;
    }

	private static float EaseInBounceImpl(float progress) {
		return 1.0f - EaseOutBounceImpl( 1.0f - progress );
	}
	
	private static float EaseOutBounceImpl(float progress) {
		if ( progress < ( 1 / 2.75f ) ) {
			return 7.5625f * progress * progress;
		} else if ( progress < ( 2 / 2.75f ) ) {
			return 7.5625f * ( progress -= ( 1.5f / 2.75f ) ) * progress + 0.75f;
		} else if ( progress < ( 2.5 / 2.75 ) ) {
			return 7.5625f * ( progress -= ( 2.25f / 2.75f ) ) * progress + 0.9375f;
		} else {
			return 7.5625f * ( progress -= ( 2.625f / 2.75f ) ) * progress + 0.984375f;
		}
	}
	
	private static float EaseInOutBounceImpl(float progress) {
		if ( progress < 0.5f ) return EaseInBounceImpl( progress * 2.0f ) * 0.5f;
		return EaseOutBounceImpl( progress * 2.0f - 1.0f ) * 0.5f + 0.5f;
	}
}

public class PTweener {
	private static List<PTween> tweens = new List<PTween>();

	public static bool add(PTween tween) {
		if(tween==null) return false;
		tweens.Add(tween);
		return true;
	}

	public static bool removeOne(PTween tween) {
		if(tween==null) return false;
		//tweens.Remove(tween);
		tween.isRemove = true;
		return true;
	}

	public static bool remove(PTween tween) { // remove Group
		if(tween==null) return false;
		for(PTween t = tween.NextTween; t!=null; t = t.NextTween) {
			//tweens.Remove(t);
			t.isRemove = true;
		}
		for(PTween t = tween.PrevTween; t!=null; t = t.PrevTween) {
			//tweens.Remove(t);
			t.isRemove = true;
		}
		//tweens.Remove(tween);
		tween.isRemove = true;
		return true;
	}

	public static void clear() {
		tweens.Clear();
	}
	
	public static void update(float elapsedTime = 1.0f / 60.0f) {
		foreach(PTween t in tweens) {
			if(t.PrevTween==null) {
				t.update(elapsedTime);
			}
		}
		tweens.RemoveAll(t => (t.isRemove) || (t.State == PTweenState.Finished && t.isAutoRemove));
	}

	// 補間クラス作成
	public static PTween tween<T>(object obj, string name, T from, T to, float duration, PTweenEaseFunc easeFunc, PTweenOnComplete onComplete=null, params object[] props) {
		return tween(obj, duration, easeFunc, onComplete, "target", name, "from", from, "to", to);
	}
	
	public static PTween tween(object obj, float duration, PTweenEaseFunc easeFunc, params object[] props) {
		return tween(obj, duration, easeFunc, props);
	}

	public static PTween tween(object obj, float duration, PTweenEaseFunc easeFunc, PTweenOnComplete onComplete, params object[] props) {
		PTweenArgs args = new PTweenArgs (props);
		float delay = args.getValue<float> ("delay",  0.0f);
		if(onComplete==null) {
			onComplete = args.getValue<PTweenOnComplete> ("onComplate", null);
		}
		PTween rtw = null;

		if (args.isExist ("from") && args.isExist ("to")) {
			if (args.isType<int>("from")) {
				rtw = play<int>(obj, args, duration, easeFunc, onComplete, delay);
			} else if (args.isType<float>("from")) {
				rtw = play<float>(obj, args, duration, easeFunc, onComplete, delay);
			} else if (args.isType<Vector2>("from")) {
				rtw = play<Vector2>(obj, args, duration, easeFunc, onComplete, delay);
			} else if (args.isType<Vector3>("from")) {
				rtw = play<Vector3>(obj, args, duration, easeFunc, onComplete, delay);
			} else if (args.isType<Vector4>("from")) {
				rtw = play<Vector4>(obj, args, duration, easeFunc, onComplete, delay);
			} else if (args.isType<Color>("from")) {
				rtw = play<Color>(obj, args, duration, easeFunc, onComplete, delay);
			} else if (args.isType<Quaternion>("from")) {
				rtw = play<Quaternion>(obj, args, duration, easeFunc, onComplete, delay);
			} else {
				Debug.LogError("PTween: invalid type:" + args.getType("from"));
				return null;
			}
		}
		return rtw;
	}

	// 補間クラス作成
	// targetObj
	// target
	// from
	// to
	// duduration or time
	// easeType
	// onComplete
	// delay
	// isLoop
	public static PTween _tween(params object[] props) {
		PTweenArgs args = new PTweenArgs (props);

		object obj = args.getValue<object>("targetObj", null);

		PTweenEaseFunc easeFunc = args.getValue<PTweenEaseFunc>("easeType", PEase.Linear);
		float duration = args.getValue<float>("duration", "time", 1.0f);
		return tween(obj, duration, easeFunc, props);
	}

	public static PTween createTween<T>() where T : struct {
		PTween tween = null;
		Type t = typeof(T);
		if (t==typeof(int)) { tween = new IntTween(); }
		else if (t==typeof(float)) { tween = new FloatTween(); }
		else if (t==typeof(Vector2)) { tween = new Vector2Tween(); }
		else if (t==typeof(Vector3)) { tween = new Vector3Tween(); }
		else if (t==typeof(Vector4)) { tween = new Vector4Tween(); }
		else if (t==typeof(Color)) { tween = new ColorTween(); }
		else if (t==typeof(Quaternion)) { tween = new QuaternionTween(); }
		else { Debug.LogError("PTween createTween: invalid type: " + t); return null; }
		add(tween);
		return tween;
	}
	
	private static PTween play<T>(object obj, PTweenArgs args, float duration, PTweenEaseFunc easeFunc, PTweenOnComplete onComplete, float delay) where T : struct {
		var tw = createTween<T>() as PTween<T>;
		if(tw!=null) {
			tw.isLoop = args.getValue<bool> ("isLoop",  false);

			if (obj != null) {
				string valueName = args.getValue<string> ("target", null);
				if (valueName != null) {
					tw.setTarget(obj, valueName);
				}
				/*if(onComplete!=null) {
				tw.AddOnComplete(onComplete);
				}*/
			}
			tw.play(args.getValue<T>("from"), args.getValue<T>("to"), duration, easeFunc, onComplete, delay);
		}
		return tw;
	}

	class PTweenArgs {
		Hashtable args;
		
		public PTweenArgs(params object[] props) {
			args = cleanArgs(hash(props));
		}

		public Hashtable hash(params object[] props){
			Hashtable hashTable = new Hashtable(props.Length/2);
			if (props.Length %2 != 0) {
				Debug.LogError("PTween Error: Hash requires an even number of arguments!"); 
				return null;
			}else{
				int i = 0;
				while(i < props.Length - 1) {
					hashTable.Add(props[i], props[i+1]);
					i += 2;
				}
				return hashTable;
			}
		}

		Hashtable cleanArgs(Hashtable args){
			Hashtable argsCopy = new Hashtable(args.Count);
			Hashtable argsCaseUnified = new Hashtable(args.Count);
			
			foreach (DictionaryEntry item in args) {
				argsCopy.Add(item.Key, item.Value);
			}
			
			foreach (DictionaryEntry item in argsCopy) {
				if(item.Value==null) continue;
				if(item.Value.GetType() == typeof(System.Double)){
					double original = (double)item.Value;
					float casted = (float)original;
					args[item.Key] = casted;
				}
			}   
			
			foreach (DictionaryEntry item in args) {
				argsCaseUnified.Add(item.Key.ToString().ToLower(), item.Value);
			}   
			
			args = argsCaseUnified;
			return args;
		}

		public bool isType<T>(string name) {
			return args[name] is T;
		}

		public Type getType(string name) {
			return args[name].GetType();
		}

		public bool isExist(string name) {
			return args.Contains(name);
		}

		public T getValue<T>(string name) {
			if (args.Contains(name)) {
				return (T)args[name];
			} else {
				return default(T);
			}
		}

		public T getValue<T>(string name, T defaultValue) {
			if (args.Contains(name)) {
				return (T)args[name];
			} else {
				return defaultValue;
			}
		}

		public T getValue<T>(string nameA, string nameB, T defaultValue) {
			if (args.Contains(nameA)) {
				return (T)args[nameA];
			} else if (args.Contains (nameB)) {
				return (T)args[nameB];
			} else {
				return defaultValue;
			}
		}
	}
}

public class PTweenCompilerHint {
	// for iOS (ExecutionEngineException: Attempting to JIT compile method * while running with –aot-only.)
	static public void unusedCode() {
		PTween<int> ti = PTweener.tween<int>(null, "hint", default(int), default(int), 1.0f, PEase.Linear) as PTween<int>;
		PTween<float> tf = PTweener.tween<float>(null, "hint", default(float), default(float), 1.0f, PEase.Linear) as PTween<float>;
		PTween<Color> tc = PTweener.tween<Color>(null, "hint", default(Color), default(Color), 1.0f, PEase.Linear) as PTween<Color>;
		PTween<Vector2> tv2 = PTweener.tween<Vector2>(null, "hint", default(Vector2), default(Vector2), 1.0f, PEase.Linear) as PTween<Vector2>;
		PTween<Vector3> tv3 = PTweener.tween<Vector3>(null, "hint", default(Vector3), default(Vector3), 1.0f, PEase.Linear) as PTween<Vector3>;
		PTween<Vector4> tv4 = PTweener.tween<Vector4>(null, "hint", default(Vector4), default(Vector4), 1.0f, PEase.Linear) as PTween<Vector4>;
		PTween<Quaternion> tq = PTweener.tween<Quaternion>(null, "hint", Quaternion.identity, Quaternion.identity, 1.0f, PEase.Linear) as PTween<Quaternion>;
		
		ti.tween<int>(null, "hint", default(int), default(int), 1.0f, PEase.Linear);
		ti.tween<float>(null, "hint", default(float), default(float), 1.0f, PEase.Linear);
		ti.tween<Color>(null, "hint", default(Color), default(Color), 1.0f, PEase.Linear);
		ti.tween<Vector2>(null, "hint", default(Vector2), default(Vector2), 1.0f, PEase.Linear);
		ti.tween<Vector3>(null, "hint", default(Vector3), default(Vector3), 1.0f, PEase.Linear);
		ti.tween<Vector4>(null, "hint", default(Vector4), default(Vector4), 1.0f, PEase.Linear);
		ti.tween<Quaternion>(null, "hint", Quaternion.identity, Quaternion.identity, 1.0f, PEase.Linear);
		
		tf.tween<int>(null, "hint", default(int), default(int), 1.0f, PEase.Linear);
		tf.tween<float>(null, "hint", default(float), default(float), 1.0f, PEase.Linear);
		tf.tween<Color>(null, "hint", default(Color), default(Color), 1.0f, PEase.Linear);
		tf.tween<Vector2>(null, "hint", default(Vector2), default(Vector2), 1.0f, PEase.Linear);
		tf.tween<Vector3>(null, "hint", default(Vector3), default(Vector3), 1.0f, PEase.Linear);
		tf.tween<Vector4>(null, "hint", default(Vector4), default(Vector4), 1.0f, PEase.Linear);
		tf.tween<Quaternion>(null, "hint", Quaternion.identity, Quaternion.identity, 1.0f, PEase.Linear);
		
		tc.tween<int>(null, "hint", default(int), default(int), 1.0f, PEase.Linear);
		tc.tween<float>(null, "hint", default(float), default(float), 1.0f, PEase.Linear);
		tc.tween<Color>(null, "hint", default(Color), default(Color), 1.0f, PEase.Linear);
		tc.tween<Vector2>(null, "hint", default(Vector2), default(Vector2), 1.0f, PEase.Linear);
		tc.tween<Vector3>(null, "hint", default(Vector3), default(Vector3), 1.0f, PEase.Linear);
		tc.tween<Vector4>(null, "hint", default(Vector4), default(Vector4), 1.0f, PEase.Linear);
		tc.tween<Quaternion>(null, "hint", Quaternion.identity, Quaternion.identity, 1.0f, PEase.Linear);
		
		tv2.tween<int>(null, "hint", default(int), default(int), 1.0f, PEase.Linear);
		tv2.tween<float>(null, "hint", default(float), default(float), 1.0f, PEase.Linear);
		tv2.tween<Color>(null, "hint", default(Color), default(Color), 1.0f, PEase.Linear);
		tv2.tween<Vector2>(null, "hint", default(Vector2), default(Vector2), 1.0f, PEase.Linear);
		tv2.tween<Vector3>(null, "hint", default(Vector3), default(Vector3), 1.0f, PEase.Linear);
		tv2.tween<Vector4>(null, "hint", default(Vector4), default(Vector4), 1.0f, PEase.Linear);
		tv2.tween<Quaternion>(null, "hint", Quaternion.identity, Quaternion.identity, 1.0f, PEase.Linear);
		
		tv3.tween<int>(null, "hint", default(int), default(int), 1.0f, PEase.Linear);
		tv3.tween<float>(null, "hint", default(float), default(float), 1.0f, PEase.Linear);
		tv3.tween<Color>(null, "hint", default(Color), default(Color), 1.0f, PEase.Linear);
		tv3.tween<Vector2>(null, "hint", default(Vector2), default(Vector2), 1.0f, PEase.Linear);
		tv3.tween<Vector3>(null, "hint", default(Vector3), default(Vector3), 1.0f, PEase.Linear);
		tv3.tween<Vector4>(null, "hint", default(Vector4), default(Vector4), 1.0f, PEase.Linear);
		tv3.tween<Quaternion>(null, "hint", Quaternion.identity, Quaternion.identity, 1.0f, PEase.Linear);
		
		tv4.tween<float>(null, "hint", default(float), default(float), 1.0f, PEase.Linear);
		tv4.tween<Color>(null, "hint", default(Color), default(Color), 1.0f, PEase.Linear);
		tv4.tween<Vector2>(null, "hint", default(Vector2), default(Vector2), 1.0f, PEase.Linear);
		tv4.tween<Vector3>(null, "hint", default(Vector3), default(Vector3), 1.0f, PEase.Linear);
		tv4.tween<Vector4>(null, "hint", default(Vector4), default(Vector4), 1.0f, PEase.Linear);
		tv4.tween<Quaternion>(null, "hint", Quaternion.identity, Quaternion.identity, 1.0f, PEase.Linear);
		
		tq.tween<int>(null, "hint", default(int), default(int), 1.0f, PEase.Linear);
		tq.tween<float>(null, "hint", default(float), default(float), 1.0f, PEase.Linear);
		tq.tween<Color>(null, "hint", default(Color), default(Color), 1.0f, PEase.Linear);
		tq.tween<Vector2>(null, "hint", default(Vector2), default(Vector2), 1.0f, PEase.Linear);
		tq.tween<Vector3>(null, "hint", default(Vector3), default(Vector3), 1.0f, PEase.Linear);
		tq.tween<Vector4>(null, "hint", default(Vector4), default(Vector4), 1.0f, PEase.Linear);
		tq.tween<Quaternion>(null, "hint", Quaternion.identity, Quaternion.identity, 1.0f, PEase.Linear);
		
		PTweener.clear();
	}
}
