using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cinemachine;
using UnityEngine.Rendering.PostProcessing;

public class PlayerMovement : MonoBehaviour
{
    private Transform playerModel;
    public CinemachinePathBase dollyTrack; // Assign your dolly track in the Inspector
    public float maxDistanceFromTrack = 20f; // Maximum allowed distance from the dolly track

    [Header("Settings")]
    public bool joystick = true;

    [Space]

    [Header("Parameters")]
    public float xySpeed = 18;
    public float lookSpeed = 340;
    public float forwardSpeed = 6;

    [Space]

    [Header("Public References")]
    public Transform aimTarget;
    public CinemachineDollyCart dolly;
    public Transform cameraParent;

    [Space]

    [Header("Particles")]
    public ParticleSystem trail;
    public ParticleSystem circle;
    public ParticleSystem barrel;
    public ParticleSystem stars;


    public float currentX = 0;
    public float currentY = 0;

    void Start()
    {
        playerModel = transform.GetChild(0);
        SetSpeed(forwardSpeed);
    }

    void Update()
    {
        float h = joystick ? Input.GetAxis("Horizontal") : Input.GetAxis("Mouse X");
        float v = joystick ? Input.GetAxis("Vertical") : Input.GetAxis("Mouse Y");

        LocalMove(h, v, xySpeed);
        RotationLook(h,v, lookSpeed);
        HorizontalLean(playerModel, h, 80, .1f);

        if (Input.GetButtonDown("Action"))
            Boost(true);

        if (Input.GetButtonUp("Action"))
            Boost(false);

        if (Input.GetButtonDown("Fire3"))
            Break(true);

        if (Input.GetButtonUp("Fire3"))
            Break(false);

        //if (Input.GetButtonDown("TriggerL") || Input.GetButtonDown("TriggerR"))
        //{
        //    int dir = Input.GetButtonDown("TriggerL") ? -1 : 1;
        //    QuickSpin(dir);
        //}


    }

    void LocalMove(float x, float y, float speed)
    {
        transform.localPosition += new Vector3(x, y, 0) * speed * Time.deltaTime;

        // Clamp the player's position relative to the dolly track
        ClampPosition();
    }

    void ClampPosition()
    {
        if (dollyTrack == null)
        {
            Debug.LogError("Dolly track is not assigned!");
            return;
        }

        // Find the closest point on the dolly track
        Vector3 closestPointOnTrack = GetClosestPointOnTrack();

        // Calculate the offset between the player and the closest point on the track
        Vector3 offset = transform.position - closestPointOnTrack;

        // If the distance exceeds the maximum allowed, clamp the position
        if (offset.magnitude > maxDistanceFromTrack)
        {
            // Normalize the offset and scale it to the maximum allowed distance
            offset = offset.normalized * maxDistanceFromTrack;

            // Set the player's position back within the allowed range
            transform.position = closestPointOnTrack + offset;
        }
    }

    Vector3 GetClosestPointOnTrack()
    {
        // Initialize variables
        float closestDistance = float.MaxValue;
        Vector3 closestPoint = Vector3.zero;

        // Loop through the track's position samples to find the closest point
        for (float t = 0; t <= dollyTrack.PathLength; t += 0.1f)
        {
            Vector3 point = dollyTrack.EvaluatePositionAtUnit(t, CinemachinePathBase.PositionUnits.Distance);
            float distance = Vector3.Distance(transform.position, point);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPoint = point;
            }
        }

        return closestPoint;
    }


    void RotationLook(float h, float v, float speed)
        {
            aimTarget.parent.position = Vector3.zero;
            aimTarget.localPosition = new Vector3(h, v, 1);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(aimTarget.position), Mathf.Deg2Rad * speed * Time.deltaTime);
        }

    void HorizontalLean(Transform target, float axis, float leanLimit, float lerpTime)
    {
        Vector3 targetEulerAngels = target.localEulerAngles;
        target.localEulerAngles = new Vector3(targetEulerAngels.x, targetEulerAngels.y, Mathf.LerpAngle(targetEulerAngels.z, -axis * leanLimit, lerpTime));
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(aimTarget.position, .5f);
        Gizmos.DrawSphere(aimTarget.position, .15f);
    }

    //public void QuickSpin(int dir)
    //{
    //    if (!DOTween.IsTweening(playerModel))
    //    {
    //        playerModel.DOLocalRotate(new Vector3(playerModel.localEulerAngles.x, playerModel.localEulerAngles.y, 360 * -dir), .4f, RotateMode.LocalAxisAdd).SetEase(Ease.OutSine);
    //        barrel.Play();
    //    }
    //}

    void SetSpeed(float x)
    {
        dolly.m_Speed = x;
    }

    void SetCameraZoom(float zoom, float duration)
    {
        cameraParent.DOLocalMove(new Vector3(0, 0, zoom), duration);
    }

    void DistortionAmount(float x)
    {
        Camera.main.GetComponent<PostProcessVolume>().profile.GetSetting<LensDistortion>().intensity.value = x;
    }

    void FieldOfView(float fov)
    {
        cameraParent.GetComponentInChildren<CinemachineVirtualCamera>().m_Lens.FieldOfView = fov;
    }

    void Chromatic(float x)
    {
        Camera.main.GetComponent<PostProcessVolume>().profile.GetSetting<ChromaticAberration>().intensity.value = x;
    }


    void Boost(bool state)
    {

        if (state)
        {
            cameraParent.GetComponentInChildren<CinemachineImpulseSource>().GenerateImpulse();
            trail.Play();
            circle.Play();
        }
        else
        {
            trail.Stop();
            circle.Stop();
        }
        trail.GetComponent<TrailRenderer>().emitting = state;

        float origFov = state ? 40 : 55;
        float endFov = state ? 55 : 40;
        float origChrom = state ? 0 : 1;
        float endChrom = state ? 1 : 0;
        float origDistortion = state ? 0 : -30;
        float endDistorton = state ? -30 : 0;
        float starsVel = state ? -20 : -1;
        float speed = state ? forwardSpeed * 2 : forwardSpeed;
        float zoom = state ? -7 : 0;

        DOVirtual.Float(origChrom, endChrom, .5f, Chromatic);
        DOVirtual.Float(origFov, endFov, .5f, FieldOfView);
        DOVirtual.Float(origDistortion, endDistorton, .5f, DistortionAmount);
        var pvel = stars.velocityOverLifetime;
        pvel.z = starsVel;

        DOVirtual.Float(dolly.m_Speed, speed, .15f, SetSpeed);
        SetCameraZoom(zoom, .4f);
    }

    void Break(bool state)
    {

        float speed = state ? forwardSpeed / 3 : forwardSpeed;
        float zoom = state ? 3 : 0;


        DOVirtual.Float(dolly.m_Speed, speed, .15f, SetSpeed);
        SetCameraZoom(zoom, .4f);
    }
}
