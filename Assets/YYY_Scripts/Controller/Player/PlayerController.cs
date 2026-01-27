using UnityEngine;

public class PlayerController : MonoBehaviour
{

    [Header("组件引用")]
    private PlayerMovement playerMovement;

    private PlayerInteraction playerInteraction;
    // private PlayerAttack playerAttack;

    [Header("属性")]
    public float HpMax = 5;
    public float EnergyMax = 180;
    private Vector2 movementInput;
    private bool isShiftHeld;
    [Header("输入切换")]
    public bool enableLegacyInput = false; // 置为false以使用集中式输入管理
    
    void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerInteraction = GetComponent<PlayerInteraction>();
    }

    // Update is called once per frame
    void Update()
    {
        if (enableLegacyInput)
        {
            // 收集移动输入
            movementInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            isShiftHeld = Input.GetKey(KeyCode.LeftShift);
            
            // 传递给移动系统
            playerMovement.SetMovementInput(movementInput, isShiftHeld);
        }
    }




}
