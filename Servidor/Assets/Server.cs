using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

public class Server : MonoBehaviour
{
    private UdpClient server;
    private IPEndPoint anyEP;
    private Thread receiveThread;
    private Dictionary<string, int> clientIds = new Dictionary<string, int>();
    private int nextId = 1;

    void Start()
    {
        server = new UdpClient(5001);
        anyEP = new IPEndPoint(IPAddress.Any, 0);

        receiveThread = new Thread(ReceiveData);
        receiveThread.Start();

        Debug.Log("Servidor iniciado na porta 5001 (modo duplas: 4 jogadores).");
    }

    void ReceiveData()
    {
        while (true)
        {
            byte[] data = server.Receive(ref anyEP);
            string msg = Encoding.UTF8.GetString(data);
            string key = anyEP.Address + ":" + anyEP.Port;

            // Novo cliente (até 4 para duplas)
            if (!clientIds.ContainsKey(key) && nextId <= 4)
            {
                clientIds[key] = nextId++;
                string assignMsg = "ASSIGN:" + clientIds[key];
                server.Send(Encoding.UTF8.GetBytes(assignMsg), assignMsg.Length, anyEP);
                Debug.Log("Novo cliente conectado: " + key + " => ID " + clientIds[key] + " (duplas)");
            }

            Debug.Log($"Servidor recebeu: {msg}");

            // Retransmite para todos os conectados (incluindo GAMEOVER para sincronizar fim do jogo)
            if (msg.StartsWith("POS:") || msg.StartsWith("BALL:") || msg.StartsWith("SCORE:") || msg == "GAMEOVER")
            {
                byte[] bdata = Encoding.UTF8.GetBytes(msg);
                foreach (var kvp in clientIds)
                {
                    var parts = kvp.Key.Split(':');
                    IPEndPoint ep = new IPEndPoint(IPAddress.Parse(parts[0]), int.Parse(parts[1]));
                    server.Send(bdata, bdata.Length, ep);
                }
            }
        }
    }

    void OnApplicationQuit()
    {
        receiveThread?.Abort();
        server?.Close();
    }
}