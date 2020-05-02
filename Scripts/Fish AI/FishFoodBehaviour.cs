using System.Collections;
using UnityEngine;

public class FishFoodBehaviour : MonoBehaviour
{
    #region Variables

    #region  Invisilbe

    private Fish _fish;

    #endregion

    #region Visible

    [SerializeField, Tooltip("Check this layer for food")]
    private LayerMask foodLayer;

    [SerializeField, Tooltip("How far a fish can detect food form")]
    private float foodDetectionRadius = 5;

    [SerializeField, Tooltip("the size of the food the fish can eat")]
    private float FoodSizeAbleToEatRelativeToOwnSize = 2;

    #endregion Visible

    #endregion Variables


    private void Awake()
    {
        _fish = GetComponent<Fish>();
    }

    //look for food in the fish radius
    public IEnumerator CheckIfFoodIsNearBy()
    {
        WaitForSeconds wait = new WaitForSeconds(1);

        while (true)
        {
            float closetFoodDistance = 999;
            Collider closetFood = null;

            foreach (var collider in Physics.OverlapSphere(transform.position, foodDetectionRadius))
            {
                var iEatable = collider.GetComponent<IEatable>();

                if (iEatable == null) continue;

                if (iEatable.CurrentSize * FoodSizeAbleToEatRelativeToOwnSize >
                    _fish.CurrentSize)
                    continue;

                float distance = Vector3.Distance(collider.transform.position, transform.position);

                if (distance < closetFoodDistance)
                {
                    closetFoodDistance = distance;
                    closetFood = collider;
                }
            }

            if (closetFood != null)
            {
                _fish.FoundFood(closetFood.transform);
            }

            yield return wait;
        }
    }

    //check if able to eat the object colliding with
    private void OnCollisionEnter(Collision other)
    {
        var eatableTemp = other.gameObject.GetComponent<IEatable>();

        if (eatableTemp == null)
            return;

        if (eatableTemp.CurrentSize * FoodSizeAbleToEatRelativeToOwnSize > _fish.CurrentSize)
            return;

        AteFood(eatableTemp.CurrentSize);
        eatableTemp.Eaten();
    }

    //scale the size of fish depending on what the fish ate
    private void AteFood(float foodSize)
    {
        _fish.SetFishScale(_fish.CurrentSize * (1 + foodSize / 1));
    }

    public void UpdateFishFoodDetectionRadius(float newRadius)
    {
        foodDetectionRadius = newRadius;
    }
}