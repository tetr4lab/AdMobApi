//	Copyright© tetr4lab.
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Tetr4lab.TaskEx;

/// <summary>
/// AdMobを使うためのラッパー
/// 
/// 互換性
///		バナー、インタースティシャル、リワードビデオに対応
/// 準備
///		AdMobコンソール
///			広告ID(AppID, UnitID)を作成する
///			テストデバイスを登録する
///				登録に必要なデバイス側IDは、同梱のサンプルアプリでも表示可能
///	導入
///		プロジェクトにGoogle Mobile Ads Unityパッケージ を導入する
///			ref: https://github.com/googleads/googleads-mobile-unity/releases
///		Assets > External Dependency Manager > XXXXX Resolver > Resolve を実施する
///		Assets > Google Mobile Ads > Settings... でAppIDを設定する
///			テスト用AppID
///			Android: ca-app-pub-3940256099942544~3347511713
///			iOS:     ca-app-pub-3940256099942544~1458002511
///		このスクリプトを適当なシーンオブジェクトにアタッチする (シーン全体で一つだけ)
///			UnitIDを設定する
///				設定しなければテストユニットになる
///			複数のシーンで共有する場合は、ルートオブジェクトにアタッチして、インスペクタで Donot Destroy にチェックする
///	概念
///		ユニット
///			論理広告(AdMobApi)クラスのインスタンス
///			「シーン」と「ユニット番号」で特定する
///		ユニット番号
///			シーン毎に0から開始
///				どのシーンも最初に生成したユニットの番号は0
///			ユニットを破棄しても、シーンが存在する限り使い回されない
///				追加生成した場合は、シーンで最大のユニット番号+1が割り当てられる
///				シーンのユニットが全て破棄されると再度0から割り当てられる
///		シーン
///			ユニットのグループを表す文字列 (Unity の Scene Asset とは無関係)
///			所属するユニットには、「シーン」内でユニークな「ユニット番号」が与えられる
///	使い方
///		概要
///			オプトインを経て AdMobApi.Allow を有効にすることで、モジュールが初期化される
///			明示的にAdMobApi.Initialize()を呼ぶ必要は無い(が呼んでも良い)、完了待ちは AdMobApi.Acceptable をチェックする
///			指定「シーン」の異なる広告は同時に表示されないように排他制御される
///		広告の作成
///			var instance = new AdMobApi (string scene, bool bottom); // アンカーアダプティブバナー
///			var instance = new AdMobApi (string scene, AdSize size, AdPosition pos); // バナー
///			var instance = new AdMobApi (string scene); // インタースティシャル
///			var instance = new AdMobApi (string scene, Action<Reward> onAdRewarded); // リワード
///		ユニットの取得
///			List<AdMobApi> instances = AdMobApi.GetSceneAds (string scene); // 指定シーンの全ユニット
///			AdMobApi instance = AdMobApi.GetSceneAds (string scene, int unit); // 指定の単一ユニット
///		表示の制御
///			AdMobApi.SetActive (string scene, bool active); // シーン (排他制御あり)
///			AdMobApi.SetActive (); // 全シーン
///			bool activity = AdMobApi.GetActive (string scene); // シーン
///			instance.ActiveSelf = (bool) active; // ユニット (排他制御なし)
///			bool activity = instance.ActiveSelf; // ユニット
///		ユニットの破棄
///			instance.Destroy (); // ユニット
///			AdMobApi.Destroy (string scene); // 指定シーンの全ユニット
///			AdMobApi.Destroy (); // 全シーンの全ユニット
/// </summary>
namespace GoogleMobileAds.Utility {

	/// <summary>広告制御コンポーネント</summary>
	public class AdMobObj : MonoBehaviour {

		#region static
		/// <summary>ロード失敗後のリトライ間隔フレーム数</summary>
		public const int ReLoadIntervalFrames = 300;

		// シングルトンインスタンス
		private static AdMobObj instance;

		// 広告ユニットIDの取得
		public static AdUnitIDs AdUnitId => (instance?.adUnitId == null || instance.adUnitId.Count < 1) ? new AdUnitIDs () : instance.adUnitId;

#if UNITY_EDITOR && ALLOW_ADS
		/// <summary>広告表示体の取得</summary>
		public static RectTransform GetAdRectTransform (int index = -1, string name = null, float width = 0f, float height = 0f) => instance.FindAdObject (index, name, width, height)?.GetComponent<RectTransform> ();
#endif
        #endregion static

