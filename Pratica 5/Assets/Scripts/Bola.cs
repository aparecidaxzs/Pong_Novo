using UnityEngine;
using TMPro;
using System.Globalization;

public class Bola : MonoBehaviour
{
    private Rigidbody2D rb;
    private Client udpClient;
    private bool bolaLancada = false;

    public int PontoTimeA = 0; // Jogadores 1 e 2
    public int PontoTimeB = 0; // Jogadores 3 e 4

    public TextMeshProUGUI textoPontoA;
    public TextMeshProUGUI textoPontoB;
    public TextMeshProUGUI VitoriaTimeA;
    public TextMeshProUGUI VitoriaTimeB;

    public float velocidade = 5f;
    public float fatorDesvio = 2f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        udpClient = FindObjectOfType<Client>();

        // Apenas o jogador 1 (host) lança a bola
        if (udpClient != null && udpClient.myId == 1)
        {
            Invoke(nameof(LancarBola), 1f);
        }
    }

    void Update()
    {
        if (udpClient == null) return;

        // Apenas o jogador 1 sincroniza a bola com os outros
        if (udpClient.myId == 1)
        {
            if (!bolaLancada)
            {
                bolaLancada = true;
                Invoke(nameof(LancarBola), 1f);
            }

            string msg = "BALL:" +
                         transform.position.x.ToString(CultureInfo.InvariantCulture) + ";" +
                         transform.position.y.ToString(CultureInfo.InvariantCulture);

            udpClient.SendUdpMessage(msg);
        }
    }

    void LancarBola()
    {
        float dirX = Random.Range(0, 2) == 0 ? -1 : 1;
        float dirY = Random.Range(-0.5f, 0.5f);
        rb.linearVelocity = new Vector2(dirX, dirY).normalized * velocidade;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (udpClient == null) return;

        if (col.gameObject.CompareTag("Raquete"))
        {
            // Rebote dinâmico baseado no ponto de contato
            float posYbola = transform.position.y;
            float posYraquete = col.transform.position.y;
            float alturaRaquete = col.collider.bounds.size.y;

            float diferenca = (posYbola - posYraquete) / (alturaRaquete / 2f);
            Vector2 direcao = new Vector2(Mathf.Sign(rb.linearVelocity.x), diferenca * fatorDesvio);
            rb.linearVelocity = direcao.normalized * velocidade;
        }
        else if (col.gameObject.CompareTag("GolEsquerda"))
        {
            // Gol contra Time A
            PontoTimeB++;
            AtualizarPlacar();

            if (udpClient.myId == 1)
            {
                EnviarPlacar();
                ResetBola();
            }
        }
        else if (col.gameObject.CompareTag("GolDireita"))
        {
            // Gol contra Time B
            PontoTimeA++;
            AtualizarPlacar();

            if (udpClient.myId == 1)
            {
                EnviarPlacar();
                ResetBola();
            }
        }
    }

    void AtualizarPlacar()
    {
        textoPontoA.text = "Time A: " + PontoTimeA;
        textoPontoB.text = "Time B: " + PontoTimeB;
    }

    void EnviarPlacar()
    {
        string msg = "SCORE:" + PontoTimeA + ";" + PontoTimeB;
        udpClient.SendUdpMessage(msg);
    }

    void ResetBola()
    {
        transform.position = Vector3.zero;
        rb.linearVelocity = Vector2.zero;

        if (PontoTimeA >= 10 || PontoTimeB >= 10)
        {
            GameOver();
        }
        else
        {
            Invoke(nameof(LancarBola), 1f);
        }
    }

    void GameOver()
    {
        rb.linearVelocity = Vector2.zero;
        transform.position = Vector3.zero;

        if (PontoTimeA >= 10)
        {
            VitoriaTimeA.gameObject.SetActive(true);
        }
        else if (PontoTimeB >= 10)
        {
            VitoriaTimeB.gameObject.SetActive(true);
        }
    }
}
