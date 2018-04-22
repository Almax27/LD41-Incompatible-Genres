﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GunAudio
{
    public List<AudioClip> clips = new List<AudioClip>();
    public float pitchVariance = 0.0f;
}

[RequireComponent(typeof(AudioSource))]
public class GunAudioController : MonoBehaviour {

    public GunAudio ReloadStart;
    public GunAudio ReloadKeyPress;
    public GunAudio ReloadEnd;
    public GunAudio Fire;
    public GunAudio DryFire;
    public GunAudio Hit;
    public GunAudio HitDamaging;

    AudioSource m_audioSource = null;

    private void Awake()
    {
        m_audioSource = GetComponent<AudioSource>();
        Debug.Assert(m_audioSource);
    }

    void PlayGunAudio(GunAudio gunAudio)
    {
        if(m_audioSource && gunAudio != null && gunAudio.clips.Count > 0)
        {
            m_audioSource.clip = gunAudio.clips[Random.Range(0, gunAudio.clips.Count)];
            m_audioSource.pitch = 1.0f + Random.Range(-gunAudio.pitchVariance, gunAudio.pitchVariance);
            m_audioSource.Play();
        }
    }

    public void OnReloadStart()
    {
        PlayGunAudio(ReloadStart);
    }

    public void OnReloadKeyPress()
    {
        PlayGunAudio(ReloadKeyPress);
    }

    public void OnReloadEnd()
    {
        PlayGunAudio(ReloadEnd);
    }

    public void OnFire()
    {
        PlayGunAudio(Fire);
    }

    public void OnDryFire()
    {
        PlayGunAudio(DryFire);
    }

    public void OnHit()
    {
        PlayGunAudio(Hit);
    }

    public void OnHitDamaging()
    {
        PlayGunAudio(HitDamaging);
    }
}
