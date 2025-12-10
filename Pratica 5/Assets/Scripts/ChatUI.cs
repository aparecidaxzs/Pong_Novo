using UnityEngine;
using TMPro;

public class ChatUI : MonoBehaviour
{
    public TextMeshProUGUI chatBox;
    public TMP_InputField input;
    private TcpChatClient client;

    void Start()
    {
        client = FindObjectOfType<TcpChatClient>();
    }

    void Update()
    {
        while (TcpChatClient.messageQueue.TryDequeue(out string msg))
        {
            chatBox.text += msg + "\n";
        }
    }

    public void Enviar()
    {
        if (string.IsNullOrWhiteSpace(input.text)) return;

        string txt = "Jogador: " + input.text;

        client.Send(txt);

        input.text = "";
        input.Select();
    }

    public void EnviarMensagem()
    {
        string msg = input.text;

        if (!string.IsNullOrEmpty(msg))
        {
            // Envia para o servidor TCP
            EnviarParaServidor(myId + ": " + msg);

            // Adiciona no chat local
            chatBox.text += "\n" + myId + ": " + msg;

            input.text = ""; // Limpa o campo
            input.ActivateInputField(); // Mantém o cursor no input
        }

    }

    if (Input.GetKeyDown(KeyCode.Return))
    {
        EnviarMensagem();
    }

}
