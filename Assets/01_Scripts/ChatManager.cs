using System;
using TMPro;
using UnityEngine;

public class ChatManager: MonoBehaviour {
  [SerializeField] private GameObject chatMessageInputObject;
  [SerializeField] private GameObject chatHistoryPanelObject;

  private TMP_InputField chatInputField;
  private TMP_InputField chatHistoryPanel;

  private ChatServer server;
  private ChatClient client;

  private void Start() {
    chatInputField = chatMessageInputObject.GetComponent<TMP_InputField>();
    chatHistoryPanel = chatHistoryPanelObject.GetComponent<TMP_InputField>();
  }

  public void StartServer() {
    StopServer();

    server = new();
    server.StartListen();
  }

  public void StopServer() {
    server?.Close();
    server = null;
  }

  public void StartClient() {
    StopClient();

    client = new();
    client.ChatReceivedEvent += OnClientChatReceived;
    client.ChatSentEvent += OnClientChatSent;
    client.Connect();
  }

  public void StopClient() {
    client?.Close();
    client = null;
  }

  public void SendChat() {
    string message = chatInputField.text;
    client.SendChat(ChatType.Text, message);
  }

  public void ClearChatFields() {
    chatInputField.text = "";
    chatHistoryPanel.text = "";
  }

  private void OnClientChatReceived(object sender, ChatContent chat) {
    MainThreadWorker.Instance.EnqueueJob(() => {
      chatHistoryPanel.text += $"<b>Player {chat.PlayerNumber}</b>: {chat.Content}\n";
    });
  }

  private void OnClientChatSent(object sender, EventArgs _) {
    chatInputField.text = "";
  }
}
