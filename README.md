uProcessing
======================

Processing for Unity (Processing like MonoBehaviour)


About
--------
UnityでProcessingっぽく書けるライブラリ（アセット）一式です。
現在移植してある関数は一部で、最適化などはされていません。
LowLevelAPIで描くのではなく、Hierarchyに普通にGameObjectを生成して使う構成なのが特徴です。  
~~Unity 4~~Unity 5を対象としています。

使い方は簡単、PGraphics（MonoBehaviour）またはuProcessingを継承したスクリプトをオブジェクトに貼り付け、setup()とdraw()関数をオーバーライドし、Processingに似た形でコードを書くだけです。Unityのコードもそのまま共存して使えます。言語はC#です。

![基本画面](http://dev.eyln.com/GitHub/uProcessing/screenshot.png "ScreenShot")

#### Primitives.cs
    
	using UnityEngine;
	using System.Collections;

	public class Primitives : PGraphics {

		protected override void setup() {
			size(512, 512, P3D);
			
			stroke(0, 128, 64);
			strokeWeight(20);
			fill(0, 255, 128);
			rect(20, 20, 300, 400);
			
			noStroke();
			fill(255);
			ellipse(350, 350, 300, 300);

			noLoop();
		}

		protected override void draw() {
		}

		protected override void onKeyTyped() {
			if(key == ESC) { loadScene("Menu"); }
		}
	}


Updates
--------

#### 2016.1.22

ひとまずUnity5向けにプロジェクトをコンバートし、動作が変わっていたところを修正しました。
その他、細かいところでちょっと便利になってる箇所も。


#### 2015.4.17

テキストやJsonの読み込み、保存、jsonにも対応したり、PShapeを使って星型のプリミティブを作ったりなどもできるようになりました。
その他、ボタン表示やサウンド再生、Tweenerによる補間機能なんかも入ってます。サンプルも増やしました。
いろいろ再設計したいところが多いものの、ひとまずアップ。Unity4.6でしか確認してません。


Samples
-------
Assets/uProcessing/Scenesに各種サンプルシーンが入っています。
Menuのシーンから各シーンを呼び出せて、各シーンでESCキーを押すと前のシーンに戻れます。

![サンプル画面](http://dev.eyln.com/GitHub/uProcessing/hello.png "hello")
![サンプル画面](http://dev.eyln.com/GitHub/uProcessing/menu.png "menu")
![サンプル画面](http://dev.eyln.com/GitHub/uProcessing/primitives.png "primitives")
![サンプル画面](http://dev.eyln.com/GitHub/uProcessing/images.png "images")
![サンプル画面](http://dev.eyln.com/GitHub/uProcessing/shapes.png "shapes")
![サンプル画面](http://dev.eyln.com/GitHub/uProcessing/dialog.png "dialog")
![サンプル画面](http://dev.eyln.com/GitHub/uProcessing/earth.png "earth")
![サンプル画面](http://dev.eyln.com/GitHub/uProcessing/performance.png "performance")
![サンプル画面](http://dev.eyln.com/GitHub/uProcessing/action.png "action")
![サンプル画面](http://dev.eyln.com/GitHub/uProcessing/jsondata.png "jsondata")
![サンプル画面](http://dev.eyln.com/GitHub/uProcessing/tweens.png "tweens")
![サンプル画面](http://dev.eyln.com/GitHub/uProcessing/sounds.png "sounds")


Extras
-------
#### 画面モード
size()で指定する画面モードはProcessing用の座標系であるP2D、P3Dの他、Unity用のU2D、U3Dを用意しています。P系はY軸とZ軸の向きがUnityと逆方向で、オブジェクトを内部的に縮小スケールしてシーンに配置しています（座標指定時の座標がピクセル単位なのでそのまま配置すると大きすぎるので）。

#### PGameObject
box()やrect()などもすべて個別のGameObjectとなっています。戻り値でPGameObject型のデータを取得できます。

    PGameObejct obj = box(100);

PGameObjectはMonoBehaviourを継承したコンポーネントなので、obj.gameObjectのような形で通常のゲームオブジェクトにもアクセスできます。

#### GameObjectのリサイクル
box()やrect()などもすべて個別のGameObjectとなっています。毎回消して生成するのが無駄だと感じる場合は、recycle()という関数を呼ぶことで、それ以降同じ順番で呼ばれたGameObjectを消さずに使いまわすようになります。
ただし、完全に呼び出し順依存なので、途中に一時的なGameObjectを生成する場合はnoRecycle()を直前に呼んで、GameObjectを使いまわさないようにしてください。

beginRecycle()〜endRecycle()やbeginNoRecycle()〜endRecycle()も使えます。なお、beginRecycle(番号)〜endRecycle()で囲ったものはその番号のグループとしてIDが割り振られ、clearRecycle(番号)で破棄できます。

#### GameObjectのキープ
recycle()の場合、GameObjectは消さずに使いまわすとはいえ、毎回draw()内で描画指示する必要があります。もっとUnity的に一度生成したら放っておいてもオブジェクトが描画されるような形で使いたい場合、keep()、noKeep()またはbeginKeep()〜endKeep()を使うと実現できます。

#### 拡張
その他、prefab()でPrefabから生成できたり、loadScene()でシーンをロードできたりといったUnity的な便利関数も一部追加しています。

PGraphicsのIsEnableMaterialPBをtrueにしておく（デフォルトでtrue）とマテリアルをインスタンスごとに複製せずに色を指定するようになります。

PGraphicsを継承したuProcessingクラスの方を使うと、playBGM()、PlaySE()でサウンドを鳴らしたり、tween()で数値、座標、色などを補間したり、button()やdialog()で簡易的なユーザーインターフェイスを表示したりもできます。

補間処理の例をあげるとこんな感じです。

	// int型の数値で255から0の値に1秒かけて補間する
	PTween t = tween(255, 0, 1.0f, PEase.InCubic);
	//int value = (int)t.Value;

	// メンバ変数のColor colを灰色から緑に0.5秒かけて補間して
	tween(this, "col", Color.gray, Color.green, 0.5f, PEase.OutQuad)
		.wait(1.0f) // 1秒待って
		.to(Color.red, 1.0f, PEase.Linear) // さらに赤に1秒で補間し
		.reverse()  // これまでの補間の流れを逆にして最初に戻る
		.loop();    // それを繰り返す

詳しくは各サンプルをご覧ください。サウンド用のPSoundや補間用のPTweenerはuProcessingとは独立して使うこともできます。


Attention
-----------
Processingにある関数でも、uProcessingには未定義のものや仕様が異なるものがあります。
テストなど不十分のα版ですので、ご注意ください。  
また今後予告なく仕様を変更することがあります。
 
 
License
----------
#### uProcessing

Copyright &copy; 2014 NISHIDA Ryota [http://dev.eyln.com][EYLN]
Distributed under the [ZLIB License][ZLIB].
 
[EYLN]: http://dev.eyln.com/
[ZLIB]: http://opensource.org/licenses/zlib

#### UnityChan
![UnityChanLicenseLogo](http://unity-chan.com/images/imageLicenseLogo.png "UnityChanLicenseLogo")

サンプルで使用しているUnityChanのデータ一式については別途[UnityChanのライセンス/利用規約][UnityChanLicense]に従う必要があります。

[UnityChanLicense]: http://unity-chan.com/download/guideline.html
