---
title: Unity で Google Mobile Ads (AdMob) を使う
tags: Unity AdMob Android iOS C#
---
## 前提
- Unity 2022.3.51f1
- Google Mobile Ads Unity Plugin v9.1.1
- Apple App Store、Google Play Store
- この記事では、Google Mobile Ads Unity Pluginの一部機能を限定的に使用し、汎用性のない部分があります。
- この記事では、以下の内容を扱いません。
  - 各ストアのルールやコンソールの使い方など
  - Google Mobile Adsのルールやコンソールの使い方など
  - Unityエディタの使い方やモバイル向けのビルド方法など
- この記事のソースは、実機でテストしていますが、本番広告でのテストはしていません。
  - テストは限定的で、デバイスやシチュエーションなどを網羅していません。
  - プロジェクトで使用したIDはテスト用のもので、ストアで公開する製品に使用できるものではありません。

## できること
- AndroidまたはiOSで、AdMobを利用します。
- 複数のバナー、インタースティシャル、リワードビデオ広告の表示を制御し、状況に応じて広告を切り替えます。
- 報酬の獲得を検出します。

### 制限事項
- `Project Settings` > `Player` > `Resolution Scaling` > `Resolution Scaling Mode` が`Fixed DPI`だと、バナーのサイズを正しく取得できない可能性が高いです。
    - `Disabled`での使用を推奨します。

