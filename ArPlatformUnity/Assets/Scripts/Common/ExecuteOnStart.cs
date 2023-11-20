using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ExecuteOnStart : MonoBehaviour
{
    public UnityEvent FunctionToCall;
    // Start is called before the first frame update
    void Start()
    {
        FunctionToCall?.Invoke();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
