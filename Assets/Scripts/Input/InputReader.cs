using UnityEngine;
using UnityEngine.InputSystem;

public class InputReader : MonoBehaviour, PlayerControlls.IPlayerActions, PlayerControlls.IUIActions
{
    [Header("Mode")]
    [SerializeField] private InputMode defaultMode = InputMode.Gameplay;
    [SerializeField] private InputMode currentMode = InputMode.Gameplay;

    [Header("Snapshots")]
    [SerializeField] private PlayerInputSnapshot player = new PlayerInputSnapshot();
    [SerializeField] private UIInputSnapshot ui = new UIInputSnapshot();

    private PlayerControlls controls;

    public PlayerInputSnapshot Player => player;
    public UIInputSnapshot UI => ui;
    public InputMode CurrentMode => currentMode;
    public bool AcceptsGameplayInput => currentMode == InputMode.Gameplay || currentMode == InputMode.GameplayAndUI;
    public bool AcceptsUIInput => currentMode == InputMode.UIOnly || currentMode == InputMode.GameplayAndUI;

    private void Awake()
    {
        controls = new PlayerControlls();
        controls.Player.SetCallbacks(this);
        controls.UI.SetCallbacks(this);
        currentMode = defaultMode;
    }

    private void OnEnable()
    {
        ApplyMode();
    }

    private void OnDisable()
    {
        controls?.Player.Disable();
        controls?.UI.Disable();
        player.Reset();
        ui.Reset();
    }

    private void OnDestroy()
    {
        if (controls == null)
            return;

        controls.Player.RemoveCallbacks(this);
        controls.UI.RemoveCallbacks(this);
        controls.Dispose();
        controls = null;
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        player.Tick(deltaTime);
        ui.Tick(deltaTime);
    }

    private void LateUpdate()
    {
        player.ClearFrameState();
        ui.ClearFrameState();
    }

    public void SetMode(InputMode mode)
    {
        if (currentMode == mode)
            return;

        currentMode = mode;
        ApplyMode();
    }

    private void ApplyMode()
    {
        if (controls == null)
            return;

        switch (currentMode)
        {
            case InputMode.Gameplay:
                controls.Player.Enable();
                controls.UI.Disable();
                ui.Reset();
                break;
            case InputMode.UIOnly:
                controls.Player.Disable();
                controls.UI.Enable();
                player.Reset();
                break;
            case InputMode.GameplayAndUI:
                controls.Player.Enable();
                controls.UI.Enable();
                break;
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        player.SetMove(context.canceled ? Vector2.zero : context.ReadValue<Vector2>());
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        player.SetLook(context.canceled ? Vector2.zero : context.ReadValue<Vector2>());
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        UpdateButton(player.Interact, context);
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        UpdateButton(player.Crouch, context);
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        UpdateButton(player.Jump, context);
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        UpdateButton(player.Sprint, context);
    }

    public void OnRB(InputAction.CallbackContext context)
    {
        UpdateButton(player.LightAttack, context);
    }

    public void OnRT(InputAction.CallbackContext context)
    {
        UpdateButton(player.HeavyAttack, context);
    }

    public void OnLB(InputAction.CallbackContext context)
    {
        UpdateButton(player.Guard, context);
    }

    public void OnLT(InputAction.CallbackContext context)
    {
        UpdateButton(player.LeftTrigger, context);
    }

    public void OnAlt(InputAction.CallbackContext context)
    {
        UpdateButton(player.Alt, context);
    }

    public void OnNavigate(InputAction.CallbackContext context)
    {
        ui.SetNavigate(context.canceled ? Vector2.zero : context.ReadValue<Vector2>());
    }

    public void OnSubmit(InputAction.CallbackContext context)
    {
        UpdateButton(ui.Submit, context);
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        UpdateButton(ui.Cancel, context);
    }

    public void OnPoint(InputAction.CallbackContext context)
    {
        ui.SetPoint(context.canceled ? Vector2.zero : context.ReadValue<Vector2>());
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        UpdateButton(ui.Click, context);
    }

    public void OnRightClick(InputAction.CallbackContext context)
    {
        UpdateButton(ui.RightClick, context);
    }

    public void OnMiddleClick(InputAction.CallbackContext context)
    {
        UpdateButton(ui.MiddleClick, context);
    }

    public void OnScrollWheel(InputAction.CallbackContext context)
    {
        ui.SetScrollWheel(context.canceled ? Vector2.zero : context.ReadValue<Vector2>());
    }

    public void OnTrackedDevicePosition(InputAction.CallbackContext context)
    {
    }

    public void OnTrackedDeviceOrientation(InputAction.CallbackContext context)
    {
    }

    private static void UpdateButton(InputButtonState button, InputAction.CallbackContext context)
    {
        if (context.started || context.performed)
            button.SetPressed(true);
        else if (context.canceled)
            button.SetPressed(false);
    }
}
