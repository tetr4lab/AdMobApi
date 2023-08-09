//	Copyright© tetr4lab.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
#if ALLOW_ADS
using GoogleMobileAds.Api;

namespace GoogleMobileAds.Utility {

	/// <summary>論理広告</summary>
	public class AdMobApi {

		/// <summary>状態/要求</summary>
		protected enum Status {
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
		protected static bool allow;
		/// <summary>初期化完了</summary>
		public static bool Acceptable;
		/// <summary>生成された広告一覧</summary>
		protected static List<AdMobApi> adsList;
		/// <summary>再入抑制用</summary>
		protected static bool isInitializing;

		/// <summary>指定シーンの広告一覧を得る</summary>
		public static List<AdMobApi> GetSceneAds (string scene) => adsList?.FindAll (ad => ad.Scene == scene) ?? new List<AdMobApi> { };

		/// <summary>指定のシーンと番号の広告を得る</summary>
		public static AdMobApi GetSceneAds (string scene, int unit) => adsList?.Find (ad => ad.Scene == scene && ad.Unit == unit);

		/// <summary>シーンの破棄</summary>
		/// <param name="scene">シーン (nullなら全て)</param>
		public static void Destroy (string scene = null) {
			if (string.IsNullOrEmpty (scene)) {
				foreach (var ad in adsList) {
					ad.Remove ();
				}
				adsList.Clear ();
			} else {
				foreach (var ad in GetSceneAds (scene)) {
					ad.Destroy ();
				}
			}
		}

