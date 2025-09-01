using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace CFSM
{
    public class BlackBoard
    {
        private Dictionary<string, object> board = new();

        public void SetValue(string key, object value)
        {
            board[key] = value;
        }

        public bool GetValue(string key, out object value)
        {
            if (board.ContainsKey(key))
            {
                value = board[key];
                return true;
            }
            value = null;
            return false;
        }
    }

    public class CharacterFSM : MonoBehaviour, IStateMachine
    {
        public GameObject player;
        public PlayerManager playerManager;
        public Transform playerTransform;
        public Rigidbody playerBody;
        public Camera mainCamera;
        public Animator animator;
        public InputHandler inputHandler;
        public BlackBoard bb;
        
        Dictionary<int, BaseState> StateTable { set; get; }//×´Ì¬±í
        int currentStateIndex;


        // Start is called before the first frame update
        private void Awake()
        {
            player = gameObject;
            playerManager = GetComponent<PlayerManager>();
            playerTransform = GetComponent<Transform>();
            playerBody = GetComponent<Rigidbody>();
            mainCamera = Camera.main;
            animator = GetComponentInChildren<Animator>();
            inputHandler = GetComponent<InputHandler>();
            bb = new BlackBoard();
            StateTable = new Dictionary<int, BaseState>();
            StateTable[0] = new LocomotionState(this);
        }

        void Start()
        {
            currentStateIndex = 0;
            SwitchState(currentStateIndex);
        }

        // Update is called once per frame
        public void StateUpdate()
        {
            StateTable[currentStateIndex].Update();
        }

        public void StateFixedUpdate()
        {
            StateTable[currentStateIndex].FixedUpdate();
        }
        void Update()
        {
            StateUpdate();
        }

        private void FixedUpdate()
        {
            StateFixedUpdate();
        }
    
        public void SwitchState(int id)
        {
            if (!StateTable.ContainsKey(id))
                return;
            StateTable[currentStateIndex].Exit();
            currentStateIndex = id;
            StateTable[currentStateIndex].Enter();
        }

        public void MakeTransition(int id, int priority, Func<bool> listen, Action transAction)
        {
            if(StateTable.ContainsKey(id))
                StateTable[id].AddListen(priority, listen, transAction);
        }
    }
}
