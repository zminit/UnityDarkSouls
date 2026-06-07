using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class UIInputAdapter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader inputReader;

    [Header("Events")]
    public UnityEvent<Vector2> onNavigate = new UnityEvent<Vector2>();
    public UnityEvent<Vector2> onPoint = new UnityEvent<Vector2>();
    public UnityEvent<Vector2> onScrollWheel = new UnityEvent<Vector2>();
    public UnityEvent onSubmit = new UnityEvent();
    public UnityEvent onCancel = new UnityEvent();
    public UnityEvent<Vector2> onClick = new UnityEvent<Vector2>();
    public UnityEvent<Vector2> onRightClick = new UnityEvent<Vector2>();
    public UnityEvent<Vector2> onMiddleClick = new UnityEvent<Vector2>();

    private Vector2 lastPoint;

    private void Awake()
    {
        if (inputReader == null)
            inputReader = GetComponent<InputReader>();
    }

    private void Update()
    {
        if (inputReader == null || !inputReader.AcceptsUIInput)
            return;

        UIInputSnapshot input = inputReader.UI;

        if (input.Navigate != Vector2.zero)
            onNavigate.Invoke(input.Navigate);

        if (input.Point != lastPoint)
        {
            lastPoint = input.Point;
            onPoint.Invoke(input.Point);
        }

        if (input.ScrollWheel != Vector2.zero)
            onScrollWheel.Invoke(input.ScrollWheel);

        if (input.Submit.WasPressedThisFrame)
            onSubmit.Invoke();

        if (input.Cancel.WasPressedThisFrame)
            onCancel.Invoke();

        if (input.Click.WasPressedThisFrame)
            onClick.Invoke(input.Point);

        if (input.RightClick.WasPressedThisFrame)
            onRightClick.Invoke(input.Point);

        if (input.MiddleClick.WasPressedThisFrame)
            onMiddleClick.Invoke(input.Point);
    }
}
