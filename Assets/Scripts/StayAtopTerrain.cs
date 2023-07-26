using UnityEngine;

public class StayAtopTerrain : MonoBehaviour
{
    [SerializeField] private LayerMask terrainLayerMask;
    [SerializeField] private float height = 5f; // Amount above transform position the object exists in
    [SerializeField] private bool fallToTerrain = false; // if this should not only rise to above terrain, but also fall to terrain

    private void Update()
    {
        // Send a raycast from far above the object down to right above it.
        // If it hits terrain, then object is below the terrain,
        // probably because a mountain was placed on it,
        // move the object to above

        // use a high check distance if fall to terrain, so object will check far below it for terrain
        // if not, ray should go only to just above the object.
        if (Physics.Raycast(transform.position + Vector3.up * (20f + height*0.5f), -transform.up, out RaycastHit hit, fallToTerrain ? 60f : 20f, terrainLayerMask))
        {
            // send to point plus half of capsule collider's height (5) because transform is in center of player
            transform.position = hit.point + Vector3.up * height * 0.5f;
        }
    }
}