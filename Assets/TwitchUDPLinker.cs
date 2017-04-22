using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using UnityEngine;

public class TwitchUDPLinker : MonoBehaviour
{

  public int remotePort;
  public int listenPort;

  // receiving Thread
  Thread receiveThread;
  UdpClient client;

  static System.Collections.Generic.Queue<string> msgQ;

  // start from unity3d
  public void Start()
  {
    Debug.Log("Starting Listener");

    msgQ = new System.Collections.Generic.Queue<string>();
    
    receiveThread = new Thread(new ThreadStart(ReceiveData));
    receiveThread.IsBackground = true;
    receiveThread.Start();
  }

  public void OnDisable()
  {
    receiveThread.Abort();
    if (client != null) client.Close();
  }

  public void Update()
  {
    lock (msgQ)
    {
      while (msgQ.Count > 0)
      {
        Debug.Log(msgQ.Dequeue());
      }
    }
  }

  void OnApplicationQuit()
  {
    receiveThread.Abort();
    if (client != null) client.Close();
  }


  // receive threadf
  private void ReceiveData()
  {
    client = new UdpClient();
    IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), remotePort);
    client.Connect(endPoint);

    byte[] greetingdata = Encoding.UTF8.GetBytes("Hello");
    client.Send(greetingdata, greetingdata.Length);

    // IPEndPoint endPointListen = new IPEndPoint(IPAddress.Parse("127.0.0.1"), listenPort);

    byte[] pongData = Encoding.UTF8.GetBytes("Pong");

    while (Thread.CurrentThread.IsAlive)
    {
      try
      {
        byte[] data = client.Receive(ref endPoint);
        string text = Encoding.UTF8.GetString(data);
        
        lock (msgQ)
        {
          msgQ.Enqueue(text);
        }
      }
      catch (Exception err)
      {
        print(err.ToString());
      }
   
    }
  }
}
