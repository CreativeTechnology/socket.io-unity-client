using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

// Inherit from SocketIoClient Class
public class Example : SocketIoClient {
	
	public GameObject cube;
	
	void Awake() {
		// Setup Socket Connection
		SetupClient("ws://ewi1544.ewi.utwente.nl:5000/socket.io/websocket");
	}
	
	
	void Start() {
		StartClient();
	}
	
	public void Update() {
		// Calls "HandleMessage" if a message was on stack
		ProcessQueue();
		cube.transform.RotateAround(Vector3.up, Time.deltaTime/5.0f);
	}
	
	public override void HandleMessage(string msg) {
		switch(msg) {
			case "red": 	cube.transform.renderer.material.color = Color.red;		break;
			case "green": 	cube.transform.renderer.material.color = Color.green;	break;
			case "blue": 	cube.transform.renderer.material.color = Color.blue;	break;
			default:		print("Unknown: " + msg);								break;
		}
	}

}