using UnityEngine;

[System.Serializable]
public class ChainSettings
{
    public int numberOfLinks = 10;
    public float chainWeight = 1.0f;
    public float sphereRadius = 0.5f;
    public float minSphereRadius = 0.3f;
    public float gapSize = 0.1f;
    public GameObject suspensionPoint1;
    public GameObject suspensionPoint2;
    public float jointSpring = 500.0f;
    public float jointDamper = 10.0f;
    public float maxAngularVelocity = 5.0f;
}

public class ChainGenerator : MonoBehaviour
{
    [Header("Chain Settings")]
    [SerializeField]
    private ChainSettings chainSettings;

    private int previousNumberOfLinks;
    private float previousChainWeight;
    private float previousSphereRadius;
    private float previousMinSphereRadius;
    private float previousGapSize;
    private GameObject previousSuspensionPoint1;
    private GameObject previousSuspensionPoint2;
    private float previousJointSpring;
    private float previousJointDamper;
    private float previousMaxAngularVelocity;

    private GameObject chainGameObject;
    private GameObject[] chainLinks;

    void Start()
    {
        GenerateChain(transform.position);
        UpdatePreviousValues();
    }

    private void Update()
    {
        if (Application.isPlaying && ParametersChanged())
        {
            DestroyChain();
            GenerateChain(transform.position);
            UpdatePreviousValues();
        }

        CheckAngularVelocity();
    }

    void GenerateChain(Vector3 startPosition)
    {
        chainGameObject = new GameObject("Chain");
        chainLinks = new GameObject[chainSettings.numberOfLinks];

        GameObject previousLink = null;

        for (int i = 0; i < chainSettings.numberOfLinks; i++)
        {
            GameObject link = new GameObject("ChainLink");
            link.transform.parent = chainGameObject.transform;

            link.transform.position = startPosition + Vector3.down * i * (2 * GetAdjustedSphereRadius() + chainSettings.gapSize);

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.parent = link.transform;
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = new Vector3(GetAdjustedSphereRadius() * 2, GetAdjustedSphereRadius() * 2, GetAdjustedSphereRadius() * 2);

            Rigidbody linkRigidbody = link.AddComponent<Rigidbody>();
            linkRigidbody.mass = chainSettings.chainWeight;

            if (previousLink != null)
            {
                HingeJoint joint = link.AddComponent<HingeJoint>();
                joint.connectedBody = previousLink.GetComponent<Rigidbody>();
                joint.axis = new Vector3(0, 1, 0);
                joint.anchor = new Vector3(0, -GetAdjustedSphereRadius(), 0);
                joint.connectedAnchor = new Vector3(0, GetAdjustedSphereRadius(), 0);

                JointSpring hingeSpring = new JointSpring
                {
                    spring = chainSettings.jointSpring,
                    damper = chainSettings.jointDamper
                };

                joint.spring = hingeSpring;
            }
            else
            {
                if (chainSettings.suspensionPoint1 != null)
                {
                    ConfigurableJoint joint = link.AddComponent<ConfigurableJoint>();
                    joint.connectedBody = chainSettings.suspensionPoint1.GetComponent<Rigidbody>();
                    joint.autoConfigureConnectedAnchor = false;
                    joint.connectedAnchor = Vector3.zero;
                    joint.xMotion = ConfigurableJointMotion.Locked;
                    joint.yMotion = ConfigurableJointMotion.Locked;
                    joint.zMotion = ConfigurableJointMotion.Locked;
                    joint.angularXMotion = ConfigurableJointMotion.Locked;
                    joint.angularYMotion = ConfigurableJointMotion.Locked;
                    joint.angularZMotion = ConfigurableJointMotion.Locked;
                    joint.linearLimit = new SoftJointLimit { limit = 0.001f };
                }
            }

            chainLinks[i] = link;
            previousLink = link;
        }

        if (previousLink != null && chainSettings.suspensionPoint2 != null)
        {
            ConfigurableJoint joint = previousLink.AddComponent<ConfigurableJoint>();
            joint.connectedBody = chainSettings.suspensionPoint2.GetComponent<Rigidbody>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = Vector3.zero;
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;
            joint.linearLimit = new SoftJointLimit { limit = 0.001f };
        }
    }

    void DestroyChain()
    {
        if (chainGameObject != null)
        {
            Destroy(chainGameObject);
        }
    }

    private void CheckAngularVelocity()
    {
        foreach (var link in chainLinks)
        {
            if (link != null)
            {
                Rigidbody linkRigidbody = link.GetComponent<Rigidbody>();
                if (linkRigidbody != null && linkRigidbody.angularVelocity.magnitude > chainSettings.maxAngularVelocity)
                {
                    HingeJoint hingeJoint = link.GetComponent<HingeJoint>();
                    if (hingeJoint != null)
                    {
                        JointSpring hingeSpring = hingeJoint.spring;
                        hingeSpring.damper *= 2.0f;
                        hingeJoint.spring = hingeSpring;
                    }
                }
            }
        }
    }

    private float GetAdjustedSphereRadius()
    {
        return Mathf.Max(chainSettings.sphereRadius, chainSettings.minSphereRadius);
    }

    private bool ParametersChanged()
    {
        return chainSettings.numberOfLinks != previousNumberOfLinks ||
               !Mathf.Approximately(chainSettings.chainWeight, previousChainWeight) ||
               !Mathf.Approximately(chainSettings.sphereRadius, previousSphereRadius) ||
               !Mathf.Approximately(chainSettings.minSphereRadius, previousMinSphereRadius) ||
               !Mathf.Approximately(chainSettings.gapSize, previousGapSize) ||
               chainSettings.suspensionPoint1 != previousSuspensionPoint1 ||
               chainSettings.suspensionPoint2 != previousSuspensionPoint2 ||
               !Mathf.Approximately(chainSettings.jointSpring, previousJointSpring) ||
               !Mathf.Approximately(chainSettings.jointDamper, previousJointDamper) ||
               !Mathf.Approximately(chainSettings.maxAngularVelocity, previousMaxAngularVelocity);
    }

    private void UpdatePreviousValues()
    {
        previousNumberOfLinks = chainSettings.numberOfLinks;
        previousChainWeight = chainSettings.chainWeight;
        previousSphereRadius = chainSettings.sphereRadius;
        previousMinSphereRadius = chainSettings.minSphereRadius;
        previousGapSize = chainSettings.gapSize;
        previousSuspensionPoint1 = chainSettings.suspensionPoint1;
        previousSuspensionPoint2 = chainSettings.suspensionPoint2;
        previousJointSpring = chainSettings.jointSpring;
        previousJointDamper = chainSettings.jointDamper;
        previousMaxAngularVelocity = chainSettings.maxAngularVelocity;
    }
}