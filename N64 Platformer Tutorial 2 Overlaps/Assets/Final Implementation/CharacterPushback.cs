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
            // ResolvePushbacks();
            ResolvePushbacksAndStore();
            transform.position = internalPosition;
            transform.rotation = internalRotation;
        }

        void ResolvePushbacks()
        {
            Vector3 resolvedPosition = internalPosition;

            if (CharacterCol.IterativePushback(3,
                ref resolvedPosition,
                internalRotation,
                internalColliderOverlaps,
                validOverlapMask,
                QueryTriggerInteraction.Ignore,
                0F))
            {
                internalPosition = resolvedPosition;
            }
        }

        void ResolvePushbacksAndStore()
        {
            Vector3 resolvedPosition = internalPosition;

            int nbPushbacksRegistered = CharacterCol.StoreIterativePushback(
                out bool wasResolved,
                3, // attempt resolve three times per fixed update
                ref resolvedPosition, // pass in position to be written to
                internalRotation, // pass in rotation
                internalColliderOverlaps, // pass in collider buffer
                internalOverlapHits,
                validOverlapMask, // pass in overlap layermask
                QueryTriggerInteraction.Ignore, // pass in query type
                0F); // pass in inflate

            if (wasResolved)
            {
                internalPosition = resolvedPosition;
            }

            if (nbPushbacksRegistered > 0)
            {
                for (int i = nbPushbacksRegistered - 1; i >= 0; i--)
                {
                    Debug.DrawRay(internalPosition, internalOverlapHits[i].normal, Color.red);
                }
            }

        }
    }
}
