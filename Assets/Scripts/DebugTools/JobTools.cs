using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;


namespace LiquidPlanet.Debug
{
    
    [ExecuteInEditMode]
    public class JobTools : MonoBehaviour
    {
        private JobTools() {}

        private static JobTools _instance = null;
        
        public bool _runParallel;
        // Inner loop batch count used for jobs which use (multiple) rows as operating array
        public uint _batchCountInRow;
        // Inner loop batch count used for jobs where the operating array is determined by the number of hardware threads
        public uint _batchCountInThread;

        //private void Enable()
        //{
        //    _instance = this;
        //}


        public static JobTools Get() => _instance;

        private void OnValidate()
        {
            if (_instance == null)
                _instance = this;
            if (_batchCountInRow == 0)
                _batchCountInRow = 1;
            if (_batchCountInThread == 0)
                _batchCountInThread = 1;
        }
    }

}
