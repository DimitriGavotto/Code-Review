using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AltoMovement : MonoBehaviour
{
    //enum representing the strength of player input
    private enum EMoveStrength
    {
        NULL,
        MINIMAL,
        MEDIUM,
        MAXIMUM
    }

    #region Variables

    #region Visible

    [Header("Text")] public TextMeshProUGUI accelerationText;
    public TextMeshProUGUI groundedText;
    public TextMeshProUGUI magnitudeText;
    public TextMeshProUGUI massText;
    public TextMeshProUGUI gravityText;
    public TextMeshProUGUI dragText;
    public TextMeshProUGUI dynamicFrictionText;
    public TextMeshProUGUI staticFrictionText;
    public TextMeshProUGUI bouncinessText;
    public TextMeshProUGUI jetpackForceText;
    public TextMeshProUGUI deadZoneText;

    [Space, Header("Alto variables")] [SerializeField, Tooltip("ray cast checking on this layer")]
    private LayerMask floor;

    [SerializeField, Tooltip("Origin of RayCast")]
    Transform tposition;

    [SerializeField, Tooltip("Line renderer for magnitude and direction")]
    LineRenderer[] lines;

    [SerializeField, Tooltip("ball representing center of gravity ")]
    private Transform forceOnBoardPosition;

    [SerializeField, Tooltip("rotate the line renderer")]
    Transform rotation;

    [SerializeField] private Transform altoBoard;

    [Space, Header("Movement"), Tooltip("gravity force")]
    public float _gravity = 9.81f;

    [Tooltip("board acceleration")] public float acceleration = 17;
    [Tooltip("Jetpack acceleration")] public float jetpackForce = 20;


    [Tooltip("Zone where the input of the alto will be ignored")]
    public float deadZone = 0.4f;

    [Space, Header("Testing"), SerializeField]
    private bool disabledHaptics;

    #endregion

    #region Invisible

    private bool jetpackOn;
    private bool _grounded;

    [HideInInspector] public CapsuleCollider _bodyCollider;


    [HideInInspector] public Rigidbody rigi;

    [HideInInspector] public float magnitude;

    [HideInInspector] public float degree;

    private Vector3 direction;

    public bool JetpackOn => jetpackOn;

    #endregion

    #endregion


    public void Start()
    {
        _bodyCollider = GetComponent<CapsuleCollider>();
        rigi = GetComponent<Rigidbody>();

        #region Assign default physics values

        rigi.drag = 0.7f;
        var material = _bodyCollider.material;

        material.bounciness = 0.3f;
        material.staticFriction = 0.6f;
        material.dynamicFriction = 0.6f;

        #endregion

        PlayerRefMan.instance.altoMovement = this;
        LoadPhysicData();
    }

    //get input from specific player
    public void ReceiveMyAltoData(float _degree, float _magnitude)
    {
        degree = _degree;
        magnitude = _magnitude;
    }


    private void FixedUpdate()
    {
        direction = DirFromAngle(degree, false).normalized;

        if (AltoManager.Instance.canMove)
        {
            Move();
        }

        AltoVisualisation();
        TextUpdate();
    }


    private void Move()
    {
        //apply gravity
        rigi.AddForce(Vector3.down * _gravity);

        //apply jetpack force when on
        if (jetpackOn)
            rigi.AddForce(Vector3.up * jetpackForce);


        //todo: add different controls for in air and on ground

        //calculate movement direction
        RaycastingMovement();


        var ray = new Ray(tposition.position, Vector3.down);

        //check if grounded
        _grounded = Physics.SphereCast(ray, 0.45f, (_bodyCollider.height / 2) * 1.5f, floor);

        //apply force if input is above the deadZone
        if (IsEngaged(magnitude))
        {
            rigi.AddForce(direction * acceleration);
        }
    }

    private void RaycastingMovement()
    {
        //set up position the height and the raycst origine
        Vector3 position = tposition.position;
        float height = _bodyCollider.height;
        Vector3 startOfRaycast =
            new Vector3(position.x, position.y - ((height / 2) - ((height / 2) * 0.1f)), position.z);

        //create 3 different ray cast for the 3 type of situations
        var ray = new Ray(position, Vector3.down);
        var rayRamp = new Ray(startOfRaycast, direction);
        var rayRampDown = new Ray(startOfRaycast + direction / 2, Vector3.down);

        //check if alto is touching the floor
        if (!Physics.Raycast(ray, out var hitInfo, 1.2f, floor))
            return;

        Vector3 directionForRamps;

        //change the direction of force if going up ramp
        if (Physics.Raycast(rayRamp, out var hitPosition, 0.5f, floor))
        {
            directionForRamps = (hitPosition.point - hitInfo.point).normalized;
            direction = directionForRamps;
        }
        //change the direction of force if going down ramp
        else
        {
            if (!Physics.Raycast(rayRampDown, out var hitPositionDown, 0.5f, floor))
                return;

            directionForRamps = (hitPositionDown.point - hitInfo.point).normalized;
            direction = directionForRamps;
        }
    }


    private void OnCollisionEnter()
    {
        if (disabledHaptics) return;

        int playerNumber = PlayerRefMan.instance.localPlayer.playerNumber;

        //todo set up case 3 and 4

        // determine which alto should vibrate depending on playerNumber
        switch (playerNumber)
        {
            case 1:
                PlayerRefMan.instance.localPlayer.CmdSendHapticsRight1(false, 1);
                PlayerRefMan.instance.localPlayer.CmdSendHapticsLeft1(false, 1);
                break;
            case 2:
                PlayerRefMan.instance.localPlayer.CmdSendHapticsRight2(false, 1);
                PlayerRefMan.instance.localPlayer.CmdSendHapticsLeft2(false, 1);
                break;

            case 3:
                throw new NotImplementedException("No haptic set up yet for a third player");

            case 4:
                throw new NotImplementedException("No haptic set up yet for a fourth player");

            default:
                Debug.LogError(
                    $"Wrong player number for alto haptic, playerNumber:{PlayerRefMan.instance.localPlayer.playerNumber}");
                return;
        }
    }

    #region TOOLS

    //transform a direction from an angle into a vector 3 direction
    private Vector3 DirFromAngle(float angleInDegree, bool angleIsGlobal)
    {
        //if not global add the current y rotation of the object
        if (!angleIsGlobal)
        {
            angleInDegree += transform.eulerAngles.y;
        }

        // get the x and z position of a angle  and set the y to zero
        return new Vector3(Mathf.Sin(angleInDegree * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegree * Mathf.Deg2Rad));
    }

    public void ToggleJetPack(bool value)
    {
        jetpackOn = value;
    }

    private void AltoVisualisation()
    {
//        var dir = DirFromAngle(degree, false).normalized;

        //determine the axis of rotation
        var myCrossProduct = Vector3.Cross(direction, Vector3.up);

        // Rotate the alto board depending of user input
        altoBoard.localRotation = Quaternion.identity;
        altoBoard.Rotate(myCrossProduct, -magnitude * 15, Space.Self);


        // position the Centre of gravity(COG) depending on user input
        var x = Mathf.Sin(Mathf.Deg2Rad * degree) * magnitude;
        var z = Mathf.Cos(Mathf.Deg2Rad * degree) * magnitude;

        var tempVector3OfCOG = new Vector3(x / 3.5f, 0.189f, z / 3.5f);

        // lerp COG
        forceOnBoardPosition.localPosition =
            Vector3.Lerp(forceOnBoardPosition.localPosition, tempVector3OfCOG, 2 * Time.deltaTime);

        //rotate arrows in the direction the user's input
        rotation.transform.rotation = Quaternion.LookRotation(direction.normalized);

        PlayerRefMan.instance.localPlayer.UpdateDegree(degree);

        //active arrows depending of magnitude input
        switch (CurrentMoveStrength(magnitude))
        {
            case EMoveStrength.MINIMAL:
                lines[0].enabled = true;
                lines[1].enabled = false;
                lines[2].enabled = false;
                break;
            case EMoveStrength.MEDIUM:
                lines[0].enabled = true;
                lines[1].enabled = true;
                lines[2].enabled = false;
                break;
            case EMoveStrength.MAXIMUM:
                lines[0].enabled = true;
                lines[1].enabled = true;
                lines[2].enabled = true;
                break;
            case EMoveStrength.NULL:
                lines[0].enabled = false;
                lines[1].enabled = false;
                lines[2].enabled = false;
                break;
            default:
                print($"error magnitude is out of bounds:{magnitude}");
                break;
        }
    }

    private void TextUpdate()
    {
        accelerationText.text = acceleration.ToString("F2") + "A";
        magnitudeText.text = magnitude + "Mag";
        gravityText.text = _gravity.ToString("F2") + "G";
        dragText.text = rigi.drag.ToString("F2") + "D";
        massText.text = rigi.mass.ToString("F2") + "M";
        dynamicFrictionText.text = _bodyCollider.material.dynamicFriction.ToString("F2") + "DF";
        staticFrictionText.text = _bodyCollider.material.staticFriction.ToString("F2") + "ST";
        bouncinessText.text = _bodyCollider.material.bounciness.ToString("F2") + "B";
        jetpackForceText.text = jetpackForce.ToString("F2") + "J";
        deadZoneText.text = deadZone.ToString("F2");
        groundedText.text = "Grounded:" + _grounded;
    }


    private EMoveStrength CurrentMoveStrength(float currentMagnitude)
    {
        if (currentMagnitude > 0.8f) return EMoveStrength.MAXIMUM;
        if (currentMagnitude > 0.6f) return EMoveStrength.MEDIUM;
        if (currentMagnitude > 0.4f) return EMoveStrength.MINIMAL;
        return EMoveStrength.NULL;
    }

    private bool IsEngaged(float magnitudeValue)
    {
        return magnitudeValue >= deadZone;
    }

    public void SavePhysicData()
    {
        Save.SavePhysicsData(this);
    }

    private void LoadPhysicData()
    {
        var data = Save.LoadPhysicData();

        if (data == null)
            return;

        _gravity = data.gravity;
        rigi.mass = data.mass;
        rigi.drag = data.drag;
        rigi.angularDrag = data.angularDrag;
        acceleration = data.acceleration;

        var material = _bodyCollider.material;

        material.dynamicFriction = data.dynamicFritcion;
        material.staticFriction = data.staticFritcion;
        material.bounciness = data.bounciness;
    }

    #endregion

    #region Debugging

    //check if value of vector 3 is valid
    public bool V3ContainsNaN(Vector3 v3)
    {
        return float.IsNaN(v3.x) == true || float.IsNaN(v3.y) == true || float.IsNaN(v3.z) == true;
    }

    #endregion
}