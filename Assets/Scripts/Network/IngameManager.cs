﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Current Game Manager.
/// </summary>
public class IngameManager : MonoBehaviour {

    bool _isGamePlaying;
    public bool isGamePlaying
    {
        get { return _isGamePlaying; }
        set { _isGamePlaying = value; }
    }

    Server server;

    /// <summary>
    /// Players' spawning point
    /// </summary>
    //List<Vector3> spawnPoint; -> Necessary?

    /// <summary>
    /// Map number.
    /// </summary>
    public int mapNumber = 0;
    public string[] mapList;

    /// <summary>
    /// 3rd cam mode
    /// </summary>
    public bool is3rdCam = false;

    /// <summary>
    /// Zombie - Human Switching rotation left time (seconds)
    /// </summary>
    public int rotateTimeLeft = 30;
    /// <summary>
    /// Zombie - Human Switching rotation time term (seconds).
    /// Decided randomly(10s~35s) when role rotated.
    /// </summary>
    public int rotateTimeTerm = 30;
    /// <summary>
    /// Last rotated(switched) datetime.
    /// </summary>
    private System.DateTime lastRotatedTime;

    /// <summary>
    /// Attack range.
    /// </summary>
    private readonly float ATTACK_RANGE = 1.3f;
    /// <summary>
    /// Attack angle range
    /// </summary>
    private readonly float ATTACK_ANGLE_RANGE = 10;

    void Start()
    {
        server = GameObject.FindGameObjectWithTag("NetworkController").GetComponent<Server>();
    }

    /// <summary>
    /// Initialize game
    /// </summary>
    /// <returns>Result of initializing. True = success</returns>
    public bool Initialize()
    {
        try
        {
            //Zombie or Human
            DistributeRole();

            //Set each player's position and rotation
            foreach(ClientInfo c in server.clientLists)
            {
                System.Random rnd = new System.Random();
                float x = rnd.Next(5, 195);
                float y = 20;
                float z = rnd.Next(5, 195);
                c.userPosition = new Vector3(x, y, z);
            }

            //Receiving Role rotate timer start => in GameController.cs FixedUpdate() gamestart part. (cross thread..)
        }
        catch (System.Exception e)
        {
            print(e.Message);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Set role to players zombie or human.
    /// Only called from Server.
    /// </summary>
    public void DistributeRole()
    {
        print("role distribute start");
        if (!server.isServerOpened) return;
        int half = server.clientLists.Count / 2;
        for (int i = 0; i < server.clientLists.Count; i++)
        {
            if (i < half) server.clientLists[i].userState = PlayerState.Human;
            else server.clientLists[i].userState = PlayerState.Zombie;
        }
        //mix(shuffle)
        System.Random rnd = new System.Random();
        int n = server.clientLists.Count;
        while (n > 1)
        {
            n--;
            int k = rnd.Next(n + 1);
            int ps = (int)server.clientLists[k].userState;
            server.clientLists[k].userState = (PlayerState)((int)server.clientLists[n].userState);
            server.clientLists[n].userState = (PlayerState)ps;
        }
        //Send role information to clients
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach(ClientInfo ci in server.clientLists)
            sb.AppendFormat("{0}:{1},", ci.identification, (int)ci.userState);
        server.NoticeData(NetCommand.RoleRotate, sb.ToString());

        lastRotatedTime = System.DateTime.Now;
        rotateTimeTerm = rnd.Next(10, 12); //32);
    }

    /// <summary>
    /// Players role rotation timer / DistributeRole caller.
    /// </summary>
    /// <returns></returns>
    public IEnumerator ReceiveRotatePlayerRoleTimer()
    {
        if(server.isServerOpened)
            while(isGamePlaying)
            {
                yield return new WaitForSeconds(0.5f);

                int between = (System.DateTime.Now - lastRotatedTime).Seconds;
                rotateTimeLeft = rotateTimeTerm - between;

                //Send time information to clients
                server.NoticeData(NetCommand.TimeCount, rotateTimeLeft.ToString());

                //Current role time out. Rotate role again.
                if (rotateTimeLeft < 0) DistributeRole();
            }
    }

    /// <summary>
    /// Attack - Kill check.
    /// </summary>
    /// <returns>Identification string of killed player.</returns>
    public string AttackCheck(ref ClientInfo attacker)
    {
        string attacked = string.Empty;
        foreach(ClientInfo c in server.clientLists)
        {
            if (c.identification == attacker.identification) continue;
            print(c.userPosition);
            print(attacker.userPosition);
            print((c.userPosition - attacker.userPosition).sqrMagnitude);
            if((c.userPosition - attacker.userPosition).sqrMagnitude < ATTACK_RANGE)
            {
                if(Vector3.Angle(attacker.userRotation * Vector3.forward, c.userPosition - attacker.userPosition) < ATTACK_ANGLE_RANGE)
                {
                    attacked = c.identification;
                    break;
                }
            }
        }
        return attacked;
    }
}
