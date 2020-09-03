using UnityEngine;

namespace com.chs.final
{
    /// <summary>
    /// A custom MonoBehaviour script that combines the Collider component system with tracing and filter methods
    /// to easily allow for the implmementation of casting primitives in the physics worldspace.
    /// </summary>
    /// 

    public abstract class CharacterCollider : MonoBehaviour
    {
        const int MaxOverlapResolutions = 5;

        /// <summary>
        /// CharacterCollider.Overlap is a blueprint method designed to be overridden by your
        /// own custom CharacterCollider implementations.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="orientation"></param>
        /// <param name="validOverlapMask"></param>
        /// <param name="interactionType"></param>
        /// <param name="tmpColliderBuffer"></param>
        /// <param name="inflate"></param>
        /// <returns></returns>
        public abstract int Overlap(Vector3 position,
            Quaternion orientation,
            LayerMask validOverlapMask,
            QueryTriggerInteraction interactionType,
            Collider[] tmpColliderBuffer,
            float inflate = 0F);

        /// <summary>
        /// CharacterCollider.Trace is a blueprint method designed to be overridden by your own custom CharacterCollider implementations.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="direction"></param>
        /// <param name="traceDistance"></param>
        /// <param name="orientation"></param>
        /// <param name="validHitMask"></param>
        /// <param name="interactionType"></param>
        /// <param name="tmpBuffer"></param>
        /// <returns></returns>
        public abstract int Trace(Vector3 position,
            Vector3 direction,
            float traceDistance,
            Quaternion orientation,
            LayerMask validHitMask,
            QueryTriggerInteraction interactionType,
            RaycastHit[] tmpBuffer,
            float inflate = 0F);

        /// <summary>
        /// The blueprint method that your implementation must override, as it returns access to the character's physics collider.
        /// </summary>
        /// <returns></returns>
        public abstract Collider Collider(); // reference accessor to use in Filter comparisons

        /// <summary>
        /// The generic method used to iteratively resolve an overlap between this CharacterCollider and other colliders.
        /// </summary>
        /// <param name="steps"></param>
        /// <param name="position"></param>
        /// <param name="lastResolutionPosition"></param>
        /// <param name="orientation"></param>
        /// <param name="tmpColliderBuffer"></param>
        /// <param name="validOverlapsMask"></param>
        /// <param name="interactionType"></param>
        /// <param name="inflate"></param>
        /// <returns></returns>
        public bool IterativePushback(int steps,
            Vector3 position,
            out Vector3 lastResolutionPosition,
            Quaternion orientation,
            Collider[] tmpColliderBuffer,
            LayerMask validOverlapsMask,
            QueryTriggerInteraction interactionType,
            float inflate = 0F)
        {
            steps = Mathf.Min(steps, MaxOverlapResolutions);
            bool lastStepResolved = false;
            Vector3 naivePosition = position;

            while (steps-- >= 0)
                lastStepResolved = Pushback(ref naivePosition,
                    orientation,
                    tmpColliderBuffer,
                    validOverlapsMask,
                    QueryTriggerInteraction.Ignore,
                    inflate);

            lastResolutionPosition = naivePosition;

            return lastStepResolved;
        }

        /// <summary>
        /// An implementation of the IterativePushback() loop that will store all overlaps into an OverlapHit buffer provided
        /// </summary>
        /// <param name="steps"></param>
        /// <param name="position"></param>
        /// <param name="lastResolutionPosition"></param>
        /// <param name="orientation"></param>
        /// <param name="tmpColliderBuffer"></param>
        /// <param name="tmpOverlapBuffer"></param>
        /// <param name="validOverlapsMask"></param>
        /// <param name="interactionType"></param>
        /// <param name="inflate"></param>
        /// <returns></returns>
        public bool StoreIterativePushback(
            int steps,
            Vector3 position,
            out Vector3 lastResolutionPosition,
            Quaternion orientation,
            Collider[] tmpColliderBuffer,
            OverlapHit[] tmpOverlapBuffer,
            LayerMask validOverlapsMask,
            QueryTriggerInteraction interactionType,
            float inflate = 0F)
        {
            steps = Mathf.Min(steps, MaxOverlapResolutions);
            bool lastStepResolved = false;
            int nbOverlapsWritten = 0;
            Vector3 naivePosition = position;

            while (steps-- >= 0)
                lastStepResolved = StorePushback(
                    ref nbOverlapsWritten,
                    ref naivePosition,
                    orientation,
                    tmpColliderBuffer,
                    tmpOverlapBuffer,
                    validOverlapsMask,
                    QueryTriggerInteraction.Ignore,
                    inflate);

            lastResolutionPosition = naivePosition;

            return lastStepResolved;
        }

