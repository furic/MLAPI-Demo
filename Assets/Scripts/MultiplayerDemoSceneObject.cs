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
			Debug.Log("MultiplayerDemoSceneObject:ServerSyncRpc");
		}

		[ClientRpc]
		void SyncClientRpc()
		{
			Debug.Log("MultiplayerDemoSceneObject:ClientSyncRpc");
		}

	}
}
