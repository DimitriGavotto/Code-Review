using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class FishEvolution : MonoBehaviour
{
    #region Variables

    #region Visible

    [SerializeField] private float minFishSize = 0.2f;
    [SerializeField] private float maxFishSize = 0.4f;

    #endregion

    #region Invisilbe

    private Fish _fish;

    #endregion

    #endregion


    private void Awake()
    {
        _fish = GetComponent<Fish>();
    }

    public void SetMyRandomGenes()
    {
        float fishSize = Random.Range(minFishSize, maxFishSize);
        float percentage = (fishSize / maxFishSize) + 0.5f;

        float fishSpeed = Random.Range(0.1f * percentage, 1.5f * percentage);
        float fishRotationSpeed = Random.Range(2f * percentage, 5f * percentage);
        float foodDetectionRadius = Random.Range(0.5f * percentage, 2f * percentage);

//        print($"Size:{fishSize},Speed:{fishSpeed}");
//        print($"rotation:{fishRotationSpeed},Speed:{foodDetectionRadius}");
        _fish.SetFishMovementValues(fishSize, fishSpeed, fishRotationSpeed, foodDetectionRadius);
    }
}