using UnityEngine;


[RequireComponent(typeof(Collider), typeof(Rigidbody), typeof(FishMovementBehaviour))]
public class Fish : MonoBehaviour, IEatable
{


    #region Invinsible Variables

    #endregion

    #region Visible Variables

    [SerializeField] private GameObject fishBlood;
    [SerializeField] private bool applyRandomGenes;

    #endregion

    private FishMovementBehaviour _fishMovementBehaviour;
    private FishFoodBehaviour _foodBehaviour;
    private FishEvolution _fishEvolution;

   

    #region Properties
    public float CurrentSize => transform.localScale.x;

    #endregion


    private void Awake()
    {
        _fishMovementBehaviour = GetComponent<FishMovementBehaviour>();
        _foodBehaviour = GetComponent<FishFoodBehaviour>();
        _fishEvolution = GetComponent<FishEvolution>();
    }

    void Start()
    {
        //check if the behaviour exist and call them if they do
        if (_foodBehaviour.enabled)
            StartCoroutine(_foodBehaviour.CheckIfFoodIsNearBy());

        if (_fishEvolution.enabled && applyRandomGenes)
            _fishEvolution.SetMyRandomGenes();


        StartCoroutine(_fishMovementBehaviour.SetRandomWanderingPoint());
    }


    private void Update()
    {
        _fishMovementBehaviour.UpdateTargetAndFindValidPath();
    }


    #region Eating Behaviour

    //fish being eaten
    public void Eaten()
    {
        var tempParticle = Instantiate(fishBlood, transform.position, Quaternion.identity);
        Destroy(tempParticle, 5);
        Destroy(gameObject);
    }

    //update target if food has been found
    public void FoundFood(Transform foodTransform)
    {
        _fishMovementBehaviour.GoTowardsNewTarget(foodTransform);
    }

    #endregion

    //set fish size and movement behaviour
    public void SetFishMovementValues(float fishSize, float fishSpeed, float fishRotationSpeed,
        float foodDetectionRadius)
    {
        _fishMovementBehaviour.SetMovementValues(fishSpeed, fishRotationSpeed);

        _foodBehaviour.UpdateFishFoodDetectionRadius(foodDetectionRadius);

        SetFishScale(fishSize);
    }

    //scale up the fish
    public void SetFishScale(float fishSize)
    {
        fishSize = Mathf.Clamp(fishSize, 0.1f, 1);

        transform.localScale = Vector3.one * fishSize;

        _fishMovementBehaviour.IncreaseFishSpeed(0.1f);
    }
}