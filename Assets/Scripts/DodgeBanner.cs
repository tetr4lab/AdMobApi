using UnityEngine;
using GoogleMobileAds.Utility;


/// <summary>
/// バナー回避のためにサイズを調整する
///		制御対象のオブジェクトにアタッチする
///		オブジェクトは、アンカー(0, 0)-(1, 1)で配置し、バナーの配置に応じてピボットのYを0または1にする
///		つまり、ここではサイズを制御するのみで、避ける方向は制御しない
/// </summary>
public class DodgeBanner : MonoBehaviour {

	/// <summary>対象のシーン</summary>
	[SerializeField] private string scene = default;
	/// <summary>対象のユニット番号(生成順) (通常は0)</summary>
	[SerializeField] private int unit = default;

#if (UNITY_ANDROID || UNITY_IPHONE) && ALLOW_ADS

	/// <summary>初期化完了</summary>
	private bool inited = false;

	/// <summary>対象のバナー広告</summary>
	private AdMobApi ads => AdMobApi.GetSceneAds (scene, unit);

	/// <summary>バナー状態</summary>
	private bool state => AdMobApi.Allow && AdMobApi.GetActive (scene);

	/// <summary>直前のバナー状態</summary>
	private bool lastState;

	/// <summary>初期サイズ</summary>
	private Vector2 initialSize;

	/// <summary>サイズ調整対象 (初期sizeDelta (0, 0))</summary>
	private RectTransform rect;

	/// <summary>初期化</summary>
	private async void Start () {
		lastState = false;
		rect = transform as RectTransform;
		initialSize = rect.sizeDelta;
		await TaskEx.DelayUntil (() => ads != null); // バナーの初期化を待つ
		if (ads.Type != AdType.Banner) {
			throw new System.ArgumentOutOfRangeException ($"'{scene}' is not Banner.");
		}
		updateSize ();
		inited = true;
	}

	/// <summary>サイズ調整</summary>
	private void updateSize () {
		var size = initialSize;
		if (lastState = state) { // 回線接続がないとサイズが取得できないので、都度取得することが望ましい
			var lastSize = size;
			size.y -= ads.bannerSize.y / rect.lossyScale.y; // AdMobの申告サイズ (SmartBannerでは実際よりも大きい場合がある)
			Debug.Log ($"bannerSize {ads.bannerSize}, {lastSize} => {size}, {rect.sizeDelta}");
		}
		rect.sizeDelta = size;
	}

	/// <summary>駆動</summary>
	private void Update () {
		if (inited && lastState != state) {
			Debug.Log ($"DodgeBanner {lastState} => {state}");
			updateSize (); // 遅延して状態が変化した場合に応じる
		}
	}

#endif
}
