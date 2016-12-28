WebSocketSample
===============

WebSocketSample for Unity

## Build

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
