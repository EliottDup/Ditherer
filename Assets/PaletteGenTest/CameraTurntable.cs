using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class CameraTurntable : MonoBehaviour
{
    private Vector3 _orbit = Vector3.zero;
    private float _r, _theta, _phi; // dist, angle to Z axis, angle to x axis

    private InputActionMap _actions;

    private InputAction _pan, _hold, _delta, _zoom, _reset;

    public float startDistance = 5;

    [Range(0, 1)]
    [SerializeField]
    private float _orbitSens, _moveSens;

    private void Awake()
    {
        _actions = InputSystem.actions.FindActionMap("TurnTable");
        _pan = _actions.FindAction("Pan");
        _hold = _actions.FindAction("Hold");
        _delta = _actions.FindAction("Delta");
        _zoom = _actions.FindAction("Zoom");
        _reset = _actions.FindAction("Reset");

        _reset.performed += (_) => ResetPos();

        ResetPos();
    }

    void ResetPos()
    {
        _r = startDistance;
        _theta = Mathf.PI / 4;
        _phi = _theta;
        _orbit = Vector3.one * 0.5f;
    }

    private void Update()
    {
        Vector2 delta = _delta.ReadValue<Vector2>();
        if (_hold.IsPressed())
        {
            if (_pan.IsPressed())
            {
                _orbit -= _r * _moveSens * 0.01f * (transform.up * delta.y + transform.right * delta.x);
            }
            else
            {
                delta *= _orbitSens * 0.1f;
                _phi -= delta.x;
                _theta += delta.y;
                _theta = Mathf.Clamp(_theta, .1f, Mathf.PI - .1f);
            }
        }
        if (_zoom.IsInProgress())
        {
            float dir = _zoom.ReadValue<Vector2>().y;
            _r -= dir * _r * 0.2f;
            _r = Mathf.Max(_r, .1f);
        }

        transform.position = _orbit + new Vector3(_r * Mathf.Sin(_theta) * Mathf.Cos(_phi), _r * Mathf.Cos(_theta), _r * Mathf.Sin(_theta) * Mathf.Sin(_phi));
        transform.LookAt(_orbit, Vector3.up);
    }
}
