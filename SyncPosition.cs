/* SyncPosition.cs - Eddie Cameron
 * ----------------------------
 * Will sync the position and rotation of two objects across a network. Much smoother than just sending transform information in the network view.
 * Needs some improvement, perhaps with better interpolation in case of dropped packets, and scale support. Better support for an object having
 * more than one master would be nice too. One day...
 * But still saves time/hassle/jerky movement
 * ----------------------------
 */ 

using UnityEngine;
using System.Collections;

[RequireComponent( typeof( NetworkView ) )]
public class SyncPosition : MonoBehaviour 
{
	public bool isMaster;		// is this the master object?	(master controls all movement)
	
	float nextSyncTime = -1f;
	float sendRate;
	
	Vector3 targPos;
	Quaternion targRot;
	
	Vector3 lastPos;
	Quaternion lastRot;
	
	public float damping = 10f;	// how fast the slave will catch up to the master. Less is smoother but won't be able to keep up with fast moving masters.
	
	void Awake()
	{
		sendRate = Network.sendRate;
	}
	
	void Update()
	{
		if ( Network.connections.Length > 0 )
		{
			if ( isMaster )
			{
				// sends pos/rot each network frame
				if ( Time.time > nextSyncTime )
				{
					networkView.RPC( "UpdatePosition", RPCMode.Others, transform.position, transform.rotation );
					nextSyncTime = Time.time + 1 / sendRate;
				}
			}
			else
			{
				// will try to guess next target position between network frames. 
				// for objects that change velocity slowly, you could reduce sendrate by quite a lot to save bandwidth
				Vector3 posChange = transform.position - lastPos;
				Quaternion rotChange = Quaternion.FromToRotation(  lastRot.eulerAngles, transform.rotation.eulerAngles );
				
				lastPos = transform.position;
				lastRot = transform.rotation;
				
				targPos += posChange;
				targRot *= rotChange;
				
				transform.position = Vector3.Lerp ( transform.position, targPos, Time.deltaTime * damping );
				transform.rotation = Quaternion.Lerp ( transform.rotation, targRot, Time.deltaTime * damping );
			}
		}
	}
	
	[RPC]
	void UpdatePosition( Vector3 newPos, Quaternion newRot )
	{
		targPos = newPos;
		targRot = newRot;
	}	
}
