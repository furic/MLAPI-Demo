using UnityEngine;
using MLAPI;
using MLAPI.Messaging;
using Steamworks;

namespace VRCade.Multiplayer.Demo
{
	public class MultiplayerDemoPlayer : NetworkBehaviour
	{
		public static MultiplayerDemoPlayer myPlayer;

		AudioSource audioSource;

		void Awake()
		{
			audioSource = GetComponent<AudioSource>();
		}

		void Start()
		{
			if (IsLocalPlayer) {
				myPlayer = this;
				SteamUser.StartVoiceRecording(); // Start recording automatically
			}
		}

		public void OnServerStart()
		{
			Debug.LogFormat("MultiplayerDemoPlayer:OnServerStart - IsLocalPlayer={0}", IsLocalPlayer);
		}

		public void OnSyncClick()
		{
			Debug.Log("MultiplayerDemoPlayer:OnSyncClick");
			if (IsServer) {
				SyncClientRpc();
			} else {
				SyncServerRpc();
			}
		}

		[ServerRpc]
		void SyncServerRpc()
		{
			Debug.Log("MultiplayerDemoPlayer:ServerSyncRpc");
		}

		[ClientRpc]
		void SyncClientRpc()
		{
			Debug.Log("MultiplayerDemoPlayer:ClientSyncRpc");
		}

		void Update()
		{
			if (IsLocalPlayer) {
				EVoiceResult voiceResult = SteamUser.GetAvailableVoice(out uint compressed);
				//Debug.LogFormat("MultiplayerDemoPlayer:Update - voiceResult={0}, compressed={1}", voiceResult, compressed);
				if (voiceResult == EVoiceResult.k_EVoiceResultOK && compressed > 1024) {
					byte[] byteBuffer = new byte[1024];
					voiceResult = SteamUser.GetVoice(true, byteBuffer, 1024, out uint bufferSize);
					if (voiceResult == EVoiceResult.k_EVoiceResultOK && bufferSize > 0) {
						SendVoiceDataServerRpc(byteBuffer, bufferSize);
					}
				}
			}
		}

		[ServerRpc]
		void SendVoiceDataServerRpc(byte[] byteBuffer, uint byteCount)
		{
			//Debug.LogFormat("MultiplayerDemoPlayer:SendVoiceData - destBuffer.Length={0}, byteCount={1}", byteBuffer.Length, byteCount);
			var colliders = Physics.OverlapSphere(transform.position, 50, LayerMask.GetMask(new string[] { "Player" }));
			foreach (var collider in colliders) {
				var networkedObject = collider.GetComponent<NetworkObject>();
				if (networkedObject.OwnerClientId == GetComponent<NetworkObject>().OwnerClientId) { // Do not play voice on the player's own client
					continue;
				}
				if (networkedObject != null) {
					PlaySoundClientRpc(byteBuffer, byteCount,
						new ClientRpcParams {
							Send = new ClientRpcSendParams {
								TargetClientIds = new ulong[] { networkedObject.OwnerClientId }
							}
						}
					);
				}
			}
		}

		[ClientRpc]
		void PlaySoundClientRpc(byte[] byteBuffer, uint byteCount, ClientRpcParams clientRpcParams = default)
		{
			//Debug.LogFormat("MultiplayerDemoPlayer:ClientPlaySound - destBuffer.Length={0}, byteCount={1}", byteBuffer.Length, byteCount);
			byte[] destBuffer = new byte[22050 * 2];
			EVoiceResult voiceResult = SteamUser.DecompressVoice(byteBuffer, byteCount, destBuffer, (uint) destBuffer.Length, out uint bytesWritten, 22050);
			//Debug.LogFormat("MultiplayerDemoPlayer:ClientPlaySound - voiceResult={0}, bytesWritten={1}", voiceResult, bytesWritten);
			if (voiceResult == EVoiceResult.k_EVoiceResultOK && bytesWritten > 0) {
				audioSource.clip = AudioClip.Create(UnityEngine.Random.Range(100, 1000000).ToString(), 22050, 1, 22050, false);
				float[] test = new float[22050];
				for (int i = 0; i < test.Length; ++i) {
					test[i] = (short) (destBuffer[i * 2] | destBuffer[i * 2 + 1] << 8) / 32768.0f;
				}
				audioSource.clip.SetData(test, 0);
				audioSource.Play();
			}
		}

	}
}
