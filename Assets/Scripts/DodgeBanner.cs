using UnityEngine;
using GoogleMobileAds.Utility;
using System.Collections;

/// <summary>
/// アンカーバナー回避のためにサイズを調整する
///		制御対象のオブジェクトにアタッチする
///		オブジェクトは、アンカー(0, 0)-(1, 1)で配置し、バナーの配置に応じてピボットのYを0または1にする
///		つまり、ここではサイズを制御するのみで、避ける方向は制御しない
/// </summary>
public class DodgeBanner : MonoBehaviour {

	/// <summary>対象のセット</summary>
	[SerializeField] private string adSet = default;

	/// <summary>対象のユニット番号(生成順) (通常は0)</summary>
	[SerializeField] private int unit = default;

	/// <summary>避け始める変位量 (超えるまで避けず、越えた分だけ避ける)</summary>
	[SerializeField] private float threshold = 0f;

#if (UNITY_ANDROID || UNITY_IPHONE) && ALLOW_ADS

	/// <summary>対象の外部設定</summary>
	public void SetTarget (string set, int unit) {
		adSet = set;
		this.unit = unit;
		targetAds = null;
		updateSize ();
	}

	/// <summary>初期化完了</summary>
	private bool inited = false;

	/// <summary>対象のバナー広告</summary>
	private AdMobApi targetAds {
		get => _targetAds ??= AdMobApi.GetSceneAds (adSet, unit);
		set => _targetAds = value;
	}
	private AdMobApi _targetAds;

	/// <summary>バナー状態</summary>
	private bool isActive => AdMobApi.Allow && AdMobApi.GetActive (adSet);

	/// <summary>初期サイズ</summary>
	private Vector2 initialSize;

	/// <summary>サイズ調整対象 (初期sizeDelta (0, 0))</summary>
	private RectTransform rect;

	/// <summary>初期化</summary>
	private IEnumerator Start () {
		rect = transform as RectTransform;
		initialSize = rect.sizeDelta;
		yield return new WaitUntil (() => targetAds != null); // バナーの初期化を待つ
		if (targetAds.Type != AdType.Banner) {
			throw new System.ArgumentOutOfRangeException ($"'{adSet}' is not Banner.");
		}
		inited = true;
	}

	/// <summary>サイズ調整</summary>
	private void updateSize () {
		var size = initialSize;
		if (isActive) { // 回線接続がないとサイズが取得できないので、都度取得することが望ましい
			var height = targetAds.BannerPixelSize.y / rect.lossyScale.y - threshold;
			Debug.Log ($"DodgeBanner {name} {height} {targetAds.BannerPixelSize.y} / {rect.lossyScale.y} - {threshold}");
			if (height > 0f) {
				size.y -= height;
			}
		}
		rect.sizeDelta = size;
	}

	/// <summary>駆動</summary>
	private void Update () {
		if (inited && targetAds?.Dirty == true) {
			updateSize (); // 遅延して状態が変化した場合に応じる
		}
	}

#endif
}