        /// <summary>インスペクタで定義可能な広告ユニットID</summary>
        [SerializeField]
		private AdUnitIDs adUnitId = new AdUnitIDs {
			{ RuntimePlatform.Android,      AdType.Banner,       "ca-app-pub-3940256099942544/6300978111" },
			{ RuntimePlatform.Android,      AdType.Interstitial, "ca-app-pub-3940256099942544/1033173712" },
			{ RuntimePlatform.Android,      AdType.Rewarded,     "ca-app-pub-3940256099942544/5224354917" },
			{ RuntimePlatform.IPhonePlayer, AdType.Banner,       "ca-app-pub-3940256099942544/2934735716" },
			{ RuntimePlatform.IPhonePlayer, AdType.Interstitial, "ca-app-pub-3940256099942544/4411468910" },
			{ RuntimePlatform.IPhonePlayer, AdType.Rewarded,     "ca-app-pub-3940256099942544/1712485313" },
		};

        /// <summary>複数シーンで共有 (使用の際はルートオブジェクトにアタッチのこと)</summary>
        [SerializeField]
        private bool donotDestroy = false;

#if ALLOW_ADS
        /// <summary>再接続時の再構築開始までの遅延時間(ms)</summary>
        private const int OnlineDelayTime = 500;

		/// <summary>画面サイズ変化時の再構築開始までの遅延時間(ms)</summary>
		private const int ChangeScreenDelayTime = 200;

		/// <summary>オンライン判定 (実際の使用可能性は判定しない)</summary>
		private bool isOnLine => (Application.internetReachability != NetworkReachability.NotReachable);
		private bool lastOnline;

		/// <summary>初期化時の再入抑止用</summary>
		private bool isInitializing;

		/// <summary>駆動時の再入抑止用</summary>
		private bool isExclusionControling;

		/// <summary>画面のサイズ</summary>
		private Vector2Int lastScreenSize;

		/// <summary>失敗後の経過フレーム数</summary>
		private int elapsedFramesSinceLoadFailure = -1;

#if UNITY_EDITOR
		/// <summary>表示体を特定する</summary>
		public GameObject FindAdObject (int index = -1, string name = null, float width = 0f, float height = 0f) {
			var rootObjects = gameObject.scene.GetRootGameObjects ();
			if (index >= 0) {
				if (index < rootObjects.Length) {
					return check (rootObjects [index]);
				}
			} else {
				for (var i = 0; i < rootObjects.Length; i++) {
					var obj = check (rootObjects [i]);
					if (obj) { return obj; }
				}
			}
			return null;

			GameObject check (GameObject obj) {
				if ((name == null || obj.name.StartsWith (name))) {
					Canvas canvas;
					if ((canvas = obj.GetComponent<Canvas> ()) && canvas.sortingOrder == Int16.MaxValue) {
						Image image;
						if ((width <= 0f && height <= 0f) || (
								(image = obj.GetComponentInChildren<Image> ())
								&& (width <= 0f || image.rectTransform.sizeDelta.x == width)
								&& (height <= 0f || image.rectTransform.sizeDelta.y == height)
							)
						) {
							return obj;
						}
					}
				}
				return null;
			}
		}
#endif

        /// <summary>シングルトン初期化</summary>
        private void Start () {
			if (instance == null) {
				instance = this;
				if (donotDestroy) {
					DontDestroyOnLoad (gameObject);
				}
			} else {
                Debug.LogError ($"singleton duplicated on {GetPath (transform)}", gameObject);
				Destroy (this);
				string GetPath (Transform node) => (node == null) ? "" : $"{GetPath (node.parent)}/{node.name}";
			}
		}

#if UNITY_ANDROID && !UNITY_2022_3_OR_NEWER
        // 2022.3.16でFIXされた不具合(UUM-57151)への対応
        /// <summary>制御状態の変化</summary>
        private void OnApplicationPause (bool pause) {
            Debug.Log ($"OnApplicationPause { pause}");
            if (!pause) {
                // 復帰したらバナーを再生成
                AdMobApi.ReMake ();
            }
        }
#endif

