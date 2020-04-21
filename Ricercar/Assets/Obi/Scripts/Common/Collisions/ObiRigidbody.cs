﻿using UnityEngine;
using System;
using System.Collections;

namespace Obi{

	/**
	 * Small helper class that lets you specify Obi-only properties for rigidbodies.
	 */

	[ExecuteInEditMode]
	[RequireComponent(typeof(Rigidbody))]
	public class ObiRigidbody : ObiRigidbodyBase
	{
		private Rigidbody unityRigidbody;

		public override void Awake(){
			unityRigidbody = GetComponent<Rigidbody>();
			base.Awake();
		}

		public override void UpdateIfNeeded(){

			velocity = unityRigidbody.velocity;
			angularVelocity = unityRigidbody.angularVelocity;

			adaptor.Set(unityRigidbody,kinematicForParticles);
			Oni.UpdateRigidbody(OniRigidbody,ref adaptor);

		}

		/**
		 * Reads velocities back from the solver.
		 */
		public override void UpdateVelocities()
        {

			// kinematic rigidbodies are passed to Obi with zero velocity, so we must ignore the new velocities calculated by the solver:
			if (Application.isPlaying && (unityRigidbody.isKinematic || !kinematicForParticles))
            {

                Oni.GetRigidbodyVelocity(OniRigidbody,ref oniVelocities);
                unityRigidbody.velocity += oniVelocities.linearVelocity - velocity;
                unityRigidbody.angularVelocity += oniVelocities.angularVelocity - angularVelocity;
			}
		}
	}
}

