using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LongWebSocketsConnection
{
    public class LongWebSocketConnection
    {
		readonly ClientWebSocket _webSocket = new ClientWebSocket();

		public async Task Connect()
		{
			Console.WriteLine("Connecting to service");
			await _webSocket.ConnectAsync(new Uri("wss://echo.websocket.org/"), CancellationToken.None)
				.ConfigureAwait(continueOnCapturedContext: false);
			Console.WriteLine("Connected");
		}

		public async Task ReadDataForever()
		{
			Console.WriteLine("Reading data");

			var buffer = new ArraySegment<byte>(new byte[1024]);
			var completeMessageStream = new MemoryStream();
			while (_webSocket.State == WebSocketState.Open)
			{
				while (true)
				{
					var result = await _webSocket.ReceiveAsync(buffer, CancellationToken.None)
						.ConfigureAwait(continueOnCapturedContext: false);
					switch (result.MessageType)
					{
						case WebSocketMessageType.Binary:
							throw new NotSupportedException("Didn't expect a binary message type");
						case WebSocketMessageType.Text:
							completeMessageStream.Write(buffer.Array, 0, result.Count);
							break;
						case WebSocketMessageType.Close:
							Console.WriteLine($"WebSocket closed: {result.CloseStatus.Value} {result.CloseStatusDescription}");
							return;
					}
					if (result.EndOfMessage)
						break;
				}

				var receivedMessage = Encoding.UTF8.GetString(completeMessageStream.ToArray());
				Console.WriteLine($"Received message: '{receivedMessage}'");
				completeMessageStream.SetLength(0);		//Reset the stream
			}

			Console.WriteLine($"State!=Open: {_webSocket.State}");
		}

		public async Task WriteData(string message)
		{
			if (_webSocket.State != WebSocketState.Open)
				throw new InvalidOperationException($"WebSocket State!=Open: {_webSocket.State}");

			Console.WriteLine($"Writing '{message}'");
			var data = Encoding.UTF8.GetBytes(message);
			var segment = new ArraySegment<byte>(data);
			await _webSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None)
				.ConfigureAwait(continueOnCapturedContext: false);
		}
	}
}
