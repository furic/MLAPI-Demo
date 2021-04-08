using MLAPI;
using MLAPI.Messaging;
using MLAPI.NetworkVariable;
using UnityEngine;

namespace VRCade.Multiplayer.Demo
{
	/// <summary>
	/// A network script attached to a object spawns in runtime.
	/// </summary>
	public class MultiplayerDemoSpawnedObject : NetworkBehaviour
	{
		public static MultiplayerDemoSpawnedObject Instance;
		[SerializeField] private NetworkVariableInt networkVariableInt;

		void Awake()
		{
			Instance = this;
		}

		void Start()
		{
			if (IsServer) {
				InvokeRepeating(nameof(TestSycnedVar), 10, 30);
			}
		}

		void TestSycnedVar()
		{
			networkVariableInt.Value = Random.Range(1, 999);
		}

		public void OnSyncClick()
		{
			Debug.Log("MultiplayerDemoSpawnedObject:OnSyncClick");
			if (IsServer) {
				SyncClientRpc();
			} else {
				SyncServerRpc();
			}
		}

		[ServerRpc]
		void SyncServerRpc()
		{
			Debug.Log("MultiplayerDemoSpawnedObject:ServerSyncRpc");
		}

		[ClientRpc]
		void SyncClientRpc()
		{
			Debug.Log("MultiplayerDemoSpawnedObject:ClientSyncRpc");
		}
	}
}