        /// <summary>駆動</summary>
        private async void Update () {
			if (instance != this || !AdMobApi.Allow) { return; }
			if (!isInitializing) {
				// 初期化
				isInitializing = true;
				AdMobApi.Initialize ();
				// 初期化待ち
				await TaskEx.DelayUntil (() => AdMobApi.Acceptable);
				// デリゲートにも書けなくはないが、接続検査はメインスレッドで実行する必要がある
				lastOnline = isOnLine;
				lastScreenSize = new Vector2Int (Screen.width, Screen.height);
				Debug.Log ($"Set {(lastOnline ? "On" : "Off")}Line, Screen{lastScreenSize}");
			}
			if (AdMobApi.Acceptable && !isExclusionControling) {
				isExclusionControling = true;
				var remaking = false;
				// 新しい接続を検出
				if (lastOnline != isOnLine) {
					Debug.Log ($"Change {(lastOnline ? "On => Off" : "Off => On")}Line");
					await TaskEx.DelayWhile (() => lastOnline != isOnLine, OnlineDelayTime);
					if (lastOnline != isOnLine) {
						// 規定時間まで状況が継続した
						if (lastOnline = isOnLine) {
							AdMobApi.ReMake ();
							remaking = true;
						}
					}
				}
				// 画面のサイズが変化したらバナーを再生成
				var newScreenSize = new Vector2Int (Screen.width, Screen.height);
				if (lastScreenSize != newScreenSize) {
					Debug.Log ($"Change Screen{lastScreenSize} => {newScreenSize}");
					await TaskEx.DelayWhile (() => newScreenSize.x == Screen.width && newScreenSize.y == Screen.height, ChangeScreenDelayTime);
					if (newScreenSize.x == Screen.width && newScreenSize.y == Screen.height) {
						// 規定時間まで状況が継続した
						lastScreenSize = newScreenSize;
						AdMobApi.ReMake (type: AdType.Banner);
						remaking = true;
					}
				}
				// 失敗したロードのリトライ
				if (!remaking && AdMobApi.IsAnyLoadFailed && isOnLine) {
					elapsedFramesSinceLoadFailure = (elapsedFramesSinceLoadFailure < 0) ? 0 : elapsedFramesSinceLoadFailure + 1;
					if (elapsedFramesSinceLoadFailure >= ReLoadIntervalFrames) {
						AdMobApi.ReLoad ();
						elapsedFramesSinceLoadFailure = -1;
					}
				}
				isExclusionControling = false;
			}
		}

		/// <summary>破棄</summary>
		private void OnDestroy () {
			if (instance != this) { return; }
			AdMobApi.Destroy ();
		}

#endif

    }

    /// <summary>広告タイプ</summary>
    public enum AdType {
		None = 0,
		Banner,
		Interstitial,
		Rewarded,
	}

	// インスペクタから編集するための方策
	#region AdUnitID

	/// <summary>広告ユニットID</summary>
	[Serializable]
	public class AdUnitID {

		/// <summary>プラットフォーム</summary>
		[SerializeField] public RuntimePlatform platform;

		/// <summary>種類</summary>
		[SerializeField] public AdType type;

		/// <summary>識別子</summary>
		[SerializeField] public string id;

		/// <summary>コンストラクタ</summary>
		public AdUnitID (RuntimePlatform platform, AdType type, string id) {
			this.platform = platform;
			this.type = type;
			this.id = id;
		}

	}

	/// <summary>
	/// 広告ユニットID一覧
	/// </summary>
	[Serializable]
	public class AdUnitIDs : IEnumerable {

		/// <summary>一覧</summary>
		[SerializeField] private AdUnitID [] list;

		/// <summary>登録数</summary>
		public int Count => list?.Length ?? 0;

		/// <summary>現在のプラットフォーム</summary>
#if UNITY_EDITOR && UNITY_ANDROID
		private const RuntimePlatform currentPlatform = RuntimePlatform.Android;
#elif UNITY_EDITOR && UNITY_IOS
		private const RuntimePlatform currentPlatform =	RuntimePlatform.IPhonePlayer;
#else
		private static readonly RuntimePlatform currentPlatform = Application.platform;
#endif
		/// <summary>種別からIDを得る</summary>
		public string this [AdType type] => list != null ? Array.Find (list, u => u.platform == currentPlatform && u.type == type)?.id : null;

		/// <summary>一覧に追加</summary>
		public void Add (RuntimePlatform platform, AdType type, string id) {
			var tempList = (list != null) ? new List<AdUnitID> (list) : new List<AdUnitID> { };
			tempList.Add (new AdUnitID (platform, type, id));
			list = tempList.ToArray ();
		}

		/// <summary>コレクション初期化子を使用可能にするための空実装</summary>
		public IEnumerator GetEnumerator () => throw new NotImplementedException ();

	}

	#endregion AdUnitID

}
