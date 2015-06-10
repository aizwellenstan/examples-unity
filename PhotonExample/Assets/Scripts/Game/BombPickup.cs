﻿using UnityEngine;
using System.Collections;
using BrainCloudPhotonExample.Connection;

namespace BrainCloudPhotonExample.Game
{
    public class BombPickup : MonoBehaviour
    {
        public int m_pickupID;
        private bool m_isActive = false;
        private float m_lifeTime = 100;

        void OnTriggerEnter(Collider aOther)
        {
            if (!m_isActive) return;
            if (aOther.GetComponent<PhotonView>() != null && aOther.GetComponent<PhotonView>().owner == PhotonNetwork.player)
            {
                GameObject.Find("GameManager").GetComponent<GameManager>().BombPickedUp(aOther.GetComponent<PhotonView>().owner, m_pickupID);
                m_isActive = false;
                Destroy(gameObject);
            }
        }

        void OnTriggerStay(Collider aOther)
        {
            if (!m_isActive) return;
            if (aOther.GetComponent<PhotonView>() != null && aOther.GetComponent<PhotonView>().owner == PhotonNetwork.player)
            {
                GameObject.Find("GameManager").GetComponent<GameManager>().BombPickedUp(aOther.GetComponent<PhotonView>().owner, m_pickupID);
                m_isActive = false;
                Destroy(gameObject);
            }
        }

        public void Activate(int aBombID)
        {
            m_lifeTime = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_bombPickupLifetime;
            m_pickupID = aBombID;
            m_isActive = true;
            GetComponent<Rigidbody>().AddForce(GetRandomDirection() * 22, ForceMode.Impulse);
        }

        Vector3 GetRandomDirection()
        {
            Vector3 randomDirection = Vector3.up;

            randomDirection = Quaternion.Euler(new Vector3(0, 0, 360 / ((((4 * m_pickupID) + 1) % 9) + 1))) * randomDirection;

            return randomDirection.normalized;
        }

        void FixedUpdate()
        {
            m_lifeTime -= Time.fixedDeltaTime;
            if (m_lifeTime <= 0 && m_isActive)
            {
                GameObject.Find("GameManager").GetComponent<GameManager>().DespawnBombPickup(m_pickupID);
                m_isActive = false;
                return;
            }

            Bounds mapBounds = GameObject.Find("MapBounds").GetComponent<Collider>().bounds;
            Vector3 position = transform.position;
            if (position.x < mapBounds.min.x + 25)
            {
                position.x = mapBounds.min.x + 25;
            }
            else if (position.x > mapBounds.max.x - 25)
            {
                position.x = mapBounds.max.x - 25;
            }

            if (position.y < mapBounds.min.y + 25)
            {
                position.y = mapBounds.min.y + 25;
            }
            else if (position.y > mapBounds.max.y - 25)
            {
                position.y = mapBounds.max.y - 25;
            }

            transform.position = position;

        }
    }
}