## Google Mobile Ads Unity Plugin
- [ドキュメント (Google)](https://developers.google.com/admob/unity/quick-start)
- [リリース (GitHub)](https://github.com/googleads/googleads-mobile-unity/releases)

### プラグインの導入方法
- サンプルプロジェクトを最初に開いた場合など、Unityエディタ上にエラーが出ている場合は、[サンプルプロジェクトの使い方 > 導入](#サンプルプロジェクトの使い方)を参照してエラーを解消してください。
    - エラーが出ている状態でプラグインを導入すると、正常に機能しない場合があります。
- 上記の「リリース」からGoogle Mobile Ads Unity Plugin のパッケージ(`.unitypackage`)をダウンロードします。
- `Assets` >  `Import Package` > `Custom Package...` でパッケージを導入します。
- `Assets` > `External Dependency Manager` > `XXXXX Resolver` > `Resolve` を実施します。
- `Assets` > `Google Mobile Ads` > `Settings...` で、AppIDを設定します。
  - 以下のAppIDが、テスト用にGoogleから提供されています。
    - Android: `ca-app-pub-3940256099942544~3347511713`
    - iOS:     `ca-app-pub-3940256099942544~1458002511`
  - 必要に応じて、AdMobのコンソールで本番IDを取得してください。

## サンプルプロジェクトの使い方
### 導入
- このサンプルプロジェクトは、Google Mobile Ads Unity Pluginを含まないため、未導入の状態でリポジトリをクローンしてエディタで開こうとするとエラーします。
    - エラーが生じている場合は、`Edit` > `Project Settings...` > `Player` > `Other Settings` > `Scripting Define Symbols` で、`ALLOW_ADS`を`_ALLOW_ADS`にして`Apply`ボタンを押してください。
- エラーのなくなった状態で、[プラグインを導入](#プラグインの導入方法)して、AppIDの設定まで終えてください。
- プラグインの導入が完了したら、シンボル`ALLOW_ADS`を戻して、忘れずに`Apply`ボタンを押してください。

### 機能
- 実行すると、テストバナーを表示します。
- バナーボタンを押すと、バナーの表示状態を切り替えます。
  - 右の切り替えボタンを押すとバナーのサイズを切り替えます。
- インタースティシャルボタンを押すと、全画面広告が表示されます。
- リワードボタンを押すと全画面広告が表示され、規定時間視聴すると報酬が得られます。
  - 報酬は累積表示されます。
- バナー、インタースティシャル、リワードの各広告の表示は相互に排他的です。

### ファイル構成
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

#### EEA域外でUMPのテストを行う
- シンボル`DEBUG_GEOGRAPHY_EEA`が定義されていると、対象領域の外でもUMPのダイアログが表示されるようになります。
- なお、AdMobコンソールの同意管理プラットフォーム(CMP)で「GDPRが適用される国」**以外**もターゲットに指定した場合は、この設定に拠らず常にダイアログが表示されます。

## 自分のプロジェクトでの使い方

### 概念
- 《ユニット》
  - 論理広告クラス(`AdMobApi`)のインスタンスです。
  - 《シーン》と、シーン毎に0から始まる「ユニット番号」で特定されます。
  - 「AdMobの広告ユニット」は「広告ユニット」と表記します。
- 《シーン》
  - 文字列の名前を持った《ユニット》のグループです。
    - 名前で特定されます。
    - 所属する《ユニット》には、《シーン》内でユニークな「ユニット番号」が与えられます。
  - 同時に表示可能な《ユニット》だけが同一《シーン》に所属でき、個々の《シーン》は排他的に使用されます。
    - インタースティシャルやリワードは全画面表示なので、同一《シーン》に所属できる《ユニット》はひとつだけになります。
    - プラグイン側の制約で、同一サイズのバナーは、所属する全ての《シーン》で、同じ位置に配置されなければなりません。
      - 例えば、ある《シーン》で`AdSize.Banner`のバナーが`AdPosition.Bottom`に配置された場合は、他の全ての《シーン》でも`AdPosition.Bottom`に配置されなければなりません。
  - 「Unityの`Scene Asset`」は、単に「シーン」と表記します。

### 導入
#### プロジェクト
- プロジェクトに、[Google Mobile Ads Unity Plugin のパッケージを導入](#プラグインの導入方法)します。
- `Package Manager`で`Add package from git URL...`から、`https://github.com/tetr4lab/AdMobApi.git?path=/Assets/AdMobApi`を入力します。
  - シーンの適当なオブジェクトに`AdMobObj.cs`をアタッチします。
    - `Donot Destroy`で使用する場合は、ルートオブジェクトにアタッチしてください。
- 必要に応じて、AdMobコンソールで広告ユニットのIDを生成します。
  - 生成したIDを、インスペクタを使用してシーンの`AdMobObj`コンポーネントに設定します。
  - 生成した広告ユニットを使用する際には、テストデバイスを登録することをお勧めします。
    - テストデバイスでは、本番用のIDを使用してもテスト広告が表示され、安全にテストが可能です。
- シンボル`ALLOW_ADS`を定義して、広告を使用するコードを有効にしてください。

#### ネームスペース
以下のネームスペースを使用します。

```csharp
using GoogleMobileAds.Api;
using GoogleMobileAds.Utility;
```
#### シンボル

- `ALLOW_ADS`
  - シンボルが定義されている場合のみ広告を使用するコードが有効になります。

#### 列挙型
- `GoogleMobileAds.Api.AdSize`
  - Googleにより、バナーのサイズが定義されています。
- `GoogleMobileAds.Api.AdPosition`
  - Googleにより、バナーの位置(主に、上、中央、下)が定義されています。

### 初期化
- `AdMobApi.Allow`を真にするまで初期化は実行されません。
    - 通常は、オプトイン後に初期化を行います。
- 初期化の完了は`AdMobApi.Acceptable`でチェックできます。

### 生成
- 《ユニット》の生成時に《シーン》の名前を与え、以降は、その名前で特定します。
  - 通常は、生成した`AdMobApi`インスタンスを保持する必要はありません。
  - バナーは、同一の《シーン》に複数の《ユニット》を割り当てることができます。
- 《ユニット》の制御は《シーン》単位で行います。
- 《ユニット》が生成されてから表示可能になるまでにはラグがあるので、あらかじめを生成しておく必要があります。

```csharp
  new AdMobApi ("banner", true);
  new AdMobApi ("banner", AdSize.MediumRectangle, AdPosition.Center);
  new AdMobApi ("interstitial");
  new AdMobApi ("rewarded", reward => coins += (int) reward.Amount);
```

#### バナー
- `new AdMobApi (string scene, bool bottom);`で、名前、下端(!上端)を与えて、アンカーアダプティブバナーを生成します。
- `new AdMobApi (string scene, AdSize size, AdPosition pos)`で、名前、サイズ、位置を与えて生成します。

#### インタースティシャル
- `new AdMobApi (string scene)`で、名前を与えて生成します。

#### リワード
- `new AdMobApi (string scene, Action<Reward> OnAdRewarded)`で、名前と報酬獲得時の処理を与えて生成します。

### 表示の制御
- `AdMobApi.GetActive (string scene)`で状態を取得できます。
- `AdMobApi.SetActive (string scene, bool active = true)`で状態を制御できます。
  - 《シーン》間で排他的に制御されます。

```csharp
  // バナーをOn/Off
  AdMobApi.SetActive ("banner", !AdMobApi.GetActive ("banner"));
```

### 報酬の取得
- 《ユニット》の生成時に登録したコールバック先で報酬を得ます。
- コールバック先は例えば以下のようなものになります。

```csharp
  private void OnAdRewarded (object sender, Reward reward) {
    Debug.Log ($"Got {(int) reward.Amount} reward{((reward.Amount > 1)? "s" : "")}");
    coins += (int) reward.Amount;
    CoinsText.text = coins.ToString ();
  }
```

- なお、一度の視聴で得られる報酬の量(`reward.Amount`)は、AdMobコンソールで定義する広告ユニットのパラメータの一つです。

### 破棄
- 再利用しない《ユニット》は、`instance.Destroy ()`で破棄できます。
- 再利用しない《シーン》は、`AdMobApi.Destroy (string scene)`で破棄できます。
- 通常は、破棄と生成を繰り返さず、表示を制御して使い回します。

## 追加のアセット
このリポジトリでは、以下のパッケージも提供しています。

### TaskEx
`System.Threading.Tasks.Task`のヘルパークラスです。
```
https://github.com/tetr4lab/AdMobApi.git?path=/Assets/TaskEx
```

#### ネームスペース
```csharp
using Tetr4lab.TaskEx;
```

#### 使用例
```csharp
await TaskEx.DelayUntil (() => AdMobApi.Acceptable);
```
初期化の完了(`AdMobApi.Acceptable`が真になる)まで待機します。

```csharp
await TaskEx.DelayWhile (() => newScreenSize.x == Screen.width && newScreenSize.y == Screen.height, ChangeScreenDelayTime);
```
一定時間、画面サイズの変化を待機します。
画面サイズが変化した後、この待機を実施して、さらに変化をチェックすることで、画面サイズの変化が安定したかどうかを検出しています。

## おわりに

説明が解りづらい、使いにくい、これがしたい、など何でも、コメントをお寄せいただけるとありがたいです。
最後までお読みいただきありがとうございました。
