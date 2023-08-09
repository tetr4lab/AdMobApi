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

	/// <summary>�o�i�[�������p�l��</summary>
	[SerializeField] private DodgeBanner dodgeBannerPanel = default;

	// �L���V�[��
	public static readonly string AdSetBanner0 = "banner0";
	public static readonly string AdSetBanner1 = "banner1";
	public static readonly string AdSetBanner2 = "banner2";
	public static readonly string AdSetBanner3 = "banner3";
	public static readonly string AdSetInterstitial = "interstitial";
	public static readonly string AdSetRewarded = "rewarded";
	public static readonly string [] AdSetBanners = new [] { AdSetBanner0, AdSetBanner1, AdSetBanner2, AdSetBanner3, };

#if ALLOW_ADS
	/// <summary>�ݐϊl����</summary>
	private int coins = 0;

	/// <summary>������</summary>
	private async void Start () {
		AdMobApi.Allow = true;
		await TaskEx.DelayUntil (() => AdMobApi.Acceptable);
		Debug.Log ("App Inited");
		new AdMobApi (AdSetBanner0, true);
		new AdMobApi (AdSetBanner0, AdSize.IABBanner, AdPosition.Center);
		new AdMobApi (AdSetBanner1, AdSize.MediumRectangle, AdPosition.Bottom);
		new AdMobApi (AdSetBanner1, AdSize.IABBanner, AdPosition.Center);
		new AdMobApi (AdSetBanner2, AdSize.Banner, AdPosition.Bottom);
		new AdMobApi (AdSetBanner2, AdSize.IABBanner, AdPosition.Center);
		new AdMobApi (AdSetBanner3, new AdSize (320, 100), AdPosition.Bottom);
		new AdMobApi (AdSetBanner3, AdSize.IABBanner, AdPosition.Center);
		ChangeBanners ();
		AdMobApi.SetActive (AdSetBanner0);
		new AdMobApi (AdSetInterstitial);
		new AdMobApi (AdSetRewarded, OnAdRewarded);

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

	/// <summary>���݂̃o�i�[</summary>
	private string AdSetBanner => AdSetBanners [_bannerSet];

	/// <summary>�o�i�[�؂�ւ�</summary>
	private void ChangeBanners (bool swap = false) {
		var current = AdMobApi.GetActive (AdSetBanner);
		AdMobApi.SetActive (AdSetBanner, false);
		if (swap) {
			_bannerSet = (_bannerSet + 1) % AdSetBanners.Length;
		}
		Debug.Log ($"CreateBanners {AdSetBanner}");
		dodgeBannerPanel.SetTarget (AdSetBanner, 0);
		AdMobApi.SetActive (AdSetBanner, current);
	}
	private int _bannerSet;

	/// <summary>�l�����R�[���o�b�N</summary>
	private void OnAdRewarded (Reward reward) {
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
				var current = AdMobApi.GetActive (AdSetBanner);
				Debug.Log ($"{button.name} {AdSetBanner} {current} => {!current}");
				AdMobApi.SetActive (AdSetBanner, !current);
				break;
			case "SwapButton":
				ChangeBanners (true);
				Debug.Log ($"{button.name} {AdSetBanner}");
				var text = button.GetComponentInChildren<Text> ();
				if (text) { text.text = _bannerSet.ToString (); }
				break;
			case "InterstitialButton":
				Debug.Log ($"{button.name}");
				AdMobApi.SetActive (AdSetInterstitial);
				break;
			case "RewardedButton":
				Debug.Log ($"{button.name}");
				AdMobApi.SetActive (AdSetRewarded);
				break;
		}
#endif
	}

}
