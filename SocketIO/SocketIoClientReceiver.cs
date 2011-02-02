using UnityEngine;
using System; 
using System.Threading; 
using System.IO;
using System.Collections;
using System.Collections.Generic;


public class SocketIoClientReceiver {
	private SocketIoClientConnection SocketIoClient = null;
	private SocketIoClient eventHandler = null;

	private volatile bool stop = false;
	
	public SocketIoClientReceiver(SocketIoClientConnection SocketIoClient) {
		this.SocketIoClient = SocketIoClient;
		this.eventHandler = SocketIoClient.getEventHandler();
	}


	public void run() {
		List<byte> messageBytes = new List<byte>();
		bool frameStart = false;
		while (!stop) {
			try {
				byte b = (byte)SocketIoClient.stream.ReadByte();
				if (b == 0x00) {
					//"~m~XXXX~m~"
					b = (byte)SocketIoClient.stream.ReadByte(); // ~
					b = (byte)SocketIoClient.stream.ReadByte(); // m
					b = (byte)SocketIoClient.stream.ReadByte(); // ~
					b = (byte)SocketIoClient.stream.ReadByte(); // X
					List<char> bytes = new List<char>();
					while (true) {
						if (b==0x7e) break;
						else {
							bytes.Add((char)b);
							b = (byte)SocketIoClient.stream.ReadByte();
						}
					}
					b = (byte)SocketIoClient.stream.ReadByte(); // m
					b = (byte)SocketIoClient.stream.ReadByte(); // ~
					string number = "";
					foreach (char c in bytes) {
						number += c;
					}
					//eventHandler.Log("Lenght: "+number);
					frameStart = true;
				} else if (b == 0xff && frameStart == true) {
					frameStart = false;
					string msg = System.Text.Encoding.UTF8.GetString(messageBytes.ToArray());
					eventHandler.queue.Enqueue(msg);
					messageBytes.Clear();
				} else if (frameStart == true){
					// filter out some wierd payload & respond heartbeats
					if (b==0x7e && messageBytes.Count==0) {
						b = (byte)SocketIoClient.stream.ReadByte();
						if (b==(byte)'h'){
							b = (byte)SocketIoClient.stream.ReadByte(); // tilde
							b = (byte)SocketIoClient.stream.ReadByte();
							List<char> bytes = new List<char>();
							while (true) {
								if (b==0xff || b==0x00 || b==0x0a || b==0x0d) break;
								else {
									bytes.Add((char)b);
									b = (byte)SocketIoClient.stream.ReadByte();
								}
							}
							string number = "";
							foreach (char c in bytes) {
								number += c;
							}
							SocketIoClient.send("~h~"+number);
						}
						frameStart = false;
					} else {
						messageBytes.Add(b);
					}
				} else if ((int)b == -1) {
					handleError();
				}
			} catch (IOException) {
				handleError();
			}
		}
	}
	
	
	public void stopit() {
		stop = true;
	}
	
	
	public bool isRunning() {
		return !stop;
	}
	
	
	private void handleError() {
		stopit();
		SocketIoClient.handleReceiverError();
	}
}