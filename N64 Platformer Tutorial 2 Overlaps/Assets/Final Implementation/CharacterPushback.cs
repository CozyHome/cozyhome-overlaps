using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace com.chs.final
{
    public class CharacterPushback : MonoBehaviour
    {
        readonly Collider[] internalColliderOverlaps = new Collider[15];
        readonly OverlapHit[] internalOverlapHits = new OverlapHit[15];

        [SerializeField] LayerMask validOverlapMask;
        [SerializeField] CharacterCollider CharacterCol;

        Vector3 internalPosition;
        Quaternion internalRotation;

        void Start()
        {
            internalPosition = transform.position;
            internalRotation = transform.rotation;
        }

        void FixedUpdate()
        {
            if (CharacterCol.IterativePushback(
                3, // attempt resolve three times per fixed update
                internalPosition, // pass in position to be written to
                out Vector3 resolvedPosition,
                internalRotation, // pass in rotation
                internalColliderOverlaps, // pass in collider buffer
                validOverlapMask, // pass in overlap layermask
                QueryTriggerInteraction.Ignore, // pass in query type
                0F)) // pass in inflate
            {
                internalPosition = resolvedPosition;
            } 

            transform.position = internalPosition;
            transform.rotation = internalRotation;
        }
    }
}
