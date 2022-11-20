using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ALLOW_ADS
using GoogleMobileAds.Api;
using GoogleMobileAds.Utility;
#endif

/// <summary>
/// �T���v�����C��
/// </summary>
public class SwitchPanel : MonoBehaviour {

    /// <summary>�ݐϊl�����\����</summary>
    [SerializeField] private Text CoinsText = default;

    /// <summary>���\����</summary>
    [SerializeField] private Text InfoPanel = default;

    // �L���V�[��
    public static readonly string AdSceneBanner = "banner";
    public static readonly string AdSceneInterstitial = "interstitial";
    public static readonly string AdSceneRewarded = "rewarded";

#if ALLOW_ADS
    /// <summary>�ݐϊl����</summary>
    private int coins = 0;

    /// <summary>������</summary>
    private async void Start () {
        AdMobApi.Allow = true;
        await TaskEx.DelayUntil (() => AdMobApi.Acceptable);
        Debug.Log ("App Inited");
        new AdMobApi (AdSceneBanner, AdSize.SmartBanner, AdPosition.Bottom);
        new AdMobApi (AdSceneBanner, AdSize.MediumRectangle, AdPosition.Center);
        AdMobApi.SetActive (AdSceneBanner);
        new AdMobApi (AdSceneInterstitial);
        new AdMobApi (AdSceneRewarded, OnAdRewarded);

        // AdId�̎擾
        string adid = null;
        if (Application.platform == RuntimePlatform.Android) {
            // Application.RequestAdvertisingIdentifierAsync()��Android��Ή��ɂȂ����̂�
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
    }

    /// <summary>�l�����R�[���o�b�N</summary>
    private void OnAdRewarded (object sender, Reward reward) {
        Debug.Log ($"Got {(int) reward.Amount} reward{((reward.Amount > 1)? "s" : "")}");
        coins += (int) reward.Amount;
        CoinsText.text = coins.ToString ();
    }
#endif

    /// <summary>�{�^���������ꂽ</summary>
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
