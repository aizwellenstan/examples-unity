﻿using UnityEngine;
using System.Collections;
using BrainCloudPhotonExample.Connection;

namespace BrainCloudPhotonExample.Game
{
    public class FlareController : MonoBehaviour
    {
        private bool m_isActive = false;
        private float m_lifeTime = 100;
        private GameObject m_offscreenIndicator;
        private PhotonPlayer m_player;

        public void Activate(PhotonPlayer aPlayer)
        {
            m_lifeTime = GameObject.Find("BrainCloudStats").GetComponent<BrainCloudStats>().m_flareLifetime;
            m_isActive = true;
            m_player = aPlayer;

        }

        void Start()
        {
            m_offscreenIndicator = transform.GetChild(1).gameObject;
        }

        // Use this for initialization
        void FixedUpdate()
        {
            m_lifeTime -= Time.fixedDeltaTime;
            if (m_lifeTime <= 0 && m_isActive)
            {
                Destroy(gameObject);
                m_isActive = false;
                return;
            }
        }

        // Update is called once per frame
        void LateUpdate()
        {
            if (m_isActive && (int)m_player.customProperties["Team"] == (int)PhotonNetwork.player.customProperties["Team"])
            {
                m_offscreenIndicator.transform.position = transform.position;
                Vector3 position = m_offscreenIndicator.transform.position;
                Vector3 point = Camera.main.WorldToScreenPoint(position);
                if (point.x > Screen.width - 10) point.x = Screen.width - 10;
                if (point.x < 0 + 10) point.x = 0 + 10;
                if (point.y > Screen.height - 10) point.y = Screen.height - 10;
                if (point.y < 0 + 10) point.y = 0 + 10;
                point.z = 10;
                point = Camera.main.ScreenToWorldPoint(point);
                m_offscreenIndicator.transform.position = point;
                point -= Camera.main.transform.position;
                m_offscreenIndicator.transform.eulerAngles = new Vector3(0, 0, Mathf.Atan2(point.y, point.x) * Mathf.Rad2Deg - 90);

                transform.GetChild(2).GetComponent<TextMesh>().text = m_player.customProperties["RoomDisplayName"].ToString();
                transform.GetChild(2).position = m_offscreenIndicator.transform.position + new Vector3(0, -0.8f, 0);
                transform.GetChild(2).eulerAngles = new Vector3(0, 0, 0);
            }
            else
            {
                transform.GetChild(1).gameObject.SetActive(false);
                transform.GetChild(2).gameObject.SetActive(false);
            }
        }
    }
}
