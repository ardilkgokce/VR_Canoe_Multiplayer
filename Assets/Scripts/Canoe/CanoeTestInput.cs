using UnityEngine;
using VRCanoe.Canoe;

public class CanoeTestInput : MonoBehaviour
{
    private CanoeMovement _movement;
    
    void Start()
    {
        _movement = GetComponent<CanoeMovement>();
    }
    
    void Update()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            _movement.AddPaddleForce(Vector3.forward * 2f, Vector3.zero, true);
        }
        
        if (Input.GetKey(KeyCode.A))
        {
            _movement.AddPaddleForce(Vector3.forward * 1f, Vector3.up, false);
        }
        
        if (Input.GetKey(KeyCode.D))
        {
            _movement.AddPaddleForce(Vector3.forward * 1f, Vector3.up, true);
        }
    }
}
