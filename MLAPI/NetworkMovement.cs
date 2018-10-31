using UnityEngine;
using System.Collections.Generic;
using MLAPI;
using MLAPI.Serialization;
using System.IO;

// Server-authoritative movement with Client-side prediction and reconciliation
// Author:gennadiy.shvetsov@gmail.com
// QoS channels used:
// channel #0: Reliable Sequenced
// channel #1: Unreliable Sequenced
[RequireComponent(typeof(CharacterController))]
public class NetworkMovement : NetworkedBehaviour
{
	#region Declarations

	CharacterController characterController;

	public bool _isServer;
	public bool _isLocalPlayer;
	public bool _isClient;
	public bool _isGrounded;

	public float verticalMouseLookLimit = 170f;
	private float _verticalSpeed = 0f;

	public float _snapDistance = 1f;

	// this is a local client setting that will be sent with inputs and relayed with results
	public bool mouseSteer = true;

	public float rotateSpeed = .1f;
	public float mouseSensitivity = 100f;

	private bool _jump = false;
	public float _jumpHeight = 10f;

	// This struct would be used to collect player inputs
	[System.Serializable]
	public struct Inputs
	{
		public float forward;
		public float sides;
		public float vertical;
		public float pitch;
		public float yaw;
		public bool mouse;
		public bool sprint;
		public bool crouch;

		public float timeStamp;
	}

	[System.Serializable]
	public struct SyncInputs
	{
		public sbyte forward;
		public sbyte sides;
		public sbyte vertical;
		public float pitch;
		public float yaw;
		public bool mouse;
		public bool sprint;
		public bool crouch;

		public float timeStamp;
	}

	// This struct would be used to collect results of Move and Rotate functions
	[System.Serializable]
	public struct Results
	{
		public Quaternion rotation;
		public Vector3 position;
		public bool sprinting;
		public bool crouching;
		public bool mousing;
		public float timeStamp;
	}

	[System.Serializable]
	public struct SyncResults : IBitWritable
	{
		public Vector3 position;
		public ushort pitch;
		public ushort yaw;
		public bool mousing;
		public bool sprinting;
		public bool crouching;
		public float timeStamp;

		public void Read(Stream stream)
		{
			using (PooledBitReader reader = PooledBitReader.Get(stream))
			{
				position = reader.ReadVector3Packed();
				pitch = reader.ReadUInt16Packed();
				yaw = reader.ReadUInt16Packed();
				mousing = reader.ReadBool();
				sprinting = reader.ReadBool();
				crouching = reader.ReadBool();
				timeStamp = reader.ReadSinglePacked();
			}
		}

		public void Write(Stream stream)
		{
			using (PooledBitWriter writer = PooledBitWriter.Get(stream))
			{
				writer.WriteVector3Packed(position);
				writer.WriteUInt16Packed(pitch);
				writer.WriteUInt16Packed(yaw);
				writer.WriteBool(mousing);
				writer.WriteBool(sprinting);
				writer.WriteBool(crouching);
				writer.WriteSinglePacked(timeStamp);
			}
		}
	}

	public Inputs _inputs;

	// Synced from server to all clients
	private NetworkedVar<SyncResults> syncResults = new NetworkedVar<SyncResults>();

	public Results rcvdResults;

	public Results _results;

	// Owner client and server would store it's inputs in this list
	public List<Inputs> _inputsList = new List<Inputs>();

	// This list stores results of movement and rotation.
	// Needed for non-owner client interpolation
	public List<Results> _resultsList = new List<Results>();

	// Interpolation related variables
	private bool _playData = false;
	private float _dataStep = 0f;
	private float _lastTimeStamp = 0f;
	// private bool _jumping = false;
	private Vector3 _startPosition;
	private Quaternion _startRotation;

	private float _step = 0;

	#endregion

	#region Monobehaviors

