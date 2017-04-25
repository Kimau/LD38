using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

using UnityEngine;

public class TwitchUDPLinker : MonoBehaviour
{
  public int remotePort;
  public int listenPort;

  public delegate void TwitchHandleMsg(TwitchMsg msg);
  public TwitchHandleMsg msgHandlerDel;

  // receiving Thread
  Thread receiveThread;
  UdpClient client;

  static Queue<string> msgQ;
  static Queue<string> sayQ;

  // start from unity3d
  public void Start()
  {
    Debug.Log("Starting Listener");
    Application.runInBackground = true;

    msgQ = new Queue<string>();
    sayQ = new Queue<string>();

    receiveThread = new Thread(new ThreadStart(ReceiveData));
    receiveThread.IsBackground = true;
    receiveThread.Start();
  }

  public static bool Sub(TwitchHandleMsg del)
  {
    var link = FindObjectOfType<TwitchUDPLinker>();
    if (link == null)
    {
      Debug.LogError("Cannot find Twitch Link");
      return false;
    } else
    {
      link.msgHandlerDel = del;
      return true;
    }    
  }

  public static void Say(string msg)
  {
    lock(sayQ)
    {
      sayQ.Enqueue(msg);
    }
  }

  public void OnDisable()
  {
    receiveThread.Abort();
    if (client != null) client.Close();
  }

  public void Update()
  {
    List<string> newChatMsgs = new List<string>();

    // Lock and fetch msgs
    lock (msgQ)
    {
      while (msgQ.Count > 0)
      {
        newChatMsgs.Add(msgQ.Dequeue());
      }
    }

      // Process Messages
      foreach (string line in newChatMsgs)
    {
      string content = line;

      if (content[0] == '[')
      {
        // Read Tag
        int endTag = content.IndexOf(']');
        string tag = content.Substring(1, endTag - 1);
        content = content.Substring(endTag + 1);

        Debug.Log("Tag: " + tag + " Content: " + content);
      }
      else if ((content[0] == '{') && (msgHandlerDel != null))
      {
        TwitchMsg msg = TwitchMsg.CreateFromJSON(content);

        internalHandleMsg(msg);
      }
      else
      {
        Debug.Log(line);
      }
    }
  }

  private void internalHandleMsg(TwitchMsg msg)
  {
    msgHandlerDel(msg);
  }

  public bool debugUI = false;
  List<TwitchMsg> fakePlayers = new List<TwitchMsg>();
  string newfakenick = "nickme";
  void OnGUI()
  {
    if (!debugUI)
      return;

    // Make a background box
    int y = 400;
    GUI.Box(new Rect(10, y, 100, 110), "Fake Message"); y += 25;
    newfakenick = GUI.TextField(new Rect(10, y, 100, 20), newfakenick); y += 25;
    if (GUI.Button(new Rect(20, y, 80, 20), "Add"))
    {
      TwitchMsg msg = new TwitchMsg();
      msg.cat = 35;
      msg.body = "!join";
      msg.msg = new TwitchSubMsg();
      msg.msg.userid = "_" + newfakenick;
      msg.msg.nick = newfakenick;
      msg.msg.content = "!join";
      msg.msg.bits = 0;
      msg.msg.badge = "";
      fakePlayers.Add(msg);

      internalHandleMsg(msg);

      newfakenick += "1";
    }

    int x = 120;
    foreach (var p in fakePlayers)
    {
      y = 480;
      p.msg.content = GUI.TextField(new Rect(x, y, 100, 20), p.msg.content); y += 25;
      if (GUI.Button(new Rect(x + 10, y, 80, 20), "Submit"))
      {
        internalHandleMsg(p);
      }

      x += 110;
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

        // Outgoing Msgs
        List<string> newChatMsgs = new List<string>();
        lock (sayQ)
        {
          while (sayQ.Count > 0)
          {
            newChatMsgs.Add(sayQ.Dequeue());
          }
        }

        // Send Message
        foreach (string line in newChatMsgs)
        {
          byte[] msgData = Encoding.UTF8.GetBytes("[say]"+line);
          client.Send(msgData, msgData.Length);
        }

      }
      catch (Exception err)
      {
        print(err.ToString());
      }

    }
  }
}
