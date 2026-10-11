using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Рух")]
    [SerializeField, Range(1f, 20f)]  private float maxSpeed = 8f;
    [SerializeField, Range(5f, 200f)] private float acceleration = 70f;
    [SerializeField, Range(5f, 200f)] private float deceleration = 90f;
    
    [Header("Земля")]
    [SerializeField] private Transform groundCheck;
    [SerializeField, Range(0.05f, 0.5f)] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;
 
    [Header("Стрибок")]
    [SerializeField, Range(0.5f, 8f)] private float baseGravity = 3f;
    [SerializeField, Range(0.5f, 8f)] private float jumpHeight = 3f;
    
    [Header("Прийоми тюнінгу")]
    [SerializeField, Range(0f, 0.4f)] private float coyoteTime = 0.12f;
    [SerializeField, Range(0f, 0.4f)] private float jumpBufferTime = 0.15f;
    [SerializeField, Range(1f, 6f)]   private float fallMultiplier = 2.2f;
    [SerializeField, Range(1f, 8f)]   private float lowJumpMultiplier = 3.5f;
    
    [Header("Вигляд")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [Header("Зворотний звʼязок")]
    [SerializeField] private CameraShake cameraShake;
    [SerializeField, Range(1f, 30f)] private float hardLandingSpeed = 9f;
    [SerializeField] private HitStop hitStop;
    
    [Header("Звук")]
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip[] footsteps;   // щонайменше ТРИ кліпи
    [SerializeField, Range(0f, 0.3f)] private float pitchVariance = 0.12f;

    [SerializeField, Range(0.1f, 0.8f)] private float stepInterval = 0.32f;
    private float stepTimer;


    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    
    private Rigidbody2D body;
    private PlayerControls controls;
    private float moveInput;
    private bool isGrounded;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private bool wasGrounded;
    private float fallSpeed;     // найбільша швидкість падіння перед дотиком
 
    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        controls = new PlayerControls();
        if (cameraShake == null) cameraShake = FindAnyObjectByType<CameraShake>();
        if (hitStop == null) hitStop = FindAnyObjectByType<HitStop>();
    }
 
    // Без Enable введення не приходить — і помилки при цьому не буде
    private void OnEnable()  => controls.Player.Enable();
    private void OnDisable() => controls.Player.Disable();
    private void OnDestroy() => controls.Dispose();
 
    private void Update()
    {
        isGrounded = CheckGround();
        
        // Поповнюємо coyote лише коли стоїмо, а не летимо вгору — див. крок 6
        bool canRefill = isGrounded && body.linearVelocity.y <= 0.01f;
        coyoteCounter = canRefill ? coyoteTime : coyoteCounter - Time.deltaTime;
 
        // Натиск не виконуємо одразу, а ЗАПАМ'ЯТОВУЄМО в таймер
        jumpBufferCounter = controls.Player.Jump.WasPressedThisFrame()
            ? jumpBufferTime
            : jumpBufferCounter - Time.deltaTime;
 
        // Стрибок відбувається, коли активні ОБИДВА таймери
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            Jump();
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }
        
        // Введення читають у Update: FixedUpdate може пропустити короткий натиск
        moveInput = controls.Player.Move.ReadValue<Vector2>().x;

        CheckLanding();
        UpdateFootsteps();
        UpdateVisuals();
    }
 
    private void FixedUpdate()
    {
        float target = moveInput * maxSpeed;
 
        // Гальмуємо ШВИДШЕ, ніж розганяємось — саме це дає відчуття контролю
        float rate = Mathf.Abs(target) > 0.01f ? acceleration : deceleration;
 
        float x = Mathf.MoveTowards(body.linearVelocity.x, target,
            rate * Time.fixedDeltaTime);
        
        
        if (body.linearVelocity.y < 0f)
            // Падає — тягнемо вниз сильніше. Прибирає «зависання» у верхній точці
            body.gravityScale = baseGravity * fallMultiplier;
        else if (body.linearVelocity.y > 0f && !controls.Player.Jump.IsPressed())
            // Летить угору, але клавішу вже відпущено — обрізаємо стрибок гравітацією
            body.gravityScale = baseGravity * lowJumpMultiplier;
        else
            body.gravityScale = baseGravity;

        body.linearVelocity = new Vector2(x, body.linearVelocity.y);
    }
    
    // Коло перекриває всю ширину ніг, а не одну точку
    private bool CheckGround() =>
        Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
 
    // Без візуалізації датчик налагоджувати неможливо
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Application.isPlaying && isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }

    private float JumpVelocity =>
        Mathf.Sqrt(2f * Mathf.Abs(Physics2D.gravity.y) * baseGravity * jumpHeight);
 
    private void Jump()
    {
        body.gravityScale = baseGravity;   // фіксуємо масштаб ДО того, як задати швидкість
        body.linearVelocity = new Vector2(body.linearVelocity.x, JumpVelocity);
    }
    
    // Викликати наприкінці Update()
    private void UpdateVisuals()
    {
        // Джерело істини — фактична швидкість тіла, а не натиснута клавіша
        animator.SetFloat(SpeedHash, Mathf.Abs(body.linearVelocity.x));
        //animator.SetFloat(SpeedHash, moveInput != 0 ? 1 : 0);
 
        // flipX, а НЕ мінусовий масштаб: інакше дочірній GroundCheck переїде
        if (Mathf.Abs(moveInput) > 0.01f)
            spriteRenderer.flipX = moveInput < 0f;
    }

    // Викликати наприкінці Update(), після isGrounded = CheckGround();
    private void CheckLanding()
    {
        if (!isGrounded)
            fallSpeed = Mathf.Max(fallSpeed, -body.linearVelocity.y);
 
        if (isGrounded && !wasGrounded)   // ← саме ПЕРЕХІД, а не стан
            OnLanded(fallSpeed);
 
        if (isGrounded) fallSpeed = 0f;
        wasGrounded = isGrounded;
    }
 
    private void OnLanded(float speed)
    {
        if (speed < hardLandingSpeed) return;
 
        //cameraShake.Shake(0.12f, 0.12f);   // ← весь варіант А
        hitStop.Stop(0.1f);   // ← весь варіант Б
    }
    
    private void PlayFootstep()
    {
        if (footsteps.Length == 0) return;
 
        // Два шари випадковості:
        // 1) різні кліпи — прибирає впізнаваність конкретного звуку
        AudioClip clip = footsteps[Random.Range(0, footsteps.Length)];
 
        // 2) розкид висоти тону — прибирає механічність повторення
        source.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
        source.PlayOneShot(clip);
    }
    
    // Викликати в Update(), після isGrounded = CheckGround();
    private void UpdateFootsteps()
    {
        bool running = isGrounded && Mathf.Abs(body.linearVelocity.x) > 1f;
        //bool running = isGrounded && moveInput != 0;
 
        // Не біжимо — скидаємо таймер, щоб перший крок після старту був одразу
        if (!running) { stepTimer = 0f; return; }
 
        stepTimer -= Time.deltaTime;
        if (stepTimer <= 0f)
        {
            PlayFootstep();
            stepTimer = stepInterval;
        }
    }


}
