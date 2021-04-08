using UnityEngine;
using UnityEngine.UI;
using MLAPI;
using MLAPI.Transports.SteamP2P;
using Steamworks;
using System.Collections.Generic;

namespace VRCade.Multiplayer.Demo
{
	/// <summary>
	/// A simple UI canvas that showcase the lobby functionality.
	/// </summary>
	public class MultiplayerDemoCanvas : NetworkBehaviour
	{

		[SerializeField] GameObject menuHolder, gameplayHolder;
		[SerializeField] Dropdown createLobbyTypeDropdown, publicLobbiesDropdown, friendLobbyiesDropdown, inviteFriendDropdown;
		[SerializeField] GameObject spawnedObjectPerfab;

		Callback<LobbyCreated_t> lobbyCreated;
		Callback<LobbyMatchList_t> lobbyMatchList;
		Callback<LobbyEnter_t> lobbyEnter;
		Callback<LobbyDataUpdate_t> lobbyDataUpdate;
		Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;

		CSteamID lobbyID;
		CSteamID hostSteamID;
		List<CSteamID> lobbyIDList = new List<CSteamID>();
		List<CSteamID> friendSteamIDList = new List<CSteamID>();
		List<CSteamID> friendLobbyIDList = new List<CSteamID>();

		void Start()
		{
			lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated); // Fires on creating a lobby in host
			lobbyMatchList = Callback<LobbyMatchList_t>.Create(OnLobbyListObtained); // Fires on listing lobbies in client
			lobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEntered); // Fires on entering a lobby in both host and client
			lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdated); // Fires on updating the lobby data in host
			gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested); // Fires on clicking the invite popup in Steam and game is already running in client

			InvokeRepeating(nameof(RequestLobbyList), 1, 1); // Keep trying for every 1s
			InvokeRepeating(nameof(RequestFriendLobbyList), 1, 1); // Keep trying for every 1s

			// Ready the argument to see if the game is started by an invitation
			string[] args = System.Environment.GetCommandLineArgs();
			for (int i = 0; i < args.Length; i++) {
				//Debug.Log("Found argument " + i + ": " + args[i]);
				if (args[i] == "+connect_lobby") {
					Debug.Log("Found connect lobby argument " + i + ": " + args[i]);
					lobbyID = (CSteamID) ulong.Parse(args[i + 1]);
					InvokeRepeating("WaitToJoinLobby", 1, 1); // Keep trying for every 1s
				}
			}
		}

		private void SwitchToGameplay()
		{
			menuHolder.SetActive(false);
			gameplayHolder.SetActive(true);
		}

		public void SwitchToMenu()
		{
			menuHolder.SetActive(true);
			gameplayHolder.SetActive(false);
		}

		#region Connection

		public void StartHost()
		{
			NetworkManager.Singleton.OnClientConnectedCallback += (clientId) => {
				Debug.Log($"Client connected, clientId={clientId}");
			};

			NetworkManager.Singleton.OnClientDisconnectCallback += (clientId) => {
				Debug.Log($"Client disconnected, clientId={clientId}");
			};

			NetworkManager.Singleton.OnServerStarted += () => {
				Debug.Log("Server started");
				GameObject spawnedObject = GameObject.Instantiate(spawnedObjectPerfab);
				spawnedObject.GetComponent<NetworkObject>().Spawn();
			};

			NetworkManager.Singleton.StartHost();
			SwitchToGameplay();

			hostSteamID = Steamworks.SteamUser.GetSteamID();
			var lobbyType = Steamworks.ELobbyType.k_ELobbyTypePublic;
			if (createLobbyTypeDropdown.value == 1)
				lobbyType = Steamworks.ELobbyType.k_ELobbyTypeFriendsOnly;
			if (createLobbyTypeDropdown.value == 2)
				lobbyType = Steamworks.ELobbyType.k_ELobbyTypePrivate;
			SteamMatchmaking.CreateLobby(lobbyType, 4);
		}

		public void StartClient()
		{
			NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
			NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;

			NetworkManager.Singleton.GetComponent<SteamP2PTransport>().ConnectToSteamID = hostSteamID.m_SteamID;

			Debug.Log($"Joining room hosted by {NetworkManager.Singleton.GetComponent<SteamP2PTransport>().ConnectToSteamID}");

			//SceneManager.LoadScene("MultiplayerDemo");
			//SceneManager.sceneLoaded += (scene, mode) => {
			//	NetworkManager.Singleton.StartClient();
			//	SwitchToGameplay();
			//};
			NetworkManager.Singleton.StartClient();
			SwitchToGameplay();
		}

		void ClientConnected(ulong clientId)
		{
			Debug.Log($"I'm connected, clientId={clientId}");
		}

		void ClientDisconnected(ulong clientId)
		{
			Debug.Log($"I'm disconnected, clientId={clientId}");
			NetworkManager.Singleton.OnClientDisconnectCallback -= ClientDisconnected;   // remove these else they will get called multiple time if we reconnect this client again
			NetworkManager.Singleton.OnClientConnectedCallback -= ClientConnected;
		}

		public void Disconnect()
		{
			SteamMatchmaking.LeaveLobby(lobbyID);
			SteamNetworking.CloseP2PSessionWithUser(hostSteamID);
			if (IsServer)
				NetworkManager.Singleton.StopHost();
			else
				NetworkManager.Singleton.StopClient();
		}

		void OnApplicationQuit()
		{
			Disconnect();
		}

		#endregion

		#region Public Lobbies

		void RequestLobbyList()
		{
			if (SteamManager.Initialized) {
				SteamMatchmaking.RequestLobbyList();
				CancelInvoke(nameof(RequestLobbyList));
			}
		}

		public void JoinPublicLobby()
		{
			SteamMatchmaking.JoinLobby(SteamMatchmaking.GetLobbyByIndex(publicLobbiesDropdown.value));
		}

		void OnLobbyCreated(LobbyCreated_t result)
		{
			if (result.m_eResult == EResult.k_EResultOK)
				Debug.Log("Lobby created successfully - LobbyID=" + result.m_ulSteamIDLobby);
			else
				Debug.Log("Lobby created failed - LobbyID=" + result.m_ulSteamIDLobby);
			lobbyID = (CSteamID) result.m_ulSteamIDLobby;
			string personalName = SteamFriends.GetPersonaName();
			SteamMatchmaking.SetLobbyData((CSteamID) result.m_ulSteamIDLobby, "name", personalName + "'s Room");
		}

		void OnLobbyListObtained(LobbyMatchList_t result)
		{
			var options = new List<Dropdown.OptionData>();
			Debug.Log("Found " + result.m_nLobbiesMatching + " public lobbies!");
			for (int i = 0; i < result.m_nLobbiesMatching; i++) {
				CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
				lobbyIDList.Add(lobbyId);
				SteamMatchmaking.RequestLobbyData(lobbyId);
				options.Add(new Dropdown.OptionData("Lobby a " + i));
			}
			publicLobbiesDropdown.AddOptions(options);
		}

		void OnLobbyDataUpdated(LobbyDataUpdate_t result)
		{
			for (int i = 0; i < lobbyIDList.Count; i++) {
				if (lobbyIDList[i].m_SteamID == result.m_ulSteamIDLobby) {
					string lobbyName = SteamMatchmaking.GetLobbyData((CSteamID) lobbyIDList[i].m_SteamID, "name");
					publicLobbiesDropdown.options[i].text = lobbyName;
					if (i == 0)
						publicLobbiesDropdown.captionText.text = lobbyName;
					return;
				}
			}
		}

		void OnLobbyEntered(LobbyEnter_t result)
		{
			lobbyID = (CSteamID) result.m_ulSteamIDLobby;
			if (result.m_EChatRoomEnterResponse == 1)
				Debug.Log($"Successfully joined lobby {SteamMatchmaking.GetLobbyData((CSteamID) result.m_ulSteamIDLobby, "name")}!");
			else
				Debug.Log("Failed to join lobby.");

			int playerCount = SteamMatchmaking.GetNumLobbyMembers((CSteamID) result.m_ulSteamIDLobby);

			// Join host's game directly
			if (playerCount > 1) {
				var ownerSteamID = SteamMatchmaking.GetLobbyMemberByIndex((CSteamID) result.m_ulSteamIDLobby, 0);
				hostSteamID = ownerSteamID;
				StartClient();
			}
		}

		#endregion

		#region Friend Lobbies

		void RequestFriendLobbyList()
		{
			if (SteamManager.Initialized) {
				int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
				Debug.Log("Found " + friendCount + " friends!");
				var friendLobbyOptions = new List<Dropdown.OptionData>();
				var inviteFriendOptions = new List<Dropdown.OptionData>();
				for (int i = 0; i < friendCount; i++) {
					CSteamID friendSteamID = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);

					friendSteamIDList.Add(friendSteamID);
					inviteFriendOptions.Add(new Dropdown.OptionData(SteamFriends.GetFriendPersonaName(friendSteamID)));

					Debug.Log("Friend: " + SteamFriends.GetFriendPersonaName(friendSteamID) + " - " + friendSteamID + " - Level " + SteamFriends.GetFriendSteamLevel(friendSteamID));
					if (SteamFriends.GetFriendGamePlayed(friendSteamID, out FriendGameInfo_t friendGameInfo) && friendGameInfo.m_steamIDLobby.IsValid()) {
						// friendGameInfo.m_steamIDLobby is a valid lobby, you can join it or use RequestLobbyData() get its metadata
						Debug.Log(SteamFriends.GetFriendPersonaName(friendSteamID) + " is hosting a lobby!");
						friendLobbyOptions.Add(new Dropdown.OptionData(SteamFriends.GetFriendPersonaName(friendSteamID) + "'s Room"));
						friendLobbyIDList.Add(friendGameInfo.m_steamIDLobby);
					} else {
						Debug.Log(SteamFriends.GetFriendPersonaName(friendSteamID) + " is not hosting a lobby, ignore.");
					}
				}
				friendLobbyiesDropdown.AddOptions(friendLobbyOptions);
				inviteFriendDropdown.AddOptions(inviteFriendOptions);

				CancelInvoke("RequestFriendLobbyList");
			}
		}

		public void InviteFriendToLobby()
		{
			var friendSteamID = friendSteamIDList[inviteFriendDropdown.value];

			Debug.Log("Inviting " + SteamFriends.GetFriendPersonaName(friendSteamID) + " (" + friendSteamID + ")");
			//bool success = SteamFriends.InviteUserToGame(friendSteamID, "Hey, join me playing game togather!");
			bool success = SteamMatchmaking.InviteUserToLobby(lobbyID, friendSteamID);
			if (success)
				Debug.Log("Successfully invite " + SteamFriends.GetFriendPersonaName(friendSteamID));
			else
				Debug.Log("Failed to invite " + SteamFriends.GetFriendPersonaName(friendSteamID));
		}

		public void InviteFriendsToLobby()
		{
			// If you want the user to select from a list of friends to invite to a lobb
			SteamFriends.ActivateGameOverlayInviteDialog(lobbyID);
		}

		void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t pCallback)
		{
			Debug.Log("[" + GameLobbyJoinRequested_t.k_iCallback + " - GameLobbyJoinRequested] - " + pCallback.m_steamIDLobby + " -- " + pCallback.m_steamIDFriend);
			SteamMatchmaking.JoinLobby(pCallback.m_steamIDLobby);
		}

		public void JoinFriendLobby()
		{
			SteamMatchmaking.JoinLobby(friendLobbyIDList[friendLobbyiesDropdown.value]);
		}

		#endregion

		void WaitToJoinLobby()
		{
			if (SteamManager.Initialized) {
				SteamMatchmaking.JoinLobby(lobbyID);
				CancelInvoke("WaitToJoinLobby");
			}
		}

		#region Message Sync

		public void OnSyncPlayerClick()
		{
			MultiplayerDemoPlayer.myPlayer.OnSyncClick();
		}

		public void OnSyncSceneObjectClick()
		{
			MultiplayerDemoSceneObject.Instance.OnSyncClick();
		}

		public void OnSyncSpawnedObjectClick()
		{
			MultiplayerDemoSpawnedObject.Instance.OnSyncClick();
		}

		#endregion

	}
}
