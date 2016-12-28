using System;
using System.Threading;
using System.Collections.Generic;
using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json;

using WebSocketSample.RPC;


namespace WebSocketSample.Server
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			var host = "ws://localhost";
			var port = 5678;
			var address = host + ":" + port.ToString();

			var gameServer = GameServer.GetInstance(address);
			gameServer.RunForever();
		}
	}

	public class GameServer
	{
		const int FPS = 30;

		const float FRAME_SEC = 1 / FPS;

		const string EXIT_KEY = "q";

		const string DEFAULT_ADDRESS = "ws//localhost:5678";

		string address;

		Dictionary<int, Player> players = new Dictionary<int, Player>();

		WebSocketServer ws;

		UIDGenerator uidGenerator = new UIDGenerator();

		static GameServer sv;

		Queue<Action> actions = new Queue<Action>(); // 非同期タスク

		private GameServer(string address)
		{
			this.address = address;
			ws = new WebSocketServer(address);
			ws.AddWebSocketService<WebSocketSampleService>("/");
		}

		static public GameServer GetInstance(string address = DEFAULT_ADDRESS)
		{
			if (sv == null)
			{
				sv = new GameServer(address);
			}
			return sv;
		}

		public WebSocketServer GetWebSocketServer()
		{
			return ws;
		}

		public void RunForever()
		{
			ws.Start();
			Console.WriteLine("Game Server started.");

			try
			{
				while (true)
				{
					PollKey();
					UpdateActions();
					Sync();
				}
			}
			catch (GameExit ex)
			{
			}
			catch (Exception ex)
			{
			}

			ws.Stop();
			Console.WriteLine("Game Server terminated.");
		}

		void Sync()
		{
			if (players.Count == 0)
			{
				return;
			}

			var playerPositions = new List<global::WebSocketSample.RPC.Player>();
			foreach (var kv in players)
			{
				var uid = kv.Key;
				var player = kv.Value;
				if (!player.PosChanged)
				{
					continue;
				}
				playerPositions.Add(
					new global::WebSocketSample.RPC.Player(
						player.uid,
						player.x,
						player.y,
						player.z
					)
				);
				player.PosChanged = false;
			}
			if (playerPositions.Count == 0)
			{
				return;
			}

			var sync = new Sync();
			sync.payload = new SyncPayload();
			sync.payload.players = playerPositions;

			var msg = JsonConvert.SerializeObject(sync);

			broadcast(msg);
		}

		void PollKey()
		{
			if (Console.KeyAvailable)
			{
				var key = Console.ReadKey(true);
				if (key.Key.ToString().ToLower() == EXIT_KEY)
				{
					throw new GameExit();
				}
				else
				{
					Console.WriteLine("Enter q to exit the game.");
				}
			}
		}

		// メインスレッドで実行するためのキューに入れる
		public void RunOnMainThread(Action action)
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

		public void Register(string senderId, MessageEventArgs e)
		{
			Console.WriteLine(">> Register");

			var register = JsonConvert.DeserializeObject<Register>(e.Data);

			var msg = JsonConvert.SerializeObject(
				new RegisterResponse(
					uidGenerator.generate()
				)
			);

			SendTo(senderId, msg);

			Console.WriteLine("<< Register Response");
		}

		public void Login(string senderId, MessageEventArgs e)
		{
			Console.WriteLine(">> Login");

			var login = JsonConvert.DeserializeObject<Login>(e.Data);

			var player = new Player(
				login.payload.uid,
				login.payload.name
			);

			players[login.payload.uid] = player;

			Console.WriteLine(player.ToString() + " login.");
		}

		public void Pos(string senderId, MessageEventArgs e)
		{
			Console.WriteLine(">> Pos");

			var pos = JsonConvert.DeserializeObject<Pos>(e.Data);
			if (players.ContainsKey(pos.payload.uid) || pos.payload.uid != 0)
			{
				var player = players[pos.payload.uid];
				player.SetPos(
					pos.payload.x,
					pos.payload.y,
					pos.payload.y
				);
			}
		}

		public void SendTo(string id, string msg)
		{
			Console.WriteLine("SendTo: " + id + " " + msg);
			var ws = GameServer.GetInstance().GetWebSocketServer();
			ws.WebSocketServices["/"].Sessions.SendTo(
				msg, id
			);
		}

		void broadcast(string msg)
		{
			Console.WriteLine("broeadcast: " + msg);
			var ws = GameServer.GetInstance().GetWebSocketServer();
			ws.WebSocketServices["/"].Sessions.Broadcast(msg);
		}
	}

	class UIDGenerator
	{
		int counter = 0;

		public int generate()
		{
			counter++;
			return counter;
		}
	}

	public class WebSocketSampleService : WebSocketBehavior
	{
		protected override void OnOpen()
		{
			Console.WriteLine("WebSocket opened.");
		}

		protected override void OnClose(CloseEventArgs e)
		{
			Console.WriteLine("WebSocket Close.");
		}

		protected override void OnMessage(MessageEventArgs e)
		{
			Console.WriteLine("WebSocket Message: " + e.Data);
			try
			{
				// メッセージディスパッチ
				DispatchMethod(e);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine(ex.StackTrace);
			}
		}

		protected override void OnError(ErrorEventArgs e)
		{
			Console.WriteLine("WebSocket Error: " + e);
		}

		// メソッドディスパッチ
		void DispatchMethod(MessageEventArgs e)
		{
			// ヘッダ解析
			var header = JsonConvert.DeserializeObject<Header>(e.Data);
			Console.WriteLine("Header: " + header.method);

			var sv = GameServer.GetInstance();
			var senderId = ID;

			if (header.method == "ping")
			{
				sv.SendTo(
					senderId,
					"{" +
					    @"""method"": ""pong""" +
					"}"
				);

				Console.WriteLine("<< pong");
			}
			else if (header.method == "register")
			{
				sv.RunOnMainThread(() => {
					sv.Register(senderId, e);
				});
			}
			else if (header.method == "login")
			{
				sv.RunOnMainThread(() => {
					sv.Login(senderId, e);
				});
			}
			else if (header.method == "pos")
			{
				sv.RunOnMainThread(() => {
					sv.Pos(senderId, e);
				});
			}
		}

	}

	/*
	struct Pos
	{
		public float x { get; set; }
		public float y { get; set; }
		public float z { get; set; }
	};
	*/

	class Player
	{
		public Player(int uid, string name,
			float x=0.0f, float y=0.0f, float z=0.0f)
		{
			this.uid = uid;
			this.name = name;
			this.x = x;
			this.y = y;
			this.z = z;
			this.PosChanged = false;
		}

		public void SetPos(float x, float y, float z)
		{
			if (this.x != x || this.y != y || this.z != z)
			{
				this.x = x;
				this.y = y;
				this.z = z;
				PosChanged = true;
			}
		}

		public override string ToString()
		{
			return string.Format(
				"<Player(uid={0},name={1},x={2},y={3},z={4})>",
				uid,
				name,
				x, y, z
			);
		}

		public int uid;
		public string name;
		public float x;
		public float y;
		public float z;
        public bool PosChanged { get; set; }
	}

	enum Status
	{
		OK = 0,
		GameExit
	};

	class GameException : Exception
	{
		Status code;

	    public GameException()
	    {
	    }

	    public GameException(string message, Status code = Status.OK)
	        : base(message)
	    {
			this.code = code;
		}
	}

	class GameExit : GameException
	{
	    public GameExit()
			: base("Game exit.", Status.GameExit)
	    {
	    }
	}
}
