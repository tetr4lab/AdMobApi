//	Copyright© tetr4lab.
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

	/// <summary>論理広告</summary>
	public class AdMobApi {

		/// <summary>状態/要求</summary>
		private enum Status {
			NONE = 0,
			LOADING,
			LOADED,
			SHOWN,
			HIDDEN,
			DELETED,
		}

		#region static

		// クラス要素
		/// <summary>初期化トリガー</summary>
		public static bool Allow {
			get => allow;
			set {
				allow = value;
				Debug.Log ($"AdMobApi Triggered {allow}");
				if (!allow) { // 全停止
					foreach (var ad in adsList) {
						ad.Remove ();
					}
				}
			}
		}
		private static bool allow;
		/// <summary>初期化完了</summary>
		public static bool Acceptable;
		/// <summary>生成された広告一覧</summary>
		private static List<AdMobApi> adsList;
		/// <summary>再入抑制用 (初期化完了に使うべからず)</summary>
		private static bool initialized;

		/// <summary>指定シーンの広告一覧を得る</summary>
		public static List<AdMobApi> GetSceneAds (string scene) => adsList?.FindAll (ad => ad.Scene == scene) ?? new List<AdMobApi> { };

		/// <summary>指定のシーンと番号の広告を得る</summary>
		public static AdMobApi GetSceneAds (string scene, int unit) => adsList?.Find (ad => ad.Scene == scene && ad.Unit == unit);

		/// <summary>クラス初期化</summary>
		public static async Task Init () {
			if (Allow && !initialized) {
				Debug.Log ("AdMobApi Init");
				initialized = true;
				adsList = new List<AdMobApi> { };
				MobileAds.Initialize (status => { Acceptable = true; }); // Initialize the Google Mobile Ads SDK.
				await TaskEx.DelayUntil (() => Acceptable);
				Debug.Log ($"AdMobApi Inited {Acceptable}");
			}
		}

		/// <summary>全体の活殺制御</summary>
		public static void SetActive (bool active) {
			SetActive (null, active);
		}

		/// <summary>活殺制御</summary>
		public static void SetActive (string scene, bool active = true) {
			if (Allow && Acceptable) {
				Debug.Log ($"Ad SetActive {scene} {active}");
				foreach (var ad in adsList) {
					if (scene == null || ad.Scene == scene) {
						ad.ActiveSelf = active;
					} else if (active) { // 活性時排他制御
						ad.ActiveSelf = false;
					}
				}
			}
		}

		/// <summary>活殺取得</summary>
		public static bool GetActive (string scene) {
			if (Allow && Acceptable) {
				//Debug.Log ($"Ad GetActive {scene}");
				foreach (var ad in adsList) {
					if (ad.Scene == scene) {
						return ad.ActiveSelf;
					}
				}
			}
			return false;
		}

		/// <summary>回線接続による再構築</summary>
		public static void ReMake (string scene = null, AdType type = AdType.None, bool keepBannerState = false) {
			if (Allow && Acceptable) {
				Debug.Log ($"Ad ReMake {scene} [{adsList.Count}], {type}, {keepBannerState}");
				foreach (var ad in adsList) {
					if ((scene == null || ad.Scene == scene) && (type == AdType.None || ad.type == type)) {
						ad.Create (keepBannerState);
					}
				}
			}
		}

#if ((UNITY_ANDROID || UNITY_IOS) && UNITY_EDITOR)
		/// <summary>エディタ専用のリワード付与</summary>
		public static Action onAdRewardedForEditor;

		/// <summary>エディタ専用の駆動</summary>
		public static void Update () {
			if (Allow && Acceptable) {
				foreach (var ad in adsList) {
					if (ad.valid && ad.request == Status.SHOWN && (ad.type == AdType.Interstitial || ad.type == AdType.Rewarded)) {
						Debug.Log ($"Ad Video {ad.type} Shown");
						if (ad.type == AdType.Rewarded && onAdRewardedForEditor != null) {
							onAdRewardedForEditor (); // リワードの付与
						}
						ad.Hide (); // 動画広告の終了
					}
				}
			}
		}
#endif

#endregion

		// メンバー要素

		/// <summary>シーン名</summary>
		public string Scene { get; protected set; }

		/// <summary>ユニット番号</summary>
		public int Unit { get; protected set; } = 0;

		/// <summary>タイプ (外部アクセス用)</summary>
		public AdType Type => type;

		private BannerView bannerView; // バナー広告
		private InterstitialAd interstitial; // インタースティシャル広告
		private RewardedAd rewardedVideo; // 動画リワード広告
		private EventHandler<Reward> onAdRewarded;	 // 動画リワード報酬時処理
		private AdType type; // タイプ
		private Status state; // 状態
		private Status request; // 要求
		private AdSize adSize; // バナーサイズ
		private AdPosition adPosition; // バナー位置
		private bool valid => state != Status.DELETED; // 有効
		
		/// <summary>バナーのピクセルサイズ (SmartBannerでは実際よりも大きい場合がある)</summary>
		public Vector2 bannerSize => (valid && bannerView != null) ? new Vector2 (bannerView.GetWidthInPixels (), bannerView.GetHeightInPixels ()) : Vector2.zero;
				
		/// <summary>表示制御 (要求レベル)</summary>
		public bool ActiveSelf {
			get => valid && request == Status.SHOWN && (
				(type == AdType.Rewarded && rewardedVideo != null)
				|| (type == AdType.Interstitial && interstitial != null)
				|| (type == AdType.Banner && bannerView != null)
			);
			set { if (valid) { if (value) { Show (); } else { Hide (); } } }
		}

		/// <summary>ロード済み</summary>
		public bool isLoaded => valid && ((interstitial != null && interstitial.IsLoaded ()) || (rewardedVideo != null && rewardedVideo.IsLoaded ()));

		/// <summary>コンストラクタ インタースティシャル</summary>
		public AdMobApi (string scene) {
			Debug.Log ($"AdMobApi {scene}");
			if (GetSceneAds (scene)?.Count > 0) {
				throw new ArgumentException ($"Duplicate scene {scene}");
            }
			type = AdType.Interstitial;
			Scene = scene;
			Create ();
			adsList.Add (this);
		}

		/// <summary>コンストラクタ バナー</summary>
		public AdMobApi (string scene, AdSize size, AdPosition pos) {
			Debug.Log ($"AdMobApi {scene} {size} {pos}");
			var ads = GetSceneAds (scene);
			foreach (var ad in ads) {
				if (ad.type != AdType.Banner) {
					throw new ArgumentException ($"Duplicate scene {scene}");
				}
            }
			adSize = size;
			adPosition = pos;
			type = AdType.Banner;
			Scene = scene;
			Unit = ads.Count;
			Create ();
			adsList.Add (this);
		}

		/// <summary>コンストラクタ リワード</summary>
		public AdMobApi (string scene, EventHandler<Reward> OnAdRewarded) {
			Debug.Log ($"AdMobApi {scene} {OnAdRewarded}");
			if (GetSceneAds (scene)?.Count > 0) {
				throw new ArgumentException ($"Duplicate scene {scene}");
			}
			onAdRewarded = OnAdRewarded;
			type = AdType.Rewarded;
			Scene = scene;
			Create ();
			adsList.Add (this);
		}

		/// <summary>生成</summary>
		/// <param name="keepBannerState">再生成時にバナーの表示を維持</param>
		private void Create (bool keepBannerState = false) {
			if (!valid) { return; }
			Remove (keepBannerState && type == AdType.Banner);
			Debug.Log ($"Ad Create {Scene} {type}, keep={keepBannerState}, stat={state}, request={request}");
#if (UNITY_ANDROID || UNITY_IOS)
			if (type == AdType.Rewarded) {
				if ((rewardedVideo = new RewardedAd (AdMobObj.AdUnitId [type])) != null) {
					rewardedVideo.OnAdLoaded += HandleAdLoaded;
					rewardedVideo.OnAdFailedToLoad += HandleAdFailedToLoad;
					rewardedVideo.OnAdOpening += HandleAdOpened;
					rewardedVideo.OnAdClosed += HandleAdClosed;
					rewardedVideo.OnUserEarnedReward += HandleAdRewarded;
					if (onAdRewarded != null) {
						rewardedVideo.OnUserEarnedReward += onAdRewarded;
					}
				}
			} else if (type == AdType.Interstitial) {
				if ((interstitial = new InterstitialAd (AdMobObj.AdUnitId [type])) != null) {
					interstitial.OnAdLoaded += HandleAdLoaded;
					interstitial.OnAdFailedToLoad += HandleAdFailedToLoad;
					interstitial.OnAdOpening += HandleAdOpened;
					interstitial.OnAdClosed += HandleAdClosed;
				}
			} else if (type == AdType.Banner) {
				if ((bannerView = new BannerView (AdMobObj.AdUnitId [AdType.Banner], adSize, adPosition)) != null) {
					bannerView.OnAdLoaded += HandleAdLoaded;
					bannerView.OnAdFailedToLoad += HandleAdFailedToLoad;
					bannerView.OnAdOpening += HandleAdOpened;
					bannerView.OnAdClosed += HandleAdClosed;
				}
			}
#endif
			Load ();
		}

		/// <summary>除去</summary>
		/// <param name="keepShown">表示状態を要求として維持</param>
		private void Remove (bool keepShown = false) {
			if (!valid) { return; }
			Debug.Log ($"Ad Remove {type} {keepShown}");
#if (UNITY_ANDROID || UNITY_IOS)
			if (bannerView != null) {
				bannerView.Destroy ();
				bannerView = null;
			}
			if (interstitial != null) {
				interstitial.Destroy ();
				interstitial = null;
			}
			if (rewardedVideo != null) {
				rewardedVideo.OnAdLoaded -= HandleAdLoaded;
				rewardedVideo.OnAdFailedToLoad -= HandleAdFailedToLoad;
				rewardedVideo.OnAdOpening -= HandleAdOpened;
				rewardedVideo.OnAdClosed -= HandleAdClosed;
				rewardedVideo.OnUserEarnedReward -= HandleAdRewarded;
				if (onAdRewarded != null) {
					rewardedVideo.OnUserEarnedReward -= onAdRewarded;
				}
				rewardedVideo = null;
			}
#endif
			if (keepShown && state == Status.SHOWN) {
				request = Status.SHOWN;
			}
			state = Status.NONE;
		}

		/// <summary>読込</summary>
		private void Load () {
			if (!valid) { return; }
			Debug.Log ($"Ad Load {Scene} {type}");
#if (UNITY_ANDROID || UNITY_IOS)
			if (rewardedVideo == null && interstitial == null && bannerView == null) {
				throw new ArgumentNullException ("no ad instance");
			}
#endif
			if (state != Status.LOADING && state != Status.LOADED) {
#if (UNITY_ANDROID || UNITY_IOS)
				AdRequest request = new AdRequest.Builder ().Build ();  // Create an empty ad request.
				if (rewardedVideo != null) {
					rewardedVideo.LoadAd (request);
				} else if (interstitial != null) {
					interstitial.LoadAd (request);
				} else if (bannerView != null) {
					bannerView.LoadAd (request);
					bannerView.Hide ();
				}
#endif
				state = Status.LOADING;
			}
		}

		/// <summary>表示</summary>
		private void Show () {
			if (!valid) { return; }
			Debug.Log ($"Ad Show {Scene} {type}");
#if (UNITY_ANDROID || UNITY_IOS)
			if (rewardedVideo != null && rewardedVideo.IsLoaded ()) {
				rewardedVideo.Show ();
			} else if (interstitial != null && interstitial.IsLoaded ()) {
				interstitial.Show ();
			} else if (bannerView != null && state == Status.LOADED) { // バナーには、IsLoadedがないため
				bannerView.Show ();
				state = Status.SHOWN;  // バナーでは、HandleAdOpenedが呼ばれないため
			}
#endif
			request = Status.SHOWN;
		}

		/// <summary>非表示</summary>
		private void Hide () {
			if (!valid) { return; }
			Debug.Log ($"Ad Hide {Scene} {type}");
#if (UNITY_ANDROID || UNITY_IOS)
			if (bannerView != null && (state == Status.SHOWN || state == Status.LOADED)) {  // バナー以外は使い捨てなので隠す概念がない
				bannerView.Hide ();
				state = Status.LOADED;  // 隠されたのではなく、ロード直後に戻ったものとする
			}
#endif
			request = Status.HIDDEN;
		}

		/// <summary>破棄</summary>
		public void Destroy () {
			if (!valid) { return; }
			Debug.Log ($"Ad Destroy {Scene} {type}");
			Remove ();
			adsList.Remove (this);
			request = state = Status.DELETED;
		}

			#region event handler

#if (UNITY_ANDROID || UNITY_IOS)
		/// <summary>ロードされた Called when an ad request has successfully loaded.</summary>
		private void HandleAdLoaded (object sender, EventArgs args) {
			Debug.Log ($"Ad Loaded {Scene} {type} {sender} {args} {request} {valid}");
			state = Status.LOADED;
			if (request == Status.SHOWN) {
				Show ();
			} else {
				Hide ();
			}
		}

		/// <summary>ロードに失敗した Called when an ad request failed to load.</summary>
		private void HandleAdFailedToLoad (object sender, AdFailedToLoadEventArgs args) {
			Debug.Log ($"Ad FailedToLoad {Scene} {type} {sender} {args} with message: {args.LoadAdError.GetMessage ()}");
			Remove (true);
		}

		/// <summary>表示された(バナー以外) / クリックされた(バナー) Called when an ad is shown or clicked.</summary>
		private void HandleAdOpened (object sender, EventArgs args) {
			Debug.Log ($"Ad Opened {Scene} {type} {sender} {args}");
			if (type == AdType.Interstitial || type == AdType.Rewarded) {
				state = Status.SHOWN;
			}
		}

		/// <summary>閉じた Called when the ad is closed.</summary>
		private void HandleAdClosed (object sender, EventArgs args) {
			Debug.Log ($"Ad Closed {Scene} {type} {sender} {args}");
			if (type == AdType.Interstitial || type == AdType.Rewarded) {
				request = state = Status.HIDDEN;
#if UNITY_IOS
				// エラー対応 (iOSのみの症状)
                //   Failed to receive ad with error: Request Error: Will not send request because ad has been used.
				//   クローズしただけでは使用終了にならず、ロードしようとすると使用中でエラーするため、破棄と再生成を行う。
				this.Create ();
#endif
				Load ();	// 次の先読み
			}
		}

		/// <summary>報酬を得た Called when the user should be rewarded for watching a video.</summary>
		private void HandleAdRewarded (object sender, Reward args) {
			string type = args.Type;
			double amount = args.Amount;
			Debug.Log ($"Ad Rewarded {Scene} {this.type} {sender} {args} for {amount.ToString ()} {type}");
		}

#endif

			#endregion


	}

#endif

		}
