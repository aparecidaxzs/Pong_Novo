using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

public class TcpChatClient : MonoBehaviour
{
    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;

    public static ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();

    void Start()
    {
        client = new TcpClient();
        client.Connect("10.57.1.76", 6000); // IP do servidor
        stream = client.GetStream();

        receiveThread = new Thread(ReceiveData);
        receiveThread.Start();

        Debug.Log("Cliente conectado ao chat TCP.");
    }

    public void Send(string txt)
    {
        if (client == null || !client.Connected) return;

        byte[] data = Encoding.UTF8.GetBytes(txt);
        stream.Write(data, 0, data.Length);
    }

    void ReceiveData()
    {
        byte[] buffer = new byte[1024];

        while (true)
        {
            if (!client.Connected) break;

            int count = 0;
            try { count = stream.Read(buffer, 0, buffer.Length); }
            catch { break; }

            if (count <= 0) break;

            string msg = Encoding.UTF8.GetString(buffer, 0, count);
            messageQueue.Enqueue(msg);
        }
    }

    void OnApplicationQuit()
    {
        receiveThread?.Abort();
        client?.Close();
    }
}