	void Start()
	{
		_isServer = isServer;
		_isLocalPlayer = isLocalPlayer;
		_isClient = isClient;

		characterController = GetComponent<CharacterController>();
		syncResults.OnValueChanged = RecieveResults;

		if (isServer)
		{
			_results.position = transform.position;
			_results.rotation = transform.rotation;

			InvokeClientRpcOnOwner(SetStartPosition, _results.position);
			InvokeClientRpcOnOwner(SetStartRotation, _results.rotation);
		}
	}

	void Update()
	{
		if (isLocalPlayer)
		{
			if (Input.GetButtonUp("SteerMode"))
				mouseSteer = !mouseSteer;

			// Getting clients inputs
			GetInputs(ref _inputs);
		}
	}

	private bool GroundedOLD()
	{
		Vector3 origin = transform.position + new Vector3(0, (-characterController.height * 0.5f) + characterController.radius, 0);
		float radius = characterController.radius;
		Vector3 direction = Vector3.down;
		RaycastHit hit;
		float maxDistance = radius + .01f;// + characterController.skinWidth + .001f;
		if (Physics.SphereCast(origin, radius, direction, out hit, maxDistance))
		{
			return true;

			//Transform other = hit.transform;
			//if (other)
			//{
			//	Vector3 down = transform.TransformDirection(Vector3.down);
			//	Vector3 toOther = other.position - transform.position;

			//	if (Vector3.Dot(down, toOther) < 0.1f)
			//		return true;
			//}
		}
		return false;
	}

	//private void OnDrawGizmos()
	//{
	//	Vector3 origin = transform.position + new Vector3(0, (-characterController.height * 0.5f) + characterController.radius, 0);
	//	float radius = characterController.radius;
	//	Gizmos.color = Color.black;
	//	Gizmos.DrawSphere(origin, radius);
	//}

	private bool Grounded()
	{
		Vector3 origin = transform.position + new Vector3(0, -characterController.height * 0.5f, 0);
		Vector3 direction = Vector3.down;
		float maxDistance = 0.1f;
		return Physics.Raycast(origin, direction, maxDistance);
	}

