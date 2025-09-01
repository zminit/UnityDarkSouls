using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public class PriorityQueue<TKey, TValue> 
    {

        public int Length = 0;
        private KeyValuePair<TKey, TValue>[] queue = new KeyValuePair<TKey, TValue>[10];

        private void Expand()
        {
            int newCapacity = Length * 2;
            KeyValuePair<TKey, TValue>[] newQueue = new KeyValuePair<TKey, TValue>[newCapacity];
            Array.Copy(queue, newQueue, newCapacity);
            queue = newQueue;
        }

    }

}
