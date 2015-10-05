/* part of Pyrolite, by Irmen de Jong (irmen@razorvine.net) */

using System;
using System.Collections.Generic;
using System.Text;
using Razorvine.Pickle;
using Razorvine.Pyro;

namespace Pyrolite.TestPyroNaming
{

/// <summary>
/// Test Pyro with the Pyro name server.
/// </summary>
public class TestNaming {

	static protected byte[] hmacKey = null;

	public static void Main(String[] args)  {
		try {
			Test();
		} catch (Exception x) {
			Console.WriteLine("unhandled exception: {0}",x);
		}
	}
	
	public static void Test() {

		Console.WriteLine("Testing Pyro nameserver connection (make sure it's running with a broadcast server)...");
		Console.WriteLine("Pyrolite version: "+Config.PYROLITE_VERSION);

		setConfig();
		Console.WriteLine("serializer used: {0}", Config.SERIALIZER);
		if(Config.SERIALIZER==Config.SerializerType.serpent)
			Console.WriteLine("note that for the serpent serializer, you need to have the Razorvine.Serpent assembly available.");

		using(NameServerProxy ns=NameServerProxy.locateNS(null, hmacKey: hmacKey))
		{
			Console.WriteLine("discovered ns at "+ns.hostname+":"+ns.port);
			ns.ping();

			Console.WriteLine("lookup of name server object:");
			PyroURI uri = ns.lookup("Pyro.NameServer");
			Console.WriteLine("   "+uri);
			Console.WriteLine("lookup of name server object, with metadata:");
			var tmeta = ns.lookup("Pyro.NameServer", true);
			Console.WriteLine("   uri:  "+tmeta.Item1);
			Console.WriteLine("   meta: "+string.Join(", " ,tmeta.Item2));
			var metadata = tmeta.Item2;
			metadata.Add("updated-by-dotnet-pyrolite");
			ns.set_metadata("Pyro.NameServer", metadata);
			
			
			Console.WriteLine("\nobjects registered in the name server:");
			IDictionary<string,string> objects = ns.list(null, null);
			foreach(string key in objects.Keys) {
				Console.WriteLine(key + " --> " + objects[key]);
			}
		
			Console.WriteLine("\nobjects registered in the name server, with metadata:");
			IDictionary<string, Tuple<string, ISet<string>>> objectsm = ns.list_with_meta(null, null);
			foreach(string key in objectsm.Keys) {
				var registration = objectsm[key];
				Console.WriteLine(key + " --> " + registration.Item1);
				Console.WriteLine("      metadata: " + string.Join(", ", registration.Item2));
			}

			Console.WriteLine("\nobjects registered having all metadata:");
			objects = ns.list(null, null, new []{"blahblah", "class:Pyro4.naming.NameServer"}, null);
			foreach(string name in objects.Keys) {
				Console.WriteLine(name + " --> " + objects[name]);
			}
			Console.WriteLine("\nobjects registered having any metadata:");
			objects = ns.list(null, null, null, new []{"blahblah", "class:Pyro4.naming.NameServer"});
			foreach(string name in objects.Keys) {
				Console.WriteLine(name + " --> " + objects[name]);
			}
			Console.WriteLine("\nobjects registered having any metadata (showing it too):");
			objectsm = ns.list_with_meta(null, null, null, new []{"blahblah", "class:Pyro4.naming.NameServer"});
			foreach(string name in objectsm.Keys) {
				var entry = objectsm[name];
				Console.WriteLine(name + " --> " + entry.Item1);
				Console.WriteLine("      metadata: " + string.Join(", ", entry.Item2));
			}

			Console.WriteLine("");
			ns.register("dotnet.test", new PyroURI("PYRO:DotnetTest@localhost:9999"), false);
			ns.register("dotnet.testmeta", new PyroURI("PYRO:DotnetTest@localhost:9999"), false, new []{"example", "from-dotnet-pyrolite"});

			Console.WriteLine("uri=" + ns.lookup("dotnet.test"));
			Console.WriteLine("using a new proxy to call the nameserver.");
			
			using(PyroProxy p=new PyroProxy(ns.lookup("Pyro.NameServer")))
			{
				p.pyroHmacKey = hmacKey;
				p.call("ping");
			}
	
			int num_removed=ns.remove(null, "dotnet.", null);
			Console.WriteLine("number of removed entries: {0}",num_removed);
			
			try {
				Console.WriteLine("uri=" + ns.lookup("dotnet.test"));	 // should fail....
			} catch (PyroException x) {
				// ok
				Console.WriteLine("got a PyroException (expected): {0}", x.Message);
			}
			
			Console.WriteLine("\r\nEnter to exit:"); Console.ReadLine();
		}

	}
	
	static void setConfig()
	{
		string tracedir=Environment.GetEnvironmentVariable("PYRO_TRACE_DIR");
		if(tracedir!=null) {
			Config.MSG_TRACE_DIR=tracedir;
		}
		string serializer=Environment.GetEnvironmentVariable("PYRO_SERIALIZER");
		if(serializer!=null) {
			Config.SERIALIZER=(Config.SerializerType) Enum.Parse(typeof(Config.SerializerType), serializer, true);
		}
	}
}

}

