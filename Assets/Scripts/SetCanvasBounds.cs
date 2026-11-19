using UnityEngine;

/// <summary>セーフエリアへの整合種別</summary>
public enum SetCanvasBoundsType {
    /// <summary>内側</summary>
    Inner,
    /// <summary>上の外側</summary>
    OuterTop,
    /// <summary>下の外側</summary>
    OuterBottom,
    /// <summary>左の外側</summary>
    OuterLeft,
    /// <summary>右の外側</summary>
    OuterRight,
}

/// <summary>対象のオブジェクトをセーフエリアに整合する</summary>
[DisallowMultipleComponent]
public class SetCanvasBounds : MonoBehaviour {
    /// <summary>整合対象</summary>
    [SerializeField]
    [Tooltip ("セーフエリアに整合させる対象を指定してください。\n無指定なら自身がアタッチされたオブジェクトを対象にします。\n対象のLeft/Right/Top/Bottomは`0`にしてください。\nCanvasは画面全体と一致させてください。")]
    private RectTransform target = default;

    /// <summary>種別</summary>
    [SerializeField]
    [Tooltip ("セーフエリアとの位置関係を指定してください。")]
    private SetCanvasBoundsType type;

    /// <summary>最後に認識した画面サイズ</summary>
    private Vector2 lastScreenSize;

    /// <summary>最後に検出したセーフエリア</summary>
    private Rect lastSafeArea;

    /// <summary>起動</summary>
    private void Awake () {
        if (target == null) {
            target = GetComponent<RectTransform> ();
        }
    }

    /// <summary>再開</summary>
    private void OnEnable () {
        lastScreenSize = Vector2.zero;
        lastSafeArea = Rect.zero;
        UpdateBounds ();
    }

    /// <summary>更新</summary>
    private void Update () {
        UpdateBounds ();
    }

    /// <summary>設定</summary>
    private void UpdateBounds () {
        if (target != null && Screen.width > 0 && Screen.height > 0) {
            var screenSize = new Vector2 (Screen.width, Screen.height);
            var safeArea = Screen.safeArea;
            if (screenSize != lastScreenSize || safeArea != lastSafeArea) {
                var anchorMin = safeArea.position / screenSize;
                var anchorMax = (safeArea.position + safeArea.size) / screenSize;
                switch (type) {
                    case SetCanvasBoundsType.Inner:
                        target.anchorMin = anchorMin;
                        target.anchorMax = anchorMax;
                        break;
                    case SetCanvasBoundsType.OuterTop:
                        target.anchorMin = new Vector2 (0f, anchorMax.y);
                        target.anchorMax = Vector2.one;
                        break;
                    case SetCanvasBoundsType.OuterBottom:
                        target.anchorMin = Vector2.zero;
                        target.anchorMax = new Vector2 (1f, anchorMin.y);
                        break;
                    case SetCanvasBoundsType.OuterLeft:
                        target.anchorMin = Vector2.zero;
                        target.anchorMax = new Vector2 (anchorMin.x, 1f);
                        break;
                    case SetCanvasBoundsType.OuterRight:
                        target.anchorMin = new Vector2 (anchorMax.x, 0f);
                        target.anchorMax = Vector2.one;
                        break;
                }
                lastScreenSize = screenSize;
                lastSafeArea = safeArea;
            }
        }
    }
}
