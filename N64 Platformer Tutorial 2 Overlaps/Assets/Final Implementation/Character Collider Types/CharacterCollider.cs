using UnityEngine;

namespace com.chs.final
{
    /// <summary>
    /// A custom MonoBehaviour script that combines the Collider component system with tracing and filter methods
    /// to easily allow for the implmementation of casting primitives in the physics worldspace.
    /// </summary>
    public abstract class CharacterCollider : MonoBehaviour
    {
        const int MaxOverlapResolutions = 8;

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
        /// <br/>I suggest that you write your own filters and pushback method for customizability.
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
        public bool IterativePushback(int steps, // maximum resolutions during an iterative loop
            ref Vector3 position, // the position to modify, don't pass your actual position in here, use a locally defined one to then set to
            Quaternion orientation, // the orientation of your primitive
            Collider[] tmpColliderBuffer, // the collider buffer to write to for our overlaps
            LayerMask validOverlapsMask, // the layermask filter for our overlaps
            QueryTriggerInteraction interactionType, // the query interaction type 
            float inflate = 0F) // our optional inflate var
        {
            steps = Mathf.Min(steps, MaxOverlapResolutions); // cap steps to 5 as to not royally fuck performance
            bool lastStepResolved = false;

            while (steps-- > 0 && !lastStepResolved) // only continue loop if we've not resolved yet
                lastStepResolved = Pushback(ref position, // pass in our position to write to
                    orientation, // pass orientation
                    tmpColliderBuffer, // pass buffer
                    validOverlapsMask, // layermask pass
                    interactionType, // query interaction pass
                    inflate); // inflate pass

            // return if we've resolved our last overlap. 
            // this is helpful in cases where warping occurs and the primitive is stuck between two colliders continuously.
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
        public int StoreIterativePushback(
            out bool lastStepResolved,
            int steps,
            ref Vector3 position,
            Quaternion orientation,
            Collider[] tmpColliderBuffer,
            OverlapHit[] tmpOverlapBuffer,
            LayerMask validOverlapsMask,
            QueryTriggerInteraction interactionType,
            float inflate = 0F)
        {
            steps = Mathf.Min(steps, MaxOverlapResolutions);
            int nbOverlapsWritten = 0;
            lastStepResolved = false;

            while (steps-- > 0 && !lastStepResolved)
                lastStepResolved = StorePushback(ref nbOverlapsWritten,
                    ref position,
                    orientation,
                    tmpColliderBuffer,
                    tmpOverlapBuffer,
                    validOverlapsMask,
                    interactionType,
                    inflate);

            return nbOverlapsWritten;
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
        private bool Pushback(
            ref Vector3 position,
            Quaternion orientation,
            Collider[] tmpColliderBuffer,
            LayerMask validOverlapsMask,
            QueryTriggerInteraction interactionType,
            float inflate)
        {
            // Cache self for filters
            Collider self = Collider();
            int nbColliderOverlaps = Overlap(position,
                    orientation,
                    validOverlapsMask,
                    interactionType,
                    tmpColliderBuffer,
                    inflate);

            // Filter out self
            OverlapFilters.FilterSelf(ref nbColliderOverlaps,
                tmpColliderBuffer,
                self);

            // Check if overlap detection returns nothing or not
            bool isResolved = nbColliderOverlaps == 0;

            if (!isResolved) // attempt solve:
            {
                for (int j = nbColliderOverlaps - 1; j >= 0; j--) // loop through queried colliders and resolve the first valid penetration
                {
                    Collider overlapCollider = tmpColliderBuffer[j]; // reference queried collider
                    Transform overlapTransform = tmpColliderBuffer[j].transform; // reference queried transform

                    if (Physics.ComputePenetration(
                        self, // pass self
                        position, // pass position
                        orientation, // pass orientation
                        overlapCollider, // pass overlapped collider
                        overlapTransform.position, // pass its position
                        overlapTransform.rotation, // pass its rotation
                        out Vector3 overlapNormal, // output penetration normal
                        out float overlapDistance)) // output distance 
                    {
                        position += overlapNormal * (overlapDistance + 0.01F); // push position out of overlap
                        break;
                    }
                }
            }

            return isResolved;
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
            // Cache self
            Collider self = Collider();

            int nbColliderOverlaps = Overlap(
                    position, // pass position
                    orientation, // pass orientation
                    validOverlapsMask, // pass layermask
                    interactionType, // pass interaction type
                    tmpColliderBuffer, // pass collider buffer
                    inflate); // pass inflate

            // filter out self
            OverlapFilters.FilterSelf(ref nbColliderOverlaps, // pass number of overlaps
                tmpColliderBuffer, // pass buffer
                self); // pass self to filter

            // if filtered overlaps exceed 0, solve iteration
            bool overlapsDetected = nbColliderOverlaps > 0;

            if (overlapsDetected) // attempt solve:
            {
                for (int j = 0; j < nbColliderOverlaps; j++) // loop through queried colliders and resolve the first valid penetration
                {
                    Collider overlapCollider = tmpColliderBuffer[j];
                    Transform overlapTransform = overlapCollider.transform;

                    if (Physics.ComputePenetration(self,
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

            return !overlapsDetected;
        }
    }

    /// <summary>
    /// Overlap Filters have been moved here to clean the CharacterCollider class.
    /// </summary>
    public static class OverlapFilters
    {
        public static void FilterSelf(ref int overlappedCollidersCount,
            Collider[] tmpColliderBuffer,
            Collider self)
        {
            int tmpIndex = overlappedCollidersCount;

            while (tmpIndex-- > 0)
            {
                if (tmpColliderBuffer[tmpIndex].Equals(self))
                {
                    overlappedCollidersCount--;
                    if (tmpIndex < overlappedCollidersCount)
                        tmpColliderBuffer[tmpIndex] = tmpColliderBuffer[overlappedCollidersCount];
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
        public static void FindClosestFilterInvalids(
        ref int tracedColliderCount,
        out int closestIndex,
        RaycastHit[] tmpBuffer,
        Collider self,
        float traceBias = 0F)
        {
            int tmpIndex = tracedColliderCount; // start our array accessor at the size and decrement in loop call to prevent any mistakes causing a crash 
            float closestTrace = Mathf.Infinity; // cache a closestTrace distance float to use obtain the closest index
            closestIndex = -1; // assume negative one to signify nothing was hit

            while (tmpIndex-- > 0)
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
        public static void FindClosest(
            ref int tracedColliderCount,
            out int closestIndex,
            RaycastHit[] tmpBuffer,
            Collider self,
            float traceBias = 0F)
        {
            int tmpIndex = tracedColliderCount;
            float closestTrace = Mathf.Infinity;
            closestIndex = -1;

            while (tmpIndex-- > 0)
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
        public static void FindFurthestFilterInvalids(
        ref int tracedColliderCount,
        out int furthestIndex,
        RaycastHit[] tmpBuffer,
        Collider self,
        float traceBias = 0F)
        {
            int tmpIndex = tracedColliderCount;
            float furthestTrace = 0F;
            furthestIndex = -1;

            while (tmpIndex-- > 0)
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
            int tmpIndex = tracedColliderCount;
            float furthestTrace = 0F;
            furthestIndex = -1;

            while (tmpIndex-- > 0)
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

            }
        }

    }
}

