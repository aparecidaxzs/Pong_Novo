using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Collections.Concurrent;

public class Client : MonoBehaviour
{
    public int myId = -1;
    private UdpClient client;
    private Thread receiveThread;
    private IPEndPoint serverEP;

    public GameObject[] players = new GameObject[4]; // Referência aos 4 jogadores
    private Vector3[] remotePositions = new Vector3[4];

    public GameObject bola;
    public int Velocidade = 20;

    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();

    void Start()
    {
        client = new UdpClient();
        serverEP = new IPEndPoint(IPAddress.Parse("10.57.10.29"), 5001); // IP do servidor
        client.Connect(serverEP);

        receiveThread = new Thread(ReceiveData);
        receiveThread.Start();

        // Solicita conexão
        client.Send(Encoding.UTF8.GetBytes("HELLO"), 5);

        // Bola inicial
        if (bola != null)
        {
            bola.transform.position = Vector3.zero;
            var rb = bola.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }
    }

    void Update()
    {
        // Processa mensagens da thread
        while (messageQueue.TryDequeue(out string msg))
        {
            ProcessMessage(msg);
        }

        if (myId == -1) return;

        // Movimento do jogador local
        float v = Input.GetAxis("Vertical");
        if (players[myId - 1] != null)
        {
            players[myId - 1].transform.Translate(new Vector3(0, v, 0) * Time.deltaTime * Velocidade);

            // Limites
            Vector3 pos = players[myId - 1].transform.position;
            pos.y = Mathf.Clamp(pos.y, -3f, 3f);
            players[myId - 1].transform.position = pos;

            // Envia posição
            string msgPos = $"POS:{myId};{pos.x.ToString("F2", CultureInfo.InvariantCulture)};{pos.y.ToString("F2", CultureInfo.InvariantCulture)}";
            SendUdpMessage(msgPos);
        }

        // Atualiza as posições dos outros jogadores
        for (int i = 0; i < 4; i++)
        {
            if (i != (myId - 1) && players[i] != null)
            {
                players[i].transform.position = Vector3.Lerp(
                    players[i].transform.position,
                    remotePositions[i],
                    Time.deltaTime * 10f
                );
            }
        }
    }

    void ReceiveData()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        while (true)
        {
            byte[] data = client.Receive(ref remoteEP);
            string msg = Encoding.UTF8.GetString(data);
            messageQueue.Enqueue(msg);
        }
    }

    void ProcessMessage(string msg)
    {
        if (msg.StartsWith("ASSIGN:"))
        {
            myId = int.Parse(msg.Substring(7));
            Debug.Log($"[Cliente] Meu ID = {myId}");

            // Define posições iniciais
            Vector3[] startPositions = new Vector3[]
            {
                new Vector3(-8f,  2f, 0f),  // Player 1 - Esquerda cima
                new Vector3(-8f, -2f, 0f),  // Player 2 - Esquerda baixo
                new Vector3( 8f,  2f, 0f),  // Player 3 - Direita cima
                new Vector3( 8f, -2f, 0f)   // Player 4 - Direita baixo
            };

            for (int i = 0; i < 4; i++)
            {
                players[i] = GameObject.Find("Player " + (i + 1));
                if (players[i] != null)
                {
                    players[i].transform.position = startPositions[i];
                    remotePositions[i] = startPositions[i];
                }
            }

            // Reset bola
            if (bola != null)
            {
                bola.transform.position = Vector3.zero;
                var rb = bola.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.linearVelocity = Vector2.zero;
            }
        }
        else if (msg.StartsWith("POS:"))
        {
            string[] parts = msg.Substring(4).Split(';');
            if (parts.Length == 3)
            {
                int id = int.Parse(parts[0]);
                if (id >= 1 && id <= 4 && id != myId)
                {
                    float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                    float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
                    remotePositions[id - 1] = new Vector3(x, y, 0);
                }
            }
        }
        else if (msg.StartsWith("BALL:"))
        {
            // Bola sincronizada entre todos
            string[] parts = msg.Substring(5).Split(';');
            if (parts.Length == 2)
            {
                float x = float.Parse(parts[0], CultureInfo.InvariantCulture);
                float y = float.Parse(parts[1], CultureInfo.InvariantCulture);

                if (bola != null)
                    bola.transform.position = new Vector3(x, y, 0);
            }
        }
        else if (msg.StartsWith("SCORE:"))
        {
            string[] parts = msg.Substring(6).Split(';');
            if (parts.Length == 2 && bola != null)
            {
                int scoreA = int.Parse(parts[0]);
                int scoreB = int.Parse(parts[1]);

                var bolaScript = bola.GetComponent<Bola>();
                bolaScript.PontoA = scoreA;
                bolaScript.PontoB = scoreB;
                bolaScript.textoPontoA.text = "Pontos: " + scoreA;
                bolaScript.textoPontoB.text = "Pontos: " + scoreB;
            }
        }
    }

    public void SendUdpMessage(string msg)
    {
        client.Send(Encoding.UTF8.GetBytes(msg), msg.Length);
    }

    void OnApplicationQuit()
    {
        receiveThread?.Abort();
        client?.Close();
    }
}
