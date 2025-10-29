using UnityEngine;
using TMPro;

public class Bola : MonoBehaviour
{
    private Rigidbody2D rb;
    private Client udpClient;
    private bool bolaLancada = false;

    [Header("Pontuação")]
    public int PontoTimeA = 0;
    public int PontoTimeB = 0;
    public TextMeshProUGUI textoPontoA;
    public TextMeshProUGUI textoPontoB;
    public TextMeshProUGUI VitoriaLocal;
    public TextMeshProUGUI VitoriaRemote;

    [Header("Configuração da Bola")]
    public float velocidade = 5f;   // Velocidade base da bola
    public float fatorDesvio = 2f;  // Quanto o ponto de contato influencia o ângulo

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        udpClient = FindObjectOfType<Client>();

        // ✅ O jogador com ID 4 será o "host da bola"
        if (udpClient != null && udpClient.myId == 4)
        {
            Invoke("LancarBola", 1f);
        }
    }

    void Update()
    {
        if (udpClient == null) return;

        // Host (ID 4) controla e envia posição da bola
        if (!bolaLancada && udpClient.myId == 4)
        {
            bolaLancada = true;
            Invoke("LancarBola", 1f);
        }

        if (udpClient.myId == 4)
        {
            string msg = "BALL:" +
                         transform.position.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + ";" +
                         transform.position.y.ToString(System.Globalization.CultureInfo.InvariantCulture);

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

        // ✅ Rebote nas raquetes
        if (col.gameObject.CompareTag("Raquete"))
        {
            float posYbola = transform.position.y;
            float posYraquete = col.transform.position.y;
            float alturaRaquete = col.collider.bounds.size.y;

            float diferenca = (posYbola - posYraquete) / (alturaRaquete / 2f);

            Vector2 direcao = new Vector2(Mathf.Sign(rb.linearVelocity.x), diferenca * fatorDesvio);
            rb.linearVelocity = direcao.normalized * velocidade;
        }
        // ✅ Gol na esquerda
        else if (col.gameObject.CompareTag("Gol1"))
        {
            PontoTimeB++;
            textoPontoB.text = "Pontos: " + PontoTimeB;
            ResetBola();
        }
        // ✅ Gol na direita
        else if (col.gameObject.CompareTag("Gol2"))
        {
            PontoTimeA++;
            textoPontoA.text = "Pontos: " + PontoTimeA;
            ResetBola();
        }
    }

    void ResetBola()
    {
        transform.position = Vector3.zero;
        rb.linearVelocity = Vector2.zero;

        if (PontoTimeA > 10 || PontoTimeB > 10)
        {
            GameOver();
        }
        // ✅ Apenas o host (ID 4) envia novo placar e relança a bola
        else if (udpClient != null && udpClient.myId == 4)
        {
            Invoke("LancarBola", 1f);

            string msg = "SCORE:" + PontoTimeA + ";" + PontoTimeB;
            udpClient.SendUdpMessage(msg);
        }
    }

    void GameOver()
    {
        transform.position = Vector3.zero;
        rb.linearVelocity = Vector2.zero;

        // Mostra mensagem de vitória para cada lado
        if (PontoTimeA > 10 && (udpClient.myId == 1 || udpClient.myId == 2))
        {
            VitoriaLocal.gameObject.SetActive(true);
        }
        else if (PontoTimeA > 10 && (udpClient.myId == 3 || udpClient.myId == 4))
        {
            VitoriaRemote.gameObject.SetActive(true);
        }
        else if (PontoTimeB > 10 && (udpClient.myId == 1 || udpClient.myId == 2))
        {
            VitoriaRemote.gameObject.SetActive(true);
        }
        else if (PontoTimeB > 10 && (udpClient.myId == 3 || udpClient.myId == 4))
        {
            VitoriaLocal.gameObject.SetActive(true);
        }
    }
}
