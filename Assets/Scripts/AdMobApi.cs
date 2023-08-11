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
			/// <summary>初期状態</summary>
			NONE = 0,
			/// <summary>読み込み中</summary>
			LOADING,
			/// <summary>読み込み済み</summary>
			LOADED,
			/// <summary>表示中/summary>
			SHOWN,
			/// <summary>非表示中</summary>
			HIDDEN,
			/// <summary>削除済み</summary>
			DELETED,
		}

		#region static
		// クラス要素
		/// <summary>初期化トリガー</summary>
		public static bool Allow {
			get => _allow;
			set {
				_allow = value;
				Debug.Log ($"AdMobApi Triggered {_allow}");
				if (!_allow) { // 全停止
					foreach (var ad in adsList) {
						ad.Remove ();
					}
				}
			}
		}
		protected static bool _allow;

		/// <summary>初期化完了</summary>
		public static bool Acceptable;

		/// <summary>生成された広告一覧</summary>
		protected static List<AdMobApi> adsList;

		/// <summary>再入抑制用</summary>
		protected static bool isInitializing;

		/// <summary>指定シーンの広告一覧を得る</summary>
		public static List<AdMobApi> GetSceneAds (string scene) => adsList?.FindAll (ad => ad.Scene == scene) ?? new List<AdMobApi> { };

		/// <summary>指定シーンの最後のユニット番号+1 ユニットがなければ0</summary>
		public static int GetNextUnitNumber (string scene) => adsList.FindLast (ad => ad.Scene == scene)?.Unit + 1 ?? 0;

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
		public static void Initialize (Action<InitializationStatus> onCompleted = null) {
			if (Allow && !isInitializing) {
				Debug.Log ("AdMobApi Init");
				isInitializing = true;
				adsList = new List<AdMobApi> { };
				MobileAds.Initialize (status => {
					Acceptable = true;
					if (onCompleted != null) {
						onCompleted (status);
					}
					Debug.Log ($"AdMobApi Inited {Acceptable}");
				});
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

		/// <summary>再構築</summary>
		public static void ReMake (string scene = null, AdType type = AdType.None) {
			if (Allow && Acceptable) {
				foreach (var ad in adsList) {
					if ((scene == null || ad.Scene == scene) && (type == AdType.None || ad.Type == type)) {
						var request = ad.ShowRequested;
						ad.Load (true);
						ad.ActiveSelf = request;
					}
				}
				Debug.Log ($"Ad ReMaked {scene}:{adsList.Count} {type} {{{string.Join (", ", adsList.ConvertAll (a => $"{a.Scene}:{a.Unit} {a.Type} {a._dirty} {a.ShowRequested}"))}}}");
			}
		}

		/// <summary>更新があったことを設定</summary>
		public static void SetDirty (string scene = null, AdType type = AdType.None) {
			if (Allow && Acceptable) {
				Debug.Log ($"Ad Dirty {scene}:{adsList.Count} {type}");
				foreach (var ad in adsList) {
					if ((scene == null || ad.Scene == scene) && (type == AdType.None || ad.Type == type)) {
						ad.Dirty = true;
					}
				}
			}
		}
		#endregion

		// メンバー要素

		/// <summary>シーン名</summary>
		public string Scene { get; protected set; }

		/// <summary>ユニット番号</summary>
		public int Unit { get; protected set; } = 0;

		/// <summary>タイプ</summary>
		public AdType Type { get; protected set; }

		/// <summary>状態</summary>
		protected Status State {
			get => _state;
			set {
				//Debug.Log ($"Ad State {Scene}:{Unit} {_state} => {value}");
				_state = value;
			}
		}
		protected Status _state;

		/// <summary>表示要求</summary>
		protected bool ShowRequested {
			get => _request;
			set {
				//Debug.Log ($"Ad Request {Scene}:{Unit} {_request} => {value}");
				_request = value;
			}
		}
		protected bool _request;

		/// <summary>更新あり (読み出すと消える)</summary>
		public bool Dirty {
			get {
				var lastDirty = _dirty;
				_dirty = false;
				return lastDirty;
			}
			set => _dirty = value;
		}
		protected bool _dirty;

		/// <summary>バナー広告</summary>
		protected BannerView bannerView;
		
		/// <summary>インタースティシャル広告</summary>
		protected InterstitialAd interstitial;
		
		/// <summary>動画リワード広告</summary>
		protected RewardedAd rewardedVideo;
		
		/// <summary>動画リワード報酬時処理</summary>
		protected Action<Reward> onAdRewarded;

		/// <summary>バナーサイズ</summary>
		protected AdSize bannerSize;

		/// <summary>バナー位置</summary>
		protected AdPosition bannerPosition;

		/// <summary>有効</summary>
		protected bool Valid => State != Status.DELETED;

#if UNITY_EDITOR
		/// <summary>広告表示体オブジェクト名</summary>
		protected string gameObjectName {
			get {
				if (bannerSize == AdSize.Banner) {
					return "BANNER";
#pragma warning disable CS0618 // 型またはメンバーが旧型式です
				} else if (bannerSize == AdSize.SmartBanner) {
#pragma warning restore CS0618 // 型またはメンバーが旧型式です
					return "SMART_BANNER";
				} else if (bannerSize == AdSize.MediumRectangle) {
					return "MEDIUM_RECTANGLE";
				} else if (bannerSize == AdSize.IABBanner) {
					return "FULL_BANNER";
				} else if (bannerSize == AdSize.Leaderboard) {
					return "LEADERBOARD";
				} else if (bannerSize.Width == 320 && bannerSize.Height == 100) {
					return "LARGE_BANNER";
				} else if (bannerSize.Width == -1 && bannerSize.Height == 0) {
					return "ADAPTIVE";
				}
				return null;
			}
		}
#endif

		/// <summary>バナーのサイズ</summary>
		public Vector2 BannerPixelSize {
			get {
				if (Valid && bannerView != null) {
#if UNITY_EDITOR
					// エディタではCanvasScalerが考慮されていないので実物のスケールを適用
					var size = new Vector2 (bannerView.GetWidthInPixels (), bannerView.GetHeightInPixels ());
					var rectTransform = AdMobObj.GetAdRectTransform (name: gameObjectName, width: size.x, height: size.y);
					if (rectTransform != null) {
						return size * rectTransform.lossyScale;
					}
					// 見つからなければ広告サイズから推定
					size = new Vector2 (bannerSize.Width, bannerSize.Height);
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
			get => Valid && ShowRequested && ((Type == AdType.Rewarded && rewardedVideo != null) || (Type == AdType.Interstitial && interstitial != null) || (Type == AdType.Banner && bannerView != null));
			set { if (Valid) { if (value) { Show (); } else { Hide (); } } }
		}

		/// <summary>ロード済み</summary>
		public bool IsLoaded => Valid && (State == Status.LOADED || State == Status.SHOWN || State == Status.HIDDEN);

		/// <summary>コンストラクタ インタースティシャル</summary>
		public AdMobApi (string scene) {
			Type = AdType.Interstitial;
			Scene = scene;
			Unit = GetNextUnitNumber (scene);
			Debug.Log ($"AdMobApi ({scene}:{Unit})");
			if (Unit > 0) {
				// 唯一のユニットでなければなりません
				throw new ArgumentException ($"Duplicate set {scene}");
			}
			Load ();
			adsList.Add (this);
		}

		/// <summary>コンストラクタ バナー</summary>
		public AdMobApi (string scene, AdSize size, AdPosition pos) {
			Type = AdType.Banner;
			Scene = scene;
			Unit = GetNextUnitNumber (scene);
			bannerSize = size;
			bannerPosition = pos;
			Debug.Log ($"AdMobApi ({scene}:{Unit}, ({size.Width}, {size.Height}), {pos}) {size.AdType}");
			foreach (var ad in adsList) {
				if (ad.Scene == scene) {
					// 表示の重複
					if (ad.Type != AdType.Banner) {
						// 全画面広告が存在します
						throw new ArgumentException ($"Duplicate set {scene} {ad.Type} != {AdType.Banner}");
					} else if (ad.bannerPosition == pos) {
						// 既にバナーがある位置です
						throw new ArgumentException ($"Duplicate position {scene} {pos} {Unit} != {ad.Unit}");
					}
				}
				if (ad.bannerSize == size && ad.bannerPosition != pos) {
					// 広告ユニットの多重使用 (同じサイズのバナーを異なる場所に配置できません)
					throw new ArgumentException ($"Duplicate unit {scene} ({size.Width}, {size.Height}) {pos} != {ad.bannerPosition}");
				}
			}
			Load ();
			adsList.Add (this);
		}

		/// <summary>コンストラクタ アンカーアダプティブバナー</summary>
		public AdMobApi (string scene, bool bottom) : this (scene, AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth (AdSize.FullWidth), bottom ? AdPosition.Bottom : AdPosition.Top) { }

		/// <summary>コンストラクタ リワード</summary>
		public AdMobApi (string scene, Action<Reward> onAdRewarded) {
			Type = AdType.Rewarded;
			Scene = scene;
			Unit = GetNextUnitNumber (scene);
			this.onAdRewarded = onAdRewarded;
			Debug.Log ($"AdMobApi ({scene}:{Unit}, {onAdRewarded})");
			if (Unit > 0) {
				// 唯一のユニットでなければなりません
				throw new ArgumentException ($"Duplicate set {scene}");
			}
			Load ();
			adsList.Add (this);
		}

		/// <summary>除去</summary>
		/// <param name="withdraw">表示要求を取り下げ</param>
		protected void Remove (bool withdraw = true) {
			if (!Valid) { return; }
#if (UNITY_ANDROID || UNITY_IOS)
			if (bannerView != null) {
				Debug.Log ($"Ad Remove {Scene}:{Unit} {Type}");
				bannerView.Destroy ();
				bannerView = null;
				Dirty = true;
			}
			if (interstitial != null) {
				Debug.Log ($"Ad Remove {Scene}:{Unit} {Type}");
				interstitial.Destroy ();
				interstitial = null;
				Dirty = true;
			}
			if (rewardedVideo != null) {
				Debug.Log ($"Ad Remove {Scene}:{Unit} {Type}");
				rewardedVideo.Destroy ();
				rewardedVideo = null;
				Dirty = true;
			}
#endif
			if (withdraw) { ShowRequested = false; }
			State = Status.NONE;
		}

		/// <summary>読込</summary>
		protected void Load (bool force = false) {
			if (Valid && (force || (State != Status.LOADING && State != Status.LOADED))) {
				Remove ();
				Debug.Log ($"Ad Load {Scene}:{Unit} {Type}");
#if (UNITY_ANDROID || UNITY_IOS)
				var adRequest = new AdRequest ();
				if (Type == AdType.Rewarded) {
					State = Status.LOADING;
					RewardedAd.Load (AdMobObj.AdUnitId [Type], adRequest, (RewardedAd ad, LoadAdError error) => {
						rewardedVideo = ad;
						if (ad == null || error != null) {
							HandleAdFailedToLoad (error);
						} else {
							rewardedVideo.OnAdFullScreenContentOpened += HandleAdOpened;
							rewardedVideo.OnAdFullScreenContentClosed += HandleAdClosed;
							HandleAdLoaded (ad.GetResponseInfo ());
						}
					});
				} else if (Type == AdType.Interstitial) {
					State = Status.LOADING;
					InterstitialAd.Load (AdMobObj.AdUnitId [Type], adRequest, (InterstitialAd ad, LoadAdError error) => {
						interstitial = ad;
						if (ad == null || error != null) {
							HandleAdFailedToLoad (error);
						} else {
							interstitial.OnAdFullScreenContentOpened += HandleAdOpened;
							interstitial.OnAdFullScreenContentClosed += HandleAdClosed;
							HandleAdLoaded (ad.GetResponseInfo ());
						}
					});
				} else if (Type == AdType.Banner) {
					if ((bannerView = new BannerView (AdMobObj.AdUnitId [AdType.Banner], bannerSize, bannerPosition)) != null) {
						bannerView.OnBannerAdLoaded += HandleAdLoaded;
						bannerView.OnBannerAdLoadFailed += HandleAdFailedToLoad;
						bannerView.OnAdClicked += HandleAdOpened;
						State = Status.LOADING;
						bannerView.LoadAd (adRequest);
						bannerView.Hide ();
					}
				}
#endif
			}
		}

		/// <summary>表示</summary>
		protected void Show () {
			if (!Valid || State == Status.SHOWN) { return; }
			Debug.Log ($"Ad Show {Scene}:{Unit} {Type} {State}");
#if (UNITY_ANDROID || UNITY_IOS)
			switch (Type) {
				case AdType.Rewarded:
					if (rewardedVideo == null) {
						Load ();
					} else if (IsLoaded) {
						rewardedVideo.Show ((Reward reward) => (onAdRewarded ?? HandleAdRewarded) (reward));
					}
					break;
				case AdType.Interstitial:
					if (interstitial == null) {
						Load ();
					} else if (IsLoaded) {
						interstitial.Show ();
					}
					break;
				case AdType.Banner:
					if (bannerView == null) {
						Load ();
					} else if (IsLoaded) {
						bannerView.Show ();
						Dirty = true;
						State = Status.SHOWN;  // バナーはすぐに表示中
					}
					break;
			}
#endif
			ShowRequested = true;
		}

		/// <summary>非表示</summary>
		protected void Hide () {
			if (!Valid || State == Status.HIDDEN) { return; }
#if (UNITY_ANDROID || UNITY_IOS)
			if (bannerView != null && (State == Status.SHOWN || State == Status.LOADED)) {  // バナー以外は使い捨てなので隠す概念がない
				Debug.Log ($"Ad Hide {Scene}:{Unit} {Type} {State}");
				bannerView.Hide ();
				Dirty = true;
				State = Status.HIDDEN;
			}
#endif
			ShowRequested = false;
		}

		/// <summary>破棄</summary>
		public void Destroy () {
			Debug.Log ($"Ad Destroy {Scene}:{Unit} {Type}");
			if (Valid) {
				Remove ();
			}
			if (adsList.Contains (this)) {
				adsList.Remove (this);
			}
			ShowRequested = false;
		}

		#region event handler

#if (UNITY_ANDROID || UNITY_IOS)
		/// <summary>ロードされた Called when an ad request has successfully loaded.</summary>
		protected void HandleAdLoaded () => HandleAdLoaded (null);

		/// <summary>ロードされた Called when an ad request has successfully loaded.</summary>
		protected void HandleAdLoaded (ResponseInfo response) {
			State = Status.LOADED;
			Debug.Log ($"Ad Loaded {Scene}:{Unit} {Type} {response} {ShowRequested} {Valid} {State}");
			if (ShowRequested) {
				Show ();
			}
		}

		/// <summary>ロードに失敗した Called when an ad request failed to load.</summary>
		protected void HandleAdFailedToLoad () => HandleAdFailedToLoad (null);

		/// <summary>ロードに失敗した Called when an ad request failed to load.</summary>
		protected void HandleAdFailedToLoad (LoadAdError error) {
			Debug.Log ($"Ad FailedToLoad {Scene}:{Unit} {Type} {error}");
			Remove (false);
		}

		/// <summary>表示された(バナー以外) / クリックされた(バナー) Called when an ad is shown or clicked.</summary>
		protected void HandleAdOpened () {
			Debug.Log ($"Ad Opened {Scene}:{Unit} {Type}");
			if (Type == AdType.Interstitial || Type == AdType.Rewarded) {
				Dirty = true;
				State = Status.SHOWN;
			}
		}

		/// <summary>閉じた Called when the ad is closed.</summary>
		protected void HandleAdClosed () {
			Debug.Log ($"Ad Closed {Scene}:{Unit} {Type}");
			if (Type == AdType.Interstitial || Type == AdType.Rewarded) {
				Dirty = true;
				State = Status.NONE;
				ShowRequested = false;
				Load ();    // 次の先読み
			}
		}

		/// <summary>報酬を得た Called when the user should be rewarded for watching a video.</summary>
		protected void HandleAdRewarded (Reward args) {
			var type = args.Type;
			var amount = args.Amount;
			Debug.Log ($"Ad Rewarded {Scene}:{Unit} {Type} {args} for {amount} {type}");
		}

#endif
		#endregion

	}

}
#endif
