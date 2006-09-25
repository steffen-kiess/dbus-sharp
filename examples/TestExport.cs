// Copyright 2006 Alp Toker <alp@atoker.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Collections.Generic;
using NDesk.DBus;
using org.freedesktop.DBus;

public class ManagedDBusTestExport
{
	public static void Main ()
	{
		Connection conn = new Connection ();

		//begin ugly bits
		ObjectPath opath = new ObjectPath ("/org/freedesktop/DBus");
		string name = "org.freedesktop.DBus";

		Bus bus = conn.GetObject<Bus> (name, opath);

		bus.NameAcquired += delegate (string acquired_name) {
			Console.WriteLine ("NameAcquired: " + acquired_name);
		};

		string myName = bus.Hello ();
		Console.WriteLine ("myName: " + myName);


		ObjectPath myOpath = new ObjectPath ("/org/ndesk/test");
		string myNameReq = "org.ndesk.test";

		DemoObject demo;

		if (bus.NameHasOwner (myNameReq)) {
			demo = conn.GetObject<DemoObject> (myNameReq, myOpath);
		} else {
			NameReply nameReply = bus.RequestName (myNameReq, NameFlag.None);

			Console.WriteLine ("nameReply: " + nameReply);

			demo = new DemoObject ();
			conn.Marshal (demo, myNameReq, myOpath);

			while (true)
				conn.Iterate ();
		}
		//end ugly bits

		demo.Say ("Hello world!");
		Console.WriteLine (demo.EchoCaps ("foo bar"));
		Console.WriteLine (demo.GetEnum ());
		demo.CheckEnum (DemoEnum.Bar);
		demo.CheckEnum (demo.GetEnum ());

		/*
		Console.WriteLine ();
		string outVal;
		demo.ReturnOut (out outVal);
		Console.WriteLine ("outVal: " + outVal);
		*/

		Console.WriteLine ();
		string[] texts = {"one", "two", "three"};
		texts = demo.EchoCapsArr (texts);
		foreach (string text in texts)
			Console.WriteLine (text);

		Console.WriteLine ();
		string[][] arrarr = demo.ArrArr ();
		Console.WriteLine (arrarr[1][0]);

		Console.WriteLine ();
		int[] vals = demo.TextToInts ("1 2 3");
		foreach (int val in vals)
			Console.WriteLine (val);

		Console.WriteLine ();
		MyTuple fooTuple = demo.GetTuple ();
		Console.WriteLine ("A: " + fooTuple.A);
		Console.WriteLine ("B: " + fooTuple.B);

		Console.WriteLine ();
		//KeyValuePair<string,string>[] kvps = demo.GetDict ();
		IDictionary<string,string> dict = demo.GetDict ();
		foreach (KeyValuePair<string,string> kvp in dict)
			Console.WriteLine (kvp.Key + ": " + kvp.Value);

		Console.WriteLine ();
		demo.SomeEvent += delegate (string arg1, int arg2, double arg3, MyTuple mt) {Console.WriteLine ("SomeEvent handler: " + arg1 + ", " + arg2 + ", " + arg3 + ", " + mt.A + ", " + mt.B);};
		demo.FireOffSomeEvent ();
		//handle the raised signal
		conn.Iterate ();
	}
}

[Interface ("org.ndesk.test")]
public class DemoObject : MarshalByRefObject
{
	public void Say (string text)
	{
		Console.WriteLine (text);
	}

	public string EchoCaps (string text)
	{
		return text.ToUpper ();
	}

	public void CheckEnum (DemoEnum e)
	{
		Console.WriteLine (e);
	}

	public DemoEnum GetEnum ()
	{
		return DemoEnum.Bar;
	}

	//this doesn't work yet, except for introspection
	public DemoEnum EnumState
	{
		get {
			return DemoEnum.Bar;
		} set {
			Console.WriteLine ("EnumState prop set to " + value);
		}
	}

	/*
	public void ReturnOut (out string val)
	{
		val = "out value";
	}
	*/

	public string[] EchoCapsArr (string[] texts)
	{
		string[] retTexts = new string[texts.Length];

		for (int i = 0 ; i != texts.Length ; i++)
			retTexts[i] = texts[i].ToUpper ();

		return retTexts;
	}

	public string[][] ArrArr ()
	{
		string[][] ret = new string[2][];

		ret[0] = new string[] {"one", "two"};
		ret[1] = new string[] {"three", "four"};

		return ret;
	}

	public int[] TextToInts (string text)
	{
		string[] parts = text.Split (' ');
		int[] rets = new int[parts.Length];

		for (int i = 0 ; i != parts.Length ; i++)
			rets[i] = Int32.Parse (parts[i]);

		return rets;
	}

	public MyTuple GetTuple ()
	{
		MyTuple tup;

		tup.A = "alpha";
		tup.B = "beta";

		return tup;
	}

	public IDictionary<string,string> GetDict ()
	{
		Dictionary<string,string> dict = new Dictionary<string,string> ();

		dict["one"] = "1";
		dict["two"] = "2";

		return dict;
	}

	/*
	public KeyValuePair<string,string>[] GetDict ()
	{
		KeyValuePair<string,string>[] rets = new KeyValuePair<string,string>[2];

		//rets[0] = new KeyValuePair<string,string> ("one", "1");
		//rets[1] = new KeyValuePair<string,string> ("two", "2");

		rets[0] = new KeyValuePair<string,string> ("second", " from example-service.py");
		rets[1] = new KeyValuePair<string,string> ("first", "Hello Dict");

		return rets;
	}
	*/

	public event SomeEventHandler SomeEvent;

	public void FireOffSomeEvent ()
	{
		Console.WriteLine ("Asked to fire off SomeEvent");

		MyTuple mt;
		mt.A = "a";
		mt.B = "b";

		if (SomeEvent != null) {
			SomeEvent ("some string", 21, 19.84, mt);
			Console.WriteLine ("Fired off SomeEvent");
		}
	}
}

public enum DemoEnum
{
	Foo,
	Bar,
}


public struct MyTuple
{
	public string A;
	public string B;
}

public delegate void SomeEventHandler (string arg1, int arg2, double arg3, MyTuple mt);