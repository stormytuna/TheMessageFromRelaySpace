using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMFRS.UI;
using UnityEngine;

namespace TMFRS.MonoBehaviours;

// Adapted, with permission, from https://codeberg.org/TacoConKvass/dscr-cli/src/commit/8683a0902571f0e399dbf131365e65002589286a/src/dscr-client/DscrSocket.cs

public class RelaySocket : MonoBehaviour
{
	private static ClientWebSocket socket;
	private static CancellationTokenSource cancellationToken;
	private static Encoding ASCII => Encoding.ASCII;
	private static Queue<string> receivedMessages = new Queue<string>();
	private static Coroutine showMessages;

	public static string? Callsign = null;
	public static bool UpdateCallsign = false;
	public static Queue<string> EnqueuedMessages = new Queue<string>();

	private async void Start() {
		cancellationToken = new CancellationTokenSource();
		socket = new ClientWebSocket();

		try {
			var goodCallsign = await TryConnect(socket, new Uri(TMFRSPlugin.RelaySource.Value), cancellationToken.Token);
			if (!goodCallsign) {
				RelayManagerWindow.BadCallsign();
				Disconnect();
				return;
			} else {
				RelayManagerWindow.GoodCallsign();
			}

			Task.Run(() => RelaySocket.Receive(socket, cancellationToken.Token));
			Task.Run(() => RelaySocket.Send(socket, cancellationToken.Token));
			Task.Run(() => RelaySocket.TryUpdateCallsign(socket, cancellationToken.Token));
		}
		catch (OperationCanceledException) {
			// noop, we don't want to log errors from closing the game
		}
		catch (Exception ex) {
			TMFRSPlugin.Logger.LogError(ex);
		}
	}

	private void Update() {
		int totalCharCount = receivedMessages.Sum(x => x.Length);
		while (receivedMessages.Count > 0) {
			RelayWindow.PrintText(receivedMessages.Dequeue(), totalCharCount < TMFRSPlugin.RelayTypeCharByCharCutoff.Value, totalCharCount > TMFRSPlugin.RelayTypeLineByLineCutoff.Value);
		}
	}

	private IEnumerator ShowMessages() {
		yield return new WaitForSeconds(0.1f);
	}

	public void Disconnect() {
		cancellationToken?.Cancel();

		socket?.Dispose();
		socket = null;

		cancellationToken?.Dispose();
		cancellationToken = null;

		Destroy(gameObject);
	}

	public static void QueueSend(string compiledMessage) {
		EnqueuedMessages.Enqueue(compiledMessage);
	}

	public static async Task Receive(ClientWebSocket socket, CancellationToken token) {
		// ~32KiB buffer, should be plenty for receiving messages
		var buffer = new byte[ushort.MaxValue / 2];
		var view = new System.Memory<byte>(buffer);

		while (socket.State == WebSocketState.Open) {
			var result = await socket.ReceiveAsync(view, token);
			if (result.MessageType == WebSocketMessageType.Close) {
				await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, token);
				return;
			}

			var text = ASCII.GetString(buffer[0..result.Count]);

			if (text[0] == 'K') {
				RelayManagerWindow.GoodCallsign();
			} else if (text[0] == 'U') {
				RelayManagerWindow.BadCallsign();
			} else {
				receivedMessages.Enqueue(text);
			}

			Array.Fill<byte>(buffer, 0, 0, result.Count);
		}
	}

	public static async Task Send(ClientWebSocket socket, CancellationToken token) {
		while (socket.State == WebSocketState.Open) {
			if (EnqueuedMessages.Count > 0) {
				await socket.SendAsync(ASCII.GetBytes(EnqueuedMessages.Dequeue()).AsMemory<byte>(), WebSocketMessageType.Text, true, token);
			}
		}
	}

	public static async Task TryUpdateCallsign(ClientWebSocket socket, CancellationToken token) {
		while (socket.State == WebSocketState.Open) {
			if (!UpdateCallsign) {
				continue;
			}

			var message = $"S,{Callsign},0";

			await socket.SendAsync(new Memory<byte>(ASCII.GetBytes(message)), WebSocketMessageType.Text, true, default);
			UpdateCallsign = false;

			// Handling result in Receive
		}
	}

	public static async Task<bool> TryConnect(ClientWebSocket socket, Uri uri, CancellationToken token) {
		var confirmationBuffer = new byte[8];
		var confirmationMemory = new Memory<byte>(confirmationBuffer);

		await socket.ConnectAsync(uri, token);

		var message = $"S,{Callsign}";

		await socket.SendAsync(new Memory<byte>(ASCII.GetBytes(message)), WebSocketMessageType.Text, true, default);
		var result = await socket.ReceiveAsync(confirmationBuffer, token);

		return confirmationBuffer[0] == 'K';
	}
}