        /// <summary>
        /// An implementation of the Pushback method that will store an overlap's resolution details.
        /// </summary>
        /// <param name="overlapHitCount"></param>
        /// <param name="position"></param>
        /// <param name="orientation"></param>
        /// <param name="tmpColliderBuffer"></param>
        /// <param name="tmpOverlapBuffer"></param>
        /// <param name="validOverlapsMask"></param>
        /// <param name="interactionType"></param>
        /// <param name="inflate"></param>
        /// <returns></returns>
        private bool StorePushback(
            ref int overlapHitCount,
            ref Vector3 position,
            Quaternion orientation,
            Collider[] tmpColliderBuffer,
            OverlapHit[] tmpOverlapBuffer,
            LayerMask validOverlapsMask,
            QueryTriggerInteraction interactionType,
            float inflate)
        {
            Collider self = Collider();

            int nbColliderOverlaps = Overlap(
                    position,
                    orientation,
                    validOverlapsMask,
                    interactionType,
                    tmpColliderBuffer,
                    inflate);

            OverlapFilters.FilterSelf(
                ref nbColliderOverlaps,
                tmpColliderBuffer,
                self);

            bool overlapsDetected = nbColliderOverlaps > 0;

            if (overlapsDetected) // attempt solve:
            {
                for (int j = 0; j < nbColliderOverlaps; j++) // loop through queried colliders and resolve the first valid penetration
                {
                    Collider overlapCollider = tmpColliderBuffer[j];
                    Transform overlapTransform = overlapCollider.transform;

                    if (Physics.ComputePenetration(
                        self,
                        position,
                        orientation,
                        overlapCollider,
                        overlapTransform.position,
                        overlapTransform.rotation,
                        out Vector3 overlapNormal,
                        out float overlapDistance))
                    {
                        position += overlapNormal * (overlapDistance + 0.01F);

                        if (overlapHitCount < tmpOverlapBuffer.Length)
                            tmpOverlapBuffer[overlapHitCount++] = new OverlapHit(overlapNormal, overlapDistance, overlapCollider);

                        break;
                    }
                }
            }

            return overlapsDetected;
        }

        /// <summary>
        /// The step called every iteration in the IterativePushback method call.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="orientation"></param>
        /// <param name="tmpColliderBuffer"></param>
        /// <param name="validOverlapsMask"></param>
        /// <param name="interactionType"></param>
        /// <param name="inflate"></param>
        /// <returns></returns>
        private bool Pushback(ref Vector3 position,
            Quaternion orientation,
            Collider[] tmpColliderBuffer,
            LayerMask validOverlapsMask,
            QueryTriggerInteraction interactionType,
            float inflate)
        {
            Collider self = Collider();

            int nbColliderOverlaps = Overlap(
                    position,
                    orientation,
                    validOverlapsMask,
                    interactionType,
                    tmpColliderBuffer,
                    inflate);

            OverlapFilters.FilterSelf(
                ref nbColliderOverlaps,
                tmpColliderBuffer,
                self);

            bool overlapsDetected = nbColliderOverlaps > 0;

            if (overlapsDetected) // attempt solve:
            {
                for (int j = 0; j < nbColliderOverlaps; j++) // loop through queried colliders and resolve the first valid penetration
                {
                    Transform overlapTransform = tmpColliderBuffer[j].transform;

                    if (Physics.ComputePenetration(
                        self,
                        position,
                        orientation,
                        tmpColliderBuffer[j],
                        overlapTransform.position,
                        overlapTransform.rotation,
                        out Vector3 overlapNormal,
                        out float overlapDistance))
                    {
                        position += overlapNormal * (overlapDistance + 0.01F);

                        break;
                    }
                }
            }

            return overlapsDetected;
        }
    }

    public static class OverlapFilters
    {
        public static void FilterSelf(ref int overlappedCollidersCount,
            Collider[] tmpColliderBuffer,
            Collider self)
        {
            for (int i = overlappedCollidersCount - 1; i >= 0; i--)
            {
                if (tmpColliderBuffer[i].Equals(self))
                {
                    overlappedCollidersCount--;
                    if (i < overlappedCollidersCount)
                        tmpColliderBuffer[i] = tmpColliderBuffer[overlappedCollidersCount];
                }
                else
                    continue;
            }
        }
    }

    /// <summary>
    /// Trace Filters have been moved here to clean the CharacterCollider class.
    /// </summary>
    public static class TraceFilters
    {

