﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq.Expressions;
using System;

[System.Serializable]
public class GunKeyBinding
{
    public Transform transform;
    public KeyCode keyCode;

    [System.NonSerialized]
    public float originalZ;

    [System.NonSerialized]
    public float tVal;
}

public enum GunState
{
    Down,
    Raising,
    Up,
    Lowering
}

public class GunController : MonoBehaviour {

    public List<char> Ammo = new List<char>("PUNCTUATOR".ToCharArray());
    public ProjectileController ProjectileControllerPrefab = null;
    public Transform muzzleTranform = null;
    public Animator gunControllerAnimator = null;
    public Animator gunAnimator = null;
    public GunAudioController gunAudioController = null;

    [Header("Keys")]
    public List<GunKeyBinding> gunKeys = new List<GunKeyBinding>();
    public float KeyAnimMoveZ = 0.012f;
    public float KeyDownAnimDuration = 0.1f;
    public float KeyUpAnimDuration = 0.2f;
    public float DeleteRepeatDelay = 0.5f;
    public float DeleteRepeatRate = 0.3f;

    [Header("AmmoDisplay")]
    public List<TextMeshPro> ammoDisplayPanels = new List<TextMeshPro>();
    public char emptyAmmoChar = ' ';

    RectTransform m_crosshair = null;
    bool m_isReloading;
    GunState m_gunState = GunState.Down;
    float m_timeToNextDelete = 0;
    Health m_targetUnderReticule = null;

    public bool GetIsReloading() { return m_isReloading; }
    public bool GetIsGunUp() { return m_gunState == GunState.Up; }
    public int GetMaxAmmoCount() { return ammoDisplayPanels.Count; }

    private void OnEnable()
    {
        GenerateGunKeyBindings();
        FindAmmoText();
        UpdateAmmoDisplay();

        if (gunControllerAnimator == null)
        {
            gunControllerAnimator = GetComponent<Animator>();
        }

        SetGunUp(true, true);
        SetReloadState(false, true);
    }

    public void SetGunUp(bool gunUp, bool force = false) 
    {
        if(gunUp && m_gunState != GunState.Up)
        {
            gunControllerAnimator.SetBool("IsGunUp", true);
            m_gunState = GunState.Raising;
            if(gunAudioController && !force) gunAudioController.OnGunUp();
        }
        else if(!gunUp && m_gunState != GunState.Down)
        {
            gunControllerAnimator.SetBool("IsGunUp", false);
            m_gunState = GunState.Lowering;
            if (gunAudioController && !force) gunAudioController.OnGunDown();
        }
    }

    public void OnGunUp()
    {
        m_gunState = GunState.Up;
        FPSPlayerController player = GameManager.Instance.Player;
        if (player && player.m_fpsHUD)
        {
            player.m_fpsHUD.SetCrosshairVisible(true);
        }
    }

    public void OnGunDown()
    {
        m_gunState = GunState.Down;
        FPSPlayerController player = GameManager.Instance.Player;
        if (player && player.m_fpsHUD)
        {
            player.m_fpsHUD.SetCrosshairVisible(false);
        }
    }

    public void TryFire()
    {
        if(CanFire())
        {
            if (Ammo.Count > 0)
            {
                OnFire();
            }
            else
            {
                OnDryFire();
            }
        }
    }

    bool CanFire()
    {
        return !m_isReloading && GetIsGunUp();
    }

    bool CanReload()
    {
        return !m_isReloading && GetIsGunUp();
    }

    void OnFire()
    {
        if(Ammo.Count > 0)
        {
            char letterToFire = Ammo[0];
            Ammo.RemoveAt(0);
            UpdateAmmoDisplay();
            SpawnProjectile(letterToFire);

            if (gunAnimator)
            {
                if (Ammo.Count == 0)
                {
                    gunAnimator.SetTrigger("OnFireLast");
                }
                else
                {
                    gunAnimator.SetTrigger("OnFire");
                }
            }

            if (gunAudioController)
            {
                gunAudioController.OnFire();
            }
        }
    }

    void OnDryFire()
    {
        if (gunAudioController)
        {
            gunAudioController.OnDryFire();
        }
        FPSPlayerController player = GameManager.Instance.Player;
        if (player && player.m_fpsHUD)
        {
            player.m_fpsHUD.OnOutOfAmmo();
        }
    }

