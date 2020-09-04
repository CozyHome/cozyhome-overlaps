using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace com.chs.final
{
    public class CharacterCapsuleCollider : CharacterCollider
    {
        /// <summary>
        /// The CapsuleCollider object we'll be using to store information relevant to the
        /// physics system
        /// </summary>
        [SerializeField] CapsuleCollider Capsule_C;

        /// <summary>
        /// A Trace offset constant used to aid Unity's 
        /// physics query in catching trace hits 
        /// directly infront of the collider
        /// </summary>
        public const float TRACEBIAS = 0.001F;

        public override int Overlap(Vector3 position,
            Quaternion orientation,
            LayerMask validOverlapMask,
            QueryTriggerInteraction interactionType,
            Collider[] tmpColliderBuffer,
            float inflate = 0)
        {
            //offset 
            position += orientation * Capsule_C.center;

            // get distance to endpoints of capsule's line segment from the center of the line
            Vector3 radiiHeight = (orientation * Vector3.up) * (Capsule_C.height * .5F - Capsule_C.radius + inflate);

            // call physics overlap
            int nbCollidersOverlapped = Physics.OverlapCapsuleNonAlloc(
                position + radiiHeight,
                position - radiiHeight,
                Capsule_C.radius + inflate,
                tmpColliderBuffer,
                validOverlapMask,
                interactionType);

            // filters wont really need to be declared concretely as overlaps will require 
            // contextual resolutions on their own most of the time
            return nbCollidersOverlapped;
        }

        /// <summary>
        /// 1. The written implementation of our CharacterCollider blueprint method. <br/> 
        /// 2. Trace() casts the CapsuleCollider across the Physics worldspace and writes to the provided hit buffer in AMBIGUOUS order. <br/>
        /// 3. This method returns the amount of hits found but does not sort them.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="direction"></param>
        /// <param name="traceDistance"></param>
        /// <param name="orientation"></param>
        /// <param name="validHitMask"></param>
        /// <param name="interactionType"></param>
        /// <param name="tmpBuffer"></param>
        /// <returns></returns>
        public override int Trace(Vector3 position,
        Vector3 direction,
        float traceDistance,
        Quaternion orientation,
        LayerMask validHitMask,
        QueryTriggerInteraction interactionType,
        RaycastHit[] tmpBuffer,
        float inflate = 0F)
        {
            position += orientation * Capsule_C.center;
            position -= direction * TRACEBIAS;

            Vector3 capsuleSegmentLength = (orientation * Vector3.up) * (inflate + Capsule_C.height * 0.5F - Capsule_C.radius);

            int tracedCollidersCount = Physics.CapsuleCastNonAlloc(
            position + capsuleSegmentLength,
            position - capsuleSegmentLength,
            Capsule_C.radius + inflate,
            direction,
            tmpBuffer,
            traceDistance + TRACEBIAS,
            validHitMask,
            interactionType);

            return tracedCollidersCount;
        }

        /// <summary>
        /// The implementation of the blueprint method that returns a reference to our CapsuleCollider component
        /// </summary>
        /// <returns></returns>
        public override Collider Collider() { return Capsule_C; }
    }
}
