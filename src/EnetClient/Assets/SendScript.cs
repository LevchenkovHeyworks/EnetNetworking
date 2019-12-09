using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ENet;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.UI;

public class SendScript : MonoBehaviour
{
    [SerializeField] private InputField inputField;
    [SerializeField] protected Text text;

    private Button button;

    private const ushort port = 33445;
    private const string ip = "192.168.88.199";

    private const int ClientTickRate = 64;

    private Host client;
    private Peer peer;

    private bool isRunned;
    private CancellationTokenSource cancellationTokenSource;

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(Click);

        ENet.Library.Initialize();

        cancellationTokenSource = new CancellationTokenSource();

        client = new Host();
        Address address = new Address();

        address.SetHost(ip);
        address.Port = port;
        client.Create();

        peer = client.Connect(address, 4);
        //Task.Run(() => RunLoop(cancellationTokenSource.Token));

        isRunned = true;
    }

    void Update()
    {
        ENet.Event netEvent;
        if (!cancellationTokenSource.IsCancellationRequested)
        {
            //client.Service(1000 / ClientTickRate, out netEvent);
            client.Service(0, out netEvent);
            Debug.Log("hi");

            switch (netEvent.Type)
            {
                case ENet.EventType.None:
                    break;

                case ENet.EventType.Connect:
                    text.text = "Client connected to server";
                    break;

                case ENet.EventType.Disconnect:
                    text.text = "Client disconnected from server";
                    break;

                case ENet.EventType.Timeout:
                    text.text = "Client connection timeout";
                    break;

                case ENet.EventType.Receive:

                    text.text += "Packet received from server - Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length;

                    var data = new byte[netEvent.Packet.Length];
                    netEvent.Packet.CopyTo(data);
                    var message = Encoding.UTF8.GetString(data);
                    text.text += "\nEcho from server: " + message;

                    netEvent.Packet.Dispose();
                    break;
            }
        }
    }

    void Click()
    {
        if (isRunned)
        {
            var message = inputField.text;
            if (message == "q")
            {
                isRunned = false;
            }
            else
            {
                Send(message);
            }
        }
        else
        {
            client.Flush();
            client.Dispose();

            ENet.Library.Deinitialize();
        }

}

    private void Send(string message)
    {
        var data = Encoding.UTF8.GetBytes(message);
        Packet packet = default(Packet);
        packet.Create(data);
        peer.Send(0, ref packet);
    }
}

