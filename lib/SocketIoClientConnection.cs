using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Threading;
using System.Collections.Generic;


public class SocketIoClientConnection {
	private Uri url = null;
	
	private SocketIoClient eventHandler = null;
	
	private volatile bool connected = false;
	
	
	public TcpClient socket = null;
	public Stream stream = null;
	private StreamReader input = null;
	
	private SocketIoClientReceiver receiver = null;
	private SocketIoClientHandshake handshake = null;
	
	private System.Object lockThis = new System.Object();
	
	public SocketIoClientConnection(Uri url) : this(url, null) {}
	
	public SocketIoClientConnection(Uri url, string protocol) {
		this.url = url;
		handshake = new SocketIoClientHandshake(url, protocol);
	}
	

	public void setEventHandler(SocketIoClient eventHandler) {
		this.eventHandler = eventHandler;
	}
	
	
	public SocketIoClient getEventHandler() {
		return this.eventHandler;
	}
	

	public void connect() {
		try {
			if (connected) {
				throw new SocketIoClientException("already connected");
			}
			
			socket = createSocket();
			stream = socket.GetStream();
			
			input = new StreamReader(stream);
			byte[] bytes = handshake.getHandshake();
			stream.Write(bytes, 0, bytes.Length);
			stream.Flush();
			
			bool handshakeComplete = false;
			List<string> handshakeLines = new List<string>();
			string line;
			
			while(!handshakeComplete) {
				line = input.ReadLine().Trim();
				if (line.Length>0) {
					handshakeLines.Add(line);
				} else {
					handshakeComplete = true;
				}
			}
			
			char[] response = new char[16];
			input.ReadBlock(response, 0, response.Length);
			
			handshake.verifyServerStatusLine(handshakeLines[0]);
			
			/* Verifying handshake fails... */
			//handshake.verifyServerResponse(response);
			
			handshakeLines.RemoveAt(0);
			
			Dictionary<string, string> headers = new Dictionary<string, string>();
			foreach (string l in handshakeLines) {
				string[] keyValue = l.Split(new char[] {':'},2);
				headers.Add(keyValue[0].Trim(), keyValue[1].Trim());
			}

			handshake.verifyServerHandshakeHeaders(headers);
			receiver = new SocketIoClientReceiver(this);
			connected = true;
			eventHandler.OnOpen();
			(new Thread(receiver.run)).Start();
		} catch (SocketIoClientException wse) {
			throw wse;
		} catch (IOException ioe) {
			throw new SocketIoClientException("error while connecting: " + ioe.StackTrace, ioe);
		}
	}

	public void send(string data) {
		lock (lockThis) {
			if (!connected) {
				throw new SocketIoClientException("error while sending text data: not connected");
			}
			
			try {
				byte[] msg = Encoding.UTF8.GetBytes(data);
				byte[] length = Encoding.UTF8.GetBytes(msg.Length+"");
				stream.WriteByte(0x00);
				stream.WriteByte(0x7e); // ~
				stream.WriteByte(0x6d); // m
				stream.WriteByte(0x7e); // ~
				stream.Write(length, 0, length.Length);
				stream.WriteByte(0x7e); // ~
				stream.WriteByte(0x6d); // m
				stream.WriteByte(0x7e); // ~
				stream.Write(msg, 0, msg.Length);
				stream.WriteByte(0xff);
				stream.Flush();
				
			} catch (IOException ioe) {
				throw new SocketIoClientException("error while sending text data", ioe);
			}
        }
	}
	
	
	public void handleReceiverError() {
		try {
			if (connected) {
				close();
			}
		} catch (SocketIoClientException) {
			//Console.WriteLine(wse.StackTrace);
		}
	}
	

	public void close() {
		lock (lockThis) {
			if (!connected) {
				//Console.WriteLine("Trying to close, but not connected");
				return;
			}
					
			if (receiver.isRunning()) {
				receiver.stopit();
			}
			
			sendCloseHandshake();
			
			closeStreams();
	
			eventHandler.OnClose();
		}
	}
	
	
	private void sendCloseHandshake() {
		lock (lockThis) {
			if (!connected) {
				throw new SocketIoClientException("error while sending close handshake: not connected");
			}
			
			try {
				stream.WriteByte(0x00);
				stream.Flush();
			}
			catch (IOException ioe) {
				throw new SocketIoClientException("error while sending close handshake", ioe);
			}
	
			connected = false;
		}
	}
	

	private TcpClient createSocket() {
		string scheme = url.Scheme;
		string host = url.Host;
		int port = url.Port;
		
		TcpClient socket;
		
		if (scheme != null && scheme.Equals("ws")) {
			if (port == -1) {
				port = 80;
			}
			
			try {
				socket = new TcpClient(host, port);
			} catch (IOException ioe) {
				throw new SocketIoClientException("error while creating socket to " + url, ioe);
			}
		} else if (scheme != null && scheme.Equals("wss")) {
			throw new SocketIoClientException("Secure Sockets not implemented");
		} else {
			throw new SocketIoClientException("unsupported protocol: " + scheme);
		}
		
		return socket;
	}
	
	
	private void closeStreams() {
		try {
			input.Close();
			stream.Close();
			socket.Close();
		} catch (IOException ioe) {
			throw new SocketIoClientException("error while closing SocketIoClient connection: ", ioe);
		}
	}
}
