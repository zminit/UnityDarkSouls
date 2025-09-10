using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeShaft : MonoBehaviour
{
    float originTime;
    long originFrame;
    struct Plan
    {
        public float remainTime;
        public Action planEvent;
    }
    Utils.SortedArray<Plan> schedule = new Utils.SortedArray<Plan>((x, y) => x.remainTime <= y.remainTime);
    // Start is called before the first frame update
    void Start()
    {
        originFrame = 0;
        originTime = DateTime.Now.Millisecond;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
