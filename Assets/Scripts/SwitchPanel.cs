using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Tetr4lab.Utilities;
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

    /// <summary>プライバシーボタン</summary>
    [SerializeField] private Button PrivacyButton = default;

	// 広告シーン
	public static readonly string AdSetBanner0 = "banner0";
	public static readonly string AdSetBanner1 = "banner1";
	public static readonly string AdSetBanner2 = "banner2";
	public static readonly string AdSetBanner3 = "banner3";
	public static readonly string AdSetInterstitial = "interstitial";
	public static readonly string AdSetRewarded = "rewarded";
	public static readonly string [] AdSetBanners = new [] {
        AdSetBanner0,
        AdSetBanner1,
        //AdSetBanner2,
        //AdSetBanner3,
    };

#if ALLOW_ADS
	/// <summary>累積獲得数</summary>
	private int coins = 0;
	private int lastCoins;

    /// <summary>初期化</summary>
    private async void Start () {
#if UMP_ENABLED
        // 同意を待機
        await AdMobApi.UmpConsentRequestAsync ();
        // プライバシーボタンの活殺
        PrivacyButton.interactable = AdMobApi.UmpConsentRequired;
#endif
        // AdMobの初期化
        AdMobApi.Allow = true;
        await TaskEx.DelayUntil (() => AdMobApi.Acceptable);
        Debug.Log ("App Init");
        var banner0 = new AdMobApi (AdSetBanner0, true);
		new AdMobApi (AdSetBanner0, AdSize.IABBanner, AdPosition.Center);
        new AdMobApi (AdSetBanner1, AdSize.MediumRectangle, AdPosition.Bottom);
        new AdMobApi (AdSetBanner1, AdSize.IABBanner, AdPosition.Center);
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
		InfoPanel.text = $"{((adid == null) ? "No AdId" : $"AdId: {adid}")}\nDeviceId: {SystemInfo.deviceUniqueIdentifier}";
        // 追加のバナー
        if (AdSetBanners.Length > 2) {
            // 最初のバナーが読み込まれるまで待つ
            await TaskEx.DelayUntil (() => banner0.State >= AdMobApi.Status.LOADED);
            new AdMobApi (AdSetBanner2, AdSize.Banner, AdPosition.Bottom);
            new AdMobApi (AdSetBanner2, AdSize.IABBanner, AdPosition.Center);
            if (AdSetBanners.Length > 3) {
                new AdMobApi (AdSetBanner3, new AdSize (320, 100), AdPosition.Bottom);
                new AdMobApi (AdSetBanner3, AdSize.IABBanner, AdPosition.Center);
            }
        }
        Debug.Log ($"App Inited {InfoPanel.text}");
	}

	/// <summary>駆動</summary>
	private void Update () {
		if (lastCoins != coins) {
			CoinsText.text = coins.ToString ();
			lastCoins = coins;
		}
		if (DebugInfoPanel) {
			var isOnline = Application.internetReachability != NetworkReachability.NotReachable;
            var ads = new List<AdMobApi> {
                AdMobApi.GetSceneAds (AdSetRewarded, 0),
                AdMobApi.GetSceneAds (AdSetInterstitial, 0),
            };
            foreach (var ad in AdSetBanners) {
                ads.Add (AdMobApi.GetSceneAds (ad, 0));
                ads.Add (AdMobApi.GetSceneAds (ad, 1));
            }
			var isLoaded = true;
            foreach (var ad in ads) {
                if (ad != null && !ad.IsLoaded) {
                    isLoaded = false;
                    break;
                }
            }
            DebugInfoPanel.text = !AdMobApi.Acceptable ? "" : string.Join ("\n", new string [] {
#if UMP_ENABLED
                $"GDPR適用領域{(AdMobApi.UmpConsentRequired ? "内" : "外")}, {(AdMobApi.UmpConsented ? "確認済" : "未確認")}",
#endif
                $"<size=40>{(isOnline && isLoaded == false ? "<color=red>対応中</color>" : "<color=green>待機中</color>")}</size>",
                $"{(isOnline ? "<color=green>ONLINE</color>" : "<color=red>OFFLINE</color>")} {(AdMobApi.IsAnyLoadFailed ? $"<color=red>FailedToLoad {(AdMobApi.MostConsecutiveFailures > 1 ? $"{AdMobApi.MostConsecutiveFailures}times" : "onece")}</color>" : "")}",
            }) + "\n";
            foreach (var ad in ads) {
                if (ad != null) {
                    DebugInfoPanel.text += $"{ad.Scene}:{ad.Unit} {ad.State} {(ad.IsLoaded ? "isLoaded" : "<color=red>notLoaded</color>")}\n";
                }
            }
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
        public async void OnPushButton (Button button) {
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
			case "PrivacyButton":
				Debug.Log ($"{button.name} {button.interactable}");
#if UMP_ENABLED
                button.interactable = false;
                await AdMobApi.UmpPrivacyOptionsRequestAsync ();
                AdMobApi.ResetLoadFailures ();
                button.interactable = true;
#endif
                break;
		}
		if (BannerButtonLavel) { BannerButtonLavel.text = $"{(AdMobApi.GetActive (adSetBanner) ? "☑" : "☐")} Banner"; }
#endif
        }

    }
