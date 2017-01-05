using UnityEngine;
using System;
using System.Collections;

using WebSocketSharp;

using WebSocketSample.RPC;

namespace WebSocketSample
{

public class MainController : MonoBehaviour
{
	WebSocket ws; // WebSocketコネクション

	DateTime lastBroadcasted = DateTime.Now; // 最後にbroadcastした時刻

	void Start()
	{
		// WebSocketのコネクション作成
		ws = new WebSocket("ws://localhost:5678");

		//
		// ハンドラの設定
		//

		// コネクション確立したときのハンドラ
		ws.OnOpen += (sender, e) =>
		{
			Debug.Log("WebSocket opened.");

			// Ping
			ws.Send(
				@"{
					""method"": ""ping""
				}"
			);
		};

		// メッセージを受信したときのハンドラ
		ws.OnMessage += (sender, e) =>
		{
			Debug.Log("WebSocket Message: " + e.Data);
		};

		// エラーが発生したときのハンドラ
		ws.OnError += (sender, e) =>
		{
			Debug.Log("WebSocket Error Message: " + e.Message);
		};

		// コネクションを閉じたときのハンドラ
		ws.OnClose += (sender, e) =>
		{
			Debug.Log("WebSocket Close: " + e.Reason);
		};

		// サーバーへ接続
		ws.Connect();
	}

	void Update()
	{
		var now = DateTime.Now;
		var d = now - lastBroadcasted;

		// 3秒毎にブロードキャストする
		if (d.Seconds >= 3)
		{
			// Broadcast
			var broadcast = new Broadcast("Hello world!");
			var serialized = JsonUtility.ToJson(broadcast);
			ws.Send(serialized);

			lastBroadcasted = now;
		}
	}
}

} // namespace WebSocketSample
