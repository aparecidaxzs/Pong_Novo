using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Generic;

public class TcpChatServer : MonoBehaviour
{
    private TcpListener listener;
    private Thread listenThread;

    private List<TcpClient> clients = new List<TcpClient>();

    void Start()
    {
        listener = new TcpListener(IPAddress.Any, 6000); // Porta do chat TCP
        listener.Start();

        listenThread = new Thread(ListenClients);
        listenThread.Start();

        Debug.Log("Servidor TCP do chat iniciado na porta 6000.");
    }

    void ListenClients()
    {
        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            lock (clients) clients.Add(client);

            Thread t = new Thread(HandleClient);
            t.Start(client);
        }
    }

    void HandleClient(object obj)
    {
        TcpClient client = (TcpClient)obj;
        NetworkStream stream = client.GetStream();

        byte[] buffer = new byte[1024];

        while (true)
        {
            int count = 0;
            try { count = stream.Read(buffer, 0, buffer.Length); }
            catch { break; }

            if (count <= 0) break;

            string msg = Encoding.UTF8.GetString(buffer, 0, count);

            Broadcast(msg);
        }

        lock (clients) clients.Remove(client);
        client.Close();
    }

    void Broadcast(string msg)
    {
        byte[] data = Encoding.UTF8.GetBytes(msg);

        lock (clients)
        {
            foreach (var cli in clients)
            {
                try
                {
                    cli.GetStream().Write(data, 0, data.Length);
                }
                catch { }
            }
        }
    }

    void OnApplicationQuit()
    {
        listener.Stop();
        listenThread?.Abort();
    }
}
