using UnityEngine;
using System.Collections;

public class SD_LaserWeapon : MonoBehaviour
{
    [Header("Referencias")]
    public SD_Player player;
    public Camera aimCamera;
    public Transform gun;
    public SpriteRenderer gunSprite;
    public Transform firePoint;
    public SD_LaserBullet bulletPrefab;
    public SD_TriviaController triviaController;
    public SD_PauseController pauseController;

    [Header("Posición")]
    public Vector2 pivotOffset = new Vector2(0f, -0.2f);
    public float orbitRadius = 0.7f;

    [Header("Disparo")]
    public float bulletSpeed = 15f;
    public float shotInterval = 0.2f;
    public int startingAmmo = 20;

    public int CurrentAmmo { get; private set; }

    private float nextShotTime;
    private Vector3 firePointOriginalPosition;

    [Header("Recarga")]
    public float reloadTime = 1.5f;

    public bool IsReloading { get; private set; }

    void Start()
    {
        if (aimCamera == null)
            aimCamera = Camera.main;

        CurrentAmmo = startingAmmo;
        firePointOriginalPosition = firePoint.localPosition;
    }

    void LateUpdate()
    {
        if (Time.timeScale == 0f ||
            (triviaController != null &&
            triviaController.BlocksGameplayInput) ||
            (pauseController != null &&
            pauseController.BlocksGameplayInput))
        {
            return;
        }
        
        if (player == null || aimCamera == null)
            return;

        // Seguir al jugador sin heredar su escala ni su giro.
        transform.position =
            player.transform.position + (Vector3)pivotOffset;

        Vector3 mouseScreen = Input.mousePosition;

        // Distancia desde la cámara hasta el plano del arma.
        mouseScreen.z =
            transform.position.z - aimCamera.transform.position.z;

        Vector3 mouseWorld = aimCamera.ScreenToWorldPoint(mouseScreen);
        mouseWorld.z = transform.position.z;

        Vector2 direction = mouseWorld - transform.position;

        // Evitar un giro indefinido si el cursor está en el pivote.
        if (direction.sqrMagnitude < 0.0001f)
            return;

        float angle =
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Mantener el arma a una distancia fija del pivote.
        gun.position =
            transform.position
            + transform.right * orbitRadius;

        // Mantener el dibujo derecho al apuntar hacia la izquierda.
        bool aimingLeft = direction.x < 0f;
        gunSprite.flipY = aimingLeft;

        // Reflejar también la boca del cañón si tiene desplazamiento Y.
        Vector3 muzzlePosition = firePointOriginalPosition;
        muzzlePosition.y =
            aimingLeft
                ? -firePointOriginalPosition.y
                : firePointOriginalPosition.y;

        firePoint.localPosition = muzzlePosition;

        if (!player.alive)
            return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            TryReload();
        }

        if (Input.GetMouseButtonDown(0))
        {
            Shoot(mouseWorld);
        }    
    }

    void Shoot(Vector3 target)
    {
        if (IsReloading || CurrentAmmo <= 0 || Time.time < nextShotTime)
            return;
        // Apuntar desde la boca del cañón hasta el cursor.
        Vector2 shotDirection = target - firePoint.position;

        if (shotDirection.sqrMagnitude < 0.0001f)
            return;

        shotDirection.Normalize();

        float bulletAngle =
            Mathf.Atan2(shotDirection.y, shotDirection.x)
            * Mathf.Rad2Deg;

        SD_LaserBullet bullet = Instantiate(
            bulletPrefab,
            firePoint.position,
            Quaternion.Euler(0f, 0f, bulletAngle)
        );

        bullet.Launch(shotDirection, bulletSpeed);

        CurrentAmmo--;
        nextShotTime = Time.time + shotInterval;
    }

    public void TryReload()
    {
        // Evitar recargas simultáneas o recargar estando muerto.
        if (IsReloading || player == null || !player.alive)
            return;

        // No recargar si el cargador ya está lleno.
        if (CurrentAmmo >= startingAmmo)
            return;

        StartCoroutine(Reload());
    }

    private IEnumerator Reload()
    {
        IsReloading = true;

        Debug.Log("Recargando...");

        yield return new WaitForSeconds(reloadTime);

        // Cancelar si el jugador murió durante la espera.
        if (player == null || !player.alive)
        {
            IsReloading = false;
            yield break;
        }

        CurrentAmmo = startingAmmo;
        IsReloading = false;

        Debug.Log("Recarga completa. Balas: " + CurrentAmmo);
    }
}