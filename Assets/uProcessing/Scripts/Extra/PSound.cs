// PSound.cs
// (c) 2014 NISHIDA Ryota, http://dev.eyln.com
//
// Implementations based on http://marupeke296.com/UNI_SND_No4_BGM.html

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

class PSound // : MonoBehaviour
{
	static PSound instance = null;
	static GameObject psoundObj = null;
	static PSoundPlayer soundPlayer;

	public PSound() {
		setup();
	}

	public static void setup(bool dontDestroyOnLoad = false) {
		if(!psoundObj) {
			psoundObj = new GameObject("PSound");
		}
		if(dontDestroyOnLoad) {
			UnityEngine.Object.DontDestroyOnLoad(psoundObj);
		}
		if(soundPlayer==null) {
			soundPlayer = new PSoundPlayer();
		}
		if(Camera.main!=null && Camera.main.gameObject.GetComponent<AudioListener>()==null) {
			Camera.main.gameObject.AddComponent<AudioListener> ();
		}
	}

	static void ready() {
		if(instance==null) instance = new PSound();
	}

	static public void destroy() {
		if(instance!=null) {
			if(soundPlayer!=null) {
				soundPlayer.clearBGM();
				soundPlayer.clearSE();
				soundPlayer = null;
			}
			if(psoundObj!=null) {
				GameObject.Destroy(psoundObj);
				psoundObj = null;
			}
			instance = null;
		}
	}

	public static void update() {
		if(instance!=null) {
			soundPlayer.update();
		}
	}

	public static void setSEVolume(float scale) {
		ready();
		soundPlayer.setSEVolume(scale);
	}

	public static void setBGMVolume(float scale) {
		ready();
		soundPlayer.setBGMVolume(scale);
	}
	
	public static void reserveSE(string resourceName, string name = null) {
		ready();
		soundPlayer.reserveSE(resourceName, name);
	}

	public static void reserveBGM(string resourceName, string name = null) {
		ready();
		soundPlayer.reserveBGM(resourceName, name);
	}
	
	public static void playSE(string name, float volume = 1.0f) {
		ready();
		soundPlayer.playSE(name, volume);
	}

	public static void playBGM(string name, float volume = 1.0f, float fadeTime = 0.0f) {
		ready();
		soundPlayer.playBGM(name, volume, fadeTime);
	}

	public static void playBGM() {
		ready();
		soundPlayer.playBGM();
	}
	
	public static void setLoopBGM(bool isLoop) {
		ready();
		soundPlayer.setLoopBGM(isLoop);
	}

	public static void pauseBGM() {
		ready();
		soundPlayer.pauseBGM();
	}

	public static bool isPauseBGM() {
		ready();
		return soundPlayer.isPauseBGM();
	}

	public static bool isPlayingBGM() {
		ready();
		return soundPlayer.isPlayingBGM();
	}

	public static void stopBGM(float fadeTime = 0.0f) {
		ready();
		soundPlayer.stopBGM(fadeTime);
	}

	public static void clear() {
		ready();
		soundPlayer.clear();
	}
	
	public static void clearSE() {
		ready();
		soundPlayer.clearSE();
	}
	
	public static void clearBGM() {
		ready();
		soundPlayer.clearBGM();
	}
	
	//
	protected class PSoundPlayer {
		GameObject soundPlayerObj;
		AudioSource audioSource;
		Dictionary<string, AudioClipInfo> seClips = new Dictionary<string, AudioClipInfo>();
		Dictionary<string, AudioClipInfo> bgmClips = new Dictionary<string, AudioClipInfo>();

		PBGMPlayer curBGMPlayer;
		PBGMPlayer fadeOutBGMPlayer;

		float bgmVolume = 1.0f;
		float seVolume = 1.0f;

		// AudioClip information
		class AudioClipInfo {
			public string resourceName;
			public string name;
			public AudioClip clip;
			
			public AudioClipInfo( string resourceName, string name ) {
				this.resourceName = resourceName;
				this.name = name;
			}
		}
		
		public PSoundPlayer() {}

		public void setSEVolume(float scale) {
			seVolume = scale;
		}

		public void setBGMVolume(float scale) {
			bgmVolume = scale;
			if (curBGMPlayer != null) {
				curBGMPlayer.volume = scale;
			}
		}

		public void clear() {
			clearSE();
			clearBGM();
		}

		public void clearSE() {
			foreach(KeyValuePair<string, AudioClipInfo> pair in seClips) {
				//GameObject.Destroy( pair.Value.clip );
				Resources.UnloadAsset(pair.Value.clip);
			}
			seClips.Clear();
		}
		
