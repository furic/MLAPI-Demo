using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using UnityEngine;

namespace VRCade.Multiplayer.Demo
{
	/// <summary>
	/// A network script attached to a object already in scene.
	/// </summary>
	public class MultiplayerDemoSceneObject : NetworkBehaviour
	{
		public static MultiplayerDemoSceneObject Instance;
		[SerializeField] private NetworkVariableInt networkVariableInt;

		void Awake()
		{
			Instance = this;
		}

		public override void NetworkStart()
		{
			Debug.Log("MultiplayerDemoSceneObject:NetworkStart");
			if (IsServer) {
				InvokeRepeating(nameof(ChangeNetworkVariableInt), 10, 30);
			}
		}

		void ChangeNetworkVariableInt()
		{
			networkVariableInt.Value = Random.Range(1, 999);
		}

		public void OnSyncClick()
		{
			Debug.Log("MultiplayerDemoSceneObject:OnSyncClick");
			if (IsServer) {
				SyncClientRpc();
			} else {
				SyncServerRpc();
			}
		}

		[ServerRpc(RequireOwnership = false)]
		void SyncServerRpc()
		{
			Debug.Log("MultiplayerDemoSceneObject:SyncServerRpc");
		}

		[ClientRpc]
		void SyncClientRpc()
		{
			Debug.Log("MultiplayerDemoSceneObject:SyncClientRpc");
		}

	}
}