		/// <summary>クラス初期化</summary>
		/// <param name="onCompleted">完了時コールバック</param>
		public static async Task Initialize (Action<InitializationStatus> onCompleted = null) {
			if (Allow && !isInitializing) {
				Debug.Log ("AdMobApi Init");
				isInitializing = true;
				adsList = new List<AdMobApi> { };
				MobileAds.Initialize (status => {
					Acceptable = true;
					if (onCompleted != null) {
						onCompleted (status);
					}
				});
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

		protected BannerView bannerView; // バナー広告
		protected InterstitialAd interstitial; // インタースティシャル広告
		protected RewardedAd rewardedVideo; // 動画リワード広告
		protected Action<Reward> onAdRewarded;   // 動画リワード報酬時処理
		protected AdType type; // タイプ
		protected Status state; // 状態
		protected Status request; // 要求
		protected AdSize adSize; // バナーサイズ
		protected AdPosition adPosition; // バナー位置
		protected bool valid => state != Status.DELETED; // 有効

#if UNITY_EDITOR
		/// <summary>広告表示体オブジェクト名</summary>
		protected string gameObjectName {
			get {
				if (adSize == AdSize.Banner) {
					return "BANNER";
#pragma warning disable CS0618 // 型またはメンバーが旧型式です
				} else if (adSize == AdSize.SmartBanner) {
#pragma warning restore CS0618 // 型またはメンバーが旧型式です
					return "SMART_BANNER";
				} else if (adSize == AdSize.MediumRectangle) {
					return "MEDIUM_RECTANGLE";
				} else if (adSize == AdSize.IABBanner) {
					return "FULL_BANNER";
				} else if (adSize == AdSize.Leaderboard) {
					return "LEADERBOARD";
				} else if (adSize.Width == 320 && adSize.Height == 100) {
					return "LARGE_BANNER";
				} else if (adSize.Width == -1 && adSize.Height == 0) {
					return "ADAPTIVE";
				}
				return null;
			}
		}
#endif

		/// <summary>バナーのサイズ</summary>
		public Vector2 bannerSize {
			get {
				if (valid && bannerView != null) {
#if UNITY_EDITOR
					// エディタではCanvasScalerが考慮されていないので実物のスケールを適用
					var size = new Vector2 (bannerView.GetWidthInPixels (), bannerView.GetHeightInPixels ());
					var rectTransform = AdMobObj.GetAdRectTransform (name: gameObjectName, width: size.x, height: size.y);
					if (rectTransform != null) {
						return size * rectTransform.lossyScale;
					}
					// 見つからなければ広告サイズから推定
					size = new Vector2 (adSize.Width, adSize.Height);
					if (size.x <= 0f) { size.x = AdSize.FullWidth; }
					if (size.y <= 0f) { size.y = 90; }
					return size * (Screen.dpi / 160);
#else
					// 実機では申告サイズ通りとみなす
					return new Vector2 (bannerView.GetWidthInPixels (), bannerView.GetHeightInPixels ());
#endif
				}
				return Vector2.zero;
			}
		}

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
		public bool IsLoaded => valid && (state == Status.LOADED || state == Status.SHOWN || state == Status.HIDDEN);

		/// <summary>コンストラクタ インタースティシャル</summary>
		public AdMobApi (string scene) {
			Debug.Log ($"AdMobApi {scene}");
			if (GetSceneAds (scene)?.Count > 0) {
				// 唯一のユニットでなければなりません
				throw new ArgumentException ($"Duplicate set {scene}");
			}
			type = AdType.Interstitial;
			Scene = scene;
			Create ();
			adsList.Add (this);
		}

		/// <summary>コンストラクタ バナー</summary>
		public AdMobApi (string scene, AdSize size, AdPosition pos) {
			var ads = GetSceneAds (scene);
			Debug.Log ($"AdMobApi {scene}[{ads.Count}] ({size.Width}, {size.Height}) {pos} {size.AdType}");
			foreach (var ad in adsList) {
				if (ad.Scene == scene) {
					// 表示の重複
					if (ad.type != AdType.Banner) {
						// 全画面広告が存在します
						throw new ArgumentException ($"Duplicate set {scene} {ad.type} != {AdType.Banner}");
					} else if (ad.adPosition == pos) {
						// 既にバナーがある位置です
						throw new ArgumentException ($"Duplicate position {scene} {pos} {ads.Count} != {ad.Unit}");
					}
				}
				if (ad.adSize == size && ad.adPosition != pos) {
					// 広告ユニットの多重使用 (同じサイズのバナーを異なる場所に配置できません)
					throw new ArgumentException ($"Duplicate unit {scene} ({size.Width}, {size.Height}) {pos} != {ad.adPosition}");
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

		/// <summary>コンストラクタ アンカーアダプティブバナー</summary>
		public AdMobApi (string scene, bool bottom) : this (scene, AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth (AdSize.FullWidth), bottom ? AdPosition.Bottom : AdPosition.Top) { }

		/// <summary>コンストラクタ リワード</summary>
		public AdMobApi (string scene, Action<Reward> onAdRewarded) {
			Debug.Log ($"AdMobApi {scene} {onAdRewarded}");
			if (GetSceneAds (scene)?.Count > 0) {
				// 唯一のユニットでなければなりません
				throw new ArgumentException ($"Duplicate set {scene}");
			}
			this.onAdRewarded = onAdRewarded;
			type = AdType.Rewarded;
			Scene = scene;
			Create ();
			adsList.Add (this);
		}

		/// <summary>生成</summary>
		/// <param name="keepBannerState">再生成時にバナーの表示を維持</param>
		protected void Create (bool keepBannerState = false) {
			if (!valid) { return; }
			Remove (keepBannerState && type == AdType.Banner);
			Debug.Log ($"Ad Create {Scene} {type}, keep={keepBannerState}, stat={state}, request={request}");
			Load ();
		}

		/// <summary>除去</summary>
		/// <param name="keepShown">表示状態を要求として維持</param>
		protected void Remove (bool keepShown = false) {
			if (!valid) { return; }
#if (UNITY_ANDROID || UNITY_IOS)
			if (bannerView != null) {
				Debug.Log ($"Ad Remove {Scene} {Unit} {type} {keepShown}");
				bannerView.Destroy ();
				bannerView = null;
			}
			if (interstitial != null) {
				Debug.Log ($"Ad Remove {Scene} {Unit} {type} {keepShown}");
				interstitial.Destroy ();
				interstitial = null;
			}
			if (rewardedVideo != null) {
				Debug.Log ($"Ad Remove {Scene} {Unit} {type} {keepShown}");
				rewardedVideo.Destroy ();
				rewardedVideo = null;
			}
#endif
			if (keepShown && state == Status.SHOWN) {
				request = Status.SHOWN;
			}
			state = Status.NONE;
		}

		/// <summary>読込</summary>
		protected void Load () {
			if (!valid) { return; }
			Debug.Log ($"Ad Load {Scene} {type}");
			if (state != Status.LOADING && state != Status.LOADED) {
#if (UNITY_ANDROID || UNITY_IOS)
				var adRequest = new AdRequest ();
				if (type == AdType.Rewarded) {
					if (rewardedVideo != null) {
						rewardedVideo.Destroy ();
						rewardedVideo = null;
					}
					RewardedAd.Load (AdMobObj.AdUnitId [type], adRequest, (RewardedAd ad, LoadAdError error) => {
						rewardedVideo = ad;
						if (ad == null || error != null) {
							HandleAdFailedToLoad (error);
						} else {
							rewardedVideo.OnAdFullScreenContentOpened += HandleAdOpened;
							rewardedVideo.OnAdFullScreenContentClosed += HandleAdClosed;
							HandleAdLoaded (ad.GetResponseInfo ());
						}
					});
				} else if (type == AdType.Interstitial) {
					if (interstitial != null) {
						interstitial.Destroy ();
						interstitial = null;
					}
					InterstitialAd.Load (AdMobObj.AdUnitId [type], adRequest, (InterstitialAd ad, LoadAdError error) => {
						interstitial = ad;
						if (ad == null || error != null) {
							HandleAdFailedToLoad (error);
						} else {
							interstitial.OnAdFullScreenContentOpened += HandleAdOpened;
							interstitial.OnAdFullScreenContentClosed += HandleAdClosed;
							HandleAdLoaded (ad.GetResponseInfo ());
						}
					});
				} else if (type == AdType.Banner) {
					if (bannerView == null) {
						if ((bannerView = new BannerView (AdMobObj.AdUnitId [AdType.Banner], adSize, adPosition)) != null) {
							bannerView.OnBannerAdLoaded += HandleAdLoaded;
							bannerView.OnBannerAdLoadFailed += HandleAdFailedToLoad;
							bannerView.OnAdClicked += HandleAdOpened;
							bannerView.OnAdImpressionRecorded += HandleAdClosed;
						}
					}
					if (bannerView != null) {
						bannerView.LoadAd (adRequest);
						bannerView.Hide ();
					}
				}
#endif
				state = Status.LOADING;
			}
		}

		/// <summary>表示</summary>
		protected void Show () {
			if (!valid && !IsLoaded) { return; }
			Debug.Log ($"Ad Show {Scene} {type}");
#if (UNITY_ANDROID || UNITY_IOS)
			if (rewardedVideo != null) {
				rewardedVideo.Show ((Reward reward) => (onAdRewarded ?? HandleAdRewarded) (reward));
			} else if (interstitial != null) {
				interstitial.Show ();
			} else if (bannerView != null) {
				bannerView.Show ();
				state = Status.SHOWN;  // バナーでは、HandleAdOpenedが呼ばれないため
			}
#endif
			request = Status.SHOWN;
		}

		/// <summary>非表示</summary>
		protected void Hide () {
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
			Debug.Log ($"Ad Destroy {Scene} {type}");
			if (valid) {
				Remove (false);
			}
			if (adsList.Contains (this)) {
				adsList.Remove (this);
			}
			request = state = Status.DELETED;
		}

		#region event handler

#if (UNITY_ANDROID || UNITY_IOS)
		/// <summary>ロードされた Called when an ad request has successfully loaded.</summary>
		protected void HandleAdLoaded () => HandleAdLoaded (null);

		/// <summary>ロードされた Called when an ad request has successfully loaded.</summary>
		protected void HandleAdLoaded (ResponseInfo response) {
			Debug.Log ($"Ad Loaded {Scene} {type} {response} {request} {valid}");
			state = Status.LOADED;
			if (request == Status.SHOWN) {
				Show ();
			} else {
				Hide ();
			}
		}

		/// <summary>ロードに失敗した Called when an ad request failed to load.</summary>
		protected void HandleAdFailedToLoad () => HandleAdFailedToLoad (null);

		/// <summary>ロードに失敗した Called when an ad request failed to load.</summary>
		protected void HandleAdFailedToLoad (LoadAdError error) {
			Debug.Log ($"Ad FailedToLoad {Scene} {type} {error}");
			Remove (true);
		}

		/// <summary>表示された(バナー以外) / クリックされた(バナー) Called when an ad is shown or clicked.</summary>
		protected void HandleAdOpened () {
			Debug.Log ($"Ad Opened {Scene} {type}");
			if (type == AdType.Interstitial || type == AdType.Rewarded) {
				state = Status.SHOWN;
			}
		}

		/// <summary>閉じた Called when the ad is closed.</summary>
		protected void HandleAdClosed () {
			Debug.Log ($"Ad Closed {Scene} {type}");
			if (type == AdType.Interstitial || type == AdType.Rewarded) {
				request = state = Status.HIDDEN;
#if UNITY_IOS
				// エラー対応 (iOSのみの症状)
				//   Failed to receive ad with error: Request Error: Will not send request because ad has been used.
				//   クローズしただけでは使用終了にならず、ロードしようとすると使用中でエラーするため、破棄と再生成を行う。
				this.Create ();
#endif
				Load ();    // 次の先読み
			}
		}

		/// <summary>報酬を得た Called when the user should be rewarded for watching a video.</summary>
		protected void HandleAdRewarded (Reward args) {
			string type = args.Type;
			double amount = args.Amount;
			Debug.Log ($"Ad Rewarded {Scene} {this.type} {args} for {amount.ToString ()} {type}");
		}

#endif

		#endregion

	}

}
#endif
