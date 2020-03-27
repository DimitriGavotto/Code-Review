using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Fish))]
public class FishMovementBehaviour : MonoBehaviour
{
    #region Variables

    #region Visible

    [SerializeField, Header("Fish Moving Behaviour")]
    private float forceApplied = 0.1f;

    [SerializeField, Tooltip("how far the wandering point is placed from the fish")]
    private float wanderingPointDistanceFromFish = 2;

    [SerializeField, Tooltip("how big of a change in direction the fish can do ")]
    private float radiusOfWandering = 2;

    [SerializeField, Tooltip("Layer that the fish will not try to avoid")]
    private LayerMask foodLayer;

    [SerializeField, Tooltip("Fish viewing distance")]
    private float raycastLength = 3;

    [SerializeField, Tooltip("How spread out are new path options")]
    private float gapBetweenRay = 0.3f;

    [SerializeField, Tooltip("How fast the fish can rotate")]
    private float  speedOfRotation = 5;

    [SerializeField, Tooltip("Max amount of options the fish will look for")]
    private int maxAmountOfLoops = 6;

    [SerializeField, Tooltip("Target to follow")]
    private Transform targetTransform;

    [SerializeField, Header("Debugging")] private bool debugging;

    #endregion

    #region Invisible

    public bool wandering;
    public Vector3 target;
    public Vector3 myDirection;
    private float maxRotationSpeed;
    List<Vector3> raycastPoints = new List<Vector3>(500);
    private bool canFindNewWanderingPoint;
    private Material fishMaterial;
    #endregion

    #endregion


    private void Start()
    {
        canFindNewWanderingPoint = true;
        maxRotationSpeed = speedOfRotation * 20;
        target = transform.position + transform.forward;

        //create amount of options the fish will need
        for (int i = 0; i < maxAmountOfLoops * (maxAmountOfLoops * 8); i++)
        {
            raycastPoints.Add(Vector3.zero);
        }
    }


    #region WanderingPosition

    public IEnumerator SetRandomWanderingPoint()
    {
        while (true)
        {
            while (wandering)
            {
                if (canFindNewWanderingPoint)
                {
                    StartCoroutine(TimerBeforeNewWanderingPoint());
                    StartCoroutine(FindNewWanderingPosition());
                }

                yield return new WaitForSeconds(Random.Range(4, 8));
            }

            yield return null;
        }
    }

    //find a a new target for the fish to go to that is not obstructed
    private IEnumerator FindNewWanderingPosition()
    {
        while (true)
        {
            //create a wandering point
            Vector3 temp = Random.insideUnitCircle * radiusOfWandering;
            Vector3 myWanderingPoint = transform.position + transform.forward * wanderingPointDistanceFromFish +
                                       transform.up * temp.x +
                                       transform.right * temp.y;

            Vector3 direction = (myWanderingPoint - transform.position).normalized;
            float distance = Vector3.Distance(myWanderingPoint, transform.position);

            if (debugging)
                Debug.DrawRay(transform.position, direction * distance, Color.yellow);

            Ray ray = new Ray(transform.position, direction);

            var arrayOfHit = Physics.SphereCastAll(ray, 0.25f, distance);

            bool hitSomething = false;

            //check if that new wandering point is not obstructed
            foreach (var obj in arrayOfHit)
            {
                if (obj.transform == transform) continue;
                hitSomething = true;
            }

            //if point is not obstructed assign new target
            if (!hitSomething)
            {
                target = myWanderingPoint;

                if (debugging)
                    Debug.DrawRay(transform.position, direction * distance, Color.green);

                yield break;
            }

            yield return null;
        }
    }

    #endregion

    //set a new target for the fish and stop fish wandering
    public void GoTowardsNewTarget(Transform newTarget)
    {
       fishMaterial.color = Color.black;
        targetTransform = newTarget;
        wandering = false;
    }

    public void UpdateTargetAndFindValidPath()
    {
        if (!wandering)
        {
            if (targetTransform != null)
            {
                target = targetTransform.position;
            }
            //if don't have a target then go back to wandering
            else
            {
                wandering = true;
            }
        }

        DetermineValidPath(target);
        RotateTheFish();
    }

