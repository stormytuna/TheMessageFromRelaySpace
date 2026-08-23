using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using DSCR;

public class DSCRSocket : MonoBehaviour
{
    private static ClientWebSocket socket;
    private static CancellationTokenSource cancellationToken;
    private static Encoding ASCII => Encoding.ASCII;

    public static string? Callsign = null;
    public static Queue<string> EnqueuedMessages = new Queue<string>();

    private async void Start() {
        cancellationToken = new CancellationTokenSource();
        socket = new ClientWebSocket();

        try {
            await TryConnect(socket, new Uri("wss://dscr-relay.dixonary.co.uk"), cancellationToken.Token);
            Task.Run(() => DSCRSocket.Receive(socket, cancellationToken.Token));
            Task.Run(() => DSCRSocket.Send(socket, cancellationToken.Token));
        } catch (OperationCanceledException) {
            // noop, we don't want to log errors from closing the game
        } catch (Exception ex) {
            Plugin.Logger.LogError(ex);
        }
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
                Plugin.Logger.LogInfo("Socket closed");
                return;
            }

            var text = ASCII.GetString(buffer[0..result.Count]);
            DSCRWindow.PrintText(text);
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

    public static async Task TryConnect(ClientWebSocket socket, Uri uri, CancellationToken token) {
        var confirmationBuffer = new byte[8];
        var confirmationMemory = new Memory<byte>(confirmationBuffer);

        await socket.ConnectAsync(uri, token);

        var message = $"S,{Callsign}";

        await socket.SendAsync(new Memory<byte>(ASCII.GetBytes(message)), WebSocketMessageType.Text, true, default);
        var result = await socket.ReceiveAsync(confirmationBuffer, token);

        if (confirmationBuffer[0] == 'K') {
            Plugin.Logger.LogInfo($"Connected with callsign {Callsign}");
        } else {
            // TODO: Handle rejected callsign
            Plugin.Logger.LogInfo("Could not connect");
        }
    }
}
