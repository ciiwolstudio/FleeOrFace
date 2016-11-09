﻿using UnityEngine;
using System.Collections;

public class WaterManager : MonoBehaviour {
    public GameObject water;
    public GameObject wind;
    // Use this for initialization
    public bool playerwaterin = false;
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void OnTriggerEnter(Collider col)
    {
        if (col.tag == "Player")
        {          
            water.GetComponent<AudioSource>().Play();
            wind.GetComponent<AudioSource>().Stop();
        }
        
    }
    public void OnTriggerExit(Collider col)
    {
        if (col.tag == "Player")
        {
            water.GetComponent<AudioSource>().Stop();
            wind.GetComponent<AudioSource>().Play();
        }

    }
}