    public void DetermineValidPath(Vector3 targetPosition)
    {
        Ray ray = new Ray(transform.position, transform.forward);

        Vector3 dir = targetPosition - transform.position;

        float distance = Vector3.Distance(targetPosition, transform.position);

        //check if reached wandering target
        if (wandering && distance <= 0.2f)
        {
            if (canFindNewWanderingPoint)
            {
                StartCoroutine(TimerBeforeNewWanderingPoint());
                StartCoroutine(FindNewWanderingPosition());
            }
        }

        if (debugging)
            Debug.DrawLine(transform.position, targetPosition, Color.gray);

        //check if fish needs to find a new unobstructed path
        if (!CheckIfTargetIsInClearSight(ray, dir)) return;


        int amountOfPointSet = 0;
        int grideSize = 3;
        Vector3 bestRayInList = Vector3.zero;
        int startingPointOfTheRaycastLoop = 8;
        Vector3 directionToTarget = (transform.position - targetPosition).normalized;
        float currentBestDotProduct = 1;

        //create a loop until either finding a valid path or reached the max amount of loops
        for (int i = 0; i < maxAmountOfLoops; i++)
        {
            amountOfPointSet = CreateLoop(grideSize, amountOfPointSet);

            //goes throught all the direction created and find a valid one
            bestRayInList = CheckAllRaycastPoint(directionToTarget, currentBestDotProduct, bestRayInList,
                startingPointOfTheRaycastLoop, amountOfPointSet);

            grideSize += 2;
            startingPointOfTheRaycastLoop += 8;

            //if best ray in list zero means not assigned
            if (bestRayInList != Vector3.zero)
            {
                break;
            }
        }

        //if could not find valid direction then go in opposite direction 
        if (bestRayInList == Vector3.zero)
        {
            myDirection = -transform.forward;
        }
        else
        {
            myDirection = bestRayInList;

            if (debugging)
                Debug.DrawRay(transform.position, myDirection * raycastLength, Color.green);
        }
    }

    private bool CheckIfTargetIsInClearSight(Ray ray, Vector3 dir)
    {
        if (!Physics.SphereCast(ray, 0.01f, raycastLength, ~foodLayer))
        {
            Ray dirToTarget = new Ray(transform.position, dir);

            if (!Physics.SphereCast(dirToTarget, 0.01f, raycastLength / 2, ~foodLayer))
            {
                myDirection = dir.normalized;

                if (debugging)
                    Debug.DrawRay(transform.position, myDirection * raycastLength, Color.black);

                return false;
            }

            myDirection = transform.forward;
            return false;
        }

        return true;
    }

    /// <summary>
    /// creates a loop of points each time bigger than the previous loop,loop are further and further from fish transform.forward 
    /// </summary>
    /// <param name="gridSize"></param> determines the size of the loop that needs to be created
    /// <param name="ammoutDoneAlready"></param> used to determine what next option needs to be assign in raycastpoints
    /// <returns></returns>
    private int CreateLoop(int gridSize, int ammoutDoneAlready)
    {
        int startPoint = Convert.ToInt32((gridSize / 2f) - 0.5f);
        int y = -startPoint;

        //creates the bottom row of the loop
        for (int x = -startPoint; x < gridSize - startPoint; x++)
        {
            raycastPoints[ammoutDoneAlready] =
                transform.position + (transform.forward + ((transform.up * y) * gapBetweenRay) +
                                      transform.right * x * gapBetweenRay);
            ammoutDoneAlready++;
        }

        y++;
        //creates the middle row(s) of the loop
        for (; y < gridSize - startPoint - 1; y++)
        {
            for (int x = -startPoint; x < gridSize - startPoint; x += gridSize - 1)
            {
                raycastPoints[ammoutDoneAlready] =
                    transform.position + (transform.forward + transform.up * y * gapBetweenRay +
                                          transform.right * x * gapBetweenRay);
                ammoutDoneAlready++;
            }
        }

        //creates the top row of the loop
        for (int x = -startPoint; x < gridSize - startPoint; x++)
        {
            raycastPoints[ammoutDoneAlready] =
                transform.position + (transform.forward + transform.up * y * gapBetweenRay +
                                      transform.right * x * gapBetweenRay);
            ammoutDoneAlready++;
        }

        //returns the total amounts of options that have been assigned
        return ammoutDoneAlready;
    }
    
