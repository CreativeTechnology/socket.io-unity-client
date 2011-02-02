using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
//using LitJson; /* For json use litjson.sourceforge.net */

// Inherit from SocketIoClient Class
public class Socket : SocketIoClient {
	
	public string host = "localhost";
	public int port = 5000;
	
	void Awake() {
		// For Webplayer sandbox:
		Security.PrefetchSocketPolicy(host, port);
		// Setup Socket Connection
		SetupClient("ws://"+host+":"+port+"/socket.io/websocket");
	}
	
	void Start() {
		// Connect client and start up read thread
		StartClient();
	}
	
	public void Update() {
		// Calls "HandleMessage" if a message was in queue
		ProcessQueue();
	}
	
	// overrides default "onMessage" behaviour:
	public override void HandleMessage(string msg) {
		print(msg);
		//JsonData message = JsonMapper.ToObject(msg);
	}
}