	void FixedUpdate()
	{
		_isGrounded = Grounded();

		if (isLocalPlayer)
		{
			_inputs.timeStamp = Time.time;
			// Client side prediction for non-authoritative client or plane movement and rotation for listen server/host
			Vector3 lastPosition = _results.position;
			Quaternion lastRotation = _results.rotation;
			bool lastCrouch = _results.crouching;
			_results.rotation = Rotate(_inputs, _results);
			_results.mousing = Mouse(_inputs, _results);
			_results.sprinting = Sprint(_inputs, _results);
			_results.crouching = Crouch(_inputs, _results);
			_results.position = Move(_inputs, _results);
			if (isServer)
			{
				// Listen server/host part
				// Sending results to other clients(state sync)
				// if (_dataStep >= GetNetworkSendInterval())
				if (_dataStep >= .05f)
				{
					if (Vector3.Distance(_results.position, lastPosition) > 0f || Quaternion.Angle(_results.rotation, lastRotation) > 0f || _results.crouching != lastCrouch)
					{
						_results.timeStamp = _inputs.timeStamp;
						// Struct need to be fully new to count as dirty 
						// Convering some of the values to get less traffic
						SyncResults tempResults;
						tempResults.position = _results.position;
						tempResults.pitch = (ushort)(_results.rotation.eulerAngles.x * 182f);
						tempResults.yaw = (ushort)(_results.rotation.eulerAngles.y * 182f);
						tempResults.mousing = _results.mousing;
						tempResults.sprinting = _results.sprinting;
						tempResults.crouching = _results.crouching;
						tempResults.timeStamp = _results.timeStamp;
						syncResults.Value = tempResults;
					}
					_dataStep = 0f;
				}
				_dataStep += Time.fixedDeltaTime;
			}
			else
			{
				// Owner client. Non-authoritative part
				// Add inputs to the inputs list so they could be used during reconciliation process
				if (Vector3.Distance(_results.position, lastPosition) > 0f || Quaternion.Angle(_results.rotation, lastRotation) > 0f || _results.crouching != lastCrouch)
					_inputsList.Add(_inputs);

				// Sending inputs to the server
				// Unfortunately there is no method overload for [Command] so I need to write several almost similar functions
				// This one is needed to save on network traffic
				SyncInputs syncInputs;
				syncInputs.forward = (sbyte)(_inputs.forward * 127f);
				syncInputs.sides = (sbyte)(_inputs.sides * 127f);
				syncInputs.vertical = (sbyte)(_inputs.vertical * 127f);
				if (Vector3.Distance(_results.position, lastPosition) > 0f)
				{
					if (Quaternion.Angle(_results.rotation, lastRotation) > 0f)
						InvokeServerRpc(Cmd_MovementRotationInputs, syncInputs.forward, syncInputs.sides, syncInputs.vertical, _inputs.pitch, _inputs.yaw, _inputs.mouse, _inputs.sprint, _inputs.crouch, _inputs.timeStamp);
					else
						InvokeServerRpc(Cmd_MovementInputs, syncInputs.forward, syncInputs.sides, syncInputs.vertical, _inputs.sprint, _inputs.crouch, _inputs.timeStamp);
				}
				else
				{
					if (Quaternion.Angle(_results.rotation, lastRotation) > 0f)
						InvokeServerRpc(Cmd_RotationInputs, _inputs.pitch, _inputs.yaw, _inputs.mouse, _inputs.crouch, _inputs.timeStamp);
					else
						InvokeServerRpc(Cmd_OnlyStances, _inputs.crouch, _inputs.timeStamp);
				}
			}
		}
		else
		{
			if (isServer)
			{
				// Server
				// Check if there is atleast one record in inputs list
				if (_inputsList.Count == 0)
					return;

				// Move and rotate part. Nothing interesting here
				Inputs inputs = _inputsList[0];
				_inputsList.RemoveAt(0);
				Vector3 lastPosition = _results.position;
				Quaternion lastRotation = _results.rotation;
				bool lastCrouch = _results.crouching;
				_results.rotation = Rotate(inputs, _results);
				_results.mousing = Mouse(inputs, _results);
				_results.sprinting = Sprint(inputs, _results);
				_results.crouching = Crouch(inputs, _results);
				_results.position = Move(inputs, _results);

				// Sending results to other clients(state sync)
				// if (_dataStep >= GetNetworkSendInterval())
				if (_dataStep >= .05f)
				{
					if (Vector3.Distance(_results.position, lastPosition) > 0f || Quaternion.Angle(_results.rotation, lastRotation) > 0f || _results.crouching != lastCrouch)
					{
						// Struct need to be fully new to count as dirty 
						// Convering some of the values to get less traffic
						_results.timeStamp = inputs.timeStamp;
						SyncResults tempResults;
						tempResults.position = _results.position;
						tempResults.pitch = (ushort)(_results.rotation.eulerAngles.x * 182f);
						tempResults.yaw = (ushort)(_results.rotation.eulerAngles.y * 182f);
						tempResults.mousing = _results.mousing;
						tempResults.sprinting = _results.sprinting;
						tempResults.crouching = _results.crouching;
						tempResults.timeStamp = _results.timeStamp;
						syncResults.Value = tempResults;
					}
					_dataStep = 0;
				}
				_dataStep += Time.fixedDeltaTime;
			}
			else
			{
				// Non-owner client a.k.a. dummy client
				// there should be at least two records in the results list so it would be possible to interpolate between them in case if there would be some dropped packed or latency spike
				// And yes this stupid structure should be here because it should start playing data when there are at least two records and continue playing even if there is only one record left 
				if (_resultsList.Count == 0)
					_playData = false;

				if (_resultsList.Count >= 2)
					_playData = true;

				if (_playData)
				{
					if (_dataStep == 0f)
					{
						_startPosition = _results.position;
						_startRotation = _results.rotation;
					}
					// _step = 1f / (GetNetworkSendInterval());
					_step = 1f / .05f;
					_results.position = Vector3.Lerp(_startPosition, _resultsList[0].position, _dataStep);
					_results.rotation = Quaternion.Slerp(_startRotation, _resultsList[0].rotation, _dataStep);
					_results.mousing = _resultsList[0].mousing;
					_results.sprinting = _resultsList[0].sprinting;
					_results.crouching = _resultsList[0].crouching;
					_dataStep += _step * Time.fixedDeltaTime;
					if (_dataStep >= 1f)
					{
						_dataStep = 0;
						_resultsList.RemoveAt(0);
					}
				}

				UpdatePosition(_results.position);
				UpdateRotation(_results.rotation);
				UpdateMouse(_results.mousing);
				UpdateSprinting(_results.sprinting);
				UpdateCrouch(_results.crouching);
			}
		}
	}

