---
title: Unity で Google Mobile Ads (AdMob) を使う
tags: Unity AdMob Android iOS C#
---
## 前提
- Unity 2022.3.7f1
- Google Mobile Ads Unity Plugin v8.5.1
- Apple App Store、Google Play Store
- この記事では、Google Mobile Adsの一部機能を限定的に使用し、汎用性のない部分があります。
- この記事では、以下の内容を扱いません。
  - Google Mobile Adsのルールやコンソールの使い方など
  - Unityエディタの使い方やモバイル向けのビルド方法など
- この記事のソースは、実機でテストしていますが、本番広告でのテストはしていません。
  - プロジェクトで使用したIDはテスト用のもので、ストアで公開する製品に使用できるものではありません。

## できること
- AndroidまたはiOSで、AdMobを利用します。
- 複数のバナー、インタースティシャル、リワードビデオ広告の表示を制御し、シーンに応じて広告を切り替えます。
- 報酬の獲得を検出します。

## リソース
### このプロジェクト
- [ソース (GitHub)](https://github.com/tetr4lab/AdMobApi)

### Google Mobile Ads Unity Plugin
- [ドキュメント (Google)](https://developers.google.com/admob/unity/quick-start)
- [リリース (GitHub)](https://github.com/googleads/googleads-mobile-unity/releases)

#### プラグインの導入方法
- 上記の「リリース」からGoogle Mobile Ads Unity Plugin のパッケージ(`.unitypackage`)をダウンロードします。
- `Assets` >  `Import Package` > `Custom Package...` でパッケージを導入します。
- `Assets` > `Google Mobile Ads` > `Settings...` で、AppIDを設定します。
  - 以下のAppIDが、テスト用にGoogleから提供されています。
    - Android: `ca-app-pub-3940256099942544~3347511713`
    - iOS:     `ca-app-pub-3940256099942544~1458002511`
  - 必要に応じて、AdMobのコンソールで本番IDを取得してください。
- `Asset` > `External Dependency Manager` > `XXXXX Resolver` > `Resolve` を実施します。

## サンプルプロジェクトの使い方
### 導入
- このサンプルプロジェクトは、Google Mobile Ads Unity Pluginを含まないため、リポジトリをクローンしてエディタで開こうとするとエラーします。
- エラーを無視して開き、[プラグインを導入](#プラグインの導入方法)してください。

### 機能
- 実行すると、テストバナーを表示します。
- バナーボタンを押すと、バナーの表示状態を切り替えます。
  - 右の切り替えボタンを押すとバナーのサイズを切り替えます。
- インタースティシャルボタンを押すと、全画面広告が表示されます。
- リワードボタンを押すと全画面広告が表示され、規定時間視聴すると報酬が得られます。
  - 報酬は累積表示されます。
- バナー、インタースティシャル、リワードの各広告の表示は相互に排他的です。

### 構成
- 汎用
  - `AdMobObj.cs`
    - 広告を制御するコンポーネント(物理広告クラス)です。
  - `AdMobApi.cs`
    - 論理広告クラスです。
  - `DodgeBanner.cs`
    - ゲームオブジェクトにアタッチすることで、表示状態に応じてバナーを避けるように制御します。
  - `SetCanvasBounds.cs`
    - ゲームオブジェクトにアタッチすることで、セーフアエリアに整合するように制御します。
  - `TaskEx.cs`
    - `System.Threading.Tasks`を拡張するクラスで、前述した一部のクラスが依存しています。
- 参考
  - `SampleScene.unity`
    - サンプルシーンです。
  - `SwitchPanel.cs`
    - サンプルシーンで広告を制御しています。

## 自分のプロジェクトでの使い方

### 概念
- 「ユニット」
  - このプロジェクトでは、「AdMobの広告ユニット」の他に、`AdMobApi`クラスとそのインスタンスの意味でも「ユニット」を使用します。
- 「セット」
  - 名前が付けられた広告ユニットのグループです。
    - 名前の文字列でセットを特定します。
    - 状況に応じてセットを切り替えて使用します。
  - 同時に表示可能な広告ユニットだけがセットに所属でき、セット同士は排他的に使用されます。
    - インタースティシャルやリワードは全画面表示なので、必ず単一ユニットのセットになります。
    - 同じサイズのバナーは、所属する全てのセットで、同じ位置に配置されなければなりません。

### 導入
#### プロジェクト
- プロジェクトに、[Google Mobile Ads Unity Plugin のパッケージを導入](#プラグインの導入方法)します。
  - 未導入だと次のステップでエラーが生じます。
- プロジェクトに、`AdMobObj.cs`と`AdMobApi.cs`を導入します。
  - シーンの適当なオブジェクトに`AdMobObj.cs`をアタッチします。
- 必要に応じて、AdMobコンソールで広告ユニットのIDを生成します。
  - 生成したIDを、インスペクタを使用してシーンの`AdMobObj`コンポーネントに設定します。
  - 広告ユニット生成の際には、テストデバイスを登録することをお勧めします。
    - テストデバイスでは、本番用のID(アプリおよびユニット)を使用してもテスト広告が表示され、安全にテストが可能です。

#### ネームスペース
以下のネームスペースを使用します。

```csharp
using GoogleMobileAds.Api;
using GoogleMobileAds.Utility;
```

#### 列挙型
- `GoogleMobileAds.Api.AdSize`
  - Googleにより、バナーのサイズが定義されています。
- `GoogleMobileAds.Api.AdPosition`
  - Googleにより、バナーの位置(主に、上、中央、下)が定義されています。

### 生成
- クラス`AdMobApi`のインスタンスが、広告ユニットのインスタンスです。
- インスタンスが生成されてから表示可能になるまでにはラグがあるので、あらかじめを準備しておく必要があります。
- 生成時にセット名を与え、以降は、その名前で特定します。

```csharp
  new AdMobApi ("banner", AdSize.SmartBanner, AdPosition.Bottom);
  new AdMobApi ("banner", AdSize.MediumRectangle, AdPosition.Center);
  new AdMobApi ("interstitial");
  new AdMobApi ("rewarded", OnAdRewarded);
```

#### バナー
- `new AdMobApi (string scene, AdSize size, AdPosition pos)`で、名前、サイズ、位置を与えて生成します。

#### インタースティシャル
- `new AdMobApi (string scene)`で、名前を与えて生成します。

#### リワード
- `new AdMobApi (string scene, EventHandler<Reward> OnAdRewarded)`で、名前と報酬獲得時の処理を与えて生成します。

### 破棄
- 二度と使用しないインスタンスは、メソッド`Destroy ()`で破棄できます。
- 通常は、破棄と生成を繰り返さず、表示を切り替えて使い回しますので、使用する必要はありません。

### 表示
- `AdMobApi.GetActive (string scene)`で状態を得て、`AdMobApi.SetActive (string scene, bool active = true)`で制御します。
  - この方法では、セット間で排他的に制御されます。
- プロパティ`ActiveSelf`で、ユニット個別に制御することも可能ですが、この方法では排他制御されません。

```csharp
  // バナー表示をスイッチ
  AdMobApi.SetActive ("banner", !AdMobApi.GetActive ("banner"));
```

### 報酬
- インスタンスの生成時に登録したコールバック先で報酬を得ます。
- コールバック先は例えば以下のようなものになります。

```csharp
  private void OnAdRewarded (object sender, Reward reward) {
    Debug.Log ($"Got {(int) reward.Amount} reward{((reward.Amount > 1)? "s" : "")}");
    coins += (int) reward.Amount;
    CoinsText.text = coins.ToString ();
  }
```

- なお、一度の視聴で得られる報酬の量(`reward.Amount`)は、AdMobnoコンソールで定義するユニットのパラメータの一つです。

## おわりに

説明が解りづらい、使いにくい、これがしたい、など何でも、コメントをお寄せいただけるとありがたいです。
最後までお読みいただきありがとうございました。
