using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBody : MonoBehaviour
{
    public BoxCollider2D colider;
    private RaycastOrigins rayOrigins;
    public CollisionInfo collisions;
    
    [SerializeField]
    private float skinWidth = .015f;

    public int rayCountHorizotal = 4;
    public int rayCountVertical = 4;
    float raySpaceHorizontal, raySpaceVertical;

    public float walkSpeed = 6;
    public float maxJumpHeight = 8;
    public float minJumpHeight = 1; 
    public float timeToApex = 1;
    public float accelerationRateAir = 0.2f;
    public float accelerationRateGround = 0.1f;

    public float climbAngleMax = 80;
    public float descendAngleMax = 75;

    private float gravity;
    private float maxJumpVelocity;
    private float minJumpVelocity;
    private Vector2 velocity;
    private float velocityXSmoothing;

    public LayerMask collisionMask;

    // Start is called before the first frame update
    void Awake()
    {
        colider = GetComponent<BoxCollider2D>();

        gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToApex, 2);
        maxJumpVelocity = Mathf.Abs(gravity) * timeToApex;
        minJumpVelocity = Mathf.Sqrt(2* Mathf.Abs(gravity) * minJumpHeight);

        
    }

    public virtual void Start()
    {
        CalculateRaySpace();
    }

    private void FixedUpdate()
    {
        if(collisions.up || collisions.down)
        {
            velocity.y = 0;
        }

        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if(Input.GetButtonDown("Jump") && collisions.down)
        {
            velocity.y = maxJumpVelocity;
        }
        if (Input.GetButtonUp("Jump"))
        {
            if (velocity.y > minJumpVelocity)
            {
                velocity.y = minJumpVelocity;
            }
        }

        float targetVelocityX = input.x * walkSpeed;
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (collisions.down)?accelerationRateGround:accelerationRateAir);
        velocity.y += gravity * Time.deltaTime;
        Move(velocity * Time.deltaTime);
    }

    public void Move(Vector2 velocity)
    {
        UpdateRayCastOrigins();
        collisions.Reset();
        collisions.velocityLast = velocity;

        if(velocity.y < 0)
        {
            DescendSlope(ref velocity);
        }
        if (velocity.x != 0)
        {
            CollisionsHorizontal(ref velocity);
        }

        if (velocity.y != 0)
        {
            CollisionsVertical(ref velocity);
        }

        transform.Translate(velocity);
    }

    void CollisionsHorizontal(ref Vector2 velocity)
    {
        float directionX = Mathf.Sign(velocity.x);
        float rayLength = Mathf.Abs(velocity.x) + skinWidth;

        for (int i = 0; i < rayCountHorizotal; i++)
        {
            Vector2 rayOrigin = (directionX == -1) ? rayOrigins.botLeft : rayOrigins.botRight;
            rayOrigin += Vector2.up * (raySpaceHorizontal * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLength, Color.green);

            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                if (i == 0 && slopeAngle <= climbAngleMax)
                {
                    if (collisions.descendingSlope)
                    {
                        collisions.descendingSlope = false;
                        velocity = collisions.velocityLast;
                    }

                    float slopeStartDistance = 0;
                    if(slopeAngle != collisions.slopeAngleLast)
                    {
                        slopeStartDistance = hit.distance - skinWidth;
                        velocity.x -= slopeStartDistance * directionX;
                    }

                    //Debug.Log("angle: " + slopeAngle);
                    ClimbSlope(ref velocity, slopeAngle);
                    velocity.x += slopeStartDistance * directionX;
                }

                if (!collisions.climbingSlope || slopeAngle > climbAngleMax)
                {
                    velocity.x = (hit.distance - skinWidth) * directionX;
                    rayLength = hit.distance;

                    if (collisions.climbingSlope)
                    {
                        velocity.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x);
                    }

                    collisions.left = directionX == -1;
                    collisions.right = directionX == 1;
                }
            }
        }
    }

    void CollisionsVertical(ref Vector2 velocity)
    {
        float directionY = Mathf.Sign(velocity.y);
        float rayLength = Mathf.Abs(velocity.y) + skinWidth;

        for (int i = 0; i < rayCountVertical; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? rayOrigins.botLeft : rayOrigins.topLeft;
            rayOrigin += Vector2.right * (raySpaceVertical * i + velocity.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength, Color.green);

            if (hit)
            {
                velocity.y = (hit.distance - skinWidth) * directionY; 
                rayLength = hit.distance;

                if (collisions.climbingSlope)
                {
                    velocity.x = velocity.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(velocity.x);
                }

                collisions.down = directionY == -1;
                collisions.up = directionY == 1;
            }
        }

        if (collisions.climbingSlope)
        {
            float directionX = Mathf.Sign(velocity.x);
            rayLength = Mathf.Abs(velocity.x) + skinWidth;
            Vector2 rayOrigin = ((directionX == -1) ? rayOrigins.botLeft : rayOrigins.botRight) + Vector2.up * velocity.y;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if(slopeAngle != collisions.slopeAngle)
                {
                    velocity.x = (hit.distance - skinWidth) * directionX;
                    collisions.slopeAngle = slopeAngle;
                }
            }
        }

    }

    void ClimbSlope(ref Vector2 velocity, float slopeAngle)
    {
        float moveDistance = Mathf.Abs(velocity.x);
        float climbVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

        if(velocity.y <= climbVelocityY)
        {
            velocity.y = climbVelocityY;
            velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);

            collisions.down = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
        }
    }

    void DescendSlope(ref Vector2 velocity)
    {
        float directionX = Mathf.Sign(velocity.x);
        Vector2 rayOrigin = (directionX == -1) ? rayOrigins.botRight : rayOrigins.botLeft;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

        if (hit)
        {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

            if(slopeAngle != 0 && slopeAngle <= descendAngleMax)
            {
                if(Mathf.Sign(hit.normal.x) == directionX)
                {
                    if(hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x))
                    {
                        float moveDistance = Mathf.Abs(velocity.x);
                        float descendVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
                        velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
                        velocity.y -= descendVelocityY;

                        collisions.slopeAngle = slopeAngle;
                        collisions.descendingSlope = true;
                        collisions.down = true;
                    }
                }
            }
        }
    }

    void CalculateRaySpace()
    {
        Bounds bounds = colider.bounds;
        bounds.Expand(skinWidth * -2);

        rayCountHorizotal = Mathf.Clamp(rayCountHorizotal, 2, int.MaxValue);
        rayCountVertical = Mathf.Clamp(rayCountVertical, 2, int.MaxValue);

        raySpaceHorizontal = bounds.size.y / (rayCountHorizotal - 1);
        raySpaceVertical = bounds.size.x / (rayCountVertical - 1);
    }

    void UpdateRayCastOrigins()
    {
        Bounds bounds = colider.bounds;
        bounds.Expand(skinWidth * -2);

        rayOrigins.botLeft = new Vector2(bounds.min.x, bounds.min.y);
        rayOrigins.botRight = new Vector2(bounds.max.x, bounds.min.y);
        rayOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        rayOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
    }

    struct RaycastOrigins
    {
        public Vector2 topLeft, topRight, botLeft, botRight;

    }

    public struct CollisionInfo
    {
        public bool up, down, left, right;

        public bool climbingSlope, descendingSlope;
        public float slopeAngle, slopeAngleLast;

        public Vector2 velocityLast;

        public void Reset()
        {
            up = down = left = right = false;
            climbingSlope = descendingSlope = false;

            slopeAngleLast = slopeAngle;
            slopeAngle = 0;
        }
    }
}
