using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public PlayerBody target;
    public Vector2 cameraTrapDimensions;

    public float verticalOffset;
    public float lookAheadDistX;
    public float smoothTimeX;
    public float smoothTimeY;

    private TrapArea focusArea;

    private float currentLookAheadX;
    private float targetLookAheadX;
    private float lookAheadDirX;
    private float smoothVelocityX;
    private float smoothVelocityY;

    void Start()
    {
        focusArea = new TrapArea(target.colider.bounds, cameraTrapDimensions);
    }

    void LateUpdate()
    {
        focusArea.Update(target.colider.bounds);

        Vector2 focusPosition = focusArea.center + Vector2.up * verticalOffset;

        if(focusArea.velocity.x != 0)
        {
            lookAheadDirX = Mathf.Sign(focusArea.velocity.x);
        }

        targetLookAheadX = lookAheadDirX * lookAheadDistX;
        currentLookAheadX = Mathf.SmoothDamp(currentLookAheadX, targetLookAheadX, ref smoothVelocityX, smoothTimeX);

        focusPosition.y = Mathf.SmoothDamp(transform.position.y, focusPosition.y, ref smoothVelocityY, smoothTimeY);
        focusPosition += Vector2.right * currentLookAheadX;
        transform.position = (Vector3)focusPosition + Vector3.forward * -10;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 1, .5f);
        Gizmos.DrawCube(focusArea.center, cameraTrapDimensions);
    }

    struct TrapArea
    {
        public Vector2 center;
        public Vector2 velocity;
        float left, right, up, down;

        public TrapArea(Bounds targetBounds, Vector2 size)
        {
            left = targetBounds.center.x - size.x / 2;
            right = targetBounds.center.x + size.x / 2;
            down = targetBounds.min.y;
            up = targetBounds.min.y + size.y;

            velocity = Vector2.zero;
            center = new Vector2((left + right) / 2, (up + down) / 2);
        }

        public void Update( Bounds targetBounds)
        {
            float shiftX = 0;
            if (targetBounds.min.x < left)
            {
                shiftX = targetBounds.min.x - left;
            }
            else if(targetBounds.max.x > right)
            {
                shiftX = targetBounds.max.x - right;
            }

            left += shiftX;
            right += shiftX;

            float shiftY = 0;
            if (targetBounds.min.y < down)
            {
                shiftY = targetBounds.min.y - down;
            }
            else if (targetBounds.max.y > up)
            {
                shiftY = targetBounds.max.y - up;
            }

            down += shiftY;
            up += shiftY;

            center = new Vector2((left + right) / 2, (up + down) / 2);

            velocity = new Vector2(shiftX, shiftY);
        }
    }

}
