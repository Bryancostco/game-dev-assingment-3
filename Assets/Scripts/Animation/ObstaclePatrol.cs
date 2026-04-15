using UnityEngine;

namespace DeliveryGame
{
    /// <summary>
    /// Moves an obstacle back and forth between a list of world-space waypoints.
    /// Uses Vector3.MoveTowards so the speed is consistent regardless of waypoint distance.
    /// Attach to any moving obstacle GameObject.
    /// </summary>
    public class ObstaclePatrol : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Waypoints")]
        [SerializeField] private Transform[] _waypoints;

        [Header("Movement")]
        [SerializeField] private float _speed           = 4f;
        [SerializeField] private float _waypointRadius  = 0.2f; // Distance at which the next waypoint is targeted

        [Header("Patrol Mode")]
        [SerializeField] private bool _pingPong = true; // True = reverse at end; False = loop back to start
        #endregion

        #region Private Fields
        private int  _currentIndex   = 0;
        private bool _movingForward  = true;
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            if (_waypoints == null || _waypoints.Length < 2) return;

            // Guard: pausing stops the obstacle
            if (GameManager.Instance?.CurrentState == GameState.Paused) return;

            Transform target = _waypoints[_currentIndex];
            if (target == null) return;

            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                _speed * Time.deltaTime);

            // Rotate to face movement direction smoothly
            Vector3 dir = (target.position - transform.position);
            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion lookRot = Quaternion.LookRotation(dir.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 10f * Time.deltaTime);
            }

            // Advance to next waypoint when close enough
            if (Vector3.Distance(transform.position, target.position) <= _waypointRadius)
                AdvanceWaypoint();
        }
        #endregion

        #region Private Methods
        private void AdvanceWaypoint()
        {
            if (_pingPong)
            {
                if (_movingForward)
                {
                    if (_currentIndex < _waypoints.Length - 1)
                        _currentIndex++;
                    else
                    {
                        _movingForward = false;
                        _currentIndex--;
                    }
                }
                else
                {
                    if (_currentIndex > 0)
                        _currentIndex--;
                    else
                    {
                        _movingForward = true;
                        _currentIndex++;
                    }
                }
            }
            else
            {
                // Loop: wrap around to start
                _currentIndex = (_currentIndex + 1) % _waypoints.Length;
            }
        }
        #endregion
    }
}