		public void clearBGM() {
			stopBGM(0.0f);
			if(curBGMPlayer!=null) {
				curBGMPlayer.destory();
				curBGMPlayer = null;
			}
			if(fadeOutBGMPlayer!=null) {
				fadeOutBGMPlayer.destory();
				fadeOutBGMPlayer = null;
			}
			foreach(KeyValuePair<string, AudioClipInfo> pair in bgmClips) {
				//GameObject.Destroy( pair.Value.clip );
				Resources.UnloadAsset(pair.Value.clip);
			}
			bgmClips.Clear();
		}

		public void reserveSE(string resourceName, string name = null) {
			if (name == null) {
				name = resourceName;
			}
			seClips.Add(name, new AudioClipInfo(resourceName, name) );
		}

		public void reserveBGM(string resourceName, string name = null) {
			if (name == null) {
				name = resourceName;
			}
			bgmClips.Add(name, new AudioClipInfo(resourceName, name) );
		}

		public bool playSE( string seName, float volume = 1.0f ) {
			if (seClips.ContainsKey (seName) == false) {
				reserveSE(seName);
				//return false; // not register
			}
			
			AudioClipInfo info = seClips[ seName ];
			
			// Load
			if ( info.clip == null )
				info.clip = (AudioClip)Resources.Load( info.resourceName );
			
			if ( soundPlayerObj == null ) {
				soundPlayerObj = new GameObject( "SoundPlayer" );
				if(psoundObj) { soundPlayerObj.transform.parent = psoundObj.transform; }
				audioSource = soundPlayerObj.AddComponent<AudioSource>();
			}
			
			// Play SE
			audioSource.PlayOneShot( info.clip, volume * seVolume );
			
			return true;
		}

		public void playBGM( string bgmName, float volume = 1.0f, float fadeTime = 0.0f ) {
			// destory old BGM
			if ( fadeOutBGMPlayer != null )
				fadeOutBGMPlayer.destory();
			
			// change to fade out for current BGM
			if ( curBGMPlayer != null ) {
				curBGMPlayer.stopBGM( fadeTime );
				fadeOutBGMPlayer = curBGMPlayer;
			}
			
			// play new BGM
			if ( bgmClips.ContainsKey( bgmName ) == false ) {
				reserveBGM(bgmName);
				// null BGM
				//curBGMPlayer = new BGMPlayer();
				//return false; // not register
			}

			curBGMPlayer = new PBGMPlayer( bgmClips[ bgmName ].resourceName );
			curBGMPlayer.localVolume = volume;
			curBGMPlayer.volume = bgmVolume;
			curBGMPlayer.playBGM( fadeTime );
		}

		public void resumeBGM() {
			playBGM();
		}

		public void playBGM() {
			if ( curBGMPlayer != null && curBGMPlayer.hadFadeOut() == false )
				curBGMPlayer.playBGM();
			if ( fadeOutBGMPlayer != null && fadeOutBGMPlayer.hadFadeOut() == false )
				fadeOutBGMPlayer.playBGM();
		}

		public void setLoopBGM(bool isLoop) {
			if ( curBGMPlayer != null ) {
				curBGMPlayer.setLoopBGM(isLoop);
			}
		}

		public void pauseBGM() {
			if ( curBGMPlayer != null )
				curBGMPlayer.pauseBGM();
			if ( fadeOutBGMPlayer != null )
				fadeOutBGMPlayer.pauseBGM();
		}

		public bool isPauseBGM() {
			return curBGMPlayer!=null ? curBGMPlayer.isPause() : false;
		}

		public bool isPlayingBGM() {
			return curBGMPlayer!=null ? curBGMPlayer.isPlaying() : false;
		}
		
		public void stopBGM( float fadeTime ) {
			if ( curBGMPlayer != null )
				curBGMPlayer.stopBGM( fadeTime );
			if ( fadeOutBGMPlayer != null )
				fadeOutBGMPlayer.stopBGM( fadeTime );
		}

		public void update() {
			if(fadeOutBGMPlayer!=null) { fadeOutBGMPlayer.update(); }
			if(curBGMPlayer!=null) { curBGMPlayer.update(); }
		}
	}

	//
	protected class PBGMPlayer {
		class State {
			protected PBGMPlayer bgmPlayer;
			public State( PBGMPlayer bgmPlayer ) {
				this.bgmPlayer = bgmPlayer;
			}
			public virtual void playBGM() {}
			public virtual void pauseBGM() {}
			public virtual void stopBGM() {}
			public virtual void update() {}
		}

		class Wait : State {
			public Wait( PBGMPlayer bgmPlayer ) : base( bgmPlayer ) {}
			
			public override void playBGM() {
				if ( bgmPlayer.fadeInTime > 0.0f )
					bgmPlayer.state = new FadeIn( bgmPlayer );
				else
					bgmPlayer.state = new Playing( bgmPlayer );
			}
		}

		class FadeIn : State {
			float t = 0.0f;
			
