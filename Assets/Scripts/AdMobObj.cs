﻿//	Copyright© tetr4lab.
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using UnityEngine;
#if ALLOW_ADS
using GoogleMobileAds.Api;
#endif

/// <summary>
/// AdMobを使うためのラッパー
/// 
/// 互換性
///		バナー、インタースティシャル、リワードビデオに対応
/// 準備
///		プロジェクトにGoogle Mobile Ads Unityパッケージ を導入する
///		Assets > Google Mobile Ads > Settings... でAppIDを設定する
///			テスト用AppID
///			Android: ca-app-pub-3940256099942544~3347511713
///			iOS:     ca-app-pub-3940256099942544~1458002511
///		Asset > External Dependency Manager > XXXXX Resolver > Resolve を実施する
///		このスクリプトを適当なシーンオブジェクトにアタッチする
///			複数アタッチしても最初に起動した一つだけが有効になる
///		AdMobコンソールで作った広告ID(AppID, UnitID)を設定する
///			設定しなければテストユニットになる
///		AdMobコンソールでテストデバイスを登録しておく
///	使い方
///		オプトインを経て AdMobApi.Allow を有効にすることで、モジュールが初期化される
///		AdMobApi.Init()を呼ぶ必要は無い、完了待ちは AdMobApi.Acceptable をチェックする
///		指定「シーン」の異なる広告は同時に表示されないように排他制御される
///		var instance = new AdMobApi (scene ...) で広告を作成
///		instance.ActiveSelf = active または SetActive (string scene) または SetActive (string scene, bool active) で表示を制御
/// </summary>
namespace GoogleMobileAds.Utility {

	/// <summary>広告コンポーネント</summary>
	public class AdMobObj : MonoBehaviour {

        #region static
		// シングルトンインスタンス
        private static AdMobObj instance;

		// 広告ユニットIDの取得
		public static AdUnitIDs AdUnitId => (instance?.adUnitId == null || instance.adUnitId.Count < 1) ? defaultAdUnitId : instance.adUnitId;

		// テスト用の広告ユニットID
		// ref: https://developers.google.com/admob/unity/test-ads
		private static readonly AdUnitIDs defaultAdUnitId = new AdUnitIDs {
#if UNITY_ANDROID
			{ AdType.AppOpen,               "ca-app-pub-3940256099942544/3419835294" },
			{ AdType.Banner,                "ca-app-pub-3940256099942544/6300978111" },
			{ AdType.Interstitial,          "ca-app-pub-3940256099942544/1033173712" },
			{ AdType.Rewarded,              "ca-app-pub-3940256099942544/5224354917" },
			{ AdType.RewardedInterstitial,  "ca-app-pub-3940256099942544/5354046379" },
			{ AdType.Native,                "ca-app-pub-3940256099942544/2247696110" },
#elif UNITY_IOS
			{ AdType.AppOpen,				"ca-app-pub-3940256099942544/5662855259" },
			{ AdType.Banner,				"ca-app-pub-3940256099942544/2934735716" },
			{ AdType.Interstitial,			"ca-app-pub-3940256099942544/4411468910" },
			{ AdType.Rewarded,				"ca-app-pub-3940256099942544/1712485313" },
			{ AdType.RewardedInterstitial,	"ca-app-pub-3940256099942544/6978759866" },
			{ AdType.Native,				"ca-app-pub-3940256099942544/3986624511" },
#endif
		};
		#endregion static

		/// <summary>インスペクタで定義可能な広告ユニットID</summary>
		[SerializeField] private AdUnitIDs adUnitId = default;

#if ALLOW_ADS
		/// <summary>再接続時の再構築開始までの遅延時間(ms)</summary>
		private const int RemakeDelayTime = 500;

		/// <summary>オンライン判定 (実際の使用可能性は判定しない)</summary>
		private bool isOnLine => (Application.internetReachability != NetworkReachability.NotReachable);
		private bool lastOnline;

		/// <summary>再入抑制用 (初期化完了として使うべからず)</summary>
		private bool initialized;

		/// <summary>画面の向き</summary>
		private DeviceOrientation lastDeviceOrientation;

		// シングルトン初期化
        private void Start () {
            if (instance == null) {
				instance = this;
            }
        }

        /// <summary>駆動</summary>
        private async void Update () {
			if (instance != this) { return; }
			if (!initialized && AdMobApi.Allow) {
				// 初期化
				initialized = true;
				await AdMobApi.Init ();
				lastOnline = isOnLine;
				Debug.Log ($"Set {(lastOnline ? "On" : "Off")}Line");
			}
			if (AdMobApi.Acceptable) {
				// 新しい接続を検出
				if (lastOnline != isOnLine) {
					Debug.Log ($"Change {(lastOnline ? "On => Off" : "Off => On")}Line");
					if (lastOnline = isOnLine) {
						await TaskEx.DelayWhile (() => isOnLine, RemakeDelayTime); // 時間までオンラインが継続するなら
						if (isOnLine) {
							AdMobApi.ReMake (keepBannerState: true);
						}
					}
				}
				// 画面の向きが変化したらバナーを再生成
				if (lastDeviceOrientation != Input.deviceOrientation) {
					Debug.Log ($"Change {lastDeviceOrientation} => {Input.deviceOrientation}");
					AdMobApi.ReMake (type: AdType.Banner, keepBannerState: true);
					lastDeviceOrientation = Input.deviceOrientation;
                }
#if ((UNITY_ANDROID || UNITY_IOS) && UNITY_EDITOR)
				// エディタ専用の駆動
				AdMobApi.Update ();
#endif
			}
		}
#endif

	}

#if ALLOW_ADS

	/// <summary>広告タイプ</summary>
	public enum AdType {
		None = 0,
		AppOpen,
		Banner,
		Interstitial,
		Rewarded,
		RewardedInterstitial,
		Native,
	}

	// Dictionary<AdType,string> で済むものを、インスペクタから編集するための回避策
	#region AdUnitID

	/// <summary>広告ユニットID</summary>
	[Serializable] public class AdUnitID {

		/// <summary>種類</summary>
		[SerializeField] public AdType type;

		/// <summary>識別子</summary>
		[SerializeField] public string id;

		/// <summary>コンストラクタ</summary>
		public AdUnitID (AdType type, string id) {
			this.type = type;
			this.id = id;
		}

	}

	/// <summary>
	/// 広告ユニットID一覧
	/// </summary>
	[Serializable] public class AdUnitIDs : IEnumerable {

		/// <summary>一覧</summary>
		[SerializeField] private AdUnitID [] list;

		/// <summary>登録数</summary>
		public int Count => list?.Length ?? 0;

		/// <summary>種別からIDを得る</summary>
		public string this [AdType type] => Array.Find (list, u => u.type == type).id;

		/// <summary>一覧に追加</summary>
		public void Add (AdType type, string id) {
			var tempList = (list != null) ? new List<AdUnitID> (list) : new List<AdUnitID> { };
			tempList.Add (new AdUnitID (type, id));
			list = tempList.ToArray ();
		}

		/// <summary>コレクション初期化子を使用可能にするための空実装</summary>
        public IEnumerator GetEnumerator () => throw new NotImplementedException ();

    }
	#endregion AdUnitID

#endif
}