    /// <summary>
    /// Raycast all the new options available in this new loop, and determine if any of those options are a valid option
    /// </summary>
    /// <param name="directionToTarget"></param> normalized vector representing the direction from fish to target
    /// <param name="currentBestDotProduct"></param> save the current closet dotProduct to the directionToTarget 
    /// <param name="bestRayInList"></param> save the current best option for the fish to take
    /// <param name="startOfForLoop"></param> determine the start of this current loop being checked based on the amount of point set
    /// <param name="amountOfPointSet"></param> enable to only check points that have not being checked before
    /// <returns></returns>
    private Vector3 CheckAllRaycastPoint(Vector3 directionToTarget, float currentBestDotProduct, Vector3 bestRayInList,
        int startOfForLoop, int amountOfPointSet)
    {
        startOfForLoop = amountOfPointSet - startOfForLoop;


        // determine if any and which option is the best option
        for (int i = startOfForLoop; i < amountOfPointSet; i++)
        {
            Vector3 dir = (raycastPoints[i] - transform.position).normalized;

            if (debugging)
                Debug.DrawRay(transform.position, dir * raycastLength, Color.red);

            if (Physics.Raycast(transform.position, dir, raycastLength)) continue;

            if (debugging)
                Debug.DrawRay(transform.position, dir * raycastLength, Color.cyan);

            float currentDotProduct = Vector3.Dot(dir.normalized, directionToTarget.normalized);

            if (!(currentDotProduct < currentBestDotProduct)) continue;
            
            currentBestDotProduct = currentDotProduct;
            bestRayInList = dir;
        }

        return bestRayInList;
    }


    private void RotateTheFish()
    {
        // Rotate the forward vector towards the target direction
        Vector3 newDirection =
            Vector3.RotateTowards(transform.forward, myDirection, speedOfRotation * Time.deltaTime, 0.0f);


        if (debugging)
            Debug.DrawRay(transform.position, newDirection, Color.magenta);

        float speed = speedOfRotation;

        //determine if close to an obstacle and increase the speed of rotation depending on how close the fish is to the obstacle
        if (Physics.Raycast(transform.position, newDirection.normalized, out var hit, raycastLength))
        {
            float distance = Vector3.Distance(transform.position, hit.point);
            speed = Mathf.Lerp(maxRotationSpeed, speedOfRotation, distance / raycastLength);
        }

        // Calculate a rotation a step closer to the target and applies rotation to this object
        Quaternion rot = Quaternion.LookRotation(newDirection);

        transform.rotation = Quaternion.Slerp(transform.rotation, rot, speed * Time.deltaTime);

        //move the fish forward
        transform.Translate(Vector3.forward * forceApplied * Time.deltaTime);
    }

    #region Tools

    private IEnumerator TimerBeforeNewWanderingPoint()
    {
        canFindNewWanderingPoint = false;
        yield return new WaitForSeconds(1);
        canFindNewWanderingPoint = true;
    }

    public void SetMovementValues(float fishSpeed, float fishRotationSpeed)
    {
        forceApplied = fishSpeed;
        speedOfRotation = fishRotationSpeed;
    }

    public void IncreaseFishSpeed(float increase)
    {
        forceApplied += increase;
    }

    #endregion

    private void OnDrawGizmos()
    {
        if (!debugging|| !Application.isPlaying)
        {
            return;
        }

        Gizmos.color = Color.blue;
        foreach (var raycastPoint in raycastPoints)
        {
            Gizmos.DrawSphere(raycastPoint, 0.01f);
        }
        
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(target, 0.05f);
        
    }
}