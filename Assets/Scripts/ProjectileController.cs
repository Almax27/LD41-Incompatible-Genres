﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class ProjectileController : MonoBehaviour {

    public float speed = 100.0f;
    public float maxDistance = 100.0f;
    public float damageRadius = 1.0f;
    public char letter = ' ';
    public LayerMask hitMask = new LayerMask();
    public LayerMask damageMask = new LayerMask();
    public GameObject instigator = null;
    public Action<bool> hitCallback = null;

    public TextMeshPro text = null;

    public List<FollowEffect> effectPrefabs = new List<FollowEffect>();
    public Spawner onHitSpawner = new Spawner();
    public Spawner onDamagedSpawner = new Spawner();

    float m_distanceTraveled = 0.0f;
    List<FollowEffect> m_activeEffects = new List<FollowEffect>();
    Vector3 m_direction = Vector3.zero;
    Vector3 m_castPosition = Vector3.zero;
    Vector3 m_castDirection = Vector3.zero;

    private void Start()
    {
        if (text)
        {
            text.text = letter.ToString();
        }
    }

    // Use this for initialization
    void OnEnable () {
		if(text)
        {
            text.text = letter.ToString();
        }
        foreach(FollowEffect prefab in effectPrefabs)
        {
            if (prefab)
            {
                GameObject gobj = Instantiate<GameObject>(prefab.gameObject);
                if (gobj)
                {
                    FollowEffect effect = gobj.GetComponent<FollowEffect>();
                    effect.target = this.transform;
                    m_activeEffects.Add(effect);
                }
            }
        }
	}

    private void OnDisable()
    {
        foreach(FollowEffect effect in m_activeEffects)
        {
            if(effect)
            {
                effect.Cleanup();
            }
        }
        m_activeEffects.Clear();
    }

    // Update is called once per frame
    void Update ()
    {
        float distanceToMove = speed * Time.deltaTime;
        transform.position += m_direction * distanceToMove;        
    }

    private void FixedUpdate()
    {
        float distanceToMove = speed * Time.fixedDeltaTime;
        m_distanceTraveled += distanceToMove;

        if (m_distanceTraveled > maxDistance)
        {
            OnExpired();
        }

        Vector3 preMoveCastPosition = m_castPosition;
        m_castPosition += m_castDirection * distanceToMove;

        //do cast
        RaycastHit hitInfo;
        bool validHit = Physics.Raycast(preMoveCastPosition, m_castDirection, out hitInfo, distanceToMove, hitMask);
        RaycastHit damageHitInfo;
        if (Physics.SphereCast(preMoveCastPosition, damageRadius, m_castDirection, out damageHitInfo, distanceToMove, damageMask))
        {
            if (!validHit || damageHitInfo.distance < hitInfo.distance + damageRadius)
            {
                hitInfo = damageHitInfo;
            }
        }
        if (hitInfo.collider)
        {
            OnHit(hitInfo);
        }
    }

    public void OnSpawn(Vector3 muzzlePosition, Vector3 castPosition, Vector3 targetPosition)
    {
        transform.position = muzzlePosition;
        m_direction = (targetPosition - muzzlePosition).normalized;
        m_castPosition = castPosition;
        m_castDirection = (targetPosition - castPosition).normalized;
    }

    public void OnHit(RaycastHit hitInfo)
    {
        Debug.Assert(hitInfo.collider);
        Debug.Log("Hit: " + hitInfo.collider.gameObject);

        transform.position = hitInfo.point;

        bool dealtDamage = false;
        Health health = hitInfo.collider.GetComponentInParent<Health>();
        if (health)
        {
            DamagePacket packet = new DamagePacket();
            packet.instigator = instigator;
            packet.letter = letter;
            if (health.TakeDamage(packet))
            {
                onDamagedSpawner.ProcessSpawns(transform, hitInfo.point, Quaternion.LookRotation(hitInfo.normal), Vector3.one);
                dealtDamage = true;
                DebugExtension.DebugWireSphere(hitInfo.point, Color.red, damageRadius, 1.0f);
            }
        }
        if (!dealtDamage)
        {
            onHitSpawner.ProcessSpawns(transform, hitInfo.point, Quaternion.LookRotation(hitInfo.normal), Vector3.one);
            DebugExtension.DebugWireSphere(hitInfo.point, Color.grey, damageRadius, 1.0f);
        }
        Destroy(gameObject);
        
    }

    void OnExpired()
    {
        Destroy(gameObject);
    }
}