    ProjectileController SpawnProjectile(char letter)
    {
        ProjectileController projectile = null;
        if (ProjectileControllerPrefab && muzzleTranform)
        {
            GameObject gobj = Instantiate<GameObject>(ProjectileControllerPrefab.gameObject);
            projectile = gobj.GetComponent<ProjectileController>();
            projectile.letter = letter;

            Vector3 traceOrigin = muzzleTranform.position;
            Vector3 targetPosition = muzzleTranform.forward * projectile.maxDistance;

            FPSPlayerController player = GameManager.Instance.Player;
            if (player && player.m_fpsHUD.m_crosshair)
            {
                Ray ray = Camera.main.ScreenPointToRay(player.m_fpsHUD.m_crosshair.position);
                traceOrigin = ray.origin;
                RaycastHit hitInfo;
                if (Physics.Raycast(ray, out hitInfo, projectile.maxDistance, projectile.hitMask | projectile.damageMask))
                {
                    targetPosition = hitInfo.point;
                    //hit is closer than the muzzle then count as an immediate hit
                    if (hitInfo.distance < Vector3.Distance(muzzleTranform.position, traceOrigin) + 0.5f)
                    {
                        projectile.OnHit(hitInfo);
                        Debug.DrawLine(muzzleTranform.position, hitInfo.point, Color.red, 1.0f);
                    }
                    else
                    {
                        Debug.DrawLine(muzzleTranform.position, targetPosition, Color.green, 2.0f);
                    }
                }
            }

            Debug.DrawLine(traceOrigin, targetPosition, Color.grey, 2.0f);
            projectile.OnSpawn(muzzleTranform.position, Camera.main.transform.position, targetPosition);
        }
        return projectile;
    }

