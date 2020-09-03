using UnityEngine;
namespace com.chs.final
{
    /// <summary>
    /// OverlapHit emulates Unity's RaycastHit and offers
    /// information on a discovered overlap.
    /// </summary>
    public struct OverlapHit
    {
        /// <summary>
        /// Implemented constructor support
        /// </summary>
        /// <param name="normal"></param>
        /// <param name="distance"></param>
        /// <param name="collider"></param>
        public OverlapHit(Vector3 normal, float distance, Collider collider)
        {
            this.normal = normal;
            this.distance = distance;
            this.collider = collider;
        }

        /// <summary>
        /// The Collider object discovered during the overlap query.
        /// </summary>
        public Collider collider;

        /// <summary>
        /// The closest penetration distance along the normal to resolve our query collider from our discovered collider.
        /// </summary>
        public float distance;

        /// <summary>
        /// The closest normal discovered to resolve our query collider from our discovered collider.
        /// </summary>
        public Vector3 normal;
    }
}
