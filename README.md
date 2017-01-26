WebSocketSample
===============

WebSocketSample for Unity

## Demo

WebSocketサーバーを使って自分と他プレイヤーの位置を同期するサンプル
青が自分, 赤が他プレイヤー

![](img/demo.gif "demo")

## Build

* cloneする時に--recursiveしていなければ
```
git submodule --init --recursive
```

* クライアント

Unityで実行

* サーバー
  - Debug
  ```
  xbuild WebSocketServer.sln
  ```
  - Release
  ```
  xbuild /p:Configuration=Release WebSocketServer.sln
  ```

## Run

* サーバー
```
cd Server/bin/Release
mono Server.exe
```

## Protocols

クライント/サーバー間のプロトコルの仕様

* アカウント登録(Client -> Server)
```
{
   "method": "register",
   "payload": {
		"name": "<プレイヤー名を入れる>"
   }
}
```

* アカウント登録レスポンス(Server -> Client)
```
{
   "method": "register_response",
   "payload": {
		"uid": <サーバーで生成されたプレイヤーIDが入ってくる>
   }
}
```

* ログイン(Client -> Server)
```
{
   "method": "login",
   "payload": {
		"uid": <プレイヤーID>,
		"name": "<プレイヤー名を入れる>"
   }
}
```

* 自プレイヤーの位置送信(Client -> Server)
```
{
   "method": "pos",
   "payload": {
		"uid": <プレイヤーID>,
		"x": <プレイヤーのX座標>,
		"y": <プレイヤーのY座標>,
		"z": <プレイヤーのZ座標>
   }
}
```

* 他プレイヤーの位置同期(Server -> Client)
```
{
   "method": "sync",
   "payload": {
		"players": [
			{ "uid": <プレイヤー1のID>, "x": <プレイヤー1のX座標>, "y": <プレイヤー1のY座標>, "z": <プレイヤー1のZ座標> },
			{ "uid": <プレイヤー2のID>, "x": <プレイヤー2のX座標>, "y": <プレイヤー2のY座標>, "z": <プレイヤー2のZ座標> },

			{ "uid": <プレイヤーnのID>, "x": <プレイヤーnのX座標>, "y": <プレイヤーnのY座標>, "z": <プレイヤーnのZ座標> },
		]
   }
}
```
	
