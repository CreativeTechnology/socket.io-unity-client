using UnityEngine;
using System;
using System.Collections.Generic;


public class SocketIoClient : MonoBehaviour {
	
	[HideInInspector]
	public Queue<string> queue;
	
	public SocketIoClientConnection websocket;
	
	public void ProcessQueue() {
		while(queue.Count>0) {
			HandleMessage(queue.Dequeue());
		}
	}
	
	public virtual void HandleMessage(string msg) {
		print("SocketIoClient: " + msg);
	}
	
	public void Send(string msg) {
		websocket.send(msg);
	}
	
	public virtual  void OnOpen() {
		print("SocketIoClient: [open]");
	}
	
	public virtual void OnClose() {
		print("SocketIoClient: [closed]");
	}

	public virtual void Log(string msg) {
		print(msg);
	}
	
	public virtual void OnApplicationQuit() {
		websocket.close();
	}
	
	public void SetupClient(string url) {
		try {
			websocket = new SocketIoClientConnection(new Uri(url));
			websocket.setEventHandler(this);
	        websocket.connect();
		} catch (SocketIoClientException wse) {
			print(wse.ToString());
		}
		
	}
	
	public void StartClient() {
		queue = new Queue<string>();
	}
}