	#endregion

	#region Helpers

	sbyte RoundToLargest(float inp)
	{
		if (inp > 0f)
			return 1;
		else if (inp < 0f)
			return -1;

		return 0;
	}

	#endregion

	#region ClientRPCs

	[ClientRPC]
	public void SetStartPosition(Vector3 position)
	{
		if (isLocalPlayer)
			_results.position = position;
	}

	[ClientRPC]
	public void SetStartRotation(Quaternion rotation)
	{
		if (isLocalPlayer)
			_results.rotation = rotation;
	}

	#endregion

	#region ServerRPCs

	// Standing on spot
	[ServerRPC(RequireOwnership = true)]
	void Cmd_OnlyStances(bool crouch, float timeStamp)
	{
		Debug.Log("Cmd_OnlyStances");
		if (isServer && !isLocalPlayer)
		{
			Debug.Log("Cmd_OnlyStances isServer");
			Inputs inputs;
			inputs.forward = 0f;
			inputs.sides = 0f;
			inputs.vertical = 0f;
			inputs.pitch = 0f;
			inputs.yaw = 0;
			inputs.mouse = false;
			inputs.sprint = false;
			inputs.crouch = crouch;
			inputs.timeStamp = timeStamp;
			_inputsList.Add(inputs);
		}
	}

	// Only rotation inputs sent 
	[ServerRPC(RequireOwnership = true)]
	void Cmd_RotationInputs(float pitch, float yaw, bool mouse, bool crouch, float timeStamp)
	{
		Debug.Log("Cmd_RotationInputs");
		if (isServer && !isLocalPlayer)
		{
			Debug.Log("Cmd_RotationInputs isServer");
			Inputs inputs;
			inputs.forward = 0f;
			inputs.sides = 0f;
			inputs.vertical = 0f;
			inputs.pitch = pitch;
			inputs.yaw = yaw;
			inputs.mouse = mouse;
			inputs.sprint = false;
			inputs.crouch = crouch;
			inputs.timeStamp = timeStamp;
			_inputsList.Add(inputs);
		}
	}

	// Rotation and movement inputs sent 
	[ServerRPC(RequireOwnership = true)]
	void Cmd_MovementRotationInputs(sbyte forward, sbyte sides, sbyte vertical, float pitch, float yaw, bool mouse, bool sprint, bool crouch, float timeStamp)
	{
		Debug.Log("Cmd_MovementRotationInputs");
		if (isServer && !isLocalPlayer)
		{
			Debug.Log("Cmd_MovementRotationInputs isServer");
			Inputs inputs;
			inputs.forward = Mathf.Clamp((float)forward / 127f, -1f, 1f);
			inputs.sides = Mathf.Clamp((float)sides / 127f, -1f, 1f);
			inputs.vertical = Mathf.Clamp((float)vertical / 127f, -1f, 1f);
			inputs.pitch = pitch;
			inputs.yaw = yaw;
			inputs.mouse = mouse;
			inputs.sprint = sprint;
			inputs.crouch = crouch;
			inputs.timeStamp = timeStamp;
			_inputsList.Add(inputs);
		}
	}

