﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tutorial : Room
{
    protected GameObject[] Levers;

    // Start is called before the first frame update
    void Start()
    {
        Levers = GameObject.FindGameObjectsWithTag("Lever");
        Anim = gameObject.GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (LeversActivated())
        {
            Debug.Log("All levers activated - Open the door.");
            Anim.SetTrigger("OpenDoor");

            // This script is no longer needed. Deactivate to reduce impact on performance.
            enabled = false;
        }
    }

    public override bool LeversActivated ()
    {
        bool isAllActivated = true;
        foreach (GameObject lever in Levers)
        {
            Transform handle = lever.transform.Find("Handle");
            
            if (!handle.GetComponent<Animator>().GetBool("LeverActivated"))
            {
                isAllActivated = false;
            }
        }

        return isAllActivated;
    }

}