			public FadeIn( PBGMPlayer bgmPlayer ) : base( bgmPlayer ) {
				bgmPlayer.source.Play();
				bgmPlayer.source.volume = 0.0f;
			}
			
			public override void pauseBGM() {
				bgmPlayer.state = new Pause( bgmPlayer, this );
			}
			
			public override void stopBGM() {
				bgmPlayer.state = new FadeOut( bgmPlayer );
			}
			
			public override void update() {
				t += Time.deltaTime;
				bgmPlayer.source.volume = t / bgmPlayer.fadeInTime * bgmPlayer.baseVolume;
				if ( t >= bgmPlayer.fadeInTime ) {
					bgmPlayer.source.volume = bgmPlayer.baseVolume;
					bgmPlayer.state = new Playing( bgmPlayer );
				}
			}
		}

		class Playing : State {
			public Playing( PBGMPlayer bgmPlayer ) : base( bgmPlayer ) {
				if ( bgmPlayer.source.isPlaying == false ) {
					bgmPlayer.source.volume = bgmPlayer.baseVolume;
					bgmPlayer.source.Play();
				}
			}
			
			public override void pauseBGM() {
				bgmPlayer.state = new Pause( bgmPlayer, this );
			}
			
			public override void stopBGM() {
				bgmPlayer.state = new FadeOut( bgmPlayer );
			}
		}

		class Pause : State {
			State preState;
			
			public Pause( PBGMPlayer bgmPlayer, State preState ) : base( bgmPlayer ) {
				this.preState = preState;
				bgmPlayer.source.Pause();
			}
			
			public override void stopBGM() {
				bgmPlayer.source.Stop();
				bgmPlayer.state = new Wait( bgmPlayer );
			}
			
			public override void playBGM() {
				bgmPlayer.state = preState;
				bgmPlayer.source.Play();
			}
		}

		class FadeOut : State {
			float initVolume;
			float t = 0.0f;

			public FadeOut( PBGMPlayer bgmPlayer ) : base( bgmPlayer ) {
				initVolume = bgmPlayer.source.volume;
				bgmPlayer.isFinishFadeOut = false;
			}
			
			public override void pauseBGM() {
				bgmPlayer.state = new Pause( bgmPlayer, this );
			}
			
			public override void update() {
				t += Time.deltaTime;
				bgmPlayer.source.volume = initVolume * ( 1.0f - t / bgmPlayer.fadeOutTime );
				if ( t >= bgmPlayer.fadeOutTime ) {
					bgmPlayer.source.volume = 0.0f;
					bgmPlayer.source.Stop();
					bgmPlayer.state = new Wait( bgmPlayer );
					bgmPlayer.isFinishFadeOut = true;
				}
			}
		}

		GameObject obj;
		AudioSource source;
		State state;
		float fadeInTime = 0.0f;
		float fadeOutTime = 0.0f;
		bool isFinishFadeOut = false;
		float baseVolume = 1.0f;
		public float localVolume = 1.0f;
		public float volume { 
			get { return source!=null ? source.volume : baseVolume; }
			set { baseVolume = localVolume * value; if(source!=null) source.volume = baseVolume; } }

		public PBGMPlayer() {}
		
		public PBGMPlayer( string bgmFileName ) {
			AudioClip clip = (AudioClip)Resources.Load( bgmFileName );
			if ( clip != null ) {
				obj = new GameObject("BGMPlayer");
				if(psoundObj) { obj.transform.parent = psoundObj.transform; }
				source = obj.AddComponent<AudioSource>();
				source.clip = clip;
				state = new Wait( this );
				Debug.Log( "load BGM " + bgmFileName);
			} else {
				Debug.LogWarning( "BGM " + bgmFileName + " is not found." );
			}
		}
		
		public void destory() {
			if ( source != null )
				GameObject.Destroy( obj );
		}
		
		public void playBGM() {
			if ( source != null ) {
				state.playBGM();
			}
		}
		
		public void playBGM( float fadeTime ) {
			if ( source != null ) {
				this.fadeInTime = fadeTime;
				state.playBGM();
			}
		}

		public void setLoopBGM(bool isLoop) {
			if ( source != null ) {
				source.loop = isLoop;
			}
		}
		
		public void pauseBGM() {
			if ( source != null ) {
				state.pauseBGM();
			}
		}

		public bool isPause() {
			return state is Pause;
		}
		
		public bool isPlaying() {
			if (source != null) {
				return source.isPlaying;//state is Playing;
			} else {
				return false;
			}
		}

		public void stopBGM( float fadeTime ) {
			if ( source != null ) {
				fadeOutTime = fadeTime;
				state.stopBGM();
			}
		}

		public void update() {
			if ( source != null ) {
				state.update();
			}
		}

		public bool hadFadeOut() {
			return isFinishFadeOut;
		}
	}
}