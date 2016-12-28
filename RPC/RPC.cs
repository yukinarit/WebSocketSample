using System;
using System.Collections.Generic;

namespace RollABall.RPC
{

[System.Serializable]
public class Header
{
	public string method;
}

[System.Serializable]
public class Pos
{
	public Pos(int uid, float x, float y, float z)
	{
		payload = new PosPayload(uid, x, y, z);
	}

	public string method = "pos";
	public PosPayload payload;
}

[System.Serializable]
public class PosPayload
{
	public PosPayload(int uid, float x, float y, float z)
	{
		this.uid = uid;
		this.x = x;
		this.y = y;
		this.z = z;
	}

	public int uid;
	public float x;
	public float y;
	public float z;
}

[System.Serializable]
public class Login
{
	public Login(int uid, string name)
	{
		payload = new LoginPayload(uid, name);
	}

	public string method = "login";
	public LoginPayload payload;
}

[System.Serializable]
public class LoginPayload
{
	public LoginPayload(int uid, string name)
	{
		this.uid = uid;
		this.name = name;
	}

	public int uid;
	public string name;
}

[System.Serializable]
public class Sync
{
	public string method = "sync";
	public SyncPayload payload = new SyncPayload();

	public override string ToString()
	{
		return "method=" + method + ", payload=" + payload.ToString();
	}
}

[System.Serializable]
public class SyncPayload
{
	public List<Player> players = new List<Player>();

	public override string ToString()
	{
		var msg = "";
		foreach (var p in players)
		{
			msg += p.ToString() + " ";
		}
		return msg;
	}
}

[System.Serializable]
public class Player
{
	public int uid = 0;
	public float x = 0;
	public float y = 0;
	public float z = 0;

	public Player()
	{
	}

	public Player(int uid, float x, float y, float z)
	{
		this.uid = uid;
		this.x = x;
		this.y = y;
		this.z = z;
	}

	public override string ToString()
	{
		var msg = string.Format(
			"Player({0},{1},{2},{3})",
			uid, x, y, z
		);
		return msg;
	}
}

[System.Serializable]
public class Register
{
	public Register(string name)
	{
		payload = new RegisterPayload(name);
	}

	public string method = "register";
	public RegisterPayload payload;
}

[System.Serializable]
public class RegisterPayload
{
	public RegisterPayload(string name)
	{
		this.name = name;
	}

	public string name;
}

[System.Serializable]
public class RegisterResponse
{
    public RegisterResponse(int uid)
    {
        this.payload = new RegisterResponsePayload(uid);
    }

	public string method = "register_response";
	public RegisterResponsePayload payload;
}

[System.Serializable]
public class RegisterResponsePayload
{
    public RegisterResponsePayload(int uid)
    {
        this.uid = uid;
    }
	public int uid;
}


[System.Serializable]
public class Ping
{
	public Ping(string name)
	{
		payload = new PingPayload(name);
	}

	public string method = "ping";
	public PingPayload payload;
}

[System.Serializable]
public class PingPayload
{
	public PingPayload(string name)
	{
		this.name = name;
	}

	public string name;
}

} // namespace RollABall