	// Only movements inputs sent
	[ServerRPC(RequireOwnership = true)]
	void Cmd_MovementInputs(sbyte forward, sbyte sides, sbyte vertical, bool sprint, bool crouch, float timeStamp)
	{
		Debug.Log("Cmd_MovementInputs");
		if (isServer && !isLocalPlayer)
		{
			Debug.Log("Cmd_MovementInputs isServer");
			Inputs inputs;
			inputs.forward = Mathf.Clamp((float)forward / 127f, -1f, 1f);
			inputs.sides = Mathf.Clamp((float)sides / 127f, -1f, 1f);
			inputs.vertical = Mathf.Clamp((float)vertical / 127f, -1f, 1f);
			inputs.pitch = 0f;
			inputs.yaw = 0f;
			inputs.mouse = false;
			inputs.sprint = sprint;
			inputs.crouch = crouch;
			inputs.timeStamp = timeStamp;
			_inputsList.Add(inputs);
		}
	}

	#endregion

	#region Virtuals

	// Next virtual functions can be changed in inherited class for custom movement and rotation mechanics
	// So it would be possible to control for example humanoid or vehicle from one script just by changing controlled pawn

	public virtual void GetInputs(ref Inputs inputs)
	{
		// Don't use one frame events in this part
		// It would be processed incorrectly 
		inputs.sides = RoundToLargest(Input.GetAxis("Horizontal"));
		inputs.forward = RoundToLargest(Input.GetAxis("Vertical"));
		inputs.sprint = Input.GetButton("Sprint");
		inputs.crouch = Input.GetButton("Crouch");
		inputs.mouse = mouseSteer;

		if (mouseSteer)
		{
			inputs.pitch = Input.GetAxis("Mouse X") * mouseSensitivity * Time.fixedDeltaTime / Time.deltaTime;
			inputs.yaw = -Input.GetAxis("Mouse Y") * mouseSensitivity * Time.fixedDeltaTime / Time.deltaTime;
		}
		else
		{
			inputs.pitch = Input.GetAxis("Pitch") * rotateSpeed * Time.fixedDeltaTime / Time.deltaTime;
			inputs.yaw = -Input.GetAxis("Yaw") * rotateSpeed * Time.fixedDeltaTime / Time.deltaTime;
		}

		float verticalTarget = -1;
		if (_isGrounded)
		{
			if (Input.GetButton("Jump"))
				_jump = true;

			verticalTarget = 0;
			inputs.vertical = 0;
		}

		if (_jump)
		{
			verticalTarget = 1;

			if (inputs.vertical >= 0.9f)
				_jump = false;
		}

		inputs.vertical = Mathf.Lerp(inputs.vertical, verticalTarget, 10f * Time.deltaTime);
	}

	public virtual void UpdatePosition(Vector3 newPosition)
	{
		if (Vector3.Distance(newPosition, transform.position) > _snapDistance)
			transform.position = newPosition;
		else
			characterController.Move(newPosition - transform.position);
	}

	public virtual void UpdateRotation(Quaternion newRotation)
	{
		transform.rotation = Quaternion.Euler(0, newRotation.eulerAngles.y, 0);
	}

	public virtual void UpdateMouse(bool sprinting) { }

	public virtual void UpdateSprinting(bool sprinting) { }

	public virtual void UpdateCrouch(bool crouch) { }

	public virtual Vector3 Move(Inputs inputs, Results current)
	{
		transform.position = current.position;
		float speed = 4f;
		if (current.crouching)
			speed = 2f;

		if (current.sprinting)
			speed = 6f;

		if (inputs.vertical > 0)
			_verticalSpeed = inputs.vertical * _jumpHeight;
		else
			_verticalSpeed = inputs.vertical * Physics.gravity.magnitude;

		characterController.Move(transform.TransformDirection((Vector3.ClampMagnitude(new Vector3(inputs.sides, 0, inputs.forward), 1) * speed) + new Vector3(0, _verticalSpeed, 0)) * Time.fixedDeltaTime);
		return transform.position;
	}

	public virtual bool Mouse(Inputs inputs, Results current)
	{
		return inputs.mouse;
	}

