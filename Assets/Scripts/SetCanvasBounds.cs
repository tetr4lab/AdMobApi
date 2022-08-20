using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
///	対象のオブジェクトをセーフエリアに整合する
///		オブジェクトは、ルートキャンバスの直下に、アンカー(0, 0)-(1, 1)、オフセット(0, 0)-(0, 0)で、配置されている必要がある。
///		オブジェクトの指定が無い場合は、コンポーネントがアタッチされているオブジェクトを対象にする
/// </summary>
public class SetCanvasBounds : MonoBehaviour {

	/// <summary>対象のオブジェクト</summary>
	[SerializeField] private RectTransform panel = default;
	/// <summary>水平方向の生後を行うかどうか</summary>
	[SerializeField] private bool HorizontalSafety = default;
	/// <summary>垂直方向の制御を行うかどうか</summary>
	[SerializeField] private bool VerticalSafety = default;

	private Rect lastSafeArea = Rect.zero;

	private void Start () {
		if (panel == null) {
			panel = GetComponent<RectTransform> ();
		}
	}

	private void Update () {
		if (panel != null) {
			Rect area = new Rect (Screen.safeArea);
			if (area != lastSafeArea) {
				var screenSize = new Vector2 (Screen.width, Screen.height);
				panel.anchorMin = new Vector2 (HorizontalSafety ? area.position.x / screenSize.x : 0, VerticalSafety ? area.position.y / screenSize.y : 0);
				panel.anchorMax = new Vector2 (HorizontalSafety ? (area.position.x + area.size.x) / screenSize.x : 1, VerticalSafety ? (area.position.y + area.size.y) / screenSize.y : 1);
				lastSafeArea = area;
			}
		}
	}

}