        /// <summary>
        /// FindClosestFilterInvalids() will return the index of the closest RaycastHit during the physics query. <br/>
        /// This method can also filter out any hits that you deem invalid.
        /// </summary>
        /// <param name="tracedColliderCount"></param>
        /// <param name="tmpBuffer"></param>
        /// <param name="closestIndex"></param>
        /// <param name="self"></param>
        /// <param name="traceBias"></param>
        public static void FindClosestFilterInvalids(ref int tracedColliderCount,
        RaycastHit[] tmpBuffer,
        out int closestIndex,
        Collider self,
        float traceBias = 0F)
        {
            int tmpIndex = tracedColliderCount - 1; // start our array accessor at the last element 
            float closestTrace = Mathf.Infinity; // cache a closestTrace distance float to use obtain the closest index
            closestIndex = -1; // assume negative one to signify nothing was hit

            while (tmpIndex >= 0)
            {
                // Subtract our trace bias to not incorrectly report hit distance:
                tmpBuffer[tmpIndex].distance -= traceBias;

                // Cache a temporary hit reference to avoid excessive array access:
                RaycastHit tmpHit = tmpBuffer[tmpIndex];
                float traceLength = tmpHit.distance;

                bool customValidHitCheck = true;

                // Valid hit branch:
                if (traceLength > 0F && // if trace length is not negative (hit is further than us along direction line)
                    tmpHit.collider != self && // if trace hit is not us
                    customValidHitCheck) // override the filter method and boolean to allow for custom checks (be creative :> )
                {
                    if (traceLength < closestTrace)
                    {
                        closestIndex = tmpIndex;
                        closestTrace = traceLength;
                    }
                }
                else // Invalid hit branch:
                {
                    if (tmpIndex < --tracedColliderCount)
                    {
                        tmpBuffer[tmpIndex] = tmpBuffer[tracedColliderCount];
                    }
                }

                tmpIndex--; // decrement (make sure not to remove this, you'll end up causing your unity to crash)
            }
        }

        /// <summary>
        /// FindClosest() will return the index of the closest RaycastHit during the physics query.
        /// </summary>
        /// <param name="tracedColliderCount"></param>
        /// <param name="tmpBuffer"></param>
        /// <param name="closestIndex"></param>
        /// <param name="self"></param>
        /// <param name="traceBias"></param>
        public static void FindClosest(ref int tracedColliderCount,
            RaycastHit[] tmpBuffer,
            out int closestIndex,
            Collider self,
            float traceBias = 0F)
        {
            int tmpIndex = tracedColliderCount - 1;
            float closestTrace = Mathf.Infinity;
            closestIndex = -1;

            while (tmpIndex >= 0)
            {
                tmpBuffer[tmpIndex].distance -= traceBias;
                RaycastHit tmpHit = tmpBuffer[tmpIndex];
                float traceLength = tmpHit.distance;

                if (traceLength > 0F && tmpHit.collider != self)
                {
                    if (traceLength < closestTrace)
                    {
                        closestIndex = tmpIndex;
                        closestTrace = traceLength;
                    }
                }
                else
                    tracedColliderCount--;

                tmpIndex--;
            }
        }

        /// <summary>
        /// FindFurthestFilterInvalids() will return the index of the furthest RaycastHit during the physics query. <br/>
        /// This method can also filter out any hits that you deem invalid.
        /// </summary>
        /// <param name="tracedColliderCount"></param>
        /// <param name="tmpBuffer"></param>
        /// <param name="closestIndex"></param>
        /// <param name="self"></param>
        /// <param name="traceBias"></param>
        public static void FindFurthestFilterInvalids(ref int tracedColliderCount,
        RaycastHit[] tmpBuffer,
        out int furthestIndex,
        Collider self,
        float traceBias = 0F)
        {
            int tmpIndex = tracedColliderCount - 1;
            float furthestTrace = 0F;
            furthestIndex = -1;

            while (tmpIndex >= 0)
            {
                tmpBuffer[tmpIndex].distance -= traceBias;
                RaycastHit tmpHit = tmpBuffer[tmpIndex];
                float traceLength = tmpHit.distance;

                if (traceLength > 0F && tmpHit.collider != self)
                {
                    if (traceLength > furthestTrace)
                    {
                        furthestIndex = tmpIndex;
                        furthestTrace = traceLength;
                    }
                }
                else
                {
                    if (tmpIndex < --tracedColliderCount)
                    {
                        tmpBuffer[tmpIndex] = tmpBuffer[tracedColliderCount];
                    }
                }

                tmpIndex--;
            }
        }

        /// <summary>
        /// FindFurthest() will return the index of the furthest RaycastHit during the physics query.
        /// </summary>
        /// <param name="tracedColliderCount"></param>
        /// <param name="tmpBuffer"></param>
        /// <param name="closestIndex"></param>
        /// <param name="self"></param>
        /// <param name="traceBias"></param>
        public static void FindFurthest(ref int tracedColliderCount,
        RaycastHit[] tmpBuffer,
        out int furthestIndex,
        Collider self,
        float traceBias = 0F)
        {
            int tmpIndex = tracedColliderCount - 1;
            float furthestTrace = 0F;
            furthestIndex = -1;

            while (tmpIndex >= 0)
            {
                tmpBuffer[tmpIndex].distance -= traceBias;
                RaycastHit tmpHit = tmpBuffer[tmpIndex];
                float traceLength = tmpHit.distance;

                if (traceLength > 0F && tmpHit.collider != self)
                {
                    if (traceLength > furthestTrace)
                    {
                        furthestIndex = tmpIndex;
                        furthestTrace = traceLength;
                    }
                }
                else
                    tracedColliderCount--;

                tmpIndex--;
            }
        }

    }
}

