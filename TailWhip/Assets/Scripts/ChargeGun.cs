using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ChargeGun : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player player;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;

    [Header("Base Shot")]
    [SerializeField] private float baseDamage = 15f;

    [Header("Charge Shot")]
    [SerializeField] private float maxChargeTime = 2f;
    [SerializeField] private float maxDamageMultiplier = 4f;
    [SerializeField] private float maxScaleMultiplier = 2.5f;
    private float chargeTimer = 0f;
    private bool isCharging = false;

    [Header("FX")]
    [SerializeField] private ParticleSystem chargeParticles;

    [Header("Reticle UI")]
    [SerializeField] private Image innerReticleImage;
    [SerializeField] private Image outerReticleImage;
    [SerializeField] private float notchAngle = 90f;
    [SerializeField] private float minChargeSpinSpeed = 90f;
    [SerializeField] private float maxChargeSpinSpeed = 720f;
    [SerializeField] private float notchSmoothTime = 0.08f;
    [SerializeField] private float shotPulse = 0.1f;
    [SerializeField] private float chargedShotPulse = 0.3f;
    [SerializeField] private float pulseRecoverSpeed = 1.5f;

    private float currAngle;
    private float targetAngle;
    private float angleVelocity;
    private float outerPulse;
    private Vector3 outerBaseScale = Vector3.one;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (player == null) player = GetComponent<Player>();
        if (player == null) player = GetComponentInParent<Player>();
        if (chargeParticles != null) chargeParticles.Stop();
        if (outerReticleImage != null) outerBaseScale = outerReticleImage.rectTransform.localScale;
        if (innerReticleImage != null)
        {
            currAngle = innerReticleImage.rectTransform.localEulerAngles.z;
            targetAngle = currAngle;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            chargeTimer = 0f;
            isCharging = true;
            if (chargeParticles != null)
            {
                chargeParticles.Clear();
                chargeParticles.Play();
            }
        }

        if (Input.GetMouseButton(0) && isCharging)
        {
            chargeTimer += Time.deltaTime;
            chargeTimer = Mathf.Min(chargeTimer, maxChargeTime);
            float chargeRatio = chargeTimer / maxChargeTime;

            if (chargeParticles != null)
            {
                chargeParticles.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.8f, chargeRatio);
            }

            float spinSpeed = Mathf.Lerp(minChargeSpinSpeed, maxChargeSpinSpeed, chargeRatio);
            currAngle -= spinSpeed * Time.deltaTime;
            targetAngle = currAngle;
            angleVelocity = 0f;
        }

        if (Input.GetMouseButtonUp(0) && isCharging)
        {
            FireChargeShot();
            isCharging = false;
            if (chargeParticles != null)
            {
                chargeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        UpdateReticleVisuals();
    }

    private void UpdateReticleVisuals()
    {
        if (innerReticleImage != null)
        {
            if (!isCharging)
            {
                currAngle = Mathf.SmoothDamp(currAngle, targetAngle, ref angleVelocity, notchSmoothTime);
            }
            innerReticleImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, currAngle);
        }

        if (outerReticleImage != null)
        {
            outerPulse = Mathf.MoveTowards(outerPulse, 0f, pulseRecoverSpeed * Time.deltaTime);
            outerReticleImage.rectTransform.localScale = outerBaseScale * (1f + outerPulse);
        }
    }

    private void TriggerShotReticleFX(float chargeRatio)
    {
        float from = Mathf.Min(targetAngle, currAngle);
        targetAngle = Mathf.Floor((from - 0.01f) / notchAngle) * notchAngle;
        outerPulse = Mathf.Lerp(shotPulse, chargedShotPulse, chargeRatio);
    }

    private void FireChargeShot()
    {
        if (bulletPrefab == null || firePoint == null || player == null) return;
    
        float chargeRatio = Mathf.Clamp01(chargeTimer / maxChargeTime);

        float finalDamage = baseDamage * Mathf.Lerp(1f, maxDamageMultiplier, chargeRatio);
        float finalScale = Mathf.Lerp(1f, maxScaleMultiplier, chargeRatio);
        float speedMultiplier = Mathf.Lerp(1f, 1.5f, chargeRatio);

        Vector3 targetPoint = player.GetAimWorldPoint();
        Vector3 direction = (targetPoint - firePoint.position).normalized;

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(direction));
        
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.Initialize(finalDamage, finalScale, speedMultiplier, player.transform);
        }

        player.PlayShootAnimation();
        TriggerShotReticleFX(chargeRatio);
    }
}
