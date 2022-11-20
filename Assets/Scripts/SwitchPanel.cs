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

    // 広告シーン
    public static readonly string AdSceneBanner = "banner";
    public static readonly string AdSceneInterstitial = "interstitial";
    public static readonly string AdSceneRewarded = "rewarded";

#if ALLOW_ADS
    /// <summary>累積獲得数</summary>
    private int coins = 0;

    /// <summary>初期化</summary>
    private async void Start () {
        AdMobApi.Allow = true;
        await TaskEx.DelayUntil (() => AdMobApi.Acceptable);
        Debug.Log ("App Inited");
        new AdMobApi (AdSceneBanner, AdSize.SmartBanner, AdPosition.Bottom);
        new AdMobApi (AdSceneBanner, AdSize.MediumRectangle, AdPosition.Center);
        AdMobApi.SetActive (AdSceneBanner);
        new AdMobApi (AdSceneInterstitial);
        new AdMobApi (AdSceneRewarded, OnAdRewarded);

        var adid = GetAdvertisingIdentifier ();
        InfoPanel.text = (adid == null) ? "No AdId" : $"AdId: {adid}";

        // AdIdの取得 (Application.RequestAdvertisingIdentifierAsync()がAndroid非対応になったので)
        string GetAdvertisingIdentifier () {
            string advertisingID = null;
            if (Application.platform == RuntimePlatform.Android) {
                try {
                    using (var player = new AndroidJavaClass ("com.unity3d.player.UnityPlayer"))
                    using (var currentActivity = player.GetStatic<AndroidJavaObject> ("currentActivity"))
                    using (var client = new AndroidJavaClass ("com.google.android.gms.ads.identifier.AdvertisingIdClient"))
                    using (var adInfo = client.CallStatic<AndroidJavaObject> ("getAdvertisingIdInfo", currentActivity)) {
                        advertisingID = adInfo.Call<string> ("getId").ToString ();
                    }
                }
                catch (System.Exception e) {
                    Debug.LogError (e);
                }
            }
            return advertisingID;
        }
    }

    /// <summary>獲得時コールバック</summary>
    private void OnAdRewarded (object sender, Reward reward) {
        Debug.Log ($"Got {(int) reward.Amount} reward{((reward.Amount > 1)? "s" : "")}");
        coins += (int) reward.Amount;
        CoinsText.text = coins.ToString ();
    }
#endif

    /// <summary>ボタンが押された</summary>
    public void OnPushButton (Button button) {
#if ALLOW_ADS
        switch (button.name) {
            case "BannerButton":
                var current = AdMobApi.GetActive (AdSceneBanner);
                Debug.Log ($"{button.name} {current} => {!current}");
                AdMobApi.SetActive (AdSceneBanner, !current);
                break;
            case "InterstitialButton":
                Debug.Log ($"{button.name}");
                AdMobApi.SetActive (AdSceneInterstitial);
                break;
            case "RewardedButton":
                Debug.Log ($"{button.name}");
                AdMobApi.SetActive (AdSceneRewarded);
                break;
        }
#endif
    }

}
