using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace com.chs.final
{
    public class CharacterBoxCollider : CharacterCollider
    {
        /* For the sake of not going insane, I decided to focus heavy documentation on the Capsule implementation for the time being. 
           Please check that out if you're interested in seeing commented code. */

        [SerializeField] BoxCollider Box_C;

        public const float TRACEBIAS = 0.001F;

        public override int Overlap(Vector3 position,
            Quaternion orientation,
            LayerMask validOverlapMask,
            QueryTriggerInteraction interactionType,
            Collider[] tmpColliderBuffer,
            float inflate = 0)
        {
            position += orientation * Box_C.center;

            Vector3 halfExtents = Box_C.size / 2F;
            for (int i = 0; i < 3; i++)
                halfExtents[i] += inflate;

            int nbCollidersOverlapped = Physics.OverlapBoxNonAlloc(
                position,
                halfExtents,
                tmpColliderBuffer,
                orientation,
                validOverlapMask,
                interactionType);

            return nbCollidersOverlapped;
        }

        public override int Trace(Vector3 position,
            Vector3 direction,
            float traceDistance,
            Quaternion orientation,
            LayerMask validHitMask,
            QueryTriggerInteraction interactionType,
            RaycastHit[] tmpBuffer,
            float inflate = 0F)
        {
            position += orientation * Box_C.center;
            position -= direction * TRACEBIAS;

            Vector3 halfExtents = Box_C.size / 2F;

            //feel free to cache these values instead of computing every trace.
            /*
             * scaling colliders per trace will be removed in a later commit. I figured it's faster just to set our desired
             * collider sizes at the start, and size the child mesh separately.
             * Vector3 scaledExtents = Box_C.size / 2F;
             * for (int i = 0; i < 3; i++)
             * scaledExtents[i] *= transform.localScale[i];
             */

            for (int i = 0; i < 3; i++)
                halfExtents[i] += inflate;

            int tracedCollidersCount = Physics.BoxCastNonAlloc
            (position,
            halfExtents,
            direction,
            tmpBuffer,
            orientation,
            traceDistance + TRACEBIAS,
            validHitMask,
            interactionType);

            return tracedCollidersCount;
        }

        public override Collider Collider()
        {
            return Box_C;
        }
    }

}
