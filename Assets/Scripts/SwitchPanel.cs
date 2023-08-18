using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ALLOW_ADS
using GoogleMobileAds.Api;
using GoogleMobileAds.Utility;
#endif

/// <summary>
/// サンプルメイン
/// </summary>
public class SwitchPanel : MonoBehaviour {

	/// <summary>累積獲得数表示体</summary>
	[SerializeField] private Text CoinsText = default;

	/// <summary>情報表示体</summary>
	[SerializeField] private Text InfoPanel = default;

	/// <summary>バナーを避けるパネル</summary>
	[SerializeField] private DodgeBanner dodgeBannerPanel = default;

	/// <summary>セット番号表示体</summary>
	[SerializeField] private Text SetNumberDisplay = default;

	/// <summary>バナーボタンラベル</summary>
	[SerializeField] private Text BannerButtonLavel = default;

	// 広告シーン
	public static readonly string AdSetBanner0 = "banner0";
	public static readonly string AdSetBanner1 = "banner1";
	public static readonly string AdSetBanner2 = "banner2";
	public static readonly string AdSetBanner3 = "banner3";
	public static readonly string AdSetInterstitial = "interstitial";
	public static readonly string AdSetRewarded = "rewarded";
	public static readonly string [] AdSetBanners = new [] { AdSetBanner0, AdSetBanner1, AdSetBanner2, AdSetBanner3, };

#if ALLOW_ADS
	/// <summary>累積獲得数</summary>
	private int coins = 0;
	private int lastCoins;

	/// <summary>初期化</summary>
	private IEnumerator Start () {
		AdMobApi.Allow = true;
		yield return new WaitUntil (() => AdMobApi.Acceptable);
		Debug.Log ("App Init");
		new AdMobApi (AdSetBanner0, true);
		new AdMobApi (AdSetBanner0, AdSize.IABBanner, AdPosition.Center);
		new AdMobApi (AdSetBanner1, AdSize.MediumRectangle, AdPosition.Bottom);
		new AdMobApi (AdSetBanner1, AdSize.IABBanner, AdPosition.Center);
		new AdMobApi (AdSetBanner2, AdSize.Banner, AdPosition.Bottom);
		new AdMobApi (AdSetBanner2, AdSize.IABBanner, AdPosition.Center);
		new AdMobApi (AdSetBanner3, new AdSize (320, 100), AdPosition.Bottom);
		new AdMobApi (AdSetBanner3, AdSize.IABBanner, AdPosition.Center);
		dodgeBannerPanel.SetTarget (AdSetBanner0, 0);
		AdMobApi.SetActive (AdSetBanner0);
		new AdMobApi (AdSetInterstitial);
		new AdMobApi (AdSetRewarded, OnAdRewarded);
		// AdIdの取得
		string adid = null;
		if (Application.platform == RuntimePlatform.Android) {
			// Application.RequestAdvertisingIdentifierAsync()がAndroid非対応になったので
			try {
				using (var player = new AndroidJavaClass ("com.unity3d.player.UnityPlayer"))
				using (var currentActivity = player.GetStatic<AndroidJavaObject> ("currentActivity"))
				using (var client = new AndroidJavaClass ("com.google.android.gms.ads.identifier.AdvertisingIdClient"))
				using (var adInfo = client.CallStatic<AndroidJavaObject> ("getAdvertisingIdInfo", currentActivity)) {
					adid = adInfo.Call<string> ("getId").ToString ();
				}
			}
			catch (System.Exception e) {
				Debug.LogError (e);
			}
		} else {
			if (Application.RequestAdvertisingIdentifierAsync (
				(string advertisingId, bool trackingEnabled, string error) => {
					InfoPanel.text = $"AdId: {advertisingId} (tracking={trackingEnabled}, error={error})";
				}
			)) {
				adid = "wait...";
			}
		}
		InfoPanel.text = (adid == null) ? "No AdId" : $"AdId: {adid}";
		Debug.Log ("App Inited");
	}

	/// <summary>駆動</summary>
	private void Update () {
		if (lastCoins != coins) {
			CoinsText.text = coins.ToString ();
			lastCoins = coins;
		}
	}

	/// <summary>現在のバナー</summary>
	private string adSetBanner => AdSetBanners [currentBannerSet];

	/// <summary>現在のバナー</summary>
	private int currentBannerSet;

	/// <summary>獲得時コールバック</summary>
	private void OnAdRewarded (Reward reward) {
		Debug.Log ($"Got {(int) reward.Amount} reward{((reward.Amount > 1)? "s" : "")}");
		coins += (int) reward.Amount;
	}
#endif

	/// <summary>ボタンが押された</summary>
	public void OnPushButton (Button button) {
#if ALLOW_ADS
		var current = AdMobApi.GetActive (adSetBanner);
		switch (button.name) {
			case "BannerButton":
				Debug.Log ($"{button.name} {adSetBanner} {current} => {!current}");
				AdMobApi.SetActive (adSetBanner, !current);
				break;
			case "SwapButton":
				AdMobApi.SetActive (adSetBanner, false);
				currentBannerSet = (currentBannerSet + 1) % AdSetBanners.Length;
				dodgeBannerPanel.SetTarget (adSetBanner, 0);
				AdMobApi.SetActive (adSetBanner, current);
				Debug.Log ($"{button.name} {adSetBanner}");
				if (SetNumberDisplay) { SetNumberDisplay.text = currentBannerSet.ToString (); }
				break;
			case "InterstitialButton":
				Debug.Log ($"{button.name}");
				AdMobApi.SetActive (AdSetInterstitial);
				break;
			case "RewardedButton":
				Debug.Log ($"{button.name}");
				AdMobApi.SetActive (AdSetRewarded);
				break;
			case "InfoPanel":
				Debug.Log ($"{button.name} {InfoPanel.text}");
				GUIUtility.systemCopyBuffer = InfoPanel.text;
				break;
		}
		if (BannerButtonLavel) { BannerButtonLavel.text = $"{(AdMobApi.GetActive (adSetBanner) ? "☑" : "☐")} Banner"; }
#endif
	}

}
