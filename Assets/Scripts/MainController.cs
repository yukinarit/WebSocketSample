using UnityEngine;
using System;
using System.Collections.Generic;

using WebSocketSharp;
using WebSocketSample.RPC;

namespace WebSocketSample
{

public class MainController : MonoBehaviour
{
	WebSocket ws; // WebSocketコネクション
	Queue<Action> actions = new Queue<Action>(); // 非同期タスク

	GameObject player; // プレイヤー
	string name; // プレイヤー名
	int playerId; // プレイヤーID
	Vector3 prevPosition; // 一つ前の位置
	Dictionary<int, GameObject> otherPlayers = new Dictionary<int, GameObject>(); // 他プレイヤー

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
		};

		// メッセージを受信したときのハンドラ
		ws.OnMessage += (sender, e) =>
		{
			Debug.Log("WebSocket Message: " + e.Data);
			try
			{
				// メッセージディスパッチ
				DispatchMethod(e);
			}
			catch (Exception ex)
			{
				Debug.LogError(ex.Message);
				Debug.LogError(ex.StackTrace);
			}
		};

		// エラーが発生したときのハンドラ
		ws.OnError += (sender, e) =>
		{
			Debug.Log("WebSocket Error Message: " + e.Message);
		};

		// コネクションを閉じたときのハンドラ
		ws.OnClose += (sender, e) =>
		{
			Debug.Log("WebSocket Close");
		};

		// サーバーへ接続
		ws.Connect();

		// Player生成
		var playerPrefab = (GameObject)Resources.Load("Player");
		player = (GameObject)Instantiate(
			playerPrefab,
			new Vector3(0.0f, 0.5f, 0.0f),
			Quaternion.identity
		);

		// Player名
		name = "yukianri";

		// アカウント登録
		Register(player);
	}

	// 毎フレーム更新
	void Update()
	{
		UpdatePosition();
		UpdateActions();
	}

	// プレイヤー位置更新
	void UpdatePosition()
	{
		if (player != null && prevPosition != player.transform.position)
		{
			var current = player.transform.position;
			var msg = String.Format(
				@"{{
					""method"": ""pos"",
					""payload"": {{
						""uid"": ""{0}"",
						""x"": ""{1}"",
						""y"": ""{2}"",
						""z"": ""{3}""
					}}
				}}",
				playerId,
				current.x,
				current.y,
				current.z
			);
			Debug.Log("JSON: " + msg);

			// サーバーに位置送信
			ws.Send(msg);

			prevPosition = current;
		}
	}

	// メソッドディスパッチ
	void DispatchMethod(MessageEventArgs e)
	{
		// ヘッダ解析
		var header = JsonUtility.FromJson<Header>(e.Data);

		if (header.method == "sync")
		{
			RunOnMainThread(() => {
				Sync(e);
			});
		}
		else if (header.method == "register_response")
		{
			RunOnMainThread(() => {
				OnRegisterResponse(e);
			});
		}
	}

	// メインスレッドで実行するためのキューに入れる
	void RunOnMainThread(Action action)
	{
		lock (actions)
		{
			actions.Enqueue(action);
		}
	}

	// キュー内のタスク実行
	void UpdateActions()
	{
		while (true)
		{
			lock (actions)
			{
				if (actions.Count == 0)
					break;

				var action = actions.Dequeue();
				action();
			}
		}
	}

	// ゲームアカウント登録
	void Register(GameObject player)
	{
		Debug.Log(">> Register");

		var msg = String.Format(
			@"{{
				""method"": ""register"",
				""payload"": {{
					""name"": ""{0}""
				}}
			}}",
			name
		);
		Debug.Log("JSON: " + msg);
		ws.Send(msg);
	}

	// ゲームアカウント登録レスポンス
	void OnRegisterResponse(MessageEventArgs e)
	{
		Debug.Log("<< RegisterResponse");
		var rv = JsonUtility.FromJson<RegisterResponse>(e.Data);
		Debug.Log(rv);

		// サーバーからもらったPlayerIDをセット
		playerId = rv.payload.uid;

		// ログイン
		Login(player);
	}

	// ゲームログイン
	void Login(GameObject player)
	{
		Debug.Log(">> Login");

		var msg = String.Format(
			@"{{
				""method"": ""login"",
				""payload"": {{
					""uid"": ""{0}"",
					""name"": ""{1}""
				}}
			}}",
			playerId,
			name
		);
		Debug.Log("JSON: " + msg);
		ws.Send(msg);
	}

	// サーバーからの座標同期リクエスト
	void Sync(MessageEventArgs e)
	{
		Debug.Log("<< Sync");

		var sync = JsonUtility.FromJson<Sync>(e.Data);
		Debug.Log(sync);
		foreach (var p in sync.payload.players)
		{
			// 自分の座標は要らない
			if (p.uid == playerId)
				continue;

			if (otherPlayers.ContainsKey(p.uid))
			{
				// すでにGameObjectが居たら位置更新
				var other = otherPlayers[p.uid];
				var newPos = new Vector3(p.x, p.y, p.z);
				if (other.transform.position != newPos)
				{
					Debug.Log("newPos:" + newPos);
					other.transform.position = newPos;
				}
			}
			else
			{
				// GameObjectが居なかったら新規作成
				var otherPlayerPrefab = (GameObject)Resources.Load("OtherPlayer");
				if (otherPlayerPrefab)
				{
					var otherPlayer = (GameObject)Instantiate(
						otherPlayerPrefab,
						new Vector3(p.x, p.y, p.z),
						Quaternion.identity
					);
					otherPlayers.Add(p.uid, otherPlayer);
					Debug.Log("Instantiated a new player: " + p.uid);
				}
			}
		}
	}

}

} // namespace WebSocketSample