    private void LateUpdate()
    {
        this.transform.rotation = Camera.main.transform.rotation;

        if (Input.GetMouseButtonDown(0))
        {
            TryFire();
        }

        //Update keys before reload input to make sure we don't capture "r" on the same frame
        UpdateGunKeys();

        if (m_isReloading)
        {
            if(Input.GetKeyDown(KeyCode.Return))
            {
                SetReloadState(false);
            }
            else if (Input.GetKey(KeyCode.Backspace) || Input.GetKey(KeyCode.Delete))
            {
                bool pressedThisFrame = Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete);
                if(pressedThisFrame)
                {
                    RemoveAmmo();
                    m_timeToNextDelete = DeleteRepeatDelay;
                }
                else
                {
                    m_timeToNextDelete -= Time.deltaTime;
                    if(m_timeToNextDelete <= 0)
                    {
                        RemoveAmmo();
                        m_timeToNextDelete = DeleteRepeatRate;
                    }
                }
            }
        }
        else //not reloading
        {
            if(Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return))
            {
                if (CanReload())
                {
                    SetReloadState(true);
                }
            }
            /*
            else if (Input.GetKeyDown(KeyCode.E))
            {
                bool IsUpOrRaising = m_gunState == GunState.Up || m_gunState == GunState.Raising;
                SetGunUp(!IsUpOrRaising);
            }
            */
        }

        if (Camera.main && ProjectileControllerPrefab)
        {
            FPSPlayerController player = GameManager.Instance.Player;
            if (player && player.m_fpsHUD.m_crosshair)
            {
                Ray ray = Camera.main.ScreenPointToRay(player.m_fpsHUD.m_crosshair.position);
                RaycastHit damageHitInfo;
                Health health = null;
                if (Physics.SphereCast(ray, ProjectileControllerPrefab.damageRadius, out damageHitInfo, ProjectileControllerPrefab.maxDistance, ProjectileControllerPrefab.damageMask))
                {
                    //make sure no geometry is in the way
                    float distanceToTarget = Vector3.Distance(ray.origin, damageHitInfo.transform.position);
                    if(!Physics.Raycast(ray, distanceToTarget, ProjectileControllerPrefab.hitMask))
                    {
                        health = damageHitInfo.collider.GetComponentInParent<Health>();
                    }
                }
                if(health != m_targetUnderReticule)
                {
                    if (m_targetUnderReticule) m_targetUnderReticule.SetIsTextVisible(false);
                    m_targetUnderReticule = health;
                }
                if(m_targetUnderReticule && !m_targetUnderReticule.m_ignoreRaycat)
                {
                    m_targetUnderReticule.SetIsTextVisible(true);
                }
            }
        }
    }

    void SetReloadState(bool isReloading, bool force = false)
    {
        if (force || isReloading != m_isReloading)
        {
            m_isReloading = isReloading;
            if (gunAnimator)
            {
                gunAnimator.SetBool("IsReloading", isReloading);
            }
            if (gunControllerAnimator)
            {
                gunControllerAnimator.SetBool("IsReloading", isReloading);
            }
            if (gunAudioController && !force)
            {
                if (isReloading)
                {
                    gunAudioController.OnReloadStart();
                }
                else
                {
                    gunAudioController.OnReloadEnd();
                }
            }
            FPSPlayerController player = GameManager.Instance.Player;
            if(player && player.m_fpsHUD && !force)
            {
                if (isReloading)
                {
                    player.m_fpsHUD.OnReloadStarted();
                }
            }
        }
    }

    void GenerateGunKeyBindings()
    {
        gunKeys.Clear();

        List<Transform> children = new List<Transform>(gameObject.GetComponentsInChildren<Transform>());
        children.RemoveAll(t => !t.name.StartsWith("Key_"));

        KeyCode alphaStart = KeyCode.A;
        for (KeyCode alphaKey = alphaStart; alphaKey < alphaStart + 26; alphaKey++)
        {
            Transform keyTransform = children.Find(t => t.name.StartsWith("Key_" + alphaKey));
            if (keyTransform)
            {
                GunKeyBinding newBinding = new GunKeyBinding();
                newBinding.keyCode = alphaKey;
                newBinding.transform = keyTransform;
                newBinding.originalZ = keyTransform.localPosition.z;
                gunKeys.Add(newBinding);
                Debug.Log("Found key: Key_" + alphaKey);
            }
        }
        UpdateGunKeys(true);
    }

    void UpdateGunKeys(bool forceUpdate = false)
    {
        if(!m_isReloading && !forceUpdate)
        {
            return;
        }
        foreach(GunKeyBinding binding in gunKeys)
        {
            if (Ammo.Count >= GetMaxAmmoCount())
                break;

            if(m_isReloading && Input.GetKeyDown(binding.keyCode))
            {
                AddAmmo(binding.keyCode);
            }
            bool isKeyHeld = Input.GetKey(binding.keyCode);
            if(forceUpdate || (isKeyHeld && binding.tVal < 1))
            {
                if (KeyDownAnimDuration > 0)
                {
                    binding.tVal = Mathf.Clamp01(binding.tVal + Time.deltaTime / KeyDownAnimDuration);
                }
                else binding.tVal = 1;
                UpdateKeyBindingTransform(binding);
            }
            else if (forceUpdate || (!isKeyHeld && binding.tVal > 0))
            {
                if (KeyDownAnimDuration > 0)
                {
                    binding.tVal = Mathf.Clamp01(binding.tVal - Time.deltaTime / KeyUpAnimDuration);
                }
                else binding.tVal = 0;
                UpdateKeyBindingTransform(binding);
            }
        }
    }

    void UpdateKeyBindingTransform(GunKeyBinding binding)
    {
        if(binding.transform)
        {
            Vector3 pos = binding.transform.localPosition;
            pos.z = binding.originalZ + KeyAnimMoveZ * binding.tVal;
            binding.transform.localPosition = pos;
        }
    }

    bool AddAmmo(KeyCode keyCode)
    {
        if (Ammo.Count < GetMaxAmmoCount())
        {
            char upperChar = keyCode.ToString().ToUpper()[0];
            Ammo.Add(upperChar);
            OnAmmoChanged();
            return true;
        }
        return false;
    }

    void RemoveAmmo()
    {
        if(Ammo.Count > 0)
        {
            Ammo.RemoveAt(Ammo.Count - 1);
            OnAmmoChanged();
        }
    }

    void RemoveAllAmmo()
    {
        if (Ammo.Count > 0)
        {
            Ammo.Clear();
            OnAmmoChanged();
        }
    }

    void OnAmmoChanged()
    {
        UpdateAmmoDisplay();
        if (gunAudioController)
        {
            gunAudioController.OnReloadKeyPress();
        }
    }

    void FindAmmoText()
    {
        string prefix = "AmmoText_";
        List<Transform> AmmoTextTransforms = new List<Transform>();
        foreach(Transform child in gameObject.GetComponentsInChildren<Transform>())
        {
            string name = child.name;
            if(name.StartsWith(prefix))
            {
                AmmoTextTransforms.Add(child);
            }
        }
        AmmoTextTransforms.Sort((t1, t2) => int.Parse(t1.name.Remove(0, prefix.Length)) < int.Parse(t2.name.Remove(0, prefix.Length)) ? -1 : 1);

        ammoDisplayPanels.Clear();
        foreach (Transform t in AmmoTextTransforms)
        {
            TextMeshPro textMeshPro = t.GetComponent<TextMeshPro>();
            Debug.Assert(textMeshPro != null);
            if(textMeshPro)
            {
                ammoDisplayPanels.Add(textMeshPro);
            }
        }
    }

    void UpdateAmmoDisplay()
    {
        for(int i = 0; i < ammoDisplayPanels.Count; i++)
        {
            if(i < Ammo.Count)
            {
                ammoDisplayPanels[i].SetText(Ammo[i].ToString().ToUpper());
            }
            else
            {
                ammoDisplayPanels[i].SetText(emptyAmmoChar.ToString().ToUpper());
            }
        }
    }
}
