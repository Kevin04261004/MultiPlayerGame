using TMPro;
using UnityEngine;

public class ChatManager: MonoBehaviour {
  [SerializeField] private GameObject p1ChatMessageInputObject;
  [SerializeField] private GameObject p2ChatMessageInputObject;
  [SerializeField] private GameObject chatHistoryPanelObject;

  private TMP_InputField chatHistoryPanel;

  private ChatServer server;
  private readonly ChatClient[] client = { null, null, null };

  private void Start() {
    chatHistoryPanel = chatHistoryPanelObject.GetComponent<TMP_InputField>();
  }

  public void StartServer() {
    server?.Close();
    server = new();

    server.StartListen();
  }

  public void StopServer() {
    server.Close();
  }

  public void StartClient(int pNum) {
    client[pNum]?.Close();
    client[pNum] = new((ushort) pNum);

    client[pNum].Connect();
    client[pNum].ChatReceivedEvent += OnClientChatReceived;
  }

  public void StopClient(int pNum) {
    client[pNum].Close();
  }

  public void SendChat(int pNum) {
    string message = (pNum == 1 ? p1ChatMessageInputObject : p2ChatMessageInputObject).GetComponent<TMP_InputField>().text;
    client[pNum].SendChat(ChatType.Text, message);
  }

  private void OnClientChatReceived(object sender, ChatPacket packet) {
    MainThreadWorker.Instance.EnqueueJob(() => {
      chatHistoryPanel.text += $"<b>Player {packet.SenderPlayerNumber}</b>: {packet.Content}\n";
    });
  }
}
