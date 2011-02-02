using System.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class SocketIoClientHandshake {	
	private string key1 = null;
	private string key2 = null;
	private byte[] key3 = null;
	public byte[] expectedServerResponse = null;
	
	private Uri url = null;
	private string origin = null;
	private string protocol = null;
	private Random r = new Random();
	
	public SocketIoClientHandshake(Uri url, string protocol) {
		this.url = url;
		this.protocol = null;
		generateKeys();
	}
	
	public byte[] getHandshake() {
		string path = url.LocalPath;
		string host = url.Host;
		origin = "http://" + host;
		
		string handshake = "GET " + path + " HTTP/1.1\r\n" +
				"Upgrade: WebSocket\r\n" +
				"Connection: Upgrade\r\n" +
				"Host: " + host + "\r\n" +
				"Origin: " + origin + "\r\n" /*+
				"Sec-WebSocket-Key2: " + key2 + "\r\n" +
				"Sec-WebSocket-Key1: " + key1 + "\r\n"*/; 
				/* Not using handshake currently */
		
		if (protocol != null) {
			handshake += "Sec-WebSocket-Protocol: " + protocol + "\r\n";
		}
		
		handshake += "\r\n";
		
		return Encoding.UTF8.GetBytes(handshake);
	}
	
	
	public void verifyServerResponse(byte[] response) {
		if (!response.Equals(expectedServerResponse)) {
			throw new SocketIoClientException("Handshake failed");
		}
	}
	
	
	public void verifyServerStatusLine(string statusLine) {
		int statusCode = int.Parse(statusLine.Substring(9, 3));
		if (statusCode == 407) {
			throw new SocketIoClientException("connection failed: proxy authentication not supported");
		}
		else if (statusCode == 404) {
			throw new SocketIoClientException("connection failed: 404 not found");
		}
		else if (statusCode != 101) {
			throw new SocketIoClientException("connection failed: unknown status code " + statusCode);
		}
	}
	
	
	public void verifyServerHandshakeHeaders(Dictionary<string, string> headers) {
		if (!headers["Upgrade"].Equals("WebSocket")) {
			throw new SocketIoClientException("connection failed: missing header field in server handshake: Upgrade");
		} else if (!headers["Connection"].Equals("Upgrade")) {
			throw new SocketIoClientException("connection failed: missing header field in server handshake: Connection");
		} else if (!headers["WebSocket-Origin"].Equals(origin)) {
			/* should be "Sec-"WebSocket-Origin if handshakes worked */
			throw new SocketIoClientException("connection failed: missing header field in server handshake: Sec-WebSocket-Origin");
		}
	}
		
	
	private void generateKeys() {
		
		int spaces1 = r.Next(1,12);
		int spaces2 = r.Next(1,12);
		
		int max1 = int.MaxValue / spaces1;
		int max2 = int.MaxValue / spaces2;
		
		int number1 = r.Next(0, max1);
		int number2 = r.Next(0, max2);
		
		int product1 = number1 * spaces1;
		int product2 = number2 * spaces2;
		
		key1 = product1.ToString();
		key2 = product2.ToString();
		
		key1 = insertRandomCharacters(key1);
		key2 = insertRandomCharacters(key2);
		
		key1 = insertSpaces(key1, spaces1);
		key2 = insertSpaces(key2, spaces2);
		
		key3 = createRandomBytes();
		
		
		//ByteBuffer buffer = ByteBuffer.allocate(4);
		MemoryStream buffer = new MemoryStream(4);
		//buffer.putInt(number1);
		using (BinaryWriter writer = new BinaryWriter(buffer)) {
			writer.Write(number1);
		}
		//byte[] number1Array = buffer.array();
		byte[] number1Array = buffer.ToArray();
		
		//buffer = ByteBuffer.allocate(4);
		buffer = new MemoryStream(4);
		//buffer.putInt(number2);
		using (BinaryWriter writer = new BinaryWriter(buffer)) {
			writer.Write(number2);
		}
		//byte[] number2Array = buffer.array();
		byte[] number2Array = buffer.ToArray();
		byte[] challenge = new byte[16];
		Array.Copy(number1Array, 0, challenge, 0, 4);
		Array.Copy(number2Array, 0, challenge, 4, 4);
		Array.Copy(key3, 0, challenge, 8, 8);

		expectedServerResponse = md5(challenge);
	}
	
	
	private string insertRandomCharacters(string key) {
		int count = r.Next(1, 12);
		
		char[] randomChars = new char[count];
		int randCount = 0;
		while (randCount < count) {
			int rand = (int) (((float)r.NextDouble()) * 0x7e + 0x21);
			if (((0x21 < rand) && (rand < 0x2f)) || ((0x3a < rand) && (rand < 0x7e))) {
				randomChars[randCount] = (char) rand;
				randCount += 1;
			}
		}
		
		for (int i = 0; i < count; i++) {
			int split = r.Next(0, key.Length);
			String part1 = key.Substring(0, split);
			String part2 = key.Substring(split);
			key = part1 + randomChars[i] + part2;
		}
		
		return key;
	}
	
	
	private string insertSpaces(String key, int spaces) {
		for (int i = 0; i < spaces; i++) {
			int split = r.Next(0, key.Length);
			String part1 = key.Substring(0, split);
			String part2 = key.Substring(split);
			key = part1 + " " + part2;
		}
		
		return key;
	}
	
	
	private byte[] createRandomBytes() {
		byte[] bytes = new byte[8];
		
		for (int i = 0; i < 8; i++) {
			bytes[i] = (byte) r.Next(0, 255);
		}
		
		return bytes;
	}
	
	
	private byte[] md5(byte[] bytes) {
		System.Security.Cryptography.MD5 md = System.Security.Cryptography.MD5.Create();
		return md.ComputeHash(bytes);
	}
}