	public virtual bool Sprint(Inputs inputs, Results current)
	{
		return inputs.sprint;
	}

	public virtual bool Crouch(Inputs inputs, Results current)
	{
		return inputs.crouch;
	}

	public virtual Quaternion Rotate(Inputs inputs, Results current)
	{
		transform.rotation = current.rotation;

		//if (mouseSteer)
		//{
		float mHor = transform.eulerAngles.y + inputs.pitch * Time.fixedDeltaTime;
		float mVert = transform.eulerAngles.x + inputs.yaw * Time.fixedDeltaTime;

		if (mVert > 180f)
			mVert -= 360f;

		mVert = Mathf.Clamp(mVert, -verticalMouseLookLimit * 0.5f, verticalMouseLookLimit * 0.5f);

		transform.rotation = Quaternion.Euler(mVert, mHor, 0f);
		//}
		//else
		//{
		//	//transform.rotation = Quaternion.Euler(0, inputs.turn * keyboardTurnSpeed, 0);
		//	if (inputs.turn != 0f)
		//	{
		//		Debug.LogFormat("Rotate {0}", transform.rotation);
		//		transform.Rotate(0, inputs.turn * rotateSpeed, 0, Space.World);
		//	}
		//}

		return transform.rotation;
	}

	#endregion

	// Updating Clients with server states
	void RecieveResults(SyncResults previousValue, SyncResults syncResults)
	{
		if (!isClient) return;
		Debug.Log("RecieveResults");

		// Converting values back

		//if (mouseSteer)
		//	results.rotation = Quaternion.Euler((float)syncResults.pitch / 182, (float)syncResults.yaw / 182, 0);
		//else
		//	results.rotation = Quaternion.Euler(0, syncResults.turn * rotateSpeed, 0);

		rcvdResults.rotation = Quaternion.Euler((float)syncResults.pitch / 182, (float)syncResults.yaw / 182, 0);

		rcvdResults.position = syncResults.position;
		rcvdResults.mousing = syncResults.mousing;
		rcvdResults.sprinting = syncResults.sprinting;
		rcvdResults.crouching = syncResults.crouching;
		rcvdResults.timeStamp = syncResults.timeStamp;

		// Discard out of order results
		if (rcvdResults.timeStamp <= _lastTimeStamp)
			return;

		_lastTimeStamp = rcvdResults.timeStamp;

		// Non-owner client
		if (!isLocalPlayer && !isServer)
		{
			// Adding results to the results list so they can be used in interpolation process
			rcvdResults.timeStamp = Time.time;
			_resultsList.Add(rcvdResults);
		}

		// Owner client
		// Server client reconciliation process should be executed in order to sync client's
		// rotation and position with server values but do it without jittering
		if (isLocalPlayer && !isServer)
		{
			// Update client's position and rotation with ones from server 
			_results.rotation = rcvdResults.rotation;
			_results.position = rcvdResults.position;
			int foundIndex = -1;

			// Search recieved time stamp in client's inputs list
			for (int index = 0; index < _inputsList.Count; index++)
			{
				// If time stamp found run through all inputs starting from needed time stamp 
				if (_inputsList[index].timeStamp > rcvdResults.timeStamp)
				{
					foundIndex = index;
					break;
				}
			}

			if (foundIndex == -1)
			{
				// Clear Inputs list if no needed records found 
				while (_inputsList.Count != 0)
					_inputsList.RemoveAt(0);

				return;
			}

			// Replay recorded inputs
			for (int subIndex = foundIndex; subIndex < _inputsList.Count; subIndex++)
			{
				_results.rotation = Rotate(_inputsList[subIndex], _results);
				_results.crouching = Crouch(_inputsList[subIndex], _results);
				_results.sprinting = Sprint(_inputsList[subIndex], _results);

				_results.position = Move(_inputsList[subIndex], _results);
			}

			// Remove all inputs before time stamp
			int targetCount = _inputsList.Count - foundIndex;
			while (_inputsList.Count > targetCount)
				_inputsList.RemoveAt(0);
		}
	}
}
