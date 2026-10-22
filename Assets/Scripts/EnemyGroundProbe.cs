using UnityEngine;

internal static class EnemyGroundProbe
{
    public static bool FindFloor(Vector2 origin, float distance, RaycastHit2D[] hits, out RaycastHit2D floor)
    {
        ContactFilter2D filter = new ContactFilter2D { useTriggers = false };
        int count = Physics2D.Raycast(origin, Vector2.down, filter, hits, distance);
        floor = default;
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            // A ray starting inside a tile is already below its top surface.
            // Ignoring that hit keeps a runner from climbing out through a gap's side.
            if (hits[i].fraction > 0f && hits[i].collider.GetComponent<GroundBreak>() != null &&
                hits[i].normal.y > 0.5f && hits[i].distance < nearest)
            {
                nearest = hits[i].distance;
                floor = hits[i];
            }
        }
        return floor.collider != null;
    }

    public static bool IsBelowScreen(Vector2 position, Camera view)
    {
        return view != null && view.orthographic && position.y < view.transform.position.y - view.orthographicSize - 3f;
    }
}
