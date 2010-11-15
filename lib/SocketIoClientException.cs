using System;

public class SocketIoClientException : Exception {
	#pragma warning disable 0414
	private static long serialVersionUID = 1L;
	#pragma warning restore 0414

	public SocketIoClientException(string message) : base(message) {}
	
	
	public SocketIoClientException(string message, Exception t) : base(message, t) {}
}
