using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class SeatGameobject
{
    [SerializeField]
    public GameObject occupiedSeatParent;

    [SerializeField]
    public TextMeshProUGUI occupierNickname;
}
public class GameMatchingScreen : MonoBehaviour
{
    [Header("Managers")]
    public PUNRoomUtils punRoomUtils;

    [Header("Internals")]
    public GameObject startButton;
    public GameObject notices;
    public List<SeatGameobject> seats;
    public Countdown countdown;

    private void OnEnable()
    {
        Init();
        PUNRoomUtils.onPlayerJoined += OnPlayerJoined;
        PUNRoomUtils.onPlayerLeft += OnPlayerLeft;
    }

    private void OnDisable()
    {
        PUNRoomUtils.onPlayerJoined -= OnPlayerJoined;
        PUNRoomUtils.onPlayerLeft -= OnPlayerLeft;
    }

    private void Init()
    {
        startButton.SetActive(PhotonNetwork.LocalPlayer.IsMasterClient);
        notices.SetActive(false);

        foreach(SeatGameobject seat in seats)
        {
            FreeSeat(seat);
        }

        if (PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            punRoomUtils.AddPlayerCustomProperty("seat", "0");
            OccupySeat(seats[0], PhotonNetwork.LocalPlayer.NickName);
        }
        else
        {
            List<Player> players = punRoomUtils.GetOtherPlayers();
            if (players.Count == 1)
            {
                int otherPlayerSeat = Int32.Parse(players[0].CustomProperties["seat"].ToString());
                OccupySeat(seats[otherPlayerSeat], players[0].NickName);

                int mySeat = (otherPlayerSeat + 1) % 2;
                punRoomUtils.AddPlayerCustomProperty("seat", "" + mySeat);
                OccupySeat(seats[mySeat], PhotonNetwork.LocalPlayer.NickName);

                notices.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"PUN: Inconsistency! There are {players.Count} players in room??");
            }
        }

    }

    private void OccupySeat(SeatGameobject seat, string nickName)
    {
        seat.occupiedSeatParent.SetActive(true);
        seat.occupierNickname.text = nickName;

    }

    private void FreeSeat(SeatGameobject seat)
    {
        seat.occupiedSeatParent.SetActive(false);
        seat.occupierNickname.text = "-";
    }

    private void OnPlayerJoined(string opponentNickname)
    {
        int mySeat = Int32.Parse(PhotonNetwork.LocalPlayer.CustomProperties["seat"].ToString());
        int otherSeat = (mySeat + 1) % 2;
        if(PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            notices.SetActive(true);
        }
        OccupySeat(seats[otherSeat], opponentNickname);
    }

    private void OnPlayerLeft()
    {
        int mySeat = Int32.Parse(PhotonNetwork.LocalPlayer.CustomProperties["seat"].ToString());
        int otherSeat = (mySeat + 1) % 2;
        notices.SetActive(false);
        FreeSeat(seats[otherSeat]);
    }

    public void TryExitRoom()
    {
        punRoomUtils.TryLeaveRoom();
    }

    public void StartGame()
    {
        if (PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsOpen = PhotonNetwork.CurrentRoom.IsVisible =false;
        }
        startButton.SetActive(false);
        GetComponent<PhotonView>().RPC("StartCountdown", RpcTarget.All);
    }


    [PunRPC]
    public void StartCountdown()
    {
        countdown.StartCountDown(() => {
            if (PhotonNetwork.LocalPlayer.IsMasterClient)
            {
                PhotonNetwork.LoadLevel("GameScene");
            }
        });
    }
}
