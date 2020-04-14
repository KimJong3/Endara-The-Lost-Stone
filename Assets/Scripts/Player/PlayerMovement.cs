﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using DG.Tweening;
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{

    [Header("Animator Components")]
    CharacterController player;
    Animator anim;
    [SerializeField]
    Animator animDead;
    

    [SerializeField] 
    Camera mainCam;
    [SerializeField]
    CoinManager _coinManager;
    PlayerLifeManager playerLifeManager;
    GodManager godManager;
    public Transform initialPosition;
    [SerializeField] CapsuleCollider attackCollider;
    [Header("Velocidad")]
    public float speedMax;
    public float acceleration = 20f;
    private float currentSpeed = 0f;
    [Header("Controles")]
    //[Range(0,1000)][SerializeField] float speedAceleration;
    private Vector3 playerInput;
    private Vector3 movePlayer;
    private Vector3 camForward;
    private Vector3 camRight;
    [Header("Gravedad")]
    [SerializeField] float gravity;
    [SerializeField] float jumpForce;
    float fallvelocity;

    [Header("Fuerza de empuje")]
    public float pushPower = 2f;

    [Header("Respawn Position")]
    [SerializeField]
    Transform respawnCheckpoint;
    [Header("Particle Coin")]
    [SerializeField]
    GameObject _prefabParticleCoin;
    float unitsGod;


    //Variables booleanas
    public static bool canMove = true;
    public bool isGod;
    [SerializeField] bool isInitialPosition;
    public bool doubleJump = true;
    [SerializeField]
    bool isInMovingPlattform;
    public bool playerIn2D;

    private void Start()
    {
        attackCollider.enabled = false;
        player = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
        playerLifeManager = GetComponent<PlayerLifeManager>();
        //godManager = GameObject.Find("Mode God Manager").GetComponent<GodManager>();
        //Cursor.visible = false;
        //Cursor.lockState = CursorLockMode.Locked;
        movePlayer.y = 0;


    }
    void Update()
    {
        if (canMove)
        {
            Movimiento();
        }
        else
        {
            anim.SetFloat("PlayerWalkVelocity", 0);
        }
    }
    public void Movimiento()
    {
        Vector3 movePlayerXZ = Vector3.zero;
        //Obtener los inputs del Input Manager
        playerInput = new Vector3(InputManager.Vector2Movement().x, 0, InputManager.Vector2Movement().y);
        playerInput = Vector3.ClampMagnitude(playerInput, 1);

        //Calcular la direccion del player respecto a la camara
        CamDirection();
        movePlayer = playerInput.x * camRight + playerInput.z * camForward;

        //Calcular el angulo del jostyck de moviemiento y asignarlo a la rotacion del player
        float shortestAngle = Vector3.SignedAngle(transform.forward, movePlayer, Vector3.up);
        transform.Rotate(Vector3.up * shortestAngle / 1.5f);

        SetGravity();
        JumpPlayer();
        //Aceleracion
        if (movePlayer.magnitude > 0.1f)
        {
            currentSpeed += acceleration * Time.deltaTime;
            currentSpeed = Mathf.Clamp(currentSpeed, 0f, speedMax);
        }
        else
        {
            currentSpeed -= acceleration * Time.deltaTime;
            currentSpeed = Mathf.Clamp(currentSpeed, 0f, speedMax);
        }

        if (PickUpObjects.IsCatchedObject())
        {
            movePlayerXZ = (movePlayer * (currentSpeed / PickUpObjects.MassObjectPicked() * Time.deltaTime));
        }
        else
        {
            movePlayerXZ = (movePlayer * currentSpeed * Time.deltaTime);
        }

        //Si estas en 2.5D, el eje Z se desactiva
        if (playerIn2D)
        {
            movePlayerXZ.z = 0;
        }

        //Asignar el movimiento al vector
        movePlayer.x = movePlayerXZ.x;
        movePlayer.z = movePlayerXZ.z;

        //Asignar movimiento al Character Controller
        player.Move(movePlayerXZ);
        movePlayerXZ.y = 0;

        //Pasar informacion al animator
        anim.SetBool("isGrounded", player.isGrounded);
        anim.SetFloat("PlayerWalkVelocity", playerInput.magnitude * currentSpeed);
    }
    public void SetGravity()
    {
        if (isGod)
        {
            ModeGod();
        }
        else
        {
            anim.SetBool("isGrounded", player.isGrounded);
            if (player.isGrounded)
            {
                fallvelocity = -gravity * Time.deltaTime;
                movePlayer.y = fallvelocity;
            }
            else
            {
                fallvelocity -= gravity * Time.deltaTime;
                movePlayer.y = fallvelocity;
                anim.SetFloat("PlayerVerticalVelocity", player.velocity.y);
            }
        }
    }
    void ModeGod()
    {
        unitsGod = godManager.UnitsToJumpInModeGod();
        if (isGod)
        {
            Debug.Log("Es Dios");
            if (Input.GetKey(KeyCode.Space))
            {
                movePlayer.y += unitsGod;
            }
            else if (Input.GetKey(KeyCode.LeftShift))
            {
                movePlayer.y -= unitsGod;

            }

            playerLifeManager.enabled = false;
        }
    }
    public void SetPlayer2D(bool b)
    {
        playerIn2D = b;
    }
    void JumpPlayer()
    {
        if (InputManager.playerInputs.PlayerInputs.Jump.triggered)
        {
            if (!doubleJump)
            {
                return;
            }

            if (!player.isGrounded && !isInMovingPlattform)
            {
                doubleJump = false;

            }
            Debug.Log("He saltado");
            if (PickUpObjects.IsCatchedObject())
            {
                float myJumpForce = jumpForce / PickUpObjects.MassObjectPicked();
                fallvelocity = myJumpForce;
                print(myJumpForce);
            }
            if (!PickUpObjects.IsCatchedObject())
            {
                fallvelocity = jumpForce;
                print(jumpForce);
            }
            movePlayer.y = fallvelocity;
            anim.SetTrigger("PlayerJump");

        }

        if (!doubleJump &&(player.isGrounded || isInMovingPlattform))
        {
            doubleJump = true;
        }

    }
    void CamDirection()
    {
        camForward = mainCam.transform.forward;
        camRight = mainCam.transform.right;

        camForward.y = 0;
        camRight.y = 0;

        camForward = camForward.normalized;
        camRight = camRight.normalized;
    }
    public void PlayerDead()
    {
        if(playerLifeManager.AttempsPlayer() <= 0)
        {
            StartCoroutine(DeadCanvasAnimation());
        }
    }
    public void RespawnToWaypoint()
    {
        if (respawnCheckpoint != null)
        {
            transform.position = respawnCheckpoint.position;
        }
        else
        {
            transform.position = initialPosition.position;
        }
        return;
    }
    public void MeleAtack()
    {
        transform.DOLocalRotate(new Vector3(0, 360, 0), .5f, RotateMode.LocalAxisAdd).OnComplete(() => attackCollider.enabled = false);
        attackCollider.enabled = true;
    }
    public void SetRespawn(Transform t)
    {
        respawnCheckpoint = t;
    }
    public void SetMovingPlattform(bool b)
    {
        isInMovingPlattform = b;
        print(isInMovingPlattform);
    }
    
    IEnumerator DeadCanvasAnimation() // Cuando te quedas sin vida
    {
        if (playerLifeManager.AttempsPlayer() <= 0)
        {
            animDead.SetTrigger("StartDead");
            yield return new WaitForSeconds(1f);
            RespawnToWaypoint();
            yield return new WaitForSeconds(1f);
            animDead.SetTrigger("EndDead");
            playerLifeManager.SetLifeToMax();
        }
        
    }
    
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        ////Salto entre plataformas
        //if (!player.isGrounded && hit.normal.y < 0.1f)
        //{
        //Debug.Log("He saltado en entre plataformas");

        //if (Input.GetKeyDown(KeyCode.Space))
        //    {
        //    verticalVelocity = jumpForce;
        //    movePlayer = hit.normal * 1.5f;
        //    }

        //}
        float valueMass;
        if (hit.collider.gameObject.name != "Baldosa")
        {
            Rigidbody rb = hit.collider.attachedRigidbody;
            if (rb == null || rb.isKinematic)
            {
                return;
            }

            if (hit.moveDirection.y < -0.3f)
            {
                return;
            }

            valueMass = rb.mass;
            Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

            rb.velocity = (pushDir * pushPower) / valueMass;
        }


    }
    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Final")
        {
            SceneManager.LoadScene("VictoryScreen");
        }
        
        if (other.CompareTag("Coin"))
        {
            Instantiate(_prefabParticleCoin, other.transform.position, Quaternion.identity);
            _coinManager.SumCoins();
            Destroy(other.gameObject);
        }
    }
    private void OnParticleCollision(GameObject other)
    {
        if (other.CompareTag("Coin"))
        {
            Instantiate(_prefabParticleCoin, other.transform.position, Quaternion.identity);
            _coinManager.SumCoins();
            Destroy(other.gameObject);
        }
    }

}