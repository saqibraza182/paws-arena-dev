using Anura.ConfigurationModule.Managers;
using Cysharp.Threading.Tasks;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace com.colorfulcoding.GameScene
{
    public class HttpNetworkCommunication : MonoBehaviour
    {
        private void Start()
        {
            RegisterStartOfTheMatch();
        }

        private async void RegisterStartOfTheMatch()
        {
            Debug.Log($"RoomId: {PhotonNetwork.CurrentRoom.Name}");

            LeaderboardPostRequestEntity req = new LeaderboardPostRequestEntity()
            {
                matchId = PhotonNetwork.CurrentRoom.Name,
                kittyUrl = GameState.selectedNFT.imageUrl,
                status = GetMatchStartStatus()
            };

            string reqJson = JsonUtility.ToJson(req);

            if (ConfigurationManager.Instance.GameConfig.enableDevLogs)
            {
                Debug.Log(reqJson);
            }

            try
            {
                await NetworkManager.POSTRequest("/leaderboard/match", reqJson, (resp) =>
                {
                    Debug.Log("[HTTP]Match start registered!");
                }, (err, code) =>
                {
                    Debug.LogWarning($"[HTTP]Error registering the match {code}: {err}");
                }, true);
            }catch(UnityWebRequestException ex)
            {
                Debug.LogWarning($"[HTTP]Error registering the match {ex.ResponseCode}: {ex.Text}");
                RoomStateManager.Instance.OnPlayerLeft();
            }
        }

        public async UniTask RegisterEndOfTheMatch(int hp, GameResolveState state)
        {
            LeaderboardPostRequestEntity req = new LeaderboardPostRequestEntity()
            {
                matchId = PhotonNetwork.CurrentRoom.Name,
                kittyUrl = GameState.selectedNFT.imageUrl,
                status = GetMatchEndStatus(),
                hp = hp,
                winner = (state == GameResolveState.DRAW ? GameResult.Draw : (state == GameResolveState.PLAYER_1_WIN ? GameResult.Player1 : GameResult.Player2))
            };

            string reqJson = JsonUtility.ToJson(req);

            if (ConfigurationManager.Instance.GameConfig.enableDevLogs)
            {
                Debug.Log(reqJson);
            }

            await NetworkManager.POSTRequest("/leaderboard/match", reqJson, (resp) =>
            {
                LeaderboardPostResponseEntity response = JsonUtility.FromJson<LeaderboardPostResponseEntity>(resp);
                Debug.Log(resp);
                Debug.Log($"[HTTP]Match ending registered! You won {response.oldPoints + response.points} points.");
                GameState.pointsChange = response;
            }, (err, code) =>
            {
                Debug.LogWarning($"[HTTP]Error registering the match ending; {code}: {err}");
            }, true);
        }

        private MatchStatus GetMatchStartStatus()
        {
            if (ConfigurationManager.Instance.Config.GetGameType() == Anura.ConfigurationModule.ScriptableObjects.GameType.SINGLEPLAYER)
            {
                return MatchStatus.MatchStartedForBothPlayers;
            }

            if (PhotonNetwork.CurrentRoom.masterClientId == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                return MatchStatus.MatchStartedForPlayer1;
            }

            return MatchStatus.MatchStartedForPlayer2;
        }

        private MatchStatus GetMatchEndStatus()
        {
            if (ConfigurationManager.Instance.Config.GetGameType() == Anura.ConfigurationModule.ScriptableObjects.GameType.SINGLEPLAYER)
            {
                return MatchStatus.MatchFinishedForBothPlayers;
            }

            if (PhotonNetwork.CurrentRoom.masterClientId == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                return MatchStatus.MatchFinishedForPlayer1;
            }

            return MatchStatus.MatchFinishedForPlayer2;
        }
    }
}
