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

	/// <summary>デバッグ情報表示体</summary>
	[SerializeField] private Text DebugInfoPanel = default;

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
#if (UNITY_ANDROID || UNITY_IPHONE) && ALLOW_ADS
		dodgeBannerPanel.SetTarget (AdSetBanner0, 0);
#endif
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
		if (DebugInfoPanel) {
			var isOnline = Application.internetReachability != NetworkReachability.NotReachable;
			var ads = new [] {
				AdMobApi.GetSceneAds (AdSetRewarded, 0),
				AdMobApi.GetSceneAds (AdSetInterstitial, 0),
				AdMobApi.GetSceneAds (AdSetBanner0, 0),
				AdMobApi.GetSceneAds (AdSetBanner0, 1),
				AdMobApi.GetSceneAds (AdSetBanner1, 0),
				AdMobApi.GetSceneAds (AdSetBanner1, 1),
				AdMobApi.GetSceneAds (AdSetBanner2, 0),
				AdMobApi.GetSceneAds (AdSetBanner2, 1),
				AdMobApi.GetSceneAds (AdSetBanner3, 0),
				AdMobApi.GetSceneAds (AdSetBanner3, 1),
			};
			var isLoaded =
				(ads [0] == null || ads [0].IsLoaded) && 
				(ads [1] == null || ads [1].IsLoaded) && 
				(ads [2] == null || ads [2].IsLoaded) && 
				(ads [3] == null || ads [3].IsLoaded) && 
				(ads [4] == null || ads [4].IsLoaded) && 
				(ads [5] == null || ads [5].IsLoaded) && 
				(ads [6] == null || ads [6].IsLoaded) && 
				(ads [7] == null || ads [7].IsLoaded) && 
				(ads [8] == null || ads [8].IsLoaded) &&
				(ads [9] == null || ads [9].IsLoaded);
			DebugInfoPanel.text = !AdMobApi.Acceptable ? "" : string.Join ("\n", new string [] {
				$"<size=40>{(isOnline && isLoaded == false ? "<color=red>対応中</color>" : "<color=green>待機中</color>")}</size>",
				$"{(isOnline ? "<color=green>ONLINE</color>" : "<color=red>OFFLINE</color>")} {(AdMobApi.FailedToLoad ? "<color=red>FailedToLoad</color>" : "")}",
				ads [0] == null ? "" : $"{ads [0].Scene}:{ads [0].Unit} {ads [0].State} {(ads [0].IsLoaded ? "IsLoaded" : "<color=red>!IsLoaded</color>")}",
				ads [1] == null ? "" : $"{ads [1].Scene}:{ads [1].Unit} {ads [1].State} {(ads [1].IsLoaded ? "IsLoaded" : "<color=red>!IsLoaded</color>")}",
				ads [2] == null ? "" : $"{ads [2].Scene}:{ads [2].Unit} {ads [2].State} {(ads [2].IsLoaded ? "IsLoaded" : "<color=red>!IsLoaded</color>")}",
				ads [3] == null ? "" : $"{ads [3].Scene}:{ads [3].Unit} {ads [3].State} {(ads [3].IsLoaded ? "IsLoaded" : "<color=red>!IsLoaded</color>")}",
				ads [4] == null ? "" : $"{ads [4].Scene}:{ads [4].Unit} {ads [4].State} {(ads [4].IsLoaded ? "IsLoaded" : "<color=red>!IsLoaded</color>")}",
				ads [5] == null ? "" : $"{ads [5].Scene}:{ads [5].Unit} {ads [5].State} {(ads [5].IsLoaded ? "IsLoaded" : "<color=red>!IsLoaded</color>")}",
				ads [6] == null ? "" : $"{ads [6].Scene}:{ads [6].Unit} {ads [6].State} {(ads [6].IsLoaded ? "IsLoaded" : "<color=red>!IsLoaded</color>")}",
				ads [7] == null ? "" : $"{ads [7].Scene}:{ads [7].Unit} {ads [7].State} {(ads [7].IsLoaded ? "IsLoaded" : "<color=red>!IsLoaded</color>")}",
				ads [8] == null ? "" : $"{ads [8].Scene}:{ads [8].Unit} {ads [8].State} {(ads [8].IsLoaded ? "IsLoaded" : "<color=red>!IsLoaded</color>")}",
				ads [9] == null ? "" : $"{ads [9].Scene}:{ads [9].Unit} {ads [9].State} {(ads [9].IsLoaded ? "IsLoaded" : "<color=red>!IsLoaded</color>")}",
			});
        }
	}

	/// <summary>現在のバナー</summary>
	private string adSetBanner => AdSetBanners [currentBannerSet];

	/// <summary>現在のバナー</summary>
	private int currentBannerSet;

	/// <summary>獲得時コールバック</summary>
	private void OnAdRewarded (Reward reward) {
		Debug.Log ($"Got {(int) reward.Amount} {reward.Type}{(reward.Amount > 1 ? "s" : "")}");
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
#if (UNITY_ANDROID || UNITY_IPHONE) && ALLOW_ADS
				dodgeBannerPanel.SetTarget (adSetBanner, 0);
#endif
				AdMobApi.SetActive (adSetBanner, current);
				Debug.Log ($"{button.name} {adSetBanner}");
				if (SetNumberDisplay) { SetNumberDisplay.text = currentBannerSet.ToString (); }
				break;
			case "InterstitialButton":
				Debug.Log ($"{button.name}");
				AdMobApi.SetActive (AdSetInterstitial);
				AdMobApi.SetActive (adSetBanner, current);
				break;
			case "RewardedButton":
				Debug.Log ($"{button.name}");
				AdMobApi.SetActive (AdSetRewarded);
				AdMobApi.SetActive (adSetBanner, current);
				break;
			case "InfoPanel":
				Debug.Log ($"{button.name} {InfoPanel.text}");
				GUIUtility.systemCopyBuffer = InfoPanel.text;
				break;
			case "SpecialButton":
				Debug.Log ($"{button.name} {Screen.fullScreen}");
				Screen.fullScreen = !Screen.fullScreen;
				break;
		}
		if (BannerButtonLavel) { BannerButtonLavel.text = $"{(AdMobApi.GetActive (adSetBanner) ? "☑" : "☐")} Banner"; }
#endif
        }

    }
