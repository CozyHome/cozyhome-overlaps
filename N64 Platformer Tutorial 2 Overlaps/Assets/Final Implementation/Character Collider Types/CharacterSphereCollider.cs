using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace com.chs.final
{
    /* For the sake of not going insane, I decided to focus heavy documentation on the Capsule implementation for the time being. 
       Please check that out if you're interested in seeing commented code. */

    public class CharacterSphereCollider : CharacterCollider
    {
        [SerializeField] SphereCollider Sphere_C;

        public const float TRACEBIAS = 0.001F;

        public override int Overlap(Vector3 position,
            Quaternion orientation,
            LayerMask validOverlapMask,
            QueryTriggerInteraction interactionType,
            Collider[] tmpColliderBuffer,
            float inflate = 0)
        {
            position += orientation * Sphere_C.center;

            int nbCollidersOverlapped =
                Physics.OverlapSphereNonAlloc(position, Sphere_C.radius + inflate, tmpColliderBuffer, validOverlapMask, interactionType);

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
            position += orientation * Sphere_C.center;
            position -= direction * TRACEBIAS;

            /*
             * scaling colliders per trace will be removed in a later commit. I figured it's faster just to set our desired
             * collider sizes at the start, and size the child mesh separately.
            float biggestScale = 0F;
            for (int i = 0; i < 3; i++)
                if (biggestScale < transform.localScale[i])
                    biggestScale = transform.localScale[i];
            */

            int tracedCollidersCount = Physics.SphereCastNonAlloc
            (position,
            Sphere_C.radius + inflate /*biggestScale*/,
            direction,
            tmpBuffer,
            traceDistance + TRACEBIAS,
            validHitMask,
            interactionType);

            return tracedCollidersCount;
        }

        public override Collider Collider()
        {
            return Sphere_C;
        }
    }
}
