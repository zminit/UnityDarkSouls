using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Utils
{
    public class SortedArray<T> : IEnumerable
    {
        public int Length = 0;
        private T[] array = new T[10];
        Func<T, T, bool> compare;
        public SortedArray(Func<T, T, bool> compare)
        {
            this.compare = compare;
        }

        private void Expand()
        {
            int newL = Length * 2;
            T[] newArray = new T[newL];
            Array.Copy(array, newArray, Length);
            array = newArray;
        }

        private void Sort(int l, int r)
        {
            if(l >= r)
            {
                return;
            }
            int lp = l;
            int rp = r;
            while(lp < rp)
            {
                if (compare(array[lp], array[rp]))
                {
                    rp--;
                }
                else
                {
                    T pivot = array[lp];
                    array[lp] = array[rp];
                    array[rp] = pivot;
                    lp++;
                }
            }
            Sort(l, lp - 1);
            Sort(rp + 1, r);
        }

        public void Add(T v)
        {
            if(Length == array.Length)
            {
                Expand();
            }
            array[Length++] = v;
            Sort(0, Length - 1);
        }

        public IEnumerator GetEnumerator()
        {
            for(int i = 0; i < Length; i++)
            {
                yield return array[i];
            }
        }
    }
    public class UtilsTemp : MonoBehaviour
    {
        Transform mytrans;

        private void Start()
        {
            mytrans = GetComponent<Transform>();
        }
        private void Update()
        {
        }
    }
    
}